using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Port of the legacy CodeIssuePad model onto the diagnostics the shell already
/// produces: MSBuild error/warning lines ("file(line,col): error CODE: message")
/// are parsed into <see cref="CodeIssue"/> rows and grouped by severity, which is
/// the grouping the legacy pad exposes through SeverityGroupingProvider.
/// </summary>
public static class CodeIssueService
{
	public sealed record CodeIssue (string File, int Line, int Col, string Severity, string Code, string Message);

	/// <summary>Severity buckets in the legacy display order (errors first).</summary>
	public static readonly string [] SeverityOrder = { "error", "warning", "info", "hidden" };

	static readonly Regex buildMessageRegex = new (
		@"^(.+?)\((\d+),(\d+)\): (error|warning|info|hidden) ([A-Za-z0-9]+): (.*)$",
		RegexOptions.Compiled);

	/// <summary>Parses one MSBuild diagnostic line; null when the line is not a
	/// diagnostic (build progress, banners, plain output).</summary>
	public static CodeIssue? Parse (string line)
	{
		var m = buildMessageRegex.Match (line);
		if (!m.Success)
			return null;
		return new CodeIssue (m.Groups [1].Value, int.Parse (m.Groups [2].Value),
			int.Parse (m.Groups [3].Value), m.Groups [4].Value,
			m.Groups [5].Value, m.Groups [6].Value);
	}

	/// <summary>Groups issues by severity in <see cref="SeverityOrder"/>, dropping
	/// empty buckets; issues keep their arrival order inside a bucket.</summary>
	public static List<(string Severity, List<CodeIssue> Issues)> GroupBySeverity (IEnumerable<CodeIssue> issues)
	{
		var bySeverity = issues.GroupBy (i => i.Severity, StringComparer.OrdinalIgnoreCase)
			.ToDictionary (g => g.Key, g => g.ToList (), StringComparer.OrdinalIgnoreCase);
		var result = new List<(string, List<CodeIssue>)> ();
		foreach (var sev in SeverityOrder) {
			if (bySeverity.TryGetValue (sev, out var list))
				result.Add ((sev, list));
		}
		// Unknown severities (future MSBuild levels) still surface, after the known ones.
		foreach (var kv in bySeverity.Where (k => !SeverityOrder.Contains (k.Key, StringComparer.OrdinalIgnoreCase)))
			result.Add ((kv.Key, kv.Value));
		return result;
	}

	/// <summary>Legacy pad row text: "file (line,col): severity CODE: message".</summary>
	public static string FormatRow (CodeIssue issue)
		=> $"{System.IO.Path.GetFileName (issue.File)} ({issue.Line},{issue.Col}): {issue.Severity} {issue.Code}: {issue.Message}";

	/// <summary>Header line of the pad: counts per severity, e.g.
	/// "3 error(s), 12 warning(s)".</summary>
	public static string FormatSummary (IEnumerable<CodeIssue> issues)
	{
		var groups = GroupBySeverity (issues);
		if (groups.Count == 0)
			return "No code issues";
		return string.Join (", ", groups.Select (g => $"{g.Issues.Count} {g.Severity}(s)"));
	}
}
