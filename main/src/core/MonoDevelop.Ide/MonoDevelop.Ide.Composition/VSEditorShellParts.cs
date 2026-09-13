using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Utilities;

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