using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Controls;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Add Reference dialog rebuilt after the legacy SelectReferenceDialog
/// (MonoDevelop.Ide.Projects): a notebook with All / Packages / Projects /
/// .Net Assembly tabs, a filter entry above the tab content, the references
/// already in the project listed at the bottom with a Remove button, and the
/// OK / Cancel pair — inside the same Win11 chrome as the main window.
/// OK rewrites the .csproj ItemGroups (PackageReference / ProjectReference /
/// Reference) like the legacy ReferenceInformations → project save.
/// </summary>
public class AddReferenceDialog : DialogWindow
{
	class RefRow
	{
		public string Name = "";
		public string Secondary = "";
		public string Type = "Package";			public string Kind = "PackageReference"; // PackageReference | ProjectReference | Reference
			public string Raw = ""; // original Include / full path
	}

	static readonly string [] KnownPackages = {
		"Newtonsoft.Json", "System.Json", "System.Numerics.Vectors", "System.Runtime.Serialization",
		"System.Transactions", "System.Web", "System.Windows", "System.Xml.Linq",
		"System.Drawing.Common", "Microsoft.CSharp", "Microsoft.Extensions.Logging",
		"MongoDB.Bson", "MongoDB.Driver", "MySql.Data", "Npgsql", "RestSharp",
	};

	readonly string projectPath;
	readonly string solutionPath;
	readonly ListBox candidates = new ();
	readonly TextBox filter = new ();
	readonly ListBox refsList = new ();
	readonly Button removeButton;
	readonly List<RefRow> refs = new ();
	readonly List<string> candidateNames = new ();
	readonly Dictionary<string, string> candidateKind = new ();
	int selectedTab; // 0 All, 1 Packages, 2 Projects, 3 Assembly

	public string? AddedReference { get; private set; }

	public AddReferenceDialog (string projectPath) : this (projectPath, null) { }

	public AddReferenceDialog (string projectPath, string? solutionPath)
	{
		this.projectPath = projectPath;
		this.solutionPath = solutionPath ?? "";

		Title = "Add Reference";
		Width = 620;
		Height = 560;
		ShowTitleRow ();

		// ----- Filter row (legacy CombinedBox: filter entry above the tabs) -----
		filter.Watermark = "Search";
		filter.Margin = new Thickness (12, 8, 12, 4);
		filter.TextChanged += (_, _) => RefreshCandidates ();

		// ----- Tab strip (legacy notebook: All / Packages / Projects / .Net Assembly) -----
		var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness (12, 0, 12, 0) };
		foreach (var (name, i) in new [] { ("All", 0), ("Packages", 1), ("Projects", 2), (".Net Assembly", 3) }) {
			var tabName = name;
			int idx = i;
			var btn = new ToggleButton { Content = tabName, FontSize = 12, Padding = new Thickness (10, 4), Tag = idx, IsChecked = idx == 0 };
			btn.Click += (_, _) => {
				selectedTab = idx;
				foreach (ToggleButton t in tabs.Children.OfType<ToggleButton> ())
					t.IsChecked = t.Tag as int? == idx;
				filter.IsVisible = idx != 3; // legacy: filterEntry.Sensitive = PageNum != 3
				RefreshCandidates ();
			};
			tabs.Children.Add (btn);
		}

		// ----- Candidate list (tab content) -----
		candidates.Height = 230;
		candidates.DoubleTapped += (_, _) => AddSelected ();
		var candidateBorder = new Border {
			Child = candidates,
			Margin = new Thickness (12, 4, 12, 0),
			BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (1),
			CornerRadius = new CornerRadius (3),
		};

		// ----- References list (legacy boxRefs) + Remove -----
		refsList.Height = 120;
		var refsBorder = new Border {
			Child = refsList,
			Margin = new Thickness (12, 8, 12, 0),
			BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (1),
			CornerRadius = new CornerRadius (3),
		};
		removeButton = new Button { Content = "Remove", Width = 80, IsEnabled = false };
		removeButton.Click += (_, _) => RemoveSelected ();
		var refsHeader = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto"), Margin = new Thickness (12, 10, 12, 0) };
		var refsLabel = new TextBlock { Text = "References", FontWeight = FontWeight.SemiBold, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		refsLabel.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		refsHeader.Children.Add (refsLabel);
		Grid.SetColumn (removeButton, 1);
		refsHeader.Children.Add (removeButton);
		refsList.SelectionChanged += (_, _) => removeButton.IsEnabled = refsList.SelectedIndex >= 0;

		// ----- Bottom buttons (legacy OK / Cancel) -----
		var ok = new Button { Content = "OK", Width = 80, Classes = { "chromebtn" } };
		ok.Click += (_, _) => Apply ();
		var cancel = new Button { Content = "Cancel", Width = 80, Classes = { "chromebtn" } };
		cancel.Click += (_, _) => Close ();
		removeButton.Classes.Add ("chromebtn");
		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8,
			Margin = new Thickness (12, 12, 12, 12),
		};
		buttons.Children.Add (ok);
		buttons.Children.Add (cancel);

		var root = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,Auto,Auto,Auto,Auto,*") };
		Grid.SetRow (filter, 0);
		root.Children.Add (filter);
		Grid.SetRow (tabs, 1);
		root.Children.Add (tabs);
		Grid.SetRow (candidateBorder, 2);
		root.Children.Add (candidateBorder);
		Grid.SetRow (refsHeader, 3);
		root.Children.Add (refsHeader);
		Grid.SetRow (refsBorder, 4);
		root.Children.Add (refsBorder);
		var bottom = new Panel { Children = { buttons } };
		Grid.SetRow (bottom, 5);
		root.Children.Add (bottom);

		Content = root;

		LoadProject ();
		RefreshCandidates ();
	}

	// ---------- data ----------

	void LoadProject ()
	{
		refs.Clear ();
		try {
			var doc = XDocument.Load (projectPath);
			var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
			foreach (var pr in doc.Descendants (ns + "PackageReference")) {
				var inc = pr.Attribute ("Include")?.Value ?? pr.Attribute ("Update")?.Value ?? "";
				if (inc.Length > 0)
					refs.Add (new RefRow { Name = inc.Split (',') [0], Secondary = pr.Attribute ("Version")?.Value ?? "", Type = "Package", Kind = "PackageReference", Raw = inc });
			}
			foreach (var r in doc.Descendants (ns + "Reference")) {
				var inc = r.Attribute ("Include")?.Value ?? "";
				if (inc.Length > 0)
					refs.Add (new RefRow { Name = inc.Split (',') [0], Secondary = r.Element (ns + "HintPath")?.Value ?? "", Type = "Assembly", Kind = "Reference", Raw = inc });
			}
			foreach (var pr in doc.Descendants (ns + "ProjectReference")) {
				var inc = pr.Attribute ("Include")?.Value ?? "";
				if (inc.Length > 0)
					refs.Add (new RefRow { Name = Path.GetFileNameWithoutExtension (inc), Secondary = inc, Type = "Project", Kind = "ProjectReference", Raw = inc });
			}
		} catch (Exception ex) {
			Console.WriteLine ("[addref] load error: " + ex.Message);
		}
		RefreshRefs ();
		DiscoverCandidates ();
	}

	/// <summary>Candidate sources per tab: packages+assemblies in All/Packages, sibling projects in Projects, GAC-ish assemblies in .Net Assembly.</summary>
	void DiscoverCandidates ()
	{
		candidateNames.Clear ();
		candidateKind.Clear ();

		// Packages from existing PackageReference items anywhere in the csproj family,
		// plus the known-popular package list (legacy NuGet repository).
		foreach (var p in KnownPackages) {
			candidateNames.Add (p);
			candidateKind [p] = "PackageReference";
		}

		// Projects: siblings under the solution directory (legacy ProjectReferencePanel).
		var dir = Path.GetDirectoryName (projectPath) ?? "";
		var slnDir = solutionPath.Length > 0 ? Path.GetDirectoryName (solutionPath) ?? dir : dir;
		try {
			foreach (var csproj in Directory.EnumerateFiles (slnDir, "*.csproj", SearchOption.AllDirectories)) {
				if (!string.Equals (csproj, projectPath, StringComparison.OrdinalIgnoreCase)) {
					var name = Path.GetFileNameWithoutExtension (csproj);
					if (!candidateNames.Contains (name)) {
						candidateNames.Add (name);
						candidateKind [name] = "ProjectReference:" + csproj;
					}
				}
			}
		} catch { /* unreadable dirs */ }

		// Assemblies: framework assemblies of the project's target framework.
		try {
			var doc = XDocument.Load (projectPath);
			var tfm = doc.Descendants ().FirstOrDefault (e => e.Name.LocalName == "TargetFramework")?.Value;
			if (!string.IsNullOrEmpty (tfm)) {
				var refDir = Path.Combine ("/usr/lib/dotnet/packs", "Microsoft.NETCore.App.Ref", tfm.Split ('.') [0] + ".0.0", "ref", tfm);
				if (!Directory.Exists (refDir)) {
					var packsRoot = "/usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref";
					if (Directory.Exists (packsRoot)) {
						var ver = Directory.EnumerateDirectories (packsRoot).OrderByDescending (d => d).FirstOrDefault ();
						if (ver is not null)
							refDir = Path.Combine (ver, "ref", tfm);
					}
				}
				if (Directory.Exists (refDir)) {
					foreach (var dll in Directory.EnumerateFiles (refDir, "*.dll", SearchOption.AllDirectories)) {
						var name = Path.GetFileNameWithoutExtension (dll);
						if (!candidateNames.Contains (name)) {
							candidateNames.Add (name);
							candidateKind [name] = "Reference";
						}
					}
				}
			}
		} catch { /* pack probing is best-effort */ }

		// Custom typed names are always allowed (AssemblyReferencePanel browse): Enter
		// in the filter adds the typed name; the placeholder row sorts last.
		candidateNames.Add ("\u2026type a custom name in the filter");
		candidateKind ["\u2026type a custom name in the filter"] = "Custom";
		filter.KeyDown += (_, e) => {
			if (e.Key == Key.Return && (filter.Text?.Trim ().Length ?? 0) > 0) {
				AddCustom (filter.Text.Trim ());
				e.Handled = true;
			}
		};
	}

	void RefreshCandidates ()
	{
		var query = filter.Text?.Trim () ?? "";
		candidates.Items.Clear ();
		foreach (var name in candidateNames.OrderBy (n => n == "\u2026type a custom name in the filter" ? "\uFFFF" : n)) {
			bool byTab = selectedTab switch {
				1 => candidateKind.GetValueOrDefault (name) == "PackageReference",
				2 => candidateKind.GetValueOrDefault (name)?.StartsWith ("ProjectReference") == true,
				3 => candidateKind.GetValueOrDefault (name) == "Reference" || candidateKind.GetValueOrDefault (name) == "Custom",
				_ => true,
			};
			if (!byTab || (query.Length > 0 && !name.Contains (query, StringComparison.OrdinalIgnoreCase)))
				continue;
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness (4, 1) };
			var icon = new Avalonia.Controls.Image { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center };
			var kind = candidateKind.GetValueOrDefault (name) ?? "";
			icon.Source = IconService.GetImage (kind.StartsWith ("ProjectReference") ? "md-project" : kind == "Reference" || kind == "Custom" ? "md-empty-file-icon" : "md-package");
			var tb = new TextBlock { Text = name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
			tb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			row.Children.Add (icon);
			row.Children.Add (tb);
			candidates.Items.Add (row);
		}
	}

	void RefreshRefs ()
	{
		refsList.Items.Clear ();
		foreach (var r in refs.OrderBy (r => r.Name, StringComparer.OrdinalIgnoreCase)) {
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness (4, 1) };
			var icon = new Avalonia.Controls.Image {
				Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center,
				Source = IconService.GetImage (r.Kind == "ProjectReference" ? "md-project" : r.Kind == "Reference" ? "md-empty-file-icon" : "md-package"),
			};
			var nameTb = new TextBlock { Text = r.Name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
			nameTb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			row.Children.Add (icon);
			row.Children.Add (nameTb);
			if (r.Secondary.Length > 0 && r.Secondary != r.Name) {
				var secTb = new TextBlock { Text = r.Secondary, FontSize = 11, Opacity = 0.65, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
				secTb.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
				row.Children.Add (secTb);
			}
			refsList.Items.Add (row);
		}
	}

	// ---------- actions ----------

	void AddSelected ()
	{
		int idx = candidates.SelectedIndex;
		if (idx < 0 || idx >= candidates.Items.Count)
			return;
		var text = ExtractRowText (candidates.Items [idx]);
		if (text.Length == 0)
			return;
		var custom = candidateKind.GetValueOrDefault (text) == "Custom" ? (filter.Text?.Trim () ?? "") : text;
		var name = custom.Length > 0 ? custom : text;
		AddCustom (name, candidateKind.GetValueOrDefault (name) ?? "Reference");
	}

	void AddCustom (string name, string kind = "Custom")
	{
		if (name.Length == 0)
			return;
		if (refs.Any (r => r.Name == name)) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ($"[refs] '{name}' is already referenced");
			return;
		}
		if (kind == "Custom")
			kind = "Reference";
		refs.Add (kind.StartsWith ("ProjectReference")
			? new RefRow { Name = name, Secondary = kind.Substring ("ProjectReference:".Length), Type = "Project", Kind = "ProjectReference", Raw = MakeProjectInclude (kind.Substring ("ProjectReference:".Length)) }
			: new RefRow { Name = name, Type = kind == "Reference" ? "Assembly" : "Package", Kind = kind, Raw = name });
		AddedReference = name;
		RefreshRefs ();
	}

	static string ExtractRowText (object? item)
	{
		if (item is StackPanel sp)
			foreach (var child in sp.Children)
				if (child is TextBlock tb)
					return tb.Text ?? "";
		return item?.ToString () ?? "";
	}

	string MakeProjectInclude (string projectFile)
	{
		// Relative path from the project directory, like the legacy ProjectReferencePanel.
		var baseDir = Path.GetDirectoryName (projectPath) ?? "";
		return Path.GetRelativePath (baseDir, projectFile);
	}

	void RemoveSelected ()
	{
		int idx = refsList.SelectedIndex;
		if (idx < 0)
			return;
		// Map visual index back to the ordered ref list.
		var ordered = refs.OrderBy (r => r.Name, StringComparer.OrdinalIgnoreCase).ToList ();
		if (idx < ordered.Count) {
			refs.Remove (ordered [idx]);
			RefreshRefs ();
		}
	}

	/// <summary>Programmatic single-add (QA + menu path): adds the reference and saves.</summary>
	public bool TryAddReference (string name)
	{
		if (refs.Any (r => r.Name == name)) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ($"[refs] '{name}' is already referenced");
			return false;
		}
		refs.Add (new RefRow { Name = name, Type = "Package", Kind = "PackageReference", Raw = name });
		AddedReference = name;
		return Apply ();
	}

	/// <summary>OK: rewrite the csproj reference items (keep everything else intact).</summary>
	bool Apply ()
	{
		try {
			var doc = XDocument.Load (projectPath);
			var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

			// Remove existing reference items (with their now-empty ItemGroups), then
			// append one ItemGroup with the new set (legacy ReferenceInformations is
			// saved wholesale).
			var toRemove = doc.Descendants ()
				.Where (e => e.Name.LocalName is "PackageReference" or "Reference" or "ProjectReference")
				.ToList ();
			foreach (var e in toRemove) {
				var parent = e.Parent;
				e.Remove ();
				if (parent is not null && parent.Name.LocalName == "ItemGroup" && !parent.Nodes ().Any () && !parent.HasAttributes)
					parent.Remove ();
			}

			var itemGroup = new XElement (ns + "ItemGroup");
			bool any = false;
			foreach (var r in refs.OrderBy (r => r.Name, StringComparer.OrdinalIgnoreCase)) {
				switch (r.Kind) {
					case "PackageReference":
						itemGroup.Add (new XElement (ns + "PackageReference",
							new XAttribute ("Include", r.Name),
							r.Secondary.Length > 0 ? new XAttribute ("Version", r.Secondary) : null));
						break;
					case "ProjectReference":
						itemGroup.Add (new XElement (ns + "ProjectReference", new XAttribute ("Include", r.Raw)));
						break;
					default:
						itemGroup.Add (new XElement (ns + "Reference", new XAttribute ("Include", r.Raw)));
						break;
				}
				any = true;
			}
			if (any)
				doc.Root!.Add (itemGroup);
			doc.Save (projectPath);
			Close ();
			return true;
		} catch (Exception ex) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ("[refs] error saving: " + ex.Message);
			return false;
		}
	}
}
