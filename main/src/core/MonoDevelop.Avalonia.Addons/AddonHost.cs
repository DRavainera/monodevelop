using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MonoDevelop.AvaloniaAddons.Extensions;
using MonoDevelop.AvaloniaAddons.Manifests;
using MonoDevelop.AvaloniaAddons.Packages;

namespace MonoDevelop.AvaloniaAddons
{
	public sealed class AddonLoadState
	{
		public AddonManifest Manifest { get; init; }
		public IAvaloniaAddon Addon { get; set; }
		public AddonLoadContext Context { get; set; }
		public string Error { get; set; }
		public bool Loaded { get; private set; }
		/// <summary>Marked when the add-in activated, or when it contributed nodes
		/// (manifest-only add-ins have no assembly).</summary>
		public void MarkLoaded () => Loaded = Error is null;
	}

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
		/// <summary>Extension-point registry populated by the discovered add-ins.</summary>
		public IAddonExtensionRegistry Extensions => extensions;
		public IReadOnlyList<string> Log => log;
		public string RootDirectory => root;

		void Note (string m) { log.Add (m); Console.WriteLine ("[avalonia-addons] " + m); }

		/// <summary>Discovers manifests (one level of subfolders) and registers their nodes.</summary>
		public void Discover ()
		{
			addons.Clear ();
			if (!Directory.Exists (root)) { Note ($"root not found: {root}"); return; }
			var manifests = new List<string> ();
			foreach (var f in Directory.EnumerateFiles (root, "*" + ManifestSuffix, SearchOption.AllDirectories).OrderBy (f => f))
				manifests.Add (f);
			foreach (var file in manifests) {
				try {
					var m = JsonSerializer.Deserialize<AddonManifest> (File.ReadAllText (file));
					if (m?.Identity is null || string.IsNullOrEmpty (m.Identity.Id)) { Note ("invalid manifest: " + file); continue; }
					var asm = ResolveAssembly (m, file);
					var state = new AddonLoadState { Manifest = m };
					if (asm is not null)
						state.Context = new AddonLoadContext (asm);
					else if (!string.IsNullOrEmpty (m.EntryPoint))
						state.Error = $"entry point not found: {m.EntryPoint}";
					addons.Add (state);
				} catch (Exception ex) { Note ($"manifest error {file}: {ex.Message}"); }
			}
		}

		string ResolveAssembly (AddonManifest m, string manifestFile)
		{
			if (string.IsNullOrEmpty (m.EntryPoint))
				return null;
			var dir = Path.GetDirectoryName (manifestFile);
			var guess = Path.Combine (dir ?? root, m.EntryPoint);
			return File.Exists (guess) ? guess : null;
		}

		/// <summary>Loads add-in assemblies, composes and activates packages.</summary>
		public void LoadAll ()
		{
			foreach (var state in addons) {
				try {
					if (state.Manifest.EntryPoint is { Length: > 0 } && state.Context is { } alc) {
						var asmPath = ResolveAssembly (state.Manifest, FindManifestPath (state.Manifest));
						if (asmPath is not null) {
							var asm = alc.LoadFromAssemblyPath (asmPath);
							foreach (var t in SafeGetTypes (asm)) {
								if (t is null || t.IsAbstract || t.IsInterface)
									continue;
								if (!typeof(IPackage).IsAssignableFrom (t) && !typeof(IAvaloniaAddon).IsAssignableFrom (t))
									continue;
								object inst = null;
								try { inst = Activator.CreateInstance (t); }
								catch (Exception ex) { Note ($"{state.Manifest.Identity.Id}: ctor {t.Name}: {ex.Message}"); continue; }
								composition.Add (inst);
								if (inst is IPackage pkg)
									pkg.Initialize (new Context (root, extensions, composition, state.Manifest.Identity.Id, Note));
							}
						}
					}
					state.MarkLoaded ();
				} catch (Exception ex) { state.Error = ex.Message; Note ($"{state.Manifest.Identity.Id}: {ex.Message}"); }
			}
			// register extension nodes from the manifests (only for add-ins that loaded,
			// so the UI never renders nodes whose classes are unavailable)
			foreach (var state in addons) {
				if (!state.Loaded)
					continue;
				foreach (var (path, nodes) in state.Manifest.Extensions) {
					if (!extensions.HasExtensionPoint (path))
						extensions.DeclareExtensionPoint (path);
					foreach (var node in nodes ?? new List<AddonExtensionNode> ())
						extensions.AddNode (path, node);
				}
			}
			// activate autoloaded packages
			foreach (var p in composition.GetExports (typeof (IPackage)).OfType<IPackage> ())
				_ = p;
			foreach (var inst in composition.Parts) {
				if (inst is IPackage pkg && inst.GetType ().GetCustomAttribute<ProvideAutoLoadAttribute> () is not null)
					pkg.Load ();
			}
			foreach (var st in addons)
				st.MarkLoaded ();
			Note ($"loaded {addons.Count (a => a.Loaded)}/{addons.Count} add-in(s)");
		}

		string FindManifestPath (AddonManifest m)
			=> Directory.EnumerateFiles (root, "*" + ManifestSuffix, SearchOption.AllDirectories)
				.FirstOrDefault (f => File.ReadAllText (f).Contains (m.Identity.Id)) ?? Path.Combine (root, m.Identity.Id + ManifestSuffix);

		static IEnumerable<Type> SafeGetTypes (Assembly asm)
		{
			try { return asm.GetTypes (); }
			catch (ReflectionTypeLoadException ex) { return (ex.Types ?? Array.Empty<Type> ()).Where (t => t is not null); }
		}

		sealed class Context : IAddonContext
		{
			readonly Action<string> note;
			public Context (string root, IAddonExtensionRegistry ext, ICompositionHost comp, string addonId, Action<string> note)
			{ RootDirectory = root; Extensions = ext; Composition = comp; AddonId = addonId; this.note = note; }
			public string AddonId { get; }
			public string RootDirectory { get; }
			public IAddonExtensionRegistry Extensions { get; }
			public ICompositionHost Composition { get; }
			public void Log (string message) => note ($"{AddonId}: {message}");
		}

		public void UnloadAll ()
		{
			foreach (var inst in composition.Parts.OfType<IPackage> ())
				inst.Unload ();
		}

		public void Dispose () => UnloadAll ();
	}
}
