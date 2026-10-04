using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.AvaloniaAddons.Manifests;

namespace MonoDevelop.AvaloniaAddons
{
	/// <summary>Query of contributed extension nodes (same shape the legacy code used).</summary>
	public interface IAddonExtensionRegistry
	{
		IReadOnlyList<AddonExtensionNode> GetExtensionNodes (string path);
		bool HasExtensionPoint (string path);
		IReadOnlyList<string> ExtensionPoints { get; }
	}

	/// <summary>In-memory registry of extension points + nodes contributed by add-ins.</summary>
	public sealed class AddonExtensionRegistry : IAddonExtensionRegistry
	{
		readonly Dictionary<string, List<AddonExtensionNode>> nodes = new (StringComparer.Ordinal);
		readonly HashSet<string> points = new (StringComparer.Ordinal);
		readonly List<string> order = new ();

		public IReadOnlyList<string> ExtensionPoints => order;

		public void DeclareExtensionPoint (string path)
		{
			if (string.IsNullOrEmpty (path) || !points.Add (path))
				return;
			order.Add (path);
			if (!nodes.TryGetValue (path, out _))
				nodes [path] = new List<AddonExtensionNode> ();
		}

		public void AddNode (string path, AddonExtensionNode node)
		{
			if (string.IsNullOrEmpty (path) || node is null)
				return;
			if (!nodes.TryGetValue (path, out var list))
				nodes [path] = list = new List<AddonExtensionNode> ();
			list.Add (node);
		}

		public IReadOnlyList<AddonExtensionNode> GetExtensionNodes (string path)
			=> path is not null && nodes.TryGetValue (path, out var list) ? list : Array.Empty<AddonExtensionNode> ();

		public bool HasExtensionPoint (string path) => path is not null && points.Contains (path);
	}

	/// <summary>Known extension points of the shell (VS-style, declared by the host).</summary>
	public static class ExtensionPoints
	{
		/// <summary>Preferences/Global Options: sections and panels (mirrors the legacy
		/// /MonoDevelop/Ide/GlobalOptionsDialog tree).</summary>
		public const string GlobalOptionsDialog = "/MonoDevelop/Ide/GlobalOptionsDialog";
		/// <summary>Per-mime-type policy pages (legacy MimeTypePolicyPanels).</summary>
		public const string MimeTypePolicyPanels = "/MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels";
		public const string StartupHandlers = "/MonoDevelop/Ide/StartupHandlers";
		public const string Pads = "/MonoDevelop/Ide/Pads";
		public const string Docking = "/MonoDevelop/Ide/Docking";

		public static IEnumerable<string> All { get; } = new [] {
			GlobalOptionsDialog, MimeTypePolicyPanels, StartupHandlers, Pads, Docking,
		};
	}
}
