//
// CSharpCompletionService.cs
//
// Compatibility stub for the CSharpBinding addin. The Roslyn 3.4.0-beta4-final
// fork used by this repository does not ship the editor completion services
// (Microsoft.CodeAnalysis.CSharp.Completion.*), so MonoDevelop's own
// completion machinery needs a small concrete completion service to map back
// from a Roslyn CompletionItem to a MonoDevelop completion provider.
//
// Deferred/legacy: this mirrors the old MonoDevelop type that lived in the
// missing Roslyn features assemblies. Kept minimal for offline stabilization.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion
{
	class CSharpCompletionService : CompletionService
	{
		public CompletionProvider GetProvider (CompletionItem completionItem)
		{
			return null;
		}
	}
}