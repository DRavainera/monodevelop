using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell;

public partial class App : Application
{
	/// <summary>Add-ins for the Avalonia shell (loaded from Program.AddonsDirectory).</summary>
	public static MonoDevelop.AvaloniaAddons.AddonHost? Addins { get; private set; }

	/// <summary>Platform hooks contributed by the host's platform add-in
	/// (Mac/Windows/LinuxPlatform via IPlatformHooks); null when the add-in tree
	/// has no platform add-in for this OS. The shell routes its desktop
	/// operations (open URL/folder, default monospace face) through it.</summary>
	public static MonoDevelop.AvaloniaAddons.IPlatformHooks? Platform { get; private set; }

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
		// Platform hooks: the loaded platform add-in is the shell's platform
		// backend (the host gates it by the manifest "os" field, so at most one
		// registers per OS).
		Platform = addons.Parts.OfType <MonoDevelop.AvaloniaAddons.IPlatformHooks> ().FirstOrDefault ();
		Console.WriteLine ($"[avalonia-addons] root={addons.RootDirectory} discovered={addons.Addons.Count}");
		Console.WriteLine ($"[avalonia-addons] platform={Platform?.Name ?? "(none)"} monospace={Platform?.DefaultMonospaceFont ?? "-"}");
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