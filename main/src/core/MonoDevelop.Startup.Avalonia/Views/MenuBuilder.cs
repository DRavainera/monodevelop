using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Services;
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
		}
		return item;
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
		if (iconId is null || !IconService.IsKnown (iconId))
			return;
		var image = IconService.GetImage (iconId);
		if (image is null)
			return;
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
