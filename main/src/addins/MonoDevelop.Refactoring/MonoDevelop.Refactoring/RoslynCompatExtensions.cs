using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Refactoring
{
	static class RoslynCompatExtensions
	{
		public static Document GetOpenDocumentInCurrentContextWithChanges (this ITextSnapshot snapshot)
		{
			return snapshot.TextBuffer.AsTextContainer ().CurrentText.GetOpenDocumentInCurrentContextWithChanges ();
		}
	}
}