using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Services;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the `dotnet test` (vstest console, minimal verbosity) output
/// parser and the supporting discovery pieces (test-project detection,
/// filter builder, node aggregation) that the Unit Tests pad runner relies on.
/// </summary>
public class TestRunParserTests
{
	// ----- Per-test status lines -----

	[Fact]
	public void TryParseTestLine_passed_with_duration ()
	{
		Assert.True (TestRunParser.TryParseTestLine ("  Passed Demo.CalcTests.AddWorks [12 ms]",
			out var name, out var state, out var ms));
		Assert.Equal ("Demo.CalcTests.AddWorks", name);
		Assert.Equal (UnitTestState.Success, state);
		Assert.Equal (12.0, ms);
	}

	[Fact]
	public void TryParseTestLine_failed_data_driven_suffix ()
	{
		Assert.True (TestRunParser.TryParseTestLine ("  Failed Demo.CalcTests.AddCases (1, 2, 3) [3 ms]",
			out var name, out var state, out var ms));
		Assert.Equal ("Demo.CalcTests.AddCases (1, 2, 3)", name);
		Assert.Equal (UnitTestState.Failure, state);
		Assert.Equal (3.0, ms);
	}

	[Fact]
	public void TryParseTestLine_skipped_without_duration ()
	{
		Assert.True (TestRunParser.TryParseTestLine ("  Skipped Demo.CalcTests.NotYet",
			out var name, out var state, out var ms));
		Assert.Equal (UnitTestState.Skipped, state);
		Assert.Equal (0.0, ms);
	}

	[Fact]
	public void TryParseTestLine_rejects_non_status_lines ()
	{
		Assert.False (TestRunParser.TryParseTestLine (
			"Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 15 ms — X.dll (net10.0)",
			out _, out _, out _));
		Assert.False (TestRunParser.TryParseTestLine ("A total of 1 test files matched the specified pattern.",
			out _, out _, out _));
		Assert.False (TestRunParser.TryParseTestLine ("  Error Message:", out _, out _, out _));
	}

	[Fact]
	public void TryParseTestLine_sub_millisecond_second_and_clock_durations ()
	{
		// The NUnit adapter under -v minimal prints short names with "[< 1 ms]".
		Assert.True (TestRunParser.TryParseTestLine ("  Skipped NotYet [< 1 ms]",
			out var name, out var state, out var ms));
		Assert.Equal ("NotYet", name);
		Assert.Equal (UnitTestState.Skipped, state);
		Assert.Equal (0.0, ms);

		Assert.True (TestRunParser.TryParseTestLine ("  Passed Demo.CalcTests.Slow [15 s]",
			out _, out var state2, out var ms2));
		Assert.Equal (UnitTestState.Success, state2);
		Assert.Equal (15000.0, ms2);

		Assert.True (TestRunParser.TryParseTestLine ("  Failed Demo.CalcTests.Huge [00:01:02]",
			out _, out var state3, out _));
		Assert.Equal (UnitTestState.Failure, state3);
	}

	// ----- Summary line -----

	[Fact]
	public void TryParseSummaryLine_passed_summary ()
	{
		Assert.True (TestRunParser.TryParseSummaryLine (
			"Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 15 ms — Demo.Tests.dll (net10.0)",
			out var passed, out var failed, out var skipped, out var total));
		Assert.Equal (2, passed);
		Assert.Equal (0, failed);
		Assert.Equal (0, skipped);
		Assert.Equal (2, total);
	}

	[Fact]
	public void TryParseSummaryLine_failed_and_aborted_summaries ()
	{
		Assert.True (TestRunParser.TryParseSummaryLine (
			"Failed!  - Failed: 1, Passed: 1, Skipped: 0, Total: 2, Duration: 16 ms — Demo.Tests.dll (net10.0)",
			out var passed, out var failed, out var skipped, out var total));
		Assert.Equal ((1, 1, 0, 2), (passed, failed, skipped, total));

		Assert.True (TestRunParser.TryParseSummaryLine (
			"Aborted! - Failed: 0, Passed: 0, Skipped: 0, Total: 0, Duration: 1 ms — X.dll",
			out passed, out failed, out skipped, out total));
		Assert.Equal ((0, 0, 0, 0), (passed, failed, skipped, total));
	}

	[Fact]
	public void TryParseSummaryLine_rejects_other_lines ()
	{
		Assert.False (TestRunParser.TryParseSummaryLine ("Test run for /x/Demo.Tests.dll", out _, out _, out _, out _));
		Assert.False (TestRunParser.TryParseSummaryLine ("Passed the assertion", out _, out _, out _, out _));
	}

	// ----- Failure location -----

	[Fact]
	public void TryParseFailureLocation_stack_trace_reference ()
	{
		const string text = """
			Assert.Equal() Failure
			Expected: 3
			Actual:   4
			  Stack Trace:
			     at Demo.CalcTests.SubFails() in /home/x/Demo.Tests/CalcTests.cs:line 10
			""";
		Assert.True (TestRunParser.TryParseFailureLocation (text, out var file, out var line));
		Assert.Equal ("/home/x/Demo.Tests/CalcTests.cs", file);
		Assert.Equal (10, line);

		Assert.False (TestRunParser.TryParseFailureLocation ("no location here", out _, out _));
	}

	// ----- Test project detection -----

	static string WriteTempCsproj (string content)
	{
		var path = Path.Combine (Path.GetTempPath (), $"mdtest-{Guid.NewGuid ():N}.csproj");
		File.WriteAllText (path, content);
		return path;
	}

	[Fact]
	public void IsTestProject_sdk_test_sdk_marker ()
	{
		var p = WriteTempCsproj ("""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
			  <ItemGroup>
				<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
				<PackageReference Include="NUnit" Version="4.2.0" />
			  </ItemGroup>
			</Project>
			""");
		try {
			Assert.True (UnitTestDiscoveryService.IsTestProject (p));
		} finally {
			File.Delete (p);
		}
	}

	[Fact]
	public void IsTestProject_xunit_and_mstest_package_markers ()
	{
		var p1 = WriteTempCsproj ("""
			<Project Sdk="Microsoft.NET.Sdk">
			  <ItemGroup><PackageReference Include="xunit" Version="2.9.2" /></ItemGroup>
			</Project>
			""");
		var p2 = WriteTempCsproj ("""
			<Project Sdk="Microsoft.NET.Sdk">
			  <ItemGroup><PackageReference Include="MSTest.TestAdapter" Version="3.6.0" /></ItemGroup>
			</Project>
			""");
		var p3 = WriteTempCsproj ("""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup>
			</Project>
			""");
		var p4 = WriteTempCsproj ("""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
			  <ItemGroup><PackageReference Include="Newtonsoft.Json" Version="13.0.3" /></ItemGroup>
			</Project>
			""");
		try {
			Assert.True (UnitTestDiscoveryService.IsTestProject (p1));
			Assert.True (UnitTestDiscoveryService.IsTestProject (p2));
			Assert.True (UnitTestDiscoveryService.IsTestProject (p3));
			Assert.False (UnitTestDiscoveryService.IsTestProject (p4));
		} finally {
			File.Delete (p1);
			File.Delete (p2);
			File.Delete (p3);
			File.Delete (p4);
		}
	}

	[Fact]
	public void IsTestProject_missing_file_is_not_a_test_project ()
	{
		Assert.False (UnitTestDiscoveryService.IsTestProject ("/nonexistent/x.csproj"));
	}

	// ----- Filter builder -----

	[Fact]
	public void BuildFilter_or_of_substring_matches ()
	{
		var filter = UnitTestDiscoveryService.BuildFilter (
			new[] { "A.B.T1", "A.B.T2", "A.B.T1" });
		Assert.Equal ("FullyQualifiedName~A.B.T1|FullyQualifiedName~A.B.T2", filter);
		Assert.Equal ("", UnitTestDiscoveryService.BuildFilter (Array.Empty<string> ()));
	}

	// ----- Node model: aggregation, counters, reset -----

	static UnitTestNode Tree (params (string name, UnitTestState state)[] leaves)
	{
		var root = new UnitTestNode ("solution", "S");
		var proj = new UnitTestNode ("project", "P");
		var cls = new UnitTestNode ("class", "C") { FullName = "N.C" };
		proj.Children.Add (cls);
		root.Children.Add (proj);
		cls.Parent = proj;
		proj.Parent = root;
		foreach (var (name, state) in leaves) {
			var t = new UnitTestNode ("test", name) { FullName = "N.C." + name, State = state };
			t.Parent = cls;
			cls.Children.Add (t);
		}
		return root;
	}

	[Fact]
	public void AggregatedState_priority_rules ()
	{
		Assert.Equal (UnitTestState.Success, Tree (
			("A", UnitTestState.Success), ("B", UnitTestState.Success)).AggregatedState ());
		Assert.Equal (UnitTestState.Failure, Tree (
			("A", UnitTestState.Success), ("B", UnitTestState.Failure)).AggregatedState ());
		Assert.Equal (UnitTestState.Mixed, Tree (
			("A", UnitTestState.Success), ("B", UnitTestState.Skipped)).AggregatedState ());
		Assert.Equal (UnitTestState.Running, Tree (
			("A", UnitTestState.Success), ("B", UnitTestState.Running)).AggregatedState ());
		Assert.Equal (UnitTestState.NotRun, Tree (
			("A", UnitTestState.NotRun), ("B", UnitTestState.NotRun)).AggregatedState ());
		Assert.Equal (UnitTestState.Skipped, Tree (
			("A", UnitTestState.Skipped)).AggregatedState ());
	}

	[Fact]
	public void CountStates_and_reset ()
	{
		var root = Tree (
			("A", UnitTestState.Success),
			("B", UnitTestState.Failure),
			("C", UnitTestState.Skipped),
			("D", UnitTestState.NotRun));
		root.CountStates (out var passed, out var failed, out var notRun);
		Assert.Equal ((1, 1, 2), (passed, failed, notRun));

		root.ResetState ();
		Assert.All (root.Descendants ().Where (d => d.Kind == "test"), t =>
			Assert.Equal (UnitTestState.NotRun, t.State));
	}

	[Fact]
	public void FindTestByFullName_exact_and_data_driven_prefix ()
	{
		var root = Tree (("AddCases", UnitTestState.Success));
		Assert.NotNull (root.FindTestByFullName ("N.C.AddCases"));
		Assert.NotNull (root.FindTestByFullName ("N.C.AddCases (1, 2, 3)"));
		Assert.Null (root.FindTestByFullName ("N.C.Other"));
	}
}
