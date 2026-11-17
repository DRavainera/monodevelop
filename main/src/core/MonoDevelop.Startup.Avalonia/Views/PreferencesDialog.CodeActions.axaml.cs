using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.CodeAnalysis.CodeRefactorings;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

// ---------- F4: Text Editor > Source Analysis > C# > Code Actions ----------
// Port of the legacy MonoDevelop.CodeActions.CodeActionPanel (its widget is
// ContextActionPanelWidget): a single-column list [toggle | name] over the C#
// refactorings with a search filter on top. The Avalonia port rebuilds that
// list in code (a ListBox of [CheckBox | name] rows) and wires the SAME
// persistence the GTK writes through CodeRefactoringDescriptor:
//   key   = "CodeActions.C#.<TypeFullName>" (Language is "C#", the
//           MimeTypeToLanguage("text/x-csharp") value; IdString is the provider
//           type's FullName).
//   load  = PropertyService.Get(key, true) -> enabled BY DEFAULT.
//   store = PropertyService.Set(key, value) -> the value is always written.
//           The only XML divergence from GTK: the GTK store layer deletes a
//           value that equals the default it registered in-process (so GTK
//           usually keeps just the disabled entries); the flat Avalonia store
//           keeps the "True" entries too. Value-compatible both ways: an absent
//           key reads back as enabled (default true) in both UIs.
//
// Enumeration: the legacy panel pulls its descriptors from
// MonoDevelopWorkspaceDiagnosticAnalyzerProviderService, which scans every
// loaded assembly that references Microsoft.CodeAnalysis for non-abstract
// [ExportCodeRefactoringProvider] types (OptionsTable.ProcessAssembly). In
// Roslyn 5.9.0 the ~82 C# providers live in the Microsoft.CodeAnalysis.*.Features
// assemblies, not in the two assemblies named in
// /MonoDevelop/Refactoring/AnalyzerAssemblies (the legacy list predates the
// Features split; the AppDomain scan is what actually surfaces them). The
// shell does the equivalent scan directly over the shared main/build closure:
// every Microsoft.CodeAnalysis*.dll that references Microsoft.CodeAnalysis is
// loaded (Assembly.LoadFrom, default context) and GetTypes()'d, catching
// ReflectionTypeLoadException exactly like the legacy ProcessAssembly (the
// missing Microsoft.CodeAnalysis.Elfie reference makes some types unloadable;
// the provider types are among the loadable ones). Only the attribute is read -
// the provider is never instantiated - so the Features/Bcl/Composition closure
// is not dragged into main/build. The ExportCodeRefactoringProviderAttribute
// type itself lives in Microsoft.CodeAnalysis.Workspaces, one of the core
// assemblies the shell already references, so no new dependency is needed.
//
// Filter quirk (replicated): the legacy FillTreeStore keeps a row when
// Name.IndexOf(filter, OrdinalIgnoreCase) > 0 - STRICTLY greater - so a filter
// matching at position 0 (e.g. "add" against "Add ...") hides the row; rows
// are ordered by Name (StringComparer.Ordinal) and empty names are dropped.
// The legacy Pango markup match-highlight (Util.MarkupSearchResult) has no
// Avalonia equivalent: the rows render as plain text.
partial class PreferencesDialog
{
	sealed class CodeActionRow
	{
		public string Name;         // ExportCodeRefactoringProviderAttribute.Name
		public string TypeFullName; // CodeRefactoringDescriptor.IdString
		public bool State;          // legacy providerStates (enabled)
		public CheckBox? Toggle;
	}

	// CodeRefactoringService.MimeTypeToLanguage("text/x-csharp").
	const string CaLanguage = "C#";

	readonly List<CodeActionRow> caRows = new ();

	static string CaKey (CodeActionRow row) => "CodeActions." + CaLanguage + "." + row.TypeFullName;

	// The rows currently rendered in the list (what the filter lets through).
	IEnumerable<CodeActionRow> VisibleCaRows (string filter) =>
		caRows
			.Where (row => !string.IsNullOrEmpty (row.Name) &&
				(string.IsNullOrEmpty (filter) || row.Name.IndexOf (filter, StringComparison.OrdinalIgnoreCase) > 0))
			.OrderBy (row => row.Name, StringComparer.Ordinal);

	/// <summary>Scans the shared build closure for the C# code refactoring
	/// providers (the legacy ProcessAssembly rule over every
	/// Microsoft.CodeAnalysis*.dll that references Microsoft.CodeAnalysis).</summary>
	List<CodeActionRow> ScanCodeRefactoringProviders ()
	{
		var rows = new List<CodeActionRow> ();
		var dir = Path.GetDirectoryName (Assembly.GetEntryAssembly ()?.Location);
		if (dir is null) {
			MainWindow.Instance?.Output ("[prefs-codeactions] scan failed: no entry assembly directory");
			return rows;
		}
		var seen = new HashSet<string> (StringComparer.Ordinal);
		foreach (var path in Directory.GetFiles (dir, "Microsoft.CodeAnalysis*.dll").OrderBy (x => x, StringComparer.Ordinal)) {
			string name;
			try {
				name = AssemblyName.GetAssemblyName (path).Name ?? "";
			} catch {
				continue;
			}
			if (name.Length == 0 || !seen.Add (name))
				continue;

			Assembly asm;
			try {
				asm = Assembly.LoadFrom (Path.GetFullPath (path));
			} catch (Exception ex) {
				MainWindow.Instance?.Output ("[prefs-codeactions] load failed " + name + ": " + ex.Message);
				continue;
			}

			bool refsRoslyn;
			try {
				refsRoslyn = asm.GetReferencedAssemblies ().Any (a => a.Name == "Microsoft.CodeAnalysis");
			} catch {
				refsRoslyn = true;
			}
			if (!refsRoslyn)
				continue;

			Type[] types;
			try {
				types = asm.GetTypes ();
			} catch (ReflectionTypeLoadException ex) {
				// Legacy ProcessAssembly: log the loader exceptions and keep the
				// types that did load (the providers are among them).
				types = ex.Types.Where (t => t != null).ToArray ();
			} catch (Exception ex) {
				MainWindow.Instance?.Output ("[prefs-codeactions] types failed " + name + ": " + ex.Message);
				continue;
			}

			foreach (var t in types) {
				if (t.IsAbstract)
					continue;
				var attr = t.GetCustomAttributes (typeof (ExportCodeRefactoringProviderAttribute), false).FirstOrDefault () as ExportCodeRefactoringProviderAttribute;
				if (attr is null)
					continue;
				// Legacy filter: Language (first attr language) contains "C#".
				var lang = (attr.Languages ?? Array.Empty<string> ()).FirstOrDefault () ?? "";
				if (!lang.Contains (CaLanguage, StringComparison.Ordinal))
					continue;
				rows.Add (new CodeActionRow {
					Name = attr.Name ?? "",
					TypeFullName = t.FullName ?? "",
				});
			}
		}
		return rows;
	}

	void RebuildCaList (string filter)
	{
		CaList.Items.Clear ();
		foreach (var row in VisibleCaRows (filter)) {
			// Single legacy column [toggle | name]: the toggle is fixed-width,
			// the name takes the rest (legacy PackStart(toggle,false) +
			// PackStart(text,true) with end ellipsis).
			var grid = new Grid { ColumnDefinitions = new ColumnDefinitions ("Auto,*") };
			row.Toggle = new CheckBox { IsChecked = row.State, VerticalAlignment = VerticalAlignment.Center };
			row.Toggle.IsCheckedChanged += OnCaToggled;
			Grid.SetColumn (row.Toggle, 0);
			var nameBlock = new TextBlock {
				Text = row.Name,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness (8, 0, 0, 0),
				TextTrimming = TextTrimming.CharacterEllipsis,
			};
			Grid.SetColumn (nameBlock, 1);
			grid.Children.Add (row.Toggle);
			grid.Children.Add (nameBlock);
			CaList.Items.Add (new ListBoxItem { Content = grid });
		}
	}

	void OnCaFilterChanged (object? sender, TextChangedEventArgs e) => ApplyCaFilter ();

	// Legacy ApplyFilter: re-fill the list with the trimmed filter text. The
	// TextChanged event is wired in the XAML for the interactive path; the QA
	// oracle drives this directly because in the headless oracle (the dialog is
	// never shown) the TextChanged raise is deferred until teardown, so the
	// intermediate filter states would be missed (same explicit-drive pattern as
	// the F3 oracle, which calls the preview update directly).
	void ApplyCaFilter () => RebuildCaList (CaFilter.Text.Trim ());

	void OnCaToggled (object? sender, RoutedEventArgs e)
	{
		if (sender is not CheckBox cb)
			return;
		var row = caRows.FirstOrDefault (r => ReferenceEquals (r.Toggle, cb));
		if (row is null)
			return;
		row.State = cb.IsChecked == true; // legacy providerStates[provider]
	}

	void LoadCodeActionsPanel ()
	{
		caRows.Clear ();
		try {
			caRows.AddRange (ScanCodeRefactoringProviders ());
			// Legacy: IsEnabled getter = PropertyService.Get(key, true).
			foreach (var row in caRows)
				row.State = SettingsStore.GetBool (CaKey (row), true);
			RebuildCaList (null);
			MainWindow.Instance?.Output ("[prefs-codeactions] loaded: providers=" + caRows.Count);
		} catch (Exception ex) {
			MainWindow.Instance?.Output ("[prefs-codeactions] load failed: " + ex.Message);
		}
	}

	// Legacy ApplyChanges: descriptor.IsEnabled = providerStates[descriptor] for
	// every provider (the setter always writes the value).
	void StoreCodeActionsPanel ()
	{
		if (caRows.Count == 0) {
			MainWindow.Instance?.Output ("[prefs-codeactions] stored: skipped (load failed)");
			return;
		}
		foreach (var row in caRows)
			SettingsStore.SetBool (CaKey (row), row.State);
		MainWindow.Instance?.Output ("[prefs-codeactions] stored: rows=" + caRows.Count);
	}

	/// <summary>QA oracle for the Code Actions port (expected on a virgin profile,
	/// Roslyn 5.9.0 closure): the 82 C# providers enumerate and load enabled by
	/// default; the filter replicates the legacy IndexOf > 0 quirk ("add" hides
	/// every "Add ..." row -> 0, "provider" -> 38); toggling two providers off and
	/// storing round-trips through CodeActions.C#.&lt;TypeFullName&gt;; re-enabling
	/// still writes the key (always-write, no default-delete); then the previous
	/// key values are restored (no residue).</summary>
	public void QaCodeActionsOracle ()
	{
		if (caRows.Count == 0) {
			MainWindow.Instance?.Output ("[prefs-codeactions] oracle: load failed, nothing to test");
			return;
		}
		var byName = caRows.ToDictionary (r => r.Name, StringComparer.Ordinal);
		var rOrganize = byName ["OrganizeImports"];
		var rRename = byName ["RenameTracking"];
		var keys = caRows.Select (CaKey).ToArray ();
		// Snapshot every key ("" == absent) so the restore leaves no residue.
		var prev = keys.Select (k => SettingsStore.GetString (k) ?? "").ToArray ();
		try {
			// Defaults: a virgin profile has no CodeActions.* keys -> all enabled.
			MainWindow.Instance?.Output ("[prefs-codeactions] defaults: all-enabled=" + caRows.All (r => r.State));

			// Filter quirk (legacy IndexOf > 0): "add" matches at position 0 in
			// every "Add ..." name -> hidden (0 rows); "provider" sits mid-name
			// -> 38 rows. Driven through the same ApplyCaFilter the TextChanged
			// handler calls (explicit, because the headless oracle defers the
			// event until teardown).
			CaFilter.Text = "add";
			ApplyCaFilter ();
			var fAdd = CaList.Items.Count;
			CaFilter.Text = "provider";
			ApplyCaFilter ();
			var fProvider = CaList.Items.Count;
			CaFilter.Text = "";
			ApplyCaFilter ();
			var fAll = CaList.Items.Count;
			var firstItem = (ListBoxItem)CaList.Items[0];
			var firstText = (TextBlock)((Grid)firstItem.Content!).Children[1];
			MainWindow.Instance?.Output ("[prefs-codeactions] filter: all=" + fAll + " provider=" + fProvider
				+ " add=" + fAdd + " first='" + firstText.Text + "'");

			// Toggle two providers off through the real toggle path, keep one at
			// its default (enabled), store, then re-read from the store.
			rOrganize.Toggle.IsChecked = false;
			rRename.Toggle.IsChecked = false;
			StoreCodeActionsPanel ();
			bool ok = SettingsStore.GetBool (CaKey (rOrganize), true) == false
				&& SettingsStore.GetBool (CaKey (rRename), true) == false
				&& SettingsStore.GetBool (CaKey (byName ["Add Await Code Action Provider"]), true) == true;
			MainWindow.Instance?.Output ("[prefs-codeactions] roundtrip: toggled-off stored ok=" + ok
				+ " value='" + (SettingsStore.GetString (CaKey (rOrganize)) ?? "absent") + "'");

			// Always-write: flipping back to the default (enabled) still writes
			// the key (the flat store has no default-delete; GTK deletes it at
			// the store layer, so the XML differs but the value reads the same).
			rOrganize.Toggle.IsChecked = true;
			StoreCodeActionsPanel ();
			MainWindow.Instance?.Output ("[prefs-codeactions] always-write: re-enabled value='"
				+ (SettingsStore.GetString (CaKey (rOrganize)) ?? "absent") + "'");
		} finally {
			for (int i = 0; i < keys.Length; i++)
				SettingsStore.SetString (keys[i], prev[i] == "" ? null : prev[i]);
			MainWindow.Instance?.Output ("[prefs-codeactions] restored previous keys");
		}
	}
}
