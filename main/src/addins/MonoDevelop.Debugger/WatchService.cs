using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MonoDevelop.Debugger.Services;

/// <summary>One pinned watch of the editor store (legacy PinnedWatch parity):
/// the expression plus the editor location it is pinned to (file + 1-based
/// line, the column fields are kept for round-trip fidelity).</summary>
public record WatchEntry (string File, int Line, string Expression)
{
	public int Column { get; init; } = 1;
	public int EndLine { get; init; }
	public int EndColumn { get; init; }
	public int OffsetX { get; init; }
	public int OffsetY { get; init; }
}

/// <summary>
/// Watch-expression persistence like the legacy DebuggingService.OnStoreUserPrefs:
/// the &lt;sln&gt;.userprefs 'Properties' element keeps a
/// &lt;MonoDevelop.Ide.DebuggingService.PinnedWatches&gt; element whose children
/// carry one expression each. The legacy PinnedWatchStore also serializes the
/// pinned editor location (file/line/column/offset) — the Watch pad list only
/// round-trips the expression, while the editor pinned-watch bubbles serialize
/// the full location like the legacy adorners. The element key matches the
/// legacy one so both IDEs can read the same file.
/// </summary>
public static class WatchService
{
	public const string StoreElement = "MonoDevelop.Ide.DebuggingService.PinnedWatches";

	/// <summary>Watch-pad expressions only (pre-file/line entries keep working:
	/// a &lt;Watch expression="…"/&gt; without location loads as a pad watch).</summary>
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

	/// <summary>Pinned editor watches: full PinnedWatch rows (file/line/expression).
	/// Entries without a file (pad watches saved by older builds) are skipped.</summary>
	public static IReadOnlyList<WatchEntry> LoadPinned (string slnPath)
	{
		var result = new List<WatchEntry> ();
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
			foreach (var w in store.ChildNodes.OfType<XmlElement> ()) {
				var expr = w.GetAttribute ("expression");
				if (expr.Length == 0)
					continue;
				// Legacy PinnedWatch serializes the file relative to the solution
				// (ProjectPathItemProperty); absolute paths round-trip too.
				var file = w.GetAttribute ("file");
				if (file.Length > 0 && !Path.IsPathRooted (file) && baseDir is not null)
					file = Path.Combine (baseDir, file);
				if (file.Length == 0)
					continue;
				_ = int.TryParse (w.GetAttribute ("line"), out var line);
				_ = int.TryParse (w.GetAttribute ("column"), out var column);
				_ = int.TryParse (w.GetAttribute ("endLine"), out var endLine);
				_ = int.TryParse (w.GetAttribute ("endColumn"), out var endColumn);
				_ = int.TryParse (w.GetAttribute ("offsetX"), out var offsetX);
				_ = int.TryParse (w.GetAttribute ("offsetY"), out var offsetY);
				result.Add (new WatchEntry (file, line, expr) {
					Column = column > 0 ? column : 1,
					EndLine = endLine,
					EndColumn = endColumn,
					OffsetX = offsetX,
					OffsetY = offsetY,
				});
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

	/// <summary>Updates ONLY the pad-watch expression rows, preserving the
	/// pinned editor rows already in the store. The solution-open path must
	/// not wipe the legacy pins it just loaded (a plain Save there would
	/// replace the whole element with expression-only rows).</summary>
	public static void SavePreservingPins (string slnPath, IEnumerable<string> padWatches)
		=> SavePinned (slnPath, padWatches, LoadPinned (slnPath));

	/// <summary>Saves the full pinned store: pad watches as expression-only rows
	/// (they have no editor location) and editor pins with the legacy location
	/// attributes, file relative to the solution when possible.</summary>
	public static void SavePinned (string slnPath, IEnumerable<string> padWatches, IEnumerable<WatchEntry> pinnedWatches)
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
		foreach (var expr in padWatches.Where (e => !string.IsNullOrEmpty (e)).Distinct ()) {
			var we = doc.CreateElement ("Watch");
			we.SetAttribute ("expression", expr);
			store.AppendChild (we);
		}
		foreach (var w in pinnedWatches.Where (w => w.Expression is { Length: > 0 })) {
			var we = doc.CreateElement ("Watch");
			// Legacy PinnedWatch serialization: ProjectPathItemProperty writes the
			// file relative to the solution directory when it lives under it.
			var file = w.File;
			if (!string.IsNullOrEmpty (baseDir) && file.StartsWith (baseDir, StringComparison.Ordinal))
				file = file.Substring (baseDir.Length).TrimStart (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			we.SetAttribute ("file", file);
			we.SetAttribute ("line", Math.Max (1, w.Line).ToString ());
			we.SetAttribute ("column", Math.Max (1, w.Column).ToString ());
			we.SetAttribute ("endLine", w.EndLine.ToString ());
			we.SetAttribute ("endColumn", w.EndColumn.ToString ());
			we.SetAttribute ("offsetX", w.OffsetX.ToString ());
			we.SetAttribute ("offsetY", w.OffsetY.ToString ());
			we.SetAttribute ("expression", w.Expression);
			store.AppendChild (we);
		}
		root.AppendChild (store);
		Directory.CreateDirectory (Path.GetDirectoryName (prefs)!);
		doc.Save (prefs);
	}

	static string PrefsPath (string slnPath)
		=> slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
}
