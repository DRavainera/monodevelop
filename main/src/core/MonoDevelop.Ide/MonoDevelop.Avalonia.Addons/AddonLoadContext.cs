using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MonoDevelop.AvaloniaAddons
{
    /// <summary>
    /// Load context for an add-in, using AssemblyDependencyResolver for proper dependency resolution.
    /// </summary>
    public sealed class AddonLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;
        private readonly string assemblyPath;

        public Assembly Assembly { get; private set; }
        public string AssemblyName { get; }

        public AddonLoadContext(string assemblyPathOrName, string manifestDirectory)
        {
            if (File.Exists(assemblyPathOrName))
            {
                // It's a file path
                this.assemblyPath = assemblyPathOrName;
                this.AssemblyName = Path.GetFileNameWithoutExtension(assemblyPathOrName);
                this.resolver = new AssemblyDependencyResolver(assemblyPathOrName);
            }
            else
            {
                // It's an assembly name: next to the manifest, then the unified
                // build tree (main/build; the single build output directory, same
                // ../../../build convention as the add-in csproj references).
                var name = assemblyPathOrName + ".dll";
                var candidates = new[]
                {
                    Path.Combine(manifestDirectory, name),
                    Path.GetFullPath(Path.Combine(manifestDirectory, "..", "..", "..", "build", name)),
                };
                this.assemblyPath = candidates.FirstOrDefault(File.Exists);
                if (this.assemblyPath is null)
                    throw new FileNotFoundException($"Assembly '{assemblyPathOrName}' not found in directory '{manifestDirectory}'");
                this.AssemblyName = Path.GetFileNameWithoutExtension(this.assemblyPath);
                this.resolver = new AssemblyDependencyResolver(this.assemblyPath);
            }

            LoadAssembly();
        }

        private void LoadAssembly()
        {
            try
            {
                Assembly = LoadFromAssemblyPath(assemblyPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load assembly '{assemblyPath}': {ex.Message}", ex);
            }
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Shared assemblies (the shell core: MonoDevelop.Avalonia.Addons,
            // MonoDevelop.Core, Xwt, ... — everything the shell itself loads)
            // must come from the default context: a second copy in this ALC would
            // break type identity (the add-in's IAvaloniaAddon/PlatformService
            // would not be assignable to the host's types, so LoadAll would never
            // activate the add-in's parts). Returning null lets the default
            // context serve the already-loaded instance.
            foreach (var a in AssemblyLoadContext.Default.Assemblies)
            {
                if (string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.Ordinal))
                    return null;
            }

            // Add-in-private dependencies: resolve with the add-in's own deps.
            var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Fall back to default resolution
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // Try to resolve unmanaged DLLs
            var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}