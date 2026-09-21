using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;	using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy OptionsDialog (GlobalOptionsDialog.addin.xml section order) with
/// the panels that are wired in this shell: Visual Style, UI Language, Author, Key
/// Bindings, Fonts, Updates, Tasks, External Tools, Feedback, Load/Save, Build and
/// Maintenance — every one reading/writing the exact keys and file formats of the GTK UI
/// (MonoDevelopProperties.xml, Custom.kb.xml, MonoDevelop-tools.xml), so both UIs share
/// settings.
/// </summary>
public partial class PreferencesDialog : Window
{
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

	string? pendingLanguage;
	string? storedLanguage;
	bool updatingDetails;

	// Editable models of the panels (re-stored on OK like OptionsPanel.ApplyChanges).
	readonly Dictionary<string, string> keyBindings = new ();
	List<SettingsStore.ExternalTool> tools = new ();

	public PreferencesDialog ()
	{
		InitializeComponent ();
		ThemeDarkRadio!.IsCheckedChanged += OnThemeRadioChecked;
		ThemeLightRadio!.IsCheckedChanged += OnThemeRadioChecked;
		LoadSectionIcons ();
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
		LoadMaintenancePanel ();
		// Default selection: Visual Style (the first functional panel), like the GTK
		// dialog opens on the first selectable section.
		SelectPanel ("style");
	}

	// Section icons come from the redesigned MonoDevelop.Ide icon set (md-prefs-*),
	// in the same spot the legacy options dialog showed them: left of each section.
	void LoadSectionIcons ()
	{
		SetIcon (IconStyle!, "md-prefs-visual-style");
		SetIcon (IconLanguage!, "md-prefs-language");
		SetIcon (IconAuthor!, "md-prefs-author-information");
		SetIcon (IconKeyBindings!, "md-prefs-key-bindings");
		SetIcon (IconFonts!, "md-prefs-fonts");
		SetIcon (IconUpdates!, "md-prefs-updates");
		SetIcon (IconTasks!, "md-prefs-task-list");
		SetIcon (IconExternalTools!, "md-prefs-external-tools");
		SetIcon (IconLoadSave!, "md-prefs-load-save");
		SetIcon (IconBuild!, "md-prefs-build");
		SetIcon (IconSdkLocations!, "md-prefs-sdk-locations");
		SetIcon (IconFormatting!, "md-prefs-code-formatting");
		SetIcon (IconCodeSnippets!, "md-prefs-code-templates");
		SetIcon (IconNaming!, "md-prefs-dotnet-naming-policies");
		SetIcon (IconCodeFormatting!, "md-prefs-code-formatting");
		SetIcon (IconStandardHeader!, "md-prefs-header");
		SetIcon (IconFeedback!, "md-prefs-generic");
		SetIcon (IconMaintenance!, "md-prefs-generic");
	}

	static void SetIcon (Image image, string stockId)
	{
		if (IconService.GetImage (stockId) is Bitmap bmp)
			image.Source = bmp;
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

	// ---------- Panel switching (OptionsDialog.SelectPanel) ----------

	/// <summary>Selects a section by id, mirroring OptionsDialog.SelectPanel.</summary>
	public void SelectPanel (string panelId)
	{
		var item = SectionList?.Items.OfType<ListBoxItem> ().FirstOrDefault (i => (string?)i.Tag == panelId);
		if (item is not null)
			SectionList.SelectedItem = item;
	}

	void OnSectionSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (SectionList?.SelectedItem is not ListBoxItem item)
			return;
		var id = item.Tag as string;

		updatingDetails = true;

		PanelStyle!.IsVisible = id == "style";
		PanelLanguage!.IsVisible = id == "language";
		PanelAuthor!.IsVisible = id == "author";
		PanelKeyBindings!.IsVisible = id == "keybindings";
		PanelFonts!.IsVisible = id == "fonts";
		PanelUpdates!.IsVisible = id == "updates";
		PanelTasks!.IsVisible = id == "tasks";
		PanelExternalTools!.IsVisible = id == "externaltools";
		PanelFeedback!.IsVisible = id == "feedback";
		PanelLoadSave!.IsVisible = id == "loadsave";
		PanelBuild!.IsVisible = id == "build";
		PanelMaintenance!.IsVisible = id == "maintenance";
		PanelPlaceholder!.IsVisible = id is not (
			"style" or "language" or "author" or "keybindings" or "fonts" or "updates"
			or "tasks" or "externaltools" or "feedback" or "loadsave" or "build" or "maintenance");
		if (PanelPlaceholder.IsVisible)
			PlaceholderTitle!.Text = ExtractSectionTitle (item.Content);

		if (id == "style") {
			var dark = Application.Current?.ActualThemeVariant != ThemeVariant.Light;
			ThemeDarkRadio!.IsChecked = dark;
			ThemeLightRadio!.IsChecked = !dark;
		}

		updatingDetails = false;
	}

	static string ExtractSectionTitle (object? content)
	{
		if (content is StackPanel { Children: { } children })
			return children.OfType<TextBlock> ().FirstOrDefault ()?.Text ?? "";
		return content?.ToString ()?.Trim () ?? "";
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
		StoreMaintenancePanel ();
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e) => Close ();
}
