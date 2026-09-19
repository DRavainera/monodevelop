using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class PreferencesDialog : Window
{
	public PreferencesDialog ()
	{
		InitializeComponent ();
		ThemeDarkRadio.IsCheckedChanged += OnThemeRadioChecked;
		ThemeLightRadio.IsCheckedChanged += OnThemeRadioChecked;
		// Default selection: Visual Style (the first functional panel), like the GTK
		// dialog opens on the first selectable section.
		SelectPanel ("style");
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

		// Only the Visual Style panel is ported so far; the rest show the placeholder.
		var ported = id == "style";
		PanelStyle!.IsVisible = ported;
		PanelPlaceholder!.IsVisible = !ported;
		if (!ported)
			PlaceholderTitle!.Text = item.Content?.ToString ()?.Trim () ?? id ?? "";

		if (ported) {
			var dark = Application.Current?.RequestedThemeVariant != ThemeVariant.Light;
			ThemeDarkRadio!.IsChecked = dark;
			ThemeLightRadio!.IsChecked = !dark;
		}
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
