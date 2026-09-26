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

		// Keep the SkiaSharp fontconfig font manager from hanging on user
		// web fonts (WOFF/WOFF2) BEFORE any Avalonia/Skia type initializes.
		MonoDevelop.Ide.Services.FontconfigSanitizer.Apply ();

		// UI language from the legacy preference (same MonoDevelopProperties.xml the
		// GTK UI reads), applied before any string is built.
		MonoDevelop.Ide.Services.GettextService.Initialize ();
		ArmStartupWatchdog ();
		BuildAvaloniaApp ().StartWithClassicDesktopLifetime (StripOldGui (args));
		return 0;
	}

	static string [] StripOldGui (string [] args)
		=> args.Where (a => a != "--old-gui").ToArray ();

	// Startup watchdog (early detection instead of a silent hang): Avalonia
	// init can wedge inside the SkiaSharp fontconfig font manager (a
	// SkFontMgr_fontconfig loop over user WOFF/WOFF2 font directories — see
	// docs/interfaz-plan.md §M16f) burning 100% CPU BEFORE any window
	// exists. If the main window has not opened within the budget (default
	// 30s, MD_STARTUP_WATCHDOG overrides), print the known-cause diagnostic
	// and exit fast.
	public static readonly System.Threading.CancellationTokenSource StartupWatchdogDone
		= new System.Threading.CancellationTokenSource ();

	static void ArmStartupWatchdog ()
	{
		// First sanitizer run builds the private fontconfig cache from scratch
		// (every system + user font is scanned once); give it room by default.
		int fallback = MonoDevelop.Ide.Services.FontconfigSanitizer.FirstRun ? 120 : 30;
		int seconds = int.TryParse (Environment.GetEnvironmentVariable ("MD_STARTUP_WATCHDOG"), out var s) && s > 0 ? s : fallback;
		_ = System.Threading.Tasks.Task.Run (async () => {
			try {
				await System.Threading.Tasks.Task.Delay (seconds * 1000, StartupWatchdogDone.Token);
			} catch (OperationCanceledException) {
				return; // started fine — disarm
			}
			Console.Error.WriteLine ($"[fatal] The UI did not start within {seconds}s.");
			Console.Error.WriteLine ("[fatal] Known cause: user fonts with WOFF/WOFF2 directories (~/.local/share/fonts) trip an infinite loop in SkiaSharp's fontconfig font manager (SkFontMgr_fontconfig::GetFamilyNames -> FcPatternGetString). Upstream fix: bound the family scan / skip non-TT containers.");
			Console.Error.WriteLine ("[fatal] Workaround: run with FONTCONFIG_FILE pointing to a config that only includes system fonts, e.g. the recipe in docs/interfaz-plan.md (M16f).");
			Environment.Exit (2);
		});
	}

	// Runs the GTK legacy UI (MonoDevelop.dll, staged in the same main/build tree
	// as this Avalonia shell) as a child process and forwards its exit code, so the
	// Avalonia binary is the only entrypoint users need to know about.
	static int LaunchLegacyGtk (string [] args)
	{
		string? gtkDll = null;
		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 6 && dir is not null && gtkDll is null; i++) {
			// Single build tree: the GTK MonoDevelop.dll is staged in the SAME main/build
			// directory as this Avalonia shell.
			var candidate = Path.Combine (dir, "MonoDevelop.dll");
			if (File.Exists (candidate)) {
				gtkDll = candidate;
				break;
			}
			dir = Path.GetDirectoryName (dir);
		}
		if (gtkDll is null) {
			Console.Error.WriteLine ("--old-gui: legacy GTK runtime not found next to the Avalonia build (looked for MonoDevelop.dll under main/build).");
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
		// The GTK MonoDevelop.dll refuses to start on its own (hidden legacy UI
		// guard keyed on MONODEVELOP_LEGACY_UI): only this relay, invoked with
		// --old-gui, sets that environment variable for the child process.
		psi.EnvironmentVariables ["MONODEVELOP_LEGACY_UI"] = "1";
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
					or "--bookmarks" or "--addref" or "--brace" or "--buildone" or "--mcaret" or "--fmt" or "--diff" or "--fold" or "--viewcmds" or "--bubbles" or "--compl" or "--tool" or "--ctxmenu" or "--filter" or "--props" or "--dirtyfiles" or "--editqa" or "--totd" or "--progress" or "--encodings" or "--newconfig" or "--newconfig-real" or "--openimport" or "--activeconfig" or "--newproject" or "--bmkpad" or "--bkpad" or "--locals" or "--attachdlg" or "--watch" or "--condbp" or "--attachreal" or "--step" or "--tree" or "--imm"
						or "--gutterbp" or "--frame" or "--immcompl" or "--persistqa" or "--watchedit" or "--pinwatch" or "--legacyqa"
				|| a.StartsWith ("--prefs=", StringComparison.Ordinal)
				|| a.StartsWith ("--gotoline", StringComparison.Ordinal)) ?? "";

	// Optional path of a solution to open at startup (--sln=<path>); the Solution
	// pad is then populated with the real projects of that solution.
	public static string SolutionArg =>
		Environment.GetCommandLineArgs ().FirstOrDefault (a => a.StartsWith ("--sln=", StringComparison.Ordinal)) is { } arg
			? arg.Substring ("--sln=".Length) : "";

	// Avalonia configuration, don't remove; also used by visual designer.
	// WithInterFont was dropped: it embeds a bundled font that Skia must
	// register through the same fontconfig manager that hangs on web fonts —
	// the system fonts (plus the sanitizer rejects) provide coverage.
	public static AppBuilder BuildAvaloniaApp ()
		=> AppBuilder.Configure<App> ()
			.UsePlatformDetect ()
			.LogToTrace ();
}
