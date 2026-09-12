using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

    public static class CompatSymbolExtensions
    {
        public static TLanguageService GetLanguageService<TLanguageService>(this Microsoft.CodeAnalysis.Document document)
            where TLanguageService : class, Microsoft.CodeAnalysis.Host.ILanguageService
        {
            return document.Project.LanguageServices.GetService<TLanguageService>();
        }

        public static System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.INamedTypeSymbol> GetAllTypes(this Microsoft.CodeAnalysis.INamespaceSymbol namespaceSymbol)
        {
            return GetAllTypes(namespaceSymbol, System.Threading.CancellationToken.None);
        }

        public static System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.INamedTypeSymbol> GetAllTypes(this Microsoft.CodeAnalysis.INamespaceSymbol namespaceSymbol, System.Threading.CancellationToken cancellationToken)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return type;
            }

            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var type in GetAllTypes(childNamespace, cancellationToken))
                    yield return type;
            }
        }

        public static System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.INamedTypeSymbol> GetBaseTypes(this Microsoft.CodeAnalysis.ITypeSymbol type)
        {
            for (var current = type != null ? (Microsoft.CodeAnalysis.INamedTypeSymbol)type.BaseType : null; current != null; current = (Microsoft.CodeAnalysis.INamedTypeSymbol)current.BaseType)
                yield return current;
        }

        public static bool IsAccessibleWithin(this Microsoft.CodeAnalysis.ISymbol symbol, Microsoft.CodeAnalysis.ISymbol within, Microsoft.CodeAnalysis.ITypeSymbol throughTypeOpt = null)
        {
            if (symbol == null || within == null)
                return true;

            switch (symbol.DeclaredAccessibility)
            {
                case Microsoft.CodeAnalysis.Accessibility.Public:
                case Microsoft.CodeAnalysis.Accessibility.NotApplicable:
                    return true;
                case Microsoft.CodeAnalysis.Accessibility.Private:
                    return IsWithinTypeOrNested(symbol.ContainingType, within);
                case Microsoft.CodeAnalysis.Accessibility.Internal:
                    return symbol.ContainingAssembly == within.ContainingAssembly;
                case Microsoft.CodeAnalysis.Accessibility.Protected:
                case Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal:
                    return IsWithinContainingTypeOrDerived(symbol.ContainingType, within);
                case Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal:
                    return symbol.ContainingAssembly == within.ContainingAssembly ||
                           IsWithinContainingTypeOrDerived(symbol.ContainingType, within);
                default:
                    return true;
            }
        }

        private static bool IsWithinTypeOrNested(Microsoft.CodeAnalysis.ITypeSymbol containingType, Microsoft.CodeAnalysis.ISymbol within)
        {
            if (containingType == null)
                return false;

            for (var type = (within as Microsoft.CodeAnalysis.ITypeSymbol) ?? within.ContainingType; type != null; type = type.ContainingType)
            {
                if (type.OriginalDefinition == containingType.OriginalDefinition)
                    return true;
            }

            return false;
        }

        private static bool IsWithinContainingTypeOrDerived(Microsoft.CodeAnalysis.ITypeSymbol containingType, Microsoft.CodeAnalysis.ISymbol within)
        {
            if (containingType == null)
                return true;

            if (IsWithinTypeOrNested(containingType, within))
                return true;

            for (var type = (within as Microsoft.CodeAnalysis.ITypeSymbol) ?? within.ContainingType; type != null; type = type.BaseType)
            {
                if (type.OriginalDefinition == containingType.OriginalDefinition)
                    return true;
            }

            return false;
        }
    }
}

namespace Microsoft.CodeAnalysis.Notification
{
    public enum NotificationSeverity { Information, Warning, Error }
    public interface INotificationService : Microsoft.CodeAnalysis.Host.IWorkspaceService { }
    public interface INotificationServiceCallback : Microsoft.CodeAnalysis.Host.IWorkspaceService { }
    public interface IForegroundNotificationService { }
    public interface IGlobalOperationNotificationService
    {
    }
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
    public class ExtensionManager : Microsoft.CodeAnalysis.Host.IWorkspaceService, Microsoft.CodeAnalysis.Extensions.IExtensionManager
    {
        public ExtensionManager() { }
        public virtual void HandleException(object provider, Exception exception) { }
        public virtual bool IsDisabled(object provider) => false;
        public virtual bool CanHandleException(object provider, Exception exception) => true;
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

    public static class CompatTextExtensions
    {
        public static Microsoft.CodeAnalysis.Document GetOpenDocumentInCurrentContext(this SourceTextContainer container, Microsoft.CodeAnalysis.Workspace workspace = null)
        {
            if (container == null || workspace == null)
                return null;
            return workspace.CurrentSolution.GetDocument(workspace.GetDocumentIdInCurrentContext(container));
        }
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

    public static class Contract
    {
        public static void ThrowIfNull(object value, string message = null)
        {
            if (value == null)
                throw new ArgumentNullException(message);
        }

        public static void ThrowIfFalse(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException();
        }

        public static void Requires(bool condition, string message = null)
        {
            if (!condition)
                throw new ArgumentException(message);
        }

        public static void Fail(string message = null)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static class Hash
    {
        public static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * -1521134295) + newKey);
        }

        public static int Combine(bool newKey, int currentKey)
        {
            return Combine(newKey ? 1 : 0, currentKey);
        }
    }

    public static class PathUtilities
    {
        public static bool IsAbsolute(string path)
        {
            return System.IO.Path.IsPathRooted(path);
        }

        public static string CombineAbsoluteAndRelativePaths(string root, string relativePath)
        {
            if (root == null || relativePath == null)
                return null;
            return System.IO.Path.Combine(root, relativePath);
        }
    }

    public static class FileUtilities
    {
        public static DateTime GetFileTimeStamp(string fileName)
        {
            try
            {
                return System.IO.File.GetLastWriteTimeUtc(fileName);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static string NormalizeDirectoryPath(string path)
        {
            if (path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                return path;
            return path + System.IO.Path.DirectorySeparatorChar;
        }

        public static System.IO.Stream OpenRead(string filePath)
        {
            return new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        }
    }

    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this System.IO.Stream stream)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public interface ISupportDirectMemoryAccess
    {
        IntPtr GetPointer();
    }
}

namespace Microsoft.CodeAnalysis
{
    public static class ImmutableArrayExtensions
    {
        public static System.Collections.Immutable.ImmutableArray<T> WhereAsArray<T>(
            this System.Collections.Immutable.ImmutableArray<T> array,
            Func<T, bool> predicate)
        {
            var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                    builder.Add(array[i]);
            }
            return builder.MoveToImmutable();
        }

        public static System.Collections.Immutable.ImmutableArray<TResult> SelectAsArray<T, TResult>(
            this System.Collections.Immutable.ImmutableArray<T> array,
            Func<T, TResult> selector)
        {
            var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<TResult>(array.Length);
            for (int i = 0; i < array.Length; i++)
                builder.Add(selector(array[i]));
            return builder.MoveToImmutable();
        }

        public static System.Collections.Immutable.ImmutableArray<T> AsImmutableOrEmpty<T>(
            this System.Collections.Generic.IEnumerable<T> items)
        {
            if (items == null)
                return default(System.Collections.Immutable.ImmutableArray<T>);
            return System.Collections.Immutable.ImmutableArray.CreateRange<T>(items);
        }
    }

    public static class SerializableBytes
    {
        public static System.IO.Stream CreateWritableStream()
        {
            return new System.IO.MemoryStream();
        }
    }

    public static class RoslynCompatExtensions
    {
        public static Microsoft.CodeAnalysis.DocumentId GetDocumentIdInCurrentContext(this Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.DocumentId documentId)
        {
            return documentId;
        }

        public static Microsoft.CodeAnalysis.Text.SourceText GetTextSynchronously(this Microsoft.CodeAnalysis.Document document, System.Threading.CancellationToken cancellationToken)
        {
            return document.GetTextAsync(cancellationToken).GetAwaiter().GetResult();
        }

        public static SyntaxToken FindTokenOnLeftOfPosition(this SyntaxNode root, int position, CancellationToken cancellationToken = default(CancellationToken), bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            SyntaxTree tree = root.SyntaxTree;
            if (tree != null)
                return tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeSkipped, includeDirectives, includeDocumentationComments);
            return FindTokenOnLeftOfPositionCore(root, position, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        public static SyntaxToken FindTokenOnLeftOfPosition(this SyntaxTree tree, int position, CancellationToken cancellationToken = default(CancellationToken), bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            return FindTokenOnLeftOfPositionCore(tree.GetRoot(cancellationToken), position, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        static SyntaxToken FindTokenOnLeftOfPositionCore(SyntaxNode root, int position, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            SyntaxToken token = root.FindToken(position, findInsideTrivia: includeSkipped || includeDirectives || includeDocumentationComments);
            if (token.RawKind != 0) {
                if (token.Span.End <= position)
                    return token;
                if (token.SpanStart > position)
                    return token.GetPreviousToken();
                return token;
            }
            return default(SyntaxToken);
        }

        public static SyntaxToken GetNextToken(this SyntaxToken syntaxToken, bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            if (syntaxToken.Parent == null)
                return default(SyntaxToken);
            SyntaxTree tree = syntaxToken.Parent.SyntaxTree;
            if (tree == null)
                return default(SyntaxToken);
            SyntaxNode root = tree.GetRoot();
            int position = syntaxToken.Span.End;
            SyntaxToken result = default(SyntaxToken);
            while (position < root.FullSpan.End) {
                result = root.FindToken(position, findInsideTrivia: includeDirectives || includeSkipped || includeDocumentationComments);
                if (result.RawKind != 0 && (result.SpanStart > syntaxToken.Span.End || (includeZeroWidth && result.SpanStart == syntaxToken.Span.End && result != syntaxToken))) {
                    if (!includeSkipped && result.IsKind(SyntaxKind.SkippedTokensTrivia)) {
                        position = result.Span.End;
                        continue;
                    }
                    return result;
                }
                position++;
            }
            return default(SyntaxToken);
        }

        public static SyntaxToken GetPreviousToken(this SyntaxToken syntaxToken, bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false)
        {
            if (syntaxToken.Parent == null)
                return default(SyntaxToken);
            SyntaxTree tree = syntaxToken.Parent.SyntaxTree;
            if (tree == null)
                return default(SyntaxToken);
            SyntaxNode root = tree.GetRoot();
            int position = syntaxToken.SpanStart - 1;
            SyntaxToken result = default(SyntaxToken);
            while (position >= 0) {
                result = root.FindToken(position, findInsideTrivia: includeDirectives || includeSkipped);
                if (result.RawKind != 0 && (result.Span.End <= syntaxToken.SpanStart || includeZeroWidth))
                    return result;
                position--;
            }
            return default(SyntaxToken);
        }

        public static string GetText(this SyntaxKind kind)
        {
            return Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetText(kind);
        }

        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.IsKind(kind) || string.Equals(token.Text, Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetText(kind), StringComparison.Ordinal);
        }

        public static Task<SemanticModel> GetSemanticModelForSpanAsync(this Document document, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSemanticModelAsync(cancellationToken);
        }

        public static Task<SemanticModel> GetSemanticModelForNodeAsync(this Document document, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSemanticModelAsync(cancellationToken);
        }

        public static Task<SemanticModel> GetPartialSemanticModelAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSemanticModelAsync(cancellationToken);
        }

        public static SyntaxNode GetSyntaxRootSynchronously(this Document document, CancellationToken cancellationToken)
        {
            return document.GetSyntaxRootAsync(cancellationToken).GetAwaiter().GetResult();
        }

        public static SyntaxTree GetSyntaxTreeSynchronously(this Document document, CancellationToken cancellationToken)
        {
            return document.GetSyntaxTreeAsync(cancellationToken).GetAwaiter().GetResult();
        }

        public static Task<bool> IsForkedDocumentWithSyntaxChangesAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        public static IEnumerable<Document> GetRelatedDocuments(this SourceTextContainer textContainer)
        {
            var workspace = Workspace.GetWorkspaceRegistration(textContainer).Workspace;
            if (workspace == null)
                return Enumerable.Empty<Document>();
            var container = textContainer;
            return workspace.CurrentSolution.Projects.SelectMany(p => p.Documents).Where(d => container.Equals(d.GetSyntaxTreeSynchronously(default(CancellationToken))?.GetText(default(CancellationToken))?.Container)).ToArray();
        }

        public static bool IsRightSideOfDotOrArrow(this ExpressionSyntax expression)
        {
            var memberAccess = expression.Parent as MemberAccessExpressionSyntax;
            return memberAccess != null && memberAccess.Name == expression;
        }

        public static bool IsRightOfDotOrArrowOrColonColon(this SyntaxTree tree, int position, SyntaxToken tokenLeftOfPosition, CancellationToken cancellationToken = default(CancellationToken))
        {
            return tokenLeftOfPosition.IsKind(SyntaxKind.DotToken) || tokenLeftOfPosition.IsKind(SyntaxKind.MinusGreaterThanToken) || tokenLeftOfPosition.IsKind(SyntaxKind.ColonColonToken);
        }

        public static ExpressionSyntax GetParentConditionalAccessExpression(this ExpressionSyntax expression)
        {
            return expression.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>();
        }

        public static bool IsAnyLiteralExpression(this ExpressionSyntax expression)
        {
            switch (expression.Kind()) {
            case SyntaxKind.NumericLiteralExpression:
            case SyntaxKind.StringLiteralExpression:
            case SyntaxKind.CharacterLiteralExpression:
            case SyntaxKind.TrueLiteralExpression:
            case SyntaxKind.FalseLiteralExpression:
            case SyntaxKind.NullLiteralExpression:
            case SyntaxKind.DefaultLiteralExpression:
                return true;
            }
            return false;
        }

        public static bool IsPreProcessorDirectiveContext(this SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var text = tree.GetText(cancellationToken);
            var line = text.Lines.GetLineFromPosition(position);
            var lineText = line.ToString();
            var offset = Math.Min(position - line.Start, lineText.Length);
            return lineText.Substring(0, offset).TrimStart().StartsWith("#", StringComparison.Ordinal);
        }

        public static bool IsInNonUserCode(this SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            return tree.IsPreProcessorDirectiveContext(position, cancellationToken) || IsInGeneratedCode(tree, position, cancellationToken);
        }

        static bool IsInGeneratedCode(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var text = tree.GetText(cancellationToken);
            var line = text.Lines.GetLineFromPosition(position);
            return line.ToString().Trim().StartsWith("//", StringComparison.Ordinal);
        }

        public static bool IsGlobalStatementContext(this SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.Parent != null && token.Parent is GlobalStatementSyntax;
        }

        public static bool IsExpressionContext(this SyntaxTree tree, int position, SyntaxToken tokenLeftOfPosition, bool allowDeconstruction = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return false;
        }

        public static bool IsStatementContext(this SyntaxTree tree, int position, SyntaxToken tokenLeftOfPosition, CancellationToken cancellationToken = default(CancellationToken))
        {
            return false;
        }

        public static bool IsTypeContext(this SyntaxTree tree, int position, CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.Parent != null && token.Parent.AncestorsAndSelf().Any(n => n is BaseTypeDeclarationSyntax);
        }

        public static bool IsTypeDeclarationContext(this SyntaxTree tree, int position, SyntaxToken tokenLeftOfPosition, CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.Parent != null && token.Parent.AncestorsAndSelf().Any(n => n is BaseTypeDeclarationSyntax);
        }

        public static bool IsLabelContext(this SyntaxTree tree, int position, CancellationToken cancellationToken = default(CancellationToken))
        {
            return false;
        }

        public static BaseTypeDeclarationSyntax GetContainingTypeOrEnumDeclaration(this SyntaxTree tree, int position, CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.Parent?.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        }

        public static TypeDeclarationSyntax GetContainingTypeDeclaration(this SyntaxTree tree, int offset, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(offset, cancellationToken);
            return token.Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        }

        public static IEnumerable<INamespaceSymbol> GetUsingNamespacesInScope(this SemanticModel semanticModel, SyntaxNode node = null)
        {
            return semanticModel.Compilation.GlobalNamespace.GetNamespaceMembers().Cast<INamespaceSymbol>();
        }

        public static INamespaceSymbol GetEnclosingNamespace(this SemanticModel semanticModel, int position, CancellationToken cancellationToken = default(CancellationToken))
        {
            var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
            while (symbol != null) {
                if (symbol is INamespaceSymbol ns)
                    return ns;
                symbol = symbol.ContainingSymbol;
            }
            return semanticModel.Compilation.GlobalNamespace;
        }

        public static bool IsDocComment(this SyntaxTrivia trivia)
        {
            return trivia.ToString().StartsWith("///", StringComparison.Ordinal);
        }

        public static bool IsElastic(this SyntaxTrivia trivia)
        {
            return false;
        }

        public static bool IsSkippedTokensTrivia(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(SyntaxKind.SkippedTokensTrivia);
        }

        public static SyntaxTrivia GetMatchingDirective(this SyntaxTrivia trivia, CancellationToken cancellationToken = default(CancellationToken))
        {
            return default(SyntaxTrivia);
        }

        public static NamespaceDeclarationSyntax GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
        {
            return contextNode.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
        }

        public static SyntaxList<T> ToSyntaxList<T>(this IEnumerable<T> nodes) where T : SyntaxNode
        {
            return SyntaxFactory.List<T>(nodes);
        }

        public static bool IsSorted<T>(this SyntaxList<T> list, IComparer<T> comparer = null) where T : SyntaxNode
        {
            var c = comparer ?? Comparer<T>.Default;
            for (int i = 1; i < list.Count; i++) {
                if (c.Compare(list[i - 1], list[i]) > 0)
                    return false;
            }
            return true;
        }

        public static bool OverlapsHiddenPosition(this SyntaxTree tree, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return false;
        }

        public static SyntaxToken GetPreviousTokenIfTouchingWord(this SyntaxToken token, int position)
        {
            if (token.Span.End == position)
                return token;
            return token.GetPreviousToken();
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
                return method.Parameters;
            if (symbol is INamedTypeSymbol named && named.DelegateInvokeMethod != null)
                return named.DelegateInvokeMethod.Parameters;
            return ImmutableArray<IParameterSymbol>.Empty;
        }

        public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol named && named.IsGenericType && !named.ConstructedFrom.Equals(named))
                return named.TypeArguments;
            if (symbol is IMethodSymbol method && method.IsGenericMethod)
                return method.TypeArguments.Cast<ITypeSymbol>().ToImmutableArray();
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        public static bool IsOrdinaryMethod(this ISymbol symbol)
        {
            return symbol is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary;
        }

        public static bool IsConstructor(this ISymbol symbol)
        {
            var method = symbol as IMethodSymbol;
            return method != null && (method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor);
        }

        public static bool IsAttribute(this ISymbol symbol)
        {
            var type = symbol as INamedTypeSymbol;
            if (type == null)
                return false;
            for (var current = type.BaseType; current != null; current = current.BaseType) {
                if (current.ToDisplayString() == "System.Attribute")
                    return true;
            }
            return type.ToDisplayString() == "System.Attribute";
        }

        public static ISymbol OverriddenMember(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
                return method.OverriddenMethod;
            if (symbol is IPropertySymbol property)
                return property.OverriddenProperty;
            if (symbol is IEventSymbol @event)
                return @event.OverriddenEvent;
            return null;
        }

        public static bool IsReducedExtension(this IMethodSymbol method)
        {
            return method.ReducedFrom != null;
        }

        public static bool IsNullable(this ITypeSymbol symbol)
        {
            if (symbol != null && symbol.TypeKind != TypeKind.Error)
                return symbol.IsReferenceType;
            return false;
        }

        public static bool InheritsFromOrImplementsOrEqualsIgnoringConstruction(this INamedTypeSymbol type, INamedTypeSymbol other)
        {
            if (type == null || other == null)
                return false;
            if (type.Equals(other))
                return true;
            for (var current = type.BaseType; current != null; current = current.BaseType) {
                if (current.OriginalDefinition.Equals(other.OriginalDefinition) || current.Equals(other))
                    return true;
            }
            return type.AllInterfaces.Any(i => i.OriginalDefinition.Equals(other.OriginalDefinition) || i.Equals(other));
        }

        public static string ToSignatureDisplayString(this ISymbol symbol, SymbolDisplayFormat format = null)
        {
            return symbol.ToDisplayString(format ?? SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        public static TypeSyntax GenerateTypeSyntax(this ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol)
                return SyntaxFactory.ParseTypeName(type.Name);
            if (type is INamedTypeSymbol named && named.TypeArguments.Length > 0) {
                var args = SyntaxFactory.SeparatedList(named.TypeArguments.Select(a => a.GenerateTypeSyntax()));
                return SyntaxFactory.GenericName(SyntaxFactory.Identifier(named.Name), SyntaxFactory.TypeArgumentList(args));
            }
            return SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        public static Task<ISymbol> FindApplicableAlias(this ITypeSymbol type, int positionInDocument, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return Task.FromResult<ISymbol>(null);
        }

        public static int GetColumnFromLineOffset(this string text, int offset, int tabSize)
        {
            int column = 0;
            int bound = Math.Max(0, Math.Min(offset, text.Length));
            for (int i = 0; i < bound; i++) {
                if (text[i] == '\t')
                    column = column / tabSize * tabSize + tabSize;
                else
                    column++;
            }
            return column;
        }

        public static string GetFirstLineText(this string text)
        {
            var newLineIndex = text.IndexOf('\n');
            if (newLineIndex < 0)
                return text;
            return text.Substring(0, newLineIndex);
        }

        public static string GetLastLineText(this string text)
        {
            var newLineIndex = text.LastIndexOf('\n');
            if (newLineIndex < 0)
                return text;
            return text.Substring(newLineIndex + 1);
        }

        public static ImmutableArray<T> AsImmutable<T>(this IEnumerable<T> items)
        {
            return items.ToImmutableArray();
        }

        public static T GetAncestor<T>(this SyntaxNode node) where T : SyntaxNode
        {
            return node.Ancestors().OfType<T>().FirstOrDefault();
        }

        public static T GetAncestorOrThis<T>(this SyntaxNode node) where T : SyntaxNode
        {
            return node.AncestorsAndSelf().OfType<T>().FirstOrDefault();
        }

        public static bool IsWrittenTo(this SyntaxNode node)
        {
            return node.Parent is AssignmentExpressionSyntax assign && assign.Left == node;
        }

        public static bool IsOnlyWrittenTo(this SyntaxNode node)
        {
            return IsWrittenTo(node);
        }
    }
}

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public class ArrayBuilder<T> : System.Collections.Generic.IEnumerable<T>
    {
        private readonly List<T> _items;

        private ArrayBuilder(int capacity)
        {
            _items = new List<T>(capacity);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public T this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void AddRange(params T[] items)
        {
            _items.AddRange(items);
        }

        public System.Collections.Immutable.ImmutableArray<T> ToImmutable()
        {
            return System.Collections.Immutable.ImmutableArray.CreateRange(_items);
        }

        public T[] ToArrayAndFree()
        {
            var result = _items.ToArray();
            Clear();
            return result;
        }

        public System.Collections.Immutable.ImmutableArray<T> ToImmutableAndFree()
        {
            var result = ToImmutable();
            Clear();
            return result;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public static ArrayBuilder<T> GetInstance(int capacity = 0)
        {
            return new ArrayBuilder<T>(capacity);
        }
    }

    public class PooledObject<T> : IDisposable
    {
        public T Object { get; private set; }

        public PooledObject(T obj)
        {
            Object = obj;
        }

        public void Free()
        {
        }

        public void Dispose()
        {
        }
    }

    public class ObjectPool<T> : IDisposable where T : class
    {
        public T Allocate()
        {
            return Activator.CreateInstance<T>();
        }

        public T AllocateAndClear()
        {
            return Allocate();
        }

        public void ClearAndFree(T element)
        {
        }

        public void ForgetTrackedObject(T element)
        {
        }

        public PooledObject<T> GetPooledObject()
        {
            return new PooledObject<T>(Allocate());
        }

        public void Free(T element)
        {
        }

        public T Object
        {
            get { return Allocate(); }
        }

        public void Dispose()
        {
        }
    }
}

namespace Microsoft.CodeAnalysis
{
    public static class SharedPools
    {
        private static readonly object s_gate = new object();
        private static readonly Dictionary<Type, object> s_pools = new Dictionary<Type, object>();

        public static Microsoft.CodeAnalysis.PooledObjects.ObjectPool<byte[]> ByteArray = new Microsoft.CodeAnalysis.PooledObjects.ObjectPool<byte[]>();

        public static Microsoft.CodeAnalysis.PooledObjects.ObjectPool<T> Default<T>() where T : class
        {
            lock (s_gate)
            {
                object result;
                if (!s_pools.TryGetValue(typeof(T), out result))
                {
                    result = new Microsoft.CodeAnalysis.PooledObjects.ObjectPool<T>();
                    s_pools.Add(typeof(T), result);
                }
                return (Microsoft.CodeAnalysis.PooledObjects.ObjectPool<T>)result;
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.Formatting
{
    public static class StringBuilderPool
    {
        public static System.Text.StringBuilder Allocate()
        {
            return new System.Text.StringBuilder();
        }

        public static string ReturnAndFree(System.Text.StringBuilder sb)
        {
            var result = sb.ToString();
            sb.Clear();
            return result;
        }
    }
}

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    public static class CommonFormattingHelpers
    {
        public static Microsoft.CodeAnalysis.Text.TextSpan GetFormattingSpan(Microsoft.CodeAnalysis.SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            return span;
        }
    }
}

namespace Roslyn.Utilities
{
    public static class ExceptionUtilities
    {
        public static System.Exception Unreachable
        {
            get { return new System.NotImplementedException(); }
        }
    }
}

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    public static class FatalError
    {
        public static Action<Exception> Handler { get; set; }

        public static Action<Exception> NonFatalHandler { get; set; }

        public static bool ReportWithoutCrashUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
                return true;
            try
            {
                NonFatalHandler?.Invoke(exception);
            }
            catch
            {
            }
            return true;
        }
    }
}

namespace Microsoft.CodeAnalysis.Internal.Log
{
    public enum FunctionId
    {
        Unknown,
        VirtualMemory_MemoryLow,
        BKTree_ExceptionInCacheRead,
        StorageDatabase_Exceptions,
        SymbolTreeInfo_ExceptionInCacheRead,
        Extension_Exception,
    }

    public interface ILogger
    {
        bool IsEnabled(FunctionId functionId);
        void Log(FunctionId functionId, LogMessage logMessage);
        void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, System.Threading.CancellationToken cancellationToken);
        void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, System.Threading.CancellationToken cancellationToken);
    }

    public abstract class LogMessage
    {
        public abstract string GetMessage();
    }

    public sealed class KeyValueLogMessage : LogMessage
    {
        public static KeyValueLogMessage Create(Action<System.Collections.Generic.Dictionary<string, object>> action)
        {
            if (action != null)
                action(new System.Collections.Generic.Dictionary<string, object>());
            return new KeyValueLogMessage();
        }

        public override string GetMessage()
        {
            return string.Empty;
        }
    }

    public sealed class AggregateLogger : ILogger
    {
        public static AggregateLogger Create(System.Collections.Generic.IEnumerable<ILogger> loggers)
        {
            return new AggregateLogger();
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return false;
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, System.Threading.CancellationToken cancellationToken)
        {
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, System.Threading.CancellationToken cancellationToken)
        {
        }
    }

    public static class Logger
    {
        public static void Log(FunctionId functionId, LogMessage logMessage)
        {
        }

        public static void SetLogger(AggregateLogger logger)
        {
        }

        public static ILogger GetLogger()
        {
            return null;
        }
    }
}

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    public class FeatureAttribute : Attribute
    {
        public static readonly FeatureAttribute InfoBar = new FeatureAttribute("InfoBar");

        public string FeatureName { get; private set; }

        public FeatureAttribute(string featureName)
        {
            FeatureName = featureName;
        }
    }
}

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    public class ProgressTracker : Microsoft.CodeAnalysis.Shared.Utilities.IProgressTracker
    {
    }

    public class SignatureComparer
    {
        public static readonly SignatureComparer Instance = new SignatureComparer();

        public static readonly SignatureComparer Ordinal = new SignatureComparer();

        public bool HaveSameSignature(Microsoft.CodeAnalysis.ISymbol symbol1, Microsoft.CodeAnalysis.ISymbol symbol2, bool caseSensitive)
        {
            return object.Equals(symbol1, symbol2);
        }

        public bool Equals(Microsoft.CodeAnalysis.ISymbol symbol1, Microsoft.CodeAnalysis.ISymbol symbol2)
        {
            return object.Equals(symbol1, symbol2);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class NameGenerator
    {
        public static string GenerateUniqueName(string baseName, string extension, Func<string, bool> contains)
        {
            var name = string.Concat(baseName, extension);
            var index = 1;
            while (contains(name))
                name = string.Concat(baseName, index++.ToString(), extension);
            return name;
        }
    }
}

namespace Microsoft.CodeAnalysis.Options
{
    public class RoamingProfileStorageLocation : OptionStorageLocation
    {
        public string KeyName { get; private set; }

        public RoamingProfileStorageLocation(string keyName)
        {
            KeyName = keyName;
        }

        public string GetKeyNameForLanguage(string language)
        {
            return KeyName + language;
        }
    }

    public class LocalUserProfileStorageLocation : OptionStorageLocation
    {
        public string KeyName { get; private set; }

        public LocalUserProfileStorageLocation(string keyName)
        {
            KeyName = keyName;
        }
    }
}

namespace Microsoft.CodeAnalysis.CodeStyle
{
    public interface ICodeStyleOption
    {
        System.Xml.Linq.XElement ToXElement();
    }

    public class NamingStylePreferences
    {
        public System.Xml.Linq.XElement CreateXElement()
        {
            return new System.Xml.Linq.XElement("NamingStylePreferences");
        }

        public static NamingStylePreferences FromXElement(System.Xml.Linq.XElement element)
        {
            return new NamingStylePreferences();
        }
    }

    public enum PreferBracesPreference
    {
        None = 0,
        Always = 1,
        WhenMultiline = 2,
    }

    public enum ExpressionBodyPreference
    {
        Never = 0,
        WhenPossible = 1,
        WhenOnSingleLine = 2,
    }
}

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    public static class CSharpCodeStyleOptions
    {
        private static readonly Microsoft.CodeAnalysis.CodeStyle.NotificationOption Silent =
            Microsoft.CodeAnalysis.CodeStyle.NotificationOption.Silent;

        private static Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool> Bool(bool value)
        {
            return new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>(value, Silent);
        }

        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> VarForBuiltInTypes =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(VarForBuiltInTypes), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> VarWhenTypeIsApparent =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(VarWhenTypeIsApparent), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> VarElsewhere =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(VarElsewhere), Bool(false));

        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferAutoProperties =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferAutoProperties), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferObjectInitializer =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferObjectInitializer), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferCollectionInitializer =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferCollectionInitializer), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferExplicitTupleNames =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferExplicitTupleNames), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferInferredTupleNames =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferInferredTupleNames), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferInferredAnonymousTypeMemberNames =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferInferredAnonymousTypeMemberNames), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferCoalesceExpression =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferCoalesceExpression), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferNullPropagation =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferNullPropagation), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferIsNullCheckOverReferenceEqualityMethod =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferIsNullCheckOverReferenceEqualityMethod), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferReadonly =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferReadonly), Bool(false));

        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferPatternMatchingOverIsWithCastCheck =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferPatternMatchingOverIsWithCastCheck), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferPatternMatchingOverAsWithNullCheck =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferPatternMatchingOverAsWithNullCheck), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferSimpleDefaultExpression =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferSimpleDefaultExpression), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferLocalOverAnonymousFunction =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferLocalOverAnonymousFunction), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferInlinedVariableDeclaration =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferInlinedVariableDeclaration), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferDeconstructedVariableDeclaration =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferDeconstructedVariableDeclaration), Bool(false));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferThrowExpression =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferThrowExpression), Bool(true));
        public static readonly Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> PreferConditionalDelegateCall =
            new Microsoft.CodeAnalysis.Options.PerLanguageOption<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>>(nameof(CSharpCodeStyleOptions), nameof(PreferConditionalDelegateCall), Bool(true));

        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>> PreferExpressionBodiedMethods =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferExpressionBodiedMethods), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>(Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference.Never, Silent));
        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>> PreferExpressionBodiedConstructors =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferExpressionBodiedConstructors), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>(Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference.Never, Silent));
        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>> PreferExpressionBodiedOperators =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferExpressionBodiedOperators), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>(Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference.Never, Silent));
        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>> PreferExpressionBodiedProperties =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferExpressionBodiedProperties), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>(Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference.Never, Silent));
        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>> PreferExpressionBodiedIndexers =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferExpressionBodiedIndexers), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>(Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference.Never, Silent));
        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>> PreferExpressionBodiedAccessors =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferExpressionBodiedAccessors), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference>(Microsoft.CodeAnalysis.CodeStyle.ExpressionBodyPreference.Never, Silent));

        public static readonly Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.PreferBracesPreference>> PreferBraces =
            new Microsoft.CodeAnalysis.Options.Option<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.PreferBracesPreference>>(nameof(CSharpCodeStyleOptions), nameof(PreferBraces), new Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<Microsoft.CodeAnalysis.CodeStyle.PreferBracesPreference>(Microsoft.CodeAnalysis.CodeStyle.PreferBracesPreference.WhenMultiline, Silent));
    }
}

namespace Microsoft.CodeAnalysis.Editing
{
    public static class GenerationOptions
    {
        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> PlaceSystemNamespaceFirst =
            new Microsoft.CodeAnalysis.Options.Option<bool>("GenerationOptions", nameof(PlaceSystemNamespaceFirst), true);

        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> SeparateImportDirectiveGroups =
            new Microsoft.CodeAnalysis.Options.Option<bool>("GenerationOptions", nameof(SeparateImportDirectiveGroups), false);
    }
}

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    public static class SymbolSearchOptions
    {
        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> SuggestForTypesInNuGetPackages =
            new Microsoft.CodeAnalysis.Options.Option<bool>("SymbolSearchOptions", nameof(SuggestForTypesInNuGetPackages), true);
    }
}

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Metadata view used to read workspace-service exports coming back from the
    /// <see cref="IMefHostExportProvider"/> bridged into the VS MEF composition.
    /// Roslyn's own <c>WorkspaceServiceMetadata</c> is internal to
    /// Microsoft.CodeAnalysis.Workspaces.dll, so this compat assembly exposes the
    /// same shape publicly (ServiceType must be the assembly-qualified type name).
    /// </summary>
    public class WorkspaceServiceMetadata
    {
        public string ServiceType { get; }
        public string Layer { get; }

        public WorkspaceServiceMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            if (data != null)
            {
                if (data.TryGetValue("ServiceType", out var serviceType))
                    ServiceType = serviceType as string;
                if (data.TryGetValue("Layer", out var layer))
                    Layer = layer as string;
            }
        }
    }

    /// <summary>
    /// Functional replacement for the internal <c>MefWorkspaceServices</c> of
    /// Microsoft.CodeAnalysis.Workspaces.dll (4.8). Workspace services are resolved
    /// from the bridged host exports keyed by <c>ServiceType</c> metadata, which is
    /// compared against <c>typeof(T).AssemblyQualifiedName</c>. The original stub
    /// returned <c>default</c> for every service, which made Roslyn's
    /// <c>Workspace..ctor</c> throw "Service of type ... is required".
    /// </summary>
    public class MefWorkspaceServices : Microsoft.CodeAnalysis.Host.HostWorkspaceServices
    {
        private readonly Microsoft.CodeAnalysis.Host.HostServices _hostServices;
        private readonly Microsoft.CodeAnalysis.Workspace _workspace;
        private readonly System.Collections.Generic.Dictionary<string, Microsoft.CodeAnalysis.Host.IWorkspaceService> _services;

        public MefWorkspaceServices(Microsoft.CodeAnalysis.Host.HostServices hostServices, Microsoft.CodeAnalysis.Workspace workspace)
        {
            _hostServices = hostServices;
            _workspace = workspace;
            _services = new System.Collections.Generic.Dictionary<string, Microsoft.CodeAnalysis.Host.IWorkspaceService>(StringComparer.Ordinal);

            if (hostServices is IMefHostExportProvider provider)
            {
                foreach (var export in provider.GetExports<Microsoft.CodeAnalysis.Host.Mef.IWorkspaceServiceFactory, WorkspaceServiceMetadata>())
                {
                    var serviceType = export.Metadata?.ServiceType;
                    if (string.IsNullOrEmpty(serviceType))
                        continue;

                    var factory = export.Value;
                    if (factory != null)
                        _services[serviceType] = factory.CreateService(this);
                }

                foreach (var export in provider.GetExports<Microsoft.CodeAnalysis.Host.IWorkspaceService, WorkspaceServiceMetadata>())
                {
                    var serviceType = export.Metadata?.ServiceType;
                    if (string.IsNullOrEmpty(serviceType))
                        continue;

                    var service = export.Value;
                    if (service != null)
                        _services[serviceType] = service;
                }
            }
        }

        public override Microsoft.CodeAnalysis.Host.HostServices HostServices
        {
            get { return _hostServices; }
        }

        public override Microsoft.CodeAnalysis.Workspace Workspace
        {
            get { return _workspace; }
        }

        public override TWorkspaceService GetService<TWorkspaceService>()
        {
            if (_services.TryGetValue(typeof(TWorkspaceService).AssemblyQualifiedName, out var service))
                return (TWorkspaceService)service;

            return default(TWorkspaceService);
        }

        public override System.Collections.Generic.IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(Microsoft.CodeAnalysis.Host.HostWorkspaceServices.MetadataFilter filter)
        {
            return System.Linq.Enumerable.Empty<TLanguageService>();
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

        public static Microsoft.CodeAnalysis.Document GetOpenDocumentInCurrentContextWithChanges(this Microsoft.CodeAnalysis.Text.SourceText sourceText)
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

namespace Microsoft.CodeAnalysis
{
    public abstract class ValueSource<T>
    {
        public virtual T GetValue() => default(T);

        public virtual bool HasValue => true;

        public virtual bool TryGetValue(out T value)
        {
            value = default(T);
            return false;
        }

        public virtual T GetValue(System.Threading.CancellationToken cancellationToken) => GetValue();

        public virtual System.Threading.Tasks.Task<T> GetValueAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.FromResult(GetValue());
    }
}

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class DiagnosticData
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string HelpLink { get; set; }
        public bool IsEnabledByDefault { get; set; }
        public bool IsSuppressed { get; set; }
        public DiagnosticSeverity Severity { get; set; }
        public DiagnosticSeverity DefaultSeverity { get; set; }
        public DiagnosticDataLocation DataLocation { get; set; }
        public ImmutableArray<DiagnosticDataLocation> AdditionalLocations { get; set; } = ImmutableArray<DiagnosticDataLocation>.Empty;
        public IEnumerable<string> CustomTags { get; set; } = Array.Empty<string>();
        public ImmutableDictionary<string, string> Properties { get; set; } = ImmutableDictionary<string, string>.Empty;
    }

    public class DiagnosticDataLocation
    {
        public Microsoft.CodeAnalysis.Text.TextSpan? SourceSpan { get; set; }
        public string DocumentId { get; set; }
        public string ProjectId { get; set; }
        public string TreeFilePath { get; set; }
    }

    public static class DiagnosticDataExtensions
    {
        public static System.Threading.Tasks.Task<Location> ConvertLocationAsync(this DiagnosticDataLocation dataLocation, Project project, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(Location.None);
        }

        public static System.Threading.Tasks.Task<ImmutableArray<Location>> ConvertLocationsAsync(this IEnumerable<DiagnosticDataLocation> dataLocations, Project project, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(ImmutableArray<Location>.Empty);
        }
    }

    public static class InternalRuntimeDiagnosticOptions
    {
        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> Syntax = new Microsoft.CodeAnalysis.Options.Option<bool>("InternalSolutionCrawlerOptions", nameof(Syntax), false);
        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> Semantic = new Microsoft.CodeAnalysis.Options.Option<bool>("InternalSolutionCrawlerOptions", nameof(Semantic), false);
    }
}

namespace Microsoft.CodeAnalysis.ErrorLogger
{
    public interface IErrorLoggerService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        void LogException(object source, Exception exception);
    }
}

namespace Microsoft.CodeAnalysis.Execution
{
    public interface ISupportTemporaryStorage
    {
        System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Host.ITemporaryStreamStorage> GetStorages();
    }
}

namespace Microsoft.CodeAnalysis.Extensions
{
    public interface IExtensionManager : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        bool IsDisabled(object provider);
        bool CanHandleException(object provider, Exception exception);
        void HandleException(object provider, Exception exception);
    }

    public interface IInfoBarService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        void ShowInfoBarInActiveView(string message, params InfoBarUI[] items);
        void ShowInfoBarInGlobalView(string message, params InfoBarUI[] items);
    }

    public interface IErrorReportingService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        void ShowErrorInfoInActiveView(string message, params InfoBarUI[] items);
        void ShowGlobalErrorInfo(string message, params InfoBarUI[] items);
        void ShowDetailedErrorInfo(Exception exception);
    }

    public class InfoBarUI
    {
        public enum UIKind { Close, Button, HyperLink }

        public string Title { get; private set; }
        public UIKind Kind { get; private set; }
        public Action Action { get; private set; }
        public bool CloseAfterAction { get; private set; }

        public InfoBarUI(string title, UIKind kind, Action action = null, bool closeAfterAction = false)
        {
            Title = title;
            Kind = kind;
            Action = action;
            CloseAfterAction = closeAfterAction;
        }
    }
}

namespace Microsoft.CodeAnalysis.Host
{
    public interface IWorkspaceCacheService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        event EventHandler CacheFlushRequested;
    }

    public interface IFrameworkAssemblyPathResolver : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        string ResolveAssemblyPath(Microsoft.CodeAnalysis.ProjectId projectId, string assemblyName, string fullyQualifiedName = null);
    }

    public interface IMetadataService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        Microsoft.CodeAnalysis.PortableExecutableReference GetReference(string resolvedPath, Microsoft.CodeAnalysis.MetadataReferenceProperties properties);
    }

    public interface ICachedObjectOwner
    {
    }

    public interface IProjectCacheHostService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        IDisposable EnableCaching(Microsoft.CodeAnalysis.ProjectId key);
        T CacheObjectIfCachingEnabledForKey<T>(Microsoft.CodeAnalysis.ProjectId key, object owner, T instance) where T : class;
        T CacheObjectIfCachingEnabledForKey<T>(Microsoft.CodeAnalysis.ProjectId key, ICachedObjectOwner owner, T instance) where T : class;
    }

    public interface IWorkspaceTaskScheduler
    {
        System.Threading.Tasks.Task ScheduleTask(Action taskAction, string taskName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task ScheduleTask(Func<System.Threading.Tasks.Task> taskFunc, string taskName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task<T> ScheduleTask<T>(Func<System.Threading.Tasks.Task<T>> taskFunc, string taskName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }

    public interface IWorkspaceTaskSchedulerFactory
    {
        IWorkspaceTaskScheduler CreateBackgroundTaskScheduler();
        IWorkspaceTaskScheduler CreateEventingTaskQueue();
    }

    public static class CompatHostExtensions
    {
        public static IProjectCacheHostService CacheService(this SolutionServices services)
        {
            return services.GetService<IProjectCacheHostService>();
        }
    }
}

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public static class MefConstruction
    {
        public const string ImportingConstructorMessage = "Use an importing constructor instead.";
    }

    public class FileExtensionsMetadata
    {
        public System.Collections.Generic.IEnumerable<string> Extensions { get; private set; }
        public string DataAnnotationFileExtensions { get; private set; }
        public string TextFileExtensions { get; private set; }
    }
}

namespace Microsoft.CodeAnalysis.Options
{
    public interface IOptionPersister
    {
        bool TryFetch(Microsoft.CodeAnalysis.Options.OptionKey optionKey, out object value);
        bool TryPersist(Microsoft.CodeAnalysis.Options.OptionKey optionKey, object value);
    }

    public interface IGlobalOptionService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        void RefreshOption(Microsoft.CodeAnalysis.Options.OptionKey optionKey, object value);
    }

    public interface IOptionService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        void RegisterDocumentOptionsProvider(Microsoft.CodeAnalysis.Options.IDocumentOptionsProvider provider);
    }

    public abstract class EditorConfigStorageLocation : OptionStorageLocation
    {
        protected EditorConfigStorageLocation(string keyName)
        {
        }
    }

    public sealed class EditorConfigStorageLocation<T> : EditorConfigStorageLocation
    {
        public string KeyName { get; private set; }

        public EditorConfigStorageLocation(string keyName)
            : base(keyName)
        {
            KeyName = keyName;
        }
    }

    public interface IDocumentOptions
    {
        bool TryGetDocumentOption(Microsoft.CodeAnalysis.Options.OptionKey option, out object value);
    }

    public interface IDocumentOptionsProvider
    {
        System.Threading.Tasks.Task<IDocumentOptions> GetOptionsForDocumentAsync(Microsoft.CodeAnalysis.Document document, System.Threading.CancellationToken cancellationToken);
    }

    public interface IDocumentOptionsProviderFactory
    {
        IDocumentOptionsProvider TryCreate(Microsoft.CodeAnalysis.Workspace workspace);
    }
}

namespace Microsoft.CodeAnalysis.Options.Providers
{
    public interface IOptionProvider
    {
    }
}

namespace Microsoft.CodeAnalysis.Shared.Options
{
    public static class RuntimeOptions
    {
        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> FullSolutionAnalysis = new Microsoft.CodeAnalysis.Options.Option<bool>("RuntimeOptions", nameof(FullSolutionAnalysis), false);
        public static readonly Microsoft.CodeAnalysis.Options.Option<bool> FullSolutionAnalysisInfoBarShown = new Microsoft.CodeAnalysis.Options.Option<bool>("RuntimeOptions", nameof(FullSolutionAnalysisInfoBarShown), false);
    }
}

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    public interface IAsyncToken : IDisposable
    {
    }

    public interface IAsynchronousOperationListener
    {
        Microsoft.CodeAnalysis.Shared.TestHooks.IAsyncToken BeginAsyncOperation(string operationName, string feature = null);
    }

    public interface IAsynchronousOperationListenerProvider
    {
        IAsynchronousOperationListener GetListener(string featureName);
        IAsynchronousOperationListener GetListener(Microsoft.CodeAnalysis.Shared.TestHooks.FeatureAttribute featureAttribute);
    }
}

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    public interface IProgressTracker
    {
    }
}

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    public interface ISolutionCrawlerRegistrationService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        void Register(Microsoft.CodeAnalysis.Workspace workspace);
        void Unregister(Microsoft.CodeAnalysis.Workspace workspace);
    }
}

namespace Microsoft.CodeAnalysis.Storage
{
    public interface IPersistentStorageLocationService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;

        bool IsSupported(Microsoft.CodeAnalysis.Workspace workspace);

        string TryGetStorageLocation(Microsoft.CodeAnalysis.SolutionId solutionId);
    }

    public class PersistentStorageLocationChangingEventArgs : EventArgs
    {
        public Microsoft.CodeAnalysis.SolutionId SolutionId { get; private set; }
        public string NewStorageLocation { get; private set; }
        public bool MustUseNewStorageLocationImmediately { get; private set; }

        public PersistentStorageLocationChangingEventArgs(Microsoft.CodeAnalysis.SolutionId solutionId, string newStorageLocation, bool mustUseNewStorageLocationImmediately)
        {
            SolutionId = solutionId;
            NewStorageLocation = newStorageLocation;
            MustUseNewStorageLocationImmediately = mustUseNewStorageLocationImmediately;
        }
    }

    public static class StorageOptions
    {
        public static readonly Microsoft.CodeAnalysis.Options.Option<int> SolutionSizeThreshold = new Microsoft.CodeAnalysis.Options.Option<int>("StorageOptions", nameof(SolutionSizeThreshold), int.MaxValue);
    }
}

namespace Microsoft.CodeAnalysis.SolutionSize
{
    public static class SolutionSize
    {
    }
}

namespace Microsoft.CodeAnalysis
{
    public static class SymbolCompatExtensions
    {
        public static bool IsKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol != null && symbol.Kind == kind;
        }

        public static bool IsMandatoryNamedParameterPosition(this SyntaxToken token)
        {
            return token.Parent is NameColonSyntax || token.Parent is NameEqualsSyntax;
        }
    }

    public static class DocumentCompatExtensions
    {
        public static Document WithFrozenPartialSemantics(this Document document, CancellationToken cancellationToken)
        {
            return document;
        }

        public static bool IsOpen(this Document document)
        {
            return document != null && document.Project.Solution.Workspace.IsDocumentOpen(document.Id);
        }

        public static void SetDocumentContext(this Workspace workspace, DocumentId documentId)
        {
        }
    }

    public class RelativePathResolver
    {
        public System.Collections.Immutable.ImmutableArray<string> BasePaths { get; }

        public string BaseDirectory { get; }

        public RelativePathResolver(System.Collections.Immutable.ImmutableArray<string> basePaths, string baseDirectory)
        {
            BasePaths = basePaths;
            BaseDirectory = baseDirectory;
        }
    }
}

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    public static class SemanticModelCompatExtensions
    {
        public static INamedTypeSymbol GetEnclosingNamedTypeOrAssembly(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            if (semanticModel == null)
                return null;
            var token = semanticModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var type = token.Parent?.AncestorsAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeDeclarationSyntax>().FirstOrDefault();
            return type != null ? semanticModel.GetDeclaredSymbol(type, cancellationToken) as INamedTypeSymbol : null;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class CSharpCompatSyntaxExtensions
    {
        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
        {
            return node != null && node.Parent != null && node.Parent.IsKind(kind);
        }

        public static Microsoft.CodeAnalysis.CSharp.Syntax.DirectiveTriviaSyntax GetMatchingDirective(this Microsoft.CodeAnalysis.CSharp.Syntax.DirectiveTriviaSyntax directive, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (directive == null)
                return null;
            var root = directive.SyntaxTree.GetRoot(cancellationToken);
            var directives = root.DescendantNodes(descendIntoTrivia: true).OfType<Microsoft.CodeAnalysis.CSharp.Syntax.DirectiveTriviaSyntax>().ToList();
            if (!directive.IsKind(SyntaxKind.RegionDirectiveTrivia))
                return null;
            int depth = 0;
            foreach (var d in directives)
            {
                if (d.SpanStart <= directive.SpanStart)
                    continue;
                if (d.IsKind(SyntaxKind.RegionDirectiveTrivia))
                    depth++;
                else if (d.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                {
                    if (depth == 0)
                        return d;
                    depth--;
                }
            }
            return null;
        }
    }

    public sealed class CSharpSyntaxFactsService
    {
        public static readonly CSharpSyntaxFactsService Instance = new CSharpSyntaxFactsService();

        public bool IsVerbatimStringLiteral(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.StringLiteralToken) && token.Text.StartsWith("@", StringComparison.Ordinal)
                || token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);
        }

        public bool IsStringLiteral(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.StringLiteralToken) && !token.Text.StartsWith("@", StringComparison.Ordinal);
        }

        public SyntaxNode GetContainingTypeDeclaration(SyntaxNode rootedNode, int position)
        {
            if (rootedNode == null)
                return null;
            var token = rootedNode.FindToken(position);
            return token.Parent?.AncestorsAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>().FirstOrDefault() as SyntaxNode
                ?? token.Parent?.AncestorsAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.DelegateDeclarationSyntax>().FirstOrDefault() as SyntaxNode;
        }
    }
}

namespace Microsoft.CodeAnalysis.Options
{
    public interface IEditorConfigStorageLocation
    {
        bool TryGetOption(object rawEditorConfig, System.Type type, out object value);
    }
}

namespace Roslyn.Utilities
{
    public static class KeyValuePairUtil
    {
        public static System.Collections.Generic.KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new System.Collections.Generic.KeyValuePair<TKey, TValue>(key, value);
        }
    }

    public static class SpecializedCollections
    {
        public static System.Collections.Generic.IEnumerable<T> EmptyEnumerable<T>()
        {
            return System.Linq.Enumerable.Empty<T>();
        }
    }
}

namespace Microsoft.CodeAnalysis.Host
{
    public class WorkspaceMetadataFileReferenceResolver : MetadataReferenceResolver
    {
        public IMetadataService MetadataService { get; }

        public RelativePathResolver PathResolver { get; }

        public WorkspaceMetadataFileReferenceResolver(IMetadataService metadataService, RelativePathResolver pathResolver)
        {
            MetadataService = metadataService;
            PathResolver = pathResolver;
        }

        public override System.Collections.Immutable.ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, Microsoft.CodeAnalysis.MetadataReferenceProperties properties)
        {
            return default(System.Collections.Immutable.ImmutableArray<PortableExecutableReference>);
        }

        public override bool Equals(object other)
        {
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return PathResolver != null ? PathResolver.GetHashCode() : 0;
        }
    }
}

namespace Microsoft.CodeAnalysis.Rename
{
    public sealed class RenamableSymbolInfo
    {
        public ISymbol Symbol { get; }

        public RenamableSymbolInfo(ISymbol symbol)
        {
            Symbol = symbol;
        }
    }

    public static class RenameLocations
    {
        public static class ReferenceProcessing
        {
            public static System.Threading.Tasks.Task<RenamableSymbolInfo> GetRenamableSymbolAsync(Document document, int position, CancellationToken cancellationToken)
            {
                ISymbol symbol = null;
                if (document != null)
                {
                    var model = document.GetSemanticModelAsync(cancellationToken).GetAwaiter().GetResult();
                    symbol = model != null ? model.GetEnclosingSymbol(position, cancellationToken) : null;
                }
                return System.Threading.Tasks.Task.FromResult(new RenamableSymbolInfo(symbol));
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public interface IMefHostExportProvider
    {
        System.Collections.Generic.IEnumerable<System.Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>();
        System.Collections.Generic.IEnumerable<System.Lazy<TExtension>> GetExports<TExtension>();
    }

    public interface ILanguageMetadata
    {
        string Language { get; }
    }

    public class LanguageMetadata : ILanguageMetadata
    {
        public string Language { get; }

        public LanguageMetadata(string language)
        {
            Language = language;
        }
    }

    public class OrderableLanguageMetadata : LanguageMetadata
    {
        public string Order { get; }

        public OrderableLanguageMetadata(string language, string order)
            : base(language)
        {
            Order = order;
        }
    }

    public static class LanguageMetadataExtensions
    {
        public static System.Collections.Generic.IEnumerable<TExtension> FilterToSpecificLanguage<TExtension, TMetadata>(
            this System.Collections.Generic.IEnumerable<System.Lazy<TExtension, TMetadata>> exports, string language)
            where TMetadata : ILanguageMetadata
        {
            return exports
                .Where(e => string.IsNullOrEmpty(e.Metadata.Language) || e.Metadata.Language == language)
                .Select(e => e.Value)
                .ToList();
        }
    }
}

namespace Microsoft.CodeAnalysis
{
    public interface IOrganizeImportsService : Host.ILanguageService
    {
        System.Threading.Tasks.Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    public static class LocationExtensions
    {
        public static bool IsVisibleSourceLocation(this Location location)
        {
            return location != null && location.IsInSource;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    public class UsingsAndExternAliasesDirectiveComparer : System.Collections.Generic.IComparer<Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax>
    {
        public static readonly System.Collections.Generic.IComparer<Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax> SystemFirstInstance =
            new UsingsAndExternAliasesDirectiveComparer(systemFirst: true);

        public static readonly System.Collections.Generic.IComparer<Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax> NormalInstance =
            new UsingsAndExternAliasesDirectiveComparer(systemFirst: false);

        private readonly bool systemFirst;

        private UsingsAndExternAliasesDirectiveComparer(bool systemFirst)
        {
            this.systemFirst = systemFirst;
        }

        public int Compare(Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax x, Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax y)
        {
            var xName = x?.Name?.ToString();
            var yName = y?.Name?.ToString();
            if (xName == null)
                return yName == null ? 0 : -1;
            if (yName == null)
                return 1;
            bool xIsSystem = xName.StartsWith("System", System.StringComparison.Ordinal);
            bool yIsSystem = yName.StartsWith("System", System.StringComparison.Ordinal);
            if (xIsSystem != yIsSystem)
                return systemFirst ? (xIsSystem ? -1 : 1) : (xIsSystem ? 1 : -1);
            return string.CompareOrdinal(xName, yName);
        }
    }
}

namespace Roslyn.Utilities
{
    public static class RoslynUtilitiesExtensions
    {
        public static System.Collections.Generic.ISet<T> ToSet<T>(this System.Collections.Generic.IEnumerable<T> source, System.Collections.Generic.IEqualityComparer<T> comparer = null)
        {
            return new System.Collections.Generic.HashSet<T>(source, comparer ?? System.Collections.Generic.EqualityComparer<T>.Default);
        }
    }
}
