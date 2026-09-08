//
// MonoDevelopFrameworkAssemblyPathResolver.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory (typeof (IFrameworkAssemblyPathResolver), ServiceLayer.Host), Shared]
	class MonoDevelopFrameworkAssemblyPathResolverFactory : IWorkspaceServiceFactory, IFrameworkAssemblyPathResolver
	{
		class MonoDevelopFrameworkAssemblyPathResolver : IFrameworkAssemblyPathResolver
		{
			readonly MonoDevelopWorkspace workspace;
			public MonoDevelopFrameworkAssemblyPathResolver (MonoDevelopWorkspace workspace)
			{
				this.workspace = workspace;
			}

			public string ResolveAssemblyPath (ProjectId projectId, string assemblyName, string fullyQualifiedName = null)
			{
				if (workspace == null)
					return null;

				if (!(workspace.GetMonoProject (projectId) is DotNetProject monoProject))
					return null;

				string assemblyFile = monoProject.AssemblyContext.GetAssemblyLocation (assemblyName, monoProject.TargetFramework);
				if (assemblyFile != null) {
					//if (string.IsNullOrEmpty(fullyQualifiedName) || CanResolveType(ResolveAssembly (projectId, assemblyName), fullyQualifiedName))
					return assemblyFile;
				}

				return null;
			}
		}

		MonoDevelopFrameworkAssemblyPathResolver resolver;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			resolver = new MonoDevelopFrameworkAssemblyPathResolver (workspaceServices.Workspace as MonoDevelopWorkspace);
			return this;
		}

		public string ResolveAssemblyPath (ProjectId projectId, string assemblyName, string fullyQualifiedName = null)
		{
			return resolver.ResolveAssemblyPath (projectId, assemblyName, fullyQualifiedName);
		}
	}
}
