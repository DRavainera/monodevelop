// 
// DotNetTargetRuntime.cs
//  
// Represents the .NET runtime (6.0+/8.0) MonoDevelop is currently running on.
// Replaces the Windows-only, .NET Framework-centric MsNetTargetRuntime for the
// post-Mono migration: MonoDevelop now runs on .NET via the shared runtime instead
// of invoking the `mono` binary, so the running runtime must be discovered and
// registered on every platform.
// 
// Copyright (c) 2026 Daniel Ravainera
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Core.Assemblies
{
	/// <summary>
	/// The .NET runtime (net6.0+) that MonoDevelop is running on. On .NET the previously
	/// discovered Mono runtimes do not exist; the running process itself is the runtime,
	/// so a single instance is always produced with <see cref="IsRunning"/> == true.
	/// </summary>
	class DotNetTargetRuntime: TargetRuntime
	{
		readonly string sdkMSBuildPath;

		public DotNetTargetRuntime ()
		{
			sdkMSBuildPath = ResolveSdkMSBuildPath ();
		}

		public override string DisplayRuntimeName {
			get {
				return "Microsoft .NET (Core)";
			}
		}

		public override string RuntimeId {
			get {
				return ".NET";
			}
		}

		public override string Version {
			get {
				return Environment.Version.ToString ();
			}
		}

		public override bool IsRunning {
			get {
				return true;
			}
		}

		public override string GetAssemblyDebugInfoFile (string assemblyPath)
		{
			return Path.ChangeExtension (assemblyPath, ".pdb");
		}

		public override IExecutionHandler GetExecutionHandler ()
		{
			return new NativePlatformExecutionHandler ();
		}

		protected override TargetFrameworkBackend CreateBackend (TargetFramework fx)
		{
			// .NET (Core) target framework handling is still pending; fall back to the
			// NotSupportedFrameworkBackend for now so the running runtime can be registered
			// without pretending to install frameworks it does not provide.
			return null;
		}

		public override string GetMSBuildBinPath (string toolsVersion)
		{
			if (toolsVersion != "Current") {
				var path = GetMSBuildBinPath ("Current");
				if (path != null)
					return path;
			}
			return sdkMSBuildPath;
		}

		public override string GetMSBuildToolsPath (string toolsVersion)
		{
			return GetMSBuildBinPath (toolsVersion);
		}

		public override string GetMSBuildExtensionsPath ()
		{
			var dotnetRoot = RuntimeEnvironmentUtil.GetDotnetRoot ();
			if (dotnetRoot == null)
				return null;
			return Path.Combine (dotnetRoot, "sdk");
		}

		internal protected override IEnumerable<string> GetGacDirectories ()
		{
			// .NET (Core) has no GAC.
			yield break;
		}

		protected override void OnInitialize ()
		{
		}

		internal string GetSdkMSBuildBinPath ()
		{
			return sdkMSBuildPath;
		}

		static string ResolveSdkMSBuildPath ()
		{
			// Locate the .NET SDK that contains MSBuild.dll.
			var dotnetRoot = RuntimeEnvironmentUtil.GetDotnetRoot ();
			if (dotnetRoot != null) {
				string sdkDir = Path.Combine (dotnetRoot, "sdk");
				if (Directory.Exists (sdkDir)) {
					foreach (string versionDir in Directory.EnumerateDirectories (sdkDir).OrderByDescending (Path.GetFileName)) {
						if (File.Exists (Path.Combine (versionDir, "MSBuild.dll")))
							return versionDir;
					}
				}
			}

			// Fall back to probing the version reported by the `dotnet` SDK.
			string sdkVer;
			try {
				sdkVer = RunDotnet ("--version").Trim ();
			} catch {
				sdkVer = null;
			}
			if (!string.IsNullOrEmpty (sdkVer)) {
				var dir = Path.Combine (RuntimeEnvironmentUtil.GetDotnetRoot () ?? ".", "sdk", sdkVer);
				if (Directory.Exists (dir) && File.Exists (Path.Combine (dir, "MSBuild.dll")))
					return dir;
			}
			return null;
		}

		static string RunDotnet (string arguments)
		{
			var psi = new System.Diagnostics.ProcessStartInfo ("dotnet", arguments) {
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			using (var p = System.Diagnostics.Process.Start (psi)) {
				string output = p.StandardOutput.ReadToEnd ();
				p.WaitForExit ();
				return output;
			}
		}
	}

	static class RuntimeEnvironmentUtil
	{
		public static string GetDotnetRoot ()
		{
			var dotnetRoot = Environment.GetEnvironmentVariable ("DOTNET_ROOT");
			if (!string.IsNullOrEmpty (dotnetRoot))
				return dotnetRoot;
			// Resolve from the running coreclr location, e.g.
			// <dir>/shared/Microsoft.NETCore.App/<version>/System.Private.CoreLib.dll
			var coreDir = System.IO.Path.GetDirectoryName (typeof(object).Assembly.Location);
			if (coreDir != null)
				return System.IO.Path.GetDirectoryName (System.IO.Path.GetDirectoryName (System.IO.Path.GetDirectoryName (coreDir)));
			return null;
		}
	}
}