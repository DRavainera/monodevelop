using System;
using System.Diagnostics;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.Core;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.Platform
{
	// Windows platform implementation for the Avalonia shell. The host composes it
	// as an add-in and the shell resolves it as the platform backend through
	// IPlatformHooks (App.Platform) — it does not touch the GTK DesktopService,
	// which belongs to the legacy --old-gui path. URLs open through shell execute,
	// folders through explorer. Compiled only on Windows (see
	// Directory.Build.targets).
	public class WindowsPlatform : PlatformService, IAvaloniaAddon, IPlatformHooks
	{
		public string Id => "MonoDevelop.Avalonia.WindowsPlatform";

		public void Initialize (IAddonContext context)
		{
			if (!System.OperatingSystem.IsWindows ())
				return;
			context.Log ("WindowsPlatform ready");
		}

		public void Load ()
		{
		}

		public void Unload ()
		{
		}

		public override string DefaultMonospaceFont => "Consolas 10";

		public override string Name => "Windows";

		public override void ShowUrl (string url)
		{
			try {
				Process.Start (new ProcessStartInfo {
					FileName = url,
					UseShellExecute = true,
				});
			} catch (Exception ex) {
				LoggingService.LogError ($"Failed to open URL '{url}': {ex.Message}");
			}
		}

		public override void OpenFolder (FilePath folderPath, FilePath [] selectFiles)
		{
			try {
				Process.Start (new ProcessStartInfo {
					FileName = "explorer.exe",
					Arguments = $"\"{folderPath}\"",
					UseShellExecute = true,
				});
			} catch (Exception ex) {
				LoggingService.LogError ($"Failed to open folder '{folderPath}': {ex.Message}");
			}
		}

		// IPlatformHooks surface for the shell (the base OpenFolder takes a FilePath).
		public void OpenFolder (string path) => OpenFolder (new FilePath (path), new FilePath [0]);
	}
}
