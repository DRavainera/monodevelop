// Stubs for Roslyn Features/EditorFeatures types that are missing from the
// netstandard2.0 compile references, but which are internal to the real Roslyn
// assemblies (FixAllState, ISyntaxFactsService, etc.) and therefore not
// accessible from MonoDevelop.Refactoring. These type-shape shadows are
// declared in this assembly so the old-editor code paths can compile against 4.8.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal sealed class CodeFix
    {
        public CodeFix(Diagnostic primaryDiagnostic, CodeAction action)
        {
            PrimaryDiagnostic = primaryDiagnostic;
            Action = action;
        }

        public Diagnostic PrimaryDiagnostic { get; }
        public CodeAction Action { get; }
    }

    internal sealed class FixAllState
    {
        public FixAllState()
        {
        }

        public FixAllProvider FixAllProvider { get; internal set; }
        public FixAllScope Scope { get; internal set; }
        public IEnumerable<string> DiagnosticIds { get; internal set; }

        public string GetDefaultFixAllTitle()
        {
            return null;
        }

        public FixAllContext CreateFixAllContext(IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return null;
        }

        public FixAllState WithScopeAndEquivalenceKey(FixAllScope scope, string equivalenceKey)
        {
            return this;
        }
    }

    internal class CodeFixCollection
    {
        public CodeFixCollection(
            object provider,
            TextSpan textSpan,
            ImmutableArray<CodeFix> fixes,
            FixAllState fixAllState,
            ImmutableArray<FixAllScope> supportedScopes,
            Diagnostic firstDiagnostic)
        {
            Provider = provider;
            TextSpan = textSpan;
            Fixes = fixes;
            FixAllState = fixAllState;
            SupportedScopes = supportedScopes;
            FirstDiagnostic = firstDiagnostic;
        }

        public object Provider { get; }
        public TextSpan TextSpan { get; }
        public ImmutableArray<CodeFix> Fixes { get; }
        public FixAllState FixAllState { get; }
        public ImmutableArray<FixAllScope> SupportedScopes { get; }
        public Diagnostic FirstDiagnostic { get; }
    }

    internal interface ICodeFixService
    {
        Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(
            Document document,
            TextSpan span,
            bool shouldFixTextSpan,
            CancellationToken cancellationToken);
    }
}

namespace Microsoft.CodeAnalysis
{
    internal interface IDocumentTextDifferencingService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document newDocument, Document oldDocument, CancellationToken cancellationToken);
    }

    internal static class SharedCompatExtensions
    {
        public static INamedTypeSymbol GetEnclosingNamedType(this SemanticModel model, int position, CancellationToken cancellationToken)
        {
            return null;
        }

        public static T WaitAndGetResult<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return task.GetAwaiter().GetResult();
        }

        public static IEnumerable<OptionKey> GetChangedOptions(this OptionSet optionSet, OptionSet otherOptionSet)
        {
            return Enumerable.Empty<OptionKey>();
        }
    }

    internal static class DocumentCompatExtensions
    {
        public static bool IsGeneratedCode(this Document document, CancellationToken cancellationToken)
        {
            return false;
        }
    }

    internal static class AnalyzerCompatExtensions
    {
        public static string GetAnalyzerId(this DiagnosticAnalyzer analyzer)
        {
            return analyzer?.GetType()?.Name ?? "Unknown";
        }
    }

    internal static class CodeActionExtensionsCompat
    {
        private const string NestedActionsFullName = "Microsoft.CodeAnalysis.CodeActions.CodeAction+CodeActionWithNestedActions";

        internal static bool TryGetNestedActions(this CodeAction action, out ImmutableArray<CodeAction> nestedActions, out bool isInlinable)
        {
            nestedActions = default;
            isInlinable = false;

            if (action == null)
                return false;

            var type = action.GetType();
            if (type == null || type.FullName != NestedActionsFullName)
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (type.GetProperty("NestedCodeActions", flags)?.GetValue(action) is IEnumerable<CodeAction> list)
                nestedActions = list.ToImmutableArray();

            if (type.GetProperty("IsInlinable", flags)?.GetValue(action) is bool b)
                isInlinable = b;

            return true;
        }
    }
}

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DesktopAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
        }

        public System.Reflection.Assembly LoadFromPath(string fullPath)
        {
            return null;
        }
    }
}

namespace Microsoft.CodeAnalysis.Options
{
    internal static class OptionServiceExtensionsCompat
    {
        public static OptionSet GetOptions(this IOptionService service)
        {
            return null;
        }

        public static void SetOptions(this IOptionService service, OptionSet optionSet)
        {
        }

        public static T GetOption<T>(this IOptionService service, Option<T> option, string languageName = null)
        {
            return default;
        }

        public static T GetOption<T>(this IOptionService service, PerLanguageOption<T> option, string languageName)
        {
            return default;
        }
    }
}

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public interface IWorkspaceDiagnosticAnalyzerProviderService
    {
        IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader();
        IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages();
    }
}

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal interface IGenerateTypeOptionsService
    {
        GenerateTypeOptionsResult GetGenerateTypeOptions(
            string className,
            GenerateTypeDialogOptions generateTypeDialogOptions,
            Document document,
            Microsoft.CodeAnalysis.Notification.INotificationService notificationService,
            Microsoft.CodeAnalysis.ProjectManagement.IProjectManagementService projectManagementService,
            Microsoft.CodeAnalysis.LanguageServices.ISyntaxFactsService syntaxFactsService);
    }
}

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal interface IExtractInterfaceOptionsService
    {
        Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            Microsoft.CodeAnalysis.LanguageServices.ISyntaxFactsService syntaxFactsService,
            Microsoft.CodeAnalysis.Notification.INotificationService notificationService,
            List<ISymbol> extractableMembers,
            string defaultInterfaceName,
            List<string> conflictingTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName);
    }
}

namespace Microsoft.CodeAnalysis.LanguageServices
{
    [Flags]
    public enum DisplayNameOptions
    {
        IncludeType = 0x1,
        IncludeNamespaces = 0x2,
        IncludeParameters = 0x4,
        IncludeGenericsWhenStructured = 0x8,
        IncludeTypeParameters = 0x10,
        IncludeAwaitKeywords = 0x20,
        IncludeNonImportableTypes = 0x40,
        UseArityForGenericTypes = 0x80
    }

    public interface ISyntaxFactsService : Microsoft.CodeAnalysis.Host.ILanguageService
    {
        bool IsValidIdentifier(string name);
        bool IsVerbatimIdentifier(string name);
        bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace);
        bool IsSkippedTokensTrivia(SyntaxNode syntax);
        SyntaxNode GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = false);
        string GetDisplayName(SyntaxNode node, DisplayNameOptions options = DisplayNameOptions.IncludeType);
    }
}

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchService
    {
        Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken);
        Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken);
        Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken);
    }

    internal sealed class PackageWithAssemblyResult
    {
        public PackageWithAssemblyResult(string packageName, string version, int rank)
        {
            PackageName = packageName;
            Version = version;
            Rank = rank;
        }

        public string PackageName { get; }
        public string Version { get; }
        public int Rank { get; }
    }

    internal sealed class PackageWithTypeResult
    {
        public PackageWithTypeResult(string packageName, string typeName, string version, int rank, ImmutableArray<string> containingNamespaceNames)
        {
            PackageName = packageName;
            TypeName = typeName;
            Version = version;
            Rank = rank;
            ContainingNamespaceNames = containingNamespaceNames;
        }

        public string PackageName { get; }
        public string TypeName { get; }
        public string Version { get; }
        public int Rank { get; }
        public ImmutableArray<string> ContainingNamespaceNames { get; }
    }

    internal sealed class ReferenceAssemblyWithTypeResult
    {
        public ReferenceAssemblyWithTypeResult(string assemblyName, string typeName, ImmutableArray<string> containingNamespaceNames)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            ContainingNamespaceNames = containingNamespaceNames;
        }

        public string AssemblyName { get; }
        public string TypeName { get; }
        public ImmutableArray<string> ContainingNamespaceNames { get; }
    }
}

namespace Microsoft.CodeAnalysis.Packaging
{
    internal interface IPackageInstallerService : Microsoft.CodeAnalysis.Host.IWorkspaceService
    {
        event EventHandler PackageSourcesChanged;

        ImmutableArray<PackageSource> GetPackageSources();
        ImmutableArray<string> GetInstalledVersions(string packageName);
        IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);
        bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName);
        bool IsEnabled(ProjectId projectId);
        bool CanShowManagePackagesDialog();
        void ShowManagePackagesDialog(string packageName);
        bool TryInstallPackage(Workspace workspace, DocumentId documentId, string source, string packageName, string versionOpt, bool includePrerelease, CancellationToken cancellationToken);
    }

    internal sealed class PackageSource
    {
        public PackageSource(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public string Name { get; }
        public string Source { get; }
    }
}

namespace Roslyn.Utilities
{
    internal sealed class ConcurrentSet<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly ConcurrentDictionary<T, byte> dictionary;

        public ConcurrentSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public ConcurrentSet(IEqualityComparer<T> comparer)
        {
            dictionary = new ConcurrentDictionary<T, byte>(comparer);
        }

        public bool Add(T item) => dictionary.TryAdd(item, 0);
        public bool Remove(T item) => dictionary.TryRemove(item, out _);
        public void Clear() => dictionary.Clear();
        public bool Contains(T item) => dictionary.ContainsKey(item);
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;
        public IEnumerator<T> GetEnumerator() => dictionary.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void CopyTo(T[] array, int arrayIndex) => dictionary.Keys.CopyTo(array, arrayIndex);

        void ICollection<T>.Add(T item) => Add(item);
    }
}
