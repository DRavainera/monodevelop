using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell;

public partial class App : Application
{
	/// <summary>Add-ins for the Avalonia shell (loaded from Program.AddonsDirectory).</summary>
	public static MonoDevelop.AvaloniaAddons.AddonHost? Addins { get; private set; }

	public override void Initialize ()
	{
		AvaloniaXamlLoader.Load (this);
	}

	public override void OnFrameworkInitializationCompleted ()
	{
		// New Avalonia add-in host (Mono.Addins stays with the GTK IDE).
		var addons = new MonoDevelop.AvaloniaAddons.AddonHost (Program.AddonsDirectory);
		addons.Discover ();
		addons.LoadAll ();
		Addins = addons;
		Console.WriteLine ($"[avalonia-addons] root={addons.RootDirectory} discovered={addons.Addons.Count}");
		foreach (var a in addons.Addons)
			Console.WriteLine ($"[avalonia-addons] {a.Manifest.Identity.Id} v{a.Manifest.Identity.Version} loaded={a.Loaded}{(a.Error is null ? "" : " error=" + a.Error)}");
		foreach (var path in addons.Extensions.ExtensionPoints)
			Console.WriteLine ($"[avalonia-addons] ep {path} nodes={addons.Extensions.GetExtensionNodes (path).Count}");


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