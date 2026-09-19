// Licensed to the .NET Foundation under one or more agreements.
// (Port of Microsoft.Build's NuGetFrameworkWrapper semantics for the in-proc evaluator.)
//
// The modern .NET SDK imports rely on $([MSBuild]::GetTargetFrameworkIdentifier(...)) and
// $([MSBuild]::GetTargetFrameworkVersion(...)) to derive TargetFrameworkIdentifier /
// TargetFrameworkVersion from the TargetFramework property. This backend resolves those
// using the real NuGet.Frameworks parser when it can be located (it ships next to
// MSBuild.dll in every .NET SDK root, so the in-proc evaluator and the remote builder
// both find the matching copy), and falls back to a portable table that reproduces the
// identifiers verified against the real MSBuild implementation.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoDevelop.Core;

namespace Microsoft.Build.Evaluation
{
	internal static class TargetFrameworkIntrinsics
	{
		// Fallback table: TFM short-identifier prefix -> framework identifier.
		// Order matters: 'net' (with no version part separator) must be tested last.
		static readonly KeyValuePair<string, string> [] fallbackIdentifiers = new KeyValuePair<string, string> [] {
			new KeyValuePair<string, string> ("netstandard", ".NETStandard"),
			new KeyValuePair<string, string> ("netcoreapp", ".NETCoreApp"),
			new KeyValuePair<string, string> ("windows", "Windows"),
			new KeyValuePair<string, string> ("uap", "UAP"),
			new KeyValuePair<string, string> ("tizen", "Tizen"),
			new KeyValuePair<string, string> ("monoandroid", "MonoAndroid"),
			new KeyValuePair<string, string> ("xamarinios", "Xamarin.iOS"),
			new KeyValuePair<string, string> ("xamarinmac", "Xamarin.Mac"),
			new KeyValuePair<string, string> ("xamarintvos", "Xamarin.TVOS"),
			new KeyValuePair<string, string> ("xamarinwatchos", "Xamarin.WatchOS"),
			new KeyValuePair<string, string> ("maccatalyst", "Microsoft.MacCatalyst"),
			new KeyValuePair<string, string> ("macos", "Microsoft.macOS"),
			new KeyValuePair<string, string> ("ios", "Microsoft.iOS"),
			new KeyValuePair<string, string> ("tvos", "Microsoft.tvOS"),
			new KeyValuePair<string, string> ("watchos", "Microsoft.watchOS"),
			new KeyValuePair<string, string> ("android", "Microsoft.Android"),
			new KeyValuePair<string, string> ("net", ".NETFramework"),
		};

		static readonly object loadLock = new object ();
		static bool loadAttempted;
		static bool nugetAvailable;

		// Delegates into NuGet.Frameworks, resolved once by reflection.
		static MethodInfo nugetParse;
		static MethodInfo nugetGetFramework;
		static MethodInfo nugetGetVersion;
		static MethodInfo nugetGetPlatform;
		static MethodInfo nugetGetPlatformVersion;

		/// <summary>
		/// MSBuild intrinsic: $([MSBuild]::GetTargetFrameworkIdentifier('net8.0')) => '.NETCoreApp'.
		/// </summary>
		internal static string GetTargetFrameworkIdentifier (string tfm)
		{
			if (string.IsNullOrEmpty (tfm))
				return "Unsupported";

			if (TryLoadNuGetFrameworks ()) {
				var fw = NuGetParse (tfm);
				if (fw != null)
					return (string)nugetGetFramework.Invoke (null, new object [] { fw });
			}

			return GetFallbackIdentifier (tfm);
		}

		/// <summary>
		/// MSBuild intrinsic: $([MSBuild]::GetTargetFrameworkVersion('net8.0')) => '8.0'.
		/// <paramref name="versionPartCount"/> is the minimum number of version parts in the result.
		/// </summary>
		internal static string GetTargetFrameworkVersion (string tfm, int versionPartCount = 2)
		{
			if (TryLoadNuGetFrameworks ()) {
				var fw = NuGetParse (tfm);
				if (fw != null) {
					var version = (Version)nugetGetVersion.Invoke (null, new object [] { fw });
					return GetNonZeroVersionParts (version, versionPartCount);
				}
			}

			return GetNonZeroVersionParts (GetFallbackVersion (GetFallbackVersionString (tfm)), versionPartCount);
		}

		/// <summary>
		/// MSBuild intrinsic: $([MSBuild]::GetTargetPlatformIdentifier('net8.0-windows')) => 'windows'.
		/// </summary>
		internal static string GetTargetPlatformIdentifier (string tfm)
		{
			if (string.IsNullOrEmpty (tfm))
				return "Unsupported";

			if (TryLoadNuGetFrameworks ()) {
				var fw = NuGetParse (tfm);
				if (fw != null)
					return (string)nugetGetPlatform.Invoke (null, new object [] { fw });
			}

			int dash = tfm.IndexOf ('-');
			return dash < 0 ? "" : GetFallbackIdentifier (tfm.Substring (dash + 1)).ToLowerInvariant ();
		}

		/// <summary>
		/// MSBuild intrinsic: $([MSBuild]::GetTargetPlatformVersion('net8.0-windows10.0.19041')) => '10.0.19041'.
		/// </summary>
		internal static string GetTargetPlatformVersion (string tfm, int versionPartCount = 2)
		{
			if (TryLoadNuGetFrameworks ()) {
				var fw = NuGetParse (tfm);
				if (fw != null) {
					var version = (Version)nugetGetPlatformVersion.Invoke (null, new object [] { fw });
					return GetNonZeroVersionParts (version, versionPartCount);
				}
			}

			int dash = tfm.IndexOf ('-');
			if (dash < 0)
				return versionPartCount > 0 ? "0.0" : "0";
			return GetNonZeroVersionParts (GetFallbackVersion (tfm.Substring (dash + 1)), versionPartCount);
		}

		// Port of NuGetFrameworkWrapper.GetNonZeroVersionParts (MSBuild):
		// at least minVersionPartCount parts, trailing zero parts trimmed beyond that.
		static string GetNonZeroVersionParts (Version version, int minVersionPartCount)
		{
			var nonZeroVersionParts =
				version.Revision == 0 ? version.Build == 0 ? version.Minor == 0 ? 1 : 2 : 3 : 4;
			return version.ToString (Math.Max (nonZeroVersionParts, minVersionPartCount));
		}

		static bool TryLoadNuGetFrameworks ()
		{
			if (loadAttempted)
				return nugetAvailable;

			lock (loadLock) {
				if (loadAttempted)
					return nugetAvailable;

				loadAttempted = true;
				try {
					// An already-loaded NuGet.Frameworks (e.g. the copy PackageManagement ships
					// with) is preferred: binding a second copy with a different assembly
					// version would fail anyway, and any of them parses modern TFMs fine.
					var loaded = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (
						a => string.Equals (a.GetName ().Name, "NuGet.Frameworks", StringComparison.OrdinalIgnoreCase));
					if (loaded != null && TryUseParser (loaded))
						return true;

					foreach (var candidate in GetCandidateAssemblies ()) {
						try {
							// Candidates that do not exist are simply skipped; only a load
							// failure of an existing assembly is worth reporting.
							if (!File.Exists (candidate))
								continue;

							var asm = Assembly.LoadFrom (candidate);
							if (TryUseParser (asm))
								return true;
						} catch (Exception ex) {
							LoggingService.LogInternalError ("Could not load NuGet.Frameworks from " + candidate, ex);
						}
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Error looking for NuGet.Frameworks assemblies", ex);
				}
				return nugetAvailable;
			}
		}

		static bool TryUseParser (Assembly asm)
		{
			var type = asm.GetType ("NuGet.Frameworks.NuGetFramework", throwOnError: false);
			if (type == null)
				return false;

			var parse = type.GetMethod ("Parse", new Type [] { typeof (string) });
			var framework = type.GetProperty ("Framework")?.GetGetMethod ();
			var version = type.GetProperty ("Version")?.GetGetMethod ();
			var platform = type.GetProperty ("Platform")?.GetGetMethod ();
			var platformVersion = type.GetProperty ("PlatformVersion")?.GetGetMethod ();

			if (parse == null || framework == null || version == null || platform == null || platformVersion == null)
				return false;

			// Probe once so broken copies are detected here instead of at evaluation time.
			var probe = parse.Invoke (null, new object [] { "net8.0" });
			if (probe == null)
				return false;

			nugetParse = parse;
			nugetGetFramework = framework;
			nugetGetVersion = version;
			nugetGetPlatform = platform;
			nugetGetPlatformVersion = platformVersion;
			nugetAvailable = true;
			return true;
		}

		static IEnumerable<string> GetCandidateAssemblies ()
		{
			// The parser that matches the SDK being used is the one next to MSBuild.dll.
			var msbuildExePath = Environment.GetEnvironmentVariable ("MSBUILD_EXE_PATH");
			if (!string.IsNullOrEmpty (msbuildExePath)) {
				var dir = Path.GetDirectoryName (msbuildExePath);
				if (!string.IsNullOrEmpty (dir))
					yield return Path.Combine (dir, "NuGet.Frameworks.dll");
			}

			var sdksPath = Environment.GetEnvironmentVariable ("MSBuildSDKsPath");
			if (!string.IsNullOrEmpty (sdksPath)) {
				// MSBuildSDKsPath points at <sdk-root>/Sdks
				var sdkRoot = Path.GetDirectoryName (sdksPath);
				if (!string.IsNullOrEmpty (sdkRoot))
					yield return Path.Combine (sdkRoot, "NuGet.Frameworks.dll");
			}

			// The runtime directory of the IDE / builder (net10run copy).
			yield return Path.Combine (AppContext.BaseDirectory, "NuGet.Frameworks.dll");
		}

		static object NuGetParse (string tfm)
		{
			try {
				return nugetParse.Invoke (null, new object [] { tfm });
			} catch (Exception ex) {
				LoggingService.LogInternalError ("NuGet.Frameworks failed to parse '" + tfm + "'", ex);
				return null;
			}
		}

		// Fallback implementations (used when NuGet.Frameworks cannot be located).

		static string GetFallbackIdentifier (string tfm)
		{
			var trimmed = tfm.Trim ();
			foreach (var kvp in fallbackIdentifiers) {
				if (trimmed.Length >= kvp.Key.Length &&
					trimmed.StartsWith (kvp.Key, StringComparison.OrdinalIgnoreCase)) {
					// The bare 'net' prefix is ambiguous: classic short names (net4x, net11)
					// are .NETFramework while modern short names (net5.0+) are .NETCoreApp.
					// NuGet (and the real MSBuild) disambiguate by the first version digit.
					if (kvp.Key == "net") {
						var rest = trimmed.Substring (3);
						return rest.Length > 0 && rest [0] >= '5' && rest [0] <= '9' ? ".NETCoreApp" : ".NETFramework";
					}
					return kvp.Value;
				}
			}
			return "Unsupported";
		}

		static string GetFallbackVersionString (string tfm)
		{
			// Strip any platform suffix (net8.0-windows10.0.19041 => 8.0), then strip the
			// short-name prefix. Identifier prefixes end either at the first digit or are
			// alphabetic aliases followed by a digit.
			int dash = tfm.IndexOf ('-');
			var versionPart = dash >= 0 ? tfm.Substring (0, dash) : tfm;

			int start = 0;
			while (start < versionPart.Length && !char.IsDigit (versionPart [start]))
				start++;
			return start == 0 ? versionPart : versionPart.Substring (start);
		}

		static Version GetFallbackVersion (string versionString)
		{
			if (string.IsNullOrEmpty (versionString))
				return new Version (0, 0, 0, 0);

			var parts = versionString.Split ('.');
			var numbers = new int [Math.Min (4, parts.Length)];
			for (int i = 0; i < numbers.Length; i++) {
				int.TryParse (parts [i], out numbers [i]);
			}

			try {
				return numbers.Length switch {
					1 => new Version (numbers [0], 0, 0, 0),
					2 => new Version (numbers [0], numbers [1], 0, 0),
					3 => new Version (numbers [0], numbers [1], numbers [2], 0),
					_ => new Version (numbers [0], numbers [1], numbers [2], numbers [3]),
				};
			} catch {
				return new Version (0, 0, 0, 0);
			}
		}
	}
}
