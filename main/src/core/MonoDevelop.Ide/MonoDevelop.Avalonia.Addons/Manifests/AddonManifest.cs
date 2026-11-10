using System;
using System.Collections.Generic;
using System.Text.Json;
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

		/// <summary>OS gate: empty = all OSes; "linux" | "macos" | "windows" loads the
		/// add-in only on that OS (the host skips it elsewhere with a clear state
		/// error). Needed for the platform add-ins, whose DLLs are cross-compiled and
		/// present in the build tree of every OS.</summary>
		[JsonPropertyName ("os")]
		public string Os { get; set; } = "";

		[JsonPropertyName ("assets")]
		public List<AddonAsset> Assets { get; set; } = new ();

		[JsonPropertyName ("extensions")]
		public Dictionary<string, List<AddonExtensionNode>> Extensions { get; set; } = new ();

		/// <summary>Set when the add-in was imported from the Visual Studio Marketplace
		/// (see <c>Marketplace.VsixImporter</c>): the source package id and links, so the
		/// manager's "More information" can point at the gallery listing. Null for the
		/// shipped/user add-ins that are not marketplace imports.</summary>
		[JsonPropertyName ("marketplace")]
		public AddonMarketplaceSource Marketplace { get; set; }
	}

	public sealed class AddonMarketplaceSource
	{
		/// <summary>The marketplace listing id ("publisher.name", the query's
		/// "publisher.name" / filterType-7 key) this add-in was imported from. It is the
		/// stable tie between a gallery row (whose id is exactly this value) and the
		/// installed add-in, so the manager can de-duplicate an installed marketplace
		/// extension out of the gallery. Distinct from <see cref="PackageId"/> (the
		/// package's own "publisher.vsixId") and from the identity id (the vsixmanifest
		/// "Identity Id"), which are different naming spaces.</summary>
		[JsonPropertyName ("listingId")]
		public string ListingId { get; set; } = "";

		[JsonPropertyName ("packageId")]
		public string PackageId { get; set; } = "";

		[JsonPropertyName ("galleryUrl")]
		public string GalleryUrl { get; set; } = "";

		[JsonPropertyName ("publisher")]
		public string Publisher { get; set; } = "";

		[JsonPropertyName ("isVisualStudioDesktop")]
		public bool IsVisualStudioDesktop { get; set; }
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

		// /MonoDevelop/Core/MimeTypes nodes (legacy MimeTypeNode shape): the mime
		// type's base type, whether it is a text format and the file patterns it
		// supports ("*.cs" globs or bare names like "Makefile", "|" separated).
		// Older manifests carry a single legacy "pattern" and a comma-separated
		// "fileExtensions" string, so the converter accepts both shapes.
		[JsonPropertyName ("baseType")] public string BaseType { get; set; } = "";
		[JsonPropertyName ("isText")] public bool IsText { get; set; }
		[JsonPropertyName ("pattern")] public string Pattern { get; set; } = "";
		[JsonPropertyName ("fileExtensions")]
		[JsonConverter (typeof (FileExtensionsJsonConverter))]
		public List<string> FileExtensions { get; set; } = new ();
	}

	/// <summary>Accepts the file-pattern list either as a JSON array of strings or as
	/// a (comma-separated) string, the two shapes the shipped manifests use. Writes
	/// the canonical array-of-strings shape (used when the marketplace importer
	/// serializes a generated manifest).</summary>
	public sealed class FileExtensionsJsonConverter : JsonConverter<List<string>>
	{
		public override List<string> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var list = new List<string> ();
			if (reader.TokenType == JsonTokenType.Null)
				return list;
			if (reader.TokenType == JsonTokenType.String) {
				var raw = reader.GetString () ?? "";
				foreach (var part in raw.Split (',')) {
					var p = part.Trim ();
					if (p.Length > 0)
						list.Add (p);
				}
				return list;
			}
			while (reader.Read () && reader.TokenType != JsonTokenType.EndArray) {
				if (reader.TokenType == JsonTokenType.String) {
					var p = reader.GetString ();
					if (p is { Length: > 0 })
						list.Add (p);
				}
			}
			return list;
		}

		public override void Write (Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
		{
			// Empty → an empty array (not null) so the round-trip is stable.
			writer.WriteStartArray ();
			foreach (var p in value ?? new List<string> ())
				writer.WriteStringValue (p);
			writer.WriteEndArray ();
		}
	}
}