using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Port of the legacy MonoDevelop.UnitTesting discovery surface onto a text
/// scan. The legacy pipeline (UnitTestService + ITestProvider + VsTest adapter)
/// discovers tests through the project model and the VS test SDK; the shell has
/// neither, so test classes and methods are recovered from the C# text with
/// attribute regexes (same anchored-scan approach as <see cref="SymbolIndexService"/>)
/// and execution goes through <c>dotnet test</c> with a FullyQualifiedName
/// filter, whose console output is parsed back into node states.
///
/// Supported frameworks: NUnit ([Test]/[TestCase]/[TestCaseSource]/[Ignore]/
/// [Explicit]), xUnit ([Fact]/[Theory]/Skip=) and MSTest ([TestMethod]/
/// [DataTestMethod]). The FullyQualifiedName format (Namespace.Class.Method)
/// is identical in the three, so one FQN covers discovery and filtering.
/// </summary>
public enum UnitTestState
{
	NotRun,
	Running,
	Success,
	Failure,
	Skipped,
	Error,
	Mixed,
}

/// <summary>One node of the Unit Tests pad tree: solution ▸ project ▸
/// namespace ▸ class ▸ test. Grouping levels (Kind != "test") aggregate the
/// states of their descendants; leaf states are driven by the
/// <see cref="TestRunParser"/> output of <c>dotnet test</c>.</summary>
public sealed class UnitTestNode
{
	public UnitTestNode (string kind, string name)
	{
		Kind = kind;
		Name = name;
	}

	/// <summary>One of: solution, project, namespace, class, test.</summary>
	public string Kind { get; }

	public string Name { get; }

	/// <summary>vstest FullyQualifiedName; null for the (global) namespace
	/// group, which has no single filterable name of its own.</summary>
	public string? FullName { get; init; }

	/// <summary>Source file (class and test nodes).</summary>
	public string? File { get; init; }

	/// <summary>1-based line in <see cref="File"/> (class and test nodes).</summary>
	public int Line { get; init; }

	/// <summary>Owning .csproj (project nodes and everything below).</summary>
	public string? ProjectPath { get; set; }

	/// <summary>Test nodes: the base method name without data-case arguments
	/// (case node `M (1, 2)` → `M`); equals <see cref="Name"/> for plain
	/// tests. Used to map adapter short-name lines and method-level filters.</summary>
	public string MethodName { get; init; } = "";

	/// <summary>Test nodes: the FQN used in the vstest `--filter` expression.
	/// Data-case nodes filter by the base-method FQN: each adapter registers
	/// case FQNs with its own argument format (xUnit `a: 1`, NUnit `1`,
	/// MSTest `(1)` without space), so the exact case string is not portable
	/// and a case-level run executes the whole method.</summary>
	public string? FilterKey { get; init; }

	/// <summary>Detected framework of the test (nunit, xunit, mstest); empty
	/// for grouping levels.</summary>
	public string Framework { get; set; }

	/// <summary>[Explicit] (NUnit): excluded from bulk runs unless selected.</summary>
	public bool Explicit { get; set; }

	/// <summary>Class marked as test class by a fixture attribute
	/// ([TestFixture]/[TestClass]) even without test methods found by the scan.</summary>
	public bool TestClassMarker { get; set; }

	public UnitTestState State { get; set; } = UnitTestState.NotRun;

	/// <summary>Failure message / error output (test nodes after a failed run).</summary>
	public string? Message { get; set; }

	public double DurationMs { get; set; }

	public List<UnitTestNode> Children { get; } = new ();

	public UnitTestNode? Parent { get; set; }

	public IEnumerable<UnitTestNode> Descendants ()
	{
		foreach (var c in Children) {
			yield return c;
			foreach (var d in c.Descendants ())
				yield return d;
		}
	}

	/// <summary>Leaf test whose FullyQualifiedName matches <paramref name="fqn"/>;
	/// data-driven vstest lines carry a "(args)" suffix, so a prefix match on the
	/// bare FQN (or FQN + ".") counts.</summary>
	public UnitTestNode? FindTestByFullName (string fqn)
	{
		foreach (var t in Descendants ().Where (d => d.Kind == "test")) {
			if (t.FullName == fqn
				|| fqn.StartsWith (t.FullName + ".", StringComparison.Ordinal)
				|| fqn.StartsWith (t.FullName + " (", StringComparison.Ordinal))
				return t;
		}
		return null;
	}

	/// <summary>Legacy TestNodeBuilder group icon: Running wins, then any
	/// Failure/Error, then a Success mixed with Skipped/NotRun is Mixed, then
	/// the uniform state, then NotRun.</summary>
	public UnitTestState AggregatedState ()
	{
		if (Kind == "test")
			return State;
		bool anyRunning = false, anyFailure = false, anySuccess = false, anySkipped = false, anyNotRun = false;
		foreach (var c in Children) {
			switch (c.AggregatedState ()) {
			case UnitTestState.Running: anyRunning = true; break;
			case UnitTestState.Failure:
			case UnitTestState.Error: anyFailure = true; break;
			case UnitTestState.Success: anySuccess = true; break;
			case UnitTestState.Skipped: anySkipped = true; break;
			case UnitTestState.Mixed:
				// A Mixed child never implies Failure at the parent level.
				anySuccess = anySkipped = anyNotRun = true;
				break;
			default: anyNotRun = true; break;
			}
		}
		if (anyRunning)
			return UnitTestState.Running;
		if (anyFailure)
			return UnitTestState.Failure;
		if (anySuccess && (anySkipped || anyNotRun))
			return UnitTestState.Mixed;
		if (anySuccess)
			return UnitTestState.Success;
		if (anySkipped)
			return UnitTestState.Skipped;
		return UnitTestState.NotRun;
	}

	/// <summary>Legacy ShowTestCounters suffix: "(N passed, M failed, K not run)".
	/// NotRun includes tests skipped by attribute and tests the run never reached.</summary>
	public void CountStates (out int passed, out int failed, out int notRun)
	{
		passed = failed = notRun = 0;
		foreach (var t in Descendants ().Where (d => d.Kind == "test")) {
			switch (t.State) {
			case UnitTestState.Success: passed++; break;
			case UnitTestState.Failure:
			case UnitTestState.Error: failed++; break;
			default: notRun++; break;
			}
		}
	}

	/// <summary>Clears a subtree to its pre-run state (legacy
	/// UnitTestService.ResetResult before a new run).</summary>
	public void ResetState ()
	{
		State = UnitTestState.NotRun;
		Message = null;
		DurationMs = 0;
		foreach (var c in Children)
			c.ResetState ();
	}
}

public static class UnitTestDiscoveryService
{
	// csproj markers that identify a test project: the SDK-style
	// Microsoft.NET.Test.Sdk reference, the explicit IsTestProject property,
	// or a test-framework package (NUnit / xUnit / MSTest family ids).
	static readonly Regex packageRefRegex = new (
		@"<PackageReference\s+[^>]*Include\s*=\s*""([^""]+)""",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);
	static readonly Regex isTestProjectRegex = new (
		@"<IsTestProject\s*>\s*true\s*</IsTestProject>",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static bool IsTestProject (string csprojPath)
	{
		try {
			var text = File.ReadAllText (csprojPath);
			if (isTestProjectRegex.IsMatch (text))
				return true;
			foreach (Match m in packageRefRegex.Matches (text)) {
				var id = m.Groups [1].Value.Trim ();
				var lower = id.ToLowerInvariant ();
				if (lower == "microsoft.net.test.sdk"
					|| lower.StartsWith ("nunit", StringComparison.Ordinal)
					|| lower.StartsWith ("xunit", StringComparison.Ordinal)
					|| lower.StartsWith ("mstest", StringComparison.Ordinal))
					return true;
			}
		} catch {
			// Unreadable project file: not a test project.
		}
		return false;
	}

	// Declaration patterns run on the trimmed line, like SymbolIndexService.
	// The namespace pattern does NOT require the brace/semicolon: both K&R and
	// Allman block namespaces and file-scoped namespaces are recognized; the
	// form (file-scoped vs block) is decided by what follows the name.
	static readonly Regex namespaceRegex = new (
		@"^\s*namespace\s+([A-Za-z_][\w\.]*)",
		RegexOptions.Compiled);
	static readonly Regex classDeclRegex = new (
		@"^((?:public|private|protected|internal|static|sealed|abstract|partial|readonly|new|unsafe|file)\s+)*(class|interface|record)(?:\s+(?:class|struct))?\s+([A-Za-z_][\w]*)",
		RegexOptions.Compiled);
	// Method: <modifiers> <type> <name> ( — the first token of the type group
	// must not be a declaration keyword so positional records (`record Point
	// (int X)`) after an attribute line do not register as test methods.
	static readonly Regex methodDeclRegex = new (
		@"^((?:public|private|protected|internal|static|async|virtual|override|sealed|abstract|partial|readonly|extern|unsafe|new|ref)\s+)*([\w<>\[\],\.?]+(?:\s+[\w<>\[\],\.?]+)*)\s+([A-Za-z_][\w]*)\s*(?:<[^>]*>)?\s*\(",
		RegexOptions.Compiled);
	static readonly HashSet<string> declarationKeywords = new (StringComparer.Ordinal) {
		"class", "interface", "struct", "enum", "record", "namespace", "delegate",
	};

	// Attribute names that mark a test method, by framework.
	static readonly HashSet<string> nunitTestAttrs = new (StringComparer.Ordinal) {
		"Test", "TestCase", "TestCaseSource", "ValueSource", "RandomDataSource", "Retry",
	};
	static readonly HashSet<string> xunitTestAttrs = new (StringComparer.Ordinal) {
		"Fact", "Theory", "SkippableFact",
	};
	static readonly HashSet<string> mstestTestAttrs = new (StringComparer.Ordinal) {
		"TestMethod", "DataTestMethod",
	};
	// Class-level markers that make a type a test class even when the scan
	// found no test methods: NUnit [TestFixture], MSTest [TestClass],
	// xUnit [Collection].
	static readonly HashSet<string> fixtureAttrs = new (StringComparer.Ordinal) {
		"TestFixture", "TestClass", "Collection",
	};

	/// <summary>True when the whole line is one or more attribute applications
	/// (`[Attr]`, `[Attr(args)]`, stacked on one line); extracts name+args so
	/// the declaration on the next line can be classified.</summary>
	public static bool TryParseAttributeLine (string line, out List<(string Name, string Args)> attrs)
	{
		attrs = new List<(string, string)> ();
		int i = 0;
		while (i < line.Length) {
			while (i < line.Length && char.IsWhiteSpace (line [i]))
				i++;
			if (i >= line.Length)
				break;
			if (line [i] != '[')
				return false;
			int end = line.IndexOf (']', i);
			if (end < 0)
				return false;
			var inner = line [(i + 1)..end].Trim ();
			int paren = inner.IndexOf ('(');
			var name = (paren < 0 ? inner : inner.AsSpan (0, paren).ToString ()).Trim ();
			var args = paren < 0 ? "" : inner [(paren + 1)..].TrimEnd (')').Trim ();
			if (!Regex.IsMatch (name, @"^[A-Za-z_][\w\.]*$"))
				return false;
			attrs.Add ((name, args));
			i = end + 1;
		}
		return attrs.Count > 0;
	}

	static string ShortName (string attrName)
		=> attrName.Contains ('.') ? attrName [(attrName.LastIndexOf ('.') + 1)..] : attrName;

	/// <summary>Splits an attribute argument list on top-level commas, honoring
	/// string literals and nested parentheses/braces/collection initializers;
	/// tokens are echoed as written (trimmed), so the display matches the source.</summary>
	public static List<string> SplitArgs (string args)
	{
		var parts = new List<string> ();
		if (string.IsNullOrWhiteSpace (args))
			return parts;
		int depth = 0, start = 0;
		bool inStr = false;
		for (int i = 0; i < args.Length; i++) {
			var c = args [i];
			if (inStr) {
				if (c == '\\' && i + 1 < args.Length)
					i++;
				else if (c == '"')
					inStr = false;
			} else if (c == '"') {
				inStr = true;
			} else if (c == '(' || c == '{') {
				depth++;
			} else if (c == ')' || c == '}') {
				depth--;
			} else if (c == ',' && depth == 0) {
				parts.Add (args [start..i].Trim ());
				start = i + 1;
			}
		}
		parts.Add (args [start..].Trim ());
		return parts.Where (p => p.Length > 0).ToList ();
	}

	/// <summary>Parameter names of a method declaration line
	/// (`public void M (int a, string b) {` → a, b); empty when the parameter
	/// list is not complete on the line.</summary>
	static List<string> ParseParamNames (string decl)
	{
		var names = new List<string> ();
		int open = decl.IndexOf ('(');
		if (open < 0)
			return names;
		int depth = 0, close = -1;
		bool inStr = false;
		for (int i = open; i < decl.Length; i++) {
			var c = decl [i];
			if (inStr) {
				if (c == '\\' && i + 1 < decl.Length)
					i++;
				else if (c == '"')
					inStr = false;
			} else if (c == '"') {
				inStr = true;
			} else if (c == '(') {
				depth++;
			} else if (c == ')') {
				depth--;
				if (depth == 0) {
					close = i;
					break;
				}
			}
		}
		if (close < 0)
			return names;
		foreach (var p in SplitArgs (decl [(open + 1)..close])) {
			var t = p.Split ('=') [0].Trim ();
			var toks = t.Split (' ', StringSplitOptions.RemoveEmptyEntries);
			if (toks.Length > 0)
				names.Add (toks [toks.Length - 1]);
		}
		return names;
	}

	/// <summary>Display name of a data case, per the adapter the run will use:
	/// xUnit `M (a: 1, b: 2)` (parameter names, when the declaration provides
	/// as many as there are arguments), NUnit `M (1, 2)`, MSTest `M(1, 2)`
	/// (no space before the parenthesis). Argument values are echoed as written.</summary>
	public static string CaseDisplayName (string framework, string methodName, List<string> paramNames, string rawArgs)
	{
		var args = SplitArgs (rawArgs);
		var display = framework == "xunit" && paramNames.Count == args.Count && paramNames.All (n => n.Length > 0)
			? string.Join (", ", args.Select ((a, i) => paramNames [i] + ": " + a))
			: string.Join (", ", args);
		return framework == "mstest" ? methodName + "(" + display + ")" : methodName + " (" + display + ")";
	}

	/// <summary>Classifies an attribute stack: is it a test method, which
	/// framework, is it [Explicit], is it skipped by attribute (NUnit [Ignore]
	/// / xUnit Skip=)?</summary>
	public static (bool IsTest, string Framework, bool Explicit, bool Skipped) ClassifyTest (List<(string Name, string Args)> attrs)
	{
		int nunit = 0, xunit = 0, mstest = 0;
		bool explicit_ = false, skipped = false;
		foreach (var (name, args) in attrs) {
			var shortName = ShortName (name);
			if (nunitTestAttrs.Contains (shortName))
				nunit++;
			else if (xunitTestAttrs.Contains (shortName))
				xunit++;
			else if (mstestTestAttrs.Contains (shortName))
				mstest++;
			if (shortName == "Explicit")
				explicit_ = true;
			if (shortName == "Ignore")
				skipped = true;
			// xUnit: [Fact (Skip = "reason")]
			if ((shortName == "Fact" || shortName == "Theory" || shortName == "SkippableFact")
				&& Regex.IsMatch (args, @"Skip\s*=", RegexOptions.IgnoreCase))
				skipped = true;
		}
		int best = Math.Max (nunit, Math.Max (xunit, mstest));
		if (best == 0)
			return (false, "", false, false);
		var framework = best == nunit ? "nunit" : best == xunit ? "xunit" : "mstest";
		return (true, framework, explicit_, skipped);
	}

	/// <summary>Scans one C# file and returns its test classes (method children
	/// attached, File/Line set); classes with neither test methods nor a
	/// fixture marker are dropped, like the legacy provider's HasTests check.</summary>
	public static List<UnitTestNode> DiscoverFile (string file)
	{
		string text;
		try {
			text = File.ReadAllText (file);
		} catch {
			return new List<UnitTestNode> ();
		}
		return DiscoverText (text, file);
	}

	/// <summary>Text overload of <see cref="DiscoverFile"/> (unit-testable
	/// without touching disk).</summary>
	public static List<UnitTestNode> DiscoverText (string text, string? file = null)
	{
		var classes = new List<UnitTestNode> ();
		// Indentation scope stack (SymbolIndexService convention): a CLOSING
		// brace at indent i pops every scope opened at indent >= i; a lone
		// opening brace (Allman) sits at its owner's indent and never pops.
		var scopes = new List<(int Indent, string Kind, string Name, UnitTestNode? Node)> ();
		string fileNs = "";
		var pendingAttrs = new List<(string Name, string Args)> ();
		bool inBlockComment = false, inVerbatimString = false, inRawString = false;
		int ln = 0;
		foreach (var raw in text.Split ('\n')) {
			ln++;
			var line = raw.TrimEnd ('\r');
			bool startedInComment = inBlockComment || inVerbatimString || inRawString;
			SymbolIndexService.UpdateLexState (line, ref inBlockComment, ref inVerbatimString, ref inRawString);
			if (startedInComment)
				continue;
			var trimmed = line.TrimStart ();
			if (trimmed.Length == 0 || trimmed.StartsWith ("//", StringComparison.Ordinal))
				continue;
			int indent = line.Length - trimmed.Length;
			if (trimmed.StartsWith ("}", StringComparison.Ordinal)) {
				PopScopes (indent);
				pendingAttrs.Clear ();
				continue;
			}
			if (trimmed.StartsWith ("{", StringComparison.Ordinal))
				continue;
			PopScopes (indent);
			if (TryParseAttributeLine (line, out var attrs)) {
				// Accumulate: [Test] + [Explicit] must both survive until the
				// declaration line clears the stack.
				pendingAttrs.AddRange (attrs);
				continue;
			}
			var nsm = namespaceRegex.Match (line);
			if (nsm.Success) {
				var nsName = nsm.Groups [1].Value;
				var rest = line [nsm.Length..].TrimStart ();
				if (rest.StartsWith (";", StringComparison.Ordinal))
					fileNs = nsName; // file-scoped: namespace X;
				else
					scopes.Add ((indent, "ns", nsName, null)); // block (K&R or Allman)
				pendingAttrs.Clear ();
				continue;
			}
			var cm = classDeclRegex.Match (trimmed);
			if (cm.Success) {
				var ns = CurrentNamespace ();
				var className = cm.Groups [3].Value;
				var node = new UnitTestNode ("class", className) {
					FullName = ns.Length > 0 ? ns + "." + className : className,
					File = file,
					Line = ln,
				};
				node.TestClassMarker = pendingAttrs.Any (a => fixtureAttrs.Contains (ShortName (a.Name)));
				scopes.Add ((indent, "class", className, node));
				classes.Add (node);
				pendingAttrs.Clear ();
				continue;
			}
			var mm = methodDeclRegex.Match (trimmed);
			if (mm.Success) {
				var firstTok = mm.Groups [2].Value.Split (' ') [0];
				if (!declarationKeywords.Contains (firstTok)) {
					// Nearest enclosing class, and whether a method body sits
					// between it and this line (local functions are not members:
					// the legacy provider does not see them either).
					UnitTestNode? scopeClass = null;
					bool insideMethodBody = false;
					for (int i = scopes.Count - 1; i >= 0; i--) {
						if (scopes [i].Kind == "class") {
							scopeClass = scopes [i].Node;
							break;
						}
						if (scopes [i].Kind == "method") {
							insideMethodBody = true;
							break;
						}
					}
				var (isTest, framework, explicit_, skipped) = ClassifyTest (pendingAttrs);
				if (isTest && scopeClass is not null && !insideMethodBody) {
					var methodName = mm.Groups [3].Value;
					var baseFqn = scopeClass.FullName + "." + methodName;
					// Data attributes with literal arguments become one node per
					// case (legacy TestNodeBuilder expanded them the same way);
					// source-based attributes ([TestCaseSource]…) cannot be
					// expanded from text and keep a single method node.
					var dataCases = new List<string> ();
					foreach (var (attrName, attrArgs) in pendingAttrs) {
						if (attrArgs.Length > 0 && ShortName (attrName) is "TestCase" or "InlineData" or "DataRow")
							dataCases.Add (attrArgs);
					}
					if (dataCases.Count == 0) {
						scopeClass.Children.Add (new UnitTestNode ("test", methodName) {
							FullName = baseFqn,
							FilterKey = baseFqn,
							MethodName = methodName,
							File = file,
							Line = ln,
							Framework = framework,
							Explicit = explicit_,
							State = skipped ? UnitTestState.Skipped : UnitTestState.NotRun,
						});
					} else {
						var paramNames = ParseParamNames (trimmed);
						foreach (var rawArgs in dataCases) {
							var caseName = CaseDisplayName (framework, methodName, paramNames, rawArgs);
							scopeClass.Children.Add (new UnitTestNode ("test", caseName) {
								FullName = scopeClass.FullName + "." + caseName,
								FilterKey = baseFqn,
								MethodName = methodName,
								File = file,
								Line = ln,
								Framework = framework,
								Explicit = explicit_,
								State = skipped ? UnitTestState.Skipped : UnitTestState.NotRun,
							});
						}
					}
				}
					scopes.Add ((indent, "method", mm.Groups [3].Value, null));
				}
				pendingAttrs.Clear ();
				continue;
			}
			pendingAttrs.Clear ();
		}
		return classes
			.Where (c => c.Children.Count > 0 || c.TestClassMarker)
			.ToList ();

		string CurrentNamespace ()
		{
			if (fileNs.Length > 0)
				return fileNs;
			for (int i = scopes.Count - 1; i >= 0; i--)
				if (scopes [i].Kind == "ns")
					return scopes [i].Name;
			return "";
		}

		void PopScopes (int indent)
		{
			while (scopes.Count > 0 && scopes [^1].Indent >= indent)
				scopes.RemoveAt (scopes.Count - 1);
		}
	}

	/// <summary>One project of the loaded solution: project node with
	/// namespace ▸ class ▸ test children over its .cs files (bin/obj/.git
	/// excluded, like the Classes pad). Returns null when the project is not a
	/// test project or has no test classes (legacy HasTests).</summary>
	public static UnitTestNode? DiscoverProject (string projectPath)
	{
		if (!IsTestProject (projectPath))
			return null;
		var projNode = new UnitTestNode ("project", Path.GetFileNameWithoutExtension (projectPath)) {
			ProjectPath = projectPath,
		};
		var dir = Path.GetDirectoryName (projectPath);
		if (dir is null)
			return null;
		var nsMap = new Dictionary<string, UnitTestNode> (StringComparer.Ordinal);
		foreach (var file in Directory.EnumerateFiles (dir, "*.cs", SearchOption.AllDirectories)
			.Where (f => !f.Contains ("/obj/") && !f.Contains ("/bin/") && !f.Contains ("/.git/"))
			.OrderBy (f => f, StringComparer.OrdinalIgnoreCase)) {
			foreach (var cls in DiscoverFile (file)) {
				var fqn = cls.FullName ?? "";
				int dot = fqn.LastIndexOf ('.');
				var nsName = dot > -1 ? fqn [..dot] : "";
				if (!nsMap.TryGetValue (nsName, out var nsNode)) {
					nsNode = new UnitTestNode ("namespace", nsName.Length > 0 ? nsName : "(global)") {
						FullName = nsName.Length > 0 ? nsName : null,
						ProjectPath = projectPath,
					};
					nsMap [nsName] = nsNode;
					projNode.Children.Add (nsNode);
				}
				cls.Parent = nsNode;
				cls.ProjectPath = projectPath;
				foreach (var t in cls.Children) {
					t.Parent = cls;
					t.ProjectPath = projectPath;
				}
				nsNode.Children.Add (cls);
			}
		}
		if (projNode.Children.Count == 0)
			return null;
		SortChildren (projNode);
		return projNode;
	}

	/// <summary>Loaded-solution root node: one child per test project found in
	/// <paramref name="projects"/> (the .sln entries). Returns null when the
	/// solution has no test project with tests (legacy: the pad shows nothing
	/// under the root and Run All is disabled).</summary>
	public static UnitTestNode? DiscoverSolution (string solutionName, IEnumerable<(string Name, string ProjectPath)> projects)
	{
		var root = new UnitTestNode ("solution", solutionName);
		foreach (var (_, projPath) in projects) {
			var proj = DiscoverProject (projPath);
			if (proj is null)
				continue;
			proj.Parent = root;
			AssignParents (proj);
			root.Children.Add (proj);
		}
		root.Children.Sort ((a, b) => string.Compare (a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		return root.Children.Count > 0 ? root : null;
	}

	static void AssignParents (UnitTestNode node)
	{
		foreach (var c in node.Children) {
			c.Parent = node;
			AssignParents (c);
		}
	}

	static void SortChildren (UnitTestNode node)
	{
		node.Children.Sort ((a, b) => string.Compare (a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		foreach (var c in node.Children)
			SortChildren (c);
	}

	/// <summary>vstest --filter expression for the given FullyQualifiedNames
	/// (OR of substring matches: a data-driven case line carries a "(args)"
	/// suffix, so `=` exact match would miss it). Empty input → empty filter
	/// (run everything in the project).</summary>
	public static string BuildFilter (IEnumerable<string> fullyQualifiedNames)
	{
		var parts = fullyQualifiedNames
			.Where (f => !string.IsNullOrEmpty (f))
			.Distinct (StringComparer.Ordinal)
			.Select (f => "FullyQualifiedName~" + f);
		return string.Join ("|", parts);
	}

	/// <summary>Maps a status-line test name onto the nodes of the running
	/// scope. Line names come in two shapes: the vstest FullyQualifiedName
	/// (xUnit always; NUnit at normal verbosity) or the adapter's short name
	/// (the NUnit adapter under `-v minimal` prints `Failed SubFails [12 ms]`);
	/// either may carry the data-case suffix. Resolution order: exact
	/// FullName, FullName + case suffix, exact Name, then the base name (the
	/// line name minus its ` (...)` suffix) against the node's base FQN /
	/// Name / MethodName. ALL matches are returned: a short-name line cannot
	/// disambiguate same-named methods of two classes in the project, and the
	/// line's status applies to every one of them.</summary>
	public static IReadOnlyList<UnitTestNode> ResolveNodes (IReadOnlyList<UnitTestNode> scope, string lineName)
	{
		var hits = scope.Where (t =>
			t.FullName == lineName
			|| (t.FullName?.StartsWith (lineName + ".", StringComparison.Ordinal) ?? false)
			|| (t.FullName?.StartsWith (lineName + " (", StringComparison.Ordinal) ?? false)
			|| t.Name == lineName
			|| (t.Name.Length > 0 && lineName.StartsWith (t.Name + " (", StringComparison.Ordinal))
		).ToList ();
		if (hits.Count > 0)
			return hits;
		var baseName = lineName.Contains (" (", StringComparison.Ordinal)
			? lineName [..lineName.LastIndexOf (" (", StringComparison.Ordinal)]
			: lineName;
		return scope.Where (t =>
			t.FilterKey == baseName
			|| t.FullName == baseName
			|| t.Name == baseName
			|| t.MethodName == baseName
		).ToList ();
	}
}

/// <summary>Parses the <c>dotnet test</c> (vstest console, minimal verbosity)
/// output back into node states: the per-test status lines, the final summary
/// line and the failure-location reference used by Go To Failure (the legacy
/// TestResultBuilder did the same mapping from the VsTest adapter events).</summary>
public static class TestRunParser
{
	// "  Passed Demo.CalcTests.AddWorks [12 ms]"
	// "  Failed Demo.CalcTests.SubFails (42) [3 ms]"
	// "  Skipped NotYet [< 1 ms]"              (sub-millisecond)
	// "  Passed Demo.CalcTests.Slow [15 s]"    (seconds)
	// "  Failed Demo.CalcTests.Huge [00:01:02]" (clock form)
	// Anchored to end-of-line so "  Error Message:" (the failure block header)
	// is not read as a test named "Message".
	static readonly Regex testLineRegex = new (
		@"^\s*(Passed|Failed|Skipped|Error)\s+([A-Za-z_][\w\.]*(?:\s*\([^)]*\))?)\s*(\[\s*(?:< ?\d+ ?ms|\d+(?:\.\d+)? ?(?:ms|s)|\d+:\d{2}:\d{2})\s*\])?\s*$",
		RegexOptions.Compiled);

	// "Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 15 ms — Demo.Tests.dll (net10.0)"
	// "Failed!  - Failed: 1, Passed: 1, Skipped: 0, Total: 2, Duration: 16 ms — ..."
	// "Aborted! - Failed: 0, Passed: 0, Skipped: 0, Total: 0, Duration: ..."
	static readonly Regex summaryRegex = new (
		@"^(Passed|Failed|Aborted)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)",
		RegexOptions.Compiled);

	// "   at Demo.CalcTests.SubFails() in /home/x/Demo.Tests/CalcTests.cs:line 10"
	static readonly Regex failureLocationRegex = new (
		 @"\bin\s+(.+?\.cs):line\s+(\d+)",
		RegexOptions.Compiled);

	/// <summary>Per-test status line. <paramref name="testName"/> is the
	/// FullyQualifiedName (with the data-driven "(args)" suffix when present).</summary>
	public static bool TryParseTestLine (string line, out string testName, out UnitTestState state, out double durationMs)
	{
		testName = "";
		state = UnitTestState.NotRun;
		durationMs = 0;
		var m = testLineRegex.Match (line);
		if (!m.Success)
			return false;
		testName = m.Groups [2].Value.Trim ();
		state = m.Groups [1].Value switch {
			"Passed" => UnitTestState.Success,
			"Failed" => UnitTestState.Failure,
			"Skipped" => UnitTestState.Skipped,
			_ => UnitTestState.Error,
		};
		durationMs = ParseDuration (m.Groups [3].Value);
		return true;
	}

	// "[12 ms]" → 12, "[15 s]" → 15000, "[< 1 ms]" / "[00:01:02]" → 0
	static double ParseDuration (string raw)
	{
		var t = raw.Trim ().Trim ('[', ']').Trim ();
		if (t.Length > 0 && t [0] == '<')
			return 0;
		if (t.EndsWith (" ms", StringComparison.Ordinal)
			&& double.TryParse (t.AsSpan (0, t.Length - 3), NumberStyles.Float, CultureInfo.InvariantCulture, out var ms))
			return ms;
		if (t.EndsWith (" s", StringComparison.Ordinal)
			&& double.TryParse (t.AsSpan (0, t.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
			return s * 1000;
		return 0;
	}

	/// <summary>Final summary line (vstest prints it once per test run).</summary>
	public static bool TryParseSummaryLine (string line, out int passed, out int failed, out int skipped, out int total)
	{
		passed = failed = skipped = total = 0;
		var m = summaryRegex.Match (line);
		if (!m.Success)
			return false;
		failed = int.Parse (m.Groups [2].Value, CultureInfo.InvariantCulture);
		passed = int.Parse (m.Groups [3].Value, CultureInfo.InvariantCulture);
		skipped = int.Parse (m.Groups [4].Value, CultureInfo.InvariantCulture);
		total = int.Parse (m.Groups [5].Value, CultureInfo.InvariantCulture);
		return true;
	}

	/// <summary>First "in /path/File.cs:line N" reference in a failure message
	/// or stack trace (legacy TestResult.GetFailureLocation).</summary>
	public static bool TryParseFailureLocation (string text, out string file, out int line)
	{
		file = "";
		line = 0;
		var m = failureLocationRegex.Match (text);
		if (!m.Success)
			return false;
		file = m.Groups [1].Value;
		line = int.Parse (m.Groups [2].Value, CultureInfo.InvariantCulture);
		return true;
	}
}
