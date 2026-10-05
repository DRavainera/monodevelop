using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.AvaloniaAddons;

namespace MonoDevelop.Platform
{
    public class WindowsPlatform : PlatformService, IAvaloniaAddon
    {
        public string Id => "MonoDevelop.Avalonia.WindowsPlatform";

        public void Initialize(IAddonContext context)
        {
            DesktopService.SetPlatformService(this);
        }

        public void Load() { }
        public void Unload() { }

        public override IEnumerable<DesktopApplication> GetApplications(string filename)
        {
            return Array.Empty<DesktopApplication>();
        }

        protected override string OnGetMimeTypeForUri(string uri)
        {
            return "application/octet-stream";
        }

        protected override string OnGetMimeTypeDescription(string mimeType)
        {
            return mimeType;
        }

        public override void ShowUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex) { LoggingService.LogError("Failed to open URL", ex); }
        }

        public override string DefaultMonospaceFont => "Consolas";
        public override string Name => "Windows";

        protected override string OnGetIconIdForFile(string filename)
        {
            return "generic-file";
        }

        protected override Xwt.Drawing.Image OnGetIconForFile(string filename) => null;

        public override bool IsWindows => true;
        public override bool IsMac => false;
        public override bool IsLinux => false;
    }
}