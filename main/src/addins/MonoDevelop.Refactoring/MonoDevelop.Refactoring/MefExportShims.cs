//
// MefExportShims.cs
//
// Author:
//       Buffy (Codebuff) <noreply@codebuff.com>
//
// Copyright (c) 2026 MonoDevelop fork contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// MEF export shims for the Roslyn services the old-editor extensions import.
//
// The real Roslyn implementations live in Microsoft.CodeAnalysis.Editor(Features)
// assemblies that are intentionally NOT staged on this platform (see CompositionManager):
//   - ICodeFixService          -> Microsoft.CodeAnalysis.Editor (EditorFeatures)
//   - ICodeRefactoringService  -> internal in Microsoft.CodeAnalysis.Features, with a
//                                 different signature (TextDocument + CodeActionOptionsProvider)
//   - IDiagnosticService       -> obsolete Roslyn contract, superseded by the internal
//                                 IDiagnosticAnalyzerService push/pull model
// The fork compiles compatible type-shape shadows of those contracts (RoslynCompatStubs.cs
// and MonoDevelop.Ide TypeSystem/DiagnosticsCompat.cs), but nothing exported them, so every
// document open tripped:
//   "Expected 1 export(s) with contract name Microsoft.CodeAnalysis.Diagnostics.IDiagnosticService
//    but found 0 after applying applicable constraints."
// for ResultsEditorExtension / CodeActionEditorExtension, and the extensions failed to create.
//
// Until the full editor-features pipeline is ported, export inert implementations that satisfy
// the imports so the extensions compose and keep their other functionality. The behaviors they
// would provide (suggested fixes, refactorings, diagnostic markers) degrade to "no results",
// matching the pattern of other documented no-op shims in this migration (e.g. the telemetry
// ILoggingServiceInternal replacement).

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Refactoring.Composition
{
	[Export (typeof (IDiagnosticService))]
	sealed class InertDiagnosticService : IDiagnosticService
	{
		public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated {
			add { }
			remove { }
		}

		public IEnumerable<Microsoft.CodeAnalysis.Common.UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs (
			Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
		{
			yield break;
		}

		public IEnumerable<DiagnosticData> GetDiagnostics (
			Workspace workspace, ProjectId projectId, DocumentId documentId, object id,
			bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
		{
			yield break;
		}
	}

	[Export (typeof (ICodeFixService))]
	sealed class InertCodeFixService : ICodeFixService
	{
		public Task<ImmutableArray<CodeFixCollection>> GetFixesAsync (
			Document document, TextSpan span, bool shouldFixTextSpan, CancellationToken cancellationToken)
		{
			return Task.FromResult (ImmutableArray<CodeFixCollection>.Empty);
		}
	}
}

namespace Microsoft.CodeAnalysis
{
	// Exported under the contract the DiagnosticsCompat stub declares (MonoDevelop.Ide
	// TypeSystem/DiagnosticsCompat.cs), which is what MonoDevelopWorkspace.ProjectSystemHandler
	// imports to register the HostDiagnosticUpdateSource when a workspace loads. The real
	// IDiagnosticUpdateSourceRegistrationService lives internal to Microsoft.CodeAnalysis.Features
	// and its export carries a different contract identity, so the stub import found 0 exports and
	// the workspace load failed with "Could not load parser database". Until the diagnostics
	// pipeline is ported, Register is a no-op matching the inert shim pattern above.
	[Export (typeof (IDiagnosticUpdateSourceRegistrationService))]
	sealed class InertDiagnosticUpdateSourceRegistrationService : IDiagnosticUpdateSourceRegistrationService
	{
		public void Register (AbstractHostDiagnosticUpdateSource source) { }
	}
}

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
	// Exported under the contract the MonoRoslynCompat stub declares (GetRefactoringsAsync(Document, TextSpan, ct)),
	// which is what CodeActionEditorExtension and ResultTooltipProvider import.
	[Export (typeof (ICodeRefactoringService))]
	sealed class InertCodeRefactoringService : ICodeRefactoringService
	{
		public Task<ImmutableArray<CodeRefactoring>> GetRefactoringsAsync (
			Document document, TextSpan textSpan, CancellationToken cancellationToken)
		{
			return Task.FromResult (ImmutableArray<CodeRefactoring>.Empty);
		}
	}
}
