using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using MonoDevelop.Ide.Services;
using Avalonia.Interactivity;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Converts MenuEntry trees (legacy menu model) into Avalonia Menu/MenuItem trees,
/// keeping mnemonic underscores, legacy icons, right-aligned shortcut hints and separators.
/// </summary>
public static class MenuBuilder
{
	// Oleada 3: the built MenuItems bound to a legacy command id (CommandAction).
	// RefreshCommandStates mirrors the CommandInfo state onto them.
	static readonly List<Avalonia.Controls.MenuItem> commandItems = new ();

	// Oleada 3 (slice 9): the model-built header of each command-bound item —
	// restored when the command's Update leaves the catalog label unchanged
	// (the catalog label can differ from the model one, e.g. "Activate 1" vs "_1").
	static readonly Dictionary<Avalonia.Controls.MenuItem, string> originalHeaders = new ();

	// Oleada 3 (slice 9): command-bound items whose icon slot is free — their
	// Checked state is mirrored as a ✓ Icon glyph (items with a model icon or a
	// model check keep theirs).
	static readonly HashSet<Avalonia.Controls.MenuItem> checkEligible = new ();

	// Oleada 3 (slice C): command-bound items whose catalog command is an array
	// command (type="...|array"). Legacy GTK renders the master hidden and
	// inlines the array children as siblings right after the master in the
	// parent menu (CommandMenuItem.Update: Visible=false + menu.Insert), so the
	// master is never a submenu: its children are rebuilt into the parent
	// collection on every refresh from the CommandArrayInfo.
	static readonly List<Avalonia.Controls.MenuItem> arrayMasters = new ();

	// Oleada 3 (slice C): the parent Items collection captured at build time
	// (top-level list or the parent submenu's Items). At refresh the master's
	// parent is re-resolved from it (falling back to the visual parent).
	static readonly Dictionary<Avalonia.Controls.MenuItem, IList> arrayMasterParent = new ();


	// Oleada 3 (slice C): the inline children currently rendered for each
	// array master (MenuItems + array separators).
	static readonly Dictionary<Avalonia.Controls.MenuItem, List<Control>> arrayChildren = new ();

	// Oleada 3 (slice 10): submenus carrying the model's AutoHide flag — hidden
	// while every one of their children is invisible (the model's "an empty
	// group disappears" marker, e.g. Tools > Session Recorder while automated
	// testing is disabled). Evaluated on every state refresh, after the child
	// visibility has been mirrored.
	static readonly List<Avalonia.Controls.MenuItem> autoHideSubs = new ();

	public static List<Control> BuildItems (IReadOnlyList<MenuService.MenuEntry> entries)
	{
		commandItems.Clear (); // the menu is rebuilt: refresh the tracked set
		originalHeaders.Clear ();
		checkEligible.Clear ();
		arrayMasters.Clear ();
		arrayMasterParent.Clear ();
		arrayChildren.Clear ();
		var items = new List<Control> ();
		foreach (var entry in entries)
			AppendEntry (entry, items, topLevel: true);
		return items;
	}

	/// <summary>
	/// Oleada 3: mirrors the current state of every command-bound menu item
	/// (runs each command's Update handler without dispatching): Enabled, and
	/// since slice 9 Visible, the dynamic label and the Checked glyph. Called
	/// after the menu is built and on every document/workspace state change.
	/// </summary>
	public static void RefreshCommandStates ()
	{
		foreach (var item in commandItems) {
			if (item.Tag is not string id)
				continue;
			var info = MonoDevelop.Components.Commands.CommandService.GetCommandInfo (id);
			item.IsEnabled = info.Enabled;
			item.IsVisible = info.Visible;
			// Items bound to a synthetic id (e.g. "cmd:welcome") come back as an
			// empty CommandInfo (Command == null) — only catalog-bound items
			// carry a stateful Text/Checked to mirror.
			if (info.Command is { } cmd) {
				// Mirror the label only when the handler actually renamed it,
				// restoring the model header otherwise (the catalog label can
				// differ from the model one — "_1" vs "Activate 1").
				item.Header = info.Text != cmd.Text ? info.Text : originalHeaders[item];
				if (checkEligible.Contains (item))
					item.Icon = info.Checked ? CheckGlyph () : null;
			}
		}
		// Oleada 3 (slice C): re-inline the array children as siblings of their
		// always-hidden master (Window > Document List / Window List, the
		// Navigate History points, View List / Layout List) from the current
		// CommandArrayInfo.
		foreach (var item in arrayMasters)
			RebuildArraySiblings (item, (string)item.Tag!);
		// Oleada 3 (slice 10): apply the AutoHide model flag — a sub disappears
		// while all of its child items are invisible (separators are not
		// counted as children). Runs after the per-item visibility mirror.
		foreach (var item in autoHideSubs)
			item.IsVisible = item.Items.OfType<Avalonia.Controls.MenuItem> ().Any (c => c.IsVisible);
	}

	// Extracts the legacy command id from a menu action (CommandAction.Target).
	public static string? GetCommandId (Delegate? action)
		=> action?.Target is CommandAction ca ? ca.Id : null;

	/// <summary>
	/// Oleada 3 (slice C, item 7): the inline children (MenuItems only, array
	/// separators excluded) currently rendered for the array command `id` — the
	/// QA dump verifies their dispatch payload (Tag) alongside their headers.
	/// </summary>
	public static List<Avalonia.Controls.MenuItem> GetArrayChildren (string id)
	{
		var master = arrayMasters.FirstOrDefault (m => (string)m.Tag! == id);
		if (master is null || !arrayChildren.TryGetValue (master, out var list))
			return new ();
		return list.OfType<Avalonia.Controls.MenuItem> ().ToList ();
	}

	// Oleada 3 (slice C): appends one model entry to `target` (the caller's
	// container — the top-level list or the parent submenu's Items). Array
	// command leaves register their master plus the captured parent; the
	// children themselves only exist once the refresh has inlined them.
	static void AppendEntry (MenuService.MenuEntry entry, IList target, bool topLevel = false)
	{
		if (entry.IsSeparator) {
			target.Add (new Separator ());
			return;
		}

		var item = new MenuItem {
			Header = FormatHeader (entry.Label),
			IsEnabled = !entry.Disabled,
		};
		if (!topLevel)
			SetIcon (item, entry.Icon);
		if (entry.Checked)
			item.Icon = CheckGlyph ();

		if (entry.IsHeader) {
			item.IsEnabled = false;
			target.Add (item);
			return;
		}

		if (entry.Children.Count > 0) {
			target.Add (item);
			foreach (var child in entry.Children)
				AppendEntry (child, item.Items);
			if (entry.AutoHide)
				autoHideSubs.Add (item); // Oleada 3 (slice 10): track for the visibility sweep
		} else if (entry.OnClick is not null) {
			var click = entry.OnClick;
			// Oleada 3 (slice 9 / slice C): array commands (catalog
			// type="...|array", e.g. OpenDocumentList / OpenWindowList) are
			// legacy array menu items — their leaf click is suppressed (the
			// children, inlined by the refresh, dispatch with their dataItem).
			bool isArrayCommand = click.Target is CommandAction arrayCa
				&& MonoDevelop.Components.Commands.CommandService.Manager.GetActionCommand (arrayCa.Id)?.CommandArray == true;
			if (!isArrayCommand)
				item.Click += (_, _) => click ();
			if (click.Target is CommandAction ca) {
				item.Tag = ca.Id;
				KeyboardShortcutRegistry.Register (ca.Id, item);
				commandItems.Add (item); // Oleada 3: track for state refresh
				originalHeaders[item] = (string)item.Header!; // slice 9: label restore source
				if (entry.Icon is null && !entry.Checked)
					checkEligible.Add (item); // slice 9: the ✓ glyph slot is free
				if (isArrayCommand) {
					// Legacy: the master is never rendered — its children are
					// inlined as siblings by RefreshCommandStates.
					item.IsVisible = false;
					arrayMasters.Add (item);
					arrayMasterParent[item] = target;
				}
			}
			// Make the legacy shortcut a real accelerator: parse it into a KeyGesture so
			// KeyboardShortcutRegistry.AttachHotKeys binds it and the menu shows it.
			if (!string.IsNullOrEmpty (entry.Shortcut))
				item.InputGesture = ParseGesture (entry.Shortcut);
			target.Add (item);
		} else {
			target.Add (item);
		}
	}

	/// <summary>
	/// Oleada 3 (slice C): (re)builds an array command's inline children in the
	/// parent menu, right after the (always-hidden) master, from the command's
	/// current CommandArrayInfo — the legacy GTK array rendering
	/// (CommandMenuItem.Update: master Visible=false, children Inserted after
	/// the master, IsArraySeparator → separator). Full rebuild on every
	/// refresh: the headers, checked state, visibility and the dispatch Tag
	/// all come from the current array. Child visibility mirrors the legacy
	/// array children (created with disabledVisible=false):
	/// ci.Visible && ci.Enabled.
	/// </summary>
	static void RebuildArraySiblings (Avalonia.Controls.MenuItem master, string id)
	{
		// Legacy: the array master is never visible, empty array or not (the
		// scalar mirror above may have set info.Visible — force the legacy state).
		master.IsVisible = false;

		IList? parent = arrayMasterParent.TryGetValue (master, out var captured) && captured.Contains (master)
			? captured
			: (master.Parent as ItemsControl)?.Items;
		if (parent is null)
			return;
		int idx = parent.IndexOf (master);
		if (idx < 0)
			return;

		// Drop the previous inline children (they were rebuilt from a stale
		// CommandArrayInfo on the last refresh).
		if (arrayChildren.TryGetValue (master, out var old))
			foreach (var c in old)
				if (parent.IndexOf (c) >= 0)
					RemoveItem (parent, c);

		var arr = MonoDevelop.Components.Commands.CommandService.GetCommandInfo (id).ArrayInfo;
		var children = new List<Control> ();
		if (arr is { Count: > 0 })
			foreach (var ci in arr) {
				if (ci.IsArraySeparator) {
					var sep = new Separator ();
					InsertItem (parent, idx + 1 + children.Count, sep);
					children.Add (sep);
					continue;
				}
				// Ronda 6 (BUG 1): a CommandInfoSet is a named group — the legacy
				// GTK renders it as a SUBMENU (e.g. View List's "Pads"/"Debug
				// Pads"). Build a submenu MenuItem and inline its leaves; each
				// leaf dispatches the master id with its own dataItem, so the
				// nesting level does not change the Run(dataItem) payload.
				if (ci is MonoDevelop.Components.Commands.CommandInfoSet set) {
					var setMenu = new Avalonia.Controls.MenuItem {
						Header = set.Text,
						IsEnabled = set.Enabled,
					};
					AppendArraySetChildren (setMenu, set.CommandInfos, id);
					// An empty group disappears (the "all children invisible"
					// marker, like the model's AutoHide submenus).
					setMenu.IsVisible = setMenu.Items.OfType<Avalonia.Controls.MenuItem> ().Any (c => c.IsVisible);
					InsertItem (parent, idx + 1 + children.Count, setMenu);
					children.Add (setMenu);
					continue;
				}
				var child = new Avalonia.Controls.MenuItem {
					Header = ci.Text,
					IsEnabled = ci.Enabled,
					// Legacy array children are created with disabledVisible=false.
					IsVisible = ci.Visible && ci.Enabled,
					Tag = ci.DataItem,
				};
				if (ci.Checked)
					child.Icon = CheckGlyph ();
				var data = ci.DataItem;
				child.Click += (_, _) => MonoDevelop.Components.Commands.CommandService.Dispatch (id, data, MonoDevelop.Components.Commands.CommandSource.MainMenu);
				InsertItem (parent, idx + 1 + children.Count, child);
				children.Add (child);
			}
		arrayChildren[master] = children;
	}

	// Ronda 6 (BUG 1): fills a set submenu's Items from a CommandArrayInfo —
	// the leaves of a CommandInfoSet. Each leaf dispatches the ARRAY MASTER id
	// (not the set) with its own dataItem, matching the flat-array case and the
	// legacy handler's Run(dataItem) (which forwards the dataItem regardless of
	// which CommandInfo it came from). Recurses for a nested set and renders
	// separators.
	static void AppendArraySetChildren (Avalonia.Controls.MenuItem setMenu, MonoDevelop.Components.Commands.CommandArrayInfo arr, string id)
	{
		foreach (var ci in arr) {
			if (ci.IsArraySeparator) {
				setMenu.Items.Add (new Separator ());
				continue;
			}
			if (ci is MonoDevelop.Components.Commands.CommandInfoSet nested) {
				var sub = new Avalonia.Controls.MenuItem { Header = nested.Text, IsEnabled = nested.Enabled };
				AppendArraySetChildren (sub, nested.CommandInfos, id);
				sub.IsVisible = sub.Items.OfType<Avalonia.Controls.MenuItem> ().Any (c => c.IsVisible);
				setMenu.Items.Add (sub);
				continue;
			}
			var child = new Avalonia.Controls.MenuItem {
				Header = ci.Text,
				IsEnabled = ci.Enabled,
				IsVisible = ci.Visible && ci.Enabled,
				Tag = ci.DataItem,
			};
			if (ci.Checked)
				child.Icon = CheckGlyph ();
			var data = ci.DataItem;
			child.Click += (_, _) => MonoDevelop.Components.Commands.CommandService.Dispatch (id, data, MonoDevelop.Components.Commands.CommandSource.MainMenu);
			setMenu.Items.Add (child);
		}
	}

	// Oleada 3 (slice C): Avalonia 12's ItemCollection (ItemsSourceView) throws
	// "Collection is read-only." from its EXPLICIT IList mutation methods even
	// in writable Items mode — mutation must go through the concrete public
	// methods, which route to the backing collection. Plain lists keep IList.
	static void InsertItem (IList parent, int index, Control item)
	{
		if (parent is ItemCollection ic)
			ic.Insert (index, item);
		else
			parent.Insert (index, item);
	}

	static void RemoveItem (IList parent, Control item)
	{
		if (parent is ItemCollection ic)
			ic.Remove (item);
		else
			parent.Remove (item);
	}

	// The checked-state glyph (legacy CheckMenuItem.Active parity) — a fresh
	// instance per assignment, since an Avalonia control has a single parent.
	static TextBlock CheckGlyph ()
		=> new () { Text = "\u2713", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

	/// <summary>Parses a legacy/shell shortcut label ("Ctrl Shift N", "Ctrl+M", chords
	/// "Ctrl+M|N") into an Avalonia KeyGesture. Chords/alternates are not representable
	/// by KeyGesture, so the first chord is used for the menu accelerator.</summary>
	static KeyGesture? ParseGesture (string shortcut)
	{
		var s = shortcut.Replace ('+', ' ');
		var pipe = s.IndexOf ('|');
		if (pipe >= 0)
			s = s.Substring (0, pipe);
		var mods = KeyModifiers.None;
		Key? key = null;
		foreach (var tok in s.Split (' ', StringSplitOptions.RemoveEmptyEntries)) {
			switch (tok.ToLowerInvariant ()) {
			case "ctrl": case "control": mods |= KeyModifiers.Control; break;
			case "shift": mods |= KeyModifiers.Shift; break;
			case "alt": mods |= KeyModifiers.Alt; break;
			case "meta": case "cmd": case "super": case "win": mods |= KeyModifiers.Meta; break;
			default:
				if (Enum.TryParse<Key> (tok, ignoreCase: true, out var k))
					key = k;
				break;
			}
		}
		return key is null ? null : new KeyGesture (key.Value, mods);
	}

	static string FormatHeader (string label)
	{
		// Mnemonics: keep the legacy "_File" convention (Avalonia renders the underscore
		// literally, which matches the GTK look when alt is not pressed).
		return label;
	}

	// Legacy icons live in the IconPresenter slot (Fluent theme part name in Avalonia 12).
	static void SetIcon (MenuItem item, string? iconId)
	{
		if (iconId is null || !IconService.IsKnown (iconId)) {
			Console.Error.WriteLine ($"[menu-icon] '{iconId}' skipped (known={iconId is not null && IconService.IsKnown (iconId)})");
			return;
		}
		var image = IconService.GetImage (iconId);
		if (image is null) {
			Console.Error.WriteLine ($"[menu-icon] '{iconId}' resolved to NULL");
			return;
		}
		var host = new Panel { Width = 22, Height = 18 };
		var img = new Avalonia.Controls.Image {
			Source = image,
			Width = 16,
			Height = 16,
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
			VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
		};
		host.Children.Add (img);
		item.Icon = host;
	}
}
