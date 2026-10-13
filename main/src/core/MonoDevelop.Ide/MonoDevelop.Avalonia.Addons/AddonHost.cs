using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
	/// Persistence of per-user add-in state (disabled / uninstalled ids) for
	/// <see cref="AddonHost"/>. The add-on module is standalone (no dependency on the
	/// shell's services), so the host exposes this minimal contract and the shell wires
	/// it to its own settings store. When no store is set the runtime operations still
	/// work, but their state does not survive a restart.
	/// </summary>
	public interface IAddonUserState
	{
		string Get (string key);
		void Set (string key, string value);
	}

	/// <summary>
	/// Hosts the shell add-ins: discovers manifests under the shipped root and the user
	/// root, loads each in its own AssemblyLoadContext, builds the MEF-like composition
	/// and activates packages. Also exposes the Add-in Manager runtime operations
	/// (enable/disable, update, install from directory or file, uninstall). Independent
	/// from Mono.Addins (which stays with the GTK IDE).
	/// </summary>
	public sealed class AddonHost : IDisposable
	{
		public const string ManifestSuffix = ".avaloniaaddon.json";

		public const string StateKeyDisabled = "MonoDevelop.Avalonia.Addins.Disabled";
		public const string StateKeyUninstalled = "MonoDevelop.Avalonia.Addins.Uninstalled";

		readonly List<AddonLoadState> addons = new ();
		readonly AddonExtensionRegistry extensions = new ();
		readonly CompositionHost composition = new ();
		readonly List<string> log = new ();
		readonly string root;
		readonly string userRoot;
		readonly Dictionary<string, List<object>> partsByAddon = new (StringComparer.Ordinal);
		readonly Dictionary<string, List<(string Path, AddonExtensionNode Node)>> nodesByAddon = new (StringComparer.Ordinal);

		public AddonHost (string rootDirectory, string userRootDirectory = null)
		{
			root = rootDirectory;
			userRoot = userRootDirectory;
			foreach (var p in ExtensionPoints.All)
				extensions.DeclareExtensionPoint (p);
		}

		/// <summary>Optional persistence for the disabled/uninstalled sets. Wired by the
		/// shell (e.g. to SettingsStore); null when the host runs standalone.</summary>
		public IAddonUserState StateStore { get; set; }

		public IReadOnlyList<AddonLoadState> Addons => addons;
		public IAddonExtensionRegistry Extensions => extensions;
		public IReadOnlyList<string> Log => log;
		public string RootDirectory => root;

		/// <summary>Root where user installs land (outside the shipped tree); null when
		/// the host was created without one, in which case install-from-directory is
		/// unavailable.</summary>
		public string UserRootDirectory => userRoot;

		/// <summary>Composed parts of every loaded add-in (platform services, packages, ...).</summary>
		public IReadOnlyList<object> Parts => composition.Parts;

		void Note (string m) { log.Add (m); Console.WriteLine ("[avalonia-addons] " + m); }

		/// <summary>Discovers manifests under the shipped root and the user root and
		/// registers them. No assembly is loaded here: contexts are created lazily by
		/// <see cref="EnsureContext"/>, so a missing assembly surfaces as a load error,
		/// not a discovery error. Dependency validation happens in <see cref="LoadAll"/>,
		/// once the whole discovered set is known.</summary>
		public void Discover ()
		{
			// A re-discovery on a live host must start from a clean composition:
			// drop the parts/nodes of the previous set before registering new ones.
			UnloadAll ();
			addons.Clear ();
			partsByAddon.Clear ();
			nodesByAddon.Clear ();
			DiscoverIn (root, userAddin: false);
			if (userRoot is { Length: > 0 } && Directory.Exists (userRoot))
				DiscoverIn (userRoot, userAddin: true);
			MarkSuperseded ();
		}

		void DiscoverIn (string dir, bool userAddin)
		{
			if (!Directory.Exists (dir)) {
				Note ($"root not found: {dir}");
				return;
			}
			var disabled = LoadIdSet (StateKeyDisabled);
			var uninstalled = LoadIdSet (StateKeyUninstalled);
			foreach (var file in Directory.EnumerateFiles (dir, "*" + ManifestSuffix, SearchOption.AllDirectories).OrderBy (f => f)) {
				AddonManifest m;
				try {
					m = JsonSerializer.Deserialize<AddonManifest> (File.ReadAllText (file));
				} catch (Exception ex) {
					Note ($"manifest error {file}: {ex.Message}");
					continue;
				}
				if (m?.Identity is null || string.IsNullOrEmpty (m.Identity.Id)) {
					Note ("invalid manifest: " + file);
					continue;
				}
				// A shipped add-in can be uninstalled (hidden) by the manager; the user
				// root never carries uninstall markers — uninstalling a user add-in
				// removes its folder from disk.
				if (!userAddin && uninstalled.Contains (m.Identity.Id)) {
					Note ($"{m.Identity.Id}: skipped (uninstalled)");
					continue;
				}
				RegisterManifest (file, m, userAddin, disabled);
			}
		}

		/// <summary>Adds a parsed manifest to the discovered set, applying the OS gate
		/// and the persisted disabled flag.</summary>
		AddonLoadState RegisterManifest (string file, AddonManifest m, bool userAddin, IReadOnlySet<string> disabled)
		{
			AddonLoadState state = null;
			try {
				state = new AddonLoadState { Manifest = m, ManifestPath = file, IsUserAddin = userAddin, Enabled = !disabled.Contains (m.Identity.Id) };
				addons.Add (state);
				if (!string.IsNullOrEmpty (m.Os) && !MatchesCurrentOs (m.Os)) {
					state.Error = $"not supported on this platform (requires: {m.Os})";
					Note ($"{m.Identity.Id}: skipped (requires {m.Os})");
				}
			} catch (Exception ex) {
				if (state is not null) state.Error = ex.Message;
				Note ($"manifest error {file}: {ex.Message}");
			}
			return state;
		}

		/// <summary>Lazily creates the add-in's AssemblyLoadContext. Manifest-only add-ins
		/// (no entry point and no assets) never get one. Returns false — and records the
		/// error on the state — when the assembly cannot be resolved.</summary>
		bool EnsureContext (AddonLoadState state)
		{
			if (state.Context is not null)
				return true;
			var m = state.Manifest;
			if (m.EntryPoint is not { Length: > 0 } && (m.Assets == null || m.Assets.Count == 0))
				return true;
			var asm = ResolveAssembly (m, state.ManifestPath);
			if (asm is null) {
				state.Error = "assembly not found for entry point";
				Note ($"{m.Identity.Id}: assembly not found for entry point");
				return false;
			}
			var manifestDir = Path.GetDirectoryName (state.ManifestPath) ?? root;
			try {
				state.Context = new AddonLoadContext (asm, manifestDir);
			} catch (Exception ex) {
				state.Error = ex.Message;
				Note ($"{m.Identity.Id}: {ex.Message}");
				return false;
			}
			return true;
		}

		/// <summary>Among the states sharing an id (shipped and user roots), the highest
		/// version wins; the rest are flagged <see cref="AddonLoadState.Superseded"/> so
		/// the manager can list them without loading them. Ties (same version) favour the
		/// user add-in. Idempotent, so runtime operations can re-run it after adding or
		/// removing states.</summary>
		void MarkSuperseded ()
		{
			foreach (var a in addons)
				a.Superseded = false;
			foreach (var group in addons.Where (a => a.Error is null)
				.GroupBy (a => a.Manifest.Identity.Id, StringComparer.Ordinal)) {
				if (group.Count () < 2)
					continue;
				var newest = group
					.OrderByDescending (a => VersionOrZero (a.Manifest.Identity.Version))
					.ThenBy (a => a.IsUserAddin ? 0 : 1)
					.First ();
				foreach (var a in group)
					if (!ReferenceEquals (a, newest))
						a.Superseded = true;
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
				
				// Find dependency addon (the active, non-superseded version when several
				// are staged under the same id)
				var depAddon = addons.FirstOrDefault (a => a.Manifest.Identity.Id == dep.Id && !a.Superseded)
					?? addons.FirstOrDefault (a => a.Manifest.Identity.Id == dep.Id);
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
				if (state.Error is not null || state.Superseded) continue;
				if (!ValidateDependencies (state.Manifest)) {
					state.Error = "dependencies not satisfied";
					Note ($"{state.Manifest.Identity.Id}: dependencies not satisfied");
				}
			}

			// Phase 2: load assemblies, then compose the enabled add-ins (their parts
			// and extension nodes).
			foreach (var state in addons) {
				if (state.Error is not null || state.Superseded) continue;
				try {
					if (!EnsureContext (state))
						continue;
					if (state.Context is { } alc)
						_ = SafeGetTypes (alc.Assembly); // force load: surface broken references early
					state.MarkLoaded ();
					if (state.Enabled)
						ComposeAddon (state);
				} catch (Exception ex) {
					state.Error = ex.Message;
					Note ($"{state.Manifest.Identity.Id}: {ex.Message}");
				}
			}

			Note ($"loaded {addons.Count (a => a.Loaded && a.Enabled && !a.Superseded)}/{addons.Count} add-in(s)");
		}

		/// <summary>Instantiates the add-in's IPackage/IAvaloniaAddon parts, registers
		/// them in the composition and contributes the manifest's extension nodes.
		/// Idempotent: the add-in's previous parts and nodes are removed first.</summary>
		void ComposeAddon (AddonLoadState state)
		{
			UnloadAddon (state);
			var id = state.Manifest.Identity.Id;
			var parts = new List<object> ();
			if (state.Context is { } alc) {
				foreach (var t in SafeGetTypes (alc.Assembly)) {
					if (t is null || t.IsAbstract || t.IsInterface) continue;
					if (!typeof (IPackage).IsAssignableFrom (t) && !typeof (IAvaloniaAddon).IsAssignableFrom (t)) continue;

					object inst = null;
					try { inst = Activator.CreateInstance (t); }
					catch (Exception ex) { Note ($"{id}: ctor {t.Name}: {ex.Message}"); continue; }
					composition.Add (inst);
					parts.Add (inst);

					if (inst is IAvaloniaAddon addon) {
						addon.Initialize (new Context (root, extensions, composition, id, Note));
						if (addon.GetType ().GetCustomAttribute<ProvideAutoLoadAttribute> () is not null) addon.Load ();
					}

					if (inst is IPackage pkg) {
						pkg.Initialize (new Context (root, extensions, composition, id, Note));
						if (pkg.GetType ().GetCustomAttribute<ProvideAutoLoadAttribute> () is not null) pkg.Load ();
					}
				}
			}
			partsByAddon [id] = parts;

			var nodes = new List<(string Path, AddonExtensionNode Node)> ();
			if (state.Manifest.AutoLoad) {
				foreach (var (path, nodesList) in state.Manifest.Extensions) {
					if (!extensions.HasExtensionPoint (path)) extensions.DeclareExtensionPoint (path);
					foreach (var node in nodesList ?? new List<AddonExtensionNode> ()) {
						extensions.AddNode (path, node);
						nodes.Add ((path, node));
					}
				}
			}
			nodesByAddon [id] = nodes;
		}

		/// <summary>Reverts <see cref="ComposeAddon"/>: unloads the add-in's packages,
		/// removes its parts from the composition and its nodes from the registry. The
		/// assembly stays in its (non-collectible) ALC, so re-enabling recomposes the
		/// same parts.</summary>
		void UnloadAddon (AddonLoadState state)
		{
			var id = state.Manifest.Identity.Id;
			if (partsByAddon.TryGetValue (id, out var parts)) {
				foreach (var inst in parts) {
					if (inst is IPackage pkg) {
						try { pkg.Unload (); }
						catch (Exception ex) { Note ($"{id}: unload {inst.GetType ().Name}: {ex.Message}"); }
					}
				}
				composition.RemoveRange (parts);
				partsByAddon.Remove (id);
			}
			if (nodesByAddon.TryGetValue (id, out var nodes)) {
				foreach (var (path, node) in nodes)
					extensions.RemoveNode (path, node);
				nodesByAddon.Remove (id);
			}
		}

		/// <summary>The active (non-superseded) state of an add-in, or null.</summary>
		public AddonLoadState FindAddon (string id)
			=> addons.FirstOrDefault (a => a.Manifest.Identity.Id == id && !a.Superseded);

		/// <summary>All discovered states with the given id, newest first. More than one
		/// entry means the add-in has staged updates (<see cref="UpdateAll"/>).</summary>
		public IReadOnlyList<AddonLoadState> FindVersions (string id)
			=> addons.Where (a => a.Manifest.Identity.Id == id)
				.OrderByDescending (a => VersionOrZero (a.Manifest.Identity.Version))
				.ThenBy (a => a.IsUserAddin ? 0 : 1)
				.ToList ();

		/// <summary>Enables or disables an add-in at runtime: its parts and extension
		/// nodes are composed or removed, while the assembly stays in its
		/// (non-collectible) ALC. The choice persists across restarts via the state
		/// store.</summary>
		public bool SetEnabled (string id, bool enabled, out string error)
		{
			var state = FindAddon (id);
			if (state is null) { error = $"add-in '{id}' not found"; return false; }
			if (state.Error is not null) { error = $"add-in '{id}' has an error: {state.Error}"; return false; }
			if (!EnsureContext (state)) { error = state.Error ?? "cannot load add-in"; return false; }
			state.MarkLoaded ();
			state.Enabled = enabled;
			if (enabled)
				ComposeAddon (state);
			else
				UnloadAddon (state);
			// Persist the user's disabled choice even when the add-in did not load
			// (broken assembly): it was disabled on purpose and must stay disabled
			// once it loads again. Enabled is only ever false by user choice or by
			// restoring this same set, so no other state ends up in it.
			var disabled = new SortedSet<string> (StringComparer.Ordinal);
			foreach (var a in addons)
				if (!a.Enabled)
					disabled.Add (a.Manifest.Identity.Id);
			SaveIdSet (StateKeyDisabled, disabled);
			Note ($"{id}: {(enabled ? "enabled" : "disabled")}");
			error = null;
			return true;
		}

		/// <summary>Uninstalls an add-in: a user add-in's folder is removed from disk; a
		/// shipped one is hidden with a persisted uninstalled marker (the shipped tree is
		/// source, not writable). The add-in disappears from the discovered set.</summary>
		public bool Uninstall (string id, out string error)
		{
			var state = FindAddon (id) ?? addons.FirstOrDefault (a => a.Manifest.Identity.Id == id);
			if (state is null) { error = $"add-in '{id}' not found"; return false; }
			RemoveState (state);
			// Re-run the version precedence: removing the active version of an id can
			// un-supersede an older one (e.g. uninstalling a user add-in that shadowed
			// a shipped one) — it must become active (and composed) again, not be left
			// orphaned with a stale Superseded flag.
			MarkSuperseded ();
			ActivateId (id);
			if (state.IsUserAddin) {
				DeleteFolder (state.ManifestPath);
				Note ($"uninstalled {id} {state.Manifest.Identity.Version} (user add-in removed)");
			} else {
				var uninstalled = LoadIdSet (StateKeyUninstalled);
				uninstalled.Add (id);
				SaveIdSet (StateKeyUninstalled, uninstalled);
				Note ($"uninstalled {id} {state.Manifest.Identity.Version} (shipped add-in hidden)");
			}
			error = null;
			return true;
		}

		/// <summary>Applies the newest staged version of an add-in and discards the older
		/// ones: an older user version is removed from disk, an older shipped one is
		/// hidden with an uninstalled marker (only when the winner is a user add-in — a
		/// marker would hide every shipped version of the id, so a shipped winner never
		/// sets one). The winning version is composed so a mid-session update takes
		/// effect.</summary>
		public bool Update (string id, out string error)
		{
			var versions = FindVersions (id);
			if (versions.Count < 2) { error = $"no staged update for '{id}'"; return false; }
			var newest = versions [0];
			if (newest.Error is not null) { error = $"newest version of '{id}' has an error: {newest.Error}"; return false; }
			foreach (var old in versions.Skip (1)) {
				RemoveState (old);
				if (old.IsUserAddin)
					DeleteFolder (old.ManifestPath);
				else if (newest.IsUserAddin) {
					var uninstalled = LoadIdSet (StateKeyUninstalled);
					uninstalled.Add (old.Manifest.Identity.Id);
					SaveIdSet (StateKeyUninstalled, uninstalled);
					Note ($"{old.Manifest.Identity.Id}: shipped version hidden after update to {newest.Manifest.Identity.Version}");
				}
			}
			MarkSuperseded ();
			if (newest.Enabled && !newest.Superseded) {
				if (!EnsureContext (newest)) { error = newest.Error ?? "cannot load add-in"; return false; }
				newest.MarkLoaded ();
				ComposeAddon (newest);
			}
			Note ($"updated {id} -> {newest.Manifest.Identity.Version}");
			error = null;
			return true;
		}

		/// <summary>Updates every add-in that has staged versions to discard. Returns the
		/// number of updated ids; stops at the first failure.</summary>
		public int UpdateAll (out string error)
		{
			var count = 0;
			foreach (var id in addons.Select (a => a.Manifest.Identity.Id).Distinct (StringComparer.Ordinal)
				.Where (i => FindVersions (i).Count > 1)
				.OrderBy (i => i, StringComparer.Ordinal)) {
				if (!Update (id, out error))
					return count;
				count++;
			}
			error = null;
			return count;
		}

		/// <summary>Installs an add-in from a directory (the manifest either directly in
		/// it or inside a single subfolder) into the user root, replacing any previous
		/// user version of the same id. The installed add-in is loaded and composed
		/// immediately when it has no errors.</summary>
		public bool InstallFromDirectory (string sourceDir, out string error)
		{
			if (userRoot is null) { error = "no user add-in root configured"; return false; }
			if (!Directory.Exists (sourceDir)) { error = $"directory not found: {sourceDir}"; return false; }
			var manifestFile = FindManifest (sourceDir);
			if (manifestFile is null) { error = $"no {ManifestSuffix} found in {sourceDir}"; return false; }

			AddonManifest m;
			try {
				m = JsonSerializer.Deserialize<AddonManifest> (File.ReadAllText (manifestFile));
			} catch (Exception ex) {
				error = "invalid manifest: " + ex.Message;
				return false;
			}
			if (m?.Identity is null || string.IsNullOrEmpty (m.Identity.Id)) { error = "manifest has no identity id"; return false; }
			var id = m.Identity.Id;

			var addonDir = Path.GetDirectoryName (manifestFile) ?? sourceDir;
			var dest = Path.Combine (userRoot, id);
			try {
				Directory.CreateDirectory (userRoot);
				if (Directory.Exists (dest))
					Directory.Delete (dest, recursive: true);
				CopyDirectory (addonDir, dest);
			} catch (Exception ex) {
				error = "copy failed: " + ex.Message;
				return false;
			}
			Note ($"installed {id} {m.Identity.Version} -> {dest}");

			// Drop the previous user state (its folder was replaced) and register the
			// new one.
			foreach (var old in addons.Where (a => a.IsUserAddin && a.Manifest.Identity.Id == id).ToList ())
				RemoveState (old);
			var state = RegisterManifest (Path.Combine (dest, Path.GetFileName (manifestFile)), m, userAddin: true, LoadIdSet (StateKeyDisabled));
			if (state is null) { error = "manifest could not be registered after install"; return false; }
			MarkSuperseded ();
			if (state.Error is null && !state.Superseded)
				LoadAddon (state, out _);
			error = null;
			return true;
		}

		/// <summary>Installs an add-in package file. v1 supports .zip archives only: the
		/// archive is extracted to a temporary folder and installed as a directory.</summary>
		public bool InstallFromFile (string packageFile, out string error)
		{
			if (!File.Exists (packageFile)) { error = $"file not found: {packageFile}"; return false; }
			if (!packageFile.EndsWith (".zip", StringComparison.OrdinalIgnoreCase)) { error = "only .zip packages are supported (v1)"; return false; }
			var temp = Path.Combine (Path.GetTempPath (), "md-avalonia-addon-" + Guid.NewGuid ().ToString ("N"));
			try {
				ZipFile.ExtractToDirectory (packageFile, temp, overwriteFiles: true);
				return InstallFromDirectory (temp, out error);
			} catch (Exception ex) {
				error = "could not extract package: " + ex.Message;
				return false;
			} finally {
				try { if (Directory.Exists (temp)) Directory.Delete (temp, recursive: true); } catch { }
			}
		}

		/// <summary>Loads a single add-in (used after the user fixed the cause of a load
		/// error, e.g. a missing assembly). Superseded versions cannot be loaded: only
		/// the newest staged version of an id is active.</summary>
		public bool LoadAddon (AddonLoadState state, out string error)
		{
			if (state is null) { error = "no add-in"; return false; }
			if (state.Superseded) { error = "superseded by a newer staged version"; return false; }
			state.Error = null;
			if (!EnsureContext (state)) { error = state.Error ?? "cannot load add-in"; return false; }
			try {
				if (state.Context is { } alc)
					_ = SafeGetTypes (alc.Assembly);
				state.MarkLoaded ();
				if (state.Enabled)
					ComposeAddon (state);
			} catch (Exception ex) {
				state.Error = ex.Message;
				error = ex.Message;
				Note ($"{state.Manifest.Identity.Id}: {ex.Message}");
				return false;
			}
			error = null;
			return true;
		}

		/// <summary>Loads and composes the active (non-superseded) state of an id when it
		/// is enabled and error-free. Used after a runtime operation flips which version
		/// of an id is active (e.g. uninstalling a version that shadowed an older one):
		/// the older version was superseded and never composed, so it needs a one-shot
		/// load+compose to become actually active.</summary>
		void ActivateId (string id)
		{
			var state = FindAddon (id);
			if (state is null || state.Error is not null)
				return;
			if (!EnsureContext (state))
				return;
			if (!state.Loaded) {
				if (state.Context is { } alc)
					_ = SafeGetTypes (alc.Assembly);
				state.MarkLoaded ();
			}
			if (state.Enabled)
				ComposeAddon (state);
		}

		/// <summary>Drops a state: its parts and nodes are removed (if composed) and it
		/// leaves the discovered set.</summary>
		void RemoveState (AddonLoadState state)
		{
			var id = state.Manifest.Identity.Id;
			if (partsByAddon.ContainsKey (id) || nodesByAddon.ContainsKey (id))
				UnloadAddon (state);
			addons.Remove (state);
		}

		/// <summary>Removes the folder containing the given manifest (user add-ins are
		/// uninstalled by deleting them from disk).</summary>
		void DeleteFolder (string manifestPath)
		{
			if (manifestPath is not { Length: > 0 })
				return;
			var dir = Path.GetDirectoryName (manifestPath);
			if (dir is null || !Directory.Exists (dir))
				return;
			try {
				Directory.Delete (dir, recursive: true);
			} catch (Exception ex) {
				Note ($"could not remove {dir}: {ex.Message}");
			}
		}

		SortedSet<string> LoadIdSet (string key)
		{
			var set = new SortedSet<string> (StringComparer.Ordinal);
			var raw = StateStore?.Get (key);
			if (raw is not { Length: > 0 })
				return set;
			try {
				var arr = JsonSerializer.Deserialize<string[]> (raw);
				if (arr is not null)
					foreach (var id in arr)
						if (id is { Length: > 0 })
							set.Add (id);
			} catch {
				// Corrupt user state: start from an empty set rather than failing the
				// whole discovery.
			}
			return set;
		}

		void SaveIdSet (string key, IReadOnlySet<string> set)
		{
			if (StateStore is null)
				return;
			try {
				StateStore.Set (key, set.Count == 0 ? null : JsonSerializer.Serialize (set.OrderBy (x => x, StringComparer.Ordinal).ToArray ()));
			} catch (Exception ex) {
				Note ($"could not persist add-in state '{key}': {ex.Message}");
			}
		}

		static Version VersionOrZero (string v) => Version.TryParse (v, out var ver) ? ver : new Version (0, 0);

		static void CopyDirectory (string source, string dest)
		{
			Directory.CreateDirectory (dest);
			foreach (var file in Directory.EnumerateFiles (source, "*", SearchOption.AllDirectories)) {
				var target = Path.Combine (dest, Path.GetRelativePath (source, file));
				Directory.CreateDirectory (Path.GetDirectoryName (target));
				File.Copy (file, target, overwrite: true);
			}
		}

		/// <summary>Finds the add-in manifest in <paramref name="dir"/>: directly in it,
		/// or inside a single subfolder (package wrappers).</summary>
		static string FindManifest (string dir)
		{
			var direct = Directory.EnumerateFiles (dir, "*" + ManifestSuffix).FirstOrDefault ();
			if (direct is not null)
				return direct;
			var subdirs = Directory.GetDirectories (dir);
			if (subdirs.Length == 1) {
				var inSub = Directory.EnumerateFiles (subdirs [0], "*" + ManifestSuffix).FirstOrDefault ();
				if (inSub is not null)
					return inSub;
			}
			return null;
		}

		static IEnumerable<Type> SafeGetTypes (Assembly asm) {
			try { return asm.GetTypes (); } catch (ReflectionTypeLoadException ex) { return (ex.Types ?? Array.Empty<Type> ()).Where (t => t is not null); }
		}

		static bool MatchesCurrentOs (string os) => os.Trim ().ToLowerInvariant () switch {
			"linux" => System.OperatingSystem.IsLinux (),
			"macos" or "osx" or "mac" => System.OperatingSystem.IsMacOS (),
			"windows" or "win" => System.OperatingSystem.IsWindows (),
			_ => true,
		};

		public void Dispose () => UnloadAll ();

		void UnloadAll () {
			foreach (var state in addons)
				if (partsByAddon.ContainsKey (state.Manifest.Identity.Id) || nodesByAddon.ContainsKey (state.Manifest.Identity.Id))
					UnloadAddon (state);
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