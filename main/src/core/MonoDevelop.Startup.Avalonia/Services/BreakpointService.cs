using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MonoDevelop.AvaloniaShell.Services;

public record BreakpointEntry (string FileName, int Line, bool Enabled);

/// <summary>
/// Breakpoint persistence like the legacy DebuggingService.OnStoreUserPrefs:
/// the &lt;sln&gt;.userprefs 'Properties' element keeps a
/// &lt;MonoDevelop.Ide.DebuggingService.Breakpoints&gt; element containing
/// &lt;Breakpoint file="…" relfile="…" line="…" column="1" [enabled="false"]/&gt;
/// children — the same shape Mono.Debugging's BreakpointStore.Save/Load writes.
/// </summary>
public static class BreakpointService
{
	public const string StoreElement = "MonoDevelop.Ide.DebuggingService.Breakpoints";

	public static IReadOnlyList<BreakpointEntry> Load (string slnPath)
	{
		var result = new List<BreakpointEntry> ();
		var prefs = PrefsPath (slnPath);
		if (!File.Exists (prefs))
			return result;
		try {
			var doc = new XmlDocument ();
			doc.Load (prefs);
			var store = doc.DocumentElement?.SelectSingleNode (StoreElement) as XmlElement;
			if (store is null)
				return result;
			var baseDir = Path.GetDirectoryName (slnPath)!;
			foreach (var bp in store.SelectNodes ("Breakpoint")!.OfType<XmlElement> ()) {
				var file = bp.GetAttribute ("relfile") is { Length: > 0 } rel && baseDir is not null
					? Path.Combine (baseDir, rel)
					: bp.GetAttribute ("file");
				if (file.Length == 0)
					continue;
				_ = int.TryParse (bp.GetAttribute ("line"), out var line);
				var enabled = bp.GetAttribute ("enabled") != "false";
				result.Add (new BreakpointEntry (file, line, enabled));
			}
		} catch { /* corrupt prefs: empty store */ }
		return result;
	}

	public static void Save (string slnPath, IEnumerable<BreakpointEntry> breakpoints)
	{
		var prefs = PrefsPath (slnPath);
		XmlDocument doc;
		if (File.Exists (prefs)) {
			doc = new XmlDocument ();
			try {
				doc.Load (prefs);
			} catch {
				doc = new XmlDocument ();
				doc.AppendChild (doc.CreateElement ("Properties"));
			}
		} else {
			doc = new XmlDocument ();
			doc.AppendChild (doc.CreateElement ("Properties"));
		}
		var root = doc.DocumentElement!;
		var old = root.SelectSingleNode (StoreElement);
		if (old is not null)
			root.RemoveChild (old);
		var store = doc.CreateElement (StoreElement);
		var baseDir = Path.GetDirectoryName (slnPath);
		foreach (var bp in breakpoints) {
			var be = doc.CreateElement ("Breakpoint");
			be.SetAttribute ("file", bp.FileName);
			if (baseDir is not null && bp.FileName.StartsWith (baseDir, StringComparison.Ordinal))
				be.SetAttribute ("relfile", bp.FileName.Substring (baseDir.Length).TrimStart (Path.DirectorySeparatorChar));
			be.SetAttribute ("line", Math.Max (1, bp.Line).ToString ());
			be.SetAttribute ("column", "1");
			if (!bp.Enabled)
				be.SetAttribute ("enabled", "false");
			store.AppendChild (be);
		}
		root.AppendChild (store);
		Directory.CreateDirectory (Path.GetDirectoryName (prefs)!);
		doc.Save (prefs);
	}

	static string PrefsPath (string slnPath)
		=> slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
}
