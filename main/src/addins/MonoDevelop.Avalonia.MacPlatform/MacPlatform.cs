using System;
using System.Diagnostics;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.Core;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.Platform
{
	// Mac platform implementation for the Avalonia shell. The host composes it as
	// an add-in and the shell resolves it as the platform backend through
	// IPlatformHooks (App.Platform) — it does not touch the GTK DesktopService,
	// which belongs to the legacy --old-gui path. `open` is the portable way to
	// open URLs/folders without the ObjC bindings. Compiled only on macOS
	// (see Directory.Build.targets).
	public class MacPlatform : PlatformService, IAvaloniaAddon, IPlatformHooks
	{
		public string Id => "MonoDevelop.Avalonia.MacPlatform";

		public void Initialize (IAddonContext context)
		{
			if (!System.OperatingSystem.IsMacOS ())
				return;
			context.Log ("MacPlatform ready");
		}

		public void Load ()
		{
		}

		public void Unload ()
		{
		}

		public override string DefaultMonospaceFont => "Menlo 12";

		public override string Name => "Mac";

		public override void ShowUrl (string url)
		{
			StartOpen (url);
		}

		public override void OpenFolder (FilePath folderPath, FilePath [] selectFiles)
		{
			StartOpen (folderPath.ToString ());
		}

		// IPlatformHooks surface for the shell (the base OpenFolder takes a FilePath).
		public void OpenFolder (string path) => OpenFolder (new FilePath (path), new FilePath [0]);

		static void StartOpen (string target)
		{
			try {
				Process.Start (new ProcessStartInfo {
					FileName = "open",
					Arguments = $"\"{target}\"",
					UseShellExecute = false,
					CreateNoWindow = true,
				});
			} catch (Exception ex) {
				LoggingService.LogError ($"Failed to open '{target}': {ex.Message}");
			}
		}
	}
}
