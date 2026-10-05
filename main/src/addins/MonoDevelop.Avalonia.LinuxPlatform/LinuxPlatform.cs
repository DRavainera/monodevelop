using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MonoDevelop.AvaloniaAddons;

namespace MonoDevelop.Platform
{
    /// <summary>
    /// Linux platform implementation for Avalonia shell.
    /// Basic implementation compatible with the addin system.
    /// </summary>
    public class LinuxPlatform : IAvaloniaAddon
    {
        public string Id => "MonoDevelop.Avalonia.LinuxPlatform";

        public void Initialize(IAddonContext context)
        {
            // Initialize Linux platform service
            TryRegisterPlatformService();
        }

        public void Load()
        {
            Log("LinuxPlatform loaded");
        }

        public void Unload()
        {
            Log("LinuxPlatform unloaded");
        }

        private void TryRegisterPlatformService()
        {
            try
            {
                // Try to create and register the platform service
                var platformService = CreatePlatformService();
                if (platformService != null)
                {
                    // Use reflection to access DesktopService if available
                    RegisterPlatformService(platformService);
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to register platform service: {ex.Message}");
            }
        }

        private object CreatePlatformService()
        {
            // Create a basic platform service implementation
            // This will be expanded when MonoDevelop.Ide assemblies are available
            return new BasicLinuxPlatformService();
        }

        private void RegisterPlatformService(object platformService)
        {
            try
            {
                // Use reflection to find and call DesktopService.SetPlatformService
                var desktopServiceType = Type.GetType("MonoDevelop.Ide.Desktop.DesktopService, MonoDevelop.Ide");
                if (desktopServiceType != null)
                {
                    var setPlatformServiceMethod = desktopServiceType.GetMethod("SetPlatformService");
                    if (setPlatformServiceMethod != null)
                    {
                        setPlatformServiceMethod.Invoke(null, new[] { platformService });
                        Log("Platform service registered successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to register platform service via reflection: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            Console.WriteLine($"[LinuxPlatform] {message}");
        }

        // Basic platform service implementation
        private class BasicLinuxPlatformService
        {
            public string DefaultMonospaceFont => "Monospace 10";
            public string Name => "Linux";
            public bool CanOpenTerminal => true;
            public bool IsWindows => false;
            public bool IsMac => false;
            public bool IsLinux => true;
            
            public string GetDesktopEnvironment()
            {
                string desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.ToUpperInvariant() ??
                               Environment.GetEnvironmentVariable("DESKTOP_SESSION")?.ToUpperInvariant() ??
                               string.Empty;

                return desktop switch
                {
                    string s when s.Contains("GNOME") => "GNOME",
                    string s when s.Contains("KDE") => "KDE",
                    string s when s.Contains("XFCE") => "XFCE",
                    string s when s.Contains("LXDE") => "LXDE",
                    string s when s.Contains("MATE") => "MATE",
                    string s when s.Contains("CINNAMON") => "CINNAMON",
                    string s when s.Contains("BUDGIE") => "BUDGIE",
                    string s when s.Contains("UNITY") => "UNITY",
                    _ => "Unknown"
                };
            }

            public void ShowUrl(string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = url,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LinuxPlatform] Failed to open URL '{url}': {ex.Message}");
                }
            }
        }
    }
}