using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Minimal gettext catalog loader mirroring the legacy GettextCatalog: reads
/// build/locale/<lang>/LC_MESSAGES/monodevelop.mo (the same compiled catalogs the
/// GTK IDE uses) and translates UI strings. The language comes from the same
/// preference the GTK UI uses (MonoDevelop.Ide.UserInterfaceLanguage in
/// MonoDevelopProperties.xml), so both UIs share the setting.
/// </summary>
public static class GettextService
{
	static Dictionary<string, string>? catalog;
	static string? currentLang;

	public static string CurrentLanguage => currentLang ?? "";

	/// <summary>Initializes the UI language from the legacy preference key.</summary>
	public static void Initialize ()
	{
		var lang = UserPreferences.Get ("MonoDevelop.Ide.UserInterfaceLanguage");
		SetLanguage (string.IsNullOrEmpty (lang) ? null : lang);
	}

	public static void SetLanguage (string? lang)
	{
		lang = string.IsNullOrEmpty (lang) ? null : lang;
		if (lang == currentLang && catalog is not null)
			return;
		currentLang = lang;
		catalog = lang is null ? null : LoadCatalog (lang);
	}

	/// <summary>Translates msgid with the loaded catalog (returns msgid if missing).</summary>
	public static string T (string msgid)
	{
		if (catalog is null || string.IsNullOrEmpty (msgid))
			return msgid;
		return catalog.TryGetValue (msgid, out var s) && s.Length > 0 ? s : msgid;
	}

	static Dictionary<string, string>? LoadCatalog (string lang)
	{
		try {
			var moPath = FindMo (lang);
			return moPath is null ? null : ParseMo (moPath);
		} catch (Exception ex) {
			Console.WriteLine ("[gettext] catalog load failed for " + lang + ": " + ex.Message);
			return null;
		}
	}

	// Locale fallback like gettext: exact (pt_BR), then base language (pt).
	static string? FindMo (string lang)
	{
		var candidates = new List<string> { lang };
		var underscore = lang.IndexOf ('_');
		if (underscore > 0)
			candidates.Add (lang[..underscore]);
		foreach (var root in CandidateRoots ()) {
			foreach (var l in candidates) {
				var p = Path.Combine (root, l, "LC_MESSAGES", "monodevelop.mo");
				if (File.Exists (p))
					return p;
			}
		}
		return null;
	}

	// The legacy build outputs the catalogs under build/locale (net10run runs from
	// main/build/net10run, so ../locale; from bin/Debug/net10.0 the repo-relative
	// main/build/locale is used as fallback).
	static IEnumerable<string> CandidateRoots ()
	{
		var found = new List<string> ();
		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 8 && dir is not null; i++) {
			var candidate = Path.GetFullPath (Path.Combine (dir, "locale"));
			if (Directory.Exists (candidate)) {
				found.Add (candidate);
				break;
			}
			dir = Path.GetDirectoryName (dir);
		}
		dir = AppContext.BaseDirectory;
		for (int i = 0; i < 8 && dir is not null; i++) {
			var candidate = Path.GetFullPath (Path.Combine (dir, "build", "locale"));
			if (Directory.Exists (candidate)) {
				found.Add (candidate);
				break;
			}
			dir = Path.GetDirectoryName (dir);
		}
		return found;
	}

	// Minimal .mo binary reader: little/big endian magic, string table offsets.
	static Dictionary<string, string> ParseMo (string path)
	{
		var result = new Dictionary<string, string> (StringComparer.Ordinal);
		var bytes = File.ReadAllBytes (path);
		if (bytes.Length < 28)
			return result;
		uint magic = BitConverter.ToUInt32 (bytes, 0);
		bool little = magic == 0x950412de;
		if (!little && magic != 0xde120495)
			return result;

		uint Read32 (int offset) => little
			? BitConverter.ToUInt32 (bytes, offset)
			: (uint)((bytes [offset] << 24) | (bytes [offset + 1] << 16) | (bytes [offset + 2] << 8) | bytes [offset + 3]);

		uint count = Read32 (4);
		uint origOff = Read32 (8);
		uint transOff = Read32 (12);
		for (uint i = 0; i < count; i++) {
			uint oLen = Read32 ((int)(origOff + i * 8));
			uint oOff = Read32 ((int)(origOff + i * 8 + 4));
			uint tLen = Read32 ((int)(transOff + i * 8));
			uint tOff = Read32 ((int)(transOff + i * 8 + 4));
			if (oOff + oLen > (uint)bytes.Length || tOff + tLen > (uint)bytes.Length)
				continue;
			var orig = Encoding.UTF8.GetString (bytes, (int)oOff, (int)oLen);
			var trans = Encoding.UTF8.GetString (bytes, (int)tOff, (int)tLen);
			if (orig.Length == 0)
				continue; // header entry
			result [orig] = trans;
		}
		return result;
	}
}
