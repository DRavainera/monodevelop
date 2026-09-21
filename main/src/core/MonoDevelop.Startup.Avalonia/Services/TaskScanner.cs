using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Port of the legacy CommentTasksView scanning: reads the comment tags from the
/// legacy property Monodevelop.TaskListTokens (default "FIXME:2;TODO:1;HACK:1;UNDONE:0",
/// tag:priority) and scans source files of the solution for "// TAG: text" comments,
/// producing task rows (file, line, tag, priority, description).
/// </summary>
public static class TaskScanner
{
	public sealed record TaskRow (string File, int Line, int Column, string Tag, int Priority, string Description);

	public static List<(string Tag, int Priority)> GetTags ()
	{
		var raw = UserPreferences.Get ("Monodevelop.TaskListTokens") ?? "FIXME:2;TODO:1;HACK:1;UNDONE:0";
		var list = new List<(string, int)> ();
		foreach (var part in raw.Split (';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			var split = part.Split (':');
			if (split.Length == 2 && int.TryParse (split [1], out var prio))
				list.Add ((split [0], prio));
			else
				list.Add ((part, 1));
		}
		return list;
	}

	public static List<TaskRow> Scan (string? root)
	{
		var rows = new List<TaskRow> ();
		if (string.IsNullOrEmpty (root) || !Directory.Exists (root))
			return rows;
		var tags = GetTags ();
		// One regex per tag, matching // TAG: or /* TAG: or * TAG: comments.
		var rxByTag = tags.ToDictionary (
			t => t.Tag,
			t => (Tag: t.Tag, Prio: t.Priority, Rx: new Regex (
				$@"(?://|/\*|\*)\s*{Regex.Escape (t.Tag)}\s*:?\s*(?<text>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)));

		foreach (var file in Directory.EnumerateFiles (root, "*.cs", SearchOption.AllDirectories)) {
			if (file.Contains ("/bin/") || file.Contains ("/obj/") || file.Contains ("/.git/"))
				continue;
			string[] lines;
			try {
				if (new FileInfo (file).Length > 1_000_000)
					continue;
				lines = File.ReadAllLines (file);
			} catch {
				continue;
			}
			for (int i = 0; i < lines.Length; i++) {
				foreach (var t in rxByTag.Values) {
					var m = t.Rx.Match (lines [i]);
					if (!m.Success)
						continue;
					var text = m.Groups ["text"].Value.Trim ();
					if (text.Length == 0)
						text = t.Tag;
					rows.Add (new TaskRow (file, i + 1, m.Index + 1, t.Tag, t.Prio, text));
					break;
				}
			}
		}
		// Priority ascending (0 first) like the legacy Task list ordering.
		return rows.OrderBy (r => r.Priority).ThenBy (r => r.File, StringComparer.OrdinalIgnoreCase).ToList ();
	}
}
