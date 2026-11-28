using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.Ide.Controls;

/// <summary>
/// Applies the gettext catalog to a live Avalonia window, so the shell keeps
/// authoring English msgids in .axaml while the UI shows the language picked in
/// Preferences > Environment > Visual Style.
///
/// The first pass records, per element and property, the msgid it found and the
/// value it wrote. Later passes start from that recorded msgid, so a language
/// change re-translates from English instead of translating a translation.
/// </summary>
public static class XamlTranslation
{
	/// <summary>Opt-out for controls whose text is data rather than UI copy —
	/// e.g. the toolbar configuration/run/TFM combos, whose items are solution
	/// configuration names (Debug/Release) and target framework monikers. The
	/// GTK toolbar never translates those either; translating them makes the
	/// painted label disagree with the SelectedItem value used for persistence.
	/// Set on a control to skip it and its whole subtree.</summary>
	public static readonly AvaloniaProperty SkipProperty =
		AvaloniaProperty.RegisterAttached<StyledElement, bool> ("Skip", typeof (XamlTranslation));

	public static void SetSkip (StyledElement element, bool value) => element.SetValue (SkipProperty, value);
	public static bool GetSkip (StyledElement element) => (bool)element.GetValue (SkipProperty)!;

	sealed class Entry
	{
		public string MsgId = "";
		public string Applied = "";
	}

	// element -> property -> entry. Entries hold the msgid per property, never a
	// translated value, which keeps repeated passes idempotent.
	static readonly Dictionary<StyledElement, Dictionary<AvaloniaProperty, Entry>> entries = new ();

	// Properties that carry user-visible text and are safe to reassign in place.
	static readonly AvaloniaProperty [] TextProperties = {
		Window.TitleProperty,
		TextBlock.TextProperty,
		ContentControl.ContentProperty,
		HeaderedContentControl.HeaderProperty,
		HeaderedItemsControl.HeaderProperty,
		TextBox.WatermarkProperty,
	};

	// Windows currently tracked; re-translated in place on language change.
	static readonly List<WeakReference<Window>> windows = new ();

	/// <summary>
	/// Starts translating <paramref name="window"/> and keeps it in sync with
	/// <see cref="GettextService.LanguageChanged"/>. Safe to call before the window
	/// is opened: the first pass runs when the visual tree exists.
	/// </summary>
	public static void Attach (Window window)
	{
		foreach (var reference in windows)
			if (reference.TryGetTarget (out var existing) && ReferenceEquals (existing, window))
				return;
		windows.Add (new WeakReference<Window> (window));

		GettextService.LanguageChanged -= OnLanguageChanged;
		GettextService.LanguageChanged += OnLanguageChanged;

		// Templates and item containers do not exist until the window is laid out,
		// so translate on open and once more after the first layout pass.
		window.Opened += (_, _) => {
			TranslateWindow (window);
			Avalonia.Threading.Dispatcher.UIThread.Post (() => TranslateWindow (window),
				Avalonia.Threading.DispatcherPriority.Background);
		};
	}

	static void OnLanguageChanged (object? sender, EventArgs e)
	{
		for (int i = windows.Count - 1; i >= 0; i--) {
			if (!windows [i].TryGetTarget (out var window)) {
				windows.RemoveAt (i);
				continue;
			}
			TranslateWindow (window);
		}
	}

	/// <summary>Translates every tracked window (used by the language selector).</summary>
	public static void RefreshAll ()
	{
		OnLanguageChanged (null, EventArgs.Empty);
	}

	/// <summary>Translates the window's content plus the code-built children of items controls.</summary>
	public static void TranslateWindow (Window window)
	{
		var visited = new HashSet<Visual> ();
		Translate (window, visited);
	}

	static void Translate (Visual visual, HashSet<Visual> visited)
	{
		if (!visited.Add (visual))
			return;

		// Data-bearing controls (toolbar config/TFM/run combos) opt out for their
		// painted content: their item strings are configuration ids, not UI copy.
		// The control itself is still translated so its tooltip keeps working;
		// only its visual/logical children (the item text) are skipped.
		bool skipChildren = visual is StyledElement element && GetSkip (element);

		if (visual is Control control)
			TranslateProperties (control);

		if (skipChildren)
			return;

		foreach (var child in visual.GetVisualChildren ())
			Translate (child, visited);

		// Item containers and template children live in the logical tree only on
		// the first pass; walk it too so combo boxes and lists get their labels.
		if (!skipChildren && visual is ILogical logical)
			foreach (var child in logical.LogicalChildren)
				if (child is Visual v)
					Translate (v, visited);
	}

	static void TranslateProperties (Control control)
	{
		foreach (var property in TextProperties)
			TranslateProperty (control, property);

		if (control.GetValue (ToolTip.TipProperty) is string tip && tip.Length > 0)
			TranslateToolTip (control, tip);
	}

	static void TranslateProperty (Control control, AvaloniaProperty property)
	{
		if (control.GetValue (property) is not string current || current.Length == 0)
			return;

		var map = GetEntryMap (control);
		if (!map.TryGetValue (property, out var entry)) {
			// The first non-empty value is the authored msgid. Values already set
			// from code are picked up here as well, which is harmless: a string
			// that is not in the catalog is written back unchanged.
			entry = new Entry { MsgId = current };
			map [property] = entry;
		} else if (!string.Equals (current, entry.Applied, StringComparison.Ordinal)) {
			// The property was changed by other code (a dynamic status message,
			// a file path, a runtime-computed label). Never adopt it: translating
			// data would corrupt it, so it is left alone until it is empty again.
			return;
		}

		var translated = GettextService.T (entry.MsgId);
		if (string.Equals (translated, entry.Applied, StringComparison.Ordinal))
			return;
		entry.Applied = translated;
		control.SetValue (property, translated);
	}

	static void TranslateToolTip (Control control, string current)
	{
		var map = GetEntryMap (control);
		if (!map.TryGetValue (ToolTip.TipProperty, out var entry)) {
			entry = new Entry { MsgId = current };
			map [ToolTip.TipProperty] = entry;
		} else if (!string.Equals (current, entry.Applied, StringComparison.Ordinal)) {
			return;
		}
		var translated = GettextService.T (entry.MsgId);
		if (string.Equals (translated, entry.Applied, StringComparison.Ordinal))
			return;
		entry.Applied = translated;
		ToolTip.SetTip (control, translated);
	}

	static Dictionary<AvaloniaProperty, Entry> GetEntryMap (StyledElement element)
	{
		if (!entries.TryGetValue (element, out var map)) {
			map = new Dictionary<AvaloniaProperty, Entry> ();
			entries [element] = map;
		}
		return map;
	}
}
