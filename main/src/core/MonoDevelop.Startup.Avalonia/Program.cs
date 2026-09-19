using System;
using System.Linq;
using Avalonia;

namespace MonoDevelop.AvaloniaShell;

internal static class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called.
	[STAThread]
	public static void Main (string [] args)
		=> BuildAvaloniaApp ().StartWithClassicDesktopLifetime (args);

	// QA hooks: --about | --prefs | --addins open the corresponding dialog at startup
	// so automated runs can validate each migrated dialog without UI navigation.
	// --prefs also accepts a value (--prefs=light|dark|<panelId>) to drive the dialog
	// programmatically in automated runs.
	public static string QaDialogArg =>
		Environment.GetCommandLineArgs ().FirstOrDefault (a =>
			a is "--about" or "--prefs" or "--addins"
			|| a.StartsWith ("--prefs=", StringComparison.Ordinal)) ?? "";

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp ()
		=> AppBuilder.Configure<App> ()
			.UsePlatformDetect ()
			.WithInterFont ()
			.LogToTrace ();
}