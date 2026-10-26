//
// MetadataReferenceCache.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using MonoDevelop.Core;
using Roslyn.Utilities;
using System.Threading;
using System.Reflection;
using System.Globalization;
using MonoDevelop.Ide.TypeSystem.MetadataReferences;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MonoDevelop.Core.Assemblies;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class MonoDevelopMetadataReferenceManager : IWorkspaceService
	{
		readonly MetadataCache _metadataCache;
		readonly MetadataReferenceCache _metadataReferenceCache;
		static readonly ConditionalWeakTable<Metadata, object> lifetimeMap = new ConditionalWeakTable<Metadata, object> ();

		internal MonoDevelopMetadataReferenceManager ()
		{
			_metadataCache = new MetadataCache ();
			_metadataReferenceCache = new MetadataReferenceCache ();
		}

		/// <exception cref="IOException"/>
		/// <exception cref="BadImageFormatException" />
		internal Metadata GetMetadata (string fullPath, DateTime snapshotTimestamp)
		{
			var key = new FileKey (fullPath, snapshotTimestamp);
			// check existing metadata
			if (_metadataCache.TryGetMetadata (key, out var metadata)) {
				return metadata;
			}

			// load module bytes into in-memory streams so the files can be closed right away
			var streams = new List<Stream> ();
			var newMetadata = CreateAssemblyMetadata (key, streams);

			// don't dispose assembly metadata since it shares module metadata
			if (!_metadataCache.TryGetOrAddMetadata (key, new RecoverableMetadataValueSource (newMetadata, streams, lifetimeMap), out metadata)) {
				newMetadata.Dispose ();
			}

			return metadata;
		}

		/// <exception cref="IOException"/>
		/// <exception cref="BadImageFormatException" />
		AssemblyMetadata CreateAssemblyMetadata (FileKey fileKey, List<Stream> streams)
		{
			var moduleMetadata = CreateModuleMetadata (fileKey, streams);
			return CreateAssemblyMetadata (fileKey, moduleMetadata, streams, CreateModuleMetadata);
		}

		ModuleMetadata CreateModuleMetadata (FileKey moduleFileKey, List<Stream> streams)
		{
			var stream = CreateModuleStream (moduleFileKey);

			var metadata = ModuleMetadata.CreateFromStream (stream);

			// first time, the metadata is created. tie lifetime.
			lifetimeMap.Add (metadata, stream);

			streams.Add (stream);

			return metadata;
		}

		Stream CreateModuleStream (FileKey moduleFileKey)
		{
			byte [] bytes;
			// open a file and let it go as soon as possible
			using (var fileStream = FileUtilities.OpenRead (moduleFileKey.FullPath)) {
				using (var copyStream = new MemoryStream ()) {
					fileStream.CopyTo (copyStream);
					bytes = copyStream.ToArray ();
				}
			}

			return new MemoryStream (bytes, writable: false);
		}

		/// <exception cref="IOException"/>
		/// <exception cref="BadImageFormatException" />
		AssemblyMetadata CreateAssemblyMetadata (
		   FileKey fileKey, ModuleMetadata manifestModule, List<Stream> streams,
		   Func<FileKey, List<Stream>, ModuleMetadata> moduleMetadataFactory)
		{
			var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance ();

			string assemblyDir = null;
			foreach (string moduleName in manifestModule.GetModuleNames ()) {
				if (moduleBuilder.Count == 0) {
					moduleBuilder.Add (manifestModule);
					assemblyDir = Path.GetDirectoryName (fileKey.FullPath);
				}

				var moduleFileKey = FileKey.Create (PathUtilities.CombineAbsoluteAndRelativePaths (assemblyDir, moduleName));
				var metadata = moduleMetadataFactory (moduleFileKey, streams);

				moduleBuilder.Add (metadata);
			}

			if (moduleBuilder.Count == 0) {
				moduleBuilder.Add (manifestModule);
			}

			return AssemblyMetadata.Create (
				moduleBuilder.ToImmutableAndFree ());
		}

		public PortableExecutableReference GetOrCreateMetadataReferenceSnapshot (string filePath, MetadataReferenceProperties properties)
		{
			return _metadataReferenceCache.GetOrCreate (this, filePath, properties).CurrentSnapshot;
		}

		internal MonoDevelopMetadataReference GetOrCreateMetadataReference (string filePath, MetadataReferenceProperties properties)
		{
			return _metadataReferenceCache.GetOrCreate (this, filePath, properties);
		}

		public void ClearCache ()
		{
			// Clear the reference cache before the metadata cache
			// as the FileWatcher updates can technically trigger while the metadata cache
			// is being cleared, avoiding unnecessary work and possible items not being invalidated.
			_metadataReferenceCache.ClearCache ();
			_metadataCache.ClearCache();
		}


		// TODO: Figure out if we want to support metadata importers
		// See how metadata importers work in roslyn VS impl. This contains code that handles setting the framework paths
		/*
		ImmutableArray<string> GetRuntimeDirectories ()
		{
			var paths = new HashSet<string> (FilePath.PathComparer) {
				RuntimeEnvironment.GetRuntimeDirectory (),
			};

			if (Core.Platform.IsWindows) {
				// These values don't make sense on non-Windows
				paths.Add (Environment.GetFolderPath (Environment.SpecialFolder.Windows));
				paths.Add (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles));
				paths.Add (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86));
			}

			// Add all the runtimes' framework folders which contain assemblies
			// TODO: Maybe listen for runtime initialization events?
			foreach (var runtime in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
				paths.AddRange (runtime.GetAllFrameworkFolders ());
			}

			// Normalize the paths and return.
			return paths.Select (FileUtilities.NormalizeDirectoryPath).ToImmutableArray ();
		}
		*/

		// This should be added to TargetRuntime.cs
		/*
		public IEnumerable<string> GetAllFrameworkFolders ()
		{
			EnsureInitialized ();
			return frameworkBackends.SelectMany (backend => backend.Value.GetFrameworkFolders ());
		}
		*/
	}
}
