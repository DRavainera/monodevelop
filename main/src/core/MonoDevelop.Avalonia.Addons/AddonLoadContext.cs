using System;
using System.Reflection;
using System.Runtime.Loader;

namespace MonoDevelop.AvaloniaAddons
{
	/// <summary>
	/// Per-add-in <see cref="AssemblyLoadContext"/> so an add-in's dependencies never
	/// leak into the shell (Visual Studio runs extensions out-of-proc; we isolate in-proc).
	/// </summary>
	public sealed class AddonLoadContext : AssemblyLoadContext
	{
		readonly AssemblyDependencyResolver resolver;

		public AddonLoadContext (string assemblyPath)
			: base (isCollectible: true)
		{
			if (!string.IsNullOrEmpty (assemblyPath))
				resolver = new AssemblyDependencyResolver (assemblyPath);
		}

		protected override Assembly Load (AssemblyName assemblyName)
		{
			if (resolver is not null) {
				var path = resolver.ResolveAssemblyToPath (assemblyName);
				if (path is not null)
					return LoadFromAssemblyPath (path);
			}
			return null; // fall back to the default context (shell/shared)
		}

		protected override IntPtr LoadUnmanagedDll (string unmanagedDllName)
		{
			if (resolver is not null) {
				var path = resolver.ResolveUnmanagedDllToPath (unmanagedDllName);
				if (path is not null)
					return LoadUnmanagedDllFromPath (path);
			}
			return IntPtr.Zero;
		}
	}
}
