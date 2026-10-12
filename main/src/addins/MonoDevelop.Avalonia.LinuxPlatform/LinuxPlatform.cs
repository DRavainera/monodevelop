using System;
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.Platform
{
	/// <summary>
	/// Linux platform implementation for the Avalonia shell (GnomePlatform
	/// successor): xdg-open for URLs and folders, the desktop terminal
	/// (x-terminal-emulator), and the default monospace face. The host composes
	/// it as an add-in and the shell resolves it as the platform backend through
	/// <see cref="IPlatformHooks"/> (App.Platform) — it does not touch the GTK
	/// DesktopService, which belongs to the legacy --old-gui path.
	/// </summary>
	public class LinuxPlatform : PlatformService, IAvaloniaAddon, IPlatformHooks
	{
		public string Id => "MonoDevelop.Avalonia.LinuxPlatform";

		public void Initialize (IAddonContext context)
		{
			if (!System.OperatingSystem.IsLinux ())
				return;
			context.Log ("LinuxPlatform ready");
		}

		public void Load ()
		{
		}

		public void Unload ()
		{
		}

		public override string DefaultMonospaceFont => "Monospace 11";

		public override string Name => "Linux";

		public override void ShowUrl (string url)
		{
			StartProcess ("xdg-open", url);
		}

		public override void OpenFolder (FilePath folderPath, FilePath [] selectFiles)
		{
			StartProcess ("xdg-open", folderPath.ToString ());
		}

		// IPlatformHooks surface for the shell (the base OpenFolder takes a FilePath).
		public void OpenFolder (string path) => OpenFolder (new FilePath (path), new FilePath [0]);

		public override bool CanOpenTerminal => true;

		public override void OpenTerminal (FilePath directory, IDictionary<string, string> environmentVariables, string title)
		{
			var dir = directory.ToString ();
			var psi = new ProcessStartInfo ("x-terminal-emulator") {
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = string.IsNullOrEmpty (dir) ? Environment.CurrentDirectory : dir,
			};
			if (environmentVariables != null)
				foreach (var env in environmentVariables)
					psi.EnvironmentVariables [env.Key] = env.Value;
			try {
				Process.Start (psi);
			} catch (Exception ex) {
				LoggingService.LogError ($"Failed to open a terminal: {ex.Message}");
			}
		}

		// Plain console process (the GnomePlatform gnome-terminal-server app-id dance
		// is desktop-specific and not needed here); pauseWhenFinished is ignored.
		public override ProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables, string title, bool pauseWhenFinished)
		{
			var proc = new ProcessWrapper ();
			proc.StartInfo = new ProcessStartInfo (command, arguments) {
				CreateNoWindow = true,
				UseShellExecute = false,
				WorkingDirectory = workingDirectory,
			};
			if (environmentVariables != null)
				foreach (var env in environmentVariables)
					proc.StartInfo.EnvironmentVariables [env.Key] = env.Value;
			proc.Start ();
			return proc.ProcessAsyncOperation;
		}

		static void StartProcess (string fileName, string arguments)
		{
			try {
				Process.Start (new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					CreateNoWindow = true,
				});
			} catch (Exception ex) {
				LoggingService.LogError ($"Failed to run '{fileName} {arguments}': {ex.Message}");
			}
		}
	}
}
