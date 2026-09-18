// 
// Gettext.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Addins;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Core
{
	public static class GettextCatalog
	{
		static Thread mainThread;

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern int SetThreadUILanguage (int LangId);

		const int LOCALE_CUSTOM_UNSPECIFIED = 4096;

		public static void Initialize ()
		{
			// no-op, triggers static ctor.
		}

		static Dictionary<string, string> localeToCulture = new Dictionary<string, string> {
			{ "cs", "cs-CZ" },
			{ "de", "de-DE" },
			{ "es", "es-ES" },
			{ "fr", "fr-FR" },
			{ "it", "it-IT" },
			{ "ja", "ja-JP" },
			{ "ko", "ko-KR" },
			{ "pl", "pl-PL" },
			{ "pt", "pt-BR" },
			{ "ru", "ru-RU" },
			{ "tr", "tr-TR" },
			{ "zh_CN", "zh-CN" },
			{ "zh_TW", "zh-TW" },
		};

		static void SetLocale (string locale)
		{
			string cultureLang;
			if (!localeToCulture.TryGetValue (locale, out cultureLang))
				cultureLang = locale.Replace ("_", "-");

			CultureInfo ci;
			try {
				ci = CultureInfo.GetCultureInfo (cultureLang);
			} catch (Exception e) {
				LoggingService.LogError ($"Failed to grab culture {cultureLang}, using default", e);
				return;
			}

			if (ci.IsNeutralCulture) {
				// We need a non-neutral culture
				foreach (CultureInfo c in CultureInfo.GetCultures (CultureTypes.AllCultures & ~CultureTypes.NeutralCultures))
					if (c.Parent != null && c.Parent.Name == ci.Name && c.LCID != LOCALE_CUSTOM_UNSPECIFIED) {
						ci = c;
						break;
					}
			}
			if (!ci.IsNeutralCulture) {
				if (Platform.IsWindows)
					SetThreadUILanguage (ci.LCID);
				mainThread.CurrentUICulture = ci;
				uiCulture = ci;
			}
			if (!Platform.IsWindows)
				Environment.SetEnvironmentVariable ("LANGUAGE", locale);
		}

		static GettextCatalog ()
		{
			mainThread = Thread.CurrentThread;
			uiCulture = CultureInfo.CurrentUICulture;

			//variable can be used to override where Gettext looks for the catalogues
			string catalog = Environment.GetEnvironmentVariable ("MONODEVELOP_LOCALE_PATH");

			// Set the user defined language
			var locale = UILocale = Runtime.Preferences.UserInterfaceLanguage;
			if (string.IsNullOrEmpty (UILocale))
				locale = Environment.GetEnvironmentVariable ("MONODEVELOP_STUB_LANGUAGE");
			if (!string.IsNullOrEmpty (locale))
				SetLocale (locale);
			
			if (string.IsNullOrEmpty (catalog) || !Directory.Exists (catalog)) {
				string location = System.Reflection.Assembly.GetExecutingAssembly ().Location;
				location = Path.GetDirectoryName (location);
				var candidates = new List<string> ();
				if (Platform.IsWindows) {
					// On windows, load the catalog from a child dir
					candidates.Add (Path.Combine (location, "locale"));
				}
				else {
					// MD is located at $prefix/lib/monodevelop/bin
					// adding "../../.." should give us $prefix
					string prefix = Path.GetFullPath (Path.Combine (Path.Combine (Path.Combine (location, ".."), ".."), ".."));
					if (Platform.IsMac)
						prefix = Path.GetFullPath (Path.Combine (prefix, "..", "MacOS"));
					//catalogue is installed to "$prefix/share/locale" by default
					candidates.Add (Path.Combine (Path.Combine (prefix, "share"), "locale"));
					// Development/build layout: the runtime is in <build>/net10run while the
					// catalogues are built to <build>/locale (one level up from the binaries).
					candidates.Add (Path.GetFullPath (Path.Combine (location, "..", "locale")));
				}
				catalog = candidates.FirstOrDefault (Directory.Exists) ?? candidates [0];
			}
			try {
				Catalog.Init ("monodevelop", catalog);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
			}

			// The Add-in Manager (Mono.Addins.Gui) is a legacy component that binds to the
			// Mono.Posix Mono.Unix.Catalog (resolved from the mono GAC by the startup assembly
			// resolver) and not to MonoDevelop.Core.Catalog. Unless that legacy catalog is
			// initialized it has no domain, so every GetString() falls back to the English msgid.
			// Initialize it with the same domain/locale so it issues gettext lookups on the
			// very same .mo files the rest of the IDE uses.
			if (!Platform.IsWindows) {
				try {
					var legacyCatalog = Type.GetType ("Mono.Unix.Catalog, Mono.Posix", false);
					legacyCatalog?.GetMethod ("Init", new Type [] { typeof (string), typeof (string) })
						?.Invoke (null, new object [] { "monodevelop", catalog });
				}
				catch (Exception ex) {
					LoggingService.LogWarning ("Failed to initialize the legacy Mono.Unix.Catalog", ex);
				}
			}

			Environment.SetEnvironmentVariable ("MONODEVELOP_LOCALE_PATH", null);
			Environment.SetEnvironmentVariable ("MONODEVELOP_STUB_LANGUAGE", null);
		}

		public static string UILocale { get; private set; }

		// Cached UI culture. In .NET 5+ reading Thread.CurrentUICulture of another thread
		// (the IDE main thread) throws InvalidOperationException, so callers on worker
		// threads must use this cached value instead.
		static CultureInfo uiCulture;

		public static CultureInfo UICulture {
			get { return uiCulture; }
		}
		
		#region GetString

		static string GetStringInternal (string phrase)
		{
			if (Platform.IsWindows && Thread.CurrentThread.CurrentUICulture != UICulture) {
				Thread.CurrentThread.CurrentUICulture = UICulture;
				SetThreadUILanguage (UICulture.LCID);
			}
			try {
				return Catalog.GetString (phrase);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to localize string", e);
				return phrase;
			}
		}
		
		public static string GetString (string phrase)
		{
			return GetStringInternal (phrase);
		}
		
		public static string GetString (string phrase, object arg0)
		{
			return string.Format (GetStringInternal (phrase), arg0);
		}
		
		public static string GetString (string phrase, object arg0, object arg1)
		{
			return string.Format (GetStringInternal (phrase), arg0, arg1);
		}
		
		public static string GetString (string phrase, object arg0, object arg1, object arg2)
		{
			return string.Format (GetStringInternal (phrase), arg0, arg1, arg2);
		}
		
		public static string GetString (string phrase, params object[] args)
		{
			return string.Format (GetStringInternal (phrase), args);
		}
		
		#endregion
		
		#region GetPluralString

		static string GetPluralStringInternal (string singular, string plural, int number)
		{
			if (Platform.IsWindows && Thread.CurrentThread.CurrentUICulture != UICulture) {
				Thread.CurrentThread.CurrentUICulture = UICulture;
				SetThreadUILanguage (UICulture.LCID);
			}
			try {
				return Catalog.GetPluralString (singular, plural, number);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to localize string", e);
				return number == 1 ? singular : plural;
			}
		}
		
		public static string GetPluralString (string singular, string plural, int number)
		{
			return GetPluralStringInternal (singular, plural, number);
		}
		
		public static string GetPluralString (string singular, string plural, int number, object arg0)
		{
			return string.Format (GetPluralStringInternal (singular, plural, number), arg0);
		}
		
		public static string GetPluralString (string singular, string plural, int number, object arg0, object arg1)
		{
			return string.Format (GetPluralStringInternal (singular, plural, number), arg0, arg1);
		}
		
		public static string GetPluralString (string singular, string plural, int number, 
			object arg0, object arg1, object arg2)
		{
			return string.Format (GetPluralStringInternal (singular, plural, number), arg0, arg1, arg2);
		}
		
		public static string GetPluralString (string singular, string plural, int number, params object[] args)
		{
			return string.Format (GetPluralStringInternal (singular, plural, number), args);
		}
		#endregion
	}
}
