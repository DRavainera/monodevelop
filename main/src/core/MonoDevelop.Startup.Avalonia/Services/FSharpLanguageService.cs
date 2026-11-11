using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.CodeAnalysis.TransparentCompiler;
using FSharp.Compiler.Diagnostics;
using FSharp.Compiler.EditorServices;
using FSharp.Compiler.Symbols;
using FSharp.Compiler.Text;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Control;
using FRange = FSharp.Compiler.Text.Range;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// In-process F# language service on top of FCS 43.12 (the same FSharpChecker
/// version that the .NET 10 SDK's F# compiler is built from). Replaces the
/// legacy fsharpbinding's LanguageService.fs (which pinned FCS 31.0.0 with a
/// different API surface — GetDeclarationListSymbols, GetDeclarationLocation,
/// the internal FSharpWorkspace types) with the 43.12 editor-service contract
/// that was probed and verified for this port:
///
/// - Completions: GetDeclarationListInfo (the modern member-resolution path;
///   GetDeclarationListSymbols returns 0 items for member completion).
/// - Hover: GetSymbolUseAtLocation(line0, col0, lineText, nameIsland) —
///   0-BASED line and column — then GetDescription for the tooltip.
/// - Go-to-definition: FSharpSymbol.DeclarationLocation (FSharpOption&lt;Range&gt;,
///   Range.StartLine is 1-based, StartColumn is 0-based).
/// - Diagnostics: ParseAndCheckFileInProject per file, with an explicit
///   sourceFiles list covering the whole project so cross-file type
///   references resolve (ParseAndCheckProject does not expose per-file
///   results in 43.12).
///
/// Framework references are resolved the same way as a real build: FSharp.Core
/// comes from the service's own base directory (the NuGet package dependency),
/// and the System* reference assemblies come from the
/// Microsoft.NETCore.App.Ref pack under the dotnet root (~/.dotnet by
/// default). The implementation assemblies in the base directory and the TPA
/// list do NOT work as compiler references (the F# compiler resolves System
/// types from ref assemblies only) — both were probed and fail with FS39.
///
/// Thread model: all FCS calls run on a dedicated single-threaded executor
/// (FSharpChecker is not thread-safe), so hover/completion/check requests
/// serialize and never race. The UI thread only calls into the service and
/// gets back plain data.
/// </summary>
public sealed class FSharpLanguageService
{
	public static FSharpLanguageService Instance { get; } = new ();

	// The checker is created lazily on first use (not in a static ctor): the
	// FSharpChecker.Create call touches the FCS assembly's static initializers
	// and must not run at type-load time on the UI thread.
	readonly object gate = new ();
	FSharpChecker? checker;

	// All FCS calls run on this single-threaded executor so the checker is
	// never touched concurrently.
	readonly Executor executor = new ();

	// Per-document cache: path → state at last check. The check is triggered
	// from the editor's text-changed hook (debounced) and from on-demand
	// feature calls (hover, completion) which re-check when the text changed
	// since the last cache hit.
	readonly ConcurrentDictionary<string, DocState> docCache = new (StringComparer.OrdinalIgnoreCase);
	sealed class DocState
	{
		public string TextHash = "";
		public FSharpParseFileResults? Parse;
		public FSharpCheckFileResults? Check;
	}

	FSharpLanguageService () { }

	// Raised on the executor thread after a check completes (cache updated).
	// Subscribers (the MainWindow F# document wiring) marshal to the UI thread
	// to refresh the editor's inline diagnostic bubbles.
	public event Action<string, CheckResult>? CheckCompleted;

	void RaiseCheckCompleted (string file, CheckResult r)
	{
		CheckCompleted?.Invoke (file, r);
	}

	FSharpChecker Checker
	{
		get
		{
			if (checker is not null)
				return checker;
			lock (gate) {
				if (checker is null) {
					checker = FSharpChecker.Create (
						FSharpOption<int>.None, // projectCacheSize
						FSharpOption<bool>.None, // keepAssemblyContents
						FSharpOption<bool>.None, // keepAllBackgroundResolutions
						FSharpOption<LegacyReferenceResolver>.None, // legacyReferenceResolver (obsolete in 43.x)
						FSharpOption<FSharpFunc<Tuple<string, DateTime>, FSharpOption<Tuple<object, nint, int>>>>.None, // tryGetMetadataSnapshot
						FSharpOption<bool>.None, // suggestNamesForErrors
						FSharpOption<bool>.None, // keepAllBackgroundSymbolUses
						FSharpOption<bool>.None, // enableBackgroundItemKeyStoreAndSemanticClassification
						FSharpOption<bool>.Some (true), // enablePartialTypeChecking: type-check incrementally as the user types
						FSharpOption<bool>.None, // parallelReferenceResolution
						FSharpOption<bool>.None, // captureIdentifiersWhenParsing
						FSharpOption<DocumentSource>.None, // documentSource
						FSharpOption<bool>.None, // useTransparentCompiler
						FSharpOption<CacheSizes>.None); // transparentCompilerCacheSizes
					Console.WriteLine ("[fsls] FSharpChecker created (FCS " + typeof (FSharpChecker).Assembly.GetName ().Version + ")");
				}
			}
			return checker;
		}
	}

	// ---------------------------------------------------------------------
	// Reference assembly resolution
	// ---------------------------------------------------------------------

	// FSharp.Core.dll sits next to the shell binary (the FSharp.Compiler.Service
	// PackageReference copies it into main/build on restore).
	static string? FindFSharpCore ()
	{
		var baseDir = AppContext.BaseDirectory;
		var p = Path.Combine (baseDir, "FSharp.Core.dll");
		if (File.Exists (p))
			return p;
		var dir = new DirectoryInfo (baseDir);
		for (int i = 0; i < 4 && dir is not null; i++, dir = dir.Parent) {
			if (dir is null)
				break;
			p = Path.Combine (dir.FullName, "FSharp.Core.dll");
			if (File.Exists (p))
				return p;
		}
		Console.WriteLine ("[fsls] WARNING: FSharp.Core.dll not found near " + baseDir);
		return null;
	}

	// The .NET ref assemblies (System*.dll) from the Microsoft.NETCore.App.Ref
	// pack. These are the only reference set that lets the F# compiler resolve
	// System types cleanly (the implementation assemblies and the TPA list
	// both produce FS39 — probed). The highest pack version wins.
	static string[]? fwRefsCache;
	static readonly object fwLock = new ();
	static string[] FindFrameworkRefs ()
	{
		if (fwRefsCache is not null)
			return fwRefsCache;
		lock (fwLock) {
			if (fwRefsCache is not null)
				return fwRefsCache;
			var dirs = new List<string> ();
			var root = Environment.GetEnvironmentVariable ("DOTNET_ROOT");
			if (!string.IsNullOrEmpty (root) && Directory.Exists (root))
				dirs.Add (root);
			var home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			if (!string.IsNullOrEmpty (home))
				dirs.Add (Path.Combine (home, ".dotnet"));
			foreach (var d in dirs.Distinct ()) {
				var packs = Path.Combine (d, "packs", "Microsoft.NETCore.App.Ref");
				if (!Directory.Exists (packs))
					continue;
				// Highest pack version wins. Version strings are compared
				// NUMERICALLY (a plain string sort would put "8.0.30" above
				// "10.0.12" because "8" > "1").
				string? bestTfm = null;
				int bestMajor = -1, bestMinor = -1, bestBuild = -1;
				foreach (var verDir in Directory.EnumerateDirectories (packs)) {
					var parts = new DirectoryInfo (verDir).Name.Split ('.');
					if (parts.Length == 0 || !int.TryParse (parts [0], out var vMajor))
						continue;
					int vMinor = parts.Length > 1 && int.TryParse (parts [1], out var mm) ? mm : 0;
					int vBuild = parts.Length > 2 && int.TryParse (parts [2], out var bb) ? bb : 0;
					bool better = vMajor > bestMajor
						|| (vMajor == bestMajor && vMinor > bestMinor)
						|| (vMajor == bestMajor && vMinor == bestMinor && vBuild > bestBuild);
					if (!better)
						continue;
					var refRoot = Path.Combine (verDir, "ref");
					if (!Directory.Exists (refRoot))
						continue;
					foreach (var tfm in Directory.EnumerateDirectories (refRoot)
						.Where (x => new DirectoryInfo (x).Name.StartsWith ("net", StringComparison.OrdinalIgnoreCase))) {
						if (Directory.Exists (tfm) && Directory.GetFiles (tfm, "System*.dll").Length > 0) {
							bestTfm = tfm;
							bestMajor = vMajor; bestMinor = vMinor; bestBuild = vBuild;
						}
					}
				}
				if (bestTfm is not null) {
					fwRefsCache = Directory.GetFiles (bestTfm, "System*.dll");
					Console.WriteLine ("[fsls] framework refs: " + bestTfm + " (" + fwRefsCache.Length + " System*.dll)");
					return fwRefsCache;
				}
			}
			// Last resort: TPA list (works for some types, but the F# compiler
			// rejects it for most System types — still better than nothing).
			var tpa = (Environment.GetEnvironmentVariable ("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
				.Split (Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
			if (tpa.Length > 0) {
				fwRefsCache = tpa;
				Console.WriteLine ("[fsls] WARNING: ref pack not found; falling back to TPA (" + tpa.Length + " assemblies)");
				return fwRefsCache;
			}
			Console.WriteLine ("[fsls] WARNING: no framework references found");
			fwRefsCache = Array.Empty<string> ();
			return fwRefsCache;
		}
	}

	// ---------------------------------------------------------------------
	// Project options construction
	// ---------------------------------------------------------------------

	// Builds FSharpProjectOptions for the given file. sourceFiles is the full
	// project file list (so cross-file types resolve); scripts (.fsx/.fsscript)
	// get the script resolution rules so their top-level expressions type-check.
	static FSharpProjectOptions MakeOptions (string file, string[] sourceFiles, bool isScript)
	{
		var core = FindFSharpCore ();
		var refs = FindFrameworkRefs ();
		var other = new List<string> (refs.Length + 1);
		if (core is not null)
			other.Add ("-r:" + core);
		other.AddRange (refs.Select (r => "-r:" + r));
		return new FSharpProjectOptions (
			Path.GetDirectoryName (file) ?? file,
			FSharpOption<string>.Some (file),
			sourceFiles,
			other.ToArray (),
			Array.Empty<FSharpReferencedProject> (),
			isIncompleteTypeCheckEnvironment: false,
			useScriptResolutionRules: isScript,
			loadTime: DateTime.Now,
			FSharpOption<FSharpUnresolvedReferencesSet>.None,
			FSharpList<Tuple<FRange, string, string>>.Empty,
			FSharpOption<long>.None);
	}

	static bool IsFSharpScript (string file)
	{
		var ext = Path.GetExtension (file).ToLowerInvariant ();
		return ext is ".fsx" or ".fsscript";
	}

	// ---------------------------------------------------------------------
	// Check (the core operation)
	// ---------------------------------------------------------------------

	public sealed record CheckResult (bool Ok, string Error, FSharpParseFileResults? Parse, FSharpCheckFileResults? Check);

	// Runs ParseAndCheckFileInProject and caches the result by (path, text
	// hash). When textOverride is given (the editor's in-memory text, which may
	// differ from disk for unsaved edits) it is the text that is checked;
	// otherwise the file is read from disk. This overload BLOCKS until the check
	// completes — use it for on-demand features (hover, completion) where the
	// result is needed immediately, after the document has been pre-loaded.
	public CheckResult CheckFile (string file, string[] sourceFiles, string? textOverride = null)
	{
		string text;
		if (textOverride is not null) {
			text = textOverride;
		} else {
			try {
				if (!File.Exists (file))
					return new CheckResult (false, "file not found: " + file, null, null);
				text = File.ReadAllText (file);
			} catch (Exception e) {
				return new CheckResult (false, e.Message, null, null);
			}
		}
		try {
			return executor.Invoke (() => CoreCheck (file, sourceFiles, text));
		} catch (Exception e) {
			Console.WriteLine ("[fsls] check failed for " + file + ": " + e.Message);
			return new CheckResult (false, e.Message, null, null);
		}
	}

	// Convenience overload: the sourceFiles default to just [file] (scripts and
	// standalone files). The MainWindow wiring passes the full project file
	// list for project members.
	public CheckResult CheckFile (string file) => CheckFile (file, new[] { file });

	// True when a completed check is cached for the file (the background
	// pre-load has finished). The QA pipeline polls this to wait for the warm
	// cache instead of blocking the UI thread on the first (slow) check.
	public bool IsChecked (string file)
	{
		return docCache.TryGetValue (file, out var st)
			&& st is not null && st.Check is not null;
	}

	// Non-blocking pre-load: warms the document cache on a background thread so
	// that a later hover/completion hits the cache instead of paying for the
	// (slow, first-time) check on the UI thread. Fire-and-forget — the caller
	// does not wait. With textOverride (the editor's live buffer) the cache is
	// kept warm against UNSAVED edits, so a feature fired right after typing
	// hits the cache instead of doing a blocking re-check on the UI thread.
	public void QueueCheck (string file, string[] sourceFiles, string? textOverride = null)
	{
		executor.Enqueue (() => {
			try {
				var text = textOverride;
				if (text is null) {
					if (!File.Exists (file))
						return;
					text = File.ReadAllText (file);
				}
				CoreCheck (file, sourceFiles, text);
			} catch (Exception e) {
				Console.WriteLine ("[fsls] precheck failed for " + file + ": " + e.Message);
			}
		});
	}

	// The actual check, run ON the single-threaded executor. Shared by the
	// blocking CheckFile and the fire-and-forget QueueCheck; manages the cache.
	CheckResult CoreCheck (string file, string[] sourceFiles, string text)
	{
		var hash = TextHash (text);
		var st = docCache.GetOrAdd (file, _ => new DocState ());
		if (string.Equals (st.TextHash, hash, StringComparison.Ordinal) && st.Check is not null)
			return new CheckResult (true, "", st.Parse, st.Check);

		var opts = MakeOptions (file, sourceFiles, IsFSharpScript (file));
		var c = Checker;
		var fa = c.ParseAndCheckFileInProject (file, 0, SourceText.ofString (text), opts, FSharpOption<string>.None);
		var t = FSharpAsync.RunSynchronously (fa, FSharpOption<int>.Some (60000), FSharpOption<CancellationToken>.None);
		if (t.Item2 is not FSharpCheckFileAnswer.Succeeded succ)
			return new CheckResult (false, "check did not succeed: " + t.Item2, null, null);
		var result = new CheckResult (true, "", t.Item1, succ.Item);
		st.TextHash = hash;
		st.Parse = result.Parse;
		st.Check = result.Check;
		RaiseCheckCompleted (file, result);
		return result;
	}

	static string TextHash (string s)
	{
		// FNV-1a: cheap and good enough for change detection.
		unchecked {
			uint h = 2166136261;
			foreach (var c in s)
				h = (h ^ c) * 16777619;
			return h.ToString ("x8") + ":" + s.Length;
		}
	}

	// ---------------------------------------------------------------------
	// Diagnostics
	// ---------------------------------------------------------------------

	// The F# diagnostics of a check result, with the "startup" pseudo-file's
	// entries filtered out (those are the FSI auxlib's own resolution messages,
	// not the user's code). Line is converted to 0-based for the editor; the
	// column is ALREADY 0-based in F#'s FRange (verified: `let x : int = "hola"`
	// reports StartColumn=14, the 0-based column of the opening quote).
	public List<(string File, int Line, int Col, string Level, string Code, string Message)> GetDiagnostics (CheckResult r)
	{
		var out2 = new List<(string, int, int, string, string, string)> ();
		if (r is not { Ok: true, Check: not null })
			return out2;
		foreach (var d in r.Check.Diagnostics) {
			if (string.Equals (d.FileName, "startup", StringComparison.OrdinalIgnoreCase))
				continue;
			var level = d.Severity == FSharpDiagnosticSeverity.Error ? "error" :
				d.Severity == FSharpDiagnosticSeverity.Warning ? "warning" : "info";
			var code = (d.ErrorNumberPrefix ?? "") + d.ErrorNumber;
			out2.Add ((d.FileName, d.StartLine - 1, d.StartColumn, level, code, d.Message));
		}
		return out2;
	}

	// ---------------------------------------------------------------------
	// Completions
	// ---------------------------------------------------------------------

	public sealed record CompletionItem (string NameInCode, string IconStock, string Description, string Category, string? FullName);

	// Returns the completion items for the caret at (line0, col0) in the given
	// file. lineText MUST be the actual content of line line0 in the file
	// (GetDeclarationListInfo uses it to re-parse the line; a mismatched
	// lineText silently returns 0 items — probed).
	public List<CompletionItem> GetCompletions (string file, string[] sourceFiles, int line0, int col0, string lineText, string? textOverride = null)
	{
		var r = CheckFile (file, sourceFiles, textOverride);
		if (r is not { Ok: true, Check: not null })
			return new List<CompletionItem> ();
		var parse = r.Parse;
		PartialLongName partial;
		try {
			var idx = Math.Clamp (col0, 0, lineText.Length);
			partial = QuickParse.GetPartialLongNameEx (lineText, idx);
		} catch {
			return new List<CompletionItem> ();
		}
		try {
			var info = executor.Invoke (() =>
				r.Check!.GetDeclarationListInfo (
					parse is null ? FSharpOption<FSharpParseFileResults>.None : FSharpOption<FSharpParseFileResults>.Some (parse),
					line0, lineText, partial,
					FSharpOption<FSharpFunc<Unit, FSharpList<AssemblySymbol>>>.None,
					FSharpOption<Tuple<Position, FSharpOption<CompletionContext>>>.None,
					FSharpOption<FSharpCodeCompletionOptions>.None));
			var out2 = new List<CompletionItem> ();
			foreach (var it in info.Items) {
				if (string.IsNullOrEmpty (it.NameInCode))
					continue;
				out2.Add (new CompletionItem (
					it.NameInCode,
					GlyphToIcon (it.Glyph),
					ToolTipToText (it.Description),
					KindToCategory (it.Glyph),
					it.FullName));
			}
			// De-duplicate by NameInCode (the F# service can return the same
			// symbol twice for overloads in some cases).
			return out2.DistinctBy (i => i.NameInCode).Take (200).ToList ();
		} catch (Exception e) {
			Console.WriteLine ("[fsls] completion failed: " + e.Message);
			return new List<CompletionItem> ();
		}
	}

	static string GlyphToIcon (FSharpGlyph? g)
	{
		// Reuses the C# element-* icon names (the same stock the C# hover uses)
		// so the completion popup and tooltip do not need new assets.
		if (g is null)
			return "element-other-declaration-16";
		return g.IsMethod || g.IsOverridenMethod || g.IsExtensionMethod ? "element-method-16"
			: g.IsField || g.IsVariable || g.IsConstant ? "element-field-16"
			: g.IsProperty ? "element-property-16"
			: g.IsClass || g.IsInterface || g.IsStruct || g.IsType || g.IsUnion || g.IsEnum || g.IsTypedef ? "element-class-16"
			: g.IsModule || g.IsNameSpace ? "element-keyword-16"
			: "element-other-declaration-16";
	}

	static string KindToCategory (FSharpGlyph? g)
	{
		if (g is null)
			return "F# symbol";
		return g.IsMethod || g.IsOverridenMethod || g.IsExtensionMethod ? "F# function"
			: g.IsField || g.IsVariable || g.IsConstant ? "F# value"
			: g.IsProperty ? "F# property"
			: g.IsClass || g.IsInterface || g.IsStruct || g.IsType || g.IsUnion || g.IsEnum ? "F# type"
			: g.IsModule ? "F# module"
			: "F# symbol";
	}

	// ---------------------------------------------------------------------
	// Hover
	// ---------------------------------------------------------------------

	public sealed record HoverInfo (string Header, string Description, string Icon, FSharpSymbol? Symbol);

	// Returns the hover info for the word under (line0, col0). The word (and
	// its dot-separated island, e.g. "A.makePoint") is extracted from lineText
	// around col0, then GetSymbolUseAtLocation resolves it against the check
	// results. col0 is 0-based (the editor caret convention).
	public HoverInfo? GetHover (string file, string[] sourceFiles, int line0, int col0, string lineText, string? textOverride = null)
	{
		var r = CheckFile (file, sourceFiles, textOverride);
		if (r is not { Ok: true, Check: not null })
			return null;
		var island = IdentifierIsland (lineText, col0);
		if (string.IsNullOrEmpty (island))
			return null;
		var names = BuildNameList (island);
		try {
			var use = executor.Invoke (() => r.Check!.GetSymbolUseAtLocation (line0, col0, lineText, names));
			if (!FSharpOption<FSharpSymbolUse>.get_IsSome (use))
				return null;
			var u = use.Value;
			var tip = r.Check.GetDescription (u.Symbol, FSharpList<Tuple<FSharpGenericParameter, FSharpType>>.Empty, false, u.Range);
			return new HoverInfo (u.Symbol.DisplayName, ToolTipToText (tip), GlyphOfSymbol (u.Symbol), u.Symbol);
		} catch (Exception e) {
			Console.WriteLine ("[fsls] hover failed: " + e.Message);
			return null;
		}
	}

	// Walks left/right over identifier characters and dots to extract the full
	// dot-separated island at the cursor (e.g. cursor on "makePoint" in
	// "A.makePoint" returns "A.makePoint"). A caret just past the last char of
	// a word steps back onto it (the hover timer fires at the pointer col,
	// which can sit right after the word).
	static string IdentifierIsland (string lineText, int col0)
	{
		if (string.IsNullOrEmpty (lineText))
			return "";
		int c = Math.Clamp (col0, 0, lineText.Length);
		if (c > 0 && c < lineText.Length && char.IsWhiteSpace (lineText [c]) && IsIdentChar (lineText [c - 1]))
			c--;
		int s = c;
		while (s > 0 && (IsIdentChar (lineText [s - 1]) || lineText [s - 1] == '.'))
			s--;
		int e = c;
		while (e < lineText.Length && IsIdentChar (lineText [e]))
			e++;
		var raw = lineText.Substring (s, e - s);
		// Trim trailing dots (caret right after "x." → island "x." → "x").
		return raw.TrimEnd ('.');
	}

	static bool IsIdentChar (char c) => char.IsLetterOrDigit (c) || c == '_';

	// Splits the dot-separated island into the FSharpList<string> of
	// qualifying names (matches the legacy findIdents: sym.Text.Split('.')).
	static FSharpList<string> BuildNameList (string island)
	{
		var parts = island.Split ('.', StringSplitOptions.RemoveEmptyEntries);
		FSharpList<string> l = FSharpList<string>.Empty;
		for (int i = parts.Length - 1; i >= 0; i--)
			l = new FSharpList<string> (parts [i], l);
		return l;
	}

	// A best-effort icon for a symbol (FSharpSymbol does not expose its glyph
	// directly in 43.12; uppercase → type, lowercase → value/function).
	static string GlyphOfSymbol (FSharpSymbol s)
	{
		var n = s.DisplayName ?? "";
		return n.Length == 0 ? "element-other-declaration-16"
			: char.IsUpper (n [0]) ? "element-class-16"
			: "element-method-16";
	}

	// ---------------------------------------------------------------------
	// Document adornments (Preferences "F# Settings" consumers, phase 6)
	// ---------------------------------------------------------------------

	// One rendered adornment over a declaration's binding name: "mutable"
	// (underline, the HighlightMutables option) or "type" (a small right-aligned
	// type-signature, the ShowTypeSignatures option). Line/Col are 0-based
	// (StartLine-1 / StartColumn) to match the editor's caret convention.
	public sealed record FSharpAdornment (int Line, int Col, int Length, string Kind, string Text);

	// The adornments for every top-level declaration of the file, computed on
	// the (single-threaded) executor from GetAllUsesOfAllSymbolsInFile's
	// definition uses. IsMutable drives the underline; functions carry their
	// FullTypeSafe as a type signature. This overload is for on-demand callers on
	// the UI thread (it checks, then computes, both serialized on the executor).
	public List<FSharpAdornment> GetFileAdornments (string file, string[] sourceFiles, string? textOverride = null)
	{
		var r = CheckFile (file, sourceFiles, textOverride);
		return executor.Invoke (() => ComputeAdornments (r));
	}

	// The adornment computation proper. Runs the FCS enumeration directly with NO
	// executor re-entry: the CheckCompleted event fires from inside CoreCheck,
	// i.e. ON the executor thread, where the MainWindow wiring calls this to keep
	// the editor's adornments in sync (an executor.Invoke there would deadlock).
	internal List<FSharpAdornment> ComputeAdornments (CheckResult r)
	{
		var out2 = new List<FSharpAdornment> ();
		if (r is not { Ok: true, Check: not null })
			return out2;
		try {
			var defs = r.Check!.GetAllUsesOfAllSymbolsInFile (FSharpOption<CancellationToken>.None)
				.Where (u => u.IsFromDefinition).ToList ();
			foreach (var u in defs) {
				var sym = u.Symbol;
				var st = sym.GetType ();
				var isFunction = (bool)(st.GetProperty ("IsFunction")?.GetValue (sym) ?? false);
				var isMutable = (bool)(st.GetProperty ("IsMutable")?.GetValue (sym) ?? false);
				var range = u.Range;
				var line = range.StartLine - 1; // 0-based for the editor
				var col = range.StartColumn;    // already 0-based
				var len = Math.Max (1, range.EndColumn - range.StartColumn);
				if (isMutable)
					out2.Add (new FSharpAdornment (line, col, len, "mutable", ""));
				if (isFunction) {
					var fts = st.GetProperty ("FullTypeSafe")?.GetValue (sym) as FSharpOption<FSharpType>;
					if (FSharpOption<FSharpType>.get_IsSome (fts))
						out2.Add (new FSharpAdornment (line, col, len, "type", FormatFSharpType (fts.Value)));
				}
			}
		} catch (Exception e) {
			Console.WriteLine ("[fsls] adornments failed: " + e.Message);
		}
		return out2;
	}

	// "type Microsoft.FSharp.Core.int -> Microsoft.FSharp.Core.int" → "int -> int"
	// (the type-signature display: drop the F# "type" prefix and the FSharp.Core
	// qualification so the gutter reads like the legacy TypeSignatureHelp).
	static string FormatFSharpType (FSharpType t)
	{
		var s = t.ToString () ?? "";
		if (s.StartsWith ("type ", StringComparison.Ordinal))
			s = s.Substring (5);
		s = s.Replace ("Microsoft.FSharp.Core.", "", StringComparison.Ordinal);
		return s.Trim ();
	}

	// ---------------------------------------------------------------------
	// Go-to-definition / find-references
	// ---------------------------------------------------------------------

	// Returns (file, line0, col0) for the symbol under (line0, col0), or null.
	// The line is converted from F#'s 1-based StartLine to the editor's
	// 0-based; the column is already 0-based in F#'s Range.
	public (string File, int Line, int Col)? GetGotoDefinition (string file, string[] sourceFiles, int line0, int col0, string lineText, string? textOverride = null)
	{
		var r = CheckFile (file, sourceFiles, textOverride);
		if (r is not { Ok: true, Check: not null })
			return null;
		var island = IdentifierIsland (lineText, col0);
		if (string.IsNullOrEmpty (island))
			return null;
		var names = BuildNameList (island);
		try {
			var use = executor.Invoke (() => r.Check!.GetSymbolUseAtLocation (line0, col0, lineText, names));
			if (!FSharpOption<FSharpSymbolUse>.get_IsSome (use))
				return null;
			var loc = use.Value.Symbol.DeclarationLocation;
			if (!FSharpOption<FRange>.get_IsSome (loc))
				return null;
			var range = loc.Value;
			return (range.FileName, range.StartLine - 1, range.StartColumn);
		} catch (Exception e) {
			Console.WriteLine ("[fsls] goto-def failed: " + e.Message);
			return null;
		}
	}

	public sealed record SymbolReference (string File, int Line, int Col, bool IsDefinition, string Text);

	// Returns all uses of the symbol under (line0, col0) in the same file
	// (cross-file references would need the project-wide results, which 43.12
	// does not expose per file).
	public List<SymbolReference> GetReferences (string file, string[] sourceFiles, int line0, int col0, string lineText, string? textOverride = null)
	{
		var out2 = new List<SymbolReference> ();
		var r = CheckFile (file, sourceFiles, textOverride);
		if (r is not { Ok: true, Check: not null })
			return out2;
		var island = IdentifierIsland (lineText, col0);
		if (string.IsNullOrEmpty (island))
			return out2;
		var names = BuildNameList (island);
		try {
			var uses = executor.Invoke (() => {
				var use = r.Check!.GetSymbolUseAtLocation (line0, col0, lineText, names);
				if (!FSharpOption<FSharpSymbolUse>.get_IsSome (use))
					return Array.Empty<FSharpSymbolUse> ();
				return r.Check.GetUsesOfSymbolInFile (use.Value.Symbol,
					FSharpOption<RelatedSymbolUseKind>.None, FSharpOption<CancellationToken>.None);
			});
			foreach (var u in uses) {
				if (!string.Equals (u.FileName, file, StringComparison.OrdinalIgnoreCase))
					continue;
				out2.Add (new SymbolReference (u.FileName, u.Range.StartLine - 1, u.Range.StartColumn,
					u.IsFromDefinition, ""));
			}
			return out2;
		} catch (Exception e) {
			Console.WriteLine ("[fsls] find-refs failed: " + e.Message);
			return out2;
		}
	}

	// ---------------------------------------------------------------------
	// ToolTipText → plain text
	// ---------------------------------------------------------------------

	// Extracts the human-readable text from an FCS ToolTipText (a list of
	// ToolTipElement groups; each group's data has MainDescription and
	// SecondaryDescription TaggedText runs). Composition errors are rendered
	// with their message.
	static string ToolTipToText (ToolTipText? tip)
	{
		if (tip is null)
			return "";
		var sb = new System.Text.StringBuilder ();
		try {
			foreach (var el in tip.Item) {
				if (el.IsGroup) {
					var g = (ToolTipElement.Group) (object) el;
					foreach (var d in g.elements) {
						AppendTagged (sb, d.MainDescription);
					}
				} else if (el.IsCompositionError) {
					var ce = (ToolTipElement.CompositionError) (object) el;
					sb.Append (ce.errorText);
				}
			}
		} catch {
			// The ToolTipText internal shape can vary across FCS patch
			// versions; a best-effort ToString is the fallback.
			sb.Append (tip.ToString ());
		}
		return sb.ToString ().Trim ();
	}

	static void AppendTagged (System.Text.StringBuilder sb, TaggedText[]? texts)
	{
		if (texts is null)
			return;
		foreach (var t in texts)
			sb.Append (t.Text);
	}

	// ---------------------------------------------------------------------
	// Single-threaded executor for the FCS calls
	// ---------------------------------------------------------------------

	// A minimal, lock-free-ish single-consumer executor: the UI thread posts
	// work and blocks on a per-call TCS; the worker thread runs it. FSharpChecker
	// is not thread-safe, so this serializes every FCS call.
	sealed class Executor
	{
		sealed class Job
		{
			public Action Run = () => { };
			public TaskCompletionSource<object?> Done = new (TaskCreationOptions.RunContinuationsAsynchronously);
		}

		readonly BlockingCollection<Job> queue = new ();
		readonly Thread worker;

		public Executor ()
		{
			worker = new Thread (Loop) { IsBackground = true, Name = "FSharpLanguageService" };
			worker.Start ();
		}

		public T Invoke<T> (Func<T> f)
		{
			var job = new Job ();
			job.Run = () => {
				try {
					job.Done.TrySetResult (f ());
				} catch (Exception e) {
					job.Done.TrySetException (e);
				}
			};
			queue.Add (job);
			var box = job.Done.Task.GetAwaiter ().GetResult ();
			return (T) box!;
		}

		public void Invoke (Action a) => Invoke<bool> (() => { a (); return true; });

		// Fire-and-forget: posts work to the single-threaded executor without
		// blocking the caller. Used for the background pre-load (QueueCheck).
		public void Enqueue (Action a)
		{
			var job = new Job ();
			job.Run = () => a ();
			queue.Add (job);
		}

		void Loop ()
		{
			foreach (var job in queue.GetConsumingEnumerable ()) {
				try {
					job.Run ();
				} catch (Exception e) {
					// Never let the worker die on a handler exception.
					job.Done.TrySetException (e);
					Console.WriteLine ("[fsls] executor handler failed: " + e);
				}
			}
		}
	}
}
