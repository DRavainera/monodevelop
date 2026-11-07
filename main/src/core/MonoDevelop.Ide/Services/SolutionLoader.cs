using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MonoDevelop.Ide.Services;

/// <summary>One solution item as parsed from the .sln. <see cref="Guid"/>
/// and <see cref="ParentGuid"/> are the braced solution GUIDs (the parent
/// link is GUID-based: folder NAMES may collide across the tree, GUIDs
/// cannot).</summary>
public record SolutionEntry (string Name, string ProjectPath, bool IsFolder, string? Parent, string? Guid, string? ParentGuid);

/// <summary>
/// Loads solution data for the Solution pad using Microsoft.Build's SolutionFile
/// (loaded by reflection from the IDE runtime or the installed SDK), mirroring how
/// the IDE locates MSBuild without hard-wiring a package reference.
/// </summary>
public static class SolutionLoader
{
	static Assembly? msbuildAsm;

	public static (string Title, IReadOnlyList<SolutionEntry> Projects)? Load (string slnPath)
	{
		// SolutionFile.Parse recurses through the NestedProjects parent links
		// (ProjectInSolution.GetUniqueProjectName): a parent cycle (including a
		// self-parent) StackOverflows, and a StackOverflowException cannot be
		// caught — it SIGABRTs the whole process. Screen such files out before
		// invoking Microsoft.Build at all.
		if (HasNestedProjectsCycle (slnPath)) {
			Console.WriteLine ("[solution] load skipped: parent cycle in NestedProjects: " + slnPath);
			return null;
		}
		try {
			var asm = GetMicrosoftBuild ();
			if (asm is null)
				return null;

			var sfType = asm.GetType ("Microsoft.Build.Construction.SolutionFile", true)!;
			var parse = sfType.GetMethod ("Parse", BindingFlags.Public | BindingFlags.Static)
				?? throw new MissingMethodException ("SolutionFile.Parse not found");
			var sf = parse.Invoke (null, new object?[] { slnPath });

			var projectsProp = sfType.GetProperty ("ProjectsInOrder")!;
			var projects = (System.Collections.IEnumerable)projectsProp.GetValue (sf)!;

			var projectTypeType = asm.GetType ("Microsoft.Build.Construction.SolutionProjectType", true)!;
			var solutionFolder = Enum.Parse (projectTypeType, "SolutionFolder");
			var unknownType = Enum.Parse (projectTypeType, "Unknown");

			var guidToName = new Dictionary<string, string> ();
			var entries = new List<SolutionEntry> ();

			foreach (var p in projects) {
				var pType = p.GetType ();
				var name = (string?)pType.GetProperty ("ProjectName")?.GetValue (p) ?? "";
				var relPath = (string?)pType.GetProperty ("RelativePath")?.GetValue (p) ?? "";
				var absPath = (string?)pType.GetProperty ("AbsolutePath")?.GetValue (p) ?? "";
				var guid = (string?)pType.GetProperty ("ProjectGuid")?.GetValue (p);
				var parentGuid = (string?)pType.GetProperty ("ParentProjectGuid")?.GetValue (p);
				var type = pType.GetProperty ("ProjectType")?.GetValue (p);

				if (guid is not null)
					guidToName [guid] = name;

				bool isFolder = Equals (type, solutionFolder) || Equals (type, unknownType);
				if (!isFolder && string.IsNullOrEmpty (relPath))
					continue;

				string? parent = null;
				if (parentGuid is { Length: > 0 } && guidToName.TryGetValue (parentGuid, out var pn))
					parent = pn;

				entries.Add (new SolutionEntry (name, isFolder ? "" : absPath, isFolder, parent,
					guid, parentGuid is { Length: > 0 } ? parentGuid : null));
			}

			var title = Path.GetFileNameWithoutExtension (slnPath);
			return (title, entries);
		} catch (Exception ex) {
			var inner = ex is TargetInvocationException tie && tie.InnerException is not null ? tie.InnerException : ex;
			Console.WriteLine ("[solution] load failed: " + inner.Message);
			return null;
		}
	}

	/// <summary>A node of the solution tree (folder or project) with its
	/// children in .sln file order.</summary>
	public sealed class SolutionTreeNode
	{
		public SolutionTreeNode (SolutionEntry entry)
		{
			Entry = entry;
		}

		public SolutionEntry Entry { get; }
		public List<SolutionTreeNode> Children { get; } = new ();

		public string Name => Entry.Name;
		public bool IsFolder => Entry.IsFolder;
		public string? ProjectPath => Entry.ProjectPath;
		public string? Guid => Entry.Guid;
	}

	/// <summary>Builds the FULL solution tree (solution folders at any depth)
	/// from the flat entry list, linking children to their parent by GUID
	/// (the <c>NestedProjects</c> relation). Entries whose parent is missing
	/// or unknown attach to the root. Pure and deterministic: the pad renders
	/// it and the QA hooks assert its shape.</summary>
	public static List<SolutionTreeNode> BuildTree (IReadOnlyList<SolutionEntry> entries)
	{
		var byGuid = new Dictionary<string, SolutionTreeNode> (StringComparer.OrdinalIgnoreCase);
		var nodes = new List<SolutionTreeNode> (entries.Count);
		foreach (var e in entries) {
			var node = new SolutionTreeNode (e);
			nodes.Add (node);
			if (!string.IsNullOrEmpty (e.Guid) && !byGuid.ContainsKey (e.Guid!))
				byGuid [e.Guid!] = node;
		}
		var roots = new List<SolutionTreeNode> ();
		foreach (var node in nodes) {
			var pg = node.Entry.ParentGuid;
			if (pg is not null && byGuid.TryGetValue (pg, out var parent) && !ReferenceEquals (parent, node))
				parent.Children.Add (node);
			else
				roots.Add (node);
		}

		// A corrupted .sln can link parent GUIDs in a cycle (A -> B -> A),
		// including a cycle hanging off a real tree. Walk with three colors
		// and cut the back edges so the tree stays finite; nothing is lost.
		var color = new Dictionary<SolutionTreeNode, int> (); // 1 = gray, 2 = black
		System.Action<SolutionTreeNode> Visit = null;
		Visit = n => {
			color [n] = 1;
			var toCut = new List<SolutionTreeNode> ();				foreach (var c in n.Children) {
				if (color.TryGetValue (c, out var cc)) {
					if (cc == 1)
						toCut.Add (c); // back edge to an ancestor: cycle
				} else {
					Visit (c); // normal exit always paints black
				}
			}
			foreach (var c in toCut)
				n.Children.Remove (c);
			color [n] = 2;
		};
		foreach (var r in roots)
			Visit (r);

		// A cycle with no root entry was never visited: anchor the first node
		// of each such component at the top level (file order) and let the
		// colored walk above cut its back edges.
		while (true) {
			SolutionTreeNode first = null;
			foreach (var n in nodes) {
				if (!color.ContainsKey (n)) {
					first = n;
					break;
				}
			}
			if (first is null)
				break;
			roots.Add (first);
			Visit (first);
		}
		return roots;
	}

	/// <summary>True when the .sln's GlobalSection(NestedProjects) declares a
	/// parent cycle — a self-parent or a chain that loops back on itself.
	/// Plain line scan, no recursion; see the note in <see cref="Load"/>.
	/// ALL <c>GlobalSection(NestedProjects)</c> sections are merged (later
	/// assignments win — the same final parent map Microsoft.Build ends with)
	/// because a corrupt .sln may declare the section more than once.</summary>
	public static bool HasNestedProjectsCycle (string slnPath)
	{
		string text;
		try {
			text = File.ReadAllText (slnPath);
		} catch (Exception) {
			return false;
		}

		var parentOf = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		var pos = 0;
		while (true) {
			var start = text.IndexOf ("GlobalSection(NestedProjects)", pos, StringComparison.OrdinalIgnoreCase);
			if (start < 0)
				break;
			var body = text.IndexOf ('\n', start);
			if (body < 0)
				break;
			var end = text.IndexOf ("EndGlobalSection", body, StringComparison.OrdinalIgnoreCase);
			if (end < 0)
				end = text.Length;
			for (var i = body + 1; i < end; ) {
				var nl = text.IndexOf ('\n', i);
				if (nl < 0)
					nl = end;
				var line = text [i..nl].TrimEnd ('\r').Trim ();
				var eq = line.IndexOf ('=');
				if (eq > 0 && eq < line.Length - 1)
					parentOf [line [..eq].Trim ()] = line [(eq + 1)..].Trim ();
				i = nl + 1;
			}
			pos = end + "EndGlobalSection".Length;
		}
		if (parentOf.Count == 0)
			return false;

		foreach (var child in parentOf.Keys.ToList ()) {
			var seen = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
			for (var cur = child; ; ) {
				if (!seen.Add (cur))
					return true; // the chain loops: cycle
				if (!parentOf.TryGetValue (cur, out var p) || p.Length == 0)
					break; // reached a root (or an unknown parent: not a cycle)
				cur = p;
			}
		}
		return false;
	}

	static Assembly GetMicrosoftBuild ()
	{
		if (msbuildAsm is not null)
			return msbuildAsm;

		// Prefer the IDE runtime dir (net10run, when the shell is staged next to it),
		// then the .NET SDK installation.
		string? shellDir = Path.GetDirectoryName (typeof (SolutionLoader).Assembly.Location);
		string? candidate = shellDir is null ? null : Path.Combine (shellDir, "Microsoft.Build.dll");
		if (candidate is null || !File.Exists (candidate)) {
			var dotnetRoot = Environment.GetEnvironmentVariable ("DOTNET_ROOT") ?? "/usr/share/dotnet";
			var home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty (dotnetRoot) || !Directory.Exists (dotnetRoot))
				dotnetRoot = Path.Combine (home, ".dotnet");
			var sdkDir = Directory.GetDirectories (Path.Combine (dotnetRoot, "sdk"))
				.OrderByDescending (d => Version.TryParse (Path.GetFileName (d), out var v) ? v : new Version (0, 0))
				.FirstOrDefault ();
			candidate = sdkDir is null ? null : Path.Combine (sdkDir, "Microsoft.Build.dll");
		}

		msbuildAsm = Assembly.LoadFrom (candidate!);
		return msbuildAsm;
	}
}
