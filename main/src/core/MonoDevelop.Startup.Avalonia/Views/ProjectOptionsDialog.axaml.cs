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
		// P2: the ASP.NET XSP web server panel (legacy XspOptionsPanel, the
		// ItemOptionPanels/Run node of the AspNet add-in, condition
		// FlavorType=...AspNetAppProjectFlavor).
		"MonoDevelop.AspNet.Execution.XspOptionsPanel",
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
				"MonoDevelop.AspNet.Execution.XspOptionsPanel" => new XspPanel (this, projectPath, label),
				_ => null!,
			};
			AddPanel (panel, label);
		}
		// P3: the ASP.NET Core run configuration (App URL / LaunchBrowser / LaunchUrl
		// over launchSettings.json). Unlike the panels above it is NOT an
		// ItemOptionPanels node: the legacy hosts it as a RunConfigurationEditor
		// (Ide/RunConfigurationEditors, opened from the run parameters dialog, whose
		// port is a separate surface). The port exposes it as a dialog tab for the
		// AspNetCore projects — documented divergence.
		if (kind?.IsAspNetCore == true &&
			registry.GetExtensionNodes ("/MonoDevelop/Ide/RunConfigurationEditors")
				.Any (n => n.ClassName == "MonoDevelop.AspNetCore.AspNetCoreRunConfigurationEditor")) {
			AddPanel (new AspNetCoreRunConfigPanel (this, projectPath, "Run Configurations"), "Run Configurations");
		}
		okButton.IsEnabled = panels.Count > 0;
	}

	void AddPanel (PanelBase p, string label)
	{
		panels.Add (p);
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

	// ---------- XSP panel (legacy AspNet.Execution.XspOptionsPanel, P2) ----------

	// The ASP.NET (classic, non-SDK) XSP web server options: the legacy
	// XspOptionsPanelWidget is an ItemOptionsPanel (NOT a per-configuration one, so
	// the dialog's Configuration selector does not drive it — Load/Store only) with
	// three sections, General Options / Security / SSL Key, that persist to the
	// .csproj's <MonoDevelop><Properties><XspParameters> (XspParameters,
	// [ItemProperty("XspParameters", IsExternal=true)]). The combo indices ARE the
	// enum values (the legacy comments say so): SslMode None/Enable/ClientAccept/
	// ClientRequire, SslProtocol Default/Tls, KeyType None/Pkcs12/PVK,
	// PasswordOptions None/Ask/Store.
	//
	// Sensitivity (legacy UpdateSensitivity): the SSL rows enable when SslMode is not
	// None; the key rows enable when SSL is enabled AND a key type is chosen; the
	// certificate file only for PVK; the password only when PasswordOptions is
	// Store — and the legacy CLEARS the password text whenever the field is not
	// sensitive (UpdateSensitivity does passwordEntry.Text = "").
	//
	// Persisted via the P0 Read/SaveXspFields. Divergence: the legacy validates the
	// IP address on store (XspParameters.Address parses it, throwing on a bad
	// address); the port keeps the entry text as-is (a non-numeric value is stored
	// verbatim and the read falls back to the 8080 default) because the legacy
	// SpinButton's 0..65535 clamp has no direct Avalonia counterpart.
	class XspPanel : PanelBase
	{
		// Legacy combo order == enum values (XspParameters.cs). The legacy combo
		// displays the labels but persists the ENUM TOKENS ((XspSslMode) index etc.),
		// so the labels and the persisted tokens are kept as parallel arrays.
		static readonly string[] SslModeLabels = { "None", "Enabled", "Accept Client Certificates", "Require Client Certificates" };
		static readonly string[] SslModeTokens = { "None", "Enable", "ClientAccept", "ClientRequire" };
		static readonly string[] SslProtocolLabels = { "Default", "TLS" };
		static readonly string[] SslProtocolTokens = { "Default", "Tls" };
		static readonly string[] KeyTypeLabels = { "None", "Pkcs12", "PVK" };
		static readonly string[] KeyTypeTokens = { "None", "Pkcs12", "PVK" };
		static readonly string[] PasswordOptionLabels = { "None", "Ask", "Store (insecure)" };
		static readonly string[] PasswordOptionTokens = { "None", "Ask", "Store" };

		public TextBox Address { get; }
		public TextBox Port { get; }
		public CheckBox Verbose { get; }
		public ComboBox SslMode { get; }
		public ComboBox SslProtocol { get; }
		public ComboBox KeyType { get; }
		public TextBox KeyFile { get; }
		public TextBox CertFile { get; }
		public ComboBox PasswordOption { get; }
		public TextBox Password { get; }

		public XspPanel (ProjectOptionsDialog dialog, string projectPath, string label)
			: base (dialog, projectPath, label, BuildBody (dialog.StorageProvider, out var address, out var port,
				out var verbose, out var sslMode, out var sslProtocol, out var keyType, out var keyFile,
				out var certFile, out var passwordOption, out var password))
		{
			Address = address;
			Port = port;
			Verbose = verbose;
			SslMode = sslMode;
			SslProtocol = sslProtocol;
			KeyType = keyType;
			KeyFile = keyFile;
			CertFile = certFile;
			PasswordOption = passwordOption;
			Password = password;
			// Legacy: the three combos drive UpdateSensitivity; the password field
			// must be kept in sync with its sensitivity too.
			SslMode.SelectionChanged += (_, _) => UpdateSensitivity ();
			KeyType.SelectionChanged += (_, _) => UpdateSensitivity ();
			PasswordOption.SelectionChanged += (_, _) => UpdateSensitivity ();
		}

		static StackPanel BuildBody (Avalonia.Platform.Storage.IStorageProvider storage,
			out TextBox address, out TextBox port, out CheckBox verbose, out ComboBox sslMode,
			out ComboBox sslProtocol, out ComboBox keyType, out TextBox keyFile, out TextBox certFile,
			out ComboBox passwordOption, out TextBox password)
		{
			// Avalonia TextBox.Text defaults to null (the GTK entry defaulted to
			// ""), so every box starts "" — Load keeps them blank when the
			// <XspParameters> section is absent and Store must not read nulls.
			address = new TextBox { Text = "" };
			port = new TextBox { Text = "", Width = 90, HorizontalAlignment = HorizontalAlignment.Left };
			verbose = new CheckBox { Content = "Verbose console output" };
			sslMode = Combo (SslModeLabels);
			sslProtocol = Combo (SslProtocolLabels);
			keyType = Combo (KeyTypeLabels);
			keyFile = new TextBox { Text = "" };
			certFile = new TextBox { Text = "" };
			passwordOption = Combo (PasswordOptionLabels);
			password = new TextBox { Text = "", PasswordChar = '●' };

			var b = new StackPanel { Spacing = 0 };
			b.Children.Add (Section ("General Options"));
			b.Children.Add (Row (L ("IP address:"), WithHint (address, "(blank = localhost)")));
			b.Children.Add (Row (L ("Port:"), WithHint (port, "(0 = random)")));
			b.Children.Add (Row (L ("Verbose:"), verbose));
			b.Children.Add (Section ("Security"));
			b.Children.Add (Row (L ("SSL mode:"), sslMode));
			b.Children.Add (Row (L ("SSL protocol:"), sslProtocol));
			b.Children.Add (Section ("SSL Key"));
			b.Children.Add (Row (L ("Key type:"), keyType));
			b.Children.Add (Row (L ("Key file:"), WithBrowse (storage, "*.p12", "Key file", keyFile)));
			b.Children.Add (Row (L ("Certificate file:"), WithBrowse (storage, "*.cer", "Certificate file", certFile)));
			b.Children.Add (Row (L ("Password:"), WithPasswordCombo (passwordOption, password)));
			return b;
		}

		static ComboBox Combo (string[] items)
		{
			var c = new ComboBox { Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
			foreach (var s in items)
				c.Items.Add (s);
			return c;
		}

		static StackPanel WithHint (TextBox box, string hint)
		{
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			row.Children.Add (box);
			row.Children.Add (new TextBlock {
				Text = hint,
				FontSize = 11,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = DimTextBrush (),
			});
			return row;
		}

		static Button BrowseButton (Avalonia.Platform.Storage.IStorageProvider storage, string pattern, string title, TextBox box)
		{
			var browse = new Button { Content = "Browse…", MinWidth = 70, Classes = { "chromebtn" } };
			browse.Click += async (_, _) => {
				var files = await storage.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
					Title = title,
					AllowMultiple = false,
					FileTypeFilter = new[] {
						new Avalonia.Platform.Storage.FilePickerFileType (title) { Patterns = new[] { pattern } },
						new Avalonia.Platform.Storage.FilePickerFileType ("All files") { Patterns = new[] { "*.*" } },
					},
				});
				if (files.Count > 0)
					box.Text = files [0].Path.LocalPath;
			};
			return browse;
		}

		static StackPanel WithBrowse (Avalonia.Platform.Storage.IStorageProvider storage, string pattern, string title, TextBox box)
		{
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			row.Children.Add (box);
			row.Children.Add (BrowseButton (storage, pattern, title, box));
			return row;
		}

		static StackPanel WithPasswordCombo (ComboBox option, TextBox password)
		{
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			row.Children.Add (option);
			row.Children.Add (password);
			return row;
		}

		public override void Load ()
		{
			var f = ConfigurationService.ReadXspFields (ProjectPath);
			if (f is null)
				return; // no <MonoDevelop> section yet: the widgets keep their blank defaults
			Address.Text = f.Address;
			Port.Text = f.Port.ToString (System.Globalization.CultureInfo.InvariantCulture);
			Verbose.IsChecked = f.Verbose;
			// The persisted values are the enum TOKENS (SaveXspFields writes them); the
			// combo indices are the enum values, so match by token, not label.
			SslMode.SelectedIndex = Math.Clamp (Array.IndexOf (SslModeTokens, f.SslMode), 0, SslModeTokens.Length - 1);
			SslProtocol.SelectedIndex = Math.Clamp (Array.IndexOf (SslProtocolTokens, f.SslProtocol), 0, SslProtocolTokens.Length - 1);
			KeyType.SelectedIndex = Math.Clamp (Array.IndexOf (KeyTypeTokens, f.KeyType), 0, KeyTypeTokens.Length - 1);
			KeyFile.Text = f.KeyFile;
			CertFile.Text = f.CertificateFile;
			PasswordOption.SelectedIndex = Math.Clamp (Array.IndexOf (PasswordOptionTokens, f.PasswordOptions), 0, PasswordOptionTokens.Length - 1);
			Password.Text = f.Password;
			UpdateSensitivity ();
		}

		// Legacy UpdateSensitivity (XspOptionsPanelWidget.cs:184-200): the SSL rows
		// enable when the mode is not None; the key rows when a key type is chosen
		// too; the certificate file only for PVK (index 2); the password only when
		// PasswordOptions is Store (index 2) — and the legacy clears the password
		// text whenever the field becomes insensitive.
		public void UpdateSensitivity ()
		{
			var sslEnabled = SslMode.SelectedIndex > 0;
			SslProtocol.IsEnabled = sslEnabled;
			KeyType.IsEnabled = sslEnabled;
			var keyEnabled = sslEnabled && KeyType.SelectedIndex > 0;
			KeyFile.IsEnabled = keyEnabled;
			PasswordOption.IsEnabled = keyEnabled;
			CertFile.IsEnabled = keyEnabled && KeyType.SelectedIndex == 2;
			Password.IsEnabled = keyEnabled && PasswordOption.SelectedIndex == 2;
			if (!Password.IsEnabled)
				Password.Text = "";
		}

		public override bool Store ()
		{
			int port;
			if (!int.TryParse (Port.Text, out port) || port < 0 || port > 65535)
				port = 8080; // legacy SpinButton clamp; an out-of-range value falls back
			// The persisted values are the enum TOKENS (XspParameters serializes the
			// enum value); the combo shows the labels, so map index -> token.
			string Token (string[] tokens, ComboBox c) =>
				c.SelectedIndex >= 0 ? tokens [Math.Clamp (c.SelectedIndex, 0, tokens.Length - 1)] : "";
			return ConfigurationService.SaveXspFields (ProjectPath, new ConfigurationService.XspFields (
				port,
				Address.Text,
				Token (SslModeTokens, SslMode), Token (SslProtocolTokens, SslProtocol),
				Token (KeyTypeTokens, KeyType),
				CertFile.Text, KeyFile.Text, Token (PasswordOptionTokens, PasswordOption),
				Password.IsEnabled ? Password.Text : "",
				Verbose.IsChecked == true));
		}
	}

	// ---------- ASP.NET Core run configuration panel (legacy AspNetCoreRunConfigurationEditor, P3) ----------

	// The ASP.NET Core run profile fields of the STARTUP run profile of launchSettings.json
	// (<projectDir>/Properties/launchSettings.json): the legacy
	// AspNetCoreRunConfigurationEditorWidget (an Xwt RunConfigurationEditor, NOT an
	// ItemOptionPanel — see BuildTabs) has three controls: the App URL entry
	// ("Where your app should listen for connections", a ;-separated list of
	// absolute URLs with an "Invalid URL" warning icon), the "Open URL in web
	// browser when app starts" check, and the URL entry ("Absolute or relative to
	// App URL", sensitive only while the check is on).
	//
	// The legacy binds the profile from the run configuration's name
	// (AspNetCoreRunConfiguration.CurrentProfile, the profile of the same name,
	// created empty when the run configuration is fresh). The port instead shows a
	// Profile combo over the file's profiles, defaulting to the FIRST one —
	// documented divergence, since the shell's run-configuration model (the
	// toolbar combo) is a separate surface and has no profile-name binding yet.
	//
	// Persisted via the P0 Read/SaveAspNetCoreRunProfile: launchBrowser is always
	// written, launchUrl and applicationUrl are written non-empty and removed when
	// empty (the legacy LaunchProfileData serialization omits them).
	class AspNetCoreRunConfigPanel : PanelBase
	{
		public ComboBox ProfileCombo { get; }
		public TextBox ApplicationUrl { get; }
		public CheckBox LaunchBrowser { get; }
		public TextBox LaunchUrl { get; }
		public TextBlock UrlWarning { get; }

		IDisposable? appUrlSubscription;

		public AspNetCoreRunConfigPanel (ProjectOptionsDialog dialog, string projectPath, string label)
			: base (dialog, projectPath, label, BuildBody (out var profile, out var appUrl, out var browser,
				out var launchUrl, out var warning))
		{
			ProfileCombo = profile;
			ApplicationUrl = appUrl;
			LaunchBrowser = browser;
			LaunchUrl = launchUrl;
			UrlWarning = warning;
			// Avalonia CheckBox has no Checked/Unchecked pair (WPF); the shell
			// routes both transitions through IsCheckedChanged.
			LaunchBrowser.IsCheckedChanged += (_, _) => LaunchUrl.IsEnabled = LaunchBrowser.IsChecked == true;
			// Avalonia TextBox does not fire TextChanged for programmatic sets
			// (it only fires for user input), so the warning tracks the text
			// through the observable instead — the oracle (and the UI) change
			// the text programmatically.
			appUrlSubscription = appUrl.GetObservable (TextBox.TextProperty)
				.Subscribe (new TextObserver (this));
			// A named handler (not a lambda): Load must unsubscribe the very same
			// delegate instance it re-subscribes around the combo rebuild.
			ProfileCombo.SelectionChanged += OnProfileChanged;
		}

		void OnProfileChanged (object? sender, SelectionChangedEventArgs e) => LoadProfile ();

		// The first OnNext carries the text's current value (GetObservable pushes
		// it on subscribe); it is harmless here, just re-evaluates the warning.
		sealed class TextObserver : IObserver<string?>
		{
			readonly AspNetCoreRunConfigPanel panel;
			public TextObserver (AspNetCoreRunConfigPanel panel) { this.panel = panel; }
			public void OnCompleted () { }
			public void OnError (System.Exception error) { }
			public void OnNext (string? value) => panel.RefreshUrlWarning ();
		}

		static StackPanel BuildBody (out ComboBox profile, out TextBox appUrl, out CheckBox browser,
			out TextBox launchUrl, out TextBlock warning)
		{
			profile = new ComboBox { Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
			// Avalonia TextBox.Text defaults to null (the GTK entry defaulted to
			// ""), so the boxes start "".
			appUrl = new TextBox { Text = "" };
			browser = new CheckBox { Content = "Open URL in web browser when app starts:" };
			launchUrl = new TextBox { Text = "" };
			warning = new TextBlock {
				Text = "Invalid URL",
				FontSize = 11,
				Foreground = WarningBrush (),
				IsVisible = false,
			};
			var appUrlRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			appUrlRow.Children.Add (appUrl);
			appUrlRow.Children.Add (warning);

			var b = new StackPanel { Spacing = 0 };
			b.Children.Add (Row (L ("Profile:"), profile));
			b.Children.Add (Section ("ASP.NET Core"));
			b.Children.Add (Row (L ("App URL:"), appUrlRow));
			b.Children.Add (new TextBlock {
				Text = "Where your app should listen for connections",
				FontSize = 11,
				Foreground = DimTextBrush (),
				Margin = new Thickness (150, 0, 0, 0),
			});
			b.Children.Add (Row (L ("Launch browser:"), browser));
			b.Children.Add (Row (L ("URL:"), launchUrl));
			b.Children.Add (new TextBlock {
				Text = "Absolute or relative to App URL",
				FontSize = 11,
				Foreground = DimTextBrush (),
				Margin = new Thickness (150, 0, 0, 0),
			});
			return b;
		}

		void RefreshUrlWarning ()
		{
			// Legacy IsValidUrl (AspNetCoreRunConfigurationEditorWidget.cs:140-152):
			// a ";"-separated list, every part must be an absolute URL.
			UrlWarning.IsVisible = !IsValidUrl (ApplicationUrl.Text);
		}

		static bool IsValidUrl (string? url)
		{
			if (url is null)
				url = "";
			if (url.Contains (';')) {
				foreach (var part in url.Split (';'))
					if (!Uri.TryCreate (part, UriKind.Absolute, out _))
						return false;
				return true;
			}
			return Uri.TryCreate (url, UriKind.Absolute, out _);
		}

		string CurProfile () => ProfileCombo.SelectedIndex >= 0 ? (string)ProfileCombo.SelectedItem! : "";

		public override void Load ()
		{
			var names = ConfigurationService.GetAspNetCoreRunProfiles (ProjectPath).ToList ();
			ProfileCombo.SelectionChanged -= OnProfileChanged;
			ProfileCombo.Items.Clear ();
			foreach (var n in names)
				ProfileCombo.Items.Add (n);
			ProfileCombo.SelectionChanged += OnProfileChanged;
			ProfileCombo.SelectedIndex = names.Count > 0 ? 0 : -1;
			LoadProfile ();
		}

		void LoadProfile ()
		{
			var name = CurProfile ();
			if (name.Length == 0) {
				ApplicationUrl.Text = "";
				LaunchBrowser.IsChecked = false;
				LaunchUrl.Text = "";
				LaunchUrl.IsEnabled = false;
				RefreshUrlWarning ();
				return;
			}
			var f = ConfigurationService.ReadAspNetCoreRunProfile (ProjectPath, name);
			if (f is null)
				return;
			ApplicationUrl.Text = f.ApplicationUrl;
			LaunchBrowser.IsChecked = f.LaunchBrowser;
			LaunchUrl.Text = f.LaunchUrl;
			LaunchUrl.IsEnabled = f.LaunchBrowser;
			RefreshUrlWarning ();
		}

		public override bool Store ()
		{
			var name = CurProfile ();
			if (name.Length == 0)
				return true; // nothing selected: nothing to write
			// Legacy SaveCore: the URL entry stores only while the check is on.
			var launchUrl = LaunchBrowser.IsChecked == true ? LaunchUrl.Text : "";
			return ConfigurationService.SaveAspNetCoreRunProfile (ProjectPath, name,
				new ConfigurationService.AspNetCoreRunProfileFields (
					name, LaunchBrowser.IsChecked == true, launchUrl, ApplicationUrl.Text));
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

	/// <summary>QA oracle for the P2 (XSP panel) and P3 (ASP.NET Core run configuration
	/// tab) ports (no --sln needed: it builds its own fixtures in temp, like the P0
	/// oracle). For a classic ASP.NET app project (the AspNetApp flavor GUID) it
	/// asserts the single XSP tab, the legacy combo TOKEN round-trip (the legacy
	/// persists the enum value, not the label), the UpdateSensitivity chain and the
	/// password clearing, and that the &lt;MonoDevelop&gt;&lt;Properties&gt;
	/// &lt;XspParameters&gt; section lands in the .csproj; for an ASP.NET Core project
	/// (SDK web) it asserts the single Run Configurations tab, the profile round-trip
	/// over launchSettings.json (launchBrowser always, launchUrl/applicationUrl
	/// removed when empty, the URL cleared when the browser check is off, the invalid
	/// URL warning) and that the file is created when absent. Cross-checks: the
	/// AspNetApp project must NOT show the run-config tab and the AspNetCore project
	/// must NOT show the XSP tab (the FlavorType condition).</summary>
	public bool QaWebOptionsOracle ()
	{
		var mw = MainWindow.Instance;
		int ok = 0, fail = 0;
		void Check (string what, bool cond)
		{
			mw?.Output ("[qa-vboptions] " + (cond ? "ok" : "FAIL") + " " + what);
			if (cond) ok++; else fail++;
		}
		void Log (string line) => mw?.Output ("[qa-vboptions] " + line);

		string Tmp (out string dir)
		{
			dir = Path.Combine (Path.GetTempPath (), "qa_weboptions_" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (dir);
			return dir;
		}
		static string Sln (string name, string guid) =>
			"Microsoft Visual Studio Solution File, Format Version 12.00\n"
			+ "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"" + name + "\", \"" + name + "\\" + name + ".csproj\", \"" + guid + "\"\n"
			+ "EndProject\nGlobal\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n"
			+ "\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|Any CPU = Release|Any CPU\n\tEndGlobalSection\n"
			+ "EndGlobal\n";

		try {
			// ----- P2: the classic ASP.NET app project (AspNetApp flavor GUID) -----
			var webDir = Tmp (out _);
			var web = Path.Combine (webDir, "WebApp.csproj");
			File.WriteAllText (web, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
				+ "<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n"
				+ "  <PropertyGroup>\n"
				+ "    <ProjectTypeGuids>{349C5851-65DF-11DA-9384-00065B846F21};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>\n"
				+ "  </PropertyGroup>\n"
				+ "</Project>\n");
			var webSln = Path.Combine (webDir, "WebApp.sln");
			File.WriteAllText (webSln, Sln ("WebApp", "{A0000000-0000-4000-8000-0000000000A1}"));
			var dlgWeb = new ProjectOptionsDialog (web, webSln);
			Check ("p2 kind AspNetApp", dlgWeb.Kind is { IsAspNetApp: true, IsAspNetCore: false, IsVBNet: false });
			Check ("p2 tabs=1 XSP", dlgWeb.Panels.Count == 1
				&& dlgWeb.Panels [0] is XspPanel && dlgWeb.Panels [0].Label == "XSP Web Server");
			Check ("p2 no run-config tab (not AspNetCore)",
				!dlgWeb.Panels.OfType<AspNetCoreRunConfigPanel> ().Any ());
			var xsp = dlgWeb.Panels.OfType<XspPanel> ().FirstOrDefault ();
			if (xsp is not null) {
				// No <MonoDevelop> section yet: Load keeps the blank defaults.
				Check ("p2 absent defaults", xsp.Port.Text.Length == 0 && xsp.Verbose.IsChecked != true);
				xsp.Address.Text = "192.168.1.10";
				xsp.Port.Text = "8180";
				xsp.Verbose.IsChecked = false;
				xsp.SslMode.SelectedIndex = 1;             // Enabled (token "Enable")
				xsp.SslProtocol.SelectedIndex = 1;         // TLS (token "Tls")
				xsp.KeyType.SelectedIndex = 1;             // Pkcs12
				xsp.KeyFile.Text = "server.p12";
				xsp.PasswordOption.SelectedIndex = 2;      // Store (insecure) (token "Store")
				xsp.Password.Text = "s3cret";
				Check ("p2 password sensitive when Store", xsp.Password.IsEnabled);
				Check ("p2 store", xsp.Store ());
				var x1 = ConfigurationService.ReadXspFields (web)!;
				Log ("p2 xsp roundtrip: port=" + x1.Port + " addr='" + x1.Address + "' ssl='" + x1.SslMode
					+ "' proto='" + x1.SslProtocol + "' keytype='" + x1.KeyType + "' keyfile='" + x1.KeyFile
					+ "' passopt='" + x1.PasswordOptions + "' pass='" + x1.Password + "' verbose=" + x1.Verbose);
				Check ("p2 rt tokens (enum values, not labels)", x1 is
					{ Port: 8180, Address: "192.168.1.10", SslMode: "Enable", SslProtocol: "Tls",
					  KeyType: "Pkcs12", KeyFile: "server.p12", PasswordOptions: "Store", Password: "s3cret",
					  Verbose: false, CertificateFile: "" });
				// Re-open: the combos must land back on the same indices by TOKEN.
				var dlgWeb2 = new ProjectOptionsDialog (web, webSln);
				var xsp2 = dlgWeb2.Panels.OfType<XspPanel> ().FirstOrDefault ();
				Check ("p2 reload by token", xsp2 is not null && xsp2.SslMode.SelectedIndex == 1
					&& xsp2.SslProtocol.SelectedIndex == 1 && xsp2.KeyType.SelectedIndex == 1
					&& xsp2.PasswordOption.SelectedIndex == 2 && xsp2.Password.Text == "s3cret"
					&& xsp2.Port.Text == "8180" && xsp2.Verbose.IsChecked == false);
				dlgWeb2.Close ();
				// Sensitivity chain (legacy UpdateSensitivity): mode None -> the whole
				// SSL/key/password chain insensitizes AND clears the password.
				xsp.SslMode.SelectedIndex = 0;             // None
				Check ("p2 sensitivity: protocol off", !xsp.SslProtocol.IsEnabled && !xsp.KeyType.IsEnabled);
				Check ("p2 sensitivity: key rows off", !xsp.KeyFile.IsEnabled && !xsp.PasswordOption.IsEnabled
					&& !xsp.CertFile.IsEnabled && !xsp.Password.IsEnabled);
				Check ("p2 sensitivity: password cleared", xsp.Password.Text.Length == 0);
				xsp.SslMode.SelectedIndex = 1;             // Enabled (PasswordOption still on Store)
				xsp.KeyType.SelectedIndex = 1;             // Pkcs12 -> cert file stays OFF
				Check ("p2 cert off for Pkcs12", !xsp.CertFile.IsEnabled && xsp.KeyFile.IsEnabled
					&& xsp.PasswordOption.IsEnabled && xsp.Password.IsEnabled);
				xsp.KeyType.SelectedIndex = 2;             // PVK -> cert file ON
				Check ("p2 cert on for PVK", xsp.CertFile.IsEnabled);
				xsp.PasswordOption.SelectedIndex = 0;      // None -> OFF + cleared again
				Check ("p2 password off for None", !xsp.Password.IsEnabled && xsp.Password.Text.Length == 0);
				// The file carries the section (and nothing else).
				var raw = File.ReadAllText (web);
				Check ("p2 section in file", raw.Contains ("<XspParameters>", StringComparison.Ordinal)
					&& raw.Contains ("<Port>8180</Port>", StringComparison.Ordinal)
					&& raw.Contains ("<SslMode>Enable</SslMode>", StringComparison.Ordinal)
					&& raw.Contains ("<PasswordOptions>Store</PasswordOptions>", StringComparison.Ordinal));
			}
			dlgWeb.Close ();

			// ----- P3: the ASP.NET Core project (SDK web) -----
			var coreDir = Tmp (out _);
			var core = Path.Combine (coreDir, "CoreApp.csproj");
			File.WriteAllText (core, "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n");
			var coreSln = Path.Combine (coreDir, "CoreApp.sln");
			File.WriteAllText (coreSln, Sln ("CoreApp", "{B0000000-0000-4000-8000-0000000000B2}"));
			var launchSettings = Path.Combine (coreDir, "Properties", "launchSettings.json");
			Directory.CreateDirectory (Path.GetDirectoryName (launchSettings)!);
			File.WriteAllText (launchSettings,
				"{\n  \"profiles\": {\n    \"CoreApp\": {\n      \"commandName\": \"Project\",\n"
				+ "      \"launchBrowser\": true,\n      \"launchUrl\": \"weatherforecast/\",\n"
				+ "      \"applicationUrl\": \"http://localhost:5080;https://localhost:7080\"\n    },\n"
				+ "    \"IIS Express\": {\n      \"commandName\": \"IIS\",\n      \"launchBrowser\": true\n    }\n  }\n}\n");
			var dlgCore = new ProjectOptionsDialog (core, coreSln);
			Check ("p3 kind AspNetCore", dlgCore.Kind is { IsAspNetCore: true, IsAspNetApp: false });
			Check ("p3 tabs=1 run-config", dlgCore.Panels.Count == 1
				&& dlgCore.Panels [0] is AspNetCoreRunConfigPanel && dlgCore.Panels [0].Label == "Run Configurations");
			Check ("p3 no XSP tab (FlavorType condition)", !dlgCore.Panels.OfType<XspPanel> ().Any ());
			var run = dlgCore.Panels.OfType<AspNetCoreRunConfigPanel> ().FirstOrDefault ();
			if (run is not null) {
				// The profile combo lists every profile; the first is selected.
				var profiles = run.ProfileCombo.Items.OfType<string> ().ToList ();
				Check ("p3 profiles listed", profiles.Count == 2 && profiles [0] == "CoreApp" && profiles [1] == "IIS Express");
				Check ("p3 loaded", run.ApplicationUrl.Text == "http://localhost:5080;https://localhost:7080"
					&& run.LaunchBrowser.IsChecked == true && run.LaunchUrl.Text == "weatherforecast/"
					&& run.LaunchUrl.IsEnabled && !run.UrlWarning.IsVisible);
				// Edit the first profile: every URL valid, no warning.
				run.ApplicationUrl.Text = "https://localhost:7180;http://localhost:5180";
				Check ("p3 warning off for valid multi-url", !run.UrlWarning.IsVisible);
				run.ApplicationUrl.Text = "not-a-url";
				Check ("p3 warning on for invalid url", run.UrlWarning.IsVisible);
				run.ApplicationUrl.Text = "https://localhost:7180;http://localhost:5180";
				run.LaunchUrl.Text = "api/values";
				Check ("p3 store", run.Store ());
				var r1 = ConfigurationService.ReadAspNetCoreRunProfile (core, "CoreApp")!;
				Log ("p3 profile roundtrip: browser=" + r1.LaunchBrowser + " url='" + r1.LaunchUrl
					+ "' appurl='" + r1.ApplicationUrl + "'");
				Check ("p3 rt", r1 is { LaunchBrowser: true, LaunchUrl: "api/values",
					ApplicationUrl: "https://localhost:7180;http://localhost:5180" });
				// The OTHER profile must be untouched.
				var other = File.ReadAllText (launchSettings);
				Check ("p3 other profile untouched", other.Contains ("\"IIS Express\"", StringComparison.Ordinal));
				// Browser off: the check is always written and the URL entry is not stored.
				run.LaunchBrowser.IsChecked = false;
				Check ("p3 url entry insensitive when browser off", !run.LaunchUrl.IsEnabled);
				Check ("p3 store2", run.Store ());
				var r2 = ConfigurationService.ReadAspNetCoreRunProfile (core, "CoreApp")!;
				Check ("p3 rt browser off", r2 is { LaunchBrowser: false, LaunchUrl: "" }
					&& r2.ApplicationUrl == "https://localhost:7180;http://localhost:5180");
				var raw2 = File.ReadAllText (launchSettings);
				Check ("p3 launchUrl removed from file", !raw2.Contains ("\"launchUrl\"", StringComparison.Ordinal));
				// ApplicationUrl empty -> the element is removed too (legacy serialization).
				run.ApplicationUrl.Text = "";
				Check ("p3 store3", run.Store ());
				var r3 = ConfigurationService.ReadAspNetCoreRunProfile (core, "CoreApp")!;
				Check ("p3 rt appurl empty", r3.ApplicationUrl.Length == 0);
				Check ("p3 applicationUrl removed from file",
					!File.ReadAllText (launchSettings).Contains ("\"applicationUrl\"", StringComparison.Ordinal));
				// A project WITHOUT launchSettings.json: no profiles, the panel stores
				// nothing (and creating one would be the legacy provider's first save).
				var bareDir = Tmp (out _);
				var bare = Path.Combine (bareDir, "BareApp.csproj");
				File.WriteAllText (bare, "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n</Project>\n");
				var dlgBare = new ProjectOptionsDialog (bare, null);
				var runBare = dlgBare.Panels.OfType<AspNetCoreRunConfigPanel> ().FirstOrDefault ();
				Check ("p3 no file -> no profiles", runBare is not null && runBare.ProfileCombo.SelectedIndex == -1
					&& runBare.ProfileCombo.Items.Count == 0);
				Check ("p3 no file -> store is a no-op", runBare is not null && runBare.Store ());
				dlgBare.Close ();
			}
			dlgCore.Close ();
		} catch (Exception ex) {
			Check ("web oracle (exception: " + ex.Message + ")", false);
			Log ("web oracle stack: " + ex.StackTrace);
		}

		Log ("total web ok=" + ok + " fail=" + fail);
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

		// Theme-aware brushes for the dim helper text and the URL validation warning
		// (no theme resource carries them; the diff viewer builds these the same way
		// off ActualThemeVariant).
		static bool DarkTheme => Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
		protected static Brush DimTextBrush () =>
			new SolidColorBrush (DarkTheme ? Avalonia.Media.Color.Parse ("#9A9A9A") : Avalonia.Media.Color.Parse ("#6E6E6E"));
		protected static Brush WarningBrush () =>
			new SolidColorBrush (DarkTheme ? Avalonia.Media.Color.Parse ("#E5C07B") : Avalonia.Media.Color.Parse ("#9A6700"));

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
