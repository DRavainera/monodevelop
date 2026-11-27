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
	static string? effectiveLang;
	static bool catalogLoaded;

	/// <summary>Raised after the language (and therefore the catalog) changed.
	/// The shell subscribes to re-translate open windows.</summary>
	public static event EventHandler? LanguageChanged;

	/// <summary>The active language ("" when the default/system language is used).</summary>
	public static string CurrentLanguage => currentLang ?? "";

	/// <summary>
	/// Effective language: the explicit preference when set, otherwise the system
	/// language resolved from the environment (gettext's LANGUAGE/LC_ALL/LANG plus
	/// the .NET UI culture). "" when no catalog matches the system locale.
	/// </summary>
	public static string EffectiveLanguage => effectiveLang ?? "";

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
	/// the default (system) language, resolved the same way the legacy GettextCatalog
	/// does. Raises <see cref="LanguageChanged"/> when the effective language changes.
	/// </summary>
	public static void SetLanguage (string? lang)
	{
		lang = string.IsNullOrEmpty (lang) ? null : lang;
		if (lang == currentLang && catalogLoaded)
			return;
		currentLang = lang;
		catalogLoaded = true;
		var effective = lang ?? ResolveSystemLanguage ();
		effectiveLang = effective;
		catalog = effective is null ? null : LoadCatalog (effective);
		Console.WriteLine ("[gettext] language=" + (lang ?? "(default)")
			+ " effective=" + (effective ?? "(none)")
			+ " catalog=" + (catalog is { Count: > 0 } ? catalog.Count + " entries" : "(none)"));
		LanguageChanged?.Invoke (null, EventArgs.Empty);
	}

	/// <summary>
	/// Resolves the system UI language in the same order the legacy GettextCatalog /
	/// GNU gettext uses: MONODEVELOP_STUB_LANGUAGE, the LANGUAGE list, LC_ALL,
	/// LC_MESSAGES, LANG, then .NET's UI culture. A locale such as "es_ES.UTF-8@euro"
	/// is normalized to "es_ES" / "es" and matched against the available catalogs,
	/// so (Default) actually translates the UI.
	/// </summary>
	public static string? ResolveSystemLanguage ()
	{
		foreach (var candidate in SystemLanguageCandidates ()) {
			if (FindMo (candidate) is not null)
				return candidate;
		}
		return null;
	}

	static IEnumerable<string> SystemLanguageCandidates ()
	{
		var seen = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		// The legacy GettextCatalog uses MONODEVELOP_STUB_LANGUAGE as an override
		// when no language is persisted, then lets gettext resolve the system one.
		foreach (var envName in new[] { "MONODEVELOP_STUB_LANGUAGE", "LANGUAGE", "LC_ALL", "LC_MESSAGES", "LANG" }) {
			var value = Environment.GetEnvironmentVariable (envName);
			if (string.IsNullOrWhiteSpace (value))
				continue;
			// LANGUAGE is a colon-separated preference list; the others are single.
			foreach (var part in value.Split (new[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries))
				foreach (var form in NormalizeLocale (part))
					if (seen.Add (form))
						yield return form;
		}
		foreach (var form in NormalizeLocale (CultureInfo.CurrentUICulture.Name))
			if (seen.Add (form))
				yield return form;
		// Never pick "en" by default: an untranslated UI is the expected fallback.
	}

	static IEnumerable<string> NormalizeLocale (string locale)
	{
		if (string.IsNullOrWhiteSpace (locale))
			yield break;
		var value = locale.Trim ();
		int at = value.IndexOf ('@');
		if (at > 0)
			value = value [..at];
		int dot = value.IndexOf ('.');
		if (dot > 0)
			value = value [..dot];
		value = value.Replace ('-', '_');
		if (value.Length == 0 || value.Equals ("C", StringComparison.OrdinalIgnoreCase)
			|| value.Equals ("POSIX", StringComparison.OrdinalIgnoreCase))
			yield break;
		yield return value; // exact form first (zh_CN, pt_BR)
		int underscore = value.IndexOf ('_');
		if (underscore > 0)
			yield return value [..underscore]; // base language (zh, pt)
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
		// The effective language drives the plural rules: with (Default) it is the
		// resolved system locale, not the empty preference.
		var lang = effectiveLang ?? currentLang ?? "";
		// Plural rules are per language, so a regional locale ("ru_RU") must also
		// match its base rule set ("ru"); the full form is tried first because the
		// Chinese rules are keyed by script/region ("zh_CN").
		int separator = lang.IndexOf ('_');
		var baseLang = separator > 0 ? lang [..separator] : lang;
		// 3 forms: Slavic (ru, uk, pl, cs, sk, hr, sr, be, lt...).
		if (IsSlavic (lang) || IsSlavic (baseLang)) {
			if (n % 10 == 1 && n % 100 != 11)
				return 0;
			if (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20))
				return 1;
			return 2;
		}
		// 2 forms but the singular is used for 0 too (fr, pt, tr, zh, ja, ko, id, vi).
		if (IsZeroSingular (lang) || IsZeroSingular (baseLang))
			return n == 0 || n == 1 ? 0 : 1;
		return n == 1 ? 0 : 1;
	}

	static bool IsSlavic (string lang)
		=> lang is "ru" or "uk" or "pl" or "cs" or "sk" or "hr" or "sr" or "be" or "lt";

	static bool IsZeroSingular (string lang)
		=> lang is "fr" or "pt" or "pt_BR" or "tr" or "zh_CN" or "zh_TW" or "ja" or "ko" or "id" or "vi";

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
