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
     public class BlockSpan { }
     public struct DocumentSpan
     {
         public Document Document { get; set; }
         public Microsoft.CodeAnalysis.Text.TextSpan SourceSpan { get; set; }
     }
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
        public string SortText { get; set; }
        public string InlineDescription { get; set; }
        public Microsoft.CodeAnalysis.Text.TextSpan Span { get; set; }
        public System.Collections.Immutable.ImmutableArray<string> Tags { get; set; }
        public System.Collections.Immutable.ImmutableDictionary<string, string> Properties { get; set; }
            = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;

        public CompletionItem()
        {
        }

        public CompletionItem(
            string displayText,
            string sortText = null,
            System.Collections.Immutable.ImmutableDictionary<string, string> properties = null,
            CompletionItemRules rules = null,
            System.Collections.Immutable.ImmutableArray<string> tags = default(System.Collections.Immutable.ImmutableArray<string>),
            string inlineDescription = null)
        {
            DisplayText = displayText;
            SortText = sortText;
            Properties = properties ?? System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
            Rules = rules;
            Tags = tags;
            InlineDescription = inlineDescription;
        }

        public static CompletionItem Create(string displayText)
        {
            return new CompletionItem(displayText);
        }

        public static CompletionItem Create(
            string displayText,
            string sortText = null,
            System.Collections.Immutable.ImmutableDictionary<string, string> properties = null,
            CompletionItemRules rules = null,
            System.Collections.Immutable.ImmutableArray<string> tags = default(System.Collections.Immutable.ImmutableArray<string>),
            string inlineDescription = null)
        {
            return new CompletionItem(displayText, sortText, properties, rules, tags, inlineDescription);
        }

        public CompletionItem WithRules(CompletionItemRules rules)
        {
            return new CompletionItem(DisplayText, SortText, Properties, rules, Tags, InlineDescription);
        }
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

        public static CompletionItemRules Create(int matchPriority = 0, bool formatOnCommit = false)
        {
            return new CompletionItemRules { MatchPriority = matchPriority, FormatOnCommit = formatOnCommit };
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

        public CompletionItemRules WithMatchPriority(int matchPriority)
        {
            return new CompletionItemRules {
                MatchPriority = matchPriority,
                EnterKeyRule = EnterKeyRule,
                FormatOnCommit = FormatOnCommit,
                CommitCharacterRules = CommitCharacterRules
            };
        }
    }

    public class CompletionChange
    {
        public Microsoft.CodeAnalysis.Text.TextChange TextChange { get; set; }
        public int? NewPosition { get; set; }

        public static CompletionChange Create(Microsoft.CodeAnalysis.Text.TextChange textChange, int? newPosition = null, bool? formatOnCommit = null)
        {
            return new CompletionChange {
                TextChange = textChange,
                NewPosition = newPosition
            };
        }
    }

    public enum CompletionTriggerKind
    {
        Invoke,
        TypeChar,
        TypingChar,
        InvokeAndTypeChar,
        Legacy,
        Insertion,
        Deletion,
        InvokeAndCommitIfUnique
    }

    public class CompletionTrigger
    {
        public CompletionTriggerKind Kind { get; }
        public char? Character { get; }

        public CompletionTrigger(CompletionTriggerKind kind, char? character = null)
        {
            Kind = kind;
            Character = character;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ExportCompletionProviderAttribute : Attribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportCompletionProviderAttribute(string name, string language)
        {
            Name = name;
            Language = language;
        }
    }

    public class CompletionContext
    {
        readonly ICollection<CompletionItem> itemCollection = new List<CompletionItem>();

        public Document Document { get; }
        public int Position { get; }
        public CancellationToken CancellationToken { get; }
        public CompletionTrigger Trigger { get; }
        public Microsoft.CodeAnalysis.Text.TextSpan CompletionListSpan { get; }
        public bool IsExclusive { get; set; }

        public IEnumerable<CompletionItem> Items
        {
            get { return itemCollection; }
        }

        public CompletionContext(Document document, int position, CancellationToken cancellationToken, CompletionTrigger trigger = null, Microsoft.CodeAnalysis.Text.TextSpan? completionListSpan = null)
        {
            Document = document;
            Position = position;
            CancellationToken = cancellationToken;
            Trigger = trigger;
            CompletionListSpan = completionListSpan ?? new Microsoft.CodeAnalysis.Text.TextSpan(position, 0);
        }

        public void AddItem(CompletionItem item)
        {
            itemCollection.Add(item);
        }

        public void AddItems(IEnumerable<CompletionItem> items)
        {
            if (items == null)
                return;
            foreach (var item in items)
                itemCollection.Add(item);
        }
    }

    public class CommonCompletionProvider : CompletionProvider
    {
        public virtual bool IsInsertionTrigger(Microsoft.CodeAnalysis.Text.SourceText text, int insertedCharacterPosition, Microsoft.CodeAnalysis.Options.OptionSet options)
        {
            return false;
        }

        protected virtual System.Threading.Tasks.Task<Microsoft.CodeAnalysis.Text.TextChange?> GetTextChangeAsync(CompletionItem item, char? ch, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult<Microsoft.CodeAnalysis.Text.TextChange?>(null);
        }

        public virtual System.Threading.Tasks.Task<Microsoft.CodeAnalysis.Text.TextChange?> GetTextChangeAsync(CompletionItem item, int? position, char? ch, System.Threading.CancellationToken cancellationToken)
        {
            return GetTextChangeAsync(item, ch, cancellationToken);
        }
    }

    public class CompletionProvider
    {
        public virtual System.Threading.Tasks.Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            char? commitCharacter,
            System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(new CompletionChange());
        }

        public virtual System.Threading.Tasks.Task ProvideCompletionsAsync(CompletionContext context)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual bool ShouldTriggerCompletion(Microsoft.CodeAnalysis.Text.SourceText text, int position, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
        {
            return true;
        }

        protected virtual System.Threading.Tasks.Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document,
            CompletionItem item,
            System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult<CompletionDescription>(null);
        }
    }

    public class CompletionList
    {
        public System.Collections.Immutable.ImmutableArray<CompletionItem> Items { get; set; }
        public Microsoft.CodeAnalysis.Text.TextSpan Span { get; set; }
        public CompletionItem SuggestionModeItem { get; set; }
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

        public virtual bool ShouldTriggerCompletion(Microsoft.CodeAnalysis.Text.SourceText text, int insertedCharacterPosition, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
        {
            return false;
        }

        public virtual System.Threading.Tasks.Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger = null,
            Microsoft.CodeAnalysis.Options.OptionSet options = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            return System.Threading.Tasks.Task.FromResult<CompletionList>(null);
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
        Token,
        TypeCharCommand,
        RetriggerCommand
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

    public class SignatureHelpItems
    {
        public Microsoft.CodeAnalysis.Text.TextSpan ApplicableSpan { get; }
        public System.Collections.Immutable.ImmutableArray<SignatureHelpItem> Items { get; }
        public int? SelectedItemIndex { get; }

        public SignatureHelpItems(Microsoft.CodeAnalysis.Text.TextSpan applicableSpan, System.Collections.Immutable.ImmutableArray<SignatureHelpItem> items, int? selectedItemIndex)
        {
            ApplicableSpan = applicableSpan;
            Items = items;
            SelectedItemIndex = selectedItemIndex;
        }
    }

    public interface ISignatureHelpProvider
    {
        bool IsTriggerCharacter(char ch);
        bool IsRetriggerCharacter(char ch);
        System.Threading.Tasks.Task<SignatureHelpItems> GetItemsAsync(Microsoft.CodeAnalysis.Document document, int position, SignatureHelpTriggerInfo triggerInfo, System.Threading.CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.Options
{
    public static class FeatureOnOffOptions
    {
        public const string FeatureName = "FeatureOnOffOptions";
        public static readonly PerLanguageOption<bool> AutoFormattingOnCloseBrace = new PerLanguageOption<bool>(FeatureName, nameof(AutoFormattingOnCloseBrace), defaultValue: false);
        public static readonly PerLanguageOption<bool> AutoFormattingOnSemicolon = new PerLanguageOption<bool>(FeatureName, nameof(AutoFormattingOnSemicolon), defaultValue: false);
        public static readonly PerLanguageOption<bool> AutoFormattingOnTyping = new PerLanguageOption<bool>(FeatureName, nameof(AutoFormattingOnTyping), defaultValue: false);
        public static readonly PerLanguageOption<bool> FormatOnPaste = new PerLanguageOption<bool>(FeatureName, nameof(FormatOnPaste), defaultValue: false);
    }

    public static class ServiceFeatureOnOffOptions
    {
        public const string FeatureName = "ServiceFeatureOnOffOptions";
        public static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = new PerLanguageOption<bool?>(FeatureName, nameof(ClosedFileDiagnostic), defaultValue: true);
    }

    public static class CompletionOptions
    {
        public const string FeatureName = "CompletionOptions";
        public static readonly PerLanguageOption<bool> ShowCompletionItemFilters = new PerLanguageOption<bool>(FeatureName, nameof(ShowCompletionItemFilters), defaultValue: false);
        public static readonly PerLanguageOption<bool?> ShowItemsFromUnimportedNamespaces = new PerLanguageOption<bool?>(FeatureName, nameof(ShowItemsFromUnimportedNamespaces), defaultValue: false);
        public static readonly PerLanguageOption<bool?> TriggerOnDeletion = new PerLanguageOption<bool?>(FeatureName, nameof(TriggerOnDeletion), defaultValue: false);
        public static readonly PerLanguageOption<bool> TriggerOnTypingLetters = new PerLanguageOption<bool>(FeatureName, nameof(TriggerOnTypingLetters), defaultValue: false);
        public static readonly PerLanguageOption<bool> HideAdvancedMembers = new PerLanguageOption<bool>(FeatureName, nameof(HideAdvancedMembers), defaultValue: false);
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
            DefinitionItem definitionItem,
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
    public interface ITextBufferSupportsFeatureService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        bool SupportsRefactorings(Microsoft.VisualStudio.Text.ITextBuffer textBuffer);
    }
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
    public interface IDocumentNavigationService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        bool TryNavigateToSpan(Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.DocumentId documentId, Microsoft.CodeAnalysis.Text.TextSpan textSpan, Microsoft.CodeAnalysis.Options.OptionSet options = null);
        bool TryNavigateToLineAndOffset(Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.DocumentId documentId, int lineNumber, int offset, Microsoft.CodeAnalysis.Options.OptionSet options = null);
        bool TryNavigateToPosition(Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.DocumentId documentId, int position, int virtualSpace, Microsoft.CodeAnalysis.Options.OptionSet options = null);
    }

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

namespace Microsoft.CodeAnalysis.Editor
{
    public interface IRefactorNotifyService
    {
        bool TryOnBeforeGlobalSymbolRenamed(
            Microsoft.CodeAnalysis.Workspace workspace,
            System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.DocumentId> changedDocumentIDs,
            Microsoft.CodeAnalysis.ISymbol symbol,
            string newName,
            bool throwOnFailure);

        bool TryOnAfterGlobalSymbolRenamed(
            Microsoft.CodeAnalysis.Workspace workspace,
            System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.DocumentId> changedDocumentIDs,
            Microsoft.CodeAnalysis.ISymbol symbol,
            string newName,
            bool throwOnFailure);
    }

    public interface ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        Microsoft.CodeAnalysis.CodeActions.CodeActionOperation CreateSymbolRenamedOperation(
            ISymbol symbol,
            string newName,
            Solution startingSolution,
            Solution updatedSolution);
    }
}

namespace Microsoft.CodeAnalysis.Editor.Commanding.Commands
{
    public class GoToImplementationCommandArgs
    {
        public GoToImplementationCommandArgs(
            object textView,
            object subjectBuffer)
        {
        }
    }
}

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    public class PreviewWorkspace : Microsoft.CodeAnalysis.Workspace
    {
        public PreviewWorkspace(Microsoft.CodeAnalysis.Host.HostServices hostServices)
            : base(hostServices, "Preview")
        {
        }
    }
}

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    public class TopLevelSuppressionCodeAction
    {
    }
}

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    public class CodeRefactoringAction
    {
        public Microsoft.CodeAnalysis.CodeActions.CodeAction action;
    }

    public class CodeRefactoring
    {
        public System.Collections.Generic.IEnumerable<CodeRefactoringAction> CodeActions { get; set; }
    }

    public interface ICodeRefactoringService
    {
        System.Threading.Tasks.Task<System.Collections.Immutable.ImmutableArray<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            Microsoft.CodeAnalysis.Text.TextSpan textSpan,
            System.Threading.CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class HostDiagnosticAnalyzerPackage
    {
        public HostDiagnosticAnalyzerPackage(
            string name,
            System.Collections.Immutable.ImmutableArray<string> analyzerFilePaths)
        {
        }
    }
}

namespace Microsoft.CodeAnalysis.ProjectManagement
{
    public interface IProjectManagementService
    {
    }
}

namespace Microsoft.CodeAnalysis.PickMembers
{
    public interface IPickMembersService
    {
        PickMembersResult PickMembers(
            string title,
            System.Collections.Immutable.ImmutableArray<ISymbol> members,
            System.Collections.Immutable.ImmutableArray<PickMembersOption> options);
    }

    public class PickMembersResult
    {
        public static readonly PickMembersResult Canceled = new PickMembersResult();

        public PickMembersResult(
            System.Collections.Immutable.ImmutableArray<ISymbol> members,
            System.Collections.Immutable.ImmutableArray<PickMembersOption> options)
        {
        }

        private PickMembersResult()
        {
        }
    }

    public class PickMembersOption
    {
        public string Title { get; set; }
        public bool Value { get; set; }
    }
}

namespace Microsoft.CodeAnalysis.GenerateType
{
    public class TypeKindOptions
    {
    }

    public class GenerateTypeDialogOptions
    {
        public bool IsPublicOnlyAccessibility { get; set; }
        public TypeKindOptions TypeKindOptions { get; set; }
    }

    public static class TypeKindOptionsHelper
    {
        public static bool IsClass(TypeKindOptions options) { return false; }
        public static bool IsEnum(TypeKindOptions options) { return false; }
        public static bool IsStructure(TypeKindOptions options) { return false; }
        public static bool IsInterface(TypeKindOptions options) { return false; }
        public static bool IsDelegate(TypeKindOptions options) { return false; }
    }

    public class GenerateTypeOptionsResult
    {
        public static readonly GenerateTypeOptionsResult Cancelled = new GenerateTypeOptionsResult();

        public GenerateTypeOptionsResult(
            Accessibility accessibility,
            TypeKind typeKind,
            string typeName,
            Project project,
            bool isNewFile,
            string newFileName,
            System.Collections.Generic.List<string> folders,
            string fullFilePath,
            Document newDocument,
            bool areFoldersValidIdentifiers,
            string defaultNamespace)
        {
        }

        private GenerateTypeOptionsResult()
        {
        }
    }
}

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    public class ExtractInterfaceOptionsResult
    {
        public enum ExtractLocation
        {
            SameFile,
            NewFile
        }

        public static readonly ExtractInterfaceOptionsResult Cancelled = new ExtractInterfaceOptionsResult();

        public ExtractInterfaceOptionsResult(
            bool isCancelled,
            System.Collections.Immutable.ImmutableArray<ISymbol> includedMembers,
            string interfaceName,
            string fileName,
            ExtractLocation location)
        {
        }

        private ExtractInterfaceOptionsResult()
        {
        }
    }
}

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    public class ParameterConfiguration
    {
        public IParameterSymbol ThisParameter { get; set; }
        public IParameterSymbol ParamsParameter { get; set; }
        public System.Collections.Immutable.ImmutableArray<IParameterSymbol> RemainingEditableParameters { get; set; }
        public System.Collections.Immutable.ImmutableArray<IParameterSymbol> ParametersWithoutDefaultValues { get; set; }

        public static ParameterConfiguration Create(
            System.Collections.Generic.IList<IParameterSymbol> parameters,
            bool tryToAddThisParameter,
            int defaultParameterIndex)
        {
            return null;
        }

        public System.Collections.Immutable.ImmutableArray<IParameterSymbol> ToListOfParameters()
        {
            return default(System.Collections.Immutable.ImmutableArray<IParameterSymbol>);
        }
    }

    public class SignatureChange
    {
        public SignatureChange(ParameterConfiguration originalConfiguration, ParameterConfiguration updatedConfiguration)
        {
        }
    }

    public class ChangeSignatureOptionsResult
    {
        public bool IsCancelled { get; set; }
        public SignatureChange UpdatedSignature { get; set; }
    }

    public interface IChangeSignatureOptionsService
    {
        ChangeSignatureOptionsResult GetChangeSignatureOptions(
            ISymbol symbol,
            ParameterConfiguration parameters,
            Microsoft.CodeAnalysis.Notification.INotificationService notificationService);
    }
}

namespace Microsoft.CodeAnalysis.FindUsages
{
    public class DefinitionItem
    {
        public System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.DocumentSpan> SourceSpans { get; set; }
    }

    public class SourceReferenceItem
    {
        public Microsoft.CodeAnalysis.DocumentSpan SourceSpan { get; set; }
        public bool IsWrittenTo { get; set; }
    }

    public interface IStreamingFindUsagesPresenter
    {
        FindUsagesContext StartSearch(string title, bool supportsReferences);
        FindUsagesContext StartSearchWithCustomColumns(string title, bool supportsReferences, bool includeContainingTypeAndMemberColumns, bool includeKindColumn);
        void ClearAll();
    }

    public abstract class FindUsagesContext
    {
        public virtual CancellationToken CancellationToken { get { return default(CancellationToken); } }

        public virtual System.Threading.Tasks.Task ReportMessageAsync(string message)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual System.Threading.Tasks.Task ReportProgressAsync(int current, int maximum)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual System.Threading.Tasks.Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual System.Threading.Tasks.Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual System.Threading.Tasks.Task OnStartedAsync()
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual System.Threading.Tasks.Task OnCompletedAsync()
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}

namespace Microsoft.VisualStudio.Imaging
{
    public static class MonoDevelopCompatibilityMarker
    {
    }
}

namespace Microsoft.VisualStudio.Imaging.Interop
{
    public static class MonoDevelopCompatibilityMarkerInterop
    {
    }
}

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    // Provides symbol-backed completion items to the CSharpBinding add-in.
    public static class SymbolCompletionItem
    {
        public static CompletionItem CreateWithSymbolId(
            string displayText,
            IEnumerable<ISymbol> symbols,
            CompletionItemRules rules,
            int position,
            System.Collections.Immutable.ImmutableDictionary<string, string> properties = null)
        {
            var props = properties ?? System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
            if (!props.ContainsKey("Text"))
                props = props.Add("Text", displayText ?? string.Empty);
            var tags = System.Collections.Immutable.ImmutableArray.Create("symbol");
            return new CompletionItem(displayText, properties: props, rules: rules, tags: tags);
        }

        public static string GetInsertionText(CompletionItem item)
        {
            string result;
            if (item != null && item.Properties != null && item.Properties.TryGetValue("Text", out result))
                return result;
            return item != null ? item.DisplayText : string.Empty;
        }

        public static System.Threading.Tasks.Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(CommonCompletionItem.GetDescription(item));
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    public static class CompletionUtilities
    {
        public static bool IsTriggerAfterSpaceOrStartOfWordCharacter(Microsoft.CodeAnalysis.Text.SourceText text, int position, Microsoft.CodeAnalysis.Options.OptionSet options)
        {
            if (text == null || position <= 0)
                return true;
            var previous = text[position - 1];
            return char.IsWhiteSpace(previous) || !char.IsLetterOrDigit(previous) && previous != '_';
        }

        public static bool IsStartingNewWord(Microsoft.CodeAnalysis.Text.SourceText text, int position)
        {
            if (text == null || position <= 0)
                return true;
            var previous = text[position - 1];
            return !char.IsLetterOrDigit(previous) && previous != '_';
        }
    }
}

namespace Microsoft.CodeAnalysis.Snippets
{
    public class SnippetInfo
    {
        public string Shortcut { get; }
        public string Title { get; }
        public string Description { get; }
        public string Group { get; }

        public SnippetInfo(string shortcut, string title, string description, string group)
        {
            Shortcut = shortcut;
            Title = title;
            Description = description;
            Group = group;
        }
    }

    public interface ISnippetInfoService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        IEnumerable<SnippetInfo> GetSnippetsIfAvailable();
        bool ShouldFormatSnippet(SnippetInfo snippetInfo);
        bool SnippetShortcutExists_NonBlocking(string shortcut);
    }
}

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    public enum HighlightSpanKind
    {
        Reference,
        WrittenReference,
        Definition
    }

    public class HighlightSpan
    {
        public Microsoft.CodeAnalysis.Text.TextSpan TextSpan { get; }
        public HighlightSpanKind Kind { get; }

        public HighlightSpan(Microsoft.CodeAnalysis.Text.TextSpan textSpan, HighlightSpanKind kind)
        {
            TextSpan = textSpan;
            Kind = kind;
        }
    }

    public class DocumentHighlights
    {
        public Microsoft.CodeAnalysis.Document Document { get; }
        public System.Collections.Immutable.ImmutableArray<HighlightSpan> HighlightSpans { get; }

        public DocumentHighlights(Microsoft.CodeAnalysis.Document document, System.Collections.Immutable.ImmutableArray<HighlightSpan> highlightSpans)
        {
            Document = document;
            HighlightSpans = highlightSpans;
        }
    }

    public interface IDocumentHighlightsService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        System.Threading.Tasks.Task<System.Collections.Immutable.ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(
            Microsoft.CodeAnalysis.Document document,
            int position,
            System.Collections.Immutable.ImmutableHashSet<Microsoft.CodeAnalysis.Document> documentsToSearch,
            System.Threading.CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    public interface ILanguageDebugInfoService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        System.Threading.Tasks.Task<DebugLocationInfo> GetLocationInfoAsync(Microsoft.CodeAnalysis.Document document, int position, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<DebugDataTipInfo> GetDataTipInfoAsync(Microsoft.CodeAnalysis.Document document, int position, System.Threading.CancellationToken cancellationToken);
    }

    public struct DebugLocationInfo
    {
        public string Name { get; }
        public int LineOffset { get; }

        public bool IsEmpty
        {
            get { return Name == null; }
        }

        public DebugLocationInfo(string name, int lineOffset)
        {
            Name = name;
            LineOffset = lineOffset;
        }
    }

    public struct DebugDataTipInfo
    {
        public Microsoft.CodeAnalysis.Text.TextSpan Span { get; }
        public string Text { get; }

        public bool IsEmpty
        {
            get { return Span.Length == 0 && Text == null; }
        }

        public bool IsDefault
        {
            get { return Span.Length == 0 && Span.Start == 0 && Text == null; }
        }

        public DebugDataTipInfo(Microsoft.CodeAnalysis.Text.TextSpan span, string text)
        {
            Span = span;
            Text = text;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    public static class BreakpointSpans
    {
        public static bool TryGetBreakpointSpan(Microsoft.CodeAnalysis.SyntaxTree tree, int position, System.Threading.CancellationToken cancellationToken, out Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            int length = tree != null ? tree.Length : 0;
            position = System.Math.Max(0, System.Math.Min(position, length));
            span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(position, position);
            return true;
        }
    }
}

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    public class UniqueNameGenerator
    {
        readonly SemanticModel semanticModel;

        public UniqueNameGenerator(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public string CreateUniqueMethodName(Microsoft.CodeAnalysis.SyntaxNode parent, string baseName)
        {
            return string.IsNullOrEmpty(baseName) ? "ExtractedMethod" : baseName;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    public class SemanticDocument
    {
        public Microsoft.CodeAnalysis.Document Document { get; }
        public SemanticModel SemanticModel { get; private set; }
        public Microsoft.CodeAnalysis.SyntaxNode Root { get; private set; }

        protected SemanticDocument(Microsoft.CodeAnalysis.Document document)
        {
            Document = document;
        }

        public static async System.Threading.Tasks.Task<SemanticDocument> CreateAsync(Microsoft.CodeAnalysis.Document document, System.Threading.CancellationToken cancellationToken)
        {
            if (document == null)
                return new SemanticDocument(null);
            return new SemanticDocument(document) {
                SemanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false),
                Root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
            };
        }
    }

    public abstract class SelectionResult
    {
        public abstract bool ContainsValidContext { get; }
    }

    public class CSharpSelectionResult : SelectionResult
    {
        public SemanticDocument SemanticDocument { get; }
        public Microsoft.CodeAnalysis.Text.TextSpan Span { get; }

        public CSharpSelectionResult(SemanticDocument semanticDocument, Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            SemanticDocument = semanticDocument;
            Span = span;
        }

        public override bool ContainsValidContext
        {
            get { return true; }
        }
    }

    public class CSharpSelectionValidator
    {
        readonly SemanticDocument semanticDocument;
        readonly Microsoft.CodeAnalysis.Text.TextSpan span;

        public CSharpSelectionValidator(SemanticDocument semanticDocument, Microsoft.CodeAnalysis.Text.TextSpan span, Microsoft.CodeAnalysis.Options.OptionSet options)
        {
            this.semanticDocument = semanticDocument;
            this.span = span;
        }

        public System.Threading.Tasks.Task<SelectionResult> GetValidSelectionAsync(System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult<SelectionResult>(new CSharpSelectionResult(semanticDocument, span));
        }
    }

    public class ExtractMethodResult
    {
        public Microsoft.CodeAnalysis.Document Document { get; set; }
        public Microsoft.CodeAnalysis.SyntaxNode MethodDeclarationNode { get; set; }
        public Microsoft.CodeAnalysis.SyntaxToken InvocationNameToken { get; set; }
    }

    public class CSharpMethodExtractor
    {
        readonly CSharpSelectionResult selectionResult;

        public CSharpMethodExtractor(CSharpSelectionResult selectionResult)
        {
            this.selectionResult = selectionResult;
        }

        public System.Threading.Tasks.Task<ExtractMethodResult> ExtractMethodAsync(System.Threading.CancellationToken cancellationToken)
        {
            Microsoft.CodeAnalysis.SyntaxNode node = null;
            Microsoft.CodeAnalysis.SyntaxToken token = default(Microsoft.CodeAnalysis.SyntaxToken);
            if (selectionResult != null && selectionResult.SemanticDocument != null && selectionResult.SemanticDocument.Root != null) {
                node = selectionResult.SemanticDocument.Root;
                token = node.GetFirstToken();
            }
            return System.Threading.Tasks.Task.FromResult(new ExtractMethodResult {
                Document = selectionResult != null ? selectionResult.SemanticDocument.Document : null,
                MethodDeclarationNode = node,
                InvocationNameToken = token
            });
        }
    }
}

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    public interface IRemoveUnnecessaryImportsService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        System.Threading.Tasks.Task<Microsoft.CodeAnalysis.Document> RemoveUnnecessaryImportsAsync(Microsoft.CodeAnalysis.Document document, System.Threading.CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    public interface IDocumentationCommentFormattingService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        string Format(string rawXmlText, Microsoft.CodeAnalysis.CompilationOptions compilationOptions, System.Threading.CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.Editor
{
    public static class ContentTypeNames
    {
        public const string CSharpContentType = "CSharp";
    }
}

namespace Microsoft.CodeAnalysis.Editor.Commanding.Commands
{
    public class SortAndRemoveUnnecessaryImportsCommandArgs
    {
        public object TextView { get; }
        public Microsoft.VisualStudio.Text.ITextBuffer SubjectBuffer { get; }

        public SortAndRemoveUnnecessaryImportsCommandArgs(object textView, Microsoft.VisualStudio.Text.ITextBuffer subjectBuffer)
        {
            TextView = textView;
            SubjectBuffer = subjectBuffer;
        }
    }
}

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    public static class ISymbolExtensions
    {
        public static string GetFullName(this ISymbol symbol)
        {
            if (symbol == null)
                return string.Empty;
            return symbol.ToDisplayString();
        }

        public static string ToNameDisplayString(this ISymbol symbol, int position = 0, SymbolDisplayFormat format = null, bool fullyQualify = false)
        {
            if (symbol == null)
                return string.Empty;
            return symbol.Name ?? string.Empty;
        }

        public static string ToTypeDisplayString(this ITypeSymbol symbol, int position = 0, SymbolDisplayFormat format = null, bool fullyQualify = false)
        {
            if (symbol == null)
                return string.Empty;
            return symbol.ToDisplayString();
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypesMD(this INamedTypeSymbol symbol)
        {
            if (symbol == null)
                yield break;
            for (var b = symbol.BaseType; b != null; b = b.BaseType)
                yield return b;
        }

        public static IEnumerable<TaggedText> GetDocumentationParts(
            this ISymbol symbol,
            SemanticModel semanticModel,
            int position,
            Microsoft.CodeAnalysis.DocumentationComments.IDocumentationCommentFormattingService formatter,
            System.Threading.CancellationToken cancellationToken)
        {
            var raw = symbol != null ? symbol.GetDocumentationCommentXml() : null;
            if (string.IsNullOrWhiteSpace(raw))
                return new TaggedText[0];
            var text = raw;
            if (formatter != null && semanticModel != null && semanticModel.Compilation != null) {
                var formatted = formatter.Format(raw, semanticModel.Compilation.Options, cancellationToken);
                if (!string.IsNullOrWhiteSpace(formatted))
                    text = formatted;
            }
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            var parts = new List<TaggedText>();
            foreach (var line in text.Split('\n')) {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                    parts.Add(new TaggedText(TextTags.Text, trimmed));
            }
            return parts;
        }
    }
}

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    public interface IDebugInfoProvider
    {
        System.Threading.Tasks.Task<DataTipInfo> GetDebugInfoAsync(Microsoft.VisualStudio.Text.SnapshotPoint snapshotPoint, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<DataTipInfo> GetDebugInfoAsync(Microsoft.VisualStudio.Text.SnapshotSpan snapshotSpan, System.Threading.CancellationToken cancellationToken);
    }

    public struct DataTipInfo
    {
        public readonly Microsoft.VisualStudio.Text.ITrackingSpan Span;
        public readonly string Text;

        public DataTipInfo(Microsoft.VisualStudio.Text.ITrackingSpan span, string text)
        {
            this.Span = span;
            this.Text = text;
        }

        public bool IsDefault
        {
            get { return Span == null && Text == null; }
        }
    }
}

namespace Microsoft.CodeAnalysis.LanguageServices
{
    public interface IContentTypeLanguageService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        Microsoft.VisualStudio.Utilities.IContentType GetDefaultContentType();
    }
}

namespace Microsoft.CodeAnalysis
{
    public interface ISymbolDisplayService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        System.Threading.Tasks.Task<System.Collections.Immutable.ImmutableDictionary<SymbolDescriptionGroups, System.Collections.Immutable.ImmutableArray<TaggedText>>> ToDescriptionGroupsAsync(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            System.Collections.Immutable.ImmutableArray<ISymbol> symbols,
            System.Threading.CancellationToken cancellationToken);
    }

    public enum SymbolDescriptionGroups
    {
        MainDescription,
        Documentation,
        Returns,
        PackageName,
        ExceptionTypes,
        StructuralTypes,
        UsageText,
        FormatFrom,
        FormatTo,
        SpecialTypes,
        SpillOverFormattedSymbols,
        AnonymousTypes,
        AwaitableUsageText,
        Exceptions,
        Captures
    }

    public static class DiagnosticCategory
    {
        public const string Compiler = "Compiler";
        public const string Build = "Build";
        public const string EditAndContinue = "Edit and Continue";
        public const string Style = "Style";
    }

    public static class MonoWorkspaceExtensions
    {
        public static void ApplyDocumentChanges(this Workspace workspace, Microsoft.CodeAnalysis.Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (workspace == null || document == null)
                return;
            workspace.TryApplyChanges(document.Project.Solution);
        }
    }
}

namespace Microsoft.CodeAnalysis.Text
{
    public static class MonoRoslynTextExtensions
    {
        public static Microsoft.CodeAnalysis.Document GetOpenDocumentInCurrentContextWithChanges(this Microsoft.VisualStudio.Text.ITextSnapshot snapshot)
        {
            return null;
        }

        public static Microsoft.CodeAnalysis.Text.SourceText AsText(this Microsoft.VisualStudio.Text.ITextSnapshot snapshot)
        {
            if (snapshot == null)
                return null;
            return Microsoft.CodeAnalysis.Text.SourceText.From(snapshot.GetText());
        }
    }
}

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    public static class CompatibilityTextExtensions
    {
        public static Microsoft.VisualStudio.Text.ITextSnapshot ApplyAndLogExceptions(this Microsoft.VisualStudio.Text.ITextEdit edit)
        {
            if (edit == null)
                return null;
            return edit.Apply();
        }
    }
}

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    public static class CommonCompletionUtilities
    {
        public static bool TryRemoveAttributeSuffix(ISymbol typeSymbol, object syntaxContext, out string name)
        {
            name = typeSymbol != null ? typeSymbol.Name : null;
            if (name != null && name.EndsWith("Attribute", System.StringComparison.Ordinal) && name.Length > "Attribute".Length)
                name = name.Substring(0, name.Length - "Attribute".Length);
            return !string.IsNullOrEmpty(name);
        }

        public static System.Threading.Tasks.Task<Microsoft.CodeAnalysis.Completion.CompletionDescription> CreateDescriptionAsync(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            ISymbol[] symbols,
            Microsoft.CodeAnalysis.SyntaxAnnotation syntaxAnnotation,
            System.Threading.CancellationToken cancellationToken)
        {
            var builder = new System.Text.StringBuilder();
            if (symbols != null) {
                foreach (var symbol in symbols) {
                    if (symbol == null)
                        continue;
                    var typeSymbol = symbol as INamedTypeSymbol;
                    var kind = typeSymbol != null ? typeSymbol.ToDisplayString() + " " : "";
                    builder.AppendLine((kind + symbol.ToDisplayString()).Trim());
                }
            }
            return System.Threading.Tasks.Task.FromResult(new Microsoft.CodeAnalysis.Completion.CompletionDescription { Text = builder.ToString().Trim() });
        }
    }
}

namespace Microsoft.CodeAnalysis.Formatting
{
    public interface IEditorFormattingService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        bool SupportsFormatOnReturn { get; }
        bool SupportsFormatOnPaste { get; }
        bool SupportsFormatSelection { get; }
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesAsync(Microsoft.CodeAnalysis.Document document, Microsoft.CodeAnalysis.Text.TextSpan? span, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesAsync(Microsoft.CodeAnalysis.Document document, char typedChar, int position, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesOnReturnAsync(Microsoft.CodeAnalysis.Document document, int position, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Text.TextChange>> GetFormattingChangesOnPasteAsync(Microsoft.CodeAnalysis.Document document, Microsoft.CodeAnalysis.Text.TextSpan span, System.Threading.CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    public class OverrideCompletionProvider
    {
    }
}
