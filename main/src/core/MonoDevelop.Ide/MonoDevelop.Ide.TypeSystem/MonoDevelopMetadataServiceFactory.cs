//
// MonoDevelopMetadataServiceFactory.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace MonoDevelop.Ide.TypeSystem.MetadataReferences
{
	[ExportWorkspaceServiceFactory (typeof (IMetadataService), ServiceLayer.Host), Shared]
	class MonoDevelopMetadataServiceFactory : IWorkspaceServiceFactory, IMetadataService
	{
		HostWorkspaceServices workspaceServices;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			this.workspaceServices = workspaceServices;
			return this;
		}

		public PortableExecutableReference GetReference (string resolvedPath, MetadataReferenceProperties properties)
		{
			// This manager is created lazily on first request to avoid it being constructed too early and
			// potentially causing deadlocks.
			var manager = workspaceServices.GetRequiredService<MonoDevelopMetadataReferenceManager> ();
			return manager.GetOrCreateMetadataReferenceSnapshot (resolvedPath, properties);
		}
	}

	[ExportWorkspaceServiceFactory (typeof (MonoDevelopMetadataReferenceManager), ServiceLayer.Host), Shared]
	class MonoDevelopMetadataReferenceManagerFactory : MonoDevelopMetadataReferenceManager, IWorkspaceServiceFactory
	{
		public MonoDevelopMetadataReferenceManagerFactory ()
			: base (null)
		{
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return this;
		}
	}
}
