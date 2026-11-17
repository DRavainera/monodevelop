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

		// The node's enable condition, mirroring the legacy add-in <Condition>
		// attribute. It is a boolean expression the shell's extension consumers
		// (e.g. the per-project options dialog) evaluate against a project context.
		// Before this field existed the value was silently dropped, so every
		// conditional node looked unconditional.
		//
		// Grammar observed in the real *.avaloniaaddon.json manifests (a subset of
		// MSBuild conditions, NOT the full MSBuild condition grammar):
		//   - a clause is "Key=Value" or a bare "Key" (true when the key holds);
		//   - a value may carry "|"-separated alternatives (OR) and a "!" prefix
		//     (negation), e.g. "ItemType=Solution|Project", "Platform=!windows";
		//   - a clause may be negated with the word "NOT" in front of it,
		//     e.g. "... AND NOT ItemType=MonoDevelop.Packaging.PackagingProject";
		//   - clauses are conjoined with "," or the word "AND";
		//   - a clause group may be wrapped in parentheses,
		//     e.g. "(ItemType=DotNetProject|Packaging) AND ProjectHasNuGetMetadata".
		// Values never contain a top-level "," (verified across the tree), which is
		// what makes the clause split below unambiguous. The full MSBuild condition
		// grammar (relational ops, functions, $() property refs) is intentionally NOT
		// supported here: P1 should evaluate a condition with
		// MonoDevelop.Projects.MSBuild.Conditions.ConditionParser.ParseAndEvaluate
		// against a project context rather than re-deriving it from these parts.
		[JsonPropertyName ("condition")] public string Condition { get; set; } = "";

		/// <summary>Decomposes <see cref="Condition"/> into its conjoined clauses,
		/// each as an ordered (Key, Value) pair, honoring the grammar above: the
		/// top-level "," / "AND" split keeps parenthesized groups intact, a bare key
		/// (no "=") yields a null Value, and a Value keeps any "|"/"!" tokens verbatim
		/// for the consumer to interpret. Returns an empty list when there is no
		/// condition. This is a structural decomposition only — it does NOT evaluate
		/// the expression.</summary>
		public List<(string Key, string? Value)> GetConditionParts ()
		{
			var parts = new List<(string, string?)> ();
			if (string.IsNullOrWhiteSpace (Condition))
				return parts;
			foreach (var raw in SplitConjunction (Condition)) {
				var p = StripOuterParens (raw);
				if (p.Length == 0)
					continue;
				var i = p.IndexOf ('=');
				if (i < 0)
					parts.Add ((p, null));
				else
					parts.Add ((p.Substring (0, i).Trim (), p.Substring (i + 1).Trim ()));
			}
			return parts;
		}

		// Splits a condition on its top-level "," and " AND " separators, keeping
		// parenthesized groups together (their inner separators are not top-level).
		static IEnumerable<string> SplitConjunction (string cond)
		{
			var depth = 0;
			var start = 0;
			for (var i = 0; i < cond.Length; i++) {
				var c = cond [i];
				if (c == '(') {
					depth++;
					continue;
				}
				if (c == ')') {
					if (depth > 0)
						depth--;
					continue;
				}
				if (depth != 0)
					continue;
				if (c == ',') {
					yield return cond.Substring (start, i - start);
					start = i + 1;
					continue;
				}
				// The " AND " word conjunction: split at the space before the word
				// (the value and the following clause are space-separated, so "AND"
				// inside an identifier cannot trigger it).
				if (c == 'A' && i > 0 && cond [i - 1] == ' ' &&
					i + 3 < cond.Length && cond.Substring (i, 3) == "AND" &&
					cond [i + 3] == ' ') {
					yield return cond.Substring (start, i - 1 - start);
					i += 4; // skip "AND "
					start = i;
					continue;
				}
			}
			yield return cond.Substring (start);
		}

		// Strips balanced parentheses that wrap the whole string, one layer at a
		// time (e.g. "(A=1|B=2)" -> "A=1|B=2"); inner parens are left alone.
		static string StripOuterParens (string s)
		{
			s = s.Trim ();
			while (s.Length >= 2 && s [0] == '(' && s [s.Length - 1] == ')') {
				var depth = 0;
				var closeAt = -1;
				for (var j = 0; j < s.Length; j++) {
					if (s [j] == '(')
						depth++;
					else if (s [j] == ')') {
						depth--;
						if (depth == 0) {
							closeAt = j;
							break;
						}
					}
				}
				if (closeAt != s.Length - 1)
					break;
				s = s.Substring (1, s.Length - 2).Trim ();
			}
			return s;
		}

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