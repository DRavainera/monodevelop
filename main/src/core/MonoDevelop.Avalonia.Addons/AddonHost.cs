using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Text.Json;
using MonoDevelop.AvaloniaAddons.Extensions;
using MonoDevelop.AvaloniaAddons.Manifests;
using MonoDevelop.AvaloniaAddons.Packages;

namespace MonoDevelop.AvaloniaAddons
{

	/// <summary>
	/// Hosts the shell add-ins: discovers manifests under a root folder, loads each in its
	/// own AssemblyLoadContext, builds the MEF-like composition and activates packages.
	/// Independent from Mono.Addins (which stays with the GTK IDE).
	/// </summary>
	public sealed class AddonHost : IDisposable
	{
		public const string ManifestSuffix = ".avaloniaaddon.json";

		readonly List<AddonLoadState> addons = new ();
		readonly AddonExtensionRegistry extensions = new ();
		readonly CompositionHost composition = new ();
		readonly List<string> log = new ();
		readonly string root;

		public AddonHost (string rootDirectory)
		{
			root = rootDirectory;
			foreach (var p in ExtensionPoints.All)
				extensions.DeclareExtensionPoint (p);
		}

		public IReadOnlyList<AddonLoadState> Addons => addons;
		public IAddonExtensionRegistry Extensions => extensions;
		public IReadOnlyList<string> Log => log;
		public string RootDirectory => root;

		void Note (string m) { log.Add (m); Console.WriteLine ("[avalonia-addons] " + m); }

		/// <summary>Discovers manifests (one level of subfolders) and registers them.
		/// Dependency validation happens in <see cref="LoadAll"/>, once the whole
		/// discovered set is known.</summary>
		public void Discover ()
		{
			addons.Clear ();
			if (!Directory.Exists (root)) { Note ($"root not found: {root}"); return; }
			var manifests = new List<string> ();
			foreach (var f in Directory.EnumerateFiles (root, "*" + ManifestSuffix, SearchOption.AllDirectories).OrderBy (f => f))
				manifests.Add (f);
			foreach (var file in manifests) {
				AddonLoadState state = null;
				try {
					var m = JsonSerializer.Deserialize<AddonManifest> (File.ReadAllText (file));
					if (m?.Identity is null || string.IsNullOrEmpty (m.Identity.Id)) { Note ("invalid manifest: " + file); continue; }

					state = new AddonLoadState { Manifest = m, ManifestPath = file };
					addons.Add (state);

					if (m.EntryPoint is not { Length: > 0 } && (m.Assets == null || m.Assets.Count == 0)) {
						// Manifest-only add-in: no assembly to load; it contributes
						// its extension nodes (preferences panels, platform hooks, ...).
						state.MarkLoaded ();
						continue;
					}

					var asm = ResolveAssembly (m, file);
					var manifestDir = Path.GetDirectoryName (file) ?? root;
					if (asm is null) {
						state.Error = "assembly not found for entry point";
						continue;
					}
					// The context resolves + loads the assembly; LoadAll composes
					// its parts (IPackage/IAvaloniaAddon) and marks the state.
					state.Context = new AddonLoadContext (asm, manifestDir);
				} catch (Exception ex) {
					if (state is not null) state.Error = ex.Message;
					Note ($"manifest error {file}: {ex.Message}");
				}
			}
		}

		string ResolveAssembly (AddonManifest m, string manifestFile)
		{
			if (m.EntryPoint is not { Length: > 0 } && (m.Assets == null || m.Assets.Count == 0)) return null;
			var dir = Path.GetDirectoryName (manifestFile) ?? root;
			
			// Prioritize Assets configuration
			if (m.Assets != null) {
				foreach (var asset in m.Assets) {
					if (asset.Assembly is { Length: > 0 }) {
						// Try as assembly name first, then as file path
						var assemblyName = asset.Assembly;
						if (assemblyName.EndsWith (".dll", StringComparison.OrdinalIgnoreCase))
							assemblyName = Path.GetFileNameWithoutExtension (assemblyName);
						
						return assemblyName; // Return assembly name for AddonLoadContext
					}
				}
			}
			
			// Legacy support for EntryPoint as file path
			if (m.EntryPoint is { Length: > 0 }) {
				var guess = Path.Combine (dir, m.EntryPoint);
				if (File.Exists (guess)) {
					// Convert file path to assembly name
					return Path.GetFileNameWithoutExtension (guess);
				}
				// Try as assembly name
				return m.EntryPoint;
			}
			
			return null;
		}

		/// <summary>Resolves the identity icon declared in the manifest (identity.icon) to a
		/// file on disk. "core:file.png" points at the legacy core icon set; any other value
		/// is a path relative to the add-ins root (or to the add-in's own folder when it
		/// exists there). Returns null when the file is not found so the UI can fall back to
		/// the generic add-in icon (the legacy dialog's plugin-32 fallback). Resolution walks
		/// up from the binary directory, mirroring IconService, so both the repo tree and a
		/// staged build resolve.</summary>
		public string? ResolveIconFile (AddonLoadState state)
		{
			var icon = state?.Manifest?.Identity?.Icon;
			if (string.IsNullOrEmpty (icon))
				return null;
			if (icon.StartsWith ("core:", StringComparison.Ordinal))
				return FindFile (new[] { Path.Combine ("src", "core", "MonoDevelop.Ide", "icons") }, icon.Substring ("core:".Length));
			if (state?.ManifestPath is { Length: > 0 }) {
				var direct = Path.GetFullPath (Path.Combine (Path.GetDirectoryName (state.ManifestPath) ?? root, icon));
				if (File.Exists (direct))
					return direct;
			}
			return FindFile (new[] { Path.Combine ("src", "addins"), "addins", "AddIns" }, icon);
		}

		static string? FindFile (string[] subdirs, string fileName)
		{
			var baseDir = AppContext.BaseDirectory;
			var candidates = new List<string> ();
			foreach (var sub in subdirs) {
				candidates.Add (Path.GetFullPath (Path.Combine (baseDir, sub, fileName)));
				candidates.Add (Path.GetFullPath (Path.Combine (baseDir, "..", "..", sub, fileName)));
			}
			var dir = baseDir;
			for (int i = 0; i < 8 && dir is not null; i++) {
				foreach (var sub in subdirs)
					candidates.Add (Path.GetFullPath (Path.Combine (dir, sub, fileName)));
				dir = Path.GetDirectoryName (dir);
			}
			foreach (var c in candidates) {
				if (File.Exists (c))
					return c;
			}
			return null;
		}

		bool ValidateDependencies (AddonManifest m)
		{
			if (m.Dependencies == null || m.Dependencies.Count == 0) return true;
			
			foreach (var dep in m.Dependencies) {
				if (dep.Id is null || string.IsNullOrEmpty (dep.Id)) continue;
				
				// Find dependency addon
				var depAddon = addons.FirstOrDefault (a => a.Manifest.Identity.Id == dep.Id);
				if (depAddon is null) {
					// Dependency on a core component (e.g. MonoDevelop.Ide) provided
					// by the host, not by another add-in: satisfied.
					continue;
				}
				
				// Check the dependency did not fail to load (assembly missing, ...)
				if (depAddon.Error is not null) {
					Note ($"Dependency '{dep.Id}' for '{m.Identity.Id}' failed to load: {depAddon.Error}");
					return false;
				}
				
				// Validate version range
				if (dep.VersionRange is { Length: > 0 } && !IsVersionSatisfied (depAddon.Manifest.Identity.Version, dep.VersionRange)) {
					Note ($"Version constraint failed: '{m.Identity.Id}' requires '{dep.Id}' {dep.VersionRange}, but found {depAddon.Manifest.Identity.Version}");
					return false;
				}
			}
			
			return true;
		}

		bool IsVersionSatisfied (string version, string range)
		{
			if (range == "[*]" || string.IsNullOrEmpty (range)) return true;
			
			try {
				// Parse range format like [9.0, 10.0)
				if (range.StartsWith ("[") && (range.EndsWith (")") || range.EndsWith ("]"))) {
					var inner = range.Substring (1, range.Length - 2); // Remove brackets
					var parts = inner.Split (',');
					if (parts.Length != 2) return version == range; // Fallback
					
					var minVersionStr = parts[0].Trim ();
					var maxVersionStr = parts[1].Trim ();
					
					// Parse versions
					if (!Version.TryParse (version, out var actualVersion)) return false;
					
					bool minInclusive = range[0] == '[';
					bool maxInclusive = range[^1] == ']';
					
					// Check minimum bound
					if (!string.IsNullOrEmpty (minVersionStr) && minVersionStr != "*") {
						if (!Version.TryParse (minVersionStr, out var minVersion)) return false;
						
						if (minInclusive) {
							if (actualVersion < minVersion) return false;
						} else {
							if (actualVersion <= minVersion) return false;
						}
					}
					
					// Check maximum bound
					if (!string.IsNullOrEmpty (maxVersionStr) && maxVersionStr != "*") {
						if (!Version.TryParse (maxVersionStr, out var maxVersion)) return false;
						
						if (maxInclusive) {
							if (actualVersion > maxVersion) return false;
						} else {
							if (actualVersion >= maxVersion) return false;
						}
					}
					
					return true;
				}
				
				// Simple equality check
				return version == range;
			} catch {
				// Fallback to simple comparison
				return version == range;
			}
		}

		/// <summary>Validates dependencies, loads add-in assemblies, composes and activates packages.</summary>
		public void LoadAll ()
		{
			// Phase 1: dependency validation against the full discovered set.
			foreach (var state in addons) {
				if (state.Error is not null) continue;
				if (!ValidateDependencies (state.Manifest)) {
					state.Error = "dependencies not satisfied";
					Note ($"{state.Manifest.Identity.Id}: dependencies not satisfied");
				}
			}
			
			// Phase 2: load assemblies (manifest-only add-ins are already loaded).
			foreach (var state in addons) {
				if (state.Loaded || state.Error is not null) continue;
				try {
					if (state.Context is { } alc) {
						foreach (var t in SafeGetTypes (alc.Assembly)) {
							if (t is null || t.IsAbstract || t.IsInterface) continue;
							if (!typeof(IPackage).IsAssignableFrom (t) && !typeof(IAvaloniaAddon).IsAssignableFrom (t)) continue;
							
							object inst = null;
							try { inst = Activator.CreateInstance (t); } 
							catch (Exception ex) { Note ($"{state.Manifest.Identity.Id}: ctor {t.Name}: {ex.Message}"); continue; }
							composition.Add (inst);
							
							if (inst is IAvaloniaAddon addon) {
								addon.Initialize (new Context (root, extensions, composition, state.Manifest.Identity.Id, Note));
								if (addon.GetType ().GetCustomAttribute<ProvideAutoLoadAttribute> () is not null) addon.Load ();
							}
							
							if (inst is IPackage pkg) {
								pkg.Initialize (new Context (root, extensions, composition, state.Manifest.Identity.Id, Note));
								if (pkg.GetType ().GetCustomAttribute<ProvideAutoLoadAttribute> () is not null) pkg.Load ();
							}
						}
					}
					state.MarkLoaded ();
				} catch (Exception ex) { 
					state.Error = ex.Message; 
					Note ($"{state.Manifest.Identity.Id}: {ex.Message}");
				}
			}
			
			// register extension nodes from the manifests (only for add-ins that
			// loaded and autoLoad is not disabled, so the UI never renders nodes
			// whose classes are unavailable)
			foreach (var state in addons) {
				if (!state.Loaded || !state.Manifest.AutoLoad) continue;
				foreach (var (path, nodes) in state.Manifest.Extensions) {
					if (!extensions.HasExtensionPoint (path)) extensions.DeclareExtensionPoint (path);
					foreach (var node in nodes ?? new List<AddonExtensionNode> ()) extensions.AddNode (path, node);
				}
			}
			// activate autoloaded packages
			foreach (var p in composition.GetExports (typeof (IPackage)).OfType<IPackage> ()) _ = p;
			foreach (var inst in composition.Parts) {
				if (inst is IPackage pkg && inst.GetType ().GetCustomAttribute<ProvideAutoLoadAttribute> () is not null) pkg.Load ();
			}
			foreach (var st in addons) st.MarkLoaded ();
			Note ($"loaded {addons.Count (a => a.Loaded)}/{addons.Count} add-in(s)");
		}

		static IEnumerable<Type> SafeGetTypes (Assembly asm) {
			try { return asm.GetTypes (); } catch (ReflectionTypeLoadException ex) { return (ex.Types ?? Array.Empty<Type> ()).Where (t => t is not null); }
		}

		public void Dispose () => UnloadAll ();

		void UnloadAll () {
			foreach (var inst in composition.Parts.OfType<IPackage> ()) inst.Unload ();
		}

		sealed class Context : IAddonContext {
			readonly Action<string> note;
			public Context (string root, IAddonExtensionRegistry ext, ICompositionHost comp, string addonId, Action<string> note) {
				this.RootDirectory = root; this.Extensions = ext; this.Composition = comp; this.AddonId = addonId; this.note = note;
			}
			public string AddonId { get; }
			public string RootDirectory { get; }
			public IAddonExtensionRegistry Extensions { get; }
			public ICompositionHost Composition { get; }
			public ICommandService CommandService { get; }
			public IPadRegistry PadRegistry { get; }
			public void Log (string message) => note ($"{AddonId}: {message}");
		}
 	}
}