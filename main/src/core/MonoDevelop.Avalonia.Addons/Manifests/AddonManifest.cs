using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonoDevelop.AvaloniaAddons.Manifests
{
	/// <summary>Add-in manifest, shaped after the Visual Studio <c>extension.vsixmanifest</c>
	/// (identity / assetType / installationTarget / tags / categories / dependencies) 
	/// so the model is familiar, plus the shell-specific pieces (autoLoad, extensions).
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

		[JsonPropertyName ("entryPoint")]
		public string EntryPoint { get; set; }

		[JsonPropertyName ("autoLoad")]
		public bool AutoLoad { get; set; } = true;

		[JsonPropertyName ("assets")]
		public List<AddonAsset> Assets { get; set; } = new ();

		[JsonPropertyName ("extensions")]
		public Dictionary<string, List<AddonExtensionNode>> Extensions { get; set; } = new ();
	}

	public sealed class AddonIdentity
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "";
		[JsonPropertyName ("displayName")] public string DisplayName { get; set; } = "";
		[JsonPropertyName ("name")] public string Name { get; set; } = "";
		[JsonPropertyName ("publisher")] public string Publisher { get; set; } = "";
		[JsonPropertyName ("version")] public string Version { get; set; } = "1.0.0";
		[JsonPropertyName ("description")] public string Description { get; set; } = "";
		[JsonPropertyName ("icon")] public string Icon { get; set; } = "";
		[JsonPropertyName ("preview")] public bool Preview { get; set; } = false;
		[JsonPropertyName ("language")] public string Language { get; set; } = "en-US";
	}

	public sealed class AddonInstallationTarget
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "MonoDevelop.Avalonia";
		[JsonPropertyName ("version")] public string Version { get; set; } = "";
	}

	public sealed class AddonDependency
	{
		[JsonPropertyName ("id")] public string Id { get; set; } = "";
		[JsonPropertyName ("versionRange")] public string VersionRange { get; set; } = "[*]";
	}

	public sealed class AddonAsset
	{
		[JsonPropertyName ("type")] public string Type { get; set; } = "Microsoft.VisualStudio.MefComponent";
		[JsonPropertyName ("src")] public string Src { get; set; }
		[JsonPropertyName ("assembly")] public string Assembly { get; set; }
		[JsonPropertyName ("className")] public string ClassName { get; set; }
	}

	public sealed class AddonExtensionNode
	{ 
		[JsonPropertyName ("id")] public string Id { get; set; } = "";
		[JsonPropertyName ("label")] public string Label { get; set; } = "";
		[JsonPropertyName ("icon")] public string Icon { get; set; } = "";
		[JsonPropertyName ("className")] public string ClassName { get; set; }
		[JsonPropertyName ("childId")] public string ChildId { get; set; }
	}
}