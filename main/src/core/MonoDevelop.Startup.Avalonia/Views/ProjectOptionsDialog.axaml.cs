using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.AvaloniaAddons.Manifests;
using MonoDevelop.Ide.Controls;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Per-project options dialog — port of the legacy ProjectOptionsDialog
/// (MonoDevelop.Ide.Projects, a MultiConfigItemOptionsDialog at 960x680 opened by
/// ProjectOperations.ShowOptions from ProjectCommands.ProjectOptions). It enumerates
/// the /MonoDevelop/ProjectModel/Gui/ItemOptionPanels extension point (every
/// sub-point: Build/General, Build/Compiler, Run, ...), evaluates each node's
/// condition against the active project, and shows one tab per ported panel that
/// applies.
///
/// The legacy dialog draws a section tree on the left and a page on the right; the
/// port keeps the same content with a tab strip (the operator's P1 design). Panels
/// without a ported factory yet (the ILAsm/Packaging/Deployment/XSP nodes) are
/// registered but produce no tab — their ports land in later phases (XSP in P2).
///
/// The per-configuration panels (the legacy MultiConfigItemOptionsPanel: the
/// Configuration:/Platform: combos live in the panel) are driven by the dialog's
/// configuration combo, which holds the solution's configurations and maps each to
/// the project platform through the solution's ProjectConfigurationPlatforms
/// (GetProjectConfigurationFor, the "Any CPU" fallback included).
///
/// Persistence goes through the ConfigurationService P0/P1 API, so both UIs produce
/// value-compatible files. P1 also aligns the VB per-config PropertyGroup condition
/// to the canonical combined form the legacy writer emits:
///   Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "
/// </summary>
public class ProjectOptionsDialog : DialogWindow
{
	const string ItemOptionPanelsPoint = "/MonoDevelop/ProjectModel/Gui/ItemOptionPanels";

	// The legacy panel classes this port implements as code-only Avalonia widgets.
	// The shipped stubs register their panels with the legacy class names; a node
	// matches a tab only when its condition holds AND its class is ported.
	static readonly HashSet<string> PortedPanelClasses = new (StringComparer.Ordinal) {
		"MonoDevelop.VBNetBinding.ProjectOptionsPanel",
		"MonoDevelop.VBNetBinding.ImportOptionsPanel",
		"MonoDevelop.VBNetBinding.ConfigurationOptionsPanel",
	};

	// The legacy General panel lists TextEncoding.SupportedEncodings (every code page
	// the runtime knows). The shell has no System.Text.Encoding.CodePages assembly, so
	// the full list is not enumerable; the common Windows-125x / ISO-8859-x / UTF /
	// CJK code pages the legacy list exposes are listed instead (documented
	// divergence). The combo is editable, so any other id still saves.
	static readonly string[] CodePageIds = {
		"1250", "1251", "1252", "1253", "1254", "1255", "1256", "1257", "1258",
		"932", "936", "949", "950", "28591", "28592", "28593", "28594", "28595",
		"28596", "28597", "28598", "28599", "28600", "28601", "28603", "28605",
		"28606", "65000", "65001", "874", "10007",
	};

	readonly string? projectPath;
	readonly string? solutionPath;
	readonly IAddonExtensionRegistry? registry;
	readonly ConfigurationService.ProjectKindInfo? kind;
	readonly List<PanelBase> panels = new ();
	readonly Dictionary<string, string> configPlatforms = new (StringComparer.Ordinal);

	readonly TextBlock headerText;
	readonly ComboBox configCombo;
	readonly StackPanel tabs;
	readonly StackPanel body;
	readonly TextBlock emptyText;
	readonly Button okButton;

	public string? ProjectPath => projectPath;
	public string? SolutionPath => solutionPath;
	public ConfigurationService.ProjectKindInfo? Kind => kind;
	public IReadOnlyList<PanelBase> Panels => panels;
	public string SelConfig { get; private set; } = "";
	public string SelPlatform { get; private set; } = "Any CPU";

	bool suppressConfig;

	public ProjectOptionsDialog (string? projectPath, string? solutionPath)
	{
		this.projectPath = projectPath;
		this.solutionPath = solutionPath;
		this.registry = App.Addins?.Extensions;
		this.kind = projectPath is not null ? ConfigurationService.GetProjectKind (projectPath) : null;

		var name = projectPath is not null ? Path.GetFileNameWithoutExtension (projectPath) : "(no solution)";
		Title = "Project Options – " + name;
		Width = 960;
		Height = 680;
		ShowTitleRow ();

		headerText = new TextBlock {
			Margin = new Thickness (12, 8, 12, 0),
			FontSize = 12,
			TextTrimming = TextTrimming.CharacterEllipsis,
		};
		headerText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));

		// Legacy MultiConfigItemOptionsPanel: the Configuration:/Platform: selector.
		// The port hoists a single Configuration combo into the dialog header so one
		// selection drives every per-config panel at once (the platform is resolved
		// from the solution mapping and shown in the header, as the F5 panel does).
		var configRow = new StackPanel {
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Margin = new Thickness (12, 4, 12, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		configRow.Children.Add (new TextBlock {
			Text = "Configuration:",
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 12,
		});
		configCombo = new ComboBox { Width = 160, VerticalAlignment = VerticalAlignment.Center };
		configCombo.SelectionChanged += (_, _) => {
			if (suppressConfig || configCombo.SelectedIndex < 0)
				return;
			SelConfig = (string)configCombo.SelectedItem!;
			SelPlatform = configPlatforms.TryGetValue (SelConfig, out var p) ? p : "Any CPU";
			RefreshHeader ();
			// Legacy OnConfigChanged -> LoadConfigData on every MultiConfig panel.
			foreach (var p2 in panels)
				p2.OnConfigChanged (SelConfig, SelPlatform);
		};
		configRow.Children.Add (configCombo);

		tabs = new StackPanel {
			Orientation = Orientation.Horizontal,
			Spacing = 2,
			Margin = new Thickness (12, 8, 12, 0),
		};

		body = new StackPanel { Margin = new Thickness (16, 8, 16, 8), Spacing = 6 };
		emptyText = new TextBlock {
			FontSize = 12,
			Margin = new Thickness (16, 24, 16, 0),
			TextWrapping = TextWrapping.Wrap,
		};
		emptyText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		var bodyBorder = new Border {
			Child = body,
			Margin = new Thickness (12, 0, 12, 0),
			BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (1),
			CornerRadius = new CornerRadius (3),
		};

		okButton = new Button { Content = "OK", MinWidth = 80, Classes = { "chromebtn" } };
		okButton.Click += (_, _) => Apply ();
		var cancel = new Button { Content = "Cancel", MinWidth = 80, Classes = { "chromebtn" } };
		cancel.Click += (_, _) => Close ();
		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8,
			Margin = new Thickness (12, 12, 12, 12),
		};
		buttons.Children.Add (okButton);
		buttons.Children.Add (cancel);

		var root = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,Auto,Auto,*,Auto") };
		Grid.SetRow (headerText, 0);
		root.Children.Add (headerText);
		Grid.SetRow (configRow, 1);
		root.Children.Add (configRow);
		Grid.SetRow (tabs, 2);
		root.Children.Add (tabs);
		Grid.SetRow (bodyBorder, 3);
		root.Children.Add (bodyBorder);
		var bottom = new Panel { Children = { buttons } };
		Grid.SetRow (bottom, 4);
		root.Children.Add (bottom);

		Content = root;

		BuildTabs ();
		PopulateConfigSelector ();
		foreach (var p in panels)
			p.Load ();
		RefreshHeader ();
		RefreshBody ();
	}

	// ---------- tabs ----------

	void BuildTabs ()
	{
		if (registry is null || projectPath is null)
			return;
		foreach (var node in registry.GetExtensionNodesUnder (ItemOptionPanelsPoint)) {
			if (!EvaluateCondition (node, kind))
				continue;
			if (!PortedPanelClasses.Contains (node.ClassName ?? ""))
				continue; // registered but not ported yet (later phases)
			var label = string.IsNullOrEmpty (node.Label) ? node.Id : node.Label;
			PanelBase panel = node.ClassName switch {
				"MonoDevelop.VBNetBinding.ProjectOptionsPanel" => new VbGeneralPanel (this, projectPath, label),
				"MonoDevelop.VBNetBinding.ImportOptionsPanel" => new VbImportsPanel (this, projectPath, label),
				"MonoDevelop.VBNetBinding.ConfigurationOptionsPanel" => new VbCompilerPanel (this, projectPath, label),
				_ => null!,
			};
			panels.Add (panel);
			var p = panel;
			var btn = new ToggleButton {
				Content = label,
				FontSize = 12,
				Padding = new Thickness (10, 4),
				Tag = p,
				IsChecked = panels.Count == 1,
			};
			btn.Click += (_, _) => SelectPanel (p);
			tabs.Children.Add (btn);
			if (panels.Count == 1)
				SelectPanel (p);
		}
		okButton.IsEnabled = panels.Count > 0;
	}

	void SelectPanel (PanelBase p)
	{
		suppressConfig = true;
		foreach (var t in tabs.Children.OfType<ToggleButton> ())
			t.IsChecked = ReferenceEquals (t.Tag, p);
		suppressConfig = false;
		RefreshBody (p);
	}

	void RefreshBody (PanelBase? selected = null)
	{
		body.Children.Clear ();
		if (panels.Count == 0) {
			emptyText.Text = projectPath is null
				? "No active project: open a solution to edit the options of its active project."
				: "This project has no ported options panels yet.";
			body.Children.Add (emptyText);
			return;
		}
		var current = selected ?? panels.FirstOrDefault (p =>
			tabs.Children.OfType<ToggleButton> ().Any (t => t.IsChecked == true && ReferenceEquals (t.Tag, p)))
			?? panels [0];
		body.Children.Add (current.Body);
	}

	void PopulateConfigSelector ()
	{
		configPlatforms.Clear ();
		var configs = new List<string> ();
		if (!string.IsNullOrEmpty (solutionPath) && File.Exists (solutionPath))
			configs = ConfigurationService.GetSolutionConfigurations (solutionPath).ToList ();
		if (configs.Count == 0)
			configs.Add ("Debug");
		suppressConfig = true;
		configCombo.Items.Clear ();
		foreach (var c in configs) {
			configPlatforms [c] = PlatformFor (c);
			configCombo.Items.Add (c);
		}
		var active = ConfigurationService.GetActiveConfiguration (solutionPath ?? "");
		var idx = configs.IndexOf (active);
		configCombo.SelectedIndex = idx >= 0 ? idx : 0;
		suppressConfig = false;
		SelConfig = (string)configCombo.SelectedItem!;
		SelPlatform = configPlatforms [SelConfig];
	}

	string PlatformFor (string config)
	{
		if (string.IsNullOrEmpty (solutionPath) || projectPath is null)
			return "Any CPU";
		return ConfigurationService.GetProjectConfigurationFor (solutionPath, config, projectPath).Platform;
	}

	void RefreshHeader ()
	{
		if (projectPath is null) {
			headerText.Text = "Project: (none) — no active project";
			return;
		}
		headerText.Text = "Project: " + Path.GetFileName (projectPath) +
			"  —  Configuration: " + (SelConfig.Length > 0 ? SelConfig + "|" + SelPlatform : "(none)");
	}

	void Apply ()
	{
		foreach (var p in panels) {
			var ok = p.Store ();
			MainWindow.Instance?.Output ("[projopts] " + p.Label + " stored: " + (ok ? "ok" : "FAIL"));
		}
		Close ();
	}

	// ---------- condition evaluation ----------

	// The legacy ItemOptionsDialog registers the conditions ItemType /
	// ActiveLanguage / FlavorType / ProjectTypeId / SupportsTarget / AppliesTo and
	// evaluates the node's <Condition> against them. ConditionParser (the legacy
	// MSBuild condition evaluator) is internal to MonoDevelop.Core and not reachable
	// from the shell, so the clause set is evaluated directly here over the
	// structural decomposition P0 added to AddonExtensionNode (GetConditionParts).
	// Keys the shipped manifests use: ProjectTypeId, FlavorType, ItemType, Platform,
	// and bare capability keys (which a flat classic project does not carry).
	static bool EvaluateCondition (AddonExtensionNode node, ConfigurationService.ProjectKindInfo? kind)
	{
		if (string.IsNullOrWhiteSpace (node.Condition))
			return true; // no condition: the node applies unconditionally
		foreach (var (key, value) in node.GetConditionParts ()) {
			if (key.StartsWith ("NOT ", StringComparison.OrdinalIgnoreCase)) {
				if (EvaluateClause (key.Substring (4).Trim (), value, kind))
					return false;
				continue;
			}
			if (!EvaluateClause (key, value, kind))
				return false;
		}
		return true;
	}

	static bool EvaluateClause (string key, string? value, ConfigurationService.ProjectKindInfo? kind)
	{
		// A value is a "|" list of alternatives (OR); each alternative may carry a
		// leading "!" (negation).
		foreach (var alt in (value ?? "").Split ('|')) {
			var a = alt.Trim ();
			if (a.Length == 0)
				continue;
			var neg = a.StartsWith ("!");
			if (a.StartsWith ("!"))
				a = a.Substring (1).Trim ();
			var hit = EvaluateValue (key, a, kind);
			if (hit != neg)
				return true;
		}
		return false;
	}

	static bool EvaluateValue (string key, string value, ConfigurationService.ProjectKindInfo? kind)
	{
		switch (key) {
		case "ProjectTypeId": {
			// Legacy ProjectTypeIdCondition: the tag list is AND of "&" tags (here a
			// single tag), each a type alias (VBNet / C# / F# / ILAsmProject) or a
			// project type GUID matched against the project's type/flavor GUIDs.
			foreach (var tag in value.Split ('&')) {
				var t = tag.Trim ();
				switch (t) {
				case "VBNet":
					if (kind?.IsVBNet != true)
						return false;
					break;
				case "C#":
					if (kind?.IsCSharp != true)
						return false;
					break;
				case "F#":
					if (kind?.IsFSharp != true)
						return false;
					break;
				case "ILAsmProject":
					if (kind?.IsILAsm != true)
						return false;
					break;
				default:
					// Unknown alias/GUID: the legacy tags set does not contain it.
					return false;
				}
			}
			return true;
		}
		case "FlavorType":
			// Legacy FlavorTypeCondition: the value is a flavor type full name; only
			// the ASP.NET app flavor is detectable from the classic .csproj signals
			// (the AspNetAppFlavorGuid in ProjectTypeGuids).
			return value == "MonoDevelop.AspNet.Projects.AspNetAppProjectFlavor" && kind?.IsAspNetApp == true;
		case "ItemType":
			// Legacy ItemTypeCondition: the type of the dialog data object. The
			// dialog always holds a Project, so "Project" holds; "DotNetProject"
			// holds for the classic .csproj/.fsproj/.vbproj/.ilproj set; anything
			// else (Solution, PackagingProject, ...) does not.
			return value switch {
				"Project" => true,
				"DotNetProject" => kind?.IsDotNetProject == true,
				_ => false,
			};
		case "Platform":
			// The RUNTIME platform the add-in system matches against ("windows",
			// "unix"): a manifest condition like Platform=!windows holds on Linux.
			return value switch {
				"windows" => RuntimeInformation.IsOSPlatform (OSPlatform.Windows),
				"unix" => !RuntimeInformation.IsOSPlatform (OSPlatform.Windows)
					&& !RuntimeInformation.IsOSPlatform (OSPlatform.OSX),
				_ => false,
			};
		default:
			// Bare capability keys (AspNetCoreSdkInstalled, UseNetCore31,
			// ProjectHasNuGetMetadata, ...): legacy projects without the corresponding
			// model capabilities do not hold them.
			return false;
		}
	}

	// ---------- QA oracle ----------

	/// <summary>QA oracle for the P1 port (run with --sln=&lt;fixture.sln&gt; holding a
	/// VB project with Debug/Release solution configurations): asserts the project
	/// kind, the tab set, the configuration selector, and the read -&gt; edit -&gt;
	/// write -&gt; re-read round-trip of the three VB panels (General over the
	/// top-level PropertyGroup, Imports over the top-level ItemGroup, Compiler over
	/// the COMBINED per-config condition), plus the empty state on a non-VB
	/// project. Restores the fixture afterwards.</summary>
	public bool QaVbOptionsOracle ()
	{
		var mw = MainWindow.Instance;
		int ok = 0, fail = 0;
		void Check (string what, bool cond)
		{
			mw?.Output ("[qa-vboptions] " + (cond ? "ok" : "FAIL") + " " + what);
			if (cond) ok++; else fail++;
		}
		void Log (string line) => mw?.Output ("[qa-vboptions] " + line);

		if (projectPath is null || !File.Exists (projectPath)) {
			Log ("FAIL no project (pass --sln=<fixture.sln> with a .vbproj)");
			return false;
		}
		Check ("kind VB", kind?.IsVBNet == true);
		Check ("tabs=3", panels.Count == 3);
		Log ("tabs='" + string.Join (",", panels.Select (p => p.Label)) + "'");
		Check ("config selector", SelConfig.Length > 0 && SelPlatform.Length > 0);
		Log ("config=" + SelConfig + " platform=" + SelPlatform);

		var general = panels.OfType<VbGeneralPanel> ().FirstOrDefault ();
		var imports = panels.OfType<VbImportsPanel> ().FirstOrDefault ();
		var compiler = panels.OfType<VbCompilerPanel> ().FirstOrDefault ();
		Check ("panels present", general is not null && imports is not null && compiler is not null);
		if (general is null || imports is null || compiler is null) {
			Log ("total ok=" + ok + " fail=" + fail);
			return false;
		}

		var original = File.ReadAllText (projectPath);
		string? Cond (string cfg, string plat) => " '$(Configuration)|$(Platform)' == '" + cfg + "|" +
			(plat == "Any CPU" ? "AnyCPU" : plat) + "' ";
		XElement? Group (string cfg, string plat)
		{
			try {
				var doc = XDocument.Load (projectPath);
				var cond = Cond (cfg, plat);
				return doc.Root?.Elements ().FirstOrDefault (e => e.Name.LocalName == "PropertyGroup"
					&& (string?)e.Attribute ("Condition") == cond);
			} catch {
				return null;
			}
		}
		string? Val (XElement? g, string name) =>
			g?.Elements ().FirstOrDefault (e => e.Name.LocalName == name)?.Value;

		try {
			// ----- General panel round-trip (top-level PropertyGroup) -----
			Log ("general loaded: target='" + general.TargetCombo.Text + "' mainclass='" + general.MainClass.Text
				+ "' mytype='" + general.MyTypeCombo.Text + "' explicit='" + general.ExplicitCombo.Text
				+ "' strict='" + general.StrictCombo.Text + "' compare='" + general.CompareCombo.Text
				+ "' infer='" + general.InferCombo.Text + "' codepage='" + general.CodePageCombo.Text + "'");
			general.TargetCombo.SelectedIndex = 2;            // Executable with GUI -> WinExe (non-default)
			general.MainClass.Text = "QaVb.Program";          // non-default
			general.MyTypeCombo.Text = "Windows";             // non-default (default is empty)
			general.ExplicitCombo.SelectedIndex = 1;          // Off (default is On -> non-default)
			general.StrictCombo.SelectedIndex = 0;            // On (non-default)
			general.CompareCombo.SelectedIndex = 1;           // Text (non-default)
			general.InferCombo.SelectedIndex = 1;             // Off (the default -> suppressed)
			general.CodePageCombo.Text = "1252";              // non-default
			Check ("general store", general.Store ());
			var f1 = ConfigurationService.ReadVbProjectFields (projectPath);
			Check ("general rt target", f1?.CompileTarget == "WinExe");
			Check ("general rt mainclass", f1?.StartupObject == "QaVb.Program");
			Check ("general rt mytype", f1?.MyType == "Windows");
			Check ("general rt explicit", f1?.OptionExplicit == "Off");
			Check ("general rt strict", f1?.OptionStrict == "On");
			Check ("general rt compare", f1?.OptionCompare == "Text");
			Check ("general rt infer", f1?.OptionInfer == "Off");
			Check ("general rt codepage", f1?.CodePage == "1252");
			// Default suppression: only the non-default elements exist in the file.
			var root = XDocument.Load (projectPath).Root!;
			bool HasEl (string name) => root.Elements ().Any (e => e.Elements ().Any (x => x.Name.LocalName == name));
			Check ("general keep outputtype", HasEl ("OutputType"));
			Check ("general keep optionexplicit (non-default)", HasEl ("OptionExplicit"));
			Check ("general suppress infer (Off is the default)", !HasEl ("OptionInfer"));
			// Restore defaults -> every non-default element is removed again.
			general.TargetCombo.SelectedIndex = 1;            // Library (the default)
			general.MainClass.Text = "";
			general.MyTypeCombo.SelectedIndex = -1;           // -> empty text (the default)
			general.ExplicitCombo.SelectedIndex = 0;          // On (the default)
			general.StrictCombo.SelectedIndex = 1;            // Off (the default)
			general.CompareCombo.SelectedIndex = 0;           // Binary (the default)
			general.InferCombo.SelectedIndex = 1;             // Off (the default)
			general.CodePageCombo.Text = "";
			Check ("general store defaults", general.Store ());
			var f2 = ConfigurationService.ReadVbProjectFields (projectPath);
			Check ("general rt defaults target", f2?.CompileTarget == "Library");
			Check ("general rt defaults strict", f2?.OptionStrict == "Off");
			var root2 = XDocument.Load (projectPath).Root!;
			bool HasEl2 (string name) => root2.Elements ().Any (e => e.Elements ().Any (x => x.Name.LocalName == name));
			Check ("general suppress outputtype (back to default)", !HasEl2 ("OutputType"));
			Check ("general suppress mytype (back to default)", !HasEl2 ("MyType"));
			Check ("general suppress optionexplicit (back to default)", !HasEl2 ("OptionExplicit"));
			Check ("general suppress codepage (back to default)", !HasEl2 ("CodePage"));

			// ----- Imports panel round-trip (top-level ItemGroup) -----
			// The fixture starts with two imports; the round-trip works from that state.
			Log ("imports loaded: count=" + imports.Items.Count + " list='" + string.Join (",", imports.Items) + "'");
			Check ("imports loaded state", imports.Items.Count == 2
				&& imports.Items [0] == "System" && imports.Items [1] == "System.Collections.Generic");
			imports.Entry.Text = "System.Text";
			imports.CmdAdd ();
			imports.Entry.Text = "System.Text";
			imports.CmdAdd (); // legacy: duplicates are ignored
			Check ("imports add+dedupe", imports.Items.Count == 3 && imports.Items [2] == "System.Text");
			imports.List.SelectedIndex = 2;
			imports.CmdRemove ();
			Check ("imports remove", imports.Items.Count == 2 && imports.Items [1] == "System.Collections.Generic");
			imports.Entry.Text = "System.Linq";
			imports.CmdAdd ();
			Check ("imports store", imports.Store ());
			var imps = ConfigurationService.ReadVbImports (projectPath);
			Check ("imports rt", imps.Count == 3 && imps [0] == "System"
				&& imps [1] == "System.Collections.Generic" && imps [2] == "System.Linq");
			// Empty the list: the writer must remove the Import items without touching
			// the other ItemGroups (the P0 H1 regression, exercised through the UI path).
			while (imports.Items.Count > 0) {
				imports.List.SelectedIndex = 0;
				imports.CmdRemove ();
			}
			Check ("imports store empty", imports.Store ());
			Check ("imports rt empty", ConfigurationService.ReadVbImports (projectPath).Count == 0);
			Check ("imports empty keeps other itemgroups", File.ReadAllText (projectPath).Contains ("<Compile Include=\"Module1.vb\" />"));

			// ----- Compiler panel round-trip (COMBINED per-config condition) -----
			Log ("compiler loaded: optimize='" + compiler.OptimizeCombo.Text + "' debug='" + compiler.DebugTypeCombo.Text
				+ "' trace='" + compiler.DefineTraceCombo.Text + "' warnings='" + compiler.WarnCombo.Text
				+ "' nowarn='" + compiler.DontWarn.Text + "' aserrors='" + compiler.TreatAsError.Text
				+ "' xmldoc='" + compiler.XmlDocCombo.Text + "' intchecks='" + compiler.RemoveIntChecksCombo.Text
				+ "' constants='" + compiler.DefineConsts.Text + "' additional='" + compiler.Additional.Text + "'");
			compiler.OptimizeCombo.SelectedIndex = 0;         // Yes (non-default)
			compiler.DebugTypeCombo.SelectedIndex = 1;        // pdbonly (non-default)
			compiler.WarnCombo.SelectedIndex = 1;             // No -> WarningLevel 0
			compiler.DontWarn.Text = "1591";
			compiler.XmlDocCombo.SelectedIndex = 0;           // Yes -> <name>.xml
			compiler.DefineConsts.Text = "DEBUG;TRACE";
			compiler.Additional.Text = "/nostdlib";
			Check ("compiler store", compiler.Store ());
			var g1 = Group (SelConfig, SelPlatform);
			Check ("compiler group combined condition", g1 is not null
				&& (string?)g1.Attribute ("Condition") == Cond (SelConfig, SelPlatform));
			Check ("compiler rt optimize", Val (g1, "Optimize") == "true");
			Check ("compiler rt debugtype", Val (g1, "DebugType") == "pdbonly");
			Check ("compiler rt warninglevel 0", Val (g1, "WarningLevel") == "0");
			Check ("compiler rt nowarn", Val (g1, "NoWarn") == "1591");
			Check ("compiler rt xmldoc name.xml", Val (g1, "DocumentationFile")
				== Path.GetFileNameWithoutExtension (projectPath) + ".xml");
			Check ("compiler rt constants", Val (g1, "DefineConstants") == "DEBUG;TRACE");
			Check ("compiler rt additional", Val (g1, "AdditionalParameters") == "/nostdlib");
			var back = ConfigurationService.ReadVbCompilerFields (projectPath, SelConfig, SelPlatform);
			Check ("compiler re-read api", back is not null && back.Optimize
				&& back.DebugType == "pdbonly" && back.WarningLevel == 0 && back.NoWarn == "1591");

			// A second configuration: the Release group must land under its own
			// combined condition (the legacy writer's shape, one group per config).
			var hasRelease = configPlatforms.ContainsKey ("Release");
			Check ("fixture has Release", hasRelease);
			if (hasRelease) {
				var releaseIdx = configCombo.Items.Cast<object> ().ToList ().IndexOf ("Release");
				configCombo.SelectedIndex = releaseIdx; // fires OnConfigChanged (suppression off)
				compiler.OnConfigChanged (SelConfig, SelPlatform);
				compiler.OptimizeCombo.SelectedIndex = 1;     // back to defaults
				compiler.DebugTypeCombo.SelectedIndex = 2;     // full (default)
				compiler.WarnCombo.SelectedIndex = 0;          // Yes -> WarningLevel 1
				compiler.DontWarn.Text = "";
				compiler.XmlDocCombo.SelectedIndex = 1;
				compiler.DefineConsts.Text = "";
				compiler.Additional.Text = "";
				Check ("compiler store Release", compiler.Store ());
				var g2 = Group ("Release", configPlatforms ["Release"]);
				Check ("compiler release combined condition", g2 is not null
					&& (string?)g2.Attribute ("Condition") == Cond ("Release", configPlatforms ["Release"]));
				Check ("compiler release defaults", Val (g2, "Optimize") is null && Val (g2, "DebugType") is null
					&& Val (g2, "WarningLevel") == "1");
			}
		} finally {
			if (File.Exists (projectPath))
				File.WriteAllText (projectPath, original, new System.Text.UTF8Encoding (false));
			Log ("restored original " + Path.GetFileName (projectPath));
		}

		// ----- Empty state: a non-VB project gets no tabs -----
		try {
			var tmp = Path.Combine (Path.GetTempPath (), "qa_vboptions_" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (tmp);
			var csproj = Path.Combine (tmp, "Empty.csproj");
			File.WriteAllText (csproj, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n");
			var dlg2 = new ProjectOptionsDialog (csproj, null);
			Check ("csharp kind", dlg2.Kind?.IsCSharp == true && dlg2.Kind?.IsVBNet != true);
			Check ("csharp 0 tabs", dlg2.Panels.Count == 0);
			dlg2.Close ();
			Directory.Delete (tmp, true);
		} catch (Exception ex) {
			Check ("csharp empty state (exception: " + ex.Message + ")", false);
		}

		Log ("total ok=" + ok + " fail=" + fail);
		return fail == 0;
	}

	// ---------- panel base ----------

	// The ported item options panels share the legacy IOptionsPanel lifecycle: the
	// dialog loads them for the current target (Load) and applies them on OK
	// (Store). Per-config panels additionally re-load when the configuration
	// selection changes (legacy MultiConfigItemOptionsPanel OnConfigChanged).
	public class PanelBase
	{
		protected readonly ProjectOptionsDialog Dialog;
		public string ProjectPath { get; }
		public string Label { get; }
		public Control Body { get; }

		public PanelBase (ProjectOptionsDialog dialog, string projectPath, string label, Control body)
		{
			Dialog = dialog;
			ProjectPath = projectPath;
			Label = label;
			Body = body;
		}

		public virtual void Load () { }
		public virtual void OnConfigChanged (string config, string platform) { }
		public virtual bool Store () => true;

		// ---------- shared layout helpers ----------

		protected static TextBlock L (string text) => new () {
			Text = text,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness (0, 0, 8, 0),
			FontSize = 12,
		};

		protected static Grid Row (TextBlock label, Control input)
		{
			var g = new Grid {
				ColumnDefinitions = ColumnDefinitions.Parse ("150,*"),
				Margin = new Thickness (0, 3, 0, 3),
			};
			g.Children.Add (label);
			Grid.SetColumn (input, 1);
			g.Children.Add (input);
			return g;
		}

		protected static Border Section (string title)
		{
			var b = new Border {
				Margin = new Thickness (0, 10, 0, 4),
				BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
				BorderThickness = new Thickness (0, 0, 0, 1),
				Child = new TextBlock {
					Text = title,
					FontWeight = FontWeight.SemiBold,
					FontSize = 12,
				},
			};
			return b;
		}

		protected static ComboBox YN (int selected)
		{
			var c = new ComboBox { Width = 90, HorizontalAlignment = HorizontalAlignment.Left };
			c.Items.Add ("Yes");
			c.Items.Add ("No");
			c.SelectedIndex = selected;
			return c;
		}
	}

	// ---------- General panel (legacy VBNetBinding.ProjectOptionsPanel) ----------

	// Project-level options of the .vbproj top-level PropertyGroup: the same rows,
	// labels and combos as the legacy ProjectOptionsPanelWidget (Compile Target /
	// Main Class / Win32 Icon / Compiler Code Page / My Type / Option Explicit /
	// Option Strict / Option Compare / Option Infer). Main Class is insensitive for
	// Library/Module targets, exactly like the legacy combo's Changed handler.
	class VbGeneralPanel : PanelBase
	{
		// Legacy combo order: Executable, Library, Executable with GUI, Module.
		static readonly string[] TargetNames = { "Executable", "Library", "Executable with GUI", "Module" };
		// The legacy model persists CompileTarget as the OutputType element (enum
		// names); the read normalizes an absent OutputType to Library.
		static readonly string[] TargetOutputTypes = { "Exe", "Library", "WinExe", "Module" };

		public ComboBox TargetCombo { get; }
		public TextBox MainClass { get; }
		public TextBox Icon { get; }
		public ComboBox CodePageCombo { get; }
		public ComboBox MyTypeCombo { get; }
		public ComboBox ExplicitCombo { get; }
		public ComboBox StrictCombo { get; }
		public ComboBox CompareCombo { get; }
		public ComboBox InferCombo { get; }

		public VbGeneralPanel (ProjectOptionsDialog dialog, string projectPath, string label)
			: base (dialog, projectPath, label, BuildBody (dialog.StorageProvider, out var target, out var mainClass,
				out var icon, out var codePage, out var myType, out var explicitC, out var strict, out var compare,
				out var infer))
		{
			TargetCombo = target;
			MainClass = mainClass;
			Icon = icon;
			CodePageCombo = codePage;
			MyTypeCombo = myType;
			ExplicitCombo = explicitC;
			StrictCombo = strict;
			CompareCombo = compare;
			InferCombo = infer;
			// Legacy compileTargetCombo.Changed: the Main Class entry is sensitive
			// except for the Library and Module targets.
			TargetCombo.SelectionChanged += (_, _) => RefreshMainClassState ();
		}

		static StackPanel BuildBody (Avalonia.Platform.Storage.IStorageProvider storage,
			out ComboBox target, out TextBox mainClass, out TextBox icon,
			out ComboBox codePage, out ComboBox myType, out ComboBox explicitC, out ComboBox strict,
			out ComboBox compare, out ComboBox infer)
		{
			target = Combo ("Executable", "Library", "Executable with GUI", "Module");
			mainClass = new TextBox ();
			icon = new TextBox ();
			var iconBox = icon; // the lambda below cannot capture the out parameter directly
			codePage = new ComboBox { IsEditable = true };
			foreach (var id in CodePageIds)
				codePage.Items.Add (id);
			myType = new ComboBox { IsEditable = true };
			myType.Items.Add ("WindowsForms");
			myType.Items.Add ("Windows");
			myType.Items.Add ("Console");
			explicitC = Combo ("On", "Off");
			strict = Combo ("On", "Off");
			compare = Combo ("Binary", "Text");
			infer = Combo ("On", "Off");

			var iconRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			iconRow.Children.Add (iconBox);
			var browse = new Button { Content = "Browse…", MinWidth = 70, Classes = { "chromebtn" } };
			browse.Click += async (_, _) => {
				var files = await storage.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
					Title = "Win32 Icon",
					AllowMultiple = false,
					FileTypeFilter = new[] {
						new Avalonia.Platform.Storage.FilePickerFileType ("Icons") { Patterns = new[] { "*.ico" } },
						new Avalonia.Platform.Storage.FilePickerFileType ("All files") { Patterns = new[] { "*.*" } },
					},
				});
				if (files.Count > 0)
					iconBox.Text = files [0].Path.LocalPath;
			};
			iconRow.Children.Add (browse);

			var b = new StackPanel { Spacing = 0 };
			b.Children.Add (Row (L ("Compile Target:"), target));
			b.Children.Add (Row (L ("Main Class:"), mainClass));
			b.Children.Add (Row (L ("Win32 Icon:"), iconRow));
			b.Children.Add (Row (L ("Compiler Code Page:"), codePage));
			b.Children.Add (Row (L ("My Type:"), myType));
			b.Children.Add (Row (L ("Option Explicit:"), explicitC));
			b.Children.Add (Row (L ("Option Strict:"), strict));
			b.Children.Add (Row (L ("Option Compare:"), compare));
			b.Children.Add (Row (L ("Option Infer:"), infer));
			return b;
		}

		static ComboBox Combo (params string[] items)
		{
			var c = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
			foreach (var s in items)
				c.Items.Add (s);
			return c;
		}

		public override void Load ()
		{
			var f = ConfigurationService.ReadVbProjectFields (ProjectPath);
			if (f is null)
				return;
			TargetCombo.SelectedIndex = Math.Max (0, Array.IndexOf (TargetOutputTypes, f.CompileTarget));
			MainClass.Text = f.StartupObject;
			Icon.Text = f.ApplicationIcon;
			CodePageCombo.Text = f.CodePage;
			// Legacy: the three known My Types select in place, an empty value leaves
			// the combo unselected, any other value appends itself as a fourth item.
			var mi = Array.IndexOf (new[] { "WindowsForms", "Windows", "Console" }, f.MyType);
			if (mi >= 0)
				MyTypeCombo.SelectedIndex = mi;
			else if (f.MyType.Length == 0)
				MyTypeCombo.SelectedIndex = -1;
			else {
				MyTypeCombo.Items.Add (f.MyType);
				MyTypeCombo.SelectedIndex = MyTypeCombo.Items.Count - 1;
			}
			ExplicitCombo.SelectedIndex = f.OptionExplicit == "On" ? 0 : 1;
			StrictCombo.SelectedIndex = f.OptionStrict == "On" ? 0 : 1;
			CompareCombo.SelectedIndex = f.OptionCompare == "Binary" ? 0 : 1;
			InferCombo.SelectedIndex = f.OptionInfer == "On" ? 0 : 1;
			RefreshMainClassState ();
		}

		void RefreshMainClassState ()
		{
			// Legacy: entryMainClass.Sensitive = target != Library && target != Module.
			var idx = TargetCombo.SelectedIndex;
			MainClass.IsEnabled = idx != (int)1 && idx != (int)3;
		}

		string Sel (ComboBox c) => c.SelectedIndex >= 0 ? (string)c.SelectedItem! : "";

		public override bool Store ()
		{
			// The legacy StorePanelContents maps the combos back onto the VBProject
			// ItemProperties; SaveVbProjectFields applies the default suppression
			// (elements removed when back to their legacy default).
			var idx = Math.Clamp (TargetCombo.SelectedIndex, 0, TargetOutputTypes.Length - 1);
			return ConfigurationService.SaveVbProjectFields (ProjectPath, new ConfigurationService.VBProjectFields (
				Sel (InferCombo), Sel (ExplicitCombo), Sel (CompareCombo), Sel (StrictCombo),
				MyTypeCombo.Text, MainClass.Text, Icon.Text, CodePageCombo.Text,
				TargetOutputTypes [idx]));
		}
	}

	// ---------- Imports panel (legacy VBNetBinding.ImportOptionsPanel) ----------

	// The .vbproj top-level <Import Include="..."/> items: a list with an Add
	// (deduplicating) / Remove button pair, exactly the legacy widget (which has no
	// Clear). The legacy widget sorts the display column ascending; the port keeps
	// the insertion order (the store order, the value-compatible one) — documented
	// micro-divergence.
	class VbImportsPanel : PanelBase
	{
		readonly List<string> items = new ();
		public ListBox List { get; }
		public TextBox Entry { get; }
		readonly Button addButton;
		readonly Button removeButton;
		public IReadOnlyList<string> Items => items;

		public VbImportsPanel (ProjectOptionsDialog dialog, string projectPath, string label)
			: base (dialog, projectPath, label, BuildBody (out var list, out var entry, out var add, out var remove))
		{
			List = list;
			Entry = entry;
			addButton = add;
			removeButton = remove;
			// Legacy: Add is enabled iff the entry has text; Remove iff a row is
			// selected.
			Entry.TextChanged += (_, _) => addButton.IsEnabled = !string.IsNullOrEmpty (Entry.Text);
			List.SelectionChanged += (_, _) => removeButton.IsEnabled = List.SelectedIndex >= 0;
			add.Click += (_, _) => CmdAdd ();
			remove.Click += (_, _) => CmdRemove ();
		}

		static StackPanel BuildBody (out ListBox list, out TextBox entry, out Button add, out Button remove)
		{
			list = new ListBox { Height = 260 };
			var listBorder = new Border {
				Child = list,
				BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
				BorderThickness = new Thickness (1),
				CornerRadius = new CornerRadius (3),
			};
			entry = new TextBox ();
			add = new Button { Content = "Add", MinWidth = 80, Classes = { "chromebtn" }, IsEnabled = false };
			remove = new Button { Content = "Remove", MinWidth = 80, Classes = { "chromebtn" }, IsEnabled = false };
			var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			btns.Children.Add (add);
			btns.Children.Add (remove);
			var row = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto") };
			row.Children.Add (entry);
			Grid.SetColumn (btns, 1);
			row.Children.Add (btns);
			var b = new StackPanel { Spacing = 6 };
			b.Children.Add (listBorder);
			b.Children.Add (row);
			return b;
		}

		public override void Load ()
		{
			items.Clear ();
			items.AddRange (ConfigurationService.ReadVbImports (ProjectPath));
			RefreshList ();
		}

		void RefreshList ()
		{
			List.ItemsSource = items;
		}

		public void CmdAdd ()
		{
			// Legacy OnCmdAddClicked: the entry text is appended verbatim when it is
			// not already present.
			var text = Entry.Text;
			if (text.Length == 0 || items.Contains (text))
				return;
			items.Add (text);
			RefreshList ();
		}

		public void CmdRemove ()
		{
			// Legacy OnCmdRemoveClicked: the selected rows are removed by value. The
			// selection is captured and cleared BEFORE mutating the list: the items
			// list is not a collection Avalonia watches, so the selection model keeps
			// its committed indices and would enumerate a stale index against the
			// shrunken source on the next selection change (ArgumentOutOfRange).
			var selected = List.SelectedItems.OfType<string> ().ToList ();
			List.SelectedIndex = -1;
			foreach (var sel in selected)
				items.Remove (sel);
			RefreshList ();
		}

		public override bool Store ()
		{
			return ConfigurationService.SaveVbImports (ProjectPath, items.ToList ());
		}
	}

	// ---------- Compiler panel (legacy VBNetBinding.ConfigurationOptionsPanel) ----------

	// Per-configuration VB compiler options: the three legacy sections (Optimization/
	// Debug options, Warnings, Misc) with the exact legacy labels and combos. The
	// legacy StorePanelContents maps the controls onto VBCompilerParameters:
	//   Optimize/DefineDebug/DefineTrace/RemoveIntegerChecks  combo Active == 0
	//   WarningsDisabled                                     Enable Warnings Active == 1
	//   DocumentationFile                                      Generate XML Active == 0 ? <projectName>.xml : ""
	//   DebugType                                            combo text (None/pdbonly/full)
	//   NoWarn/DefineConstants/WarningsAsErrors/             the four entries
	//   AdditionalParameters
	// The TreatWarningsAsErrors bool has no legacy control: the loaded value is
	// preserved through the store.
	class VbCompilerPanel : PanelBase
	{
		public ComboBox OptimizeCombo { get; }
		public ComboBox DefineDebugCombo { get; }
		public ComboBox DefineTraceCombo { get; }
		public ComboBox DebugTypeCombo { get; }
		public ComboBox WarnCombo { get; }
		public TextBox DontWarn { get; }
		public TextBox TreatAsError { get; }
		public ComboBox XmlDocCombo { get; }
		public ComboBox RemoveIntChecksCombo { get; }
		public TextBox DefineConsts { get; }
		public TextBox Additional { get; }

		// Legacy DebugType combo order: None, pdbonly, full.
		static readonly string[] DebugTypes = { "none", "pdbonly", "full" };

		bool loadedTreatAsErrors;

		public VbCompilerPanel (ProjectOptionsDialog dialog, string projectPath, string label)
			: base (dialog, projectPath, label, BuildBody (out var optimize, out var dbg, out var trace,
				out var debugType, out var warn, out var dontWarn, out var treatAsError, out var xmlDoc,
				out var intChecks, out var defineConsts, out var additional))
		{
			OptimizeCombo = optimize;
			DefineDebugCombo = dbg;
			DefineTraceCombo = trace;
			DebugTypeCombo = debugType;
			WarnCombo = warn;
			DontWarn = dontWarn;
			TreatAsError = treatAsError;
			XmlDocCombo = xmlDoc;
			RemoveIntChecksCombo = intChecks;
			DefineConsts = defineConsts;
			Additional = additional;
		}

		static StackPanel BuildBody (out ComboBox optimize, out ComboBox dbg, out ComboBox trace,
			out ComboBox debugType, out ComboBox warn, out TextBox dontWarn, out TextBox treatAsError,
			out ComboBox xmlDoc, out ComboBox intChecks, out TextBox defineConsts, out TextBox additional)
		{
			optimize = YN (1);
			dbg = YN (1);
			trace = YN (1);
			debugType = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
			foreach (var s in new[] { "None", "pdbonly", "full" })
				debugType.Items.Add (s);
			debugType.SelectedIndex = 2;
			warn = YN (0);
			dontWarn = new TextBox ();
			treatAsError = new TextBox ();
			xmlDoc = YN (1);
			intChecks = YN (1);
			defineConsts = new TextBox ();
			additional = new TextBox ();

			var b = new StackPanel { Spacing = 0 };
			b.Children.Add (Section ("Optimization/Debug options"));
			b.Children.Add (Row (L ("Optimize:"), optimize));
			b.Children.Add (Row (L ("Define DEBUG:"), dbg));
			b.Children.Add (Row (L ("Define TRACE:"), trace));
			b.Children.Add (Row (L ("Debug Type:"), debugType));
			b.Children.Add (Section ("Warnings"));
			b.Children.Add (Row (L ("Enable Warnings:"), warn));
			b.Children.Add (Row (L ("Don't warn about:"), dontWarn));
			b.Children.Add (Row (L ("Treat as error:"), treatAsError));
			b.Children.Add (Section ("Misc"));
			b.Children.Add (Row (L ("Generate XML documentation:"), xmlDoc));
			b.Children.Add (Row (L ("Remove integer checks:"), intChecks));
			b.Children.Add (Row (L ("Define constants:"), defineConsts));
			b.Children.Add (Row (L ("Additional compiler arguments:"), additional));
			return b;
		}

		public override void OnConfigChanged (string config, string platform) => Load ();

		public override void Load ()
		{
			var f = ConfigurationService.ReadVbCompilerFields (ProjectPath, Dialog.SelConfig, Dialog.SelPlatform);
			if (f is null)
				return;
			loadedTreatAsErrors = f.TreatWarningsAsErrors;
			OptimizeCombo.SelectedIndex = f.Optimize ? 0 : 1;
			DefineDebugCombo.SelectedIndex = f.DefineDebug ? 0 : 1;
			DefineTraceCombo.SelectedIndex = f.DefineTrace ? 0 : 1;
			// Legacy: DebugType lower-cased to None/pdbonly/full, anything else full.
			DebugTypeCombo.SelectedIndex = f.DebugType.ToLowerInvariant () switch {
				"none" => 0,
				"pdbonly" => 1,
				_ => 2,
			};
			// Legacy: WarningsDisabled (WarningLevel == 0) selects "No".
			WarnCombo.SelectedIndex = f.WarningLevel == 0 ? 1 : 0;
			XmlDocCombo.SelectedIndex = string.IsNullOrEmpty (f.DocumentationFile) ? 1 : 0;
			RemoveIntChecksCombo.SelectedIndex = f.RemoveIntegerChecks ? 0 : 1;
			DontWarn.Text = f.NoWarn;
			TreatAsError.Text = f.WarningsAsErrors;
			DefineConsts.Text = f.DefineConstants;
			Additional.Text = f.AdditionalParameters;
		}

		public override bool Store ()
		{
			var idx = Math.Clamp (DebugTypeCombo.SelectedIndex, 0, DebugTypes.Length - 1);
			var name = Path.GetFileNameWithoutExtension (ProjectPath);
			var fields = new ConfigurationService.VBCompilerFields (
				OptimizeCombo.SelectedIndex == 0,
				DefineDebugCombo.SelectedIndex == 0,
				DefineTraceCombo.SelectedIndex == 0,
				DebugTypes [idx],
				XmlDocCombo.SelectedIndex == 0 ? name + ".xml" : "",
				DontWarn.Text,
				TreatAsError.Text,
				loadedTreatAsErrors,
				WarnCombo.SelectedIndex == 1 ? 0 : 1,
				DefineConsts.Text,
				RemoveIntChecksCombo.SelectedIndex == 0,
				"",
				Additional.Text);
			return ConfigurationService.SaveVbCompilerFields (ProjectPath, Dialog.SelConfig, fields, Dialog.SelPlatform);
		}
	}
}
