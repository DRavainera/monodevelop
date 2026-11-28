using System;
using System.Collections.Generic;
using MonoDevelop.AvaloniaAddons.Manifests;

namespace MonoDevelop.AvaloniaAddons.ExtensionModel
{
	/// <summary>
	/// Translates the extension nodes of one manifest into typed UI contributions.
	/// Keeping this mapping in the backend (not in the shell views) is what lets any
	/// add-in — shipped, user-installed or VSIX-imported — contribute the same way.
	/// </summary>
	public static class ManifestContributionParser
	{
		public const string CommandsPoint = "/MonoDevelop/Ide/Commands";
		public const string ToolbarPoint = "/MonoDevelop/Ide/Toolbar";
		public const string PadsPoint = "/MonoDevelop/Ide/Pads";
		public const string OptionsPoint = "/MonoDevelop/Ide/GlobalOptionsDialog";

		public static void Parse (
			string addonId,
			IReadOnlyDictionary<string, List<AddonExtensionNode>> extensions,
			out List<ContributedCommand> commands,
			out List<ToolbarItem> toolbar,
			out List<ContributedPad> pads,
			out List<ContributedOptionPage> pages)
		{
			commands = new List<ContributedCommand> ();
			toolbar = new List<ToolbarItem> ();
			pads = new List<ContributedPad> ();
			pages = new List<ContributedOptionPage> ();
			if (extensions is null)
				return;

			foreach (var (point, nodes) in extensions) {
				if (nodes is null)
					continue;
				if (point == CommandsPoint || point.StartsWith (CommandsPoint + "/", StringComparison.Ordinal)) {
					foreach (var n in nodes) {
						if (string.IsNullOrEmpty (n.Id))
							continue;
						commands.Add (new ContributedCommand (addonId, n.Id, Label (n), EmptyToNull (n.Icon)));
					}
				} else if (point == ToolbarPoint) {
					foreach (var n in nodes) {
						if (string.IsNullOrEmpty (n.Id))
							continue;
						toolbar.Add (new ToolbarItem (addonId, n.Id, Label (n), EmptyToNull (n.Icon)));
					}
				} else if (point.StartsWith (PadsPoint + "/", StringComparison.Ordinal)) {
					// Sub-points such as /Pads/ProjectPad are node builders for an
					// existing pad, not new dockable pads. Only the known dock
					// suffixes introduce real pads.
					var section = point.Substring (PadsPoint.Length + 1).Trim ('/');
					bool dockSection = section.Equals ("Bottom", StringComparison.OrdinalIgnoreCase)
						|| section.Equals ("Output", StringComparison.OrdinalIgnoreCase)
						|| section.Equals ("Right", StringComparison.OrdinalIgnoreCase);
					if (!dockSection)
						continue;
					foreach (var n in nodes) {
						if (string.IsNullOrEmpty (n.Id) || !string.IsNullOrEmpty (n.ChildId))
							continue;
						pads.Add (new ContributedPad (addonId, n.Id, Label (n), EmptyToNull (n.Icon),
							string.IsNullOrEmpty (n.Group) ? ParseDockName (section) : ParseDockName (n.Group), EmptyToNull (n.ClassName)));
					}
				} else if (point == PadsPoint) {
					foreach (var n in nodes) {
						if (string.IsNullOrEmpty (n.Id) || !string.IsNullOrEmpty (n.ChildId))
							continue;
						pads.Add (new ContributedPad (addonId, n.Id, Label (n), EmptyToNull (n.Icon),
							string.IsNullOrEmpty (n.Group) ? PadDock.Left : ParseDockName (n.Group), EmptyToNull (n.ClassName)));
					}
				} else if (point == OptionsPoint || point.StartsWith (OptionsPoint + "/", StringComparison.Ordinal)) {
					foreach (var n in nodes) {
						if (string.IsNullOrEmpty (n.Id))
							continue;
						pages.Add (new ContributedOptionPage (addonId, n.Id, Label (n),
							EmptyToNull (n.Icon), ParentFromPoint (point), EmptyToNull (n.ClassName)));
					}
				}
			}
		}

		static string Label (AddonExtensionNode n)
			=> string.IsNullOrEmpty (n.Label) ? n.Id : n.Label;

		static string? EmptyToNull (string? value)
			=> string.IsNullOrEmpty (value) ? null : value;

		static string? ParentFromPoint (string point)
		{
			var suffix = point.Substring (OptionsPoint.Length).Trim ('/');
			if (suffix.Length == 0)
				return null;
			var parts = suffix.Split ('/', StringSplitOptions.RemoveEmptyEntries);
			return parts.Length == 0 ? null : parts [parts.Length - 1];
		}

		static PadDock ParseDockName (string value)
			=> value.Contains ("bottom", StringComparison.OrdinalIgnoreCase) || value.Contains ("output", StringComparison.OrdinalIgnoreCase)
				|| value.Contains ("logs", StringComparison.OrdinalIgnoreCase)
				? PadDock.Bottom
				: value.Contains ("right", StringComparison.OrdinalIgnoreCase) || value.Contains ("property", StringComparison.OrdinalIgnoreCase)
					? PadDock.Right
					: PadDock.Left;
	}
}
