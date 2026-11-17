using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Minimal Roslyn workspace for the preferences live preview (F2 infra). The legacy
/// AbstractOptionPreviewViewModel builds a PreviewWorkspace, adds the snippet as a
/// C# document and runs Formatter.FormatAsync(document, options) to normalize the
/// formatting before showing it. This shell has no MonoDevelopWorkspace/MEF, so it
/// hosts an AdhocWorkspace (same Roslyn 5.9.0 + MonoRoslynCompat closure the GTK UI
/// loads from main/build) and runs the very same Formatter call on the same snippet,
/// so the preview text matches what the GTK editor renders.
/// </summary>
sealed class RoslynPreviewService : IDisposable
{
	AdhocWorkspace workspace;

	/// <summary>
	/// Formats a C# snippet (the legacy truePreview/falsePreview/previews[] text,
	/// including its //[ ... //] marker lines) and returns the formatted text with
	/// the marker lines removed — the exact thing the preview editor shows.
	/// </summary>
	public string Format (string code, OptionSet options, CancellationToken ct)
	{
		var ws = EnsureWorkspace ();
		var project = ws.CurrentSolution.AddProject ("preview", "preview.dll", LanguageNames.CSharp);
		var doc = project.AddDocument ("preview.cs", SourceText.From (code, Encoding.UTF8));
		// The legacy UpdatePreview blocks on the same work (Formatter.FormatAsync
		// .WaitAndGetResult); the snippet is tiny so the synchronous wait on the UI
		// thread is the faithful equivalent. VSTHRD002 (sync-over-async) is expected.
#pragma warning disable VSTHRD002
		var formatted = Formatter.FormatAsync (doc, options ?? ws.Options, ct).GetAwaiter ().GetResult ();
		var text = formatted.GetTextAsync (ct).GetAwaiter ().GetResult ().ToString ();
#pragma warning restore VSTHRD002
		return StripPreviewMarkers (text);
	}

	AdhocWorkspace EnsureWorkspace ()
	{
		if (workspace is null)
			workspace = new AdhocWorkspace (CoreOnlyHostServices (), "Preview");
		return workspace;
	}

	// Build the Roslyn HostServices composing ONLY the four core assemblies (Common,
	// CSharp, Workspaces, CSharp.Workspaces). The C# formatter lives in CSharp.Workspaces
	// (the core), NOT in Microsoft.CodeAnalysis.Features, so this is enough to run
	// Formatter.FormatAsync. Restricting the composition to the core assemblies avoids
	// MEF-discovering the editor *Features* that live next to us in main/build — those
	// reference Microsoft.Bcl.AsyncInterfaces 10.0.0.1, which is not present in the shell
	// closure, and would otherwise throw at workspace creation. This keeps the preview
	// self-contained without dragging the full editor feature set (or its BCL deps) into
	// the shared main/build closure that the GTK --old-gui path also loads.
	static MefHostServices CoreOnlyHostServices ()
	{
		var names = new[] {
			"Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp",
			"Microsoft.CodeAnalysis.Workspaces", "Microsoft.CodeAnalysis.CSharp.Workspaces"
		};
		var assemblies = new System.Collections.Generic.List<System.Reflection.Assembly> ();
		foreach (var name in names) {
			var a = System.Reflection.Assembly.Load (name);
			if (a != null)
				assemblies.Add (a);
		}
		return MefHostServices.Create (assemblies);
	}

	// The legacy UpdatePreview deletes the "//[" and "//]" marker lines from the
	// formatted text before showing it. The formatter leaves those comment lines
	// untouched, so a line whose trimmed content is a marker is dropped.
	static string StripPreviewMarkers (string text)
	{
		var lines = text.Split ('\n');
		var sb = new StringBuilder ();
		for (int i = 0; i < lines.Length; i++) {
			var t = lines[i].Trim ();
			if (t == "//[" || t == "//]")
				continue;
			sb.Append (lines[i]);
			if (i < lines.Length - 1)
				sb.Append ('\n');
		}
		return sb.ToString ();
	}

	public void Dispose ()
	{
		workspace?.Dispose ();
		workspace = null;
	}
}
