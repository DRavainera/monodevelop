using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using MdHslColor = MonoDevelop.Components.HslColor;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Controls;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Add Reference dialog — 100% faithful port of the legacy
/// SelectReferenceDialog (MonoDevelop.Ide.Projects + .glade): an HPaned
/// (left: the All / Packages / Projects / .Net Assembly notebook with the
/// filter entry to the RIGHT of the tabs, legacy
/// gtk_notebook_set_action_widget(End), 180 px, Ctrl+F focuses it; right:
/// the "Selected references:" header row with a flat remove icon button
/// (gtk-delete → remove-16) over the headerless 2-line list). Candidate
/// panels: checkbox + icon + name (markup, filter-highlighted per character
/// via HslColor.GenerateHighlightColors) + metadata columns (Packages/All:
/// Version + Package; Projects: Directory; .Net Assembly: Version + Path +
/// a Browse... button bottom-right). OK rewrites the .csproj items with
/// legacy ReferenceInformations semantics (SaveProjectReferences).
/// </summary>
public class AddReferenceDialog : DialogWindow
{
	// ---------- reference model (kept from slice F; the e2e/oracle surface) ----------

	class RefRow
	{
		public string Name = "";
		public string Secondary = "";
		public string Type = "Package";
		public string Kind = "PackageReference"; // PackageReference | ProjectReference | Reference
		public string Raw = ""; // Include for PKT/PROJ; short name for assembly
		public string? HintPath = null;
	}

	// The shell's offline package repository (legacy NuGet repository list): the
	// single source Add Reference (Packages tab) and the Manage NuGet Packages
	// dialog consult — internal so both share the same list.
	internal static readonly string [] KnownPackages = {
		"Newtonsoft.Json", "System.Json", "System.Numerics.Vectors", "System.Runtime.Serialization",
		"System.Transactions", "System.Web", "System.Windows", "System.Xml.Linq",
		"System.Drawing.Common", "Microsoft.CSharp", "Microsoft.Extensions.Logging",
		"MongoDB.Bson", "MongoDB.Driver", "MySql.Data", "Npgsql", "RestSharp",
	};

	// ---------- candidate row model (one per panel) ----------

	class CandRow
	{
		public string Name = "";
		public string Version = "";
		public string Meta = "";      // "Package" (Packages tab) | directory (Projects) | full path (Assembly)
		public string Kind = "";      // "PackageReference" | "ProjectReference" | "Reference"
		public string ProjectFile = "";// absolute csproj path (Kind==ProjectReference)
		public bool Cyclic = false;
		public bool TfmIncompatible = false;
		public int Rank = -1;         // -1 = no match (only when filter active)
		public bool Checked = false;
		public string? Icon = null;
	}

	// ---------- state ----------

	readonly string projectPath;
	readonly string solutionPath;

	readonly TextBox filterBox = new ();
	readonly ListBox packageList = new ();
	readonly ListBox projectList = new ();
	readonly ListBox assemblyList = new ();
	readonly ListBox allList = new ();
	readonly ListBox selectedList = new ();
	readonly Border panelHost = new ();
	Control assemblyPanelControl = null!;
	Button browseButton = null!;
	Button removeButton = null!;

	readonly List<CandRow> packageRows = new ();
	readonly List<CandRow> projectRows = new ();
	readonly List<CandRow> assemblyRows = new ();
	readonly List<RefRow> refs = new ();
	readonly List<AssemblyRecentEntry> recentEntries = new ();

	readonly string tfm;
	readonly bool hasTfm;
	readonly string refPackDir;

	int selectedTab; // 0 All, 1 Packages, 2 Projects, 3 Assembly

	public string? AddedReference { get; private set; }

	class AssemblyRecentEntry
	{
		public string Name = "";
		public string Path = "";
		public string Version = "";
		public bool IsGlobal = true; // legacy: entries outside the "# <sln>" section
	}

	// ---------- ctor ----------

	public AddReferenceDialog (string projectPath) : this (projectPath, null) { }

	public AddReferenceDialog (string projectPath, string? solutionPath)
	{
		this.projectPath = projectPath;
		this.solutionPath = solutionPath ?? "";

		var model = ConfigurationService.LoadProjectModel (projectPath);
		this.tfm = model?.TargetFrameworks?.FirstOrDefault () ?? "";
		this.hasTfm = tfm.Length > 0;
		this.refPackDir = ResolveRefPackDir (tfm);

		BuildUi ();
		LoadProject ();
		PopulatePackages ();
		PopulateProjects ();
		LoadRecents ();
		RefreshAll ();
	}

	// ---------- shared UI helpers ----------

	static Color GetColor (string key)
	{
		var br = Application.Current?.FindResource (key) as IBrush;
		if (br is SolidColorBrush scb)
			return scb.Color;
		return Color.FromRgb (0x30, 0x30, 0x30);
	}

	static IImage? IconImage (string? name)
	{
		return name is null ? null : IconService.GetImage (name);
	}

	static Control MakeNameCell (CandRow row, string filter, bool dimName, string? secondary)
	{
		var fg = GetColor ("IdeFgBrush");
		var nameBlock = new TextBlock {
			FontSize = 12,
			LineHeight = 15,
			Foreground = new SolidColorBrush (dimName ? Color.FromRgb (0x80, 0x80, 0x80) : fg),
		};
		string text = row.Name + (row.Cyclic ? " (Cyclic dependencies not allowed)" : "");
		if (!string.IsNullOrEmpty (filter) && filter.Length > 0) {
			var positions = GetMatchPositions (filter, text);
			var bg = GetColor ("IdeWindowBgBrush");
			// HslColor's implicit ops are against Gdk.Color (legacy); build it
			// from the brush RGB and convert the result back through H/S/L.
			var bgHsl = new MdHslColor (bg.R / 255d, bg.G / 255d, bg.B / 255d);
			var fgHsl = new MdHslColor (fg.R / 255d, fg.G / 255d, fg.B / 255d);
			var hl = MdHslColor.GenerateHighlightColors (bgHsl, fgHsl, 3) [2];
			hl.ToRgb (out double hr, out double hg, out double hb);
			var hlBrush = new SolidColorBrush (Color.FromRgb ((byte) (255 * hr), (byte) (255 * hg), (byte) (255 * hb)));
			int start = 0;
			for (int i = 0; i < text.Length; i++) {
				if (positions [i]) {
					if (i > start)
						nameBlock.Inlines.Add (new Run (text [start..i]));
					nameBlock.Inlines.Add (new Run (text [i].ToString ()) {
						Background = hlBrush,
						Foreground = new SolidColorBrush (fg),
					});
					start = i + 1;
			}
		}
		if (start < text.Length)
			nameBlock.Inlines.Add (new Run (text [start..]));
	} else {
		nameBlock.Inlines.Add (new Run (row.Name));
		if (row.Cyclic)
			nameBlock.Inlines.Add (new Run (" (Cyclic dependencies not allowed)") {
				Foreground = new SolidColorBrush (Color.FromRgb (0x80, 0x80, 0x80)),
			});
	}
	if (row.TfmIncompatible)
		nameBlock.Opacity = 0.5;
	if (string.IsNullOrEmpty (secondary))
		return nameBlock;
	var secBlock = new TextBlock {
		Text = secondary,
		FontSize = 10,
		LineHeight = 13,
		Opacity = 0.65,
		TextTrimming = TextTrimming.CharacterEllipsis,
		Foreground = new SolidColorBrush (fg),
	};
	var panel = new StackPanel { Spacing = 0, VerticalAlignment = VerticalAlignment.Center, Children = { nameBlock, secBlock } };
	return panel;
	}

	// legacy GetMatchMarkup positions: indices of the matched characters (StringMatcher.GetMatch).
	static bool [] GetMatchPositions (string filter, string text)
	{
		var pos = new bool [text.Length];
		try {
			var m = StringMatcher.GetMatcher (filter, false).GetMatch (text);
			foreach (var p in m)
				if (p >= 0 && p < text.Length)
					pos [p] = true;
		} catch {
		}
		return pos;
	}

	// ---------- UI (legacy .glade: VBox [HPaned (590) | action row], title "References", 889x551) ----------

	void BuildUi ()
	{
		Title = "References";
		Width = 889;
		Height = 551;
		ShowTitleRow ();

		var borderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!;

		// ----- filter entry (legacy CombinedBox action widget at the END of the
		// tab strip: SearchEntry 180 px, Ctrl+F focuses it) -----
		filterBox.Width = 180;
		filterBox.Watermark = "Search";
		filterBox.KeyDown += (_, e) => {
			if (e.Key == Key.F && (e.KeyModifiers & KeyModifiers.Control) != 0) {
				filterBox.Focus ();
				e.Handled = true;
			}
		};
		filterBox.TextChanged += (_, _) => RefreshAll ();

		// ----- tab strip (legacy notebook: All / Packages / Projects / .Net Assembly) -----
		var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Margin = new Thickness (6, 6, 0, 0) };
		foreach (var (name, i) in new [] { ("All", 0), ("Packages", 1), ("Projects", 2), (".Net Assembly", 3) }) {
			int idx = i;
			var tabName = name;
			var btn = new ToggleButton {
				Content = tabName,
				FontSize = 12,
				Padding = new Thickness (12, 5, 12, 5),
				Tag = idx,
				IsChecked = idx == 0,
				Classes = { "ide-tab" },
			};
			btn.Click += (_, _) => {
				selectedTab = idx;
				foreach (ToggleButton t in tabs.Children.OfType<ToggleButton> ())
					t.IsChecked = t.Tag as int? == idx;
				// legacy: filterEntry.Sensitive = PageNum != 3
				filterBox.IsEnabled = idx != 3;
				SwitchPanel (); // swap the visible ListBox (RefreshAll only re-renders)
				RefreshAll ();
			};
			tabs.Children.Add (btn);
		}
		tabs.Children.Add (new Control { Width = 8 });
		tabs.Children.Add (filterBox);

		// ----- candidate panels (one per tab; the visible one swaps) -----
		panelHost.BorderBrush = borderBrush;
		panelHost.BorderThickness = new Thickness (1);
		panelHost.Margin = new Thickness (6, 2, 6, 0);
		foreach (var lb in new [] { allList, packageList, projectList, assemblyList })
			lb.DoubleTapped += (_, _) => AddSelectedCandidate (lb);
		// legacy OnReferencesTreeViewDelete: Delete removes from the
		// "Selected references:" tree (the right pane), not from the candidates.
		selectedList.KeyDown += (_, e) => {
			if (e.Key == Key.Delete) {
				RemoveSelectedRef ();
				e.Handled = true;
			}
		};

		// Assembly panel extra: Browse... button bottom-right (legacy BrowseButton).
		browseButton = new Button { Content = "Browse...", MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness (0, 4, 4, 4) };
		browseButton.Click += async (_, _) => await BrowseForAssemblies ();
		assemblyPanelControl = new Grid {
			RowDefinitions = RowDefinitions.Parse ("*,Auto"),
		};
		Grid.SetRow (assemblyList, 0);
		Grid.SetRow (browseButton, 1);
		((Grid)assemblyPanelControl).Children.Add (assemblyList);
		((Grid)assemblyPanelControl).Children.Add (browseButton);

		// ----- right pane: "Selected references:" + flat remove icon button -----
		var selHeader = new Grid {
			ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto"),
			Margin = new Thickness (6, 6, 6, 0),
		};
		var selLabel = new TextBlock {
			Text = "Selected references:",
			FontWeight = FontWeight.SemiBold,
			FontSize = 12,
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = new SolidColorBrush (GetColor ("IdeFgBrush")),
		};
		// legacy RemoveReferenceButton: flat gtk-delete icon button, tooltip "Remove"
		// (Relief.None → transparent/borderless, the shell has no Relief).
		removeButton = new Button {
			Content = IconImage ("gtk-delete"),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
			Width = 24,
			Height = 24,
		};
		ToolTip.SetTip (removeButton, "Remove");
		removeButton.Click += (_, _) => RemoveSelectedRef ();
		Grid.SetColumn (removeButton, 1);
		var selListHost = new Border {
			Child = selectedList,
			BorderBrush = borderBrush,
			BorderThickness = new Thickness (1),
			Margin = new Thickness (6, 2, 6, 6),
		};
		selectedList.DoubleTapped += (_, _) => OkClicked ();

		selHeader.Children.Add (selLabel);
		selHeader.Children.Add (removeButton);
		var rightPane = new Grid {
			RowDefinitions = RowDefinitions.Parse ("Auto,*"),
		};
		Grid.SetRow (selHeader, 0);
		Grid.SetRow (selListHost, 1);
		rightPane.Children.Add (selHeader);
		rightPane.Children.Add (selListHost);

		// ----- split, position 590 (legacy hpaned1 Position=590; the Avalonia
		// shell has no HPaned — Grid + GridSplitter, the ErrorsPad pattern) -----
		var leftPane = new Grid {
			RowDefinitions = RowDefinitions.Parse ("Auto,*"),
		};
		Grid.SetRow (tabs, 0);
		Grid.SetRow (panelHost, 1);
		leftPane.Children.Add (tabs);
		leftPane.Children.Add (panelHost);
		var splitGrid = new Grid {
			ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto,*"),
			Margin = new Thickness (6),
		};
		var split = new GridSplitter {
			Width = 4,
			Background = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			VerticalAlignment = VerticalAlignment.Stretch,
		};
		Grid.SetColumn (leftPane, 0);
		Grid.SetColumn (split, 1);
		Grid.SetColumn (rightPane, 2);
		splitGrid.Children.Add (leftPane);
		splitGrid.Children.Add (split);
		splitGrid.Children.Add (rightPane);
		// First real allocation: commit the legacy position 590 to the left column.
		bool initialSplit = true;
		splitGrid.SizeChanged += (_, _) => {
			if (initialSplit && splitGrid.Bounds.Width > 0) {
				splitGrid.ColumnDefinitions [0].Width = new GridLength (Math.Min (590, splitGrid.Bounds.Width - 120));
				initialSplit = false;
			}
		};
		var hpaned = splitGrid;

		// ----- bottom action row (legacy OK / Cancel) -----
		var ok = new Button { Content = "_OK", MinWidth = 88, IsDefault = true, Classes = { "chromebtn" } };
		ok.Click += (_, _) => OkClicked ();
		var cancel = new Button { Content = "_Cancel", MinWidth = 88, IsCancel = true, Classes = { "chromebtn" } };
		cancel.Click += (_, _) => Close ();
		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8,
			Margin = new Thickness (6),
		};
		buttons.Children.Add (ok);
		buttons.Children.Add (cancel);

		var root = new Grid {
			RowDefinitions = RowDefinitions.Parse ("*,Auto"),
		};
		Grid.SetRow (hpaned, 0);
		Grid.SetRow (buttons, 1);
		root.Children.Add (hpaned);
		root.Children.Add (buttons);

		Content = root;

		Opened += (_, _) => SwitchPanel ();
	}

	void SwitchPanel ()
	{
		var panel = selectedTab switch {
			1 => (Control)packageList,
			2 => (Control)projectList,
			3 => assemblyPanelControl,
			_ => (Control)allList,
		};
		panelHost.Child = panel;
	}

	// ---------- data load (legacy Init/ReadReference: project references + TFM) ----------

	/// <summary>Directory of the Microsoft.NETCore.App.Ref pack containing this
	/// TFM (legacy: the reference assemblies of the target framework). Uses the
	/// user's DOTNET_ROOT / ~/.dotnet/packs (the port's /usr/lib/dotnet/packs
	/// probe was wrong: that path does not exist on this machine).</summary>
	static string ResolveRefPackDir (string tfm)
	{
		string [] roots = new []
		{
			Environment.GetEnvironmentVariable ("DOTNET_ROOT"),
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".dotnet"),
			"/usr/lib/dotnet",
			"/usr/share/dotnet",
		}.Where (r => !string.IsNullOrEmpty (r)).Cast<string> ().ToArray ();
		foreach (var root in roots) {
			var packsRoot = Path.Combine (root!, "packs", "Microsoft.NETCore.App.Ref");
			if (!Directory.Exists (packsRoot))
				continue;
			// Prefer the newest pack whose ref/<tfm> exists.
			var candidates = Directory.EnumerateDirectories (packsRoot)
				.Where (d => Directory.Exists (Path.Combine (d, "ref", tfm)))
				.OrderByDescending (d => d)
				.ToList ();
			if (candidates.Count > 0)
				return Path.Combine (candidates [0], "ref", tfm);
		}
		return "";
	}

	void LoadProject ()
	{
		refs.Clear ();
		// The references come from the project model (ConfigurationService,
		// Oleada 4 slice F) — the same source the pad and Build/Run consume.
		var model = ConfigurationService.LoadProjectModel (projectPath);
		if (model is not null) {
			foreach (var ri in model.References) {
				switch (ri.Kind) {
				case "PackageReference":
					refs.Add (new RefRow { Name = ri.Include.Split (',') [0].Trim (), Secondary = ri.Version ?? "", Type = "Package", Kind = "PackageReference", Raw = ri.Include });
					break;
				case "ProjectReference":
					var inc = ri.Include.Replace ('\\', Path.DirectorySeparatorChar);
					refs.Add (new RefRow { Name = Path.GetFileNameWithoutExtension (inc), Secondary = inc, Type = "Project", Kind = "ProjectReference", Raw = ri.Include });
					break;
				default:
					refs.Add (new RefRow { Name = ri.Include.Split (',') [0].Trim (), Secondary = ri.HintPath ?? "", Type = "Assembly", Kind = "Reference", Raw = ri.Include.Split (',') [0].Trim (), HintPath = ri.HintPath });
					break;
				}
			}
		} else if (File.Exists (projectPath)) {
			// Model null with the file present: unparseable csproj — legacy
			// tolerance (dialog lists nothing, like the broken pad node).
			Console.WriteLine ("[addref] load error: unparseable csproj");
		}
	}


	// ---------- candidate population (legacy Init per panel) ----------

	void PopulatePackages ()
	{
		packageRows.Clear ();
		// Framework reference assemblies of the project TFM (legacy:
		// the ref pack enumeration of the target framework).
		if (Directory.Exists (refPackDir)) {
			try {
				foreach (var dll in Directory.EnumerateFiles (refPackDir, "*.dll", SearchOption.AllDirectories)) {
					string name, version;
					try {
						var an = System.Reflection.AssemblyName.GetAssemblyName (dll);
						name = an.Name;
						version = an.Version.ToString ();
					} catch {
						continue; // unreadable assembly: legacy skips it too
					}
					packageRows.Add (new CandRow {
						Name = name,
						Version = version,
						Meta = "Package",
						Kind = "Reference", // framework assembly: legacy <Reference Include="NombreCorto" />
						ProjectFile = dll,
						Icon = "md-empty-file-icon",
					});
				}
			} catch { /* unreadable pack */ }
		}
		// KnownPackages complement (legacy NuGet repository list).
		foreach (var p in KnownPackages)
			packageRows.Add (new CandRow {
				Name = p,
				Version = "",
				Meta = "Package",
				Kind = "PackageReference",
				Icon = "md-package",
			});
	}

	void PopulateProjects ()
	{
		projectRows.Clear ();
		var selfFull = SafeFullPath (projectPath);
		List<string> solutionProjects;
		if (solutionPath.Length > 0) {
			solutionProjects = ConfigurationService.GetSolutionProjects (solutionPath);
		} else {
			// Standalone: project files in the tree (legacy disk scan).
			var slnDir = Path.GetDirectoryName (projectPath) ?? "";
			solutionProjects = new List<string> ();
			try {
				solutionProjects.AddRange (Directory.EnumerateFiles (slnDir, "*.csproj", SearchOption.AllDirectories));
				solutionProjects.AddRange (Directory.EnumerateFiles (slnDir, "*.fsproj", SearchOption.AllDirectories));
			} catch { /* unreadable dirs */ }
		}
		foreach (var csproj in solutionProjects) {
			if (string.Equals (SafeFullPath (csproj), selfFull, StringComparison.OrdinalIgnoreCase))
				continue;
			projectRows.Add (new CandRow {
				Name = Path.GetFileNameWithoutExtension (csproj),
				Meta = Path.GetDirectoryName (csproj) ?? "",
				Kind = "ProjectReference",
				ProjectFile = SafeFullPath (csproj),
				Icon = "md-project",
			});
		}
		ComputeCyclicProjects ();
		ComputeTfmCompat ();
	}

	static string SafeFullPath (string p)
	{
		try { return Path.GetFullPath (p); } catch { return p; }
	}

	/// <summary>Legacy ProjectReferencePanel.ProjectReferencesProject: DFS from
	/// the current project over its ProjectReferences; anything reachable is
	/// cyclic for us (a cycle back to the current project would form).</summary>
	void ComputeCyclicProjects ()
	{
		// legacy ProjectReferencePanel.ProjectReferencesProject (L234): DFS from
		// the current project's ProjectReferences (resolved absolute paths);
		// anything that can reach back to the current project is cyclic.
		var selfFull = SafeFullPath (projectPath).ToLowerInvariant ();
		var baseDir = Path.GetDirectoryName (projectPath) ?? "";
		var reachable = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		var stack = new Stack<string> ();
		PushProjectRefs (projectPath, baseDir, stack);
		while (stack.Count > 0) {
			var cur = stack.Pop ();
			if (cur.ToLowerInvariant () == selfFull)
				continue;
			if (!reachable.Add (cur))
				continue;
			var curDir = Path.GetDirectoryName (cur) ?? "";
			PushProjectRefs (cur, curDir, stack);
		}
		foreach (var row in projectRows)
			row.Cyclic = reachable.Contains (row.ProjectFile);
	}

	static void PushProjectRefs (string csproj, string baseDir, Stack<string> stack)
	{
		var m = ConfigurationService.LoadProjectModel (csproj);
		if (m is null)
			return;
		foreach (var ri in m.References) {
			if (ri.Kind != "ProjectReference")
				continue;
			var inc = ri.Include.Replace ('\\', Path.DirectorySeparatorChar);
			var dep = Path.IsPathRooted (inc) ? inc : Path.GetFullPath (Path.Combine (baseDir, inc));
			if (File.Exists (dep))
				stack.Push (dep);
		}
	}

	/// <summary>Legacy simplified rule: a project candidate is incompatible when
	/// its TFM identifier differs from ours, or its version is higher (a project
	/// cannot reference a higher-TFM project). Documented divergence: the legacy
	/// RequiredTargetFramework check is reduced to identifier + version.
	/// (Uses TargetFrameworks from the project model, Oleada 4 slice 2.)</summary>
	void ComputeTfmCompat ()
	{
		if (!hasTfm)
			return;
		foreach (var row in projectRows.Where (r => !r.Cyclic)) {
			var m = ConfigurationService.LoadProjectModel (row.ProjectFile);
			if (m is null)
				continue;
			var otherTfm = m.TargetFrameworks is { Count: > 0 } ? m.TargetFrameworks [0] : "";
			if (otherTfm.Length == 0)
				continue;
			if (TfmIdentifier (otherTfm) != TfmIdentifier (tfm) || TfmVersion (otherTfm) > TfmVersion (tfm))
				row.TfmIncompatible = true;
		}
	}

	static string TfmIdentifier (string tfm) // net10.0 -> "net"
	{
		var t = tfm.TrimStart ('-').Trim ();
		int i = 0;
		while (i < t.Length && (char.IsLetter (t [i]) || t [i] == '.'))
			i++;
		return t[..Math.Min (i, t.Length)].ToLowerInvariant ();
	}

	static int TfmVersion (string tfm)
	{
		var t = tfm.TrimStart ('-').Trim ();
		int i = 0;
		while (i < t.Length && (char.IsLetter (t [i]) || t [i] == '.'))
			i++;
		int major = 0;
		int.TryParse (t [i..], out major);
		return major;
	}

	// ---------- recents (legacy RecentAssemblies2.txt: global section + "# <sln>") ----------

	static string RecentFile ()
	{
		return Path.Combine (UserProfile.Current.CacheDir.ToString (), "RecentAssemblies2.txt");
	}

	void LoadRecents ()
	{
		recentEntries.Clear ();
		try {
			var path = RecentFile ();
			if (!File.Exists (path))
				return;
			var slnName = solutionPath.Length > 0 ? Path.GetFileName (solutionPath) : null;
			string? section = null; // null = global
			foreach (var rawLine in File.ReadLines (path)) {
				var line = rawLine.Trim ();
				if (line.Length == 0)
					continue;
				if (line.StartsWith ("# ")) {
					section = line [2..];
					continue;
				}
				// line: Name|Version|Path
				var parts = line.Split ('|');
				if (parts.Length < 3)
					continue;
				var entry = new AssemblyRecentEntry {
					Name = parts [0],
					Version = parts [1],
					Path = string.Join ('|', parts.Skip (2)),
					IsGlobal = section is null,
				};
				if (slnName is null)
				{
					if (!entry.IsGlobal)
						continue; // no solution: only the global section applies
				}
				else if (entry.IsGlobal || section == slnName) {
					// with a solution: global + that solution's section
				} else {
					continue;
				}
				if (!recentEntries.Any (e => string.Equals (e.Path, entry.Path, StringComparison.OrdinalIgnoreCase)))
					recentEntries.Add (entry);
			}
		} catch { /* unreadable recents */ }
		PopulateAssemblies ();
	}

	void SaveRecent (string name, string path, string version)
	{
		try {
			var pathAbs = SafeFullPath (path);
			if (recentEntries.Any (e => string.Equals (e.Path, pathAbs, StringComparison.OrdinalIgnoreCase)))
				return;
			// legacy: registered in the global section when the file is external to
			// the solution, in the per-solution section otherwise (non-nuget).
			var slnName = solutionPath.Length > 0 ? Path.GetFileName (solutionPath) : null;
			var slnDir = solutionPath.Length > 0 ? SafeFullPath (Path.GetDirectoryName (solutionPath) ?? "") : "";
			bool isExternal = slnDir.Length == 0 ||
				!pathAbs.StartsWith (slnDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
			recentEntries.Insert (0, new AssemblyRecentEntry { Name = name, Path = pathAbs, Version = version, IsGlobal = slnName is null || isExternal });
			// legacy: max 75 entries.
			while (recentEntries.Count > 75)
				recentEntries.RemoveAt (recentEntries.Count - 1);
			var path2 = RecentFile ();
			Directory.CreateDirectory (Path.GetDirectoryName (path2) ?? "");
			// rewrite the whole file preserving the section layout (global first,
			// then the "# <sln>" section).
			var outLines = new List<string> ();
			foreach (var e in recentEntries.Where (e => e.IsGlobal))
				outLines.Add ($"{e.Name}|{e.Version}|{e.Path}");
			if (slnName is not null) {
				outLines.Add ("# " + slnName);
				foreach (var e in recentEntries.Where (e => !e.IsGlobal))
					outLines.Add ($"{e.Name}|{e.Version}|{e.Path}");
			}
			File.WriteAllLines (path2, outLines);
			PopulateAssemblies ();
		} catch { /* best effort */ }
	}

	void PopulateAssemblies ()
	{
		assemblyRows.Clear ();
		foreach (var e in recentEntries) {
			if (!File.Exists (e.Path))
				continue;
			// legacy validates each recent with AssemblyName before listing it.
			string name, version;
			try {
				var an = System.Reflection.AssemblyName.GetAssemblyName (e.Path);
				name = an.Name;
				version = an.Version.ToString ();
			} catch {
				continue;
			}
			assemblyRows.Add (new CandRow {
				Name = name,
				Version = version,
				Meta = e.Path,
				Kind = "Reference",
				ProjectFile = e.Path,
				Icon = "md-empty-file-icon",
			});
		}
	}

	// ---------- rendering (legacy sort: matchRank desc -> name -> version) ----------

	void RefreshAll ()
	{
		var filter = filterBox.Text?.Trim () ?? "";
		StringMatcher? matcher = null;
		if (filter.Length > 0 && selectedTab != 3)
			matcher = StringMatcher.GetMatcher (filter, false);

		RenderPackagePanel (matcher, filter);
		RenderProjectPanel (matcher, filter);
		RenderAssemblyPanel (matcher, filter);
		RenderAllPanel (matcher, filter);
		RenderSelected ();
	}

	static List<CandRow> Sorted (IEnumerable<CandRow> rows, StringMatcher? matcher, string filter, Func<CandRow, bool> tabOk, bool hideCyclicWhenFilter)
	{
		var query = filter.Trim ();
		var result = new List<CandRow> ();
		foreach (var r in rows) {
			if (!tabOk (r))
				continue;
			if (r.Cyclic && query.Length > 0 && hideCyclicWhenFilter)
				continue;
			if (matcher is null) {
				r.Rank = 0;
				result.Add (r);
				continue;
			}
			if (matcher.CalcMatchRank (r.Name, out int rank)) {
				r.Rank = rank;
				result.Add (r);
			}
		}
		result.Sort ((a, b) => {
			int c = b.Rank.CompareTo (a.Rank); // matchRank desc
			if (c != 0) return c;
			c = string.Compare (a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);
			if (c != 0) return c;
			return string.Compare (a.Version, b.Version, StringComparison.InvariantCulture);
		});
		return result;
	}

	Control MakeRow (CandRow r, StringMatcher? matcher, string filter, Func<CandRow, string?> secondary, bool dimName)
	{
		var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness (6, 0, 4, 0), IsChecked = r.Checked, IsEnabled = !r.Cyclic };
		var icon = new Avalonia.Controls.Image { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center, Source = IconImage (r.Icon) };
		if (r.Cyclic)
			icon.Opacity = 0.5; // legacy: icon alpha 0.5 for cyclic
		var nameCell = MakeNameCell (r, filter, dimName, secondary (r));
		nameCell.Margin = new Thickness (4, 1, 4, 1);
		var row = new Grid {
			ColumnDefinitions = ColumnDefinitions.Parse ("Auto,Auto,*"),
			Tag = r,
			Height = 32,
		};
		Grid.SetColumn (cb, 0);
		Grid.SetColumn (icon, 1);
		Grid.SetColumn (nameCell, 2);
		row.Children.Add (cb);
		row.Children.Add (icon);
		row.Children.Add (nameCell);
		cb.IsCheckedChanged += (_, _) => r.Checked = cb.IsChecked == true;
		return row;
	}

	void RenderPackagePanel (StringMatcher? matcher, string filter)
	{
		var rows = Sorted (packageRows, matcher, filter, r => r.Kind == "PackageReference" || (r.Kind == "Reference" && !string.IsNullOrEmpty (r.ProjectFile) && r.ProjectFile.StartsWith (refPackDir + Path.DirectorySeparatorChar)), false);
		packageList.Items.Clear ();
		foreach (var r in rows)
			packageList.Items.Add (MakeRow (r, matcher, filter, x => x.Kind == "PackageReference" ? $"{x.Name} {x.Version}".Trim () : $"{x.Version}".Trim (), false));
	}

	void RenderProjectPanel (StringMatcher? matcher, string filter)
	{
		var rows = Sorted (projectRows, matcher, filter, r => r.Kind == "ProjectReference", true);
		projectList.Items.Clear ();
		foreach (var r in rows)
			projectList.Items.Add (MakeRow (r, matcher, filter, x => x.Meta, true));
	}

	void RenderAssemblyPanel (StringMatcher? matcher, string filter)
	{
		// legacy: the filter is insensitive on the .Net Assembly tab.
		var rows = assemblyRows.OrderBy (r => r.Name, StringComparer.InvariantCultureIgnoreCase).ToList ();
		assemblyList.Items.Clear ();
		foreach (var r in rows)
			assemblyList.Items.Add (MakeRow (r, null, "", x => $"{x.Version}  {x.Meta}".Trim (), false));
	}

	void RenderAllPanel (StringMatcher? matcher, string filter)
	{
		// legacy All tab: packages + system assemblies + projects, one list.
		var all = new List<CandRow> ();
		all.AddRange (packageRows);
		all.AddRange (projectRows);
		var rows = Sorted (all, matcher, filter, r => true, false);
		allList.Items.Clear ();
		foreach (var r in rows) {
			string sec = r.Kind switch {
				"PackageReference" => $"{r.Name} {r.Version}".Trim (),
				"ProjectReference" => r.Meta,
				_ => $"{r.Version}".Trim (),
			};
			allList.Items.Add (MakeRow (r, matcher, filter, _ => sec, r.Kind == "ProjectReference"));
		}
	}

	void RenderSelected ()
	{
		selectedList.Items.Clear ();
		var ordered = refs.OrderBy (r => r.Name, StringComparer.InvariantCultureIgnoreCase).ToList ();
		foreach (var r in ordered) {
			var cb = new CheckBox { IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness (6, 0, 4, 0) };
			var icon = new Avalonia.Controls.Image { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center, Source = IconImage (r.Kind == "ProjectReference" ? "md-project" : r.Kind == "Reference" ? "md-empty-file-icon" : "md-package") };
			var nameCell = new StackPanel { Spacing = 0, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness (4, 1, 4, 1) };
			var fg = GetColor ("IdeFgBrush");
			var nameBlock = new TextBlock { Text = r.Name, FontSize = 12, LineHeight = 15, Foreground = new SolidColorBrush (fg) };
			nameCell.Children.Add (nameBlock);
			if (r.Secondary.Length > 0 && r.Secondary != r.Name) {
				var secBlock = new TextBlock { Text = r.Secondary, FontSize = 10, LineHeight = 13, Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = new SolidColorBrush (fg) };
				nameCell.Children.Add (secBlock);
			}
			var row = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("Auto,Auto,*"), Tag = r, Height = 32 };
			Grid.SetColumn (cb, 0);
			Grid.SetColumn (icon, 1);
			Grid.SetColumn (nameCell, 2);
			row.Children.Add (cb);
			row.Children.Add (icon);
			row.Children.Add (nameCell);
			selectedList.Items.Add (row);
		}
	}

	// ---------- actions (legacy Ok / Cancel / Remove / double-click / Browse) ----------

	CandRow? CandidateAt (ListBox list, object? item)
	{
		if (item is Control c && c.Tag is CandRow cr)
			return cr;
		if (item is CandRow cr2)
			return cr2;
		return null;
	}

	void AddSelectedCandidate (ListBox list)
	{
		var idx = list.SelectedIndex;
		if (idx < 0 || idx >= list.Items.Count)
			return;
		var row = CandidateAt (list, list.Items [idx]);
		if (row is null)
			return;
		AddCandidate (row);
	}

	void AddCandidate (CandRow row)
	{
		if (row.Cyclic || row.TfmIncompatible) {
			Output ($"[refs] '{row.Name}' is not referenceable");
			return;
		}
		if (refs.Any (r => SameReference (r, row))) {
			Output ($"[refs] '{row.Name}' is already referenced");
			return;
		}
		switch (row.Kind) {
		case "ProjectReference":
			var inc = MakeProjectInclude (row.ProjectFile);
			refs.Add (new RefRow {
				Name = row.Name,
				Secondary = inc,
				Type = "Project",
				Kind = "ProjectReference",
				Raw = inc,
			});
			AddedReference = row.Name;
			break;
		case "PackageReference":
			refs.Add (new RefRow {
				Name = row.Name,
				Secondary = row.Version,
				Type = "Package",
				Kind = "PackageReference",
				Raw = row.Name,
			});
			AddedReference = row.Name;
			break;
		default:
			// assembly (framework ref or browsed): <Reference Include="short"/> + HintPath
			refs.Add (new RefRow {
				Name = row.Name,
				Secondary = row.Meta,
				Type = "Assembly",
				Kind = "Reference",
				Raw = row.Name,
				HintPath = row.Kind == "Reference" && !row.ProjectFile.StartsWith (refPackDir + Path.DirectorySeparatorChar) ? row.ProjectFile : null,
			});
			AddedReference = row.Name;
			if (!row.ProjectFile.StartsWith (refPackDir + Path.DirectorySeparatorChar))
				SaveRecent (row.Name, row.ProjectFile, row.Version);
			break;
		}
		RenderSelected ();
	}

	bool SameReference (RefRow r, CandRow row)
	{
		switch (row.Kind) {
		case "ProjectReference":
			return r.Kind == "ProjectReference" &&
				string.Equals (r.Raw.Replace ('\\', Path.DirectorySeparatorChar), MakeIncRel (row.ProjectFile), StringComparison.OrdinalIgnoreCase);
		case "PackageReference":
			return r.Kind == "PackageReference" && string.Equals (r.Name, row.Name, StringComparison.OrdinalIgnoreCase);
		default:
			return r.Kind == "Reference" && string.Equals (r.Name, row.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	void RemoveSelectedRef ()
	{
		var idx = selectedList.SelectedIndex;
		if (idx < 0)
			return;
		var item = selectedList.Items [idx] as Control;
		var r = item?.Tag as RefRow;
		if (r is null)
			return;
		refs.Remove (r);
		RenderSelected ();
	}

	string MakeIncRel (string projectFile)
	{
		var baseDir = Path.GetDirectoryName (projectPath) ?? "";
		try { return Path.GetRelativePath (baseDir, projectFile); }
		catch { return projectFile; }
	}

	void Output (string msg) => MainWindow.Instance?.Output (msg);

	// ---------- Browse (legacy BrowseButton: multi-select dll picker) ----------

	async Task BrowseForAssemblies ()
	{
		var provider = TopLevel.GetTopLevel (this)?.StorageProvider;
		if (provider is null)
			return;
		IStorageFolder? start = null;
		var lastDir = SettingsStore.GetString ("AddReference.LastBrowseDir");
		if (!string.IsNullOrEmpty (lastDir) && Directory.Exists (lastDir))
			start = await provider.TryGetFolderFromPathAsync (lastDir!);
		var filter = new FilePickerFileType (".NET assemblies") {
			Patterns = new [] { "*.dll", "*.exe" },
		};
		var picked = await provider.OpenFilePickerAsync (new FilePickerOpenOptions {
			Title = "Select Assemblies",
			AllowMultiple = true,
			FileTypeFilter = new [] { filter },
			SuggestedStartLocation = start,
		});
		if (picked is null || picked.Count == 0)
			return;
		var dir = picked [0].Path.LocalPath;
		var parent = Path.GetDirectoryName (dir);
		if (!string.IsNullOrEmpty (parent))
			SettingsStore.SetString ("AddReference.LastBrowseDir", parent);
		foreach (var item in picked) {
			var path = item.Path.LocalPath;
			string name, version;
			try {
				var an = System.Reflection.AssemblyName.GetAssemblyName (path);
				name = an.Name;
				version = an.Version.ToString ();
			} catch {
				continue;
			}
			var exists = assemblyRows.Any (r => string.Equals (r.ProjectFile, path, StringComparison.OrdinalIgnoreCase));
			if (!exists) {
				assemblyRows.Add (new CandRow {
					Name = name,
					Version = version,
					Meta = path,
					Kind = "Reference",
					ProjectFile = path,
					Icon = "md-empty-file-icon",
				});
			}
			SaveRecent (name, path, version);
		}
		RenderAssemblyPanel (null, "");
	}

	/// <summary>Legacy ProjectReference Include: the path relative to the project
	/// directory with BACKSLASHES (MSBuildProjectService.ToMSBuildPath, L898 —
	/// the fixture writes ..\LibA\LibA.csproj on every platform).</summary>
	string MakeProjectInclude (string projectFile)
	{
		var baseDir = Path.GetDirectoryName (projectPath) ?? "";
		string rel;
		try {
			rel = Path.GetRelativePath (baseDir, SafeFullPath (projectFile));
		} catch {
			rel = projectFile;
		}
		return rel.Replace (Path.DirectorySeparatorChar, '\\');
	}

	// ---------- QA surface (headless --addref2 oracle; mirrors the legacy
	// panel counts so automated runs validate the rebuilt dialog without UI) ----------

	public int QaPackages => packageRows.Count;
	public int QaProjects => projectRows.Count;
	public int QaAssemblies => assemblyRows.Count;
	public int QaRefs => refs.Count;
	public int QaCyclicCount => projectRows.Count (r => r.Cyclic);
	public int QaTfmIncompatCount => projectRows.Count (r => r.TfmIncompatible);
	public string QaRefPackDir => refPackDir;

	/// <summary>Applies the filter to the filter box (fires the same
	/// TextChanged path as typing, legacy) and returns how many rows the
	/// CURRENT tab's list renders — the .Net Assembly tab ignores the filter
	/// (legacy filterEntry.Sensitive = PageNum != 3).</summary>
	public int QaFilterHits (string filter)
	{
		filterBox.Text = filter;
		RefreshAll (); // headless oracle: the dialog is never shown, so make sure
		// the same re-render path runs explicitly (typing fires TextChanged).
		var list = selectedTab switch {
			1 => packageList,
			2 => projectList,
			3 => assemblyList,
			_ => allList,
		};
		return list.Items.Count;
	}

	/// <summary>Switches the visible tab (0 All, 1 Packages, 2 Projects,
	/// 3 .Net Assembly) exactly like a tab click, then returns the visible
	/// list's row count.</summary>
	public int QaSwitchTab (int tab)
	{
		selectedTab = Math.Clamp (tab, 0, 3);
		filterBox.IsEnabled = selectedTab != 3;
		SwitchPanel ();
		RefreshAll ();
		var list = selectedTab switch {
			1 => packageList,
			2 => projectList,
			3 => assemblyList,
			_ => allList,
		};
		return list.Items.Count;
	}

	/// <summary>Legacy RecentAssemblies2 round-trip: registers the assembly
	/// and returns the count of .Net Assembly rows it now produces (the entry
	/// is validated with AssemblyName, like legacy Init).</summary>
	public int QaSaveRecent (string name, string path, string version)
	{
		SaveRecent (name, path, version);
		return assemblyRows.Count;
	}

	// ---------- public API (slice F QA surface — kept) + OK/Cancel ----------

	void OkClicked () => Apply ();

	/// <summary>OK: rewrite the csproj reference items via the model writer
	/// (Oleada 4 slice F; legacy ReferenceInformations semantics, everything
	/// else kept intact). The HintPath round-trip (Oleada 5 port) persists
	/// browsed assemblies like the legacy &lt;Reference&gt; with HintPath.</summary>
	bool Apply ()
	{
		var items = refs.OrderBy (r => r.Name, StringComparer.OrdinalIgnoreCase)
			.Select (r => new ConfigurationService.ReferenceItem (
				r.Kind,
				r.Raw,
				r.Kind == "PackageReference" && r.Secondary.Length > 0 ? r.Secondary : null,
				r.Kind == "Reference" ? r.HintPath : null))
			.ToList ();
		bool ok;
		try {
			ok = ConfigurationService.SaveProjectReferences (projectPath, items);
		} catch (Exception ex) {
			Output ("[refs] error saving: " + ex.Message);
			return false;
		}
		if (!ok) {
			Output ("[refs] error saving");
			return false;
		}
		Close ();
		return true;
	}

	/// <summary>Programmatic single-add (QA + menu path): adds a
	/// PackageReference and saves (documented divergence: the dialog itself
	/// resolves packages by TFM; this QA path keeps the slice-F semantics).</summary>
	public bool TryAddReference (string name)
	{
		if (refs.Any (r => r.Kind == "PackageReference" && string.Equals (r.Name, name, StringComparison.OrdinalIgnoreCase))) {
			Output ($"[refs] '{name}' is already referenced");
			return false;
		}
		refs.Add (new RefRow { Name = name, Type = "Package", Kind = "PackageReference", Raw = name });
		AddedReference = name;
		RenderSelected ();
		return Apply ();
	}

	/// <summary>Programmatic project-reference add (QA + reuse): adds a
	/// ProjectReference with a backslash relative include (legacy ToMSBuildPath)
	/// like the double-click on a solution candidate, and saves.</summary>
	public bool TryAddProjectReference (string projectFile)
	{
		var inc = MakeProjectInclude (projectFile);
		var norm = inc.Replace ('\\', Path.DirectorySeparatorChar);
		if (refs.Any (r => r.Kind == "ProjectReference" &&
			string.Equals (r.Raw.Replace ('\\', Path.DirectorySeparatorChar), norm, StringComparison.OrdinalIgnoreCase))) {
			Output ("[refs] '" + Path.GetFileNameWithoutExtension (norm) + "' is already referenced");
			return false;
		}
		refs.Add (new RefRow {
			Name = Path.GetFileNameWithoutExtension (norm),
			Secondary = inc,
			Type = "Project",
			Kind = "ProjectReference",
			Raw = inc,
		});
		AddedReference = Path.GetFileNameWithoutExtension (norm);
		RenderSelected ();
		return Apply ();
	}

	/// <summary>Programmatic remove by name (QA): drops the reference and
	/// saves; false when the name matches no reference.</summary>
	public bool TryRemoveReference (string name)
	{
		var target = refs.FirstOrDefault (r =>
			string.Equals (r.Name, name, StringComparison.OrdinalIgnoreCase)
			|| (r.Kind == "ProjectReference" && string.Equals (Path.GetFileNameWithoutExtension (r.Raw.Replace ('\\', Path.DirectorySeparatorChar)), name, StringComparison.OrdinalIgnoreCase)));
		if (target is null)
			return false;
		refs.Remove (target);
		RenderSelected ();
		return Apply ();
	}
}
