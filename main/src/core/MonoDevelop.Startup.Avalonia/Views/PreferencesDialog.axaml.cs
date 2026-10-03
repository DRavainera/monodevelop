using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy OptionsDialog: the left tree mirrors the section order of
/// GlobalOptionsDialog.addin.xml plus the add-in extensions (categories →
/// sections → sub-panels), and the right pane shows the section icon + title
/// header followed by the panel page. The wired panels read/write the exact keys
/// and file formats of the GTK UI (MonoDevelopProperties.xml, Custom.kb.xml,
/// MonoDevelop-tools.xml), so both UIs share settings.
/// </summary>
public partial class PreferencesDialog : Window
{
	// A section-tree node. Empty Id = category heading (not a page).
	sealed record PrefsNode (string Id, string Label, string Icon, List<PrefsNode> Children)
	{
		public bool IsCategory => string.IsNullOrEmpty (Id);
	}

	// Legacy language list (LocalizationService.defaultLocaleSet, same order/cultures).
	static readonly (string Culture, string DisplayName)[] LocaleSet = {
		("", "(Default)"),
		("ca", "Català"),
		("zh_CN", "中文 - 中国"),
		("zh_TW", "中文 - 台灣"),
		("cs", "Čeština"),
		("da", "Dansk"),
		("de", "Deutsch"),
		("nl", "Dutch"),
		("fr", "Français"),
		("gl", "Galego"),
		("en", "English"),
		("es", "Español"),
		("hu", "Magyar"),
		("id", "Indonesian"),
		("it", "Italiano"),
		("ja", "日本語"),
		("ko", "한국어"),
		("pl", "Polski"),
		("pt", "Português"),
		("pt_BR", "Português – Brasil"),
		("ru", "Русский"),
		("sl", "Slovenščina"),
		("sv", "Svenska"),
		("tr", "Türkçe"),
	};

	const string LanguageKey = "MonoDevelop.Ide.UserInterfaceLanguage";

	// Panel ids wired to a real page; everything else falls back to the placeholder.
	static readonly HashSet<string> functionalPanels = new () {
		"style", "author", "keybindings", "fonts", "updates", "tasks",
		"externaltools", "loadsave", "build", "buildmessages", "feedback", "maintenance",
		// Text editor group (ported from the SourceEditor2 add-in panels, same keys).
		"general", "markers", "behavior", "intellisense",
		"colortheme", "codesnippets", "languagebundles",
	};

	string? pendingLanguage;
	string? storedLanguage;
	bool updatingDetails;

	// Editable models of the panels (re-stored on OK like OptionsPanel.ApplyChanges).
	readonly Dictionary<string, string> keyBindings = new ();
	List<SettingsStore.ExternalTool> tools = new ();

	public PreferencesDialog ()
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		ThemeDarkRadio!.IsCheckedChanged += OnThemeRadioChecked;
		ThemeLightRadio!.IsCheckedChanged += OnThemeRadioChecked;
		LoadLanguagePanel ();
		LoadAuthorPanel ();
		LoadKeyBindingsPanel ();
		LoadFontsPanel ();
		LoadUpdatesPanel ();
		LoadTasksPanel ();
		LoadToolsPanel ();
		LoadFeedbackPanel ();
		LoadLoadSavePanel ();
		LoadBuildPanel ();
		LoadBuildMessagesPanel ();
		LoadMaintenancePanel ();
		LoadGeneralPanel ();
		LoadMarkersPanel ();
		LoadBehaviorPanel ();
		LoadIntelliSensePanel ();
		LoadColorThemePanel ();
		LoadSnippetsPanel ();
		LoadLanguageBundlesPanel ();
		GenWordWrap!.IsCheckedChanged += (_, _) => GenWordWrapGlyphs!.IsEnabled = GenWordWrap.IsChecked == true;
		BhAutoInsertBrace!.IsCheckedChanged += (_, _) => BhSmartSemicolon!.IsEnabled = BhAutoInsertBrace.IsChecked == true;
		BuildSectionTree ();
		// Default selection: Visual Style (the first functional panel), like the GTK
		// dialog opens on the first selectable section.
		SelectPanel ("style");
	}

	// ---------- Section tree (legacy GlobalOptionsDialog.addin.xml + addin extensions) ----------

	static PrefsNode Cat (string label, params PrefsNode[] children) => new ("", label, "", children.ToList ());
	static PrefsNode Leaf (string id, string label, string icon) => new (id, label, icon, new List<PrefsNode> ());

	static List<PrefsNode> BuildModel () => new () {
		Cat ("Environment",
			Leaf ("style", "Visual Style", "md-prefs-visual-style"),
			Leaf ("author", "Author Information", "md-prefs-author-information"),
			Leaf ("keybindings", "Key Bindings", "md-prefs-key-bindings"),
			Leaf ("fonts", "Fonts", "md-prefs-fonts"),
			Leaf ("updates", "Updates", "md-prefs-updates"),
			Leaf ("tasks", "Tasks", "md-prefs-task-list"),
			Leaf ("externaltools", "External Tools", "md-prefs-external-tools")),
		Cat ("Projects",
			Leaf ("loadsave", "Load/Save", "md-prefs-load-save"),
			new PrefsNode ("build", "Build", "md-prefs-build", new () {
				Leaf ("buildmessages", "Errors and Warnings", "md-prefs-build"),
			}),
			// Shown by the legacy only under the RUNTIME_SELECTOR feature switch; the
			// shell lists it always (net10-only) as an informative placeholder.
			Leaf ("runtimes", ".NET Runtimes", "md-prefs-generic"),
			Leaf ("sdklocations", "SDK Locations", "md-prefs-sdk-locations"),
			Leaf ("debugger", "Debugger", "md-prefs-generic"),
			Leaf ("gtkdesigner", "GTK# Designer", "md-prefs-generic")),
		Cat ("Text Editor",
			Leaf ("general", "General", "md-prefs-generic"),
			Leaf ("markers", "Markers and Rulers", "md-prefs-generic"),
			new PrefsNode ("behavior", "Behavior", "md-prefs-generic", new () {
				Leaf ("xml", "XML", "md-prefs-generic"),
				Leaf ("csharpformat", "C#", "md-prefs-code-formatting"),
			}),
			new PrefsNode ("intellisense", "IntelliSense", "md-prefs-generic", new () {
				Leaf ("intellisense-behavior", "Behavior", "md-prefs-generic"),
				Leaf ("intellisense-appearance", "Appearance", "md-prefs-generic"),
			}),
			Leaf ("colortheme", "Color Theme", "md-prefs-generic"),
			Leaf ("formatting", "Formatting", "md-prefs-code-formatting"),
			Leaf ("codesnippets", "Code Snippets", "md-prefs-code-templates"),
			Leaf ("languagebundles", "Language Bundles", "md-prefs-generic"),
			new PrefsNode ("analysis", "Source Analysis", "md-prefs-generic", new () {
				Leaf ("analysis-csharp", "C#", "md-prefs-generic"),
			}),
			Leaf ("xmlschemas", "XML Schemas", "md-prefs-generic")),
		Cat ("Source Code",
			Leaf ("naming", ".NET Naming Policies", "md-prefs-dotnet-naming-policies"),
			Leaf ("codeformatting", "Code Formatting", "md-prefs-code-formatting"),
			Leaf ("standardheader", "Standard Header", "md-prefs-header")),
		Cat ("Version Control",
			Leaf ("vcgeneral", "General", "md-prefs-solution"),
			Leaf ("vccommit", "Commit Message Style", "md-prefs-solution"),
			Leaf ("git", "Git", "md-prefs-solution"),
			Leaf ("changelog", "ChangeLog Integration", "md-prefs-generic")),
		Cat ("NuGet",
			Leaf ("nugetgeneral", "General", "md-prefs-generic"),
			Leaf ("packagesources", "Sources", "md-prefs-generic")),
		Cat ("Other",
			Leaf ("feedback", "Feedback", "md-prefs-feedback"),
			Leaf ("maintenance", "MonoDevelop Maintenance", "md-prefs-maintenance"),
			Leaf ("fsharp", "F# Settings", "md-prefs-source")),
		Cat ("Performance Diagnostics",
			Leaf ("perfgeneral", "General", "md-prefs-performance")),
	};

	void BuildSectionTree ()
	{
		foreach (var node in BuildModel ()) {
			var item = MakeTreeItem (node);
			item.IsExpanded = true; // expand everything like the GTK tree
			SectionTree!.Items.Add (item);
		}
	}

	static TreeViewItem MakeTreeItem (PrefsNode node)
	{
		var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		if (!string.IsNullOrEmpty (node.Icon)) {
			var img = new Image { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center };
			SetIcon (img, node.Icon);
			header.Children.Add (img);
		}
		header.Children.Add (new TextBlock { Text = node.Label, VerticalAlignment = VerticalAlignment.Center });
		var item = new TreeViewItem { Header = header, Tag = node };
		foreach (var child in node.Children)
			item.Items.Add (MakeTreeItem (child));
		return item;
	}

	static void SetIcon (Image image, string stockId)
	{
		if (!string.IsNullOrEmpty (stockId) && IconService.GetImage (stockId) is Bitmap bmp)
			image.Source = bmp;
		else
			image.Source = null;
	}

	/// <summary>Selects a section by id, mirroring OptionsDialog.SelectPanel.</summary>
	public void SelectPanel (string panelId)
	{
		// Back-compat: the language selector lives inside Visual Style (legacy).
		if (panelId == "language")
			panelId = "style";
		var item = FindItem (SectionTree!.Items, panelId);
		if (item is null)
			return;
		ExpandAncestors (SectionTree.Items, item);
		item.IsSelected = true;
		SectionTree.SelectedItem = item;
	}

	static TreeViewItem? FindItem (Avalonia.Controls.ItemCollection items, string id)
	{
		foreach (var it in items.OfType<TreeViewItem> ()) {
			if (it.Tag is PrefsNode { Id: var nid } && nid == id && !string.IsNullOrEmpty (id))
				return it;
			var sub = FindItem (it.Items, id);
			if (sub is not null)
				return sub;
		}
		return null;
	}

	static bool ExpandAncestors (Avalonia.Controls.ItemCollection items, TreeViewItem target)
	{
		foreach (var it in items.OfType<TreeViewItem> ()) {
			if (ReferenceEquals (it, target))
				return true;
			if (ExpandAncestors (it.Items, target)) {
				it.IsExpanded = true;
				return true;
			}
		}
		return false;
	}

	/// <summary>QA: dumps the section tree (id, label, icon and whether the icon resolves).</summary>
	public void DumpTreeForQa ()
	{
		foreach (var node in BuildModel ())
			Dump (node, 0);
		void Dump (PrefsNode n, int depth)
		{
			var pad = new string (' ', depth * 2);
			var kind = n.IsCategory ? "cat" : "section";
			var icon = string.IsNullOrEmpty (n.Icon) ? "-" :
				(IconService.GetImage (n.Icon) is not null ? n.Icon : n.Icon + " (missing)");
			MainWindow.Instance?.Output ($"[prefs-tree] {pad}{kind}: {n.Label} id={n.Id} icon={icon}");
			foreach (var c in n.Children)
				Dump (c, depth + 1);
		}
	}

	// ---------- Language ----------

	void LoadLanguagePanel ()
	{
		storedLanguage = SettingsStore.GetString (LanguageKey);
		foreach (var locale in LocaleSet)
			LanguageCombo!.Items.Add (new ComboBoxItem { Content = locale.DisplayName, Tag = locale.Culture });
		var index = Array.FindIndex (LocaleSet, l => l.Culture == storedLanguage);
		if (index < 0) index = 0;
		LanguageCombo!.SelectedIndex = index;
		LanguageCombo.SelectionChanged += OnLanguageChanged;
		pendingLanguage = null;
		UpdateLanguageRestartUi ();
	}

	void OnLanguageChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (updatingDetails || LanguageCombo?.SelectedItem is not ComboBoxItem item)
			return;
		pendingLanguage = item.Tag as string;
		UpdateLanguageRestartUi ();
	}

	void UpdateLanguageRestartUi ()
	{
		var changed = pendingLanguage != storedLanguage;
		LanguageRestartRow!.IsVisible = changed && !string.IsNullOrEmpty (pendingLanguage);
		LanguageRestartNote!.Opacity = changed ? 1.0 : 0.8;
	}

	void OnRestartClicked (object? sender, RoutedEventArgs e)
	{
		// Legacy IDEStyleOptionsPanel.RestartClicked: persist + relaunch.
		SettingsStore.SetString (LanguageKey, pendingLanguage);
		MainWindow.Instance?.Output ("[prefs] language stored (" + pendingLanguage + ") — restarting");
		RestartShell ();
	}

	static void RestartShell ()
	{
		try {
			var exe = System.Diagnostics.Process.GetCurrentProcess ().MainModule?.FileName;
			var args = Environment.GetCommandLineArgs ().Skip (1);
			if (exe is not null) {
				System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo (exe, string.Join (" ", args)) {
					UseShellExecute = true,
				});
			}
			Environment.Exit (0);
		} catch (Exception ex) {
			Console.WriteLine ("[prefs] restart failed: " + ex.Message);
		}
	}

	// ---------- Author Information ----------

	void LoadAuthorPanel ()
	{
		AuthName!.Text = SettingsStore.GetString ("Author.Name") ?? Environment.UserName;
		AuthEmail!.Text = SettingsStore.GetString ("Author.Email") ?? "";
		AuthCopyright!.Text = SettingsStore.GetString ("Author.Copyright") ?? "";
		AuthCompany!.Text = SettingsStore.GetString ("Author.Company") ?? "";
		AuthTrademark!.Text = SettingsStore.GetString ("Author.Trademark") ?? "";
	}

	void StoreAuthorPanel ()
	{
		SettingsStore.SetString ("Author.Name", NullIfEmpty (AuthName!.Text));
		SettingsStore.SetString ("Author.Email", NullIfEmpty (AuthEmail!.Text));
		SettingsStore.SetString ("Author.Copyright", NullIfEmpty (AuthCopyright!.Text));
		SettingsStore.SetString ("Author.Company", NullIfEmpty (AuthCompany!.Text));
		SettingsStore.SetString ("Author.Trademark", NullIfEmpty (AuthTrademark!.Text));
	}

	static string? NullIfEmpty (string? s) => string.IsNullOrWhiteSpace (s) ? null : s;

	// ---------- Key Bindings ----------

	// The commands the editor can bind: menu commands present in the running menu
	// (KeyboardShortcutRegistry), like the legacy panel lists Commands.addin.xml ones.
	void LoadKeyBindingsPanel ()
	{
		foreach (var kv in SettingsStore.LoadKeyBindings ())
			keyBindings [kv.Key] = kv.Value;
		KbCommandsList!.SelectionChanged += (_, _) => ShowBindingForSelection ();
		RebuildKeyBindingList ();
	}

	void RebuildKeyBindingList ()
	{
		var bindings = MainWindow.Instance?.MenuCommandBindings ()
			?? Array.Empty<(string, string)> ();
		KbCommandsList!.Items.Clear ();
		foreach (var (commandId, label) in bindings.OrderBy (b => b.Item2, StringComparer.OrdinalIgnoreCase)) {
			keyBindings.TryGetValue (commandId, out var custom);
			KbCommandsList.Items.Add (new ListBoxItem {
				Tag = commandId,
				Content = label + (string.IsNullOrEmpty (custom) ? "" : $"   —  {custom}"),
			});
		}
	}

	void ShowBindingForSelection ()
	{
		if (KbCommandsList?.SelectedItem is ListBoxItem { Tag: string cmd })
			KbAccelEntry!.Text = keyBindings.TryGetValue (cmd, out var g) ? g : "";
	}

	void OnKbAccelKeyDown (object? sender, KeyEventArgs e)
	{
		// Capture the pressed combination as an Avalonia gesture string (Control+S),
		// the same format Custom.kb.xml stores.
		if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
			or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System)
			return;
		var gesture = new KeyGesture (e.Key, e.KeyModifiers & ~KeyModifiers.Meta).ToString ();
		KbAccelEntry!.Text = gesture;
		e.Handled = true;
	}

	void OnKbUpdate (object? sender, RoutedEventArgs e)
	{
		if (KbCommandsList?.SelectedItem is not ListBoxItem { Tag: string cmd }
			|| string.IsNullOrEmpty (KbAccelEntry!.Text))
			return;
		keyBindings [cmd] = KbAccelEntry.Text;
		SettingsStore.SaveKeyBindings (keyBindings);
		MainWindow.Instance?.RebuildMenu ();
		RebuildKeyBindingList ();
		MainWindow.Instance?.Output ("[prefs] binding updated: " + cmd);
	}

	void OnKbRemove (object? sender, RoutedEventArgs e)
	{
		if (KbCommandsList?.SelectedItem is not ListBoxItem { Tag: string cmd })
			return;
		keyBindings [cmd] = "";
		SettingsStore.SaveKeyBindings (keyBindings);
		MainWindow.Instance?.RebuildMenu ();
		RebuildKeyBindingList ();
	}

	// ---------- Fonts ----------

	// Legacy FontsPanel stores the nested FontProperties property:
	// <Property key="FontProperties"><Properties><Property key="Editor" value="..."/>...
	// Roles: Editor / Pad / OutputPad; specs are "Family Size".
	static readonly string[] FontRoles = { "Editor", "Pad", "OutputPad" };
	static readonly string[] FontDefaults = { "Monospace 12", "Sans 11", "Monospace 11" };

	void LoadFontsPanel ()
	{
		// Avalonia 12 has no font-enumeration API; use the legacy fixed family catalog
		// (fontconfig families present in the run environments, GTK ordered similar).
		string[] families = {
			"DejaVu Sans Mono", "Liberation Mono", "Noto Sans Mono", "Adwaita Mono",
			"Ubuntu Mono", "Courier New", "Consolas", "Andale Mono",
			"DejaVu Sans", "Liberation Sans", "Noto Sans", "Segoe UI", "Ubuntu",
		};
		var combos = new[] { FontEditorCombo!, FontPadCombo!, FontOutputCombo! };
		for (int i = 0; i < combos.Length; i++) {
			foreach (var fam in families)
				combos [i].Items.Add (fam);
			var (family, size) = ParseFontSpec (SettingsStore.GetFontSpec (FontRoles [i]), FontDefaults [i]);
			combos [i].SelectedItem = families.Contains (family) ? family : families [0];
			((TextBox) (i == 0 ? FontEditorSize! : i == 1 ? FontPadSize! : FontOutputSize!)).Text = size;
		}
	}

	static (string, string) ParseFontSpec (string? spec, string def)
	{
		var s = string.IsNullOrWhiteSpace (spec) ? def : spec.Trim ();
		var sp = s.LastIndexOf (' ');
		if (sp <= 0 || !double.TryParse (s [(sp + 1)..], out var _))
			return (s, "12");
		return (s[..sp], s [(sp + 1)..]);
	}

	void OnFontApply (object? sender, RoutedEventArgs e)
	{
		SettingsStore.SetFontSpec ("Editor", $"{FontEditorCombo!.SelectedItem} {FontEditorSize!.Text}");
		SettingsStore.SetFontSpec ("Pad", $"{FontPadCombo!.SelectedItem} {FontPadSize!.Text}");
		SettingsStore.SetFontSpec ("OutputPad", $"{FontOutputCombo!.SelectedItem} {FontOutputSize!.Text}");
		MainWindow.Instance?.ApplyFontPreferences ();
		MainWindow.Instance?.Output ("[prefs] fonts applied");
	}

	void OnFontReset (object? sender, RoutedEventArgs e)
	{
		SettingsStore.ClearFonts ();
		LoadFontsPanel ();
		MainWindow.Instance?.ApplyFontPreferences ();
	}

	// ---------- Updates ----------

	void LoadUpdatesPanel ()
		=> UpdatesCheck!.IsChecked = SettingsStore.GetBool ("MonoDevelop.Ide.AddinUpdater.CheckForUpdates", true);

	void StoreUpdatesPanel ()
		=> SettingsStore.SetBool ("MonoDevelop.Ide.AddinUpdater.CheckForUpdates", UpdatesCheck!.IsChecked == true);

	// ---------- Tasks ----------

	void LoadTasksPanel ()
	{
		TaskHighEntry!.Text = SettingsStore.GetTaskColor ("Monodevelop.UserTasksHighPrioColor") ?? "#FF8080";
		TaskNormalEntry!.Text = SettingsStore.GetTaskColor ("Monodevelop.UserTasksNormalPrioColor") ?? "#8080FF";
		TaskLowEntry!.Text = SettingsStore.GetTaskColor ("Monodevelop.UserTasksLowPrioColor") ?? "#80FF80";
	}

	void StoreTasksPanel ()
	{
		SettingsStore.SetTaskColor ("Monodevelop.UserTasksHighPrioColor", TaskHighEntry!.Text.Trim ());
		SettingsStore.SetTaskColor ("Monodevelop.UserTasksNormalPrioColor", TaskNormalEntry!.Text.Trim ());
		SettingsStore.SetTaskColor ("Monodevelop.UserTasksLowPrioColor", TaskLowEntry!.Text.Trim ());
	}

	// ---------- External Tools ----------

	void LoadToolsPanel ()
	{
		tools = SettingsStore.LoadTools ();
		RebuildToolsList ();
		ToolsList!.SelectionChanged += (_, _) => ShowToolFields ();
	}

	void RebuildToolsList ()
	{
		ToolsList!.Items.Clear ();
		foreach (var t in tools)
			ToolsList.Items.Add (new ListBoxItem { Tag = t, Content = string.IsNullOrEmpty (t.MenuCommand) ? "(unnamed)" : t.MenuCommand });
		ToolsList.SelectedIndex = tools.Count > 0 ? 0 : -1;
	}

	void ShowToolFields ()
	{
		if (ToolsList?.SelectedItem is ListBoxItem { Tag: SettingsStore.ExternalTool t }) {
			ToolMenuCommand!.Text = t.MenuCommand;
			ToolCommand!.Text = t.Command;
			ToolArguments!.Text = t.Arguments;
			ToolInitialDirectory!.Text = t.InitialDirectory;
		}
	}

	void OnToolAdd (object? sender, RoutedEventArgs e)
	{
		var t = new SettingsStore.ExternalTool { MenuCommand = "New tool", Command = "", Arguments = "", InitialDirectory = "" };
		tools.Add (t);
		SettingsStore.SaveTools (tools);
		RebuildToolsList ();
		ToolsList.SelectedIndex = tools.Count - 1;
	}

	void OnToolRemove (object? sender, RoutedEventArgs e)
	{
		if (ToolsList?.SelectedItem is ListBoxItem { Tag: SettingsStore.ExternalTool t }) {
			tools.Remove (t);
			SettingsStore.SaveTools (tools);
			RebuildToolsList ();
		}
	}

	void StoreToolFields ()
	{
		if (ToolsList?.SelectedItem is ListBoxItem { Tag: SettingsStore.ExternalTool t }) {
			t.MenuCommand = ToolMenuCommand!.Text;
			t.Command = ToolCommand!.Text;
			t.Arguments = ToolArguments!.Text;
			t.InitialDirectory = ToolInitialDirectory!.Text;
			SettingsStore.SaveTools (tools);
		}
	}

	// ---------- Feedback ----------

	void LoadFeedbackPanel ()
	{
		var raw = SettingsStore.GetBool ("MonoDevelop.LogAgent.ReportCrashes", true);
		FbReportCheck!.IsChecked = raw && SettingsStore.GetBool ("MonoDevelop.LogAgent.ReportUsage", true);
	}

	void StoreFeedbackPanel ()
	{
		var on = FbReportCheck!.IsChecked == true;
		SettingsStore.SetBool ("MonoDevelop.LogAgent.ReportCrashes", on);
		SettingsStore.SetBool ("MonoDevelop.LogAgent.ReportUsage", on);
	}

	// ---------- Load/Save ----------

	void LoadLoadSavePanel ()
	{
		LsDefaultPath!.Text = SettingsStore.GetString ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath")
			?? Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Projects");
		LsLoadUserProps!.IsChecked = SettingsStore.GetBool ("SharpDevelop.LoadDocumentProperties", true);
		LsBackupCopies!.IsChecked = SettingsStore.GetBool ("SharpDevelop.CreateBackupCopy", false);
		var startup = SettingsStore.GetString ("MonoDevelop.Ide.StartupBehaviour") ?? "ShowStartWindow";
		LsStartupStart!.IsChecked = startup == "ShowStartWindow";
		LsStartupPrev!.IsChecked = startup == "LoadPreviousSolution";
		LsStartupEmpty!.IsChecked = startup == "EmptyEnvironment";
	}

	void StoreLoadSavePanel ()
	{
		SettingsStore.SetString ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", NullIfEmpty (LsDefaultPath!.Text));
		SettingsStore.SetBool ("SharpDevelop.LoadDocumentProperties", LsLoadUserProps!.IsChecked == true);
		SettingsStore.SetBool ("SharpDevelop.CreateBackupCopy", LsBackupCopies!.IsChecked == true);
		string startup = LsStartupPrev!.IsChecked == true ? "LoadPreviousSolution"
			: LsStartupEmpty!.IsChecked == true ? "EmptyEnvironment" : "ShowStartWindow";
		SettingsStore.SetString ("MonoDevelop.Ide.StartupBehaviour", startup);
	}

	// ---------- Build ----------

	void LoadBuildPanel ()
	{
		var before = SettingsStore.GetString ("MonoDevelop.Ide.BeforeCompileAction") ?? "SaveAllFiles";
		BldBeforeCompileCombo!.SelectedIndex = before switch {
			"Nothing" => 0,
			"PromptForSave" => 2,
			_ => 1,
		};
		BldRunWithWarnings!.IsChecked = SettingsStore.GetBool ("MonoDevelop.Ide.RunWithWarnings", true);
		BldBuildBeforeExec!.IsChecked = SettingsStore.GetBool ("MonoDevelop.Ide.BuildBeforeExecuting", true);
		BldBuildBeforeTests!.IsChecked = SettingsStore.GetBool ("BuildBeforeRunningTests", true);
		BldSkipUnmodified!.IsChecked = SettingsStore.GetBool ("MonoDevelop.Ide.SkipBuildingUnmodifiedProjects", true);
		BldParallel!.IsChecked = SettingsStore.GetBool ("MonoDevelop.ParallelBuild", true);
		var verbosity = SettingsStore.GetString ("MonoDevelop.Ide.MSBuildVerbosity") ?? "Normal";
		BldVerbosityCombo!.SelectedIndex = verbosity switch {
			"Quiet" => 0,
			"Minimal" => 1,
			"Detailed" => 3,
			"Diagnostic" => 4,
			_ => 2,
		};
	}

	void StoreBuildPanel ()
	{
		SettingsStore.SetString ("MonoDevelop.Ide.BeforeCompileAction",
			BldBeforeCompileCombo!.SelectedIndex switch {
				0 => "Nothing",
				2 => "PromptForSave",
				_ => "SaveAllFiles",
			});
		SettingsStore.SetBool ("MonoDevelop.Ide.RunWithWarnings", BldRunWithWarnings!.IsChecked == true);
		SettingsStore.SetBool ("MonoDevelop.Ide.BuildBeforeExecuting", BldBuildBeforeExec!.IsChecked == true);
		SettingsStore.SetBool ("BuildBeforeRunningTests", BldBuildBeforeTests!.IsChecked == true);
		SettingsStore.SetBool ("MonoDevelop.Ide.SkipBuildingUnmodifiedProjects", BldSkipUnmodified!.IsChecked == true);
		SettingsStore.SetBool ("MonoDevelop.ParallelBuild", BldParallel!.IsChecked == true);
		SettingsStore.SetString ("MonoDevelop.Ide.MSBuildVerbosity",
			BldVerbosityCombo!.SelectedIndex switch {
				0 => "Quiet",
				1 => "Minimal",
				3 => "Detailed",
				4 => "Diagnostic",
				_ => "Normal",
			});
	}

	// ---------- Errors and Warnings (legacy BuildMessagePanel) ----------

	void LoadBuildMessagesPanel ()
	{
		// JumpToFirst { Never, Error, ErrorOrWarning }: the legacy list shows Error /
		// Error or Warning (index maps to Error / ErrorOrWarning).
		var jump = SettingsStore.GetString ("MonoDevelop.Ide.NewJumpToFirstErrorOrWarning");
		BmJumpCombo!.SelectedIndex = jump == "ErrorOrWarning" ? 1 : 0;
		// BuildResultStates { Never, Always, OnErrors, OnErrorsOrWarnings }.
		var pad = SettingsStore.GetString ("MonoDevelop.Ide.NewShowErrorPadAfterBuild");
		BmErrorPadCombo!.SelectedIndex = pad switch {
			"OnErrors" => 1,
			"OnErrorsOrWarnings" => 2,
			_ => 0,
		};
		// ShowMessageBubbles { Never, ForErrors, ForErrorsAndWarnings }.
		var bubbles = SettingsStore.GetString ("MonoDevelop.Ide.NewShowMessageBubbles");
		BmBubblesCombo!.SelectedIndex = bubbles == "ForErrors" ? 0 : 1;
	}

	void StoreBuildMessagesPanel ()
	{
		SettingsStore.SetString ("MonoDevelop.Ide.NewJumpToFirstErrorOrWarning",
			BmJumpCombo!.SelectedIndex == 1 ? "ErrorOrWarning" : "Error");
		SettingsStore.SetString ("MonoDevelop.Ide.NewShowErrorPadAfterBuild",
			BmErrorPadCombo!.SelectedIndex switch {
				1 => "OnErrors",
				2 => "OnErrorsOrWarnings",
				_ => "Always",
			});
		SettingsStore.SetString ("MonoDevelop.Ide.NewShowMessageBubbles",
			BmBubblesCombo!.SelectedIndex == 0 ? "ForErrors" : "ForErrorsAndWarnings");
	}

	// ---------- Maintenance ----------

	void LoadMaintenancePanel ()
	{
		MtInstrumentation!.IsChecked = SettingsStore.GetBool ("MonoDevelop.EnableInstrumentation", false);
		MtAutomatedTesting!.IsChecked = SettingsStore.GetBool ("MonoDevelop.EnableAutomatedTesting", false);
	}

	void StoreMaintenancePanel ()
	{
		SettingsStore.SetBool ("MonoDevelop.EnableInstrumentation", MtInstrumentation!.IsChecked == true);
		SettingsStore.SetBool ("MonoDevelop.EnableAutomatedTesting", MtAutomatedTesting!.IsChecked == true);
	}

	// ---------- Text editor panels (ported from the SourceEditor2 add-in) ----------
	// The GTK option panels store through DefaultSourceEditorOptions / EditorPreferences,
	// which are ConfigurationProperty backed and end up in MonoDevelopProperties.xml under
	// these keys. Reading/writing the raw keys keeps both UIs sharing the settings.

	// The legacy EnumConverter writes the composite name when all members of a [Flags]
	// enum are set: IncludeWhitespaces.All == Space|Tab|LineEndings serializes as
	// "All" (verified with .NET 10 EnumConverter). Handle it on read and write.
	static readonly string[] IncludeWhitespaceMembers = { "Space", "Tab", "LineEndings" };
	static readonly string[] WordWrapMembers = { "WordWrap", "VisibleGlyphs" };

	static bool Flag (string key, string flag)
	{
		foreach (var f in (SettingsStore.GetString (key) ?? "").Split (',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			if (f.Equals (flag, StringComparison.OrdinalIgnoreCase))
				return true;
			// "All" implies every member of the flags enum.
			if (f.Equals ("All", StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	// Reads/writes a [Flags] enum the way the legacy EnumConverter does: expand a
	// composite "All" into its members so a single flag can be toggled, preserve any
	// valid member that is not in `order` (e.g. WordWrapStyles.AutoIndent), and
	// re-emit the composite name when every known member is back on.
	static void SetFlag (string key, string flag, bool on, string[] order, bool hasAll)
	{
		var members = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		var extras = new List<string> ();
		foreach (var f in (SettingsStore.GetString (key) ?? "").Split (',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			if (hasAll && f.Equals ("All", StringComparison.OrdinalIgnoreCase)) {
				foreach (var m in order)
					members.Add (m);
			} else if (!f.Equals ("None", StringComparison.OrdinalIgnoreCase)) {
				members.Add (f);
				if (!order.Any (o => o.Equals (f, StringComparison.OrdinalIgnoreCase)))
					extras.Add (f);
			}
		}
		if (on) members.Add (flag); else members.Remove (flag);

		var parts = order.Where (members.Contains).ToList ();
		foreach (var e in extras)
			if (!e.Equals (flag, StringComparison.OrdinalIgnoreCase) && !parts.Any (p => p.Equals (e, StringComparison.OrdinalIgnoreCase)))
				parts.Add (e);

		if (parts.Count == 0)
			SettingsStore.SetString (key, "None");
		else if (hasAll && extras.Count == 0 && order.All (members.Contains))
			SettingsStore.SetString (key, "All");
		else
			SettingsStore.SetString (key, string.Join (", ", parts));
	}

	// General: legacy GeneralOptionsPanel.
	void LoadGeneralPanel ()
	{
		var conv = SettingsStore.GetString ("LineEndingConversion") ?? "LeaveAsIs";
		GenLineEndingsCombo!.SelectedIndex = conv switch { "Ask" => 0, "ConvertAlways" => 2, _ => 1 };
		GenShowFoldMargin!.IsChecked = SettingsStore.GetBool ("ShowFoldMargin", true);
		GenDefaultRegionsFolding!.IsChecked = SettingsStore.GetBool ("DefaultRegionsFolding", false);
		GenDefaultCommentFolding!.IsChecked = SettingsStore.GetBool ("DefaultCommentFolding", true);
		GenWordWrap!.IsChecked = Flag ("WordWrapStyle", "WordWrap");
		GenWordWrapGlyphs!.IsChecked = Flag ("WordWrapStyle", "VisibleGlyphs");
		GenWordWrapGlyphs.IsEnabled = GenWordWrap.IsChecked == true;
	}

	void StoreGeneralPanel ()
	{
		SettingsStore.SetString ("LineEndingConversion", GenLineEndingsCombo!.SelectedIndex switch {
			0 => "Ask",
			2 => "ConvertAlways",
			_ => "LeaveAsIs",
		});
		SettingsStore.SetBool ("ShowFoldMargin", GenShowFoldMargin!.IsChecked == true);
		SettingsStore.SetBool ("DefaultRegionsFolding", GenDefaultRegionsFolding!.IsChecked == true);
		SettingsStore.SetBool ("DefaultCommentFolding", GenDefaultCommentFolding!.IsChecked == true);
		SetFlag ("WordWrapStyle", "WordWrap", GenWordWrap!.IsChecked == true, WordWrapMembers, false);
		SetFlag ("WordWrapStyle", "VisibleGlyphs", GenWordWrapGlyphs!.IsChecked == true, WordWrapMembers, false);
	}

	// Markers and rulers: legacy MarkerPanel.
	void LoadMarkersPanel ()
	{
		MkShowLineNumbers!.IsChecked = SettingsStore.GetBool ("ShowLineNumberMargin", true);
		MkShowRuler!.IsChecked = SettingsStore.GetBool ("ShowRuler", true);
		MkHighlightCurrentLine!.IsChecked = SettingsStore.GetBool ("HighlightCaretLine", false);
		MkHighlightMatchingBracket!.IsChecked = SettingsStore.GetBool ("HighlightMatchingBracket", true);
		MkHighlightUsages!.IsChecked = SettingsStore.GetBool ("EnableHighlightUsages", true);
		MkDrawIndentMarkers!.IsChecked = SettingsStore.GetBool ("ShowBlockStructure", true);
		MkEnableQuickDiff!.IsChecked = SettingsStore.GetBool ("EnableQuickDiff", false);
		MkProcedureSeparators!.IsChecked = SettingsStore.GetBool ("ShowProcedureLineSeparators", false);
		MkEnableAnimations!.IsChecked = SettingsStore.GetBool ("EnableAnimations", true);
		var ws = SettingsStore.GetString ("ShowWhitespaces") ?? "Never";
		MkShowWhitespacesCombo!.SelectedIndex = ws switch { "Selection" => 1, "Always" => 2, _ => 0 };
		// Legacy default is IncludeWhitespaces.All (every member on).
		var includeRaw = SettingsStore.GetString ("IncludeWhitespaces");
		MkIncludeSpaces!.IsChecked = includeRaw is null || Flag ("IncludeWhitespaces", "Space");
		MkIncludeTabs!.IsChecked = includeRaw is null || Flag ("IncludeWhitespaces", "Tab");
		MkIncludeLineEndings!.IsChecked = includeRaw is null || Flag ("IncludeWhitespaces", "LineEndings");
	}

	void StoreMarkersPanel ()
	{
		SettingsStore.SetBool ("ShowLineNumberMargin", MkShowLineNumbers!.IsChecked == true);
		SettingsStore.SetBool ("ShowRuler", MkShowRuler!.IsChecked == true);
		SettingsStore.SetBool ("HighlightCaretLine", MkHighlightCurrentLine!.IsChecked == true);
		SettingsStore.SetBool ("HighlightMatchingBracket", MkHighlightMatchingBracket!.IsChecked == true);
		SettingsStore.SetBool ("EnableHighlightUsages", MkHighlightUsages!.IsChecked == true);
		SettingsStore.SetBool ("ShowBlockStructure", MkDrawIndentMarkers!.IsChecked == true);
		SettingsStore.SetBool ("EnableQuickDiff", MkEnableQuickDiff!.IsChecked == true);
		SettingsStore.SetBool ("ShowProcedureLineSeparators", MkProcedureSeparators!.IsChecked == true);
		SettingsStore.SetBool ("EnableAnimations", MkEnableAnimations!.IsChecked == true);
		SettingsStore.SetString ("ShowWhitespaces", MkShowWhitespacesCombo!.SelectedIndex switch {
			1 => "Selection",
			2 => "Always",
			_ => "Never",
		});
		SetFlag ("IncludeWhitespaces", "Space", MkIncludeSpaces!.IsChecked == true, IncludeWhitespaceMembers, true);
		SetFlag ("IncludeWhitespaces", "Tab", MkIncludeTabs!.IsChecked == true, IncludeWhitespaceMembers, true);
		SetFlag ("IncludeWhitespaces", "LineEndings", MkIncludeLineEndings!.IsChecked == true, IncludeWhitespaceMembers, true);
	}

	// Behavior: legacy BehaviorPanel.
	void LoadBehaviorPanel ()
	{
		var indent = SettingsStore.GetString ("IndentStyle") ?? "Smart";
		BhIndentCombo!.SelectedIndex = indent switch { "None" => 0, "Auto" => 1, _ => 2 };
		var wordNav = SettingsStore.GetString ("WordNavigationStyle") ?? "Windows";
		BhWordNavigationCombo!.SelectedIndex = wordNav == "Unix" ? 0 : 1;
		BhAutoInsertBrace!.IsChecked = SettingsStore.GetBool ("AutoInsertMatchingBracket", true);
		BhSmartSemicolon!.IsChecked = SettingsStore.GetBool ("SmartSemicolonPlacement", false);
		BhSmartSemicolon.IsEnabled = BhAutoInsertBrace.IsChecked == true;
		BhTabAsReindent!.IsChecked = SettingsStore.GetBool ("TabIsReindent", false);
		BhSmartBackspace!.IsChecked = SettingsStore.GetBool ("SmartBackspace", true);
		BhFormatOnSave!.IsChecked = SettingsStore.GetBool ("AutoFormatDocumentOnSave", false);
		BhAutoPatternCasing!.IsChecked = SettingsStore.GetBool ("AutoSetPatternCasing", false);
		BhSelectionSurrounding!.IsChecked = SettingsStore.GetBool ("EnableSelectionWrappingKeys", false);
		BhFormattingUndo!.IsChecked = SettingsStore.GetBool ("GenerateFormattingUndoStep", true);
	}

	void StoreBehaviorPanel ()
	{
		SettingsStore.SetString ("IndentStyle", BhIndentCombo!.SelectedIndex switch {
			0 => "None",
			1 => "Auto",
			_ => "Smart",
		});
		SettingsStore.SetString ("WordNavigationStyle", BhWordNavigationCombo!.SelectedIndex == 0 ? "Unix" : "Windows");
		SettingsStore.SetBool ("AutoInsertMatchingBracket", BhAutoInsertBrace!.IsChecked == true);
		SettingsStore.SetBool ("SmartSemicolonPlacement", BhSmartSemicolon!.IsChecked == true);
		SettingsStore.SetBool ("TabIsReindent", BhTabAsReindent!.IsChecked == true);
		SettingsStore.SetBool ("SmartBackspace", BhSmartBackspace!.IsChecked == true);
		SettingsStore.SetBool ("AutoFormatDocumentOnSave", BhFormatOnSave!.IsChecked == true);
		SettingsStore.SetBool ("AutoSetPatternCasing", BhAutoPatternCasing!.IsChecked == true);
		SettingsStore.SetBool ("EnableSelectionWrappingKeys", BhSelectionSurrounding!.IsChecked == true);
		SettingsStore.SetBool ("GenerateFormattingUndoStep", BhFormattingUndo!.IsChecked == true);
	}

	// IntelliSense: legacy CompletionOptionsPanel.
	void LoadIntelliSensePanel ()
	{
		IsAutoCodeCompletion!.IsChecked = SettingsStore.GetBool ("EnableAutoCodeCompletion", true);
		IsShowImports!.IsChecked = SettingsStore.GetBool ("AddImportedItemsToCompletionList", false);
		IsIncludeKeywords!.IsChecked = SettingsStore.GetBool ("IncludeKeywordsInCompletionList", true);
		IsIncludeSnippets!.IsChecked = SettingsStore.GetBool ("IncludeCodeSnippetsInCompletionList", true);
		IsSuggestionMode!.IsChecked = SettingsStore.GetBool ("ForceCompletionSuggestionMode", false);
	}

	void StoreIntelliSensePanel ()
	{
		SettingsStore.SetBool ("EnableAutoCodeCompletion", IsAutoCodeCompletion!.IsChecked == true);
		SettingsStore.SetBool ("AddImportedItemsToCompletionList", IsShowImports!.IsChecked == true);
		SettingsStore.SetBool ("IncludeKeywordsInCompletionList", IsIncludeKeywords!.IsChecked == true);
		SettingsStore.SetBool ("IncludeCodeSnippetsInCompletionList", IsIncludeSnippets!.IsChecked == true);
		SettingsStore.SetBool ("ForceCompletionSuggestionMode", IsSuggestionMode!.IsChecked == true);
	}

	// ---------- Color Theme / Code Snippets / Language Bundles ----------
	// The GTK panels read these from SyntaxHighlightingService / CodeTemplateService,
	// which are heavy (they parse .tmTheme/.sublime-syntax with NRefactory deps). The
	// shell reconciles the SAME user directories and settings keys instead.

	static string UserDataRoot {
		get {
			var xdg = Environment.GetEnvironmentVariable ("XDG_DATA_HOME");
			var baseDir = string.IsNullOrEmpty (xdg)
				? Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".local", "share")
				: xdg;
			return Path.Combine (baseDir, "MonoDevelop", "9.0");
		}
	}
	static string ColorThemesDir => Path.Combine (UserDataRoot, "ColorThemes");
	static string LanguageBundlesDir => Path.Combine (UserDataRoot, "LanguageBundles");
	static string SnippetsDir => Path.Combine (UserDataRoot, "Snippets");

	static readonly string[] BuiltInThemes = { "Light", "Dark", "High Contrast Dark", "High Contrast Light" };

	static bool IsDarkUi => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
	// ThemeConfigurationProperty stores the light scheme under "ColorScheme" and the
	// dark one under "ColorScheme-Dark" (IdePreferences.ThemeConfigurationProperty).
	static string ColorSchemeKey => IsDarkUi ? "ColorScheme-Dark" : "ColorScheme";

	static readonly HashSet<string> themeExtensions = new (StringComparer.OrdinalIgnoreCase) { ".json", ".vssettings", ".tmtheme" };
	static string ThemeFileToName (string file)
	{
		var n = Path.GetFileName (file);
		foreach (var ext in new[] { ".tmTheme", ".vssettings", ".json" })
			if (n.EndsWith (ext, StringComparison.OrdinalIgnoreCase))
				return n[..^ext.Length];
		return Path.GetFileNameWithoutExtension (n);
	}

	void LoadColorThemePanel ()
	{
		ThemeList!.Items.Clear ();
		var names = new List<string> (BuiltInThemes);
		try {
			if (Directory.Exists (ColorThemesDir))
				foreach (var f in Directory.EnumerateFiles (ColorThemesDir).Where (f => themeExtensions.Contains (Path.GetExtension (f))))
					if (!names.Contains (ThemeFileToName (f)))
						names.Add (ThemeFileToName (f));
		} catch { /* unreadable dir */ }
		foreach (var n in names)
			ThemeList.Items.Add (n);
		var current = SettingsStore.GetString (ColorSchemeKey) ?? (IsDarkUi ? "Dark" : "Light");
		ThemeList.SelectedItem = names.FirstOrDefault (n => n.Equals (current, StringComparison.OrdinalIgnoreCase));
		MainWindow.Instance?.Output ($"[prefs-colortheme] key={ColorSchemeKey} themes={names.Count} selected={current}");
	}

	void StoreColorThemePanel ()
	{
		if (ThemeList!.SelectedItem is string theme)
			SettingsStore.SetString (ColorSchemeKey, theme);
	}

	async void OnThemeAdd (object? sender, RoutedEventArgs e)
	{
		var top = TopLevel.GetTopLevel (this);
		if (top?.StorageProvider is not { } sp)
			return;
		var files = await sp.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
			AllowMultiple = false,
			FileTypeFilter = new [] { new Avalonia.Platform.Storage.FilePickerFileType ("Color themes") { Patterns = new [] { "*.json", "*.vssettings", "*.tmTheme", "*.tmtheme" } } },
		});
		if (files.Count == 0)
			return;
		if (files [0].TryGetLocalPath () is not { } src)
			return;
		Directory.CreateDirectory (ColorThemesDir);
		File.Copy (src, Path.Combine (ColorThemesDir, Path.GetFileName (src)));
		LoadColorThemePanel ();
	}

	void OnThemeRemove (object? sender, RoutedEventArgs e)
	{
		if (ThemeList!.SelectedItem is not string name || BuiltInThemes.Contains (name))
			return;
		try {
			var file = Directory.EnumerateFiles (ColorThemesDir).FirstOrDefault (f => ThemeFileToName (f).Equals (name, StringComparison.OrdinalIgnoreCase));
			if (file is not null)
				File.Delete (file);
		} catch { /* dir gone / unreadable */ }
		LoadColorThemePanel ();
	}

	void OnThemeOpenFolder (object? sender, RoutedEventArgs e)
	{
		try {
			Directory.CreateDirectory (ColorThemesDir);
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo ("xdg-open", ColorThemesDir) { UseShellExecute = true });
		} catch (Exception ex) { MainWindow.Instance?.Output ("[prefs] open folder failed: " + ex.Message); }
	}

	// Code Snippets: the legacy stores one <Shortcut>.template.xml per user template in
	// the Snippets folder (CodeTemplateService.TemplatePath).
	readonly Dictionary<string, (string Shortcut, string Group, string Description, string Code)> snippets = new ();

	void LoadSnippetsPanel ()
	{
		snippets.Clear ();
		try {
			if (Directory.Exists (SnippetsDir)) {
				foreach (var f in Directory.EnumerateFiles (SnippetsDir, "*.xml")) {
					try {
						var root = XDocument.Load (f).Root;
						if (root is null)
							continue;
						var shortcut = (string?)root.Element ("Shortcut") ?? Path.GetFileNameWithoutExtension (f);
						snippets [f] = (shortcut,
							(string?)root.Element ("Group") ?? "",
							(string?)root.Element ("Description") ?? "",
							(string?)root.Element ("Code") ?? "");
					} catch { /* malformed template */ }
				}
			}
		} catch { /* unreadable dir */ }
		SnippetList!.Items.Clear ();
		foreach (var kv in snippets.OrderBy (s => s.Value.Group, StringComparer.OrdinalIgnoreCase).ThenBy (s => s.Value.Shortcut, StringComparer.OrdinalIgnoreCase))
			SnippetList.Items.Add (new ListBoxItem { Tag = kv.Key, Content = $"[{kv.Value.Group}] {kv.Value.Shortcut}" });
		MainWindow.Instance?.Output ($"[prefs-snippets] count={snippets.Count}");
	}

	void OnSnippetSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (SnippetList?.SelectedItem is ListBoxItem { Tag: string file } && snippets.TryGetValue (file, out var t))
			SnippetPreview!.Text = t.Code;
		else if (SnippetPreview is not null)
			SnippetPreview.Text = "";
	}

	void OnSnippetRemove (object? sender, RoutedEventArgs e)
	{
		if (SnippetList?.SelectedItem is not ListBoxItem { Tag: string file })
			return;
		try { File.Delete (file); } catch { }
		LoadSnippetsPanel ();
	}

	// Language Bundles: user bundles under the LanguageBundles folder; built-in bundles
	// ship with the IDE (the shell lists the user ones it can add/remove).
	void LoadLanguageBundlesPanel ()
	{
		BundleList!.Items.Clear ();
		try {
			if (Directory.Exists (LanguageBundlesDir))
				foreach (var f in Directory.EnumerateFileSystemEntries (LanguageBundlesDir).OrderBy (f => f, StringComparer.OrdinalIgnoreCase))
					BundleList.Items.Add (Path.GetFileName (f));
		} catch { /* unreadable dir */ }
		MainWindow.Instance?.Output ($"[prefs-bundles] count={BundleList.Items.Count}");
	}

	async void OnBundleAdd (object? sender, RoutedEventArgs e)
	{
		var top = TopLevel.GetTopLevel (this);
		if (top?.StorageProvider is not { } sp)
			return;
		var files = await sp.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
			AllowMultiple = false,
			FileTypeFilter = new [] { new Avalonia.Platform.Storage.FilePickerFileType ("Bundles") { Patterns = new [] { "*.tmBundle", "*.sublime-package", "*.tmbundle", "*.zip" } } },
		});
		if (files.Count == 0)
			return;
		if (files [0].TryGetLocalPath () is not { } src)
			return;
		Directory.CreateDirectory (LanguageBundlesDir);
		File.Copy (src, Path.Combine (LanguageBundlesDir, Path.GetFileName (src)));
		LoadLanguageBundlesPanel ();
	}

	void OnBundleRemove (object? sender, RoutedEventArgs e)
	{
		if (BundleList?.SelectedItem is not string name)
			return;
		try {
			var path = Path.Combine (LanguageBundlesDir, name);
			if (Directory.Exists (path)) Directory.Delete (path, true);
			else if (File.Exists (path)) File.Delete (path);
		} catch { /* dir gone / unreadable */ }
		LoadLanguageBundlesPanel ();
	}

	// ---------- Panel switching (OptionsDialog.SelectPanel) ----------
	void OnSectionSelected (object? sender, SelectionChangedEventArgs e)
	{
		var node = SectionTree?.SelectedItem switch {
			TreeViewItem { Tag: PrefsNode n } => n,
			PrefsNode n => n,
			_ => null,
		};
		if (node is null || node.IsCategory)
			return;
		ShowPanel (node);
	}

	void ShowPanel (PrefsNode node)
	{
		var id = node.Id;
		updatingDetails = true;

		PanelStyle!.IsVisible = id == "style";
		PanelAuthor!.IsVisible = id == "author";
		PanelKeyBindings!.IsVisible = id == "keybindings";
		PanelFonts!.IsVisible = id == "fonts";
		PanelUpdates!.IsVisible = id == "updates";
		PanelTasks!.IsVisible = id == "tasks";
		PanelExternalTools!.IsVisible = id == "externaltools";
		PanelFeedback!.IsVisible = id == "feedback";
		PanelLoadSave!.IsVisible = id == "loadsave";
		PanelBuild!.IsVisible = id == "build";
		PanelBuildMessages!.IsVisible = id == "buildmessages";
		PanelMaintenance!.IsVisible = id == "maintenance";
		PanelGeneral!.IsVisible = id == "general";
		PanelMarkers!.IsVisible = id == "markers";
		PanelBehavior!.IsVisible = id == "behavior";
		PanelIntelliSense!.IsVisible = id == "intellisense";
		PanelColorTheme!.IsVisible = id == "colortheme";
		PanelCodeSnippets!.IsVisible = id == "codesnippets";
		PanelLanguageBundles!.IsVisible = id == "languagebundles";
		PanelPlaceholder!.IsVisible = !functionalPanels.Contains (id);

		HeaderTitle!.Text = node.Label;
		SetIcon (HeaderIcon!, node.Icon);
		MainWindow.Instance?.Output ($"[prefs-panel] {id} | {node.Label} | icon={node.Icon} | placeholder={!functionalPanels.Contains (id)}");

		if (id == "style") {
			var dark = Application.Current?.ActualThemeVariant != ThemeVariant.Light;
			ThemeDarkRadio!.IsChecked = dark;
			ThemeLightRadio!.IsChecked = !dark;
		}

		updatingDetails = false;
	}

	void OnThemeRadioChecked (object? sender, RoutedEventArgs e)
	{
		if (updatingDetails || sender is not RadioButton rb || rb.IsChecked != true)
			return;
		var light = ReferenceEquals (sender, ThemeLightRadio);
		if (Application.Current is not null)
			Application.Current.RequestedThemeVariant = light ? ThemeVariant.Light : ThemeVariant.Dark;
	}

	// ---------- Apply / OK ----------

	void OnOk (object? sender, RoutedEventArgs e)
	{
		// OptionsDialog.ApplyChanges: every visible panel stores its settings.
		if (!string.Equals (pendingLanguage, storedLanguage, StringComparison.Ordinal))
			SettingsStore.SetString (LanguageKey, pendingLanguage);
		StoreAuthorPanel ();
		StoreUpdatesPanel ();
		StoreTasksPanel ();
		StoreToolFields ();
		StoreFeedbackPanel ();
		StoreLoadSavePanel ();
		StoreBuildPanel ();
		StoreBuildMessagesPanel ();
		StoreMaintenancePanel ();
		StoreGeneralPanel ();
		StoreMarkersPanel ();
		StoreBehaviorPanel ();
		StoreIntelliSensePanel ();
		StoreColorThemePanel ();
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e) => Close ();
}
