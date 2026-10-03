using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.Services;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the Code Issues pad model: MSBuild diagnostic lines are parsed into
/// rows and grouped by severity in the legacy display order.
/// </summary>
public class CodeIssueTests
{
	static CodeIssueService.CodeIssue ParseOne (string line)
		=> Assert.IsType<CodeIssueService.CodeIssue> (CodeIssueService.Parse (line));

	// ----- Parse -----

	[Fact]
	public void Parse_reads_file_line_col_severity_code_and_message ()
	{
		var issue = ParseOne ("/src/Program.cs(12,5): error CS0103: The name 'Foo' does not exist in the current context");

		Assert.Equal ("/src/Program.cs", issue.File);
		Assert.Equal (12, issue.Line);
		Assert.Equal (5, issue.Col);
		Assert.Equal ("error", issue.Severity);
		Assert.Equal ("CS0103", issue.Code);
		Assert.Equal ("The name 'Foo' does not exist in the current context", issue.Message);
	}

	[Theory]
	[InlineData ("warning")]
	[InlineData ("info")]
	[InlineData ("hidden")]
	public void Parse_accepts_every_msbuild_severity (string severity)
	{
		var issue = ParseOne ($"/src/A.cs(1,1): {severity} CS0001: msg");
		Assert.Equal (severity, issue.Severity);
	}

	[Theory]
	[InlineData ("Build started...")]
	[InlineData ("  Program -> /src/bin/Debug/net10.0/Program.dll")]
	[InlineData ("")]
	[InlineData ("/src/A.cs(1,1): note CS0001: msg")]
	public void Parse_returns_null_for_non_diagnostics (string line)
	{
		Assert.Null (CodeIssueService.Parse (line));
	}

	[Fact]
	public void Parse_keeps_colons_inside_the_message ()
	{
		var issue = ParseOne ("/src/A.cs(3,7): error CS1002: ; expected: check the line");
		Assert.Equal ("; expected: check the line", issue.Message);
	}

	// ----- GroupBySeverity -----

	[Fact]
	public void GroupBySeverity_orders_errors_before_warnings ()
	{
		var issues = new [] {
			ParseOne ("/src/A.cs(1,1): warning CS0219: w"),
			ParseOne ("/src/A.cs(2,1): error CS0103: e"),
			ParseOne ("/src/A.cs(3,1): info CS8019: i"),
		};

		var groups = CodeIssueService.GroupBySeverity (issues);

		Assert.Equal (new [] { "error", "warning", "info" }, groups.Select (g => g.Severity).ToList ());
		Assert.Single (groups [0].Issues);
	}

	[Fact]
	public void GroupBySeverity_drops_empty_buckets ()
	{
		var groups = CodeIssueService.GroupBySeverity (new [] { ParseOne ("/src/A.cs(1,1): error CS0103: e") });
		Assert.Single (groups);
		Assert.Equal ("error", groups [0].Severity);
	}

	[Fact]
	public void GroupBySeverity_keeps_arrival_order_inside_a_bucket ()
	{
		var issues = new [] {
			ParseOne ("/src/A.cs(9,1): error CS0002: second"),
			ParseOne ("/src/A.cs(1,1): error CS0001: first"),
		};

		var groups = CodeIssueService.GroupBySeverity (issues);
		Assert.Equal (new [] { "second", "first" }, groups [0].Issues.Select (i => i.Message).ToList ());
	}

	[Fact]
	public void GroupBySeverity_places_unknown_severities_last ()
	{
		var issues = new [] {
			ParseOne ("/src/A.cs(1,1): error CS0103: e"),
			new CodeIssueService.CodeIssue ("/src/A.cs", 2, 1, "critical", "CS9999", "future level"),
		};

		var groups = CodeIssueService.GroupBySeverity (issues);
		Assert.Equal (new [] { "error", "critical" }, groups.Select (g => g.Severity).ToList ());
	}

	// ----- Formatting -----

	[Fact]
	public void FormatRow_matches_the_legacy_pad_row ()
	{
		var issue = ParseOne ("/src/deep/Program.cs(12,5): error CS0103: boom");
		Assert.Equal ("Program.cs (12,5): error CS0103: boom", CodeIssueService.FormatRow (issue));
	}

	[Fact]
	public void FormatSummary_counts_per_severity ()
	{
		var issues = new [] {
			ParseOne ("/src/A.cs(1,1): error CS0103: e"),
			ParseOne ("/src/A.cs(2,1): warning CS0219: w"),
			ParseOne ("/src/A.cs(3,1): warning CS0168: w"),
		};

		Assert.Equal ("1 error(s), 2 warning(s)", CodeIssueService.FormatSummary (issues));
	}

	[Fact]
	public void FormatSummary_reports_the_empty_state ()
	{
		Assert.Equal ("No code issues", CodeIssueService.FormatSummary (new List<CodeIssueService.CodeIssue> ()));
	}
}
