using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Gettext catalog loader mirroring the legacy GettextCatalog: reads
/// build/locale/<lang>/LC_MESSAGES/monodevelop.mo (the same compiled catalogs the
/// GTK IDE uses) and translates UI strings. The language comes from the same
/// preference the GTK UI uses (MonoDevelop.Ide.UserInterfaceLanguage in
/// MonoDevelopProperties.xml), so both UIs share the setting.
///
/// The shell reuses this service for its own UI (menus, dialogs, XAML markup
/// extension), so the Avalonia and GTK UIs always show the same language and the
/// same .po catalogs. Changing the language reloads the catalog and raises
/// <see cref="LanguageChanged"/> so open windows can re-translate in place.
/// </summary>
public static class GettextService
{
	/// <summary>The legacy preference key (shared with the GTK UI).</summary>
	public const string LanguageKey = "MonoDevelop.Ide.UserInterfaceLanguage";

	// msgid -> translation. Plural forms are stored under the msgctxt-aware key
	// "singular\x00plural" (the gettext convention for msgid_plural entries).
	static Dictionary<string, string>? catalog;
	static string? currentLang;
	static bool catalogLoaded;

	/// <summary>Raised after the language (and therefore the catalog) changed.
	/// The shell subscribes to re-translate open windows.</summary>
	public static event EventHandler? LanguageChanged;

	/// <summary>The active language ("" when the default/system language is used).</summary>
	public static string CurrentLanguage => currentLang ?? "";

	/// <summary>True when a catalog with translations is loaded.</summary>
	public static bool HasCatalog => catalog is { Count: > 0 };

	/// <summary>Initializes the UI language from the legacy preference key.</summary>
	public static void Initialize ()
	{
		var lang = UserPreferences.Get (LanguageKey);
		SetLanguage (string.IsNullOrEmpty (lang) ? null : lang);
	}

	/// <summary>
	/// Switches the UI language and reloads the catalog. Passing null/empty selects
	/// the default (untranslated) language. Raises <see cref="LanguageChanged"/> when
	/// the effective language changes.
	/// </summary>
	public static void SetLanguage (string? lang)
	{
		lang = string.IsNullOrEmpty (lang) ? null : lang;
		if (lang == currentLang && catalogLoaded)
			return;
		currentLang = lang;
		catalogLoaded = true;
		catalog = lang is null ? null : LoadCatalog (lang);
		Console.WriteLine ("[gettext] language=" + (lang ?? "(default)")
			+ " catalog=" + (catalog is { Count: > 0 } ? catalog.Count + " entries" : "(none)"));
		LanguageChanged?.Invoke (null, EventArgs.Empty);
	}

	/// <summary>Translates msgid with the loaded catalog (returns msgid if missing).</summary>
	public static string T (string msgid)
	{
		if (string.IsNullOrEmpty (msgid))
			return msgid;
		if (catalog is null)
			return msgid;
		return catalog.TryGetValue (msgid, out var s) && s.Length > 0 ? s : msgid;
	}

	/// <summary>Translates msgid and formats it with the given arguments.</summary>
	public static string T (string msgid, params object[] args)
	{
		var format = T (msgid);
		if (args.Length == 0)
			return format;
		try {
			return string.Format (CultureInfo.CurrentCulture, format, args);
		} catch (FormatException) {
			// A translation with a broken placeholder must not take the UI down.
			return format;
		}
	}

	/// <summary>
	/// Translates a singular/plural pair, picking the form the locale requires
	/// (legacy GettextCatalog.GetPluralString). The plural msgid is looked up as
	/// "singular\x00plural", the standard gettext encoding.
	/// </summary>
	public static string N (string singular, string plural, int n)
	{
		if (catalog is null)
			return n == 1 ? singular : plural;
		var key = singular + '\0' + plural;
		if (catalog.TryGetValue (key, out var forms) && forms.Length > 0) {
			var list = forms.Split ('\0');
			int index = PluralIndex (n);
			if (index >= 0 && index < list.Length)
				return list [index];
		}
		return n == 1 ? singular : plural;
	}

	// Plural form selection per language (the common gettext rule sets).
	static int PluralIndex (int n)
	{
		var lang = currentLang ?? "";
		// 3 forms: Slavic (ru, uk, pl, cs, sk, hr, sr, be, lt...).
		if (lang is "ru" or "uk" or "pl" or "cs" or "sk" or "hr" or "sr" or "be" or "lt") {
			if (n % 10 == 1 && n % 100 != 11)
				return 0;
			if (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20))
				return 1;
			return 2;
		}
		// 2 forms but the singular is used for 0 too (fr, pt, tr, zh, ja, ko, id, vi).
		if (lang is "fr" or "pt" or "pt_BR" or "tr" or "zh_CN" or "zh_TW" or "ja" or "ko" or "id" or "vi")
			return n == 0 || n == 1 ? 0 : 1;
		return n == 1 ? 0 : 1;
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

		// .mo header: magic (0), revision (4), string count (8), then the offset of
		// the original and translated string tables.
		uint count = Read32 (8);
		uint origOff = Read32 (12);
		uint transOff = Read32 (16);
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
