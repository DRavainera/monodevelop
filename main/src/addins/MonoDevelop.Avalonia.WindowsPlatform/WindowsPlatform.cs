using System;
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.Core;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.Platform
{
	// Windows platform implementation for the Avalonia shell.
	// Registered through the /MonoDevelop/Core/PlatformService extension node
	// (DesktopService reads that extension point on initialization); compiled
	// only on Windows (see the OS condition in the .csproj files).
	public class WindowsPlatform : PlatformService, IAvaloniaAddon
	{
		public string Id => "MonoDevelop.Avalonia.WindowsPlatform";

		public void Initialize (IAddonContext context)
		{
			context.Log ("WindowsPlatform initialized");
		}

		public void Load ()
		{
		}

		public void Unload ()
		{
		}

		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			return Array.Empty<DesktopApplication> ();
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			return "application/octet-stream";
		}

		protected override string OnGetMimeTypeDescription (string mimeType)
		{
			return mimeType;
		}

		public override void ShowUrl (string url)
		{
			try {
				Process.Start (new ProcessStartInfo { FileName = url, UseShellExecute = true });
			} catch (Exception ex) {
				LoggingService.LogError ($"Failed to open URL '{url}': {ex.Message}");
			}
		}

		public override string DefaultMonospaceFont {
			get { return "Consolas"; }
		}

		public override string Name {
			get { return "Windows"; }
		}

		protected override string OnGetIconIdForFile (string filename)
		{
			return "generic-file";
		}

		protected override Xwt.Drawing.Image OnGetIconForFile (string filename)
		{
			return null!;
		}
	}
}
