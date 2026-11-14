// 
// UserProfile.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;

namespace MonoDevelop.Core
{
	public class UserProfile
	{
		const string PROFILE_ENV_VAR = "MONODEVELOP_PROFILE";

		//These are the known profile versions that can be migrated.
		//MUST BE SORTED, low to high.
		//The last is the current profile version.
		//Should be increased only for major MD versions. Cannot contain '+' or '-'.
		internal static string[] ProfileVersions = new[] {
			"2.4",
			"2.6",
			"2.7",
			"2.8",
			"3.0",
			"4.0",
			"5.0",
			"6.0",
			"7.0",
			"8.0",
			"9.0"
		};

		bool createFolders = false;

		static UserProfile ()
		{
			// The 9.x current profile is unversioned on Unix (no "9.0" subfolder). One-time
			// migration of the previously-versioned profile into it, idempotent, before the
			// current profile is resolved (so every consumer sees the merged data and
			// PropertyService never migrates a stale older version over it). Must not depend
			// on BrandingService/LoggingService: either could trigger this same static ctor
			// from an earlier failure path, and a re-entrant call would re-enter here.
			MigrateUnixCurrentProfile ();
			Current = GetProfile (ProfileVersions[ProfileVersions.Length-1]);
		}
		
		public static UserProfile Current { get; private set; }

		FilePath cacheDir;
		/// <summary>Location for cached data that can be regenerated.</summary>
		public FilePath CacheDir {
			get => cacheDir;
			private set {
				cacheDir = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}

		FilePath configDir;
		/// <summary>Location for current preferences/settings.</summary>
		public FilePath ConfigDir {
			get => configDir;
			private set {
				configDir = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}

		FilePath localConfigDir;
		/// <summary>Preferences/settings specific to the local machine.</summary>
		public FilePath LocalConfigDir {
			get => localConfigDir;
			private set {
				localConfigDir = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}

		FilePath userDataRoot;
		/// <summary>User-visible root location for user-created data files such as templates, snippets and color schemes.</summary>
		public FilePath UserDataRoot {
			get => userDataRoot;
			private set {
				userDataRoot = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}

		FilePath logDir;
		/// <summary>Location for log files.</summary>
		public FilePath LogDir {
			get => logDir;
			private set {
				logDir = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}

		FilePath localInstallPath;
		/// <summary>Location for files installed from external sources.</summary>
		public FilePath LocalInstallDir {
			get => localInstallPath;
			private set {
				localInstallPath = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}

		FilePath tempDir;
		//TODO: clear out temp files at startup
		/// <summary>Location for temporary files.</summary>
		public FilePath TempDir {
			get => tempDir;
			private set {
				tempDir = value;
				EnsureDirectoryExistsIfNeeded (value);
			}
		}
		
		/// <summary>Gets a location by its ID.</summary>
		internal FilePath GetLocation (UserDataKind kind)
		{
			switch (kind) {
			case UserDataKind.LocalInstall:
				return LocalInstallDir;
			case UserDataKind.Cache:
				return CacheDir;
			case UserDataKind.UserData:
				return UserDataRoot;
			case UserDataKind.Logs:
				return LogDir;
			case UserDataKind.Config:
				return ConfigDir;
			case UserDataKind.LocalConfig:
				return LocalConfigDir;
			case UserDataKind.Temp:
				return TempDir;
			default:
				throw new ArgumentException ("Unknown UserDataLocation:" + kind.ToString ());
			}
		}
		
		internal static UserProfile GetProfile (string profileVersion, bool ensureCreated = true)
		{
			var brandedEnvVar = BrandingService.BrandEnvironmentVariable (PROFILE_ENV_VAR);

			FilePath testProfileRoot = Environment.GetEnvironmentVariable (brandedEnvVar);
			if (!testProfileRoot.IsNullOrEmpty)
				return UserProfile.ForTest (profileVersion, testProfileRoot, ensureCreated);
			
			if (Platform.IsWindows)
				return UserProfile.ForWindows (profileVersion, ensureCreated);
			else if (Platform.IsMac)
				return UserProfile.ForMac (profileVersion, ensureCreated);
			else
				return UserProfile.ForUnix (profileVersion, ensureCreated);
		}

		static string GetAppId (string version)
		{
			if (Version.Parse (version).Major < 7) {
				return BrandingService.ProfileDirectoryName + "-" + version; ;
			}
			return System.IO.Path.Combine (BrandingService.ProfileDirectoryName, version);
		}
		
		/// <summary>
		/// Creates locations in a specific folder, for testing.
		/// </summary>
		internal static UserProfile ForTest (string version, FilePath profileLocation, bool ensureCreated = true)
		{
			string appId = GetAppId (version);
			return new UserProfile () {
				createFolders = ensureCreated,
				CacheDir = profileLocation.Combine (appId, "Cache"),
				UserDataRoot = profileLocation.Combine (appId, "UserData"),
				ConfigDir = profileLocation.Combine (appId, "Config"),
				LocalConfigDir = profileLocation.Combine (appId, "LocalConfig"),
				LogDir = profileLocation.Combine (appId, "Logs"),
				LocalInstallDir = profileLocation.Combine (appId, "LocalInstall"),
				TempDir = profileLocation.Combine (appId, "Temp"),
			};
		}

		internal static UserProfile ForWindows (string version, bool ensureCreated = true)
		{
			string appId = GetAppId (version);
			FilePath local = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			FilePath roaming = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			//FilePath localLow = GetKnownFolderPath (new Guid ("A520A1A4-1780-4FF6-BD18-167343C5AF16"));
			
			local = local.Combine (appId);
			roaming = roaming.Combine (appId);

			return new UserProfile () {
				createFolders = ensureCreated,
				UserDataRoot = roaming,
				ConfigDir = roaming.Combine ("Config"),
				LocalConfigDir = local.Combine ("Config"),
				LocalInstallDir = local.Combine ("LocalInstall"),
				LogDir = local.Combine ("Logs"),
				CacheDir = local.Combine ("Cache"),
				TempDir = local.Combine ("Temp"),
			};
		}
		
		internal static UserProfile ForMac (string version, bool ensureCreated = true)
		{
			string appId = GetAppId (version);
			FilePath home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			FilePath library = home.Combine ("Library");
			
			FilePath data = library.Combine (appId);
			FilePath preferences = library.Combine ("Preferences", appId);
			FilePath cache = library.Combine ("Caches", appId);
			FilePath logs = library.Combine ("Logs", appId);
			FilePath appSupport = library.Combine ("Application Support", appId);
			
			return new UserProfile () {
				createFolders = ensureCreated,
				CacheDir = cache,
				UserDataRoot = data,
				ConfigDir = preferences,
				LocalConfigDir = preferences,
				LogDir = logs,
				LocalInstallDir = appSupport.Combine ("LocalInstall"),
				TempDir = cache.Combine ("Temp"),
			};
		}
		
		internal static UserProfile ForUnix (string version, bool ensureCreated = true)
		{
			FilePath home = GetUnixHome ();
			FilePath xdgDataHome = Environment.GetEnvironmentVariable ("XDG_DATA_HOME");
			if (xdgDataHome.IsNullOrEmpty)
				xdgDataHome = home.Combine (".local", "share");
			FilePath xdgConfigHome = Environment.GetEnvironmentVariable ("XDG_CONFIG_HOME");
			if (xdgConfigHome.IsNullOrEmpty)
				xdgConfigHome = home.Combine (".config");
			FilePath xdgCacheHome = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
			if (xdgCacheHome.IsNullOrEmpty)
				xdgCacheHome = home.Combine (".cache");
			
			// The current profile (the last known version) is unversioned on Unix: it lives
			// in <xdg>/MonoDevelop directly, not <xdg>/MonoDevelop/<version>. Older versions
			// (used only as migration sources by GetMigratableProfile) keep their versioned
			// subfolder so version-migration still finds them.
			string appId = version == ProfileVersions [ProfileVersions.Length - 1]
				? BrandingService.ProfileDirectoryName
				: GetAppId (version);
			FilePath data = xdgDataHome.Combine (appId);
			FilePath config = xdgConfigHome.Combine (appId);
			FilePath cache = xdgCacheHome.Combine (appId);
			
			return new UserProfile () {
				createFolders = ensureCreated,
				UserDataRoot = data,
				LocalInstallDir = data.Combine ("LocalInstall"),
				ConfigDir = config,
				LocalConfigDir = config,
				CacheDir = cache,
				TempDir = cache.Combine ("Temp"),
				LogDir = cache.Combine ("Logs"),
			};
		}

		// One-time move of the last versioned current profile out of its "9.0" subfolder into
		// the now-unversioned profile, across all three XDG roots (<xdg-config|cache|data>/
		// MonoDevelop/9.0 -> <...>/MonoDevelop). Runs from the static ctor on Unix only — the
		// only platform whose layout changed; Windows/Mac keep their versioned layout and need
		// no migration. Idempotent: once a root's 9.0 subfolder is empty or gone there is
		// nothing to do. The current profile is resolved with ensureCreated=false so resolving
		// it does not (re)create the old 9.0 subfolders. When a file exists in both the versioned
		// source and the unversioned target, the newer copy wins (a re-run after the app already
		// wrote a newer file must not resurrect the stale one). A root's versioned subfolder is
		// removed only after every source file is verified present in the target, so an
		// interruption can never lose data.
		static void MigrateUnixCurrentProfile ()
		{
			if (!Platform.IsLinux)
				return;

			// Best effort: a failure must never prevent the IDE from starting, so any error is
			// reported and swallowed (the versioned profile is left in place; data is not lost).
			try {
				string currentVersion = ProfileVersions [ProfileVersions.Length - 1];
				var currentProfile = GetProfile (currentVersion, false);

				// LocalConfigDir == ConfigDir, TempDir/LogDir are under CacheDir and
				// LocalInstallDir under UserDataRoot on Unix, so migrating these three primary
				// roots covers every profile location.
				foreach (string root in new[] {
					currentProfile.ConfigDir.ToString (),
					currentProfile.CacheDir.ToString (),
					currentProfile.UserDataRoot.ToString (),
				}) {
					MigrateVersionedSubdir (root, currentVersion);
				}
			} catch (Exception ex) {
				Console.Error.WriteLine ("[profile-migration] failed to move the " +
					ProfileVersions [ProfileVersions.Length - 1] + " profile into the unversioned location: " + ex.Message);
			}
		}

		static void MigrateVersionedSubdir (string root, string version)
		{
			string versioned = Path.Combine (root, version);
			if (!Directory.Exists (versioned))
				return;

			// Capture the full set of source files (relative paths) before moving, so each can
			// be verified to have landed in the target afterwards.
			var sourceFiles = Directory.EnumerateFiles (versioned, "*", SearchOption.AllDirectories)
				.Select (f => Path.GetRelativePath (versioned, f))
				.ToList ();

			MergeDirectory (versioned, root);

			// Remove the versioned tree only once every source file exists in the target (moved
			// into it, or already present there as the authoritative copy). If any is missing a
			// move failed; leave the versioned tree so no data is lost.
			if (sourceFiles.All (rel => File.Exists (Path.Combine (root, rel))))
				Directory.Delete (versioned, true);
		}

		static void MergeDirectory (string source, string target)
		{
			Directory.CreateDirectory (target);
			foreach (string file in Directory.EnumerateFiles (source)) {
				string dest = Path.Combine (target, Path.GetFileName (file));
				// Newest wins: move the source into the target, overwriting the target only
				// when the source copy is strictly newer. A file already present and at least
				// as new in the target is the authoritative copy, so the (stale) source is
				// simply dropped. This keeps the most recent data without ever losing it.
				if (!File.Exists (dest)
					|| File.GetLastWriteTimeUtc (file) > File.GetLastWriteTimeUtc (dest)) {
					File.Move (file, dest, overwrite: true);
				} else {
					File.Delete (file);
				}
			}
			foreach (string sub in Directory.EnumerateDirectories (source)) {
				MergeDirectory (sub, Path.Combine (target, Path.GetFileName (sub)));
			}
		}

		// On .NET Framework/Mono, SpecialFolder.Personal returned $HOME, but on .NET Core/5+ it maps
		// to the XDG documents dir (e.g. ~/Documents), which made the whole profile (.config/.cache/
		// .local) land inside the user's documents folder. SpecialFolder.UserProfile is the folder
		// that actually means $HOME on .NET Core; fall back to the HOME variable for exotic setups.
		static FilePath GetUnixHome ()
		{
			FilePath home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			if (home.IsNullOrEmpty)
				home = Environment.GetEnvironmentVariable ("HOME");
			if (home.IsNullOrEmpty)
				home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			return home;
		}

		internal static UserProfile ForMD24 ()
		{
			FilePath appdata = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			var mdConfig = appdata.Combine ("MonoDevelop");
			return new UserProfile () {
				UserDataRoot = mdConfig,
				ConfigDir = mdConfig,
				LocalConfigDir = mdConfig,
				LocalInstallDir = mdConfig.Combine ("addins"),
				LogDir = mdConfig,
				CacheDir = mdConfig,
				//temp is not migratable
			};
		}
		/*
		static string GetKnownFolderPath (Guid knownFolderId)
		{
			var pathHandle = IntPtr.Zero;
			try {
				int hresult = SHGetKnownFolderPath (knownFolderId, 0, IntPtr.Zero, out pathHandle);
				if (hresult >= 0)
					return Marshal.PtrToStringAuto (pathHandle);
				throw Marshal.GetExceptionForHR (hresult);
			} finally {
				if (pathHandle != IntPtr.Zero)
					Marshal.FreeCoTaskMem (pathHandle);
			}
		}
		
		[DllImport ("shell32.dll")]
		static extern int SHGetKnownFolderPath ([MarshalAs (UnmanagedType.LPStruct)] Guid rfid,
			uint dwFlags, IntPtr hToken, out IntPtr pszPath);
		*/

		void EnsureDirectoryExistsIfNeeded (string dir) 
		{
			if(createFolders)
				Directory.CreateDirectory (dir);
		}
	}
	
	enum UserDataKind
	{
		Cache,
		Config,
		LocalConfig,
		UserData,
		Logs,
		LocalInstall,
		Temp,
	}
}
