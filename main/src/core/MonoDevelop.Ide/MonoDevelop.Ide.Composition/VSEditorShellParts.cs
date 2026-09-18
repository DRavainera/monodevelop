using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities.Implementation
{
	// Replaces the telemetry service that the consolidated Microsoft.VisualStudio.Platform.VSEditor
	// package would export. MonoDevelop has no telemetry, so all calls are no-ops.
	[Export (typeof (ILoggingServiceInternal))]
	internal sealed class NoOpLoggingServiceInternal : ILoggingServiceInternal
	{
		public void PostEvent (string key, params object [] namesAndProperties) { }
		public void PostEvent (string key, IReadOnlyList<object> namesAndProperties) { }
		public void PostEvent (TelemetryEventType eventType, string eventName, TelemetryResult result = TelemetryResult.Success, params (string name, object property) [] namesAndProperties) { }
		public void PostEvent (TelemetryEventType eventType, string eventName, TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties) { }
		public void PostFault (string eventName, string description, Exception exceptionObject, string additionalErrorInfo = null, bool? isIncludedInWatsonSample = null, object [] correlations = null) { }
		public void AdjustCounter (string key, string name, int delta = 1) { }
		public void PostCounters () { }
		public object CreateTelemetryOperationEventScope (string eventName, TelemetrySeverity severity, object [] correlations, IDictionary<string, object> startingProperties) => null;
		public object GetCorrelationFromTelemetryScope (object telemetryScope) => null;
		public void EndTelemetryScope (object telemetryScope, TelemetryResult result, string summary = null) { }
	}
}

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
	// The shell normally exports this to marshal callbacks back to the main thread; route them
	// through the ambient synchronization context (the Gtk main loop) when one is available.
	[Export (typeof (IForegroundNotificationService))]
	internal sealed class ForegroundNotificationService : IForegroundNotificationService
	{
		public void RegisterNotification (Action callback, object asyncToken)
		{
			if (callback == null)
				return;
			var syncContext = System.Threading.SynchronizationContext.Current;
			if (syncContext != null)
				syncContext.Post (state => callback (), null);
			else
				System.Threading.ThreadPool.QueueUserWorkItem (state => callback ());
		}
	}
}

namespace Microsoft.VisualStudio.Utilities.Implementation
{
	// Stock content types beyond the BufferFactoryService base set. Roslyn's language services and
	// the MonoDevelop language bindings target the "CSharp" content type (see
	// MonoRoslynCompat ContentTypeNames.CSharpContentType), but no vs-editor assembly staged on
	// Linux exports its ContentTypeDefinition, so every .cs buffer fell back to content type
	// "text". That broke the mime->content-type mapping (MimeTypeCatalog returned null, logged as
	// "GetContentTypeFromMimeType null leg"), TextDocument.MimeType resolved to text/plain and the
	// syntax highlighter never engaged.
	//
	// NOTE on the export shape: the registry imports Lazy<ContentTypeDefinition, ...>, so the
	// exported value must BE a ContentTypeDefinition (contract = typeof(ContentTypeDefinition)).
	// Same pattern as BufferFactoryService: [Export] with no contract on a field/property typed as
	// ContentTypeDefinition. An [Export] on a plain class would use the class itself as contract
	// and never match the registry import (verified: intellisense/sighelp DID register, exported
	// exactly this way from DefaultSignatureHelpPresenterProvider).
	internal sealed class LanguageContentTypes
	{
		[Export]
		[Name ("CSharp")]
		[BaseDefinition ("code")]
		public ContentTypeDefinition CSharpContentTypeDefinition = new ContentTypeDefinition ();

		[Export]
		[Name ("VisualBasic")]
		[BaseDefinition ("code")]
		public ContentTypeDefinition VisualBasicContentTypeDefinition = new ContentTypeDefinition ();
	}
}

namespace MonoDevelop.Ide.Composition
{
	// The brace-completion implementation lives on the Mono side (MonoDevelop.SourceEditor.Braces),
	// so vs-editor's Microsoft.VisualStudio.Text.BraceCompletion.Implementation assembly - which
	// exports the "BraceCompletion/Enabled" option definition - is never built for this bootstrap.
	// Provide the option here: EditorPreferences.Wrap requires every option to have a definition,
	// and without this one the editor view fails to load ("View failed to load" NullReferenceException).
	[Export (typeof (Microsoft.VisualStudio.Text.Editor.EditorOptionDefinition))]
	sealed class BraceCompletionEnabledOption : Microsoft.VisualStudio.Text.Editor.EditorOptionDefinition<bool>
	{
		public override Microsoft.VisualStudio.Text.Editor.EditorOptionKey<bool> Key => Microsoft.VisualStudio.Text.Editor.DefaultTextViewOptions.BraceCompletionEnabledOptionId;
		public override bool Default => true;
	}
}


