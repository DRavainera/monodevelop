using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Minimal read/write access to ~/.config/MonoDevelop/9.0/MonoDevelopProperties.xml
/// using the same Property/key/value format as the legacy IdeApp.Properties, so
/// settings created here stay compatible with the GTK UI.
/// </summary>
public static class UserPreferences
{
	static readonly string filePath = Path.Combine (
		Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
		".config", "MonoDevelop", "9.0", "MonoDevelopProperties.xml");

	static readonly object loadLock = new ();

	public static string? Get (string key)
	{
		try {
			lock (loadLock) {
				var root = Load ();
				return root?.Elements ("Property")
					.FirstOrDefault (e => (string?)e.Attribute ("key") == key)?
					.Attribute ("value")?.Value;
			}
		} catch {
			return null;
		}
	}

	/// <summary>Boolean preference helper ("True"/"False", like the legacy
	/// PropertyService). Values other than True (case-insensitive) are false.</summary>
	public static bool GetBool (string key, bool defaultValue = false)
		=> bool.TryParse (Get (key), out var v) ? v : defaultValue;

	public static void SetBool (string key, bool value)
		=> Set (key, value ? "True" : "False");

	public static void Set (string key, string? value)
	{
		try {
			lock (loadLock) {
				var root = Load () ?? new XElement ("MonoDevelopProperties", new XAttribute ("version", "2.0"));
				var el = root.Elements ("Property").FirstOrDefault (e => (string?)e.Attribute ("key") == key);
				if (value is null) {
					el?.Remove ();
				} else if (el is null) {
					root.Add (new XElement ("Property", new XAttribute ("key", key), new XAttribute ("value", value)));
				} else {
					el.SetAttributeValue ("value", value);
				}
				Save (root);
			}
		} catch (Exception ex) {
			Console.WriteLine ("[prefs] save failed: " + ex.Message);
		}
	}

	static XElement? Load ()
	{
		if (!File.Exists (filePath))
			return null;
		try {
			return XElement.Load (filePath);
		} catch {
			return null;
		}
	}

	static void Save (XElement root)
	{
		Directory.CreateDirectory (Path.GetDirectoryName (filePath)!);
		root.Save (filePath);
	}
}

/// <summary>
/// Recent solutions list backed by UserPreferences (ItemN entries, most recent first),
/// the same place the GTK Welcome page reads from (RecentFiles service).
/// </summary>
public static class RecentSolutions
{
	const string KeyPrefix = "/MonoDevelop/AvaloniaShell/RecentSolutions/Item";
	const int MaxItems = 10;

	public static IReadOnlyList<(string Path, string TimeStamp)> GetAll ()
	{
		var list = new List<(string, string)> ();
		for (int i = 0; i < MaxItems; i++) {
			var v = UserPreferences.Get (KeyPrefix + i);
			if (string.IsNullOrEmpty (v)) break;
			var parts = v.Split ('|', 2);
			list.Add ((parts [0], parts.Length > 1 ? parts [1] : ""));
		}
		return list;
	}

	public static void Add (string path)
	{
		var existing = GetAll ().Where (r => !r.Path.Equals (path, StringComparison.OrdinalIgnoreCase)).ToList ();
		existing.Insert (0, (path, DateTime.Now.ToString ("yyyy-MM-dd HH:mm")));
		while (existing.Count > MaxItems)
			existing.RemoveAt (existing.Count - 1);
		Save (existing);
	}

	public static void Clear () => Save (new List<(string, string)> ());

	/// <summary>
	/// Pinned (favorite) solutions, the legacy RecentFiles.IsFavoriteFile flag.
	/// </summary>
	public static bool IsFavorite (string path)
		=> UserPreferences.Get ($"{KeyPrefix}Fav/{path}") == "true";

	public static void SetFavorite (string path, bool favorite)
		=> UserPreferences.Set ($"{KeyPrefix}Fav/{path}", favorite ? "true" : null);

	static void Save (List<(string Path, string TimeStamp)> items)
	{
		for (int i = 0; i < MaxItems; i++)
			UserPreferences.Set (KeyPrefix + i, i < items.Count ? items [i].Path + "|" + items [i].TimeStamp : null);
	}
}
