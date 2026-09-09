using Avalonia.Controls;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class MainWindow : Window
{
	public MainWindow ()
	{
		InitializeComponent ();
		Output ("MonoDevelop Avalonia shell initialized.");
	}

	void OnTogglePad (object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

	void OnAbout (object? sender, Avalonia.Interactivity.RoutedEventArgs e)
		=> new AboutDialog ().ShowDialog (this);

	void OnPreferences (object? sender, Avalonia.Interactivity.RoutedEventArgs e)
		=> new PreferencesDialog ().ShowDialog (this);

	void OnAddinManager (object? sender, Avalonia.Interactivity.RoutedEventArgs e)
		=> new AddinManagerDialog ().ShowDialog (this);

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