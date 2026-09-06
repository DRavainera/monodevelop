// 
// Catalog.cs
// 
// Author:
//   MonoDevelop contributors
// 
// Copyright (C) 2026
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MonoDevelop.Core
{
	// Internal replacement for Mono.Posix' Mono.Unix.Catalog. Reads GNU gettext .mo binary
	// catalogs (little and big endian) so the localization subsystem no longer depends on the
	// legacy Mono.Posix assembly. Exposes the same surface used by GettextCatalog: Init, GetString,
	// GetPluralString.
	public static class Catalog
	{
		const uint MagicLittleEndian = 0x950412de;
		const uint MagicBigEndian = 0xde120495;

		static string pluralForm;
		static object loadLock = new object ();
		static Dictionary<string, TranslationEntry> entries = new Dictionary<string, TranslationEntry> (StringComparer.Ordinal);

		struct TranslationEntry
		{
			public string[] Strings; // translations, plural forms stored in order
		}

		/// <summary>Loads the gettext catalogue for the given domain. Path points at the directory
		/// that contains the &lt;lang&gt;/LC_MESSAGES/&lt;domain&gt;.mo files (the standard gettext
		/// layout). Falls back silently when the catalogue is not found.</summary>
		public static void Init (string domain, string path)
		{
			lock (loadLock) {
				entries.Clear ();
				pluralForm = null;
				var moFile = FindMoFile (domain, path);
				if (moFile != null)
					Load (moFile);
			}
		}

		static string FindMoFile (string domain, string path)
		{
			if (string.IsNullOrEmpty (path) || !Directory.Exists (path))
				return null;

			var culture = CultureInfo.CurrentUICulture;
			if (culture == null || culture.Equals (CultureInfo.InvariantCulture)) {
				// Try to derive from the LANGUAGE/LC_MESSAGES style locale first (e.g. "pt_BR").
				foreach (var lang in GetLocaleCandidates (CultureInfo.CurrentCulture)) {
					var f = LookupMoFile (domain, path, lang);
					if (f != null)
						return f;
				}
				return null;
			}

			var names = new List<string> ();
			names.Add (culture.Name); // zh-CN
			names.Add (culture.TwoLetterISOLanguageName); // zh
			if (!string.IsNullOrEmpty (culture.Name)) {
				// POSIX underscore form zh_CN
				names.Add (culture.Name.Replace ('-', '_'));
			}
			foreach (var lang in names) {
				var f = LookupMoFile (domain, path, lang);
				if (f != null)
					return f;
			}
			return null;
		}

		static IEnumerable<string> GetLocaleCandidates (CultureInfo culture)
		{
			if (culture != null && !culture.Equals (CultureInfo.InvariantCulture)) {
				if (!string.IsNullOrEmpty (culture.Name))
					yield return culture.Name;
				yield return culture.Name.Replace ('-', '_');
				yield return culture.TwoLetterISOLanguageName;
			}
			// fall back to LC environment style
			var env = Environment.GetEnvironmentVariable ("LANGUAGE") ?? Environment.GetEnvironmentVariable ("LC_ALL") ??
				Environment.GetEnvironmentVariable ("LANG");
			if (!string.IsNullOrEmpty (env)) {
				foreach (var part in env.Split (':')) {
					var lang = part.Trim ();
					if (lang.Length > 0)
						yield return lang;
				}
			}
		}

		static string LookupMoFile (string domain, string path, string lang)
		{
			if (string.IsNullOrEmpty (lang))
				return null;
			var file = Path.Combine (Path.Combine (Path.Combine (path, lang), "LC_MESSAGES"), domain + ".mo");
			return File.Exists (file) ? file : null;
		}

		static void Load (string moFile)
		{
			try {
				using (var fs = new FileStream (moFile, FileMode.Open, FileAccess.Read))
				using (var reader = new BinaryReader (fs)) {
					uint magic = reader.ReadUInt32 ();
					bool littleEndian;
					if (magic == MagicLittleEndian)
						littleEndian = true;
					else if (magic == MagicBigEndian)
						littleEndian = false;
					else
						return;

					reader.ReadUInt32 (); // revision
					uint n = Read32 (reader, littleEndian);
					uint origOffset = Read32 (reader, littleEndian);
					uint tranOffset = Read32 (reader, littleEndian);
					Read32 (reader, littleEndian); // hash table size (unused)
					Read32 (reader, littleEndian); // hash table offset (unused)

					var origLens = new uint[n];
					var origOffs = new uint[n];
					var tranLens = new uint[n];
					var tranOffs = new uint[n];

					// GNU gettext stores each table as a run of (length, offset) pairs. Read the
					// original, then the translation, string table.
					reader.BaseStream.Seek (origOffset, SeekOrigin.Begin);
					for (uint i = 0; i < n; i++) {
						origLens[i] = Read32 (reader, littleEndian);
						origOffs[i] = Read32 (reader, littleEndian);
					}

					reader.BaseStream.Seek (tranOffset, SeekOrigin.Begin);
					for (uint i = 0; i < n; i++) {
						tranLens[i] = Read32 (reader, littleEndian);
						tranOffs[i] = Read32 (reader, littleEndian);
					}

					for (uint i = 0; i < n; i++) {
						string key = ReadString (reader, origLens[i], origOffs[i]);
						string tran = ReadString (reader, tranLens[i], tranOffs[i]);

						// msgstr lines are concatenated with the platform newline. Plural forms are
						// separated by the platform EOL (the environment concatenates them).
						string[] strings;
						if (tran.IndexOf ('\n') >= 0)
							strings = tran.Split (new [] { "\n" }, StringSplitOptions.None);
						else
							strings = new[] { tran };

						entries[key] = new TranslationEntry { Strings = strings };

						if (key == "")
							pluralForm = ExtractPluralForm (strings);
					}
				}
			} catch {
				// Never break the app because of a bad catalogue; leave it empty.
				return;
			}
		}

		static string ExtractPluralForm (string[] headerLines)
		{
			foreach (var line in headerLines) {
				int idx = line.IndexOf ("plural=", StringComparison.Ordinal);
				if (idx >= 0) {
					int start = idx + "plural=".Length;
					int end = line.IndexOf (';', start);
					if (end < 0)
						end = line.Length;
					return line.Substring (start, end - start).Trim ();
				}
			}
			return null;
		}

		/// <summary>Looks up <paramref name="phrase"/> in the loaded catalogue, returning the source
		/// text verbatim when untranslated.</summary>
		public static string GetString (string phrase)
		{
			if (string.IsNullOrEmpty (phrase))
				return phrase;
			TranslationEntry e;
			if (entries.TryGetValue (phrase, out e) && e.Strings.Length > 0)
				return e.Strings[0];
			return phrase;
		}

		/// <summary>Plural lookup using the catalogue's plural formula when available.</summary>
		public static string GetPluralString (string singular, string plural, int number)
		{
			TranslationEntry e;
			if (!entries.TryGetValue (singular, out e) || e.Strings.Length == 0)
				return number == 1 ? singular : plural;

			int index = PluralIndex (pluralForm, number);
			if (index < 0 || index >= e.Strings.Length)
				return number == 1 ? singular : plural;
			var result = e.Strings[index];
			return string.IsNullOrEmpty (result) ? (number == 1 ? singular : plural) : result;
		}

		static int PluralIndex (string form, int n)
		{
			// Default: n==1 ? 0 : 1 (matches the common "Plural-Forms: nplurals=2; plural=(n != 1);")
			if (string.IsNullOrEmpty (form)) {
				if (CultureInfo.CurrentCulture.IsNeutralCulture)
					return 0;
				return n == 1 ? 0 : 1;
			}
			try {
				// Very limited evaluator supporting the common plural expressions: n, ==, !=, &&
				var expr = form.Trim ();
				int r = EvaluatePlural (expr, n);
				return r;
			} catch {
				return n == 1 ? 0 : 1;
			}
		}

		static int EvaluatePlural (string expr, int n)
		{
			// Handler for the tiny grammar: const?==n?n!=expr; commonly "(n != 1)" and friends.
			// We support: primary == n, primary != n, and (a && b) where a/b are tests. This
			// intentionally avoids a full C-expression parser; the .po files here use the standard
			// "(n % 10 == ...)" forms too, which fall back to the default when unsupported.
			expr = expr.Trim ();
			while (expr.StartsWith ("(", StringComparison.Ordinal) && expr.EndsWith (")", StringComparison.Ordinal))
				expr = expr.Substring (1, expr.Length - 2).Trim ();

			if (expr.Contains ("&&"))
				return EvaluateIntAnd (expr, n);
			if (expr.Contains ("||"))
				return EvaluateIntOr (expr, n);

			// gettext plural forms are "plural =" given in the header; the value here is already
			// the RHS. Evaluate a boolean returning 0 or 1.
			return EvalValue (expr, n) ? 1 : 0;
		}

		static int EvaluateIntAnd (string expr, int n)
		{
			// split on && at top level (not in parens) - simple split is enough for our forms
			foreach (var part in expr.Split (new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries)) {
				if (!EvalValue (part, n))
					return 0;
			}
			return 1;
		}

		static int EvaluateIntOr (string expr, int n)
		{
			foreach (var part in expr.Split (new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)) {
				if (EvalValue (part, n))
					return 1;
			}
			return 0;
		}

		static bool EvalValue (string expr, int n)
		{
			expr = expr.Trim ();
			while (expr.StartsWith ("(", StringComparison.Ordinal) && expr.EndsWith (")", StringComparison.Ordinal))
				expr = expr.Substring (1, expr.Length - 2).Trim ();

			// n == c, n != c
			if (expr.Contains ("==")) {
				var parts = expr.Split (new[] { "==" }, StringSplitOptions.None);
				return int.Parse (parts[0].Trim ().Replace ("n", n.ToString ())) == int.Parse (parts[1].Trim ().Replace ("n", n.ToString ()));
			}
			if (expr.Contains ("!=")) {
				var parts = expr.Split (new[] { "!=" }, StringSplitOptions.None);
				return int.Parse (parts[0].Trim ().Replace ("n", n.ToString ())) != int.Parse (parts[1].Trim ().Replace ("n", n.ToString ()));
			}
			// "n" alone means true (non-zero)
			int val;
			if (int.TryParse (expr.Trim ().Replace ("n", n.ToString ()), out val))
				return val != 0;
			// unsupported => true by default for n, keeping first plural
			return true;
		}

		static uint Read32 (BinaryReader reader, bool littleEndian)
		{
			// Always read as little-endian then flip if the file is big-endian.
			uint value = reader.ReadUInt32 ();
			if (!littleEndian)
				return ((value & 0x000000FFu) << 24) | ((value & 0x0000FF00u) << 8) |
					((value & 0x00FF0000u) >> 8) | ((value & 0xFF000000u) >> 24);
			return value;
		}

		static string ReadString (BinaryReader reader, uint len, uint offset)
		{
			long cur = reader.BaseStream.Position;
			long saved = cur;
			try {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				byte[] bytes = reader.ReadBytes ((int)len);
				var s = Encoding.UTF8.GetString (bytes);
				if (s.EndsWith ("\0", StringComparison.Ordinal))
					s = s.TrimEnd ('\0');
				return s;
			} finally {
				reader.BaseStream.Seek (saved, SeekOrigin.Begin);
			}
		}
	}
}