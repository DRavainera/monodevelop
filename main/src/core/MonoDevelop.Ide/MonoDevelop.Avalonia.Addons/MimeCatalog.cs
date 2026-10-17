using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonoDevelop.AvaloniaAddons
{
	/// <summary>One mime type of the catalog: id, description, stock icon id, base type,
	/// whether it is a text format and the file patterns it supports. Mirrors the legacy
	/// MimeTypeNode (minus the Roslyn content type, which the Avalonia shell does not use).</summary>
	public sealed class MimeEntry
	{
		public MimeEntry (string id, string description, string icon, string baseType, bool isText, string[] patterns)
		{
			Id = id;
			Description = description ?? "";
			Icon = icon ?? "";
			BaseType = baseType ?? "";
			IsText = isText;
			Patterns = patterns ?? Array.Empty<string> ();
		}

		public string Id { get; }
		public string Description { get; }
		public string Icon { get; }
		public string BaseType { get; }
		public bool IsText { get; }
		public string[] Patterns { get; }
	}

	/// <summary>
	/// Mime/icon catalog of the Avalonia shell, behind the platform hooks
	/// (<see cref="IPlatformHooks"/>). Built from a static table that mirrors the core
	/// legacy declarations (ExtensionModel/MimeTypes.addin.xml) plus common modern
	/// formats, overlaid by the /MonoDevelop/Core/MimeTypes extension nodes the add-ins
	/// declare in their manifests (the same source of truth as the legacy Mono.Addins
	/// catalog, so e.g. CSharpBinding keeps owning text/x-csharp). The overlay wins over
	/// the built-ins, mirroring the legacy extension point's contract. The shell wires
	/// the add-in extension registry with <see cref="SetExtensionRegistry"/>; without
	/// one the catalog still answers from its built-ins.
	/// </summary>
	public static class MimeCatalog
	{
		public const string TextPlain = "text/plain";
		public const string ApplicationXml = "application/xml";
		public const string OctetStream = "application/octet-stream";

		public const string MimeTypesPath = "/MonoDevelop/Core/MimeTypes";

		// Same default-icon behaviour as the legacy PlatformService: text/plain has a
		// dedicated icon, everything unmatched falls back to the regular file icon.
		const string TextIconId = "md-text-file-icon";
		const string DefaultIconId = "md-regular-file";

		readonly static object sync = new ();
		static IAddonExtensionRegistry registry;
		static bool cacheBuilt;
		static int cachedVersion = -1;
		static Dictionary<string, MimeEntry> byId = new (StringComparer.Ordinal);
		static Dictionary<string, string> extToId = new (StringComparer.OrdinalIgnoreCase);
		static Dictionary<string, string> nameToId = new (StringComparer.OrdinalIgnoreCase);

		// Core legacy table (ExtensionModel/MimeTypes.addin.xml) + common modern
		// formats. Icons are core stock ids resolvable by the shell's IconService.
		// text/x-csharp / application/x-csproject are NOT here: they are owned by the
		// CSharpBinding add-in, exactly as in the legacy tree.
		readonly static MimeEntry[] builtins = {
			new MimeEntry (TextPlain, "Text file", "", "", true, Array.Empty<string> ()),
			new MimeEntry (OctetStream, "Unknown", "", "", false, Array.Empty<string> ()),
			new MimeEntry ("application/xml", "XML document", "md-xml-file-icon", "", true, new [] { "*.xml" }),
			new MimeEntry ("application/x-resource", ".NET binary resource", "md-resource-file-icon", "", false, new [] { "*.resources" }),
			new MimeEntry ("text/microsoft-resx", ".NET XML resource", "md-resource-file-icon", ApplicationXml, true, new [] { "*.resx" }),
			new MimeEntry ("application/x-addin+xml", "Add-in manifest", "", ApplicationXml, false, new [] { "*.addin.xml", "*.addin" }),
			new MimeEntry ("application/png", "PNG resource", "md-file-image", "", false, new [] { "*.png" }),
			new MimeEntry ("image/jpeg", "JPEG image", "md-file-image", "", false, new [] { "*.jpg", "*.jpeg" }),
			new MimeEntry ("image/gif", "GIF image", "md-file-image", "", false, new [] { "*.gif" }),
			new MimeEntry ("image/svg+xml", "SVG image", "md-file-image", ApplicationXml, false, new [] { "*.svg" }),
			new MimeEntry ("image/x-icon", "Icon image", "md-file-image", "", false, new [] { "*.ico" }),
			new MimeEntry ("text/css", "CSS document", "md-file-css", "", true, new [] { "*.css" }),
			new MimeEntry ("text/less", "Less stylesheet", "md-file-less", "", true, new [] { "*.less" }),
			new MimeEntry ("text/x-scss", "SCSS stylesheet", "md-file-scss", "", true, new [] { "*.scss", "*.sass" }),
			new MimeEntry ("text/html", "HTML document", "md-html-file-icon", "", true, new [] { "*.html", "*.htm" }),
			new MimeEntry ("text/javascript", "JavaScript", "md-file-js", "", true, new [] { "*.js", "*.mjs" }),
			new MimeEntry ("text/typescript", "TypeScript", "md-file-ts", "", true, new [] { "*.ts", "*.tsx" }),
			new MimeEntry ("application/config+xml", "Configuration File", "", ApplicationXml, false, new [] { "*.config" }),
			new MimeEntry ("application/glade+xml", "Glade File", "", ApplicationXml, false, new [] { "*.glade" }),
			new MimeEntry ("application/manifest+xml", "Manifest File", "", ApplicationXml, false, new [] { "*.manifest" }),
			new MimeEntry ("text/x-changelog", "ChangeLog document", "", "", true, new [] { "ChangeLog|Changelog|CHANGELOG", "*.ChangeLog" }),
			new MimeEntry ("application/x-executable", ".NET program", "md-empty-file-icon", "", false, new [] { "*.exe", "*.dll" }),
			new MimeEntry ("application/x-sln", "MonoDevelop/MSBuild Solution", "md-solution", "", true, new [] { "*.sln" }),
			new MimeEntry ("application/x-solution-filter", "Solution filter", "md-solution", "", true, new [] { "*.slnf" }),
			new MimeEntry ("application/x-mds", "MonoDevelop 1.0 Solution", "md-solution", ApplicationXml, false, new [] { "*.mds" }),
			new MimeEntry ("text/x-patch", "diff files", "", "", true, new [] { "*.patch", "*.diff" }),
			new MimeEntry ("text/x-makefile", "Makefile", "", "", true, new [] { "Makefile|Makefile.am|Makefile.in" }),
			new MimeEntry ("text/x-lua", "lua files", "", "", true, new [] { "*.lua" }),
			new MimeEntry ("text/x-ilasm", "il files", "", "", true, new [] { "*.il" }),
			new MimeEntry ("text/x-jay", "jay files", "", "", true, new [] { "*.jay" }),
			new MimeEntry ("text/x-cgsrc", "Cg Shader files", "", "", true, new [] { "*.cg" }),
			new MimeEntry ("text/x-apple-strings", "Apple Strings", "", "", true, new [] { "*.strings" }),
			new MimeEntry ("text/x-sql", "sql files", "", "", true, new [] { "*.sql" }),
			new MimeEntry ("application/json", "JSON files", "", "", true, new [] { "*.json" }),
			new MimeEntry ("application/x-sh", "Shell script", "md-script-file-icon", "", true, new [] { "*.sh" }),
			new MimeEntry ("text/x-fsharp", "F# files", "md-file-source", "", true, new [] { "*.fs", "*.fsx", "*.fsi" }),
			new MimeEntry ("application/x-msbuild", "MSBuild files", "", ApplicationXml, false, new [] { "*.targets", "*.proj", "*.props", "*.tasks", "*.overridetasks", "*.projitems" }),
			new MimeEntry ("text/x-python", "Python source", "md-file-source", "", true, new [] { "*.py" }),
			new MimeEntry ("text/x-java", "Java source", "md-file-source", "", true, new [] { "*.java" }),
			new MimeEntry ("text/x-go", "Go source", "md-file-source", "", true, new [] { "*.go" }),
			new MimeEntry ("text/x-rust", "Rust source", "md-file-source", "", true, new [] { "*.rs" }),
			new MimeEntry ("text/x-ruby", "Ruby source", "md-file-source", "", true, new [] { "*.rb" }),
			new MimeEntry ("text/x-c", "C source", "md-file-source", "", true, new [] { "*.c" }),
			new MimeEntry ("text/x-c++", "C++ source", "md-file-source", "", true, new [] { "*.cpp", "*.cc", "*.cxx" }),
			new MimeEntry ("text/x-chdr", "C/C++ header", "md-file-header", "", true, new [] { "*.h", "*.hpp" }),
			new MimeEntry ("text/markdown", "Markdown document", "md-text-file-icon", "", true, new [] { "*.md", "*.markdown" }),
			new MimeEntry ("application/zip", "ZIP archive", "", "", false, new [] { "*.zip" }),
			new MimeEntry ("application/x-nuget", "NuGet package", "md-package-source", "", false, new [] { "*.nupkg" }),
		};

		/// <summary>Wires the add-in extension registry so the catalog merges the
		/// /MonoDevelop/Core/MimeTypes nodes the loaded add-ins declare. Called by the
		/// shell once after LoadAll; passing null detaches (built-ins only).</summary>
		public static void SetExtensionRegistry (IAddonExtensionRegistry extensionRegistry)
		{
			lock (sync) {
				registry = extensionRegistry;
				cacheBuilt = false;
				cachedVersion = -1;
			}
		}

		/// <summary>The mime type of a file (by name pattern); falls back to
		/// text/plain for existing text files and application/octet-stream otherwise,
		/// mirroring the legacy PlatformService tail.</summary>
		public static string GetMimeTypeForFile (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				return OctetStream;
			lock (sync) {
				EnsureLocked ();
				var file = Path.GetFileName (fileName);
				var ext = Path.GetExtension (file);
				if (ext is { Length: > 0 } && extToId.TryGetValue (ext, out var byExt))
					return byExt;
				if (nameToId.TryGetValue (file, out var byName))
					return byName;
			}
			try {
				if (Path.IsPathRooted (fileName) && File.Exists (fileName) && !LooksLikeBinary (fileName))
					return TextPlain;
			} catch (UnauthorizedAccessException) {
			} catch (IOException) {
			}
			return OctetStream;
		}

		/// <summary>Human description of a mime type (legacy GetMimeTypeDescription).</summary>
		public static string GetMimeTypeDescription (string mimeType)
		{
			if (mimeType == TextPlain)
				return "Text file";
			if (mimeType == OctetStream)
				return "Unknown";
			lock (sync) {
				EnsureLocked ();
				return FindEntryLocked (mimeType)?.Description ?? "";
			}
		}

		/// <summary>Stock icon id for a file (legacy GetIconIdForFile + GetIconForType
		/// chain, without the Xwt image step the shell resolves itself).</summary>
		public static string GetIconIdForFile (string fileName)
			=> GetIconIdForType (GetMimeTypeForFile (fileName));

		/// <summary>Stock icon id for a mime type: walks the inheritance chain until a
		/// type with an icon (text/plain and the terminal types have fixed icons).</summary>
		public static string GetIconIdForType (string mimeType)
		{
			lock (sync) {
				EnsureLocked ();
				if (string.IsNullOrEmpty (mimeType))
					return DefaultIconId;
				foreach (var id in InheritanceChainLocked (mimeType)) {
					if (id == TextPlain)
						return TextIconId;
					if (id == OctetStream)
						return DefaultIconId;
					var icon = FindEntryLocked (id)?.Icon;
					if (icon is { Length: > 0 })
						return icon;
				}
				return DefaultIconId;
			}
		}

		/// <summary>True when the mime type inherits from text/plain.</summary>
		public static bool IsText (string mimeType)
		{
			lock (sync) {
				EnsureLocked ();
				return InheritanceChainLocked (mimeType).Contains (TextPlain);
			}
		}

		/// <summary>The entry registered under the given id (overlay first), or null.</summary>
		public static MimeEntry FindEntry (string id)
		{
			lock (sync) {
				EnsureLocked ();
				return FindEntryLocked (id);
			}
		}

		static MimeEntry FindEntryLocked (string id)
			=> id is not null && byId.TryGetValue (id, out var e) ? e : null;

		/// <summary>Inheritance chain of a mime type (self first): explicit baseType,
		/// then isText/text/* to text/plain, *+xml to application/xml, stopping at the
		/// terminal types. Mirrors the legacy MimeTypeCatalog chain.</summary>
		static List<string> InheritanceChainLocked (string mimeType)
		{
			var chain = new List<string> { mimeType };
			var current = mimeType;
			for (int guard = 0; guard < 32 && current is not null; guard++) {
				if (current is TextPlain or OctetStream)
					break;
				var next = EffectiveBaseLocked (current);
				if (next is null || chain.Contains (next))
					break;
				chain.Add (next);
				current = next;
			}
			return chain;
		}

		// Legacy MimeTypeNode.BaseType semantics: explicit baseType wins; otherwise a
		// text format bases on text/plain, a *+xml type bases on application/xml.
		static string EffectiveBaseLocked (string id)
		{
			var e = FindEntryLocked (id);
			if (e?.BaseType is { Length: > 0 })
				return e.BaseType;
			if (e is { IsText: true } || id.StartsWith ("text/", StringComparison.Ordinal))
				return TextPlain;
			if (id.EndsWith ("+xml", StringComparison.Ordinal))
				return ApplicationXml;
			return null;
		}

		static void EnsureLocked ()
		{
			var v = registry is null ? -1 : registry.Version;
			if (cacheBuilt && v == cachedVersion)
				return;
			var byId = new Dictionary<string, MimeEntry> (StringComparer.Ordinal);
			var extToId = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			var nameToId = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			foreach (var e in BuildOverlay ().Concat (builtins)) {
				if (e.Id is { Length: 0 } || byId.ContainsKey (e.Id))
					continue; // overlay wins over built-ins; first definition wins
				byId [e.Id] = e;
				foreach (var pattern in e.Patterns) {
					foreach (var alt in pattern.Split ('|')) {
						var a = alt.Trim ();
						if (a.Length == 0)
							continue;
						if (a.StartsWith ("*.", StringComparison.Ordinal))
							extToId.TryAdd (a.Substring (1), e.Id);
						else
							nameToId.TryAdd (a, e.Id);
					}
				}
			}
			MimeCatalog.byId = byId;
			MimeCatalog.extToId = extToId;
			MimeCatalog.nameToId = nameToId;
			cacheBuilt = true;
			cachedVersion = v;
		}

		static MimeEntry[] BuildOverlay ()
		{
			if (registry is null)
				return Array.Empty<MimeEntry> ();
			var list = new List<MimeEntry> ();
			foreach (var node in registry.GetExtensionNodes (MimeTypesPath)) {
				if (node?.Id is not { Length: > 0 })
					continue;
				// Newer manifests list fileExtensions; older ones carry a single
				// legacy "pattern".
				var patterns = (node.FileExtensions ?? new List<string> ())
					.Where (p => p is { Length: > 0 })
					.ToList ();
				if (patterns.Count == 0 && node.Pattern is { Length: > 0 })
					patterns.Add (node.Pattern);
				list.Add (new MimeEntry (node.Id, node.Label, node.Icon, node.BaseType, node.IsText, patterns.ToArray ()));
			}
			return list.ToArray ();
		}

		// Binary sniff (legacy TextFileUtility.IsBinary contract): a NUL byte in the
		// first 4 KiB marks the file as binary.
		static bool LooksLikeBinary (string path)
		{
			using var fs = File.OpenRead (path);
			var buf = new byte[4096];
			var n = fs.Read (buf, 0, buf.Length);
			for (var i = 0; i < n; i++)
				if (buf [i] == 0)
					return true;
			return false;
		}
	}
}
