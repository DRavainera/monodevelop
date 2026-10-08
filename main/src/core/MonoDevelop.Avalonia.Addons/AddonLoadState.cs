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

        /// <summary>Absolute path of the <c>*.avaloniaaddon.json</c> that produced this state
        /// (empty for states created without a manifest file). Lets the host resolve add-in
        /// assets — e.g. the identity icon — relative to the add-in folder.</summary>
        public string ManifestPath { get; set; } = "";

        public void MarkLoaded()
        {
            // Loaded mirrors the error state: a failed add-in stays unloaded
            // (its extension nodes must not reach the UI).
            Loaded = Error is null;
            if (Loaded)
                LoadTime = DateTime.Now;
        }

        public void MarkFailed(string error)
        {
            Loaded = false;
            Error = error;
        }
    }
}