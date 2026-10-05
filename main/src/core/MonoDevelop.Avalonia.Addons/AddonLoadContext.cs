using System;
using System.IO;
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
                // It's an assembly name - try to find it
                var guess = Path.Combine(manifestDirectory, assemblyPathOrName + ".dll");
                if (File.Exists(guess))
                {
                    this.assemblyPath = guess;
                    this.AssemblyName = assemblyPathOrName;
                    this.resolver = new AssemblyDependencyResolver(guess);
                }
                else
                {
                    throw new FileNotFoundException($"Assembly '{assemblyPathOrName}' not found in directory '{manifestDirectory}'");
                }
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
            // Try to resolve using AssemblyDependencyResolver first
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