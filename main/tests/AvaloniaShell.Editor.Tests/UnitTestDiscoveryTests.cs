using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Services;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the Unit Tests pad backend: text discovery of NUnit/xUnit/MSTest
/// test classes and methods (the legacy provider pipeline replacement), test
/// project detection, the `dotnet test` output parser and the filter builder.
/// Pure text processing — no Avalonia platform needed.
/// </summary>
public class UnitTestDiscoveryTests
{
	const string NUnitSample = """
		using System;

		namespace Demo.Tests
		{
			[TestFixture]
			public class CalcTests
			{
				[Test]
				public void AddWorks ()
				{
					Assert.AreEqual (3, 1 + 2);
				}

				[TestCase (1, 2, 3)]
				[TestCase (0, 0, 0)]
				public void AddCases (int a, int b, int expected)
				{
				}

				[TestCaseSource ("CaseSource")]
				public void AddFromSource (TestCaseData data)
				{
				}

				[Test]
				[Ignore ("not ready")]
				public void SubFails ()
				{
				}

				[Test]
				[Explicit]
				public void SlowTest ()
				{
				}

				public void Helper ()
				{
				}
			}

			public class NotATestClass
			{
				[Obsolete ("x")]
				public void Plain ()
				{
				}
			}
		}
		""";

	const string XunitSample = """
		using Xunit;

		namespace Demo.XTests
		{
			public class FactTests
			{
				[Fact]
				public void Works ()
				{
				}

				[Theory]
				[InlineData (1)]
				[InlineData (2)]
				public void TheoryCase (int x)
				{
				}

				[Fact (Skip = "pending")]
				public void SkippedFact ()
				{
				}
			}
		}
		""";

	const string MstestSample = """
		using Microsoft.VisualStudio.TestTools.UnitTesting;

		namespace Demo.MTests
		{
			[TestClass]
			public class MsTests
			{
				[TestMethod]
				public void MsFact ()
				{
				}

				[DataTestMethod]
				[DataRow (1)]
				public void MsData (int x)
				{
				}
			}
		}
		""";

	const string FileScopedSample = """
		namespace Demo.FileScoped;

		[Collection ("c")]
		public class ScopedTests
		{
			[Fact]
			public void InFileScoped ()
			{
			}
		}
		""";

	// ----- NUnit discovery -----

	[Fact]
	public void DiscoverText_nunit_finds_test_methods_with_fqn_and_lines ()
	{
		var classes = UnitTestDiscoveryService.DiscoverText (NUnitSample, "/tmp/Calc.cs");
		Assert.Single (classes);
		var cls = classes [0];
		Assert.Equal ("CalcTests", cls.Name);
		Assert.Equal ("class", cls.Kind);
		Assert.Equal ("Demo.Tests.CalcTests", cls.FullName);
		Assert.Equal ("/tmp/Calc.cs", cls.File);
		Assert.Equal (6, cls.Line);
		Assert.True (cls.TestClassMarker);

		var names = cls.Children.Select (c => c.Name).ToList ();
		Assert.Equal (new[] { "AddWorks", "AddCases (1, 2, 3)", "AddCases (0, 0, 0)", "AddFromSource", "SubFails", "SlowTest" }, names);
		foreach (var t in cls.Children) {
			Assert.Equal ("test", t.Kind);
			Assert.Equal ("nunit", t.Framework);
			Assert.Equal ("Demo.Tests.CalcTests." + t.Name, t.FullName);
			Assert.True (t.Line > cls.Line, $"{t.Name} at {t.Line} should be below the class at {cls.Line}");
		}
		Assert.Equal (9, cls.Children [0].Line); // [Test] on 8, declaration on 9
		// Data-case nodes filter by the base-method FQN; source-based
		// [TestCaseSource] keeps a single node whose FilterKey is its own FQN.
		var c1 = cls.Children [1];
		Assert.Equal ("Demo.Tests.CalcTests.AddCases", c1.FilterKey);
		Assert.Equal ("AddCases", c1.MethodName);
		var src = cls.Children.Single (t => t.Name == "AddFromSource");
		Assert.Equal ("Demo.Tests.CalcTests.AddFromSource", src.FilterKey);
		Assert.Equal ("AddFromSource", src.MethodName);
	}

	[Fact]
	public void DiscoverText_nunit_marks_ignore_and_explicit ()
	{
		var cls = UnitTestDiscoveryService.DiscoverText (NUnitSample) [0];
		var ignored = cls.Children.Single (t => t.Name == "SubFails");
		Assert.Equal (UnitTestState.Skipped, ignored.State);
		var explicit_ = cls.Children.Single (t => t.Name == "SlowTest");
		Assert.True (explicit_.Explicit);
		Assert.Equal (UnitTestState.NotRun, explicit_.State);
	}

	[Fact]
	public void DiscoverText_drops_classes_without_tests_or_fixture ()
	{
		var classes = UnitTestDiscoveryService.DiscoverText (NUnitSample);
		Assert.DoesNotContain (classes, c => c.Name == "NotATestClass");
	}

	// ----- xUnit / MSTest discovery -----

	[Fact]
	public void DiscoverText_xunit_finds_fact_theory_and_skip ()
	{
		var cls = UnitTestDiscoveryService.DiscoverText (XunitSample) [0];
		Assert.Equal ("FactTests", cls.Name);
		Assert.False (cls.TestClassMarker); // no [Collection] on this class
		var names = cls.Children.Select (c => c.Name).ToList ();
		// xUnit case display carries the parameter names from the declaration.
		Assert.Equal (new[] { "Works", "TheoryCase (x: 1)", "TheoryCase (x: 2)", "SkippedFact" }, names);
		Assert.All (cls.Children, t => Assert.Equal ("xunit", t.Framework));
		Assert.Equal (UnitTestState.Skipped, cls.Children.Single (t => t.Name == "SkippedFact").State);
		Assert.Equal ("Demo.XTests.FactTests.TheoryCase", cls.Children [1].FilterKey);
	}

	[Fact]
	public void DiscoverText_mstest_finds_testmethod_and_datatestmethod ()
	{
		var cls = UnitTestDiscoveryService.DiscoverText (MstestSample) [0];
		Assert.True (cls.TestClassMarker);
		var names = cls.Children.Select (c => c.Name).ToList ();
		// MSTest case display has no space before the parenthesis.
		Assert.Equal (new[] { "MsFact", "MsData(1)" }, names);
		Assert.All (cls.Children, t => Assert.Equal ("mstest", t.Framework));
	}

	// ----- Namespace forms -----

	[Fact]
	public void DiscoverText_allman_block_namespace_sets_fqn ()
	{
		var source = """
			namespace Demo.Allman
			{
				public class C
				{
					[Fact]
					public void T ()
					{
					}
				}
			}
			""";
		var cls = UnitTestDiscoveryService.DiscoverText (source) [0];
		Assert.Equal ("Demo.Allman.C", cls.FullName);
		Assert.Equal ("Demo.Allman.C.T", cls.Children [0].FullName);
	}

	[Fact]
	public void DiscoverText_file_scoped_namespace_sets_fqn ()
	{
		var cls = UnitTestDiscoveryService.DiscoverText (FileScopedSample) [0];
		Assert.Equal ("Demo.FileScoped.ScopedTests", cls.FullName);
		Assert.True (cls.TestClassMarker); // [Collection]
		Assert.Single (cls.Children);
		Assert.Equal ("Demo.FileScoped.ScopedTests.InFileScoped", cls.Children [0].FullName);
	}

	[Fact]
	public void DiscoverText_commented_out_test_is_not_discovered ()
	{
		var source = """
			namespace Demo
			{
				public class C
				{
					// [Test]
					// public void CommentedOut ()
					// {
					// }

					[Test]
					public void Real ()
					{
						/* [Test]
						   var fake = 1; */
					}
				}
			}
			""";
		var cls = UnitTestDiscoveryService.DiscoverText (source) [0];
		Assert.Single (cls.Children);
		Assert.Equal ("Real", cls.Children [0].Name);
	}

	[Fact]
	public void DiscoverText_local_functions_and_records_do_not_become_tests ()
	{
		var source = """
			namespace Demo
			{
				public record Point (int X, int Y);

				public class C
				{
					[Obsolete ("x")]
					public record Inner (int A);

					[Fact]
					public void T ()
					{
						void Local ()
						{
						}
						Local ();
					}
				}
			}
			""";
		var classes = UnitTestDiscoveryService.DiscoverText (source);
		var cls = classes.Single (c => c.Name == "C");
		Assert.Single (cls.Children);
		Assert.Equal ("T", cls.Children [0].Name);
	}

	// ----- Attribute line parsing / classification -----

	[Fact]
	public void TryParseAttributeLine_stacked_attributes_extracted ()
	{
		Assert.True (UnitTestDiscoveryService.TryParseAttributeLine (
			"\t[TestCase (1, 2)] [TestCase (3, 4)]", out var attrs));
		Assert.Equal (2, attrs.Count);
		Assert.Equal ("TestCase", attrs [0].Name);
		Assert.Equal ("1, 2", attrs [0].Args);
		Assert.False (UnitTestDiscoveryService.TryParseAttributeLine (
			"public void M ()", out _));
		Assert.False (UnitTestDiscoveryService.TryParseAttributeLine (
			"[Test] public void M ()", out _));
	}

	[Fact]
	public void ClassifyTest_recognizes_frameworks_and_markers ()
	{
		List<(string Name, string Args)> Attrs (params (string, string)[] a) => a.ToList ();
		Assert.Equal ((true, "nunit", false, false), UnitTestDiscoveryService.ClassifyTest (Attrs (("Test", ""))));
		Assert.Equal ((true, "xunit", false, true), UnitTestDiscoveryService.ClassifyTest (Attrs (("Fact", "Skip = \"x\""))));
		Assert.Equal ((true, "mstest", false, false), UnitTestDiscoveryService.ClassifyTest (Attrs (("TestMethod", ""))));
		Assert.Equal ((true, "nunit", true, false), UnitTestDiscoveryService.ClassifyTest (Attrs (("Test", ""), ("Explicit", ""))));
		Assert.Equal ((false, "", false, false), UnitTestDiscoveryService.ClassifyTest (Attrs (("Obsolete", "x"))));
	}

	// ----- Data-case expansion helpers -----

	[Fact]
	public void SplitArgs_honors_quotes_and_nesting ()
	{
		Assert.Equal (new[] { "1", "2", "3" }, UnitTestDiscoveryService.SplitArgs ("1, 2, 3"));
		Assert.Equal (new[] { "\"a, b\"", "2" }, UnitTestDiscoveryService.SplitArgs ("\"a, b\", 2"));
		Assert.Equal (new[] { "new[] { 1, 2 }", "x" }, UnitTestDiscoveryService.SplitArgs ("new[] { 1, 2 }, x"));
		Assert.Empty (UnitTestDiscoveryService.SplitArgs (""));
		Assert.Equal (new[] { "\"a \\\\ b\"" }, UnitTestDiscoveryService.SplitArgs ("\"a \\\\ b\""));
	}

	[Fact]
	public void CaseDisplayName_formats_per_framework ()
	{
		var withParams = new List<string> { "a", "b" };
		var none = new List<string> ();
		Assert.Equal ("M (1, 2)", UnitTestDiscoveryService.CaseDisplayName ("nunit", "M", none, "1, 2"));
		Assert.Equal ("M (a: 1, b: 2)", UnitTestDiscoveryService.CaseDisplayName ("xunit", "M", withParams, "1, 2"));
		// xUnit without a matching parameter list falls back to bare values.
		Assert.Equal ("M (1, 2)", UnitTestDiscoveryService.CaseDisplayName ("xunit", "M", none, "1, 2"));
		Assert.Equal ("M(1, 2)", UnitTestDiscoveryService.CaseDisplayName ("mstest", "M", withParams, "1, 2"));
	}

	// ----- Status line → node resolution -----

	static UnitTestNode MakeNode (string name, string fullName, string? filterKey = null, string methodName = "") =>
		new UnitTestNode ("test", name) {
			FullName = fullName,
			FilterKey = filterKey,
			MethodName = methodName,
		};

	[Fact]
	public void ResolveNodes_matches_fqn_shortname_and_case_suffixes ()
	{
		var scope = new List<UnitTestNode> {
			MakeNode ("M", "Ns.C.M", "Ns.C.M", "M"),
			MakeNode ("M", "Ns.D.M", "Ns.D.M", "M"),
			MakeNode ("T (1, 2, 3)", "Ns.C.T (1, 2, 3)", "Ns.C.T", "T"),
			MakeNode ("T (0, 0, 0)", "Ns.C.T (0, 0, 0)", "Ns.C.T", "T"),
			MakeNode ("S", "Ns.C.S", "Ns.C.S", "S"),
		};
		// Exact FQN.
		Assert.Single (UnitTestDiscoveryService.ResolveNodes (scope, "Ns.C.M"));
		// FQN + case suffix → the plain node covers all its cases.
		Assert.Single (UnitTestDiscoveryService.ResolveNodes (scope, "Ns.C.S (1)"));
		// Adapter short name: same-named methods in two classes both match
		// (the minimal-verbosity console line cannot disambiguate).
		Assert.Equal (2, UnitTestDiscoveryService.ResolveNodes (scope, "M").Count);
		// Short case display → the exact case node by Name.
		Assert.Single (UnitTestDiscoveryService.ResolveNodes (scope, "T (1, 2, 3)"));
		// xUnit case format (parameter names) → base-method match, both cases.
		Assert.Equal (2, UnitTestDiscoveryService.ResolveNodes (scope, "Ns.C.T (a: 1, b: 2, expected: 3)").Count);
		// Unknown name → nothing.
		Assert.Empty (UnitTestDiscoveryService.ResolveNodes (scope, "Ns.C.Other"));
	}
}
