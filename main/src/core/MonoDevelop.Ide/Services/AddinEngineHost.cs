using System;
using System.IO;
using System.Linq;
using Mono.Addins;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Hosts the Mono.Addins engine for the Avalonia shell: one lazily-created
/// <see cref="AddinEngine"/> whose registry is refreshed so extension points
/// (e.g. /MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels,
/// /MonoDevelop/Ide/GlobalOptionsDialog) resolve like in the GTK IDE. Mirrors
/// Runtime.GetAddinRegistryLocation + UserProfile.ForUnix, and is shared with the
/// Add-in Manager dialog so the registry is only initialised once.
/// </summary>
public static class AddinEngineHost
{
	static AddinEngine? engine;
	static bool initialized;

	/// <summary>Startup directory scanned for the add-in catalog (next to the host binaries).</summary>
	public static string HostDirectory { get; private set; } = "";

	/// <summary>Ensures the add-in registry is loaded. Safe to call repeatedly.</summary>
	public static bool EnsureInitialized ()
	{
		if (initialized)
			return engine is not null;
		initialized = true;
		try {
			var home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty (home))
				home = Environment.GetEnvironmentVariable ("HOME") ?? "";

			var devConfig = Environment.GetEnvironmentVariable ("MONODEVELOP_DEV_CONFIG");
			var devAddins = Environment.GetEnvironmentVariable ("MONODEVELOP_DEV_ADDINS");

			var appId = Path.Combine ("MonoDevelop", "9.0");
			var configDir = devConfig is { Length: > 0 } ? devConfig : Path.Combine (home, ".config", appId);
			var addinsDir = devAddins is { Length: > 0 } ? devAddins : Path.Combine (home, ".local", "share", appId, "LocalInstall", "Addins");
			var databaseDir = devAddins is { Length: > 0 } ? devAddins : Path.Combine (home, ".cache", appId);

			// The IDE registers its add-ins via .addins files next to the host binaries,
			// so the scan starts there to share the IDE's catalog.
			var shellDir = Path.GetDirectoryName (typeof (AddinEngineHost).Assembly.Location);
			var hostDir = Path.GetFullPath (Path.Combine (shellDir ?? ".", "..", "..", "..", "..", "..", "..", "build"));
			if (!Directory.Exists (hostDir))
				hostDir = shellDir ?? Directory.GetCurrentDirectory ();
			HostDirectory = hostDir;
			Console.WriteLine ($"[addins] startupDirectory={hostDir}");
			// Initialise the static AddinManager facade so AddinManager.GetExtensionNodes works.
			try { AddinManager.Initialize (configDir, addinsDir); } catch (Exception ex) { Console.WriteLine ("[addins] AddinManager.Initialize: " + ex.Message); }
			engine = new AddinEngine ();
			engine.Initialize (configDir, addinsDir, databaseDir, hostDir);
			engine.Registry.Update (new HostProgressStatus ());
			try {
				int polPanels = 0;
				foreach (var _ in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels"))
					polPanels++;
				int globalOpts = 0;
				foreach (var _ in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/GlobalOptionsDialog"))
					globalOpts++;
				Console.WriteLine ($"[addins] extension nodes: MimeTypePolicyPanels={polPanels} GlobalOptionsDialog={globalOpts}");
			} catch (Exception ex) { Console.WriteLine ("[addins] extension node probe failed: " + ex.Message); }
			return true;
		} catch (Exception ex) {
			Console.WriteLine ("[addins] registry init failed: " + ex.Message);
			engine = null;
			return false;
		}
	}

	/// <summary>Shared engine (null until <see cref="EnsureInitialized"/> runs).</summary>
	public static AddinEngine? Engine => engine;

	// Minimal console progress monitor for the registry refresh.
	sealed class HostProgressStatus : Mono.Addins.IProgressStatus
	{
		bool cancelled;
		public bool IsCanceled => cancelled;
		public int LogLevel { get; set; }
		public void Cancel () => cancelled = true;
		public void SetMessage (string msg) => ReportMessage (msg);
		public void SetProgress (double progress) { }
		public void ReportError (string message) => Console.WriteLine ("[addins] ERROR: " + message);
		public void ReportError (string message, Exception exception) => Console.WriteLine ("[addins] ERROR: " + message + " " + exception?.Message);
		public void ReportWarning (string message) => Console.WriteLine ("[addins] WARN: " + message);
		public void ReportMessage (string message) => Console.WriteLine ("[addins] " + message);
		public void Log (string msg) => Console.WriteLine ("[addins] " + msg);
	}
}
