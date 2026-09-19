using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class MainWindow : Window
{
	static readonly bool IsMac = RuntimeInformation.IsOSPlatform (OSPlatform.OSX);

	public MainWindow ()
	{
		InitializeComponent ();
		Output ("MonoDevelop Avalonia shell initialized.");

		// Window drag on the chrome row (menu bar doubles as the title bar).
		PointerPressed += (s, e) => {
			if (e.GetCurrentPoint (this).Properties.IsLeftButtonPressed &&
			    TitleBarRow?.Bounds.Contains (e.GetPosition (TitleBarRow)) == true)
				BeginMoveDrag (e);
		};
		DoubleTapped += (s, e) => {
			if (TitleBarRow?.Bounds.Contains (e.GetPosition (TitleBarRow)) == true)
				ToggleMaximize ();
		};

		// The placement convention (mac left, Windows/Linux right) is handled in
		// OnOpened by re-parenting the caption buttons to the requested side; the
		// XAML default places them on the right, matching Linux and Windows.
		Opened += (s, e) => {
			if (IsMac)
				MoveCaptionButtonsLeft ();
			ApplyThemeVariant (Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark);
		};
	}

	/// <summary>macOS: caption buttons live at the left edge of the menu bar row.</summary>
	void MoveCaptionButtonsLeft ()
	{
		var grid = TitleBarRow?.Children.OfType<Grid> ().FirstOrDefault ();
		if (grid is null || CaptionButtons is null)
			return;

		Grid.SetColumn (CaptionButtons, 0);
		CaptionButtons.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
		if (grid.Children.OfType<Menu> ().FirstOrDefault () is { } menu) {
			Grid.SetColumn (menu, 1);
			menu.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
			menu.Margin = new Thickness (110, 0, 0, 0); // clear the traffic-light area
		}
	}

	void ToggleMaximize ()
	{
		if (WindowState == WindowState.Maximized)
			WindowState = WindowState.Normal;
		else
			WindowState = WindowState.Maximized;
	}

	void OnMinimize (object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

	void OnMaximize (object? sender, RoutedEventArgs e) => ToggleMaximize ();

	void OnClose (object? sender, RoutedEventArgs e) => Close ();

	void OnTheme (object? sender, RoutedEventArgs e)
	{
		var menu = (MenuItem)sender!;
		ApplyThemeVariant (menu.Name == "ThemeLight" ? ThemeVariant.Light : ThemeVariant.Dark);
	}

	void ApplyThemeVariant (ThemeVariant variant)
	{
		if (Application.Current is null)
			return;
		Application.Current.RequestedThemeVariant = variant;
		Background = new SolidColorBrush (
			variant == ThemeVariant.Light ? Color.Parse ("#FFFFFF") : Color.Parse ("#1E1E1E"));
	}

	void OnTogglePad (object? sender, RoutedEventArgs e)
	{
		var menu = (MenuItem)sender!;
		switch (menu.Name) {
		case "ViewSolutionPad":
			SolutionPane!.IsVisible = menu.IsChecked;
			break;
		case "ViewOutput":
			OutputPane!.IsVisible = menu.IsChecked;
			break;
		}
	}

	void OnAbout (object? sender, RoutedEventArgs e)
		=> new AboutDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);

	void OnPreferences (object? sender, RoutedEventArgs e)
		=> new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);

	void OnAddinManager (object? sender, RoutedEventArgs e)
		=> new AddinManagerDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);

	public void Output (string message)
	{
		if (OutputText is null)
			return;
		OutputText.Text = OutputText.Text?.Length == 0
			? message
			: OutputText.Text + "\n" + message;
		StatusText!.Text = message;
	}
}
