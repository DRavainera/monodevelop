using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonoDevelop.AvaloniaAddons.Manifests
{
	/// <summary>
	/// Add-in manifest, shaped after the Visual Studio <c>extension.vsixmanifest</c>
	/// (identity / assetType / installationTarget / tags / categories / dependencies)
	/// so the model is familiar, plus the shell-specific pieces (autoLoad, extensions).
	/// Serialized as JSON on disk (<c>&lt;id&gt;.avaloniaaddon.json</c>).
	/// </summary>
	public sealed class AddonManifest
	{
		[JsonPropertyName ("identity")]
		public AddonIdentity Identity { get; set; } = new ();

		[JsonPropertyName ("assetType")]
		public string AssetType { get; set; } = "Microsoft.VisualStudio.MefComponent";

		[JsonPropertyName ("installationTarget")]
		public AddonInstallationTarget InstallationTarget { get; set; } = new ();

		[JsonPropertyName ("tags")]
		public List<string> Tags { get; set; } = new ();

		[JsonPropertyName ("categories")]
		public List<string> Categories { get; set; } = new ();

		[JsonPropertyName ("dependencies")]
		public List<AddonDependency> Dependencies { get; set; } = new ();

		/// <summary>Type implementing <see cref="IAvaloniaAddon"/> loaded from the add-in assembly.</summary>
		[JsonPropertyName ("entryPoint")]
		public string EntryPoint { get; set; }

		[JsonPropertyName ("autoLoad")]
		public bool AutoLoad { get; set; } = true;

		/// <summary>Extension nodes per extension point path (e.g. /MonoDevelop/Ide/GlobalOptionsDialog).</summary>
		[JsonPropertyName ("extensions")]
		public Dictionary<string, List<AddonExtensionNode>> Extensions { get; set; } = new ();
	}

	public sealed class AddonIdentity
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "";
		[JsonPropertyName ("name")] public string Name { get; set; } = "";
		[JsonPropertyName ("publisher")] public string Publisher { get; set; } = "";
		[JsonPropertyName ("version")] public string Version { get; set; } = "1.0.0";
		[JsonPropertyName ("description")] public string Description { get; set; } = "";
	}

	public sealed class AddonInstallationTarget
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "MonoDevelop.Avalonia";
		[JsonPropertyName ("version")] public string Version { get; set; } = "";
	}

	public sealed class AddonDependency
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "";
		[JsonPropertyName ("version")] public string Version { get; set; } = "";
	}

	/// <summary>One contributed node (section/panel) for an extension point.</summary>
	public sealed class AddonExtensionNode
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "";
		[JsonPropertyName ("label")] public string Label { get; set; } = "";
		[JsonPropertyName ("icon")] public string Icon { get; set; }
		[JsonPropertyName ("class")] public string Class { get; set; }
		[JsonPropertyName ("childId")] public string ChildId { get; set; }
	}
}
