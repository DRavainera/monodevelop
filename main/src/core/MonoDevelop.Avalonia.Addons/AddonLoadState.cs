using MonoDevelop.AvaloniaAddons.Manifests;
using System;

namespace MonoDevelop.AvaloniaAddons
{
    /// <summary>
    /// State tracking for an add-in during loading.
    /// </summary>
    public sealed class AddonLoadState
    {
        public AddonManifest Manifest { get; set; }
        public AddonLoadContext Context { get; set; }
        public bool Loaded { get; private set; }
        public string Error { get; set; }
        public DateTime LoadTime { get; private set; }

        public void MarkLoaded()
        {
            Loaded = true;
            LoadTime = DateTime.Now;
        }

        public void MarkFailed(string error)
        {
            Loaded = false;
            Error = error;
        }
    }
}