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
using System.IO;
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
				baseName = "libgtk-x11-2.0";
			else if (dllName == "libgdk-win32-2.0-0.dll")
				baseName = "libgdk-x11-2.0";
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

		[STAThread]
		public static int Main (string[] args)
		{
			var ctx = AssemblyLoadContext.Default;
			ctx.Resolving += (_, name) => {
				if (name.Name == "System.ComponentModel.Composition") {
					string gac = "/usr/lib/mono/4.5/System.ComponentModel.Composition.dll";
					if (File.Exists (gac))
						return ctx.LoadFromAssemblyPath (gac);
				}
				foreach (string dir in MonoAssemblyDirs) {
					string path = Path.Combine (dir, name.Name + ".dll");
					if (File.Exists (path))
						return ctx.LoadFromAssemblyPath (path);
				}
				return null;
			};
			ctx.ResolvingUnmanagedDll += ResolveNativeLibrary;
			return IdeStartup.Main (args);
		}
	}
}