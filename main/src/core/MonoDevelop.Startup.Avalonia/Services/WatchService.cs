using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Watch-expression persistence like the legacy DebuggingService.OnStoreUserPrefs:
/// the &lt;sln&gt;.userprefs 'Properties' element keeps a
/// &lt;MonoDevelop.Ide.DebuggingService.PinnedWatches&gt; element whose children
/// carry one expression each. The legacy PinnedWatchStore also serializes the
/// pinned editor location (file/line/column/offset) — meaningless for the
/// shell's Watch pad list, so only the expression round-trips here. The
/// element key matches the legacy one so both IDEs can read the same file.
/// </summary>
public static class WatchService
{
	public const string StoreElement = "MonoDevelop.Ide.DebuggingService.PinnedWatches";

	public static IReadOnlyList<string> Load (string slnPath)
	{
		var result = new List<string> ();
		var prefs = PrefsPath (slnPath);
		if (!File.Exists (prefs))
			return result;
		try {
			var doc = new XmlDocument ();
			doc.Load (prefs);
			var store = doc.DocumentElement?.SelectSingleNode (StoreElement) as XmlElement;
			if (store is null)
				return result;
			foreach (var w in store.ChildNodes.OfType<XmlElement> ()) {
				var expr = w.GetAttribute ("expression");
				if (expr.Length > 0 && !result.Contains (expr))
					result.Add (expr);
			}
		} catch { /* corrupt prefs: empty store */ }
		return result;
	}

	public static void Save (string slnPath, IEnumerable<string> expressions)
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
		foreach (var expr in expressions.Where (e => !string.IsNullOrEmpty (e)).Distinct ()) {
			var we = doc.CreateElement ("Watch");
			we.SetAttribute ("expression", expr);
			store.AppendChild (we);
		}
		root.AppendChild (store);
		Directory.CreateDirectory (Path.GetDirectoryName (prefs)!);
		doc.Save (prefs);
	}

	static string PrefsPath (string slnPath)
		=> slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
}
