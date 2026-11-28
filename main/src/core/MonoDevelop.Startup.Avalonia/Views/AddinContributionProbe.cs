using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>Standalone probes for the add-in contribution model. Invoked from the
/// QA harness with `avalonia-addons-probe=contributions`; no product behaviour
/// depends on this file.</summary>
public static class AddinContributionProbe
{
	public static string Run ()
	{
		var ui = App.UIServices;
		var lines = new System.Collections.Generic.List<string> ();
		lines.Add ($"commands={ui.Commands.Count} toolbar={ui.ToolbarItems.Count} pads={ui.Pads.Count} pages={ui.OptionPages.Count}");
		lines.Add ($"ids={string.Join (",", ui.AddonIds)}");
		foreach (var p in ui.Pads)
			lines.Add ($"pad {p.Id} label={p.Label} dock={p.Dock} class={p.ClassName ?? "-"} real={AddinPadContentFactory.Supports (p.Id)}");
		foreach (var c in ui.Commands.Take (5))
			lines.Add ($"command {c.Id} label={c.Label}");
		foreach (var t in ui.ToolbarItems.Take (5))
			lines.Add ($"toolbar {t.CommandId} label={t.Label}");
		return string.Join ("\n", lines);
	}

	public static string RegexProbe ()
	{
		var pad = new RegexTesterPad ();
		var field = typeof (RegexTesterPad).GetField ("pattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
		var inputField = typeof (RegexTesterPad).GetField ("input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
		var resultsField = typeof (RegexTesterPad).GetField ("results", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
		((Avalonia.Controls.TextBox)field.GetValue (pad)!)!.Text = "a(b+)c";
		((Avalonia.Controls.TextBox)inputField.GetValue (pad)!)!.Text = "xabcyabbcz";
		typeof (RegexTesterPad).GetMethod ("Schedule", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke (pad, null);
		var matches = Regex.Matches ("xabcyabbcz", "a(b+)c");
		return $"regex={matches.Count} groups={string.Join (",", matches.Cast<Match> ().Select (m => m.Groups [1].Value))} tree={((Avalonia.Controls.TreeView)resultsField.GetValue (pad)!)!.Items.Count}";
	}
}
