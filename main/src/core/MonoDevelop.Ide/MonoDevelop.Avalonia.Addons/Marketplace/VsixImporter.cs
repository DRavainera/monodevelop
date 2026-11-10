using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using MonoDevelop.AvaloniaAddons.Manifests;

namespace MonoDevelop.AvaloniaAddons.Marketplace
{
	/// <summary>
	/// Importer that turns a downloaded Visual Studio extension package (<c>.vsix</c>)
	/// into a shell add-in and installs it through the host's user root.
	///
	/// A .vsix is a zip carrying <c>extension.vsixmanifest</c> (root) plus the payload
	/// (typically <c>extension/</c>). The shell does NOT understand that manifest, so
	/// this importer:
	///   1. extracts the package to a temp folder,
	///   2. parses <c>extension.vsixmanifest</c> (<see cref="VsixManifest.Parse"/>),
	///   3. generates the shell's <c>*.avaloniaaddon.json</c> from it (the model
	///      <see cref="AddonManifest"/> is "shaped after" the VSIX manifest), and
	///   4. hands the staged folder to <see cref="AddonHost.InstallFromDirectory"/>,
	///      which copies it to the user root, registers and (re)loads it.
	///
	/// Honest limit (documented, operator-accepted): a Visual Studio *desktop*
	/// extension compiles against the VS SDK and its MEF contributions (IVsPackage,
	/// MEF commands/pads/options) are not statically readable from the package. The
	/// importer registers the package in the add-in model with its identity/metadata
	/// (so it appears in the manager and, if it ships a loadable .NET assembly that
	/// implements the shell's <c>IPackage</c>/<c>IAvaloniaAddon</c>, gets composed).
	/// The rich surfaces (Pads / Menús / Editor / Preferences) are integrated through
	/// the shell's extension-point model as each is migrated, not by auto-decoding the
	/// VS SDK assembly.
	/// </summary>
	public sealed class VsixImporter
	{
		readonly AddonHost host;

		public VsixImporter (AddonHost host)
		{
			this.host = host ?? throw new ArgumentNullException (nameof (host));
		}

		/// <summary>The result of an import (for the dialog / logs).</summary>
		public sealed class Result
		{
			public bool Success { get; init; }
			public string Error { get; init; } = "";
			/// <summary>The shell add-in id the package was installed as.</summary>
			public string AddinId { get; init; } = "";
			/// <summary>Install location (user root subfolder).</summary>
			public string InstallPath { get; init; } = "";
			/// <summary>Whether the package targets the Visual Studio desktop host.</summary>
			public bool IsVisualStudioDesktop { get; init; }
			/// <summary>Whether a loadable .NET entry assembly was detected.</summary>
			public bool HasEntryPoint { get; init; }
			/// <summary>Dependency ids declared by the package.</summary>
			public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string> ();
		}

		/// <summary>Imports a .vsix file. On success the package is staged, a shell
		/// manifest is generated, and it is installed into the user root.
		/// <paramref name="listingId"/> is the marketplace listing id
		/// ("publisher.name") the package came from; it is persisted so the manager can
		/// de-duplicate the installed add-in out of the gallery (see
		/// <see cref="Manifests.AddonMarketplaceSource.ListingId"/>).</summary>
		public Result Import (string vsixFile, string? listingId = null)
		{
			if (!File.Exists (vsixFile))
				return new Result { Success = false, Error = "file not found: " + vsixFile };
			if (!vsixFile.EndsWith (".vsix", StringComparison.OrdinalIgnoreCase))
				return new Result { Success = false, Error = "not a .vsix package" };

			var tmp = Path.Combine (Path.GetTempPath (), "md-avalonia-vsix-" + Guid.NewGuid ().ToString ("N"));
			try {
				ZipFile.ExtractToDirectory (vsixFile, tmp, overwriteFiles: true);

				var manifestPath = Path.Combine (tmp, "extension.vsixmanifest");
				if (!File.Exists (manifestPath))
					return new Result { Success = false, Error = "package has no extension.vsixmanifest" };

				VsixManifest vsix;
				try {
					vsix = VsixManifest.Parse (File.OpenRead (manifestPath));
				} catch (Exception ex) {
					return new Result { Success = false, Error = "invalid vsixmanifest: " + ex.Message };
				}
				if (string.IsNullOrEmpty (vsix.Id))
					return new Result { Success = false, Error = "vsixmanifest has no Identity Id" };

				var addon = BuildAddonManifest (vsix, tmp, listingId);
				var staged = StageForInstall (tmp, vsix, addon);

				// Install + immediate load/compose of the new add-in only (the surgical
				// path: it does NOT re-run Discover over the whole shipped+user set). A
				// Visual Studio desktop package whose assembly compiles against the VS
				// SDK will fail to load and surface as a clear per-add-in error (the
				// manager shows it greyed out, installable/uninstallable), while a
				// package shipping a .NET assembly that implements this shell's
				// IPackage/IAvaloniaAddon surface composes its extension points.
				if (!host.InstallFromDirectory (staged, out var error))
					return new Result {
						Success = false,
						Error = "install failed: " + error,
						AddinId = addon.Identity.Id,
						IsVisualStudioDesktop = vsix.IsVisualStudioDesktop,
						HasEntryPoint = addon.EntryPoint is { Length: > 0 },
						Dependencies = vsix.Dependencies,
					};

				return new Result {
					Success = true,
					AddinId = addon.Identity.Id,
					InstallPath = Path.Combine (host.UserRootDirectory ?? "", addon.Identity.Id),
					IsVisualStudioDesktop = vsix.IsVisualStudioDesktop,
					HasEntryPoint = addon.EntryPoint is { Length: > 0 },
					Dependencies = vsix.Dependencies,
				};
			} catch (Exception ex) {
				return new Result { Success = false, Error = ex.Message };
			} finally {
				TryDelete (tmp);
			}
		}

		/// <summary>Maps a parsed VSIX manifest onto the shell's <see cref="AddonManifest"/>.
		/// The generated manifest is identity/metadata-rich (so the manager shows a
		/// meaningful row) and declares a loadable entry point only when one is
		/// confidently detected (a VS desktop extension's SDK assemblies would fail to
		/// load, so we do not force an entry point for those).
		/// <paramref name="listingId"/> (the marketplace "publisher.name") is persisted so
		/// the manager can de-duplicate the installed add-in out of the gallery.</summary>
		Manifests.AddonManifest BuildAddonManifest (VsixManifest vsix, string extractDir, string? listingId)
		{
			var m = new AddonManifest {
				Identity = new AddonIdentity {
					Id = SanitizeId (vsix.Id),
					DisplayName = string.IsNullOrEmpty (vsix.DisplayName) ? vsix.Id : vsix.DisplayName,
					Name = vsix.Id,
					Publisher = vsix.Publisher,
					Version = string.IsNullOrEmpty (vsix.Version) ? "1.0.0" : vsix.Version,
					Description = vsix.Description ?? "",
					Language = string.IsNullOrEmpty (vsix.Language) ? "en-US" : vsix.Language,
					Icon = SanitizeIcon (vsix.Icon),
				},
				AssetType = "Microsoft.VisualStudio.MefComponent",
				InstallationTarget = new AddonInstallationTarget { Id = "MonoDevelop.Avalonia" },
				Tags = SplitList (vsix.Tags),
				Categories = SplitList (vsix.Categories),
				Dependencies = vsix.Dependencies
					.Select (d => new AddonDependency { Id = SplitDep (d) })
					.ToList (),
				// Remember where this came from so the manager's "More information"
				// links to the marketplace listing, the row can note its origin, and the
				// gallery can de-duplicate the installed add-in by its listing id.
				Marketplace = new AddonMarketplaceSource {
					ListingId = listingId ?? "",
					PackageId = vsix.Publisher is { Length: > 0 } p ? p + "." + vsix.Id : vsix.Id,
					GalleryUrl = GalleryUrlFor (vsix),
					Publisher = vsix.Publisher,
					IsVisualStudioDesktop = vsix.IsVisualStudioDesktop,
				},
			};

			// A VS Code package (Microsoft.VisualStudio.Code) is a JS/TS extension: no
			// .NET assembly the shell can compose → manifest-only (no entry point).
			// A VS desktop package may carry a main .NET assembly; detect it so the
			// host can attempt to compose it (it will load only if it targets the
			// shell's IPackage/IAvaloniaAddon surface).
			if (vsix.IsVisualStudioDesktop) {
				var entry = FindEntryPoint (extractDir, vsix.Id);
				if (entry is { Length: > 0 })
					m.EntryPoint = entry;
			}

			// Record the source package + targets as extension metadata (diagnostics).
			m.Extensions ["/Marketplace/Source"] = new List<AddonExtensionNode> {
				new AddonExtensionNode {
					Id = "source",
					Label = $"imported from Visual Studio Marketplace ({(vsix.IsVisualStudioDesktop ? "Visual Studio desktop" : "VS Code")} package)",
				},
			};
			return m;
		}

		/// <summary>Builds the folder handed to <see cref="AddonHost.InstallFromDirectory"/>:
		/// a single folder holding the generated <c>.avaloniaaddon.json</c> plus the
		/// package payload (so the host copies the manifest and the payload together to
		/// the user root).</summary>
		string StageForInstall (string extractDir, VsixManifest vsix, AddonManifest addon)
		{
			// The package payload already lives in extractDir (root: extension.vsixmanifest
			// + the payload folder). Writing the generated manifest into extractDir makes
			// InstallFromDirectory find it directly; the whole folder (manifest + payload)
			// is copied to the user root, so a manifest-relative entry-point path keeps
			// resolving after the copy.
			var manifestFile = Path.Combine (extractDir, addon.Identity.Id + ".avaloniaaddon.json");
			File.WriteAllText (manifestFile, JsonSerializer.Serialize (addon, new JsonSerializerOptions { WriteIndented = true }));
			return extractDir;
		}

		/// <summary>The marketplace listing URL for a package (items?itemName=publisher.id).</summary>
		static string GalleryUrlFor (VsixManifest vsix)
		{
			var itemName = vsix.Publisher is { Length: > 0 } p ? p + "." + vsix.Id : vsix.Id;
			return "https://marketplace.visualstudio.com/items?itemName=" + Uri.EscapeDataString (itemName);
		}

		/// <summary>Finds a loadable .NET assembly in the payload and returns its path
		/// RELATIVE to <paramref name="extractDir"/> (so it keeps resolving after the
		/// folder is copied to the user root). Prefers an assembly named after the package
		/// id; else a deterministic (name-ordered) first .dll that is not a satellite
		/// resource. The pick is a best-effort heuristic: a Visual Studio desktop package
		/// usually ships native/SDK assemblies, so the resulting entrypoint often fails to
		/// load and the add-in degrades to a clear per-add-in error state (by design, not
		/// a crash). Determinism matters more than which dll is picked: the manager must
		/// show a stable result across restarts.</summary>
		static string? FindEntryPoint (string extractDir, string id)
		{
			var candidates = new List<string> ();
			foreach (var dll in SafeEnumerateFiles (extractDir, "*.dll")) {
				var name = Path.GetFileNameWithoutExtension (dll);
				if (name.EndsWith (".resources", StringComparison.OrdinalIgnoreCase))
					continue;
				if (name.Contains ("x86") || name.Contains ("arm64"))
					continue;
				candidates.Add (dll);
			}
			// Prefer an assembly named after the package id (the extension's own dll).
			foreach (var dll in candidates)
				if (string.Equals (Path.GetFileNameWithoutExtension (dll), id, StringComparison.OrdinalIgnoreCase))
					return ToRelative (extractDir, dll);
			// Else the name-order first: deterministic across runs / filesystems.
			if (candidates.Count == 0)
				return null;
			var first = candidates
				.OrderBy (p => Path.GetFileName (p), StringComparer.OrdinalIgnoreCase)
				.ThenBy (p => p, StringComparer.OrdinalIgnoreCase)
				.First ();
			return ToRelative (extractDir, first);
		}

		static string ToRelative (string baseDir, string path)
		{
			try {
				var rel = Path.GetRelativePath (Path.GetFullPath (baseDir), Path.GetFullPath (path));
				return rel.Replace ('\\', '/');
			} catch {
				return Path.GetFileName (path);
			}
		}

		static IEnumerable<string> SafeEnumerateFiles (string dir, string pattern)
		{
			try {
				return Directory.EnumerateFiles (dir, pattern, SearchOption.AllDirectories)
					.Where (p => {
						var lower = p.Replace ('\\', '/').ToLowerInvariant ();
						return !lower.Contains ("/obj/") && !lower.Contains ("/bin/")
							&& !lower.Contains ("node_modules") && !lower.Contains ("/.vs/");
					});
			} catch {
				return Array.Empty<string> ();
			}
		}

		static string SanitizeId (string id)
		{
			// Keep the gallery id but make it safe for a folder/assembly name.
			return id.Replace (' ', '_');
		}

		static string SanitizeIcon (string icon)
		{
			// The vsixmanifest Icon is a path inside the package (e.g.
			// "extension/icon.png"); the host resolves icons relative to the add-in
			// folder or the add-ins root, so pass it through as a relative path.
			return icon ?? "";
		}

		static List<string> SplitList (string csv)
		{
			if (string.IsNullOrWhiteSpace (csv))
				return new List<string> ();
			return csv.Split (',', StringSplitOptions.RemoveEmptyEntries)
				.Select (s => s.Trim ())
				.Where (s => s.Length > 0)
				.ToList ();
		}

		/// <summary>A VSIX dependency id is "publisher.extension@version" → the shell
		/// dependency id is "publisher.extension".</summary>
		static string SplitDep (string id)
		{
			var idx = id.LastIndexOf ('@');
			return idx > 0 ? id.Substring (0, idx) : id;
		}

		static void TryDelete (string dir)
		{
			try { if (Directory.Exists (dir)) Directory.Delete (dir, recursive: true); }
			catch { /* best effort */ }
		}
	}
}
