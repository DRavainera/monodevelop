using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Port of the legacy Roslyn-backed symbol surfaces (RoslynSearchCategory,
/// DocumentOutlinePad, ClassPad) onto a cheap anchored-regex scan of the C#
/// sources. The shell has no Roslyn compilation, so declarations are recovered
/// from the text: one pass yields type and member hits (field, event, property,
/// method) with their line and INDENT, and the enclosing scope chain falls out
/// of an indentation stack.
///
/// Three consumers share this scanner: the editor breadcrumb, Go To Type and the
/// search popup (flat <see cref="SymbolHit"/> list), the Document Outline pad
/// (<see cref="BuildOutline"/>) and the Classes pad (<see cref="BuildClassTree"/>).
/// </summary>
public static class SymbolIndexService
{
	public sealed record SymbolHit (string Kind, string Name, int Line, int Indent, string? Container);

	/// <summary>Nested outline node: the same hits as <see cref="SymbolHit"/> but
	/// arranged as a tree (children are the hits declared inside the node).</summary>
	public sealed record OutlineNode (string Kind, string Name, int Line, int Indent, List<OutlineNode> Children);

	/// <summary>Classes-pad node. <c>Kind</c> is one of project, namespace, class,
	/// interface, struct, enum, record, field, event, property, method; <c>File</c>/<c>Line</c>
	/// are null for the grouping levels (project, namespace).</summary>
	public sealed record ClassNode (string Kind, string Name, string? File, int Line, List<ClassNode> Children);

	// Declaration patterns run on the INDENT-STRIPPED line. The old patterns ran on
	// the raw line and let the leading whitespace be eaten by the type character
	// class (\s), so an indented `if (true)` was read as a method named "if". The
	// indent is measured separately and only feeds the scope stack.
	static readonly Regex typeDeclRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|sealed|abstract|partial|readonly|ref|unsafe|new)\s+)*((?:class|interface|struct|enum)\s+|record\s+(?:(?:class|struct)\s+)?)([A-Za-z_][A-Za-z0-9_]*)",
		RegexOptions.Compiled);
	static readonly Regex eventDeclRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|virtual|override|sealed|abstract|new|unsafe)\s+)*\bevent\s+[\w<>\[\],\.\?]+(?:\s*\[\s*\])*\s+([A-Za-z_][A-Za-z0-9_]*)",
		RegexOptions.Compiled);
	// Method: <type> <name> (...). The closing ")" is NOT required so multi-line
	// parameter lists still register; the type tokens must not contain a statement
	// keyword (`throw new X ();`, `yield return X ();`, `return Foo (x);` are calls).
	static readonly Regex methodDeclRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|async|virtual|override|sealed|abstract|partial|readonly|extern|unsafe|ref|new)\s+)*([\w<>\[\],\.\?]+(?:\s+[\w<>\[\],\.\?]+)*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>]*>)?\s*\(",
		RegexOptions.Compiled);
	static readonly Regex propertyDeclRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|async|virtual|override|sealed|abstract|partial|readonly|required|new)\s+)*([\w<>\[\],\.\?]+(?:\s+[\w<>\[\],\.\?]+)*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\{|=>)",
		RegexOptions.Compiled);
	// Indexer: <type> this [params] { get; set; }  (Roslyn exposes it as a property).
	static readonly Regex indexerDeclRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|virtual|override|sealed|abstract|readonly|new|unsafe)\s+)*([\w<>\[\],\.\?]+(?:\s+[\w<>\[\],\.\?]+)*)\s+this\s*\[",
		RegexOptions.Compiled);
	// Field: <type> <name> [= init] ;  (modifiers optional — `int Field;` is valid).
	static readonly Regex fieldDeclRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|readonly|const|volatile|new|required|unsafe)\s+)*([\w<>\[\],\.\?]+(?:\s+\[\s*\])*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=[^;]*)?;",
		RegexOptions.Compiled);
	// Allman property: `public int Prop` with the `{` on the NEXT line (the
	// same-line rule above would otherwise drop it). Only tried at type level,
	// after the other patterns, and only when the line has no other token.
	static readonly Regex allmanPropertyRegex = new (
		@"^(?:\[[^\]]*\]\s*)*((?:public|private|protected|internal|static|async|virtual|override|sealed|abstract|partial|readonly|required|new)\s+)*([\w<>\[\],\.\?]+(?:\s+\[\s*\])*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*$",
		RegexOptions.Compiled);
	static readonly Regex namespaceDeclRegex = new (
		@"^[ \t]*namespace\s+([A-Za-z_][A-Za-z0-9_\.]*)",
		RegexOptions.Compiled);

	// A bare identifier in front of `(`/`;`/`{` that is really a statement must not
	// become a member (`return Foo (x);`, `using System;`, `if (x)` ...).
	static readonly HashSet<string> statementKeywords = new (StringComparer.Ordinal) {
		"if", "else", "while", "for", "foreach", "switch", "case", "default", "do",
		"try", "catch", "finally", "using", "lock", "return", "throw", "yield",
		"await", "goto", "break", "continue", "checked", "unchecked", "fixed",
		"this", "base", "new", "var", "namespace", "delegate", "class", "interface",
		"struct", "enum", "in", "is", "as", "out", "when", "where", "select", "from",
		"let", "orderby", "join", "on", "equals", "into", "stackalloc", "sizeof",
		"typeof", "nameof",
	};

	// Per-file cache keyed by last-write timestamp: the Classes pad and the search
	// popup rescan the whole solution, so re-reading unchanged files is wasteful.
	static readonly Dictionary<string, (DateTime Stamp, List<SymbolHit> Symbols)> cache = new ();

	public static void ClearCache () => cache.Clear ();

	/// <summary>One pass over C# source: type declarations plus members (field,
	/// event, property, method), each with its line and INDENT — the container chain
	/// (breadcrumb scope, Go To Type detail) falls out of an indentation stack.</summary>
	public static List<SymbolHit> ScanSymbols (string text)
		=> ScanSymbols (text, false);

	/// <summary>Language-aware symbol scan. For F# sources, a simpler scanner
	/// extracts <c>let</c> bindings, <c>type</c> declarations and <c>module</c>
	/// declarations (the C# regexes do not match F# syntax).</summary>
	public static List<SymbolHit> ScanSymbols (string text, bool isFSharp)
	{
		if (isFSharp)
			return ScanFSharpSymbols (text);
		var hits = new List<SymbolHit> ();
		var stack = new List<(int Indent, string Name, bool IsType)> (); // enclosing scopes
		bool inBlockComment = false, inVerbatimString = false, inRawString = false;
		int ln = 0;
		foreach (var raw in text.Split ('\n')) {
			ln++;
			var line = raw.TrimEnd ('\r');
			// A line that STARTS inside a block comment or string literal is
			// skipped whole, so comment/string bodies never become symbols.
			bool startedInComment = inBlockComment || inVerbatimString || inRawString;
			UpdateLexState (line, ref inBlockComment, ref inVerbatimString, ref inRawString);
			if (startedInComment)
				continue;

			var trimmed = line.TrimStart ();
			if (trimmed.Length == 0 || trimmed.StartsWith ("//", StringComparison.Ordinal))
				continue;
			var indent = line.Length - trimmed.Length;
			// A lone opening brace (Allman style) sits at the SAME indent as its
			// owner — popping scopes on it would evict the type pushed by the very
			// previous line (Main/Double lost their container that way); only a
			// CLOSING brace pops its own scope.
			if (trimmed.StartsWith ("{", StringComparison.Ordinal))
				continue;
			if (trimmed.StartsWith ("}", StringComparison.Ordinal)) {
				PopScopes (indent);
				continue;
			}
			PopScopes (indent);
			var tm = typeDeclRegex.Match (trimmed);
			if (tm.Success) {
				var container = stack.Count > 0 ? stack [^1].Name : null;
				var typeName = tm.Groups [3].Value;
				var kindKeyword = TypeKeyword (tm.Groups [2].Value);
				hits.Add (new SymbolHit (kindKeyword, typeName, ln, indent, container));
				stack.Add ((indent, typeName, true));
				continue;
			}
			string? kind = null, name = null;
			var mm = methodDeclRegex.Match (trimmed);
			if (mm.Success && !IsStatementKeyword (mm.Groups [2].Value)) {
				kind = "method"; name = mm.Groups [3].Value;
			}
			if (kind is null) {
				var em = eventDeclRegex.Match (trimmed);
				if (em.Success) {
					kind = "event"; name = em.Groups [2].Value;
				}
			}
			if (kind is null) {
				var pm = propertyDeclRegex.Match (trimmed);
				if (pm.Success && !IsStatementKeyword (pm.Groups [2].Value)) {
					kind = "property"; name = pm.Groups [3].Value;
				}
			}
			if (kind is null) {
				var xm = indexerDeclRegex.Match (trimmed);
				if (xm.Success && !IsStatementKeyword (xm.Groups [2].Value)) {
					kind = "property"; name = "this[]";
				}
			}
			// Fields only exist at type level: a "const int Max = 10;" inside a
			// method body has the same shape and must not become a member.
			bool atTypeLevel = stack.Count > 0 && stack [^1].IsType;
			if (kind is null && atTypeLevel) {
				var fm = fieldDeclRegex.Match (trimmed);
				if (fm.Success && !IsStatementKeyword (fm.Groups [2].Value)) {
					kind = "field"; name = fm.Groups [3].Value;
				}
			}
			if (kind is null && atTypeLevel) {
				var am = allmanPropertyRegex.Match (trimmed);
				if (am.Success && !IsStatementKeyword (am.Groups [2].Value)) {
					kind = "property"; name = am.Groups [3].Value;
				}
			}
			if (kind is null)
				continue;
			var mContainer = stack.Count > 0 ? stack [^1].Name : null;
			hits.Add (new SymbolHit (kind, name!, ln, indent, mContainer));
			stack.Add ((indent, name!, false));
		}
		return hits;

		void PopScopes (int indent)
		{
			while (stack.Count > 0 && stack [^1].Indent >= indent)
				stack.RemoveAt (stack.Count - 1);
		}
	}

	// `class ` / `interface ` → keyword; `record ` / `record class ` / `record
	// struct ` all collapse to the same type kind (Roslyn exposes them as types).
	static string TypeKeyword (string clause)
	{
		var t = clause.Trim ();
		return t.StartsWith ("record", StringComparison.Ordinal) ? "record" : t;
	}

	// F# declaration scanner: extracts `module NAME`, `type NAME` and
	// `let NAME` top-level declarations. F# has no indentation-based scoping
	// (unlike C#), so the container chain stays null. The regexes are
	// deliberately conservative: they match the declaration keyword at the
	// start of a (comment-stripped) line and capture the identifier that
	// follows, skipping comments and string bodies.
	static readonly Regex fsModuleRegex = new (
		@"^module\s+([A-Za-z_][A-Za-z0-9_]*)",
		RegexOptions.Compiled);
	static readonly Regex fsTypeRegex = new (
		@"^type\s+(?:(?:abstract|sealed|static|struct|union|interface)\s+)*([A-Za-z_][A-Za-z0-9_]*)",
		RegexOptions.Compiled);
	// `let` / `let!` / `let+` (computation expressions), optionally followed by
	// the binding modifiers (rec, mutable, inline, unchecked, private) in any
	// order. The NAME is then parsed procedurally, because it is not always a
	// bare identifier: it can be a backtick identifier, a parenthesized
	// destructuring pattern (`let (a, b) = ...`) or the unit pattern `let ()`.
	// The old regex captured the token right after `let`, which was the modifier
	// (e.g. `let mutable x` → "mutable") instead of the name.
	static readonly Regex fsLetPrefixRegex = new (
		@"^let[!+]?\s+(?:(?:rec|mutable|inline|unchecked|private)\s+)*",
		RegexOptions.Compiled);

	static List<SymbolHit> ScanFSharpSymbols (string text)
	{
		var hits = new List<SymbolHit> ();
		int ln = 0;
		int blockCommentDepth = 0;
		foreach (var raw in text.Split ('\n')) {
			ln++;
			var line = raw.TrimEnd ('\r');
			// Strip block comments (nested in F#) and line comments.
			var sb = new System.Text.StringBuilder ();
			int i = 0;
			while (i < line.Length) {
				if (blockCommentDepth > 0) {
					if (line [i] == '(' && i + 1 < line.Length && line [i + 1] == '*') {
						blockCommentDepth++;
						i += 2;
						continue;
					}
					if (line [i] == '*' && i + 1 < line.Length && line [i + 1] == ')') {
						blockCommentDepth--;
						i += 2;
						continue;
					}
					i++;
					continue;
				}
				if (line [i] == '(' && i + 1 < line.Length && line [i + 1] == '*') {
					blockCommentDepth++;
					i += 2;
					continue;
				}
				if (line [i] == '/' && i + 1 < line.Length && line [i + 1] == '/')
					break; // line comment: the rest is ignored
				sb.Append (line [i]);
				i++;
			}
			var trimmed = sb.ToString ().TrimStart ();
			if (trimmed.Length == 0)
				continue;
			var m = fsModuleRegex.Match (trimmed);
			if (m.Success) {
				hits.Add (new SymbolHit ("module", m.Groups [1].Value, ln, 0, null));
				continue;
			}
			var t = fsTypeRegex.Match (trimmed);
			if (t.Success) {
				hits.Add (new SymbolHit ("class", t.Groups [1].Value, ln, 0, null));
				continue;
			}
			var lp = fsLetPrefixRegex.Match (trimmed);
			if (lp.Success) {
				// The name follows the prefix (`let`/`let!`/`let+` + modifiers).
				// It is not always a bare identifier — it can be a backtick
				// identifier, a destructuring pattern (`let (a, b)`) or the unit
				// pattern `let ()` — so it is parsed procedurally.
				bool isComputation = trimmed.Length > 3 && (trimmed [3] == '!' || trimmed [3] == '+');
				int nameStart = lp.Length;
				int p = nameStart;
				while (p < trimmed.Length && char.IsWhiteSpace (trimmed [p]))
					p++;
				string name = "";
				int afterName = trimmed.Length;
				if (p < trimmed.Length) {
					char c0 = trimmed [p];
					if (c0 == '(') {
						// Destructuring / unit pattern: use the first identifier
						// inside the parens; `let ()` has none and is skipped.
						int close = trimmed.IndexOf (')', p);
						if (close > p) {
							name = FirstIdent (trimmed.Substring (p + 1, close - p - 1));
							afterName = close + 1;
						}
					} else if (c0 == '`') {
						// Backtick identifier (`my value`): the name is wrapped in
						// backticks, so find the closing one.
						int end = trimmed.IndexOf ('`', p + 1);
						if (end > p + 1) {
							name = trimmed.Substring (p + 1, end - p - 1);
							afterName = end + 1;
						}
					} else if (char.IsLetterOrDigit (c0) || c0 == '_') {
						int end = p;
						while (end < trimmed.Length && (char.IsLetterOrDigit (trimmed [end]) || trimmed [end] == '_'))
							end++;
						name = trimmed.Substring (p, end - p);
						afterName = end;
					}
				}
				if (name.Length == 0)
					continue;
				// A `let` is a function when it has a parameter list: an explicit
				// `(...)` right after the name, or an implicit-parameter
				// identifier (`let f x = x`). A `let` followed by `=`, `: T`, or a
				// computation-expression binding (`let! x = ...`) is a value.
				int k = afterName;
				while (k < trimmed.Length && char.IsWhiteSpace (trimmed [k]))
					k++;
				bool isFunction = !isComputation && k < trimmed.Length &&
					(trimmed [k] == '(' || char.IsLetterOrDigit (trimmed [k]) || trimmed [k] == '_');
				hits.Add (new SymbolHit (isFunction ? "method" : "field", name, ln, 0, null));
			}
		}
		return hits;
	}

	// The first identifier in a destructuring pattern (e.g. "(a, b)" → "a"),
	// skipping pattern symbols like "|", "{", ",". Returns "" when there is none.
	static string FirstIdent (string s)
	{
		int i = 0;
		while (i < s.Length) {
			var c = s [i];
			if (char.IsLetterOrDigit (c) || c == '_') {
				int start = i;
				while (i < s.Length && (char.IsLetterOrDigit (s [i]) || s [i] == '_'))
					i++;
				return s.Substring (start, i - start);
			}
			i++;
		}
		return "";
	}

	// Token-aware: a type such as `Dictionary<string, int>` is fine, but any
	// whitespace-separated token being a statement keyword rejects the line
	// (`throw new X ();` → {throw,new}, `yield return X ();` → {yield,return}).
	static bool IsStatementKeyword (string typeGroup)
	{
		int i = 0;
		while (i < typeGroup.Length) {
			while (i < typeGroup.Length && char.IsWhiteSpace (typeGroup [i]))
				i++;
			int start = i;
			while (i < typeGroup.Length && !char.IsWhiteSpace (typeGroup [i]))
				i++;
			if (i > start && statementKeywords.Contains (typeGroup.Substring (start, i - start)))
				return true;
		}
		return false;
	}

	/// <summary>Tracks <c>/* */</c>, <c>@"..."</c> and <c>"""..."""</c> state
	/// across lines so declarations inside comments or string literals do not
	/// become symbols. Internal: UnitTestDiscoveryService reuses the same
	/// lex state so a commented-out <c>[Test]</c> is never discovered.</summary>
	internal static void UpdateLexState (string line, ref bool inBlockComment, ref bool inVerbatimString, ref bool inRawString)
	{
		int i = 0;
		while (i < line.Length) {
			if (inBlockComment) {
				int end = line.IndexOf ("*/", i, StringComparison.Ordinal);
				if (end < 0)
					return;
				inBlockComment = false;
				i = end + 2;
				continue;
			}
			if (inVerbatimString) {
				while (i < line.Length) {
					if (line [i] == '"') {
						// "" is an escaped quote inside a verbatim string.
						if (i + 1 < line.Length && line [i + 1] == '"') {
							i += 2;
							continue;
						}
						inVerbatimString = false;
						i++;
						break;
					}
					i++;
				}
				if (inVerbatimString)
					return;
				continue;
			}
			if (inRawString) {
				// A raw string closes on a run of three (or more) quotes.
				if (line [i] == '"' && i + 2 < line.Length && line [i + 1] == '"' && line [i + 2] == '"') {
					inRawString = false;
					i += 3;
					continue;
				}
				i++;
				continue;
			}
			if (line [i] == '/' && i + 1 < line.Length && line [i + 1] == '/')
				return; // line comment: nothing after it changes the state
			if (line [i] == '/' && i + 1 < line.Length && line [i + 1] == '*') {
				inBlockComment = true;
				i += 2;
				continue;
			}
			if (line [i] == '@' && i + 1 < line.Length && line [i + 1] == '"') {
				inVerbatimString = true;
				i += 2;
				continue;
			}
			if (line [i] == '"' && i + 2 < line.Length && line [i + 1] == '"' && line [i + 2] == '"') {
				inRawString = true;
				i += 3;
				continue;
			}
			if (line [i] == '"') {
				i++;
				while (i < line.Length) {
					if (line [i] == '\\') { i += 2; continue; }
					if (line [i] == '"') { i++; break; }
					i++;
				}
				continue;
			}
			if (line [i] == '\'') {
				i++;
				while (i < line.Length) {
					if (line [i] == '\\') { i += 2; continue; }
					if (line [i] == '\'') { i++; break; }
					i++;
				}
				continue;
			}
			i++;
		}
	}

	/// <summary>Timestamp-cached <see cref="ScanSymbols"/> over a file on disk;
	/// unreadable files contribute no symbols instead of throwing. The F#/C#
	/// scanner is chosen by the file extension (the .fs/.fsi/.fsx/.fsscript gate
	/// used everywhere else in the shell).</summary>
	public static List<SymbolHit> ScanFile (string file)
	{
		try {
			var stamp = File.GetLastWriteTimeUtc (file);
			if (cache.TryGetValue (file, out var cached) && cached.Stamp == stamp)
				return cached.Symbols;
			var isFSharp = IsFSharpSource (file);
			var syms = ScanSymbols (File.ReadAllText (file), isFSharp);
			cache [file] = (stamp, syms);
			return syms;
		} catch {
			return new List<SymbolHit> ();
		}
	}

	/// <summary>True when the path is an F# source file (the same extension gate
	/// the editor routing and the FSI pad use).</summary>
	public static bool IsFSharpSource (string path)
	{
		if (string.IsNullOrEmpty (path))
			return false;
		var ext = Path.GetExtension (path).ToLowerInvariant ();
		return ext is ".fs" or ".fsi" or ".fsx" or ".fsscript";
	}

	/// <summary>Namespace declarations with their line, so a type hit can be
	/// attributed to the namespace in effect at that line (both block and
	/// file-scoped <c>namespace X;</c> forms).</summary>
	public static List<(int Line, string Name)> ScanNamespaces (string text)
	{
		var list = new List<(int, string)> ();
		bool inBlockComment = false, inVerbatimString = false, inRawString = false;
		int ln = 0;
		foreach (var raw in text.Split ('\n')) {
			ln++;
			var line = raw.TrimEnd ('\r');
			bool startedInComment = inBlockComment || inVerbatimString || inRawString;
			UpdateLexState (line, ref inBlockComment, ref inVerbatimString, ref inRawString);
			if (startedInComment)
				continue;
			var m = namespaceDeclRegex.Match (line);
			if (m.Success)
				list.Add ((ln, m.Groups [1].Value));
		}
		return list;
	}

	/// <summary>F# module declarations with their line (the F# equivalent of the
	/// <c>namespace</c> grouping for the Classes pad). The same nested-comment
	/// stripping as the F# symbol scanner keeps commented-out modules out.</summary>
	public static List<(int Line, string Name)> ScanFSharpModules (string text)
	{
		var list = new List<(int, string)> ();
		int blockCommentDepth = 0;
		int ln = 0;
		foreach (var raw in text.Split ('\n')) {
			ln++;
			var line = raw.TrimEnd ('\r');
			var sb = new System.Text.StringBuilder ();
			int i = 0;
			while (i < line.Length) {
				if (blockCommentDepth > 0) {
					if (line [i] == '(' && i + 1 < line.Length && line [i + 1] == '*') {
						blockCommentDepth++;
						i += 2;
						continue;
					}
					if (line [i] == '*' && i + 1 < line.Length && line [i + 1] == ')') {
						blockCommentDepth--;
						i += 2;
						continue;
					}
					i++;
					continue;
				}
				if (line [i] == '(' && i + 1 < line.Length && line [i + 1] == '*') {
					blockCommentDepth++;
					i += 2;
					continue;
				}
				if (line [i] == '/' && i + 1 < line.Length && line [i + 1] == '/')
					break;
				sb.Append (line [i]);
				i++;
			}
			var m = fsModuleRegex.Match (sb.ToString ().TrimStart ());
			if (m.Success)
				list.Add ((ln, m.Groups [1].Value));
		}
		return list;
	}

	/// <summary>Document Outline pad model: the flat hits nested by indentation,
	/// preserving source order (the legacy outline is the document's own tree).
	/// F# sources (isFSharp) use the F# scanner — F# has no indentation scoping,
	/// so its nodes are flat (module ▸ type ▸ let at the top level).</summary>
	public static List<OutlineNode> BuildOutline (string text, bool isFSharp = false)
	{
		var roots = new List<OutlineNode> ();
		var stack = new List<OutlineNode> ();
		foreach (var hit in ScanSymbols (text, isFSharp)) {
			var node = new OutlineNode (hit.Kind, hit.Name, hit.Line, hit.Indent, new List<OutlineNode> ());
			while (stack.Count > 0 && stack [^1].Indent >= hit.Indent)
				stack.RemoveAt (stack.Count - 1);
			if (stack.Count > 0)
				stack [^1].Children.Add (node);
			else
				roots.Add (node);
			stack.Add (node);
		}
		return roots;
	}

	/// <summary>Classes pad model: project ▸ namespace ▸ type ▸ member, built from
	/// the (project, file) pairs of the loaded solution. Types keep their nested
	/// types and members as children; the grouping levels are sorted by name and
	/// the members by the legacy ClassNodeBuilder order (type, field, event,
	/// property, method).</summary>
	public static List<ClassNode> BuildClassTree (IEnumerable<(string Project, string File)> sources)
	{
		var projects = new Dictionary<string, ClassNode> (StringComparer.OrdinalIgnoreCase);
		var nsMaps = new Dictionary<string, Dictionary<string, ClassNode>> (StringComparer.OrdinalIgnoreCase);
		foreach (var (project, file) in sources) {
			string text;
			try {
				text = File.ReadAllText (file);
			} catch {
				continue; // unreadable files just don't contribute types
			}
			bool isFSharp = IsFSharpSource (file);
			var outline = BuildOutline (text, isFSharp);
			if (outline.Count == 0)
				continue;
			// F# files have no `namespace` declarations: their top-level
			// modules play the role of the containing namespace, so the type
			// nodes are attributed to the module in effect at their line.
			var namespaces = isFSharp ? ScanFSharpModules (text) : ScanNamespaces (text);
			if (!projects.TryGetValue (project, out var projNode)) {
				projNode = new ClassNode ("project", project, null, 0, new List<ClassNode> ());
				projects [project] = projNode;
				nsMaps [project] = new Dictionary<string, ClassNode> (StringComparer.Ordinal);
			}
			var nsMap = nsMaps [project];
			foreach (var root in outline) {
				if (root.Kind is not ("class" or "interface" or "struct" or "enum" or "record"))
					continue; // top-level methods (top-level statements) are not types
				var ns = NamespaceAt (namespaces, root.Line);
				if (!nsMap.TryGetValue (ns, out var nsNode)) {
					nsNode = new ClassNode ("namespace", ns, null, 0, new List<ClassNode> ());
					nsMap [ns] = nsNode;
					projNode.Children.Add (nsNode);
				}
				nsNode.Children.Add (ToClassNode (root, file));
			}
		}
		foreach (var proj in projects.Values) {
			SortNodes (proj.Children);
			foreach (var ns in proj.Children)
				SortNodes (ns.Children);
		}
		return projects.Values.OrderBy (p => p.Name, StringComparer.OrdinalIgnoreCase).ToList ();
	}

	static ClassNode ToClassNode (OutlineNode node, string file)
	{
		var children = node.Children.Select (c => ToClassNode (c, file)).ToList ();
		SortNodes (children);
		return new ClassNode (node.Kind, node.Name, file, node.Line, children);
	}

	// Legacy ClassNodeBuilder member order: nested types first, then field, event,
	// property, method; alphabetically inside each bucket.
	static void SortNodes (List<ClassNode> nodes)
		=> nodes.Sort ((a, b) => {
			int ra = Rank (a.Kind), rb = Rank (b.Kind);
			return ra != rb ? ra - rb : string.Compare (a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
		});

	static int Rank (string kind) => kind switch {
		"class" or "interface" or "struct" or "enum" or "record" => 0,
		"field" => 1,
		"event" => 2,
		"property" => 3,
		"method" => 4,
		_ => 5,
	};

	static string NamespaceAt (List<(int Line, string Name)> namespaces, int line)
	{
		var result = "";
		foreach (var (l, n) in namespaces) {
			if (l > line)
				break;
			result = n;
		}
		return result;
	}
}
