using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    public enum WaitIndicatorResult { Completed, Canceled }
    public interface IWaitIndicator
    {
        WaitIndicatorResult Wait(string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action);
        IWaitContext StartWait(string title, string message, bool allowCancel, bool showProgress);
    }
    public interface IWaitContext : IDisposable
    {
        CancellationToken CancellationToken { get; }
        bool AllowCancel { get; set; }
        string Message { get; set; }
    }
}

namespace Microsoft.CodeAnalysis
{
    public class DefinitionItem { }
    public class BlockSpan { }
    public class BackgroundParser : IDisposable
    {
        readonly Workspace workspace;

        public BackgroundParser(Workspace workspace)
        {
            this.workspace = workspace;
        }

        public void Start() { }
        public void Parse(Document document) { }
        public void CancelParse(DocumentId documentId) { }
        public void Dispose() { }
    }
    public class BackgroundCompiler : IDisposable
    {
        readonly Workspace workspace;

        public BackgroundCompiler(Workspace workspace)
        {
            this.workspace = workspace;
        }

        public void Dispose() { }
    }
    // AbstractHostDiagnosticUpdateSource / IDiagnosticUpdateSourceRegistrationService /
    // DiagnosticsUpdatedArgs / IDiagnosticService / AnalyzerHelper / GetExistingOrCalculatedTextSpan
    // live in MonoDevelop.Ide (a strong-named friend of Microsoft.CodeAnalysis.Workspaces)
    // because they expose the internal DiagnosticData type. See
    // MonoDevelop.Ide.TypeSystem/DiagnosticsCompat.cs.
    public class ForegroundThreadAffinitizedObject { }
    public class EngineEnvironmentSettings { }
    public class DefaultTemplateEngineHost { }
    public class ProjectCacheService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        public ProjectCacheService(Microsoft.CodeAnalysis.Workspace workspace) { }
        public ProjectCacheService(Microsoft.CodeAnalysis.Workspace workspace, int implicitCacheTimeout) { }
        public IDisposable EnableCaching(Microsoft.CodeAnalysis.ProjectId projectId) { return null; }
        public void ClearImplicitCache() { }
    }
    public delegate void ConventionsFileChangedAsyncEventHandler(object sender, EventArgs e);
    public delegate void ContextFileMovedAsyncEventHandler(object sender, EventArgs e);
    public interface IDocumentTrackingService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        event EventHandler<Microsoft.CodeAnalysis.DocumentId> ActiveDocumentChanged;
        Microsoft.CodeAnalysis.DocumentId TryGetActiveDocument();
    }
    public class RoslynAssemblyHelper
    {
        static readonly byte[] MicrosoftPublicKeyToken = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };

        public static bool HasRoslynPublicKey(object instance)
        {
            try {
                var assembly = instance?.GetType().Assembly;
                if (assembly == null)
                    return false;
                var token = assembly.GetName().GetPublicKeyToken();
                if (token == null || token.Length != MicrosoftPublicKeyToken.Length)
                    return false;
                for (int i = 0; i < token.Length; i++)
                    if (token[i] != MicrosoftPublicKeyToken[i])
                        return false;
                return true;
            } catch {
                return false;
            }
        }
    }
    // This was formerly supplied by Roslyn's non-public editor assemblies.
    // MonoDevelop only needs its value semantics and the tag strings used by
    // the completion/quick-info markup adapter.
    public struct TaggedText : IEquatable<TaggedText>
    {
        public string Tag { get; }
        public string Text { get; }

        public TaggedText(string tag, string text)
        {
            Tag = tag;
            Text = text;
        }

        public bool Equals(TaggedText other)
        {
            return string.Equals(Tag, other.Tag, StringComparison.Ordinal)
                && string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TaggedText && Equals((TaggedText)obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((Tag != null ? Tag.GetHashCode() : 0) * 397)
                    ^ (Text != null ? Text.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return Text ?? string.Empty;
        }
    }

    public static class TextTags
    {
        public const string Alias = nameof(Alias);
        public const string AnonymousTypeIndicator = nameof(AnonymousTypeIndicator);
        public const string Assembly = nameof(Assembly);
        public const string Class = nameof(Class);
        public const string Delegate = nameof(Delegate);
        public const string Enum = nameof(Enum);
        public const string ErrorType = nameof(ErrorType);
        public const string Event = nameof(Event);
        public const string Field = nameof(Field);
        public const string Interface = nameof(Interface);
        public const string Keyword = nameof(Keyword);
        public const string Label = nameof(Label);
        public const string LineBreak = nameof(LineBreak);
        public const string Local = nameof(Local);
        public const string Method = nameof(Method);
        public const string Module = nameof(Module);
        public const string Namespace = nameof(Namespace);
        public const string NumericLiteral = nameof(NumericLiteral);
        public const string Operator = nameof(Operator);
        public const string Parameter = nameof(Parameter);
        public const string Property = nameof(Property);
        public const string Punctuation = nameof(Punctuation);
        public const string RangeVariable = nameof(RangeVariable);
        public const string Space = nameof(Space);
        public const string StringLiteral = nameof(StringLiteral);
        public const string Struct = nameof(Struct);
        public const string Text = nameof(Text);
        public const string TypeParameter = nameof(TypeParameter);
    }
    public class TodoItem { }
    public class WorkspaceId
    {
        public static WorkspaceId Empty { get; } = new WorkspaceId();
        public string DebugName { get { return ""; } }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public static class TaggedTextExtensions
    {
        public static string GetFullText(this System.Collections.Immutable.ImmutableArray<TaggedText> parts)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var part in parts)
                builder.Append(part.Text);
            return builder.ToString();
        }
    }
}

namespace Microsoft.CodeAnalysis.Diagnostics
{
    using Microsoft.CodeAnalysis.Text;

    public interface IDiagnosticAnalyzerService
    {
        void Reanalyze(Microsoft.CodeAnalysis.Workspace workspace);
    }
}

namespace Microsoft.CodeAnalysis.Completion
{
    public class CompletionDescription
    {
        public string Text { get; set; }
        public System.Collections.Immutable.ImmutableArray<TaggedText> TaggedParts { get; set; }
    }

    public static class CommonCompletionItem
    {
        public static bool HasDescription(CompletionItem item)
        {
            return item != null && item.Properties != null && item.Properties.ContainsKey("Description");
        }

        public static CompletionDescription GetDescription(CompletionItem item)
        {
            string description;
            if (item != null && item.Properties != null && item.Properties.TryGetValue("Description", out description))
                return new CompletionDescription { Text = description };
            return null;
        }
    }

    public class CompletionItem
    {
        public CompletionItemRules Rules { get; set; }
        public string DisplayText { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> Tags { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, string> Properties { get; set; }
            = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
    }

    public enum EnterKeyRule
    {
        Never,
        AfterFullyTypedWord,
        Always,
        Default
    }

    public enum CharacterSetModificationKind
    {
        Add,
        Remove,
        Replace
    }

    public class CharacterSetModificationRule
    {
        public CharacterSetModificationKind Kind { get; set; }
        public System.Collections.Immutable.ImmutableArray<char> Characters { get; set; }

        public static CharacterSetModificationRule Create(CharacterSetModificationKind kind, params char[] characters)
        {
            return new CharacterSetModificationRule {
                Kind = kind,
                Characters = characters != null
                    ? System.Collections.Immutable.ImmutableArray.CreateRange(characters)
                    : System.Collections.Immutable.ImmutableArray<char>.Empty
            };
        }
    }

    public class CompletionItemRules
    {
        public static CompletionItemRules Default { get; } = new CompletionItemRules();

        public int MatchPriority { get; set; }
        public EnterKeyRule EnterKeyRule { get; set; }
        public bool FormatOnCommit { get; set; }
        public IEnumerable<CharacterSetModificationRule> CommitCharacterRules { get; set; }
            = new CharacterSetModificationRule[0];

        public static CompletionItemRules Create(int matchPriority = 0)
        {
            return new CompletionItemRules { MatchPriority = matchPriority };
        }

        public CompletionItemRules WithCommitCharacterRule(CharacterSetModificationRule rule)
        {
            return new CompletionItemRules {
                MatchPriority = MatchPriority,
                EnterKeyRule = EnterKeyRule,
                FormatOnCommit = FormatOnCommit,
                CommitCharacterRules = new[] { rule }
            };
        }
    }

    public class CompletionChange
    {
        public Microsoft.CodeAnalysis.Text.TextChange TextChange { get; set; }
        public int? NewPosition { get; set; }
    }

    public class CompletionProvider
    {
        public virtual System.Threading.Tasks.Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            string commitCharacter,
            System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(new CompletionChange());
        }
    }

    public class CompletionService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        public virtual System.Threading.Tasks.Task<CompletionDescription> GetDescriptionAsync(
            Document document,
            CompletionItem item,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            return System.Threading.Tasks.Task.FromResult<CompletionDescription>(null);
        }
    }
}

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    public enum SignatureHelpTriggerReason
    {
        Invoke,
        TypeChar,
        TypingChar,
        Token
    }

    public struct SignatureHelpTriggerInfo
    {
        public SignatureHelpTriggerReason TriggerReason { get; }
        public char? TriggerCharacter { get; }

        public SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
        {
            TriggerReason = triggerReason;
            TriggerCharacter = triggerCharacter;
        }
    }

    public class SignatureHelpParameter
    {
        public string Name { get; set; }
        public bool IsOptional { get; set; }
        public System.Collections.Immutable.ImmutableArray<TaggedText> DisplayParts { get; set; }
        public Func<System.Threading.CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; set; }
    }

    public class SignatureHelpItem
    {
        public System.Collections.Immutable.ImmutableArray<SignatureHelpParameter> Parameters { get; set; }
        public bool IsVariadic { get; set; }
        public System.Collections.Immutable.ImmutableArray<TaggedText> PrefixDisplayParts { get; set; }
        public System.Collections.Immutable.ImmutableArray<TaggedText> SeparatorDisplayParts { get; set; }
        public System.Collections.Immutable.ImmutableArray<TaggedText> SuffixDisplayParts { get; set; }
        public Func<System.Threading.CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; set; }
    }
}

namespace Microsoft.CodeAnalysis.Options
{
    public static class FeatureOnOffOptions
    {
        public const string FeatureName = "FeatureOnOffOptions";
        public static readonly Option<bool> AutoFormattingOnCloseBrace = new Option<bool>(FeatureName, nameof(AutoFormattingOnCloseBrace));
        public static readonly Option<bool> AutoFormattingOnSemicolon = new Option<bool>(FeatureName, nameof(AutoFormattingOnSemicolon));
        public static readonly Option<bool> AutoFormattingOnTyping = new Option<bool>(FeatureName, nameof(AutoFormattingOnTyping));
        public static readonly Option<bool> FormatOnPaste = new Option<bool>(FeatureName, nameof(FormatOnPaste));
    }

    public static class ServiceFeatureOnOffOptions
    {
        public const string FeatureName = "ServiceFeatureOnOffOptions";
        public static readonly Option<bool?> ClosedFileDiagnostic = new Option<bool?>(FeatureName, nameof(ClosedFileDiagnostic), defaultValue: true);
    }

    public static class CompletionOptions
    {
        public const string FeatureName = "CompletionOptions";
        public static readonly Option<bool> ShowCompletionItemFilters = new Option<bool>(FeatureName, nameof(ShowCompletionItemFilters));
        public static readonly Option<bool?> ShowItemsFromUnimportedNamespaces = new Option<bool?>(FeatureName, nameof(ShowItemsFromUnimportedNamespaces), defaultValue: false);
        public static readonly Option<bool?> TriggerOnDeletion = new Option<bool?>(FeatureName, nameof(TriggerOnDeletion), defaultValue: false);
        public static readonly Option<bool> TriggerOnTypingLetters = new Option<bool>(FeatureName, nameof(TriggerOnTypingLetters));
    }
}

namespace Microsoft.CodeAnalysis.Editor
{
    public interface ITodoListProvider
    {
        event EventHandler<Implementation.TodoComments.TodoItemsUpdatedArgs> TodoListUpdated;
    }
    public interface INavigateToSearchService { }
    public interface INavigateToSearchResult { }
    public interface INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate { }
    public class TodoItem
    {
        public string Message { get; set; }
        public int MappedLine { get; set; }
        public int MappedColumn { get; set; }
    }
    }

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    public class TodoItemsUpdatedArgs : Microsoft.CodeAnalysis.Common.UpdatedEventArgs
    {
        public ImmutableArray<TodoItem> TodoItems { get; set; }
    }
    public class TodoItem : Microsoft.CodeAnalysis.Editor.TodoItem { }
    public static class TodoCommentOptions
    {
        public static readonly Microsoft.CodeAnalysis.Options.Option<string> TokenList =
            new Microsoft.CodeAnalysis.Options.Option<string>(nameof(TodoCommentOptions), nameof(TokenList), defaultValue: "");
    }
}

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    public interface IThreadingContext
    {
        Microsoft.VisualStudio.Threading.JoinableTaskFactory JoinableTaskFactory { get; }
    }
    public class ForegroundThreadAffinitizedObject
    {
        protected ForegroundThreadAffinitizedObject() { }
        protected ForegroundThreadAffinitizedObject(IThreadingContext threadingContext) { }
        protected void ThisCanBeCalledOnAnyThread() { }
        protected void AssertIsForeground() { }
    }
    public interface IForegroundNotificationService
    {
        void RegisterNotification(Action callback, object asyncToken);
    }
}

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    public class PerLanguageOption2<T> { }
}

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    public interface ITextBufferSupportsFeatureService { }
    public interface IEditorFormattingService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        bool SupportsFormatOnReturn { get; }
        bool SupportsFormattingOnTypedCharacter(Document document, char ch);
        System.Threading.Tasks.Task<IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesOnReturnAsync(Document document, int position, CancellationToken cancellationToken);
        System.Threading.Tasks.Task<IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesAsync(Document document, char typedChar, int position, CancellationToken cancellationToken);
        System.Threading.Tasks.Task<IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesAsync(Document document, Microsoft.CodeAnalysis.Text.TextSpan? span, CancellationToken cancellationToken);
    }
    public interface ILineSeparatorService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        System.Threading.Tasks.Task<IEnumerable<Microsoft.CodeAnalysis.Text.TextSpan>> GetLineSeparatorsAsync(Document document, Microsoft.CodeAnalysis.Text.TextSpan span, CancellationToken cancellationToken);
    }
    public interface IFileWatcher { }
    public interface IForegroundNotificationService { }
    public delegate void ConventionsFileChangedAsyncEventHandler(object sender, EventArgs e);
    public delegate void ContextFileMovedAsyncEventHandler(object sender, EventArgs e);
}

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    public enum ChangeType
    {
        FileModified,
        FileDeleted
    }

    public class ConventionsFileChangeEventArgs : EventArgs
    {
        public ConventionsFileChangeEventArgs(string fileName, string directoryPath, ChangeType changeType)
        {
            FileName = fileName;
            DirectoryPath = directoryPath;
            ChangeType = changeType;
        }

        public string FileName { get; }
        public string DirectoryPath { get; }
        public ChangeType ChangeType { get; }
    }

    public class ContextFileMovedEventArgs : EventArgs
    {
        public ContextFileMovedEventArgs(string sourceFile, string targetFile)
        {
            SourceFile = sourceFile;
            TargetFile = targetFile;
        }

        public string SourceFile { get; }
        public string TargetFile { get; }
    }

    public interface IFileWatcher { }
    public delegate void ConventionsFileChangedAsyncEventHandler(object sender, EventArgs e);
    public delegate void ContextFileMovedAsyncEventHandler(object sender, EventArgs e);
}

namespace Microsoft.CodeAnalysis.Notification
{
    public enum NotificationSeverity { Information, Warning, Error }
    public interface INotificationService : Microsoft.CodeAnalysis.Host.IWorkspaceService { }
    public interface INotificationServiceCallback : Microsoft.CodeAnalysis.Host.IWorkspaceService { }
    public interface IForegroundNotificationService { }
}

namespace Microsoft.CodeAnalysis.NavigateTo
{
    public static class NavigateToItemKind
    {
        public const string Class = nameof(Class);
        public const string Constant = nameof(Constant);
        public const string Delegate = nameof(Delegate);
        public const string Enum = nameof(Enum);
        public const string EnumItem = nameof(EnumItem);
        public const string Event = nameof(Event);
        public const string Field = nameof(Field);
        public const string Interface = nameof(Interface);
        public const string Method = nameof(Method);
        public const string Module = nameof(Module);
        public const string Property = nameof(Property);
        public const string Structure = nameof(Structure);
    }

    public enum NavigateToMatchKind
    {
        Exact,
        Prefix,
        Substring,
        Regular
    }

    public sealed class NavigableItem
    {
        public Microsoft.CodeAnalysis.Document Document { get; set; }
        public Microsoft.CodeAnalysis.Text.TextSpan SourceSpan { get; set; }
    }

    public interface INavigateToSearchResult
    {
        string Name { get; }
        string Kind { get; }
        NavigableItem NavigableItem { get; }
        NavigateToMatchKind MatchKind { get; }
        bool IsCaseSensitive { get; }
        ImmutableArray<Microsoft.CodeAnalysis.Text.TextSpan> NameMatchSpans { get; }
    }

    public interface INavigateToSearchService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        System.Threading.Tasks.Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync (
            Microsoft.CodeAnalysis.Document document,
            string searchPattern,
            CancellationToken cancellationToken);
        System.Threading.Tasks.Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync (
            Microsoft.CodeAnalysis.Project project,
            string searchPattern,
            CancellationToken cancellationToken);
    }

    public interface INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        IImmutableSet<string> KindsProvided { get; }
        bool CanFilter { get; }
        System.Threading.Tasks.Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync (
            Microsoft.CodeAnalysis.Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            CancellationToken cancellationToken);
        System.Threading.Tasks.Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync (
            Microsoft.CodeAnalysis.Project project,
            ImmutableArray<Microsoft.CodeAnalysis.Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.FindUsages
{
    public interface ISymbolNavigationService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        bool TryNavigateToSymbol(
            Microsoft.CodeAnalysis.ISymbol symbol,
            Microsoft.CodeAnalysis.Project project,
            Microsoft.CodeAnalysis.Options.OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken));
        bool TrySymbolNavigationNotify(
            Microsoft.CodeAnalysis.ISymbol symbol,
            Microsoft.CodeAnalysis.Project project,
            CancellationToken cancellationToken = default(CancellationToken));
        bool WouldNavigateToSymbol(
            Microsoft.CodeAnalysis.DefinitionItem definitionItem,
            Microsoft.CodeAnalysis.Solution solution,
            CancellationToken cancellationToken,
            out string filePath,
            out int lineNumber,
            out int charOffset);
    }
}

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    public interface IFileWatcher { }
    public interface ITextBufferSupportsFeatureService { }
    public interface IForegroundNotificationService { }
}

namespace Microsoft.CodeAnalysis.Editor.Structure
{
    public class BlockSpan { }
}

namespace Microsoft.CodeAnalysis.Structure
{
    public enum BlockTypes { Member, Type, Comment }
    public class BlockSpan
    {
        public bool IsCollapsible { get; set; }
        public BlockTypes Type { get; set; }
        public Microsoft.CodeAnalysis.Text.TextSpan TextSpan { get; set; }
        public string BannerText { get; set; }
    }
    public class BlockStructure
    {
        public ImmutableArray<BlockSpan> Spans { get; set; }
    }
    public static class BlockStructureService
    {
        public static OutliningService GetService(Microsoft.CodeAnalysis.Document document) { return null; }
    }
    public class OutliningService
    {
        public System.Threading.Tasks.Task<BlockStructure> GetBlockStructureAsync(Microsoft.CodeAnalysis.Document document, CancellationToken token)
        {
            return System.Threading.Tasks.Task.FromResult(new BlockStructure { Spans = ImmutableArray<BlockSpan>.Empty });
        }
    }
}

namespace Microsoft.CodeAnalysis.Editor.Options
{
    public class PerLanguageOption2<T> { }
}

namespace Microsoft.CodeAnalysis.Common
{
    public class UpdatedEventArgs : EventArgs
    {
        public Microsoft.CodeAnalysis.Workspace Workspace { get; set; }
        public Microsoft.CodeAnalysis.DocumentId DocumentId { get; set; }
        public Microsoft.CodeAnalysis.ProjectId ProjectId { get; set; }
        public object Id { get; set; }
    }
}

namespace Microsoft.CodeAnalysis.Scripting
{
    public class ScriptOptions { }

    public sealed class ScriptSourceResolver : Microsoft.CodeAnalysis.SourceReferenceResolver
    {
        public static ScriptSourceResolver Default { get; } = new ScriptSourceResolver();

        public override string NormalizePath(string path, string baseFilePath)
        {
            return path;
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            return reference;
        }

        public override System.IO.Stream OpenRead(string resolvedPath)
        {
            return null;
        }

        public override bool Equals(object obj)
        {
            return obj is ScriptSourceResolver;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public sealed class ScriptMetadataResolver : Microsoft.CodeAnalysis.MetadataReferenceResolver
    {
        public static ScriptMetadataResolver Default { get; } = new ScriptMetadataResolver();

        public override System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.PortableExecutableReference> ResolveReference(
            string reference,
            string baseFilePath,
            Microsoft.CodeAnalysis.MetadataReferenceProperties properties)
        {
            return System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.PortableExecutableReference>.Empty;
        }

        public override bool Equals(object obj)
        {
            return obj is ScriptMetadataResolver;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}

namespace Monodoc
{
    public class RootTree
    {
        public static RootTree LoadTree()
        {
            return new RootTree();
        }

        public void AddSource(string directory) { }

#pragma warning disable 618
        public System.Xml.XmlDocument GetHelpXml(string idString)
        {
            return null;
        }
#pragma warning restore 618
    }

    public class Node
    {
        public string Caption { get; set; }
        public string PublicUrl { get; set; }
        public Tree Nodes { get; set; }
    }

    public class Tree : System.Collections.IEnumerable
    {
        public System.Collections.IEnumerator GetEnumerator()
        {
            return System.Linq.Enumerable.Empty<Node>().GetEnumerator();
        }
    }
}

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    public class EditorTaskSchedulerFactory
    {
        public EditorTaskSchedulerFactory() { }
    }
}

namespace Microsoft.CodeAnalysis.Navigation
{
    public interface IDocumentNavigationService : Microsoft.CodeAnalysis.Host.IWorkspaceService { }

    public static class NavigationOptions
    {
        public const string FeatureName = "NavigationOptions";
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<bool> PreferProvisionalTab =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<bool>(FeatureName, nameof(PreferProvisionalTab), defaultValue: false);
    }
}

namespace Microsoft.CodeAnalysis.Editor.EditorLayerExtensionManager
{
    public class ExtensionManager : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        public ExtensionManager() { }
        public virtual void HandleException(object sender, Exception exception) { }
    }
}

namespace Microsoft.CodeAnalysis.CodeActions.WorkspaceServices
{
    public interface IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService { }
}

namespace Microsoft.CodeAnalysis.Text
{
    public static class TextSpanExtensions
    {
        public static TextSpan ToTextSpan(this TextSpan textSpan)
        {
            return textSpan;
        }

        public static TextSpan ToTextSpan(this Microsoft.VisualStudio.Text.Span span)
        {
            return new TextSpan(span.Start, span.Length);
        }
    }

    public static class TextBufferExtensions
    {
        public static SourceTextContainer AsTextContainer(this Microsoft.VisualStudio.Text.ITextBuffer textBuffer)
        {
            return new TextBufferContainer(textBuffer);
        }

        public static Microsoft.VisualStudio.Text.ITextBuffer GetTextBuffer(this SourceTextContainer container)
        {
            var typed = container as TextBufferContainer;
            return typed == null ? null : typed.TextBuffer;
        }

        public static Microsoft.VisualStudio.Text.ITextBuffer TryGetTextBuffer(this SourceTextContainer container)
        {
            var typed = container as TextBufferContainer;
            return typed == null ? null : typed.TextBuffer;
        }
    }

    sealed class TextBufferContainer : SourceTextContainer
    {
        readonly Microsoft.VisualStudio.Text.ITextBuffer textBuffer;
        SourceText currentText;

        internal Microsoft.VisualStudio.Text.ITextBuffer TextBuffer
        {
            get { return textBuffer; }
        }

        internal TextBufferContainer(Microsoft.VisualStudio.Text.ITextBuffer textBuffer)
        {
            this.textBuffer = textBuffer;
            var snapshot = textBuffer.CurrentSnapshot;
            currentText = SourceText.From(snapshot.GetText(0, snapshot.Length));
            textBuffer.Changed += OnTextBufferChanged;
        }

        void OnTextBufferChanged(object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
        {
            var snapshot = e.After;
            currentText = SourceText.From(snapshot.GetText(0, snapshot.Length));
            var handlers = TextChanged;
            if (handlers == null)
                return;
            var oldSnapshot = e.Before;
            var oldText = SourceText.From(oldSnapshot.GetText(0, oldSnapshot.Length));
            handlers(this, new TextChangeEventArgs(oldText, currentText));
        }

        public override SourceText CurrentText
        {
            get { return currentText; }
        }

        public override event EventHandler<TextChangeEventArgs> TextChanged;
    }
}

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
}

namespace System.Net.Sockets
{
    public class UnixEndPoint : EndPoint
    {
        public UnixEndPoint(string filename) { }
    }
}

namespace Mono.Unix
{
    [Flags]
    public enum FileAccessPermissions
    {
        None = 0,
        UserRead = 0x100,
        UserWrite = 0x80,
        UserExecute = 0x40,
        GroupRead = 0x20,
        GroupWrite = 0x10,
        GroupExecute = 0x8,
        OtherRead = 0x4,
        OtherWrite = 0x2,
        OtherExecute = 0x1,
    }

    public class UnixFileSystemInfo
    {
        public static UnixFileSystemInfo GetFileSystemEntry(string path)
        {
            return null;
        }

        public virtual FileAccessPermissions FileAccessPermissions { get; set; }
        public virtual bool Exists { get { return false; } }
    }
}

namespace Roslyn.Utilities
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> add)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
                dictionary.Add(key, value = add(key));
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            TValue existing;
            if (!dictionary.TryGetValue(key, out existing))
                dictionary.Add(key, existing = value);
            return existing;
        }
    }
}
