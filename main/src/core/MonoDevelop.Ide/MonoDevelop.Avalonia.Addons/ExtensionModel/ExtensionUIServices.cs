using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.AvaloniaAddons.Manifests;

namespace MonoDevelop.AvaloniaAddons.ExtensionModel
{
	/// <summary>Dock edge a contributed pad asks for (mirrors the legacy
	/// PadAttribute.DockGroup: side pads vs bottom pads).</summary>
	public enum PadDock
	{
		Left,
		Right,
		Bottom,
	}

	/// <summary>A command contributed through the <c>/MonoDevelop/Ide/Commands</c>
	/// extension point. The id is the command the shell dispatches; the label/icon
	/// drive any menu or toolbar button generated from it.</summary>
	public sealed record ContributedCommand (
		string AddonId,
		string Id,
		string Label,
		string? Icon,
		string? DefaultBinding = null);

	/// <summary>A button contributed through the <c>/MonoDevelop/Ide/Toolbar</c>
	/// extension point (or synthesized from the main menu's add-in entries). It runs
	/// <see cref="CommandId"/> through the shell's command dispatcher.</summary>
	public sealed record ToolbarItem (
		string AddonId,
		string CommandId,
		string Label,
		string? Icon);

	/// <summary>A pad contributed through the <c>/MonoDevelop/Ide/Pads</c> extension
	/// point. <see cref="ClassName"/> names the add-in type that builds the content
	/// (an <c>IAvaloniaPadContent</c>); when it is empty or does not resolve the shell
	/// renders a documented placeholder instead of dropping the pad.</summary>
	public sealed record ContributedPad (
		string AddonId,
		string Id,
		string Label,
		string? Icon,
		PadDock Dock,
		string? ClassName = null);

	/// <summary>A settings/options page contributed through the
	/// <c>/MonoDevelop/Ide/GlobalOptionsDialog</c> tree. <see cref="ParentId"/> is the
	/// legacy section id it belongs to (empty = a category heading).</summary>
	public sealed record ContributedOptionPage (
		string AddonId,
		string Id,
		string Label,
		string? Icon,
		string? ParentId,
		string? ClassName = null);

	/// <summary>
	/// Observable registry of the UI contributions every add-in makes, kept by the
	/// backend so the shell can render them without knowing the individual add-ins.
	/// The host publishes/removes entries as add-ins are composed/unloaded, and
	/// <see cref="Changed"/> lets the shell refresh menus, toolbars and pads live —
	/// the Visual Studio model where enabling an extension updates the IDE at once.
	/// </summary>
	public interface IExtensionUIServices
	{
		IReadOnlyList<string> AddonIds { get; }
		IReadOnlyList<ContributedCommand> Commands { get; }
		IReadOnlyList<ToolbarItem> ToolbarItems { get; }
		IReadOnlyList<ContributedPad> Pads { get; }
		IReadOnlyList<ContributedOptionPage> OptionPages { get; }

		/// <summary>Raised after any mutation (add-in composed, unloaded, or a
		/// provider refreshed its contributions).</summary>
		event EventHandler? Changed;

		/// <summary>Replaces every contribution of one add-in (idempotent compose).</summary>
		void Publish (string addonId, IEnumerable<ContributedCommand> commands,
			IEnumerable<ToolbarItem> toolbar, IEnumerable<ContributedPad> pads,
			IEnumerable<ContributedOptionPage> pages);

		/// <summary>Drops every contribution of one add-in (disable/uninstall).</summary>
		void Remove (string addonId);
	}

	/// <summary>Default in-memory implementation. Thread-affine to the UI thread:
	/// the host composes add-ins on the main thread, and the shell reads the
	/// snapshots while rebuilding its chrome.</summary>
	public sealed class ExtensionUIServices : IExtensionUIServices
	{
		sealed class AddonContributions
		{
			public List<ContributedCommand> Commands = new ();
			public List<ToolbarItem> Toolbar = new ();
			public List<ContributedPad> Pads = new ();
			public List<ContributedOptionPage> Pages = new ();
		}

		readonly Dictionary<string, AddonContributions> byAddon = new (StringComparer.Ordinal);
		readonly List<string> order = new ();

		public event EventHandler? Changed;

		public IReadOnlyList<string> AddonIds => order;

		public IReadOnlyList<ContributedCommand> Commands
			=> Flatten (order, c => c.Commands).OrderBy (c => c.Id, StringComparer.Ordinal).ToList ();

		public IReadOnlyList<ToolbarItem> ToolbarItems
			=> Flatten (order, c => c.Toolbar).OrderBy (c => c.CommandId, StringComparer.Ordinal).ToList ();

		public IReadOnlyList<ContributedPad> Pads
			=> Flatten (order, c => c.Pads).OrderBy (p => p.Id, StringComparer.Ordinal).ToList ();

		public IReadOnlyList<ContributedOptionPage> OptionPages
			=> Flatten (order, c => c.Pages).OrderBy (p => p.ParentId ?? "", StringComparer.Ordinal)
				.ThenBy (p => p.Label, StringComparer.Ordinal).ToList ();

		IEnumerable<T> Flatten<T> (IReadOnlyList<string> order, Func<AddonContributions, List<T>> select)
		{
			foreach (var id in order)
				if (byAddon.TryGetValue (id, out var contributions))
					foreach (var item in select (contributions))
						yield return item;
		}

		/// <summary>Deduplicates a contributed command id: the first add-in that
		/// publishes an id wins, so a disabled/uninstalled add-in cannot shadow a live
		/// one and the same command is never listed twice.</summary>
		public void Publish (string addonId, IEnumerable<ContributedCommand> commands,
			IEnumerable<ToolbarItem> toolbar, IEnumerable<ContributedPad> pads,
			IEnumerable<ContributedOptionPage> pages)
		{
			if (string.IsNullOrEmpty (addonId))
				return;
			var entry = new AddonContributions {
				Commands = DistinctCommands (addonId, commands),
				Toolbar = toolbar?.ToList () ?? new (),
				Pads = pads?.ToList () ?? new (),
				Pages = pages?.ToList () ?? new (),
			};
			bool hadEntry = byAddon.ContainsKey (addonId);
			if (!hadEntry)
				order.Add (addonId);
			byAddon [addonId] = entry;
			Changed?.Invoke (this, EventArgs.Empty);
		}

		public void Remove (string addonId)
		{
			if (addonId is not { Length: > 0 } || !byAddon.Remove (addonId))
				return;
			order.Remove (addonId);
			Changed?.Invoke (this, EventArgs.Empty);
		}

		List<ContributedCommand> DistinctCommands (string addonId, IEnumerable<ContributedCommand> commands)
		{
			var result = new List<ContributedCommand> ();
			var seen = new HashSet<string> (StringComparer.Ordinal);
			// Keep ids owned by other live add-ins first, then this add-in's new ones.
			foreach (var c in Commands)
				if (!string.Equals (c.AddonId, addonId, StringComparison.Ordinal))
					seen.Add (c.Id);
			if (commands is null)
				return result;
			foreach (var c in commands) {
				if (string.IsNullOrEmpty (c.Id) || !seen.Add (c.Id))
					continue;
				result.Add (c);
			}
			return result;
		}
	}
}
