using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
	public enum AdjustSpacesOption
	{
		PreserveSpaces,
		ForceSpaces,
		ForceSpacesIfOnSingleLine,
		SuppressSpacesIfOnSingleLine,
		ForceSpacesIfOnSingleLineOrMoveLineIfOnMultiLine,
		PreserveSpacesIfOnSingleLine,
		PressSpaceOrIndentIfOnSingleLine,
	}

	public enum AdjustNewLinesOption
	{
		PreserveLines,
		ForceLines,
		ForceLinesIfOnSingleLine,
		PreserveLinesIfOnSingleLine,
		ForceLinesWithAdvanceMethodLine,
		ForceLinesAtBeginningOfFile,
	}

	public abstract class AbstractFormattingRule
	{
		public virtual AdjustSpacesOperation GetAdjustSpacesOperation (Microsoft.CodeAnalysis.SyntaxToken previousToken, Microsoft.CodeAnalysis.SyntaxToken currentToken, Microsoft.CodeAnalysis.Options.OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
		{
			return nextOperation.Invoke ();
		}

		public virtual AdjustNewLinesOperation GetAdjustNewLinesOperation (Microsoft.CodeAnalysis.SyntaxToken previousToken, Microsoft.CodeAnalysis.SyntaxToken currentToken, Microsoft.CodeAnalysis.Options.OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
		{
			return nextOperation.Invoke ();
		}
	}

	public class NoOpFormattingRule : AbstractFormattingRule
	{
		public static readonly NoOpFormattingRule Instance = new NoOpFormattingRule ();
	}

	public class BaseIndentationFormattingRule : AbstractFormattingRule
	{
		private readonly Microsoft.CodeAnalysis.SyntaxNode _root;
		private readonly Microsoft.CodeAnalysis.Text.TextSpan _span;
		private readonly int _baseIndentation;

		public BaseIndentationFormattingRule (Microsoft.CodeAnalysis.SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span, int baseIndentation)
		{
			_root = root;
			_span = span;
			_baseIndentation = baseIndentation;
		}

		public override AdjustSpacesOperation GetAdjustSpacesOperation (Microsoft.CodeAnalysis.SyntaxToken previousToken, Microsoft.CodeAnalysis.SyntaxToken currentToken, Microsoft.CodeAnalysis.Options.OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
		{
			if (_span.Contains (previousToken.Span) || _span.Contains (currentToken.Span))
				return FormattingOperations.CreateAdjustSpacesOperation (_baseIndentation, AdjustSpacesOption.PreserveSpaces);
			return nextOperation.Invoke ();
		}

		public override AdjustNewLinesOperation GetAdjustNewLinesOperation (Microsoft.CodeAnalysis.SyntaxToken previousToken, Microsoft.CodeAnalysis.SyntaxToken currentToken, Microsoft.CodeAnalysis.Options.OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
		{
			if (_span.Contains (previousToken.Span) || _span.Contains (currentToken.Span))
				return FormattingOperations.CreateAdjustNewLinesOperation (0, AdjustNewLinesOption.PreserveLines);
			return nextOperation.Invoke ();
		}
	}

	public readonly struct NextGetAdjustSpacesOperation
	{
		private readonly AbstractFormattingRule _rule;
		private readonly Microsoft.CodeAnalysis.SyntaxToken _previousToken;
		private readonly Microsoft.CodeAnalysis.SyntaxToken _currentToken;
		private readonly Microsoft.CodeAnalysis.Options.OptionSet _optionSet;

		internal NextGetAdjustSpacesOperation (AbstractFormattingRule rule, Microsoft.CodeAnalysis.SyntaxToken previousToken, Microsoft.CodeAnalysis.SyntaxToken currentToken, Microsoft.CodeAnalysis.Options.OptionSet optionSet)
		{
			_rule = rule;
			_previousToken = previousToken;
			_currentToken = currentToken;
			_optionSet = optionSet;
		}

		public AdjustSpacesOperation Invoke ()
		{
			return _rule == null ? null : _rule.GetAdjustSpacesOperation (_previousToken, _currentToken, _optionSet, in this);
		}
	}

	public readonly struct NextGetAdjustNewLinesOperation
	{
		private readonly AbstractFormattingRule _rule;
		private readonly Microsoft.CodeAnalysis.SyntaxToken _previousToken;
		private readonly Microsoft.CodeAnalysis.SyntaxToken _currentToken;
		private readonly Microsoft.CodeAnalysis.Options.OptionSet _optionSet;

		internal NextGetAdjustNewLinesOperation (AbstractFormattingRule rule, Microsoft.CodeAnalysis.SyntaxToken previousToken, Microsoft.CodeAnalysis.SyntaxToken currentToken, Microsoft.CodeAnalysis.Options.OptionSet optionSet)
		{
			_rule = rule;
			_previousToken = previousToken;
			_currentToken = currentToken;
			_optionSet = optionSet;
		}

		public AdjustNewLinesOperation Invoke ()
		{
			return _rule == null ? null : _rule.GetAdjustNewLinesOperation (_previousToken, _currentToken, _optionSet, in this);
		}
	}

	public class AdjustSpacesOperation
	{
		public int Spaces { get; }
		public AdjustSpacesOption Option { get; }

		internal AdjustSpacesOperation (int spaces, AdjustSpacesOption option)
		{
			Spaces = spaces;
			Option = option;
		}
	}

	public class AdjustNewLinesOperation
	{
		public int Lines { get; }
		public AdjustNewLinesOption Option { get; }

		internal AdjustNewLinesOperation (int lines, AdjustNewLinesOption option)
		{
			Lines = lines;
			Option = option;
		}
	}

	public static class FormattingOperations
	{
		public static AdjustSpacesOperation CreateAdjustSpacesOperation (int spaces, AdjustSpacesOption option)
		{
			return new AdjustSpacesOperation (spaces, option);
		}

		public static AdjustNewLinesOperation CreateAdjustNewLinesOperation (int lines, AdjustNewLinesOption option)
		{
			return new AdjustNewLinesOperation (lines, option);
		}
	}

	public interface IHostDependentFormattingRuleFactoryService : Microsoft.CodeAnalysis.Host.IWorkspaceService
	{
		bool ShouldUseBaseIndentation (Microsoft.CodeAnalysis.Document document);

		bool ShouldNotFormatOrCommitOnPaste (Microsoft.CodeAnalysis.Document document);

		AbstractFormattingRule CreateRule (Microsoft.CodeAnalysis.Document document, int position);

		IEnumerable<Microsoft.CodeAnalysis.Text.TextChange> FilterFormattedChanges (Microsoft.CodeAnalysis.Document document, Microsoft.CodeAnalysis.Text.TextSpan span, IList<Microsoft.CodeAnalysis.Text.TextChange> changes);
	}
}

namespace Microsoft.CodeAnalysis.FindSymbols
{
	public struct SymbolAndProjectId
	{
		public Microsoft.CodeAnalysis.ISymbol Symbol { get; }
		public Microsoft.CodeAnalysis.ProjectId ProjectId { get; }

		private SymbolAndProjectId (Microsoft.CodeAnalysis.ISymbol symbol, Microsoft.CodeAnalysis.ProjectId projectId)
		{
			Symbol = symbol;
			ProjectId = projectId;
		}

		public static SymbolAndProjectId Create (Microsoft.CodeAnalysis.ISymbol symbol, Microsoft.CodeAnalysis.ProjectId projectId)
		{
			return new SymbolAndProjectId (symbol, projectId);
		}
	}
}