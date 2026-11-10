using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonoDevelop.AvaloniaAddons.Marketplace
{
	/// <summary>
	/// One extension returned by the Visual Studio Marketplace gallery API
	/// (marketplace.visualstudio.com/_apis/public/gallery/extensionquery), reduced to
	/// the fields the shell's Add-in Manager Gallery tab needs. The wire shape is the
	/// gallery's <c>results[0].extensions[]</c>; <see cref="MarketplaceClient"/> maps it.
	/// </summary>
	public sealed class MarketplaceExtension
	{
		/// <summary>The gallery's stable id, "publisher.name" (e.g. "ms-dotnettools.csharp").</summary>
		[JsonPropertyName ("extensionId")]
		public string ExtensionId { get; set; } = "";

		/// <summary>Publisher display name (e.g. "Microsoft").</summary>
		[JsonPropertyName ("publisherName")]
		public string PublisherName { get; set; } = "";

		/// <summary>Gallery package name (e.g. "csharp").</summary>
		[JsonPropertyName ("extensionName")]
		public string ExtensionName { get; set; } = "";

		[JsonPropertyName ("displayName")]
		public string DisplayName { get; set; } = "";

		/// <summary>Newest published version (from versions[0].version).</summary>
		[JsonPropertyName ("version")]
		public string Version { get; set; } = "";

		/// <summary>ISO-8601 last-updated date (gallery's lastUpdated).</summary>
		[JsonPropertyName ("lastUpdated")]
		public string LastUpdated { get; set; } = "";

		[JsonPropertyName ("shortDescription")]
		public string ShortDescription { get; set; } = "";

		[JsonPropertyName ("fullDescription")]
		public string FullDescription { get; set; } = "";

		/// <summary>Categories (gallery "Categories" asset, comma-separated on the wire).</summary>
		[JsonPropertyName ("categories")]
		public List<string> Categories { get; set; } = new ();

		[JsonPropertyName ("tags")]
		public List<string> Tags { get; set; } = new ();

		/// <summary>Install/downloads count when the gallery reports it.</summary>
		[JsonPropertyName ("installCount")]
		public long InstallCount { get; set; }

		/// <summary>Gallery icon URL (Microsoft.VisualStudio.Services.Icons.Default).</summary>
		[JsonPropertyName ("iconUrl")]
		public string IconUrl { get; set; } = "";

		/// <summary>Direct .vsix package URL (Microsoft.VisualStudio.Services.VSIXPackage).</summary>
		[JsonPropertyName ("packageUrl")]
		public string PackageUrl { get; set; } = "";

		/// <summary>Whether the package targets the Visual Studio desktop host
		/// (installationTarget "Microsoft.VisualStudio"). VS Code extensions
		/// ("Microsoft.VisualStudio.Code") are surfaced but flagged as not a
		/// Visual Studio (desktop) extension.</summary>
		[JsonPropertyName ("isVisualStudioDesktop")]
		public bool IsVisualStudioDesktop { get; set; }

		/// <summary>Installation target id(s) parsed from the vsixmanifest when
		/// available, otherwise empty (the gallery query does not always expose it).</summary>
		[JsonPropertyName ("installationTargets")]
		public List<string> InstallationTargets { get; set; } = new ();

		public string Title => string.IsNullOrEmpty (DisplayName) ? ExtensionName : DisplayName;
		public string QualifiedName => PublisherName + "." + ExtensionName;
	}

	/// <summary>
	/// The gallery's wire shape (results[0]) — internal to the client; the client maps
	/// it to <see cref="MarketplaceExtension"/>. Kept as a POCO so System.Text.Json
	/// deserialises the raw document.
	/// </summary>
	internal sealed class GalleryQueryResponse
	{
		[JsonPropertyName ("results")]
		public List<GalleryResult> Results { get; set; } = new ();
	}

	internal sealed class GalleryResult
	{
		[JsonPropertyName ("extensions")]
		public List<GalleryExtension> Extensions { get; set; } = new ();
	}

	internal sealed class GalleryExtension
	{
		[JsonPropertyName ("publisher")]
		public GalleryPublisher Publisher { get; set; }

		[JsonPropertyName ("extensionName")]
		public string ExtensionName { get; set; } = "";

		[JsonPropertyName ("displayName")]
		public string DisplayName { get; set; } = "";

		[JsonPropertyName ("shortDescription")]
		public string ShortDescription { get; set; } = "";

		[JsonPropertyName ("lastUpdated")]
		public string LastUpdated { get; set; } = "";

		[JsonPropertyName ("assets")]
		public List<GalleryAsset> Assets { get; set; } = new ();

		[JsonPropertyName ("versions")]
		public List<GalleryVersion> Versions { get; set; } = new ();

		[JsonPropertyName ("statistics")]
		public List<GalleryStatistic> Statistics { get; set; } = new ();
	}

	internal sealed class GalleryPublisher
	{
		[JsonPropertyName ("publisherName")]
		public string PublisherName { get; set; } = "";
	}

	internal sealed class GalleryAsset
	{
		[JsonPropertyName ("assetType")]
		public string AssetType { get; set; } = "";

		[JsonPropertyName ("source")]
		public string Source { get; set; } = "";
	}

	internal sealed class GalleryVersion
	{
		[JsonPropertyName ("version")]
		public string Version { get; set; } = "";

		[JsonPropertyName ("files")]
		public List<GalleryAsset> Files { get; set; } = new ();
	}

	internal sealed class GalleryStatistic
	{
		[JsonPropertyName ("statisticName")]
		public string StatisticName { get; set; } = "";

		// The gallery reports statistic values as JSON numbers that may be
		// fractional (e.g. 2297815.0); read as double and truncate to a count.
		[JsonPropertyName ("value")]
		public double Value { get; set; }
	}
}
