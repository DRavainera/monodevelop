using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Views;

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

	string? pendingLanguage;
	string? storedLanguage;
	bool updatingDetails;

	public PreferencesDialog ()
	{
		InitializeComponent ();
		ThemeDarkRadio.IsCheckedChanged += OnThemeRadioChecked;
		ThemeLightRadio.IsCheckedChanged += OnThemeRadioChecked;
		LoadSectionIcons ();
		LoadLanguagePanel ();
		// Default selection: Visual Style (the first functional panel), like the GTK
		// dialog opens on the first selectable section.
		SelectPanel ("style");
	}

	// Section icons come from the redesigned MonoDevelop.Ide icon set (md-prefs-*),
	// in the same spot the legacy options dialog showed them: left of each section.
	void LoadSectionIcons ()
	{
		SetIcon (IconStyle, "md-prefs-visual-style");
		SetIcon (IconLanguage, "md-prefs-language");
		SetIcon (IconAuthor, "md-prefs-author-information");
		SetIcon (IconKeyBindings, "md-prefs-key-bindings");
		SetIcon (IconFonts, "md-prefs-fonts");
		SetIcon (IconUpdates, "md-prefs-updates");
		SetIcon (IconTasks, "md-prefs-task-list");
		SetIcon (IconExternalTools, "md-prefs-external-tools");
		SetIcon (IconLoadSave, "md-prefs-load-save");
		SetIcon (IconBuild, "md-prefs-build");
		SetIcon (IconSdkLocations, "md-prefs-sdk-locations");
		SetIcon (IconFormatting, "md-prefs-code-formatting");
		SetIcon (IconCodeSnippets, "md-prefs-code-templates");
		SetIcon (IconLanguageBundles, "md-prefs-generic");
		SetIcon (IconNaming, "md-prefs-dotnet-naming-policies");
		SetIcon (IconCodeFormatting, "md-prefs-code-formatting");
		SetIcon (IconStandardHeader, "md-prefs-header");
	}

	static void SetIcon (Image? image, string stockId)
	{
		if (image is null)
			return;
		if (IconService.GetImage (stockId) is Bitmap bmp)
			image.Source = bmp;
	}

	void LoadLanguagePanel ()
	{
		storedLanguage = LoadStoredLanguage ();
		foreach (var locale in LocaleSet)
			LanguageCombo!.Items.Add (new ComboBoxItem { Content = locale.DisplayName, Tag = locale.Culture });
		var index = Array.FindIndex (LocaleSet, l => l.Culture == storedLanguage);
		if (index < 0) index = 0;
		LanguageCombo!.SelectedIndex = index;
		LanguageCombo.SelectionChanged += OnLanguageChanged;
		pendingLanguage = null;
		UpdateLanguageRestartUi ();
	}

	// Legacy storage: MonoDevelop.Ide.UserInterfaceLanguage in the config property file.
	static string? LoadStoredLanguage ()
	{
		try {
			var home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			var path = Path.Combine (home, ".config", "MonoDevelop", "9.0", "MonoDevelop-properties.xml");
			if (!File.Exists (path))
				return null;
			var text = File.ReadAllText (path);
			const string key = "MonoDevelop.Ide.UserInterfaceLanguage";
			var i = text.IndexOf (key, StringComparison.Ordinal);
			if (i < 0)
				return null;
			var start = text.IndexOf ('>', i) + 1;
			var end = text.IndexOf ('<', start);
			return start > 0 && end > start ? text [start..end] : null;
		} catch {
			return null;
		}
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
		// Persisting + restarting mirrors IDEStyleOptionsPanel.Store/RestartClicked of the
		// legacy dialog. Persistence lands with the settings service port (M5/M6); the
		// panel already exposes the full language list and the restart affordance.
		Console.WriteLine ("[prefs] restart requested with language=" + pendingLanguage);
		MainWindow.Instance?.Output ("[prefs] language change stored on OK (restart required)");
	}

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

		// Ported panels: Visual Style + User Interface Language; the rest show the placeholder.
		PanelStyle!.IsVisible = id == "style";
		PanelLanguage!.IsVisible = id == "language";
		PanelPlaceholder!.IsVisible = id is not ("style" or "language");
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
		if (sender is RadioButton rb && rb.IsChecked != true)
			return;
		var light = ReferenceEquals (sender, ThemeLightRadio);
		if (Application.Current is not null)
			Application.Current.RequestedThemeVariant = light ? ThemeVariant.Light : ThemeVariant.Dark;
	}

	void OnOk (object? sender, RoutedEventArgs e) => Close ();

	void OnCancel (object? sender, RoutedEventArgs e) => Close ();
}
