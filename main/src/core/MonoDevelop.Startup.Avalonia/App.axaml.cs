using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell;

public partial class App : Application
{
	public override void Initialize ()
	{
		AvaloniaXamlLoader.Load (this);
	}

	public override void OnFrameworkInitializationCompleted ()
	{
		// Apply the stored User Interface Theme (legacy key MonoDevelop.Ide.UserInterfaceTheme:
		// "" = System/Default, "Dark", "Light"). Default follows the OS light/dark setting.
		RequestedThemeVariant = Views.PreferencesDialog.ThemeVariantFor (
			MonoDevelop.Ide.Services.SettingsStore.GetString ("MonoDevelop.Ide.UserInterfaceTheme"));

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.MainWindow = new MainWindow ();
		}

		base.OnFrameworkInitializationCompleted ();
	}
}