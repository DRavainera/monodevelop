using System;
using System.IO;
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

	public static MainWindow? Instance { get; private set; }

	public MainWindow ()
	{
		InitializeComponent ();
		Instance = this;
		Output ("MonoDevelop Avalonia shell initialized.");

		// Full legacy main menu: same structure/order/labels/icons/shortcuts as the GTK UI.
		MainMenu!.Items.Clear ();
		foreach (var item in MenuBuilder.BuildItems (MenuService.BuildMainMenu ()))
			MainMenu.Items.Add (item);

		// Window drag on the chrome row background (menu bar doubles as the title bar).
		// Attached to the row, not the window: menu/button presses are handled first by
		// their own controls and never reach this bubbling handler.
		if (TitleBarRow is not null) {
			TitleBarRow.PointerPressed += (s, e) => {
				if (e.GetCurrentPoint (this).Properties.IsLeftButtonPressed)
					BeginMoveDrag (e);
			};
			TitleBarRow.DoubleTapped += (s, e) => ToggleMaximize ();
		}

		// The placement convention (mac left, Windows/Linux right) is handled in
		// OnOpened by re-parenting the caption buttons to the requested side; the
		// XAML default places them on the right, matching Linux and Windows.
		Opened += (s, e) => {
			if (IsMac)
				MoveCaptionButtonsLeft ();
			ApplyThemeVariant (Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark);

			// Automated QA: open the requested dialog directly.
			var qa = Program.QaDialogArg;
			if (qa == "--prefs") {
				new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			} else if (qa.StartsWith ("--prefs=", StringComparison.Ordinal)) {
				var arg = qa.Substring ("--prefs=".Length);
				var dlg = new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
				dlg.ShowDialog (this);
				if (arg is "light" or "dark")
					Application.Current!.RequestedThemeVariant =
						arg == "light" ? ThemeVariant.Light : ThemeVariant.Dark;
				else
					dlg.SelectPanel (arg);
			} else if (qa == "--about") {
				new AboutDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			} else if (qa == "--addins") {
				new AddinManagerDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			}

			// Load a real solution into the Solution pad when requested (--sln=<path>).
			var slnArg = Program.SolutionArg;
			if (slnArg.Length > 0)
				LoadSolution (slnArg);
		};

		// Double-click a project in the Solution pad opens its .csproj tab in the editor.
		if (SolutionList is not null)
			SolutionList.DoubleTapped += OnSolutionOpen;
	}

	void LoadSolution (string path)
	{
		try {
			var loaded = Services.SolutionLoader.Load (path);
			if (loaded is null) {
				StatusText.Text = "Failed to load solution: " + path;
				return;
			}
			var (title, projects) = loaded.Value;
			var items = new System.Collections.ObjectModel.ObservableCollection<string> {
				$"Solution '{title}' ({projects.Count (p => !p.IsFolder)} project(s))"
			};
			foreach (var p in projects) {
				var indent = p.Parent is null ? "" : "    ";
				var icon = p.IsFolder ? "[f]" : "[p]";
				items.Add ($"{indent}{icon} {p.Name}");
			}
			SolutionList!.ItemsSource = items;
			StatusText.Text = "Loaded " + Path.GetFileName (path);
		} catch (Exception ex) {
			StatusText.Text = "Error loading solution: " + ex.Message;
		}
	}

	void OnSolutionOpen (object? sender, RoutedEventArgs e)
	{
		if (SolutionList?.SelectedItem is string sel && sel.EndsWith (".csproj", StringComparison.Ordinal))
			StatusText.Text = "Open: " + sel.Trim ();
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

	// ----- Menu actions surfaced for MenuService -----

	public void OnAboutMenu ()
		=> OnAbout (this, new RoutedEventArgs ());

	public void OnPreferencesMenu ()
		=> OnPreferences (this, new RoutedEventArgs ());

	public void OnAddinManagerMenu ()
		=> OnAddinManager (this, new RoutedEventArgs ());

	public void ToggleFullScreen ()
		=> WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;

	// Commands still pending port: report like the unported option panels instead of
	// silently hiding the legacy feature.
	public void OnMenuCommand (string commandId)
	{
		var message = $"'{commandId}' is not wired in the new UI yet — its GTK implementation remains available through --old-gui until the cutover.";
		Output ("[menu] " + message);
		Console.WriteLine ("[menu] " + message);
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
