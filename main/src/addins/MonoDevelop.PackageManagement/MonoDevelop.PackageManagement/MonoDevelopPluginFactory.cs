//
// MonoDevelopPluginFactory.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2020 Microsoft Corporation
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using NuGet.Protocol.Plugins;

namespace MonoDevelop.PackageManagement
{
	// En NuGet 7.9.0 IPluginFactory es internal y el PluginManager pide una
	// Func<TimeSpan, PluginFactory> (la clase pública). Por eso el wrapper se
	// construye como subclase de PluginFactory y solo sobrescribe GetOrCreateAsync
	// (virtual en 7.9.0) para inyectar el host mono en el camino plugin.
	sealed class MonoDevelopPluginFactory : PluginFactory
	{
		public MonoDevelopPluginFactory (TimeSpan idleTimeout)
			: base (idleTimeout)
		{
		}

		public override Task<IPlugin> GetOrCreateAsync (
			PluginFile pluginFile,
			IEnumerable<string> arguments,
			IRequestHandlers requestHandlers,
			ConnectionOptions options,
			CancellationToken sessionCancellationToken)
		{
			if (!Platform.IsWindows) {
				var modifiedCommandLine = GetModifiedCommandLine (pluginFile.Path, arguments);
				if (modifiedCommandLine != null) {
					pluginFile = new PluginFile (modifiedCommandLine.FilePath, pluginFile.State, false);
					arguments = modifiedCommandLine.Arguments;
				}
			}

			return base.GetOrCreateAsync (pluginFile, arguments, requestHandlers, options, sessionCancellationToken);
		}

		ModifiedCommandLineArguments GetModifiedCommandLine (string filePath, IEnumerable<string> arguments)
		{
			string extension = Path.GetExtension (filePath);
			if (!StringComparer.OrdinalIgnoreCase.Equals (".exe", extension)) {
				return null;
			}

			var runtime = MonoRuntimeInfo.FromCurrentRuntime ();
			if (runtime == null) {
				return null;
			}

			string monoPath = Path.Combine (runtime.Prefix, "bin", "mono");

			List<string> updatedArguments = arguments.ToList ();
			updatedArguments.Insert (0, "\"" + filePath + "\"");

			return new ModifiedCommandLineArguments {
				FilePath = monoPath,
				Arguments = updatedArguments
			};
		}

		sealed class ModifiedCommandLineArguments
		{
			public string FilePath { get; set; }
			public List<string> Arguments { get; set; }
		}
	}
}
