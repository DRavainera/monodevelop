using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MonoDevelop.CSharp.RoslynCompat
{
	public class CSharpTypeInferenceService
	{
		public IEnumerable<ITypeSymbol> InferTypes (SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			return System.Linq.Enumerable.Empty<ITypeSymbol> ();
		}

		public IEnumerable<ITypeSymbol> InferTypes (SemanticModel semanticModel, SyntaxNode syntax, CancellationToken cancellationToken)
		{
			return System.Linq.Enumerable.Empty<ITypeSymbol> ();
		}
	}

	public class CSharpSyntaxContext
	{
		public SyntaxToken LeftToken { get; internal set; }

		public SyntaxToken TargetToken { get; internal set; }

		public SyntaxTree SyntaxTree { get; internal set; }

		public SemanticModel SemanticModel { get; internal set; }

		public IReadOnlyList<ITypeSymbol> InferredTypes { get; internal set; }

		public bool IsInstanceContext { get; internal set; }

		public bool IsGlobalStatementContext { get; internal set; }

		public bool IsIsOrAsTypeContext { get; internal set; }

		public bool IsNonAttributeExpressionContext { get; internal set; }

		public bool IsParameterTypeContext { get; internal set; }

		public bool IsPreProcessorKeywordContext { get; internal set; }

		public bool IsPreProcessorExpressionContext { get; internal set; }

		public static CSharpSyntaxContext CreateContext (Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var tree = semanticModel.SyntaxTree;
			var root = tree.GetRoot (cancellationToken);
			var targetToken = root.FindToken (position);
			var leftToken = targetToken.GetPreviousToken ();
			var enclosingSymbol = semanticModel.GetEnclosingSymbol (position, cancellationToken);

			var isOrAsContext = targetToken.Parent != null
				&& targetToken.Parent.AncestorsAndSelf ()
					.OfType<BinaryExpressionSyntax> ()
					.Any (b => b.IsKind (SyntaxKind.IsExpression) || b.IsKind (SyntaxKind.AsExpression));

			return new CSharpSyntaxContext {
				LeftToken = leftToken,
				TargetToken = targetToken,
				SyntaxTree = tree,
				SemanticModel = semanticModel,
				InferredTypes = new CSharpTypeInferenceService ().InferTypes (semanticModel, position, cancellationToken).ToList (),
				IsInstanceContext = enclosingSymbol is IMethodSymbol method && !method.IsStatic,
				IsGlobalStatementContext = enclosingSymbol is IMethodSymbol mainMethod && IsTopLevelMainMethod (mainMethod),
				IsIsOrAsTypeContext = isOrAsContext,
				IsNonAttributeExpressionContext = true,
				IsParameterTypeContext = false,
				IsPreProcessorKeywordContext = IsPreProcessorLine (tree, position, cancellationToken),
				IsPreProcessorExpressionContext = IsPreProcessorLine (tree, position, cancellationToken),
			};
		}

		private static bool IsTopLevelMainMethod (IMethodSymbol method)
		{
			return method.MethodKind == MethodKind.Ordinary && method.Name == "<Main>";
		}

		private static bool IsPreProcessorLine (SyntaxTree tree, int position, CancellationToken cancellationToken)
		{
			var text = tree.GetText (cancellationToken);
			var line = text.Lines.GetLineFromPosition (position);
			var lineText = line.ToString ();
			var offsetInLine = position - line.Start;
			if (offsetInLine > lineText.Length)
				offsetInLine = lineText.Length;
			return lineText.Substring (0, offsetInLine).TrimStart ().StartsWith ("#");
		}
	}
}