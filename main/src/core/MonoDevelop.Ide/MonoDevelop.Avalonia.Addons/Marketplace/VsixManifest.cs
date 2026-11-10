using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.AvaloniaAddons.Marketplace
{
	/// <summary>
	/// The parsed <c>extension.vsixmanifest</c> of a Visual Studio extension package.
	///
	/// A .vsix is a zip whose root carries <c>extension.vsixmanifest</c> (a
	/// <c>PackageManifest</c> XML document, namespace
	/// http://schemas.microsoft.com/developer/vsx-schema/2011) plus the extension
	/// payload (typically under <c>extension/</c>). The manifest's shape is the source
	/// the shell's <see cref="Manifests.AddonManifest"/> is "shaped after", so this is
	/// the bridge from a downloadable marketplace package to the shell's add-in model.
	///
	/// The parser is dependency-free (System.Xml.Linq) and tolerant: it returns a
	/// model with whatever fields it can read rather than throwing on an unusual
	/// package, so the importer can still surface an (incomplete) row in the Gallery.
	/// </summary>
	public sealed class VsixManifest
	{
		public string Id { get; set; } = "";
		public string Publisher { get; set; } = "";
		public string Version { get; set; } = "";
		public string Language { get; set; } = "en-US";
		public string DisplayName { get; set; } = "";
		public string Description { get; set; } = "";
		public string Tags { get; set; } = "";
		public string Categories { get; set; } = "";
		public string Icon { get; set; } = "";
		/// <summary>Installation target ids (e.g. "Microsoft.VisualStudio" for desktop
		/// extensions, "Microsoft.VisualStudio.Code" for VS Code extensions).</summary>
		public List<string> InstallationTargets { get; set; } = new ();
		/// <summary>Declared dependency ids ("publisher.extension@version" form).</summary>
		public List<string> Dependencies { get; set; } = new ();
		/// <summary>Manifest <c>Properties/Property</c> (Id → Value) — engine, links,
		/// branding, etc. Kept for diagnostics and future mapping.</summary>
		public Dictionary<string, string> Properties { get; set; } = new ();

		/// <summary>True when the package targets the Visual Studio desktop host.
		/// Desktop extensions declare an EDITION target (Microsoft.VisualStudio.
		/// Community/Professional/Enterprise/Premium/...), never the bare
		/// "Microsoft.VisualStudio"; VS Code declares "Microsoft.VisualStudio.Code".
		/// So: any Microsoft.VisualStudio* target that is not the Code host.</summary>
		public bool IsVisualStudioDesktop
			=> InstallationTargets.Any (t =>
				t.StartsWith ("Microsoft.VisualStudio", StringComparison.OrdinalIgnoreCase)
				&& !t.StartsWith ("Microsoft.VisualStudio.Code", StringComparison.OrdinalIgnoreCase));

		static readonly XNamespace Vs = "http://schemas.microsoft.com/developer/vsx-schema/2011";

		/// <summary>Parses a manifest from a stream (the <c>extension.vsixmanifest</c>
		/// entry of the .vsix).</summary>
		public static VsixManifest Parse (Stream manifestStream)
		{
			var doc = XDocument.Load (manifestStream);
			var root = doc.Root;
			if (root is null)
				return new VsixManifest ();
			var m = new VsixManifest ();

			var identity = root.Element (Vs + "Metadata")?.Element (Vs + "Identity");
			if (identity is not null) {
				m.Id = (string)identity.Attribute ("Id") ?? "";
				m.Publisher = (string)identity.Attribute ("Publisher") ?? "";
				m.Version = (string)identity.Attribute ("Version") ?? "";
				m.Language = (string)identity.Attribute ("Language") ?? "en-US";
			}

			var metadata = root.Element (Vs + "Metadata");
			if (metadata is not null) {
				m.DisplayName = (string)metadata.Element (Vs + "DisplayName") ?? "";
				m.Description = (string)metadata.Element (Vs + "Description") ?? "";
				m.Tags = (string)metadata.Element (Vs + "Tags") ?? "";
				m.Categories = (string)metadata.Element (Vs + "Categories") ?? "";
				m.Icon = (string)metadata.Element (Vs + "Icon") ?? "";
				var props = metadata.Element (Vs + "Properties");
				if (props is not null)
					foreach (var p in props.Elements (Vs + "Property")) {
						var id = (string)p.Attribute ("Id");
						var val = (string)p.Attribute ("Value");
						if (id is not null)
							m.Properties [id] = val ?? "";
					}
			}

			var installation = root.Element (Vs + "Installation");
			if (installation is not null)
				foreach (var t in installation.Elements (Vs + "InstallationTarget")) {
					var id = (string)t.Attribute ("Id");
					if (id is { Length: > 0 })
						m.InstallationTargets.Add (id);
				}

			var deps = root.Element (Vs + "Dependencies");
			if (deps is not null)
				foreach (var d in deps.Elements (Vs + "Dependency")) {
					var id = (string)d.Attribute ("Id");
					if (id is { Length: > 0 })
						m.Dependencies.Add (id);
				}

			return m;
		}
	}
}
