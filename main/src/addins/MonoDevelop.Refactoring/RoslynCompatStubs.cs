// Stubs for Roslyn Features/EditorFeatures types that are missing from the
// netstandard2.0 compile references, but which reference internal Roslyn types
// (FixAllState, ISyntaxFactsService, IAnalyzerAssemblyLoader) not accessible from
// MonoRoslynCompat. This assembly is a friend of Roslyn, so the internals resolve here.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
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
