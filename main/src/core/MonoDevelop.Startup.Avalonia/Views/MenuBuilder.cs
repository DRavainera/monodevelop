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
	public static List<Control> BuildItems (IReadOnlyList<MenuService.MenuEntry> entries)
	{
		var items = new List<Control> ();
		foreach (var entry in entries)
			items.Add (BuildItem (entry, topLevel: true));
		return items;
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
			item.Icon = new TextBlock { Text = "\u2713", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

		if (entry.IsHeader) {
			item.IsEnabled = false;
			return item;
		}

		if (entry.Children.Count > 0) {
			foreach (var child in entry.Children)
				item.Items.Add (BuildItem (child));
		} else if (entry.OnClick is not null) {
			var click = entry.OnClick;
			item.Click += (_, _) => click ();
			if (click.Target is CommandAction ca) {
				item.Tag = ca.Id;
				KeyboardShortcutRegistry.Register (ca.Id, item);
			}
			// Make the legacy shortcut a real accelerator: parse it into a KeyGesture so
			// KeyboardShortcutRegistry.AttachHotKeys binds it and the menu shows it.
			if (!string.IsNullOrEmpty (entry.Shortcut))
				item.InputGesture = ParseGesture (entry.Shortcut);
		}
		return item;
	}

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
