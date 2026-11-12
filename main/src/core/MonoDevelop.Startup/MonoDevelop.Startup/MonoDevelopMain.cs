//
// IdeStartup.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2014 Xamarin Inc (http://xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using MonoDevelop.Ide;

namespace MonoDevelop.Startup
{
	public class MonoDevelopMain
	{
		static readonly string[] MonoAssemblyDirs = {
			Environment.GetEnvironmentVariable ("MD_MONO_LIB_DIR") ?? "/usr/lib/mono/gtk-sharp-2.0",
			"/usr/lib/mono/4.5",
			AppContext.BaseDirectory
		};

		static readonly string[] NativeLibraryDirs = {
			"/usr/lib64",
			"/usr/lib/x86_64-linux-gnu",
			"/lib/x86_64-linux-gnu",
			"/usr/lib"
		};

		static string GetNativeBaseName (string dllName)
		{
			if (dllName.EndsWith ("-0.dll", StringComparison.Ordinal))
				return dllName.Substring (0, dllName.Length - "-0.dll".Length);
			if (dllName.EndsWith (".dll", StringComparison.OrdinalIgnoreCase))
				return dllName.Substring (0, dllName.Length - ".dll".Length);
			return dllName;
		}

		static IntPtr ResolveNativeLibrary (Assembly assembly, string dllName)
		{
			string baseName = GetNativeBaseName (dllName);
			if (dllName == "libgtk-win32-2.0-0.dll")
				baseName = "libgtk-3";
			else if (dllName == "libgdk-win32-2.0-0.dll")
				baseName = "libgdk-3";
			else if (dllName.StartsWith ("lib", StringComparison.Ordinal)) {
				int dash = baseName.LastIndexOf ('-');
				if (dash > 3) {
					string stem = baseName.Substring (0, dash);
					string version = baseName.Substring (dash + 1);
					bool numeric = true;
					foreach (char c in version)
						numeric &= Char.IsDigit (c) || c == '.';
					if (numeric && stem.Length > 3) {
						string indexed = stem + ".so." + version;
						if (File.Exists (Path.Combine (NativeLibraryDirs [0], indexed)))
							return NativeLibrary.Load (Path.Combine (NativeLibraryDirs [0], indexed));
					}
				}
			}
			// gettext lives inside glibc (libc) on modern Linux; a standalone
			// libintl is not installed by default. Mono.Unix.Catalog and
			// Mono.Addins.Localization.GettextDomain P/Invoke "intl" for it.
			if (dllName == "intl" && RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
				try {
					return NativeLibrary.Load ("libc");
				} catch (Exception) {
					// fall through to the variant search below
				}
			}

			string[] variants = { baseName, baseName + ".so", baseName + ".so.0" };
			if (baseName.StartsWith ("lib", StringComparison.Ordinal)) {
				string underscore = "lib" + baseName.Substring (3).Replace ('-', '_');
				string[] extraVariants = { underscore + ".so", underscore + ".so.0" };
				string[] all = new string [variants.Length + extraVariants.Length];
				variants.CopyTo (all, 0);
				extraVariants.CopyTo (all, variants.Length);
				variants = all;
			}
			foreach (string dir in NativeLibraryDirs) {
				foreach (string variant in variants) {
					string path = Path.Combine (dir, variant);
					if (File.Exists (path))
						return NativeLibrary.Load (path);
				}
			}
			return IntPtr.Zero;
		}

		// A per-process cache of the addin install dirs (main/build/AddIns/<add-in>/),
		// where each add-in's own closure (its NuGet.*.dll, MonoDevelop.*.dll, ...) is
		// staged. The addin loader (Mono.Addins) only LoadFrom's the assemblies a manifest
		// lists under <Import>; cross-references *between* the DLLs of the same add-in go
		// through this resolver, which previously searched only the root build dir and never
		// the add-in dirs.
		// Field-initialized (not ??= in the resolver callback) so the directory scan runs once,
		// thread-safely, no matter which thread first hits the Resolving event.
		static readonly Lazy<string[]> AddinInstallDirs = new Lazy<string[]> (ComputeAddinInstallDirs);

		static string[] ComputeAddinInstallDirs ()
		{
			string addInsRoot = Path.Combine (AppContext.BaseDirectory, "AddIns");
			if (!Directory.Exists (addInsRoot))
				return Array.Empty<string>();
			try {
				return Directory.EnumerateDirectories (addInsRoot).ToArray();
			} catch (Exception) {
				return Array.Empty<string>();
			}
		}

		// Whether an assembly with this simple name is already loaded. Loading a second file for
		// a name the context already holds throws "Assembly with same name is already loaded"
		// (the NuGet.Common 7.9.0-at-root vs add-in-closure collision that permanently failed the
		// PackageManagementServices static ctor). The GTK process loads every add-in into the
		// Default ALC (Mono.Addins uses Assembly.LoadFrom), so scanning the whole AppDomain by
		// simple name is exact here: returning null for an already-loaded name makes the caller
		// see the original resolution failure instead of a confusing "already loaded" throw.
		static bool IsAssemblyLoaded (string simpleName)
		{
			try {
				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies ()) {
					try {
						if (a.GetName ().Name == simpleName)
							return true;
					} catch (Exception) {
						// dynamic or identity-less assembly: cannot match by name, skip
					}
				}
			} catch (Exception) {
				return false;
			}
			return false;
		}

		// True when the file at `path` can satisfy the request `name`: its declared version is
		// compatible (major.minor.build equal, revision tolerated) and the public key token,
		// when both are known, matches. The identity is read with AssemblyName.GetAssemblyName,
		// which parses the file without executing any of its code, so a stray copy (e.g. a NuGet
		// version pinned at the root) can be rejected before it is loaded. When the declared
		// identity cannot be read, the file is only allowed if the request carries no version.
		static bool CanSatisfy (AssemblyName name, string path)
		{
			AssemblyName fileAsm;
			try {
				fileAsm = AssemblyName.GetAssemblyName (path);
			} catch (Exception) {
				return name.Version == null;
			}
			if (name.Version == null)
				return true;
			Version declared = fileAsm.Version;
			if (declared == null)
				return false;
			if (declared.Major != name.Version.Major || declared.Minor != name.Version.Minor || declared.Build != name.Version.Build)
				return false;
			byte[] fileToken = fileAsm.GetPublicKeyToken ();
			byte[] reqToken = name.GetPublicKeyToken ();
			if (fileToken != null && reqToken != null && !fileToken.SequenceEqual (reqToken))
				return false;
			return true;
		}

		[STAThread]
		public static int Main (string[] args)
		{
			// The GTK UI is hidden legacy compatibility during the 9.x branch: it must
			// only start through the Avalonia shell entrypoint, whose --old-gui relay
			// sets MONODEVELOP_LEGACY_UI=1 for this child process. A direct launch of
			// MonoDevelop.dll is refused instead of showing the old UI by surprise;
			// no command line flag gates this (only the relay enables it).
			if (Environment.GetEnvironmentVariable ("MONODEVELOP_LEGACY_UI") != "1") {
				Console.Error.WriteLine ("The legacy GTK UI is hidden. Start MonoDevelop with MonoDevelop.AvaloniaShell.dll (add --old-gui for the legacy GTK UI).");
				return 2;
			}
			var ctx = AssemblyLoadContext.Default;
			ctx.Resolving += (_, name) => {
				if (name.Name == "System.ComponentModel.Composition") {
					// Load the .NET runtime facade (which type-forwards System.Lazy<T,TMetadata>
					// to System.Private.CoreLib) instead of the Mono GAC net472 copy; the Mono copy
					// defines its own Lazy<T,TMetadata> type, which breaks vs-mef imports that
					// resolve System.Lazy<T,TMetadata> against it.
					string facade = Path.Combine (AppContext.BaseDirectory, name.Name + ".dll");
					if (File.Exists (facade))
						return ctx.LoadFromAssemblyPath (facade);
					return null;
				}

				// Never load a second file for a name that is already loaded: doing so throws
				// "Assembly with same name is already loaded" (the NuGet.Common 7.9.0-at-root vs
				// add-in-closure collision that permanently failed the PackageManagementServices
				// static ctor). Returning null lets the caller see the original resolution
				// failure instead of a confusing load failure.
				if (IsAssemblyLoaded (name.Name))
					return null;

				// Search the add-in install dirs first so an add-in's own closure wins over a
				// same-named assembly staged at the root build dir; the version guard below then
				// keeps a stray copy (e.g. a NuGet version the Avalonia shell pins at the root)
				// from answering a request it cannot satisfy.
				foreach (string dir in AddinInstallDirs.Value) {
					string path = Path.Combine (dir, name.Name + ".dll");
					if (File.Exists (path) && CanSatisfy (name, path))
						return ctx.LoadFromAssemblyPath (path);
				}
				foreach (string dir in MonoAssemblyDirs) {
					string path = Path.Combine (dir, name.Name + ".dll");
					if (File.Exists (path) && CanSatisfy (name, path))
						return ctx.LoadFromAssemblyPath (path);
				}
				return null;
			};
			ctx.ResolvingUnmanagedDll += ResolveNativeLibrary;
			// Must run before any GLib.Object wrapper exists (before IdeStartup/Gtk.Init)
			MonoDevelop.Components.GtkNativeLifetimeGuard.Install ();
			return IdeStartup.Main (args);
		}
	}
}