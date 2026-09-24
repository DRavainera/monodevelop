using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;

namespace MonoDevelop.AvaloniaShell;

internal static class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called.
	[STAThread]
	public static int Main (string [] args)
	{
		// Legacy GTK UI compatibility switch: there is a single project with a
		// single build tree (main/build); the GTK# UI is kept as hidden legacy
		// compatibility during the 9.x branch and only shows up with --old-gui,
		// which relays this invocation to the GTK (net10.0) runtime staged next
		// to the Avalonia shell.
		if (args.Any (a => a == "--old-gui"))
			return LaunchLegacyGtk (args);

		// UI language from the legacy preference (same MonoDevelopProperties.xml the
		// GTK UI reads), applied before any string is built.
		MonoDevelop.AvaloniaShell.Services.GettextService.Initialize ();
		BuildAvaloniaApp ().StartWithClassicDesktopLifetime (StripOldGui (args));
		return 0;
	}

	static string [] StripOldGui (string [] args)
		=> args.Where (a => a != "--old-gui").ToArray ();

	// Runs the GTK legacy UI (main/build/net10run or main/build/bin/net10.0) as a
	// child process and forwards its exit code, so the Avalonia binary is the only
	// entrypoint users need to know about.
	static int LaunchLegacyGtk (string [] args)
	{
		string? gtkDll = null;
		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 6 && dir is not null && gtkDll is null; i++) {
			foreach (var candidate in new [] {
				Path.Combine (dir, "net10run", "MonoDevelop.dll"),
				Path.Combine (dir, "bin", "net10.0", "MonoDevelop.dll"),
				Path.Combine (dir, "MonoDevelop.dll"),
			}) {
				if (File.Exists (candidate)) {
					gtkDll = candidate;
					break;
				}
			}
			dir = Path.GetDirectoryName (dir);
		}
		if (gtkDll is null) {
			Console.Error.WriteLine ("--old-gui: legacy GTK runtime not found next to the Avalonia build (looked for net10run/MonoDevelop.dll and bin/net10.0/MonoDevelop.dll under main/build).");
			return 1;
		}
		var dotnet = Environment.ProcessPath;
		if (string.IsNullOrEmpty (dotnet))
			dotnet = "dotnet";
		var psi = new ProcessStartInfo {
			FileName = dotnet,
			UseShellExecute = false,
		};
		psi.ArgumentList.Add (gtkDll);
		foreach (var a in StripOldGui (args))
			psi.ArgumentList.Add (a);
		using var proc = Process.Start (psi);
		if (proc is null)
			return 1;
		proc.WaitForExit ();
		return proc.ExitCode;
	}

	// QA hooks: --about | --prefs | --addins open the corresponding dialog at startup
	// so automated runs can validate each migrated dialog without UI navigation.
	// --prefs also accepts a value (--prefs=light|dark|<panelId>) to drive the dialog
	// programmatically in automated runs.
	// Skips value-carrying args like --sln=<path> first so the FIRST QA flag wins.
	public static string QaDialogArg =>
		Environment.GetCommandLineArgs ().SkipWhile (a => a.StartsWith ("--sln=", StringComparison.Ordinal))
			.FirstOrDefault (a =>
				a is "--about" or "--prefs" or "--addins" or "--find" or "--build" or "--run"
					or "--goto" or "--tasks" or "--tool" or "--editops" or "--windocs" or "--navhist"
					or "--bookmarks" or "--addref" or "--brace" or "--buildone" or "--mcaret" or "--fmt" or "--diff" or "--fold" or "--viewcmds" or "--bubbles" or "--compl" or "--tool" or "--ctxmenu" or "--filter" or "--props" or "--dirtyfiles" or "--editqa" or "--totd" or "--progress" or "--encodings" or "--newconfig" or "--newconfig-real" or "--openimport" or "--activeconfig" or "--newproject" or "--bmkpad" or "--bkpad"
				|| a.StartsWith ("--prefs=", StringComparison.Ordinal)
				|| a.StartsWith ("--gotoline", StringComparison.Ordinal)) ?? "";

	// Optional path of a solution to open at startup (--sln=<path>); the Solution
	// pad is then populated with the real projects of that solution.
	public static string SolutionArg =>
		Environment.GetCommandLineArgs ().FirstOrDefault (a => a.StartsWith ("--sln=", StringComparison.Ordinal)) is { } arg
			? arg.Substring ("--sln=".Length) : "";

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp ()
		=> AppBuilder.Configure<App> ()
			.UsePlatformDetect ()
			.WithInterFont ()
			.LogToTrace ();
}
