using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
	// These types mirror MonoDevelop's former Roslyn fork (Microsoft.CodeAnalysis.SystemTools).
	// They must stay in a MonoDevelop.* assembly that is a strong-named friend of the real
	// Microsoft.CodeAnalysis.Workspaces assembly, because they expose
	// Microsoft.CodeAnalysis.Diagnostics.DiagnosticData which is internal to that assembly.

	internal abstract class AbstractHostDiagnosticUpdateSource
	{
		public abstract Workspace Workspace { get; }

		public event EventHandler<Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs> DiagnosticsUpdated;

		protected void RaiseDiagnosticsUpdated (Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs args)
		{
			DiagnosticsUpdated?.Invoke (this, args);
		}

		protected void ClearAnalyzerDiagnostics (ProjectId projectId) { }
		public void ClearAnalyzerReferenceDiagnostics (object analyzerReference, string language, ProjectId projectId) { }
	}

	internal interface IDiagnosticUpdateSourceRegistrationService
	{
		void Register (AbstractHostDiagnosticUpdateSource source);
	}
}

namespace Microsoft.CodeAnalysis.Diagnostics
{
	internal sealed class DiagnosticsUpdatedArgs : EventArgs
	{
		public object Id { get; private set; }
		public Microsoft.CodeAnalysis.Workspace Workspace { get; private set; }
		public Microsoft.CodeAnalysis.ProjectId ProjectId { get; private set; }
		public Microsoft.CodeAnalysis.DocumentId DocumentId { get; private set; }
		public bool IsRemoval { get; private set; }
		public ImmutableArray<DiagnosticData> Diagnostics { get; private set; }

		DiagnosticsUpdatedArgs (object id, Microsoft.CodeAnalysis.Workspace workspace,
			Microsoft.CodeAnalysis.ProjectId projectId, Microsoft.CodeAnalysis.DocumentId documentId,
			bool isRemoval, ImmutableArray<DiagnosticData> diagnostics)
		{
			Id = id;
			Workspace = workspace;
			ProjectId = projectId;
			DocumentId = documentId;
			IsRemoval = isRemoval;
			Diagnostics = diagnostics;
		}

		public static DiagnosticsUpdatedArgs DiagnosticsCreated<TDiagnostic> (object id,
			Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.Solution solution,
			Microsoft.CodeAnalysis.ProjectId projectId, Microsoft.CodeAnalysis.DocumentId documentId,
			ImmutableArray<TDiagnostic> diagnostics)
		{
			ImmutableArray<DiagnosticData> converted = diagnostics.IsDefaultOrEmpty
				? ImmutableArray<DiagnosticData>.Empty
				: (ImmutableArray<DiagnosticData>)(object)diagnostics;
			return new DiagnosticsUpdatedArgs (id, workspace, projectId, documentId, false, converted);
		}

		public static DiagnosticsUpdatedArgs DiagnosticsRemoved (object id,
			Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.Solution solution,
			Microsoft.CodeAnalysis.ProjectId projectId, Microsoft.CodeAnalysis.DocumentId documentId)
		{
			return new DiagnosticsUpdatedArgs (id, workspace, projectId, documentId, true, ImmutableArray<DiagnosticData>.Empty);
		}
	}

	internal interface IDiagnosticService
	{
		event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

		IEnumerable<Microsoft.CodeAnalysis.Common.UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs (
			Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.ProjectId projectId,
			Microsoft.CodeAnalysis.DocumentId documentId, CancellationToken cancellationToken);

		IEnumerable<DiagnosticData> GetDiagnostics (
			Microsoft.CodeAnalysis.Workspace workspace, Microsoft.CodeAnalysis.ProjectId projectId,
			Microsoft.CodeAnalysis.DocumentId documentId, object id, bool includeSuppressedDiagnostics,
			CancellationToken cancellationToken);
	}

	internal static class AnalyzerHelper
	{
		public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic (
			Microsoft.CodeAnalysis.ProjectId projectId,
			string language,
			string filePath,
			AnalyzerLoadFailureEventArgs e)
		{
			return null;
		}
	}

	internal static class DiagnosticDataExtensions
	{
		public static TextSpan GetExistingOrCalculatedTextSpan (this DiagnosticData diagnostic, SourceText text)
		{
			if (diagnostic == null || diagnostic.DataLocation == null)
				return new TextSpan (0, 0);
			if (diagnostic.DataLocation.SourceSpan.HasValue)
				return diagnostic.DataLocation.SourceSpan.Value;
			return new TextSpan (0, text != null ? text.Length : 0);
		}
	}
}