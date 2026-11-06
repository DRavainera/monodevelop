using System;
using System.Collections.Generic;
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

	// Oleada 3 (slice 9): command-bound items whose catalog command is an array
	// command (type="...|array"): rendered as dynamic submenus rebuilt from the
	// CommandArrayInfo on every refresh (legacy GTK array menu items).
	static readonly List<Avalonia.Controls.MenuItem> dynamicSubmenus = new ();

	public static List<Control> BuildItems (IReadOnlyList<MenuService.MenuEntry> entries)
	{
		commandItems.Clear (); // the menu is rebuilt: refresh the tracked set
		originalHeaders.Clear ();
		checkEligible.Clear ();
		dynamicSubmenus.Clear ();
		var items = new List<Control> ();
		foreach (var entry in entries)
			items.Add (BuildItem (entry, topLevel: true));
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
		// Oleada 3 (slice 9): rebuild the dynamic array submenus (Window >
		// Document List, Window List) from their current CommandArrayInfo.
		foreach (var item in dynamicSubmenus)
			RebuildArrayChildren (item, (string)item.Tag!);
	}

	// Extracts the legacy command id from a menu action (CommandAction.Target).
	public static string? GetCommandId (Delegate? action)
		=> action?.Target is CommandAction ca ? ca.Id : null;

	static Control BuildItem (MenuService.MenuEntry entry, bool topLevel = false)
	{
		if (entry.IsSeparator)
			return new Separator ();

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
			return item;
		}

		if (entry.Children.Count > 0) {
			foreach (var child in entry.Children)
				item.Items.Add (BuildItem (child));
		} else if (entry.OnClick is not null) {
			var click = entry.OnClick;
			// Oleada 3 (slice 9): array commands (catalog type="...|array",
			// e.g. OpenDocumentList / OpenWindowList) are dynamic submenus —
			// their children are rebuilt on every refresh, so the leaf click
			// action is suppressed (legacy GTK renders them as array menu
			// items, not clickable leaves).
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
				if (isArrayCommand)
					dynamicSubmenus.Add (item); // slice 9: dynamic submenu
			}
			// Make the legacy shortcut a real accelerator: parse it into a KeyGesture so
			// KeyboardShortcutRegistry.AttachHotKeys binds it and the menu shows it.
			if (!string.IsNullOrEmpty (entry.Shortcut))
				item.InputGesture = ParseGesture (entry.Shortcut);
		}
		return item;
	}

	/// <summary>
	/// Oleada 3 (slice 9): (re)builds a dynamic array submenu's children from the
	/// command's current CommandArrayInfo. A child click dispatches the array
	/// command with the child's DataItem (the legacy GTK array menu item
	/// behavior). Full rebuild on every refresh: the headers, checked state and
	/// the dispatch DataItem all come from the current array — an in-place
	/// update would keep the captured DataItem of the document that used to
	/// occupy the slot after a same-count document swap (QA F1). The master
	/// mirrors the legacy array item's empty state: hidden while the array has
	/// no children (legacy inlines the children as siblings instead of a
	/// submenu — documented slice design, deferred to a future slice).
	/// </summary>
	static void RebuildArrayChildren (Avalonia.Controls.MenuItem item, string id)
	{
		var arr = MonoDevelop.Components.Commands.CommandService.GetCommandInfo (id).ArrayInfo;
		var children = item.Items;
		children.Clear ();
		item.IsVisible = arr is { Count: > 0 };
		if (arr is not { Count: > 0 })
			return;
		foreach (var ci in arr) {
			var child = new Avalonia.Controls.MenuItem {
				Header = ci.Text,
				IsEnabled = ci.Enabled,
				Tag = ci.DataItem,
			};
			if (ci.Checked)
				child.Icon = CheckGlyph ();
			var data = ci.DataItem;
			child.Click += (_, _) => MonoDevelop.Components.Commands.CommandService.Dispatch (id, data, MonoDevelop.Components.Commands.CommandSource.MainMenu);
			children.Add (child);
		}
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
