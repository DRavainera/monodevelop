using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IThreadingContext))]
	[Shared]
	sealed class MonoDevelopThreadingContext : IThreadingContext
	{
		readonly JoinableTaskContext joinableTaskContext;

		public MonoDevelopThreadingContext ()
		{
			joinableTaskContext = new JoinableTaskContext (Runtime.MainThread, Runtime.MainSynchronizationContext);
		}

		public JoinableTaskFactory JoinableTaskFactory => joinableTaskContext.Factory;
	}
}