//
// FontconfigSanitizer.cs — keep the SkiaSharp fontconfig font manager from
// hanging on web-font files (WOFF/WOFF2).
//
// SkFontMgr_fontconfig::GetFamilyNames (libSkiaSharp) iterates the full
// fontconfig family list and calls FcPatternGetString per family; a user
// font directory holding WOFF/WOFF2 files makes that scan loop forever
// (100% CPU, no window) BEFORE Avalonia creates anything. This is
// docs/interfaz-plan.md §M16e/M16f: the hang happens inside
// StartWithClassicDesktopLifetime before the MainWindow constructor runs.
//
// Fix strategy: run this process with a PRIVATE fontconfig configuration
// that keeps every real font (system + user TTF/OTF) but REJECTS the
// web-font containers fontconfig cannot serve to Skia anyway. We reuse the
// system fonts.conf (so distro packaging, cache dirs and aliases stay) and
// append a selectfont/rejectfont rule plus a private cache dir — the
// process-local FONTCONFIG_FILE the app launches with. Nothing outside the
// process is modified: user font files stay where they are.
//
// Detection is self-healing: the sanitizer is applied whenever WOFF/WOFF2
// files exist under the user font directories and FONTCONFIG_FILE is not
// already set by the user.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MonoDevelop.Ide.Services
{
	public static class FontconfigSanitizer
	{
		static readonly string[] UserFontDirs = {
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".local", "share", "fonts"),
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".fonts"),
			// Flatpak per-user font exports land in XDG_DATA_DIRS and fontconfig
			// picks them up too (they were the surviving web fonts on the QA box).
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".local", "share", "flatpak", "exports", "share", "fonts"),
		};

		static readonly string[] WebFontPatterns = { "*.woff", "*.woff2" };

		/// <summary>Applied marker of the sanitizer (QA/self-check).</summary>
		public static bool Applied { get; private set; }

		/// <summary>
		/// True when this run (re)generated the private config — the process will
		/// scan every font file once to build the private cache, which can take
		/// well over the usual startup budget on machines with big user font
		/// collections. The startup watchdog uses it to widen its default.
		/// </summary>
		public static bool FirstRun { get; private set; }

		static string ConfigPath => Path.Combine (
			Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
			"MonoDevelop-Avalonia", "fontconfig.conf");

		/// <summary>
		/// Runs BEFORE any Avalonia/SkiaSharp call: if user web fonts exist and
		/// the environment does not already provide FONTCONFIG_FILE, generate a
		/// private config (system fonts.conf + rejectfont for WOFF/WOFF2 +
		/// private cache dir) and point this process at it.
		/// </summary>
		public static void Apply ()
		{
			try {			if (Applied || !WebFontsPresent ())
				return;
			// Respect an explicit user/environment FONTCONFIG_FILE: whoever set it
			// knows what they are doing (QA harnesses rely on this).
			if (!string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("FONTCONFIG_FILE")))
				return;
			var config = BuildConfig ();
			if (config is null)
				return;
			// Managed setenv alone is NOT enough: Environment.SetEnvironmentVariable
			// only updates the managed environment view. libfontconfig (loaded by
			// libSkiaSharp) reads the variable with getenv(3), so the value must be
			// written into the process environ block via libc setenv — without this
			// the sanitizer config is generated but never applied (M16g).
			Environment.SetEnvironmentVariable ("FONTCONFIG_FILE", config);
			var rc = setenv ("FONTCONFIG_FILE", config, 1);
			if (rc != 0)
				Console.WriteLine ("[fontconfig] WARNING: libc setenv failed (rc=" + rc + "); the native font manager may not see the sanitizer config.");
			Applied = true;
			Console.WriteLine ("[fontconfig] web-font exclusion applied: " + config);
			} catch (Exception ex) {
				// The sanitizer must never break startup; worst case is the
				// pre-fix behavior (and the startup watchdog will report it).
				Console.WriteLine ("[fontconfig] sanitizer skipped: " + ex.Message);
			}
		}

		static bool WebFontsPresent ()
		{
			foreach (var dir in UserFontDirs) {
				if (!Directory.Exists (dir))
					continue;
				foreach (var pattern in WebFontPatterns) {
					try {
						if (Directory.EnumerateFiles (dir, pattern, SearchOption.AllDirectories).Any ())
							return true;
					} catch { /* unreadable dir: not fatal */ }
				}
			}
			return false;
		}

		/// <summary>
		/// Builds (once) the private config: explicit font dirs (system + the
		/// user dirs that exist on this machine) and rejectfont globs for the
		/// leaf directories holding web fonts. Validated fontconfig semantics:
		/// rejectfont <glob> matches with single-level wildcards (dir/*), the
		/// rejected entries stay OUT of the private cache, and the config must
		/// NOT include the distro fonts.conf — its shared xdg cache entries for
		/// the web-font dirs survive the reject and re-trigger the Skia loop.
		/// </summary>
		static string BuildConfig ()
		{
			var path = ConfigPath;
			Directory.CreateDirectory (Path.GetDirectoryName (path)!);
			var cacheDir = Path.Combine (Path.GetDirectoryName (path)!, "fc-cache");
			Directory.CreateDirectory (cacheDir);

			var webDirs = CollectWebFontDirs ();
			var fontDirs = CollectFontDirs ();
			var stamp = string.Join (";", fontDirs) + "|" + string.Join (";", webDirs);
			var marker = Path.Combine (Path.GetDirectoryName (path)!, "generated-for.stamp");
			if (!File.Exists (path) || !File.Exists (marker) || File.ReadAllText (marker) != stamp) {
				FirstRun = true;
				var dirs = new System.Text.StringBuilder ();
				foreach (var dir in fontDirs)
					dirs.Append ("  <dir>").Append (XmlEscape (dir)).Append ("</dir>\n");
				var globs = new System.Text.StringBuilder ();
				foreach (var dir in webDirs)
					globs.Append ("      <glob>").Append (XmlEscape (dir + "/*"))
						.Append ("</glob>\n");
				File.WriteAllText (path,
					"<?xml version=\"1.0\"?>\n" +
					"<!DOCTYPE fontconfig SYSTEM \"fonts.dtd\">\n" +
					"<!-- Auto-generated by MonoDevelop.AvaloniaShell (FontconfigSanitizer).\n" +
					"     Explicit font dir list (system + user); rejects web-font\n" +
					"     containers (WOFF/WOFF2) that make SkiaSharp's fontconfig font\n" +
					"     manager loop forever; private cache dir. -->\n" +
					"<fontconfig>\n" +
					"  <dir>/usr/share/fonts</dir>\n" +
						dirs.ToString () +
					"  <cachedir>" + XmlEscape (cacheDir) + "</cachedir>\n" +
					"  <selectfont>\n" +
					"    <rejectfont>\n" +
						globs.ToString () +
					"    </rejectfont>\n" +
					"  </selectfont>\n" +
					"</fontconfig>\n");
				File.WriteAllText (marker, stamp);
			}
			return path;
		}

		/// <summary>Existing font directories this process should scan (system
		/// default plus the user dirs from the list that exist).</summary>
		static List<string> CollectFontDirs ()
		{
			var result = new List<string> ();
			foreach (var dir in UserFontDirs) {
				if (Directory.Exists (dir))
					result.Add (dir);
			}
			return result;
		}

		/// <summary>Leaf directories holding WOFF/WOFF2 files (the reject globs
		/// enumerate them — fontconfig globs do not support '**').</summary>
		static List<string> CollectWebFontDirs ()
		{
			var result = new List<string> ();
			foreach (var dir in UserFontDirs) {
				if (!Directory.Exists (dir))
					continue;
				foreach (var pattern in WebFontPatterns) {
					try {
						foreach (var file in Directory.EnumerateFiles (dir, pattern, SearchOption.AllDirectories))
							result.Add (Path.GetDirectoryName (file)!);
					} catch { /* unreadable dir: not fatal */ }
				}
			}
			return result.Distinct ().ToList ();
		}

		static string XmlEscape (string s)
			=> s.Replace ("&", "&amp;").Replace ("<", "&lt;").Replace (">", "&gt;");

		// libc setenv: the ONLY way to make an env change visible to native
		// libraries that read getenv(3) (fontconfig reads FONTCONFIG_FILE that
		// way). Managed Environment.SetEnvironmentVariable keeps a managed copy.
		[DllImport ("libc", SetLastError = true, CharSet = CharSet.Ansi)]
		static extern int setenv (string name, string value, int overwrite);
	}
}
