using System;
using System.Collections.Generic;
using MonoDevelop.AvaloniaAddons.ExtensionModel;

namespace MonoDevelop.AvaloniaAddons
{
	/// <summary>
	/// Default implementation of the services an add-in receives through
	/// <see cref="IAddonContext"/>. It writes into the same observable registry the
	/// manifest contributions use, so an add-in can add commands/pads at runtime and
	/// the shell picks them up without a restart — the VS package model.
	/// Each Publish sends the FULL snapshot (manifest + runtime registrations), so a
	/// runtime registration never drops what the manifest already contributed.
	/// </summary>
	public sealed class AddonShellServices : ICommandService, IPadRegistry
	{
		readonly string addonId;
		readonly IExtensionUIServices ui;
		readonly List<ContributedCommand> commands;
		readonly List<ToolbarItem> toolbar;
		readonly List<ContributedPad> pads;
		readonly List<ContributedOptionPage> pages;

		public AddonShellServices (string addonId, IExtensionUIServices ui,
			IEnumerable<ContributedCommand> initialCommands = null,
			IEnumerable<ToolbarItem> initialToolbar = null,
			IEnumerable<ContributedPad> initialPads = null,
			IEnumerable<ContributedOptionPage> initialPages = null)
		{
			this.addonId = addonId;
			this.ui = ui;
			commands = new List<ContributedCommand> (initialCommands ?? new List<ContributedCommand> ());
			toolbar = new List<ToolbarItem> (initialToolbar ?? new List<ToolbarItem> ());
			pads = new List<ContributedPad> (initialPads ?? new List<ContributedPad> ());
			pages = new List<ContributedOptionPage> (initialPages ?? new List<ContributedOptionPage> ());
		}

		public void RegisterCommand (Command cmd)
		{
			if (cmd is null || string.IsNullOrEmpty (cmd.Id))
				return;
			commands.Add (new ContributedCommand (addonId, cmd.Id, cmd.Label, cmd.Icon));
			Publish ();
		}

		public void RegisterCommandBar (ICommandBar commandBar)
		{
			// A command bar is a set of buttons; declarations are the common case.
			Publish ();
		}

		public void RegisterPad (PadDefinition def)
		{
			if (def is null || string.IsNullOrEmpty (def.Id))
				return;
			pads.Add (new ContributedPad (addonId, def.Id, def.Title, def.Icon, PadDock.Left));
			Publish ();
		}

		/// <summary>Adds a toolbar button from code (the manifest equivalent is the
		/// <c>/MonoDevelop/Ide/Toolbar</c> point).</summary>
		public void RegisterToolbarItem (string commandId, string label, string? icon = null)
		{
			if (string.IsNullOrEmpty (commandId))
				return;
			toolbar.Add (new ToolbarItem (addonId, commandId, label, icon));
			Publish ();
		}

		/// <summary>Adds an options page from code.</summary>
		public void RegisterOptionPage (string id, string label, string? parentId, string? icon = null)
		{
			if (string.IsNullOrEmpty (id))
				return;
			pages.Add (new ContributedOptionPage (addonId, id, label, icon, parentId));
			Publish ();
		}

		void Publish ()
			=> ui.Publish (addonId, commands, toolbar, pads, pages);
	}
}
