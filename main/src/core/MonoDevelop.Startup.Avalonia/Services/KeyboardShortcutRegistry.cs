using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Registry of the MenuItems that carry a legacy command id. MainWindow parses the
/// key gesture from MenuItem.InputGesture (which MenuService fills from Custom.kb.xml
/// or the legacy default) and binds it via HotKeyManager.SetHotKey — application-level
/// accelerators, so shortcuts work with focus anywhere in the app.
/// </summary>
public static class KeyboardShortcutRegistry
{
	static readonly List<(string CommandId, MenuItem Item)> items = new ();

	public static void Register (string commandId, MenuItem item) => items.Add ((commandId, item));

	/// <summary>Called at the start of each menu rebuild (MainWindow.BuildMenu).</summary>
	public static void Reset ()
	{
		// MenuItems are recreated on every rebuild: drop their HotKey bindings.
		foreach (var (_, item) in items)
			HotKeyManager.SetHotKey (item, null);
		items.Clear ();
	}

	/// <summary>
	/// Binds HotKeys for every registered MenuItem (Avalonia 12 attached-property API).
	/// Called after every rebuild since the items are new instances each time.
	/// </summary>
	public static void AttachHotKeys (Window window)
	{
		foreach (var (commandId, item) in items) {
			if (item.InputGesture is not KeyGesture gesture)
				continue;
			try {
				HotKeyManager.SetHotKey (item, gesture);
			} catch (Exception ex) {
				Console.WriteLine ($"[shortcuts] hotkey {gesture} for {commandId}: {ex.Message}");
			}
		}
	}

	public static IReadOnlyList<(string CommandId, KeyGesture Gesture)> GetBindings ()
	{
		var list = new List<(string, KeyGesture)> ();
		foreach (var (commandId, item) in items) {
			if (item.InputGesture is KeyGesture g && !list.Exists (b => b.Item1 == commandId))
				list.Add ((commandId, g));
		}
		return list;
	}
}
