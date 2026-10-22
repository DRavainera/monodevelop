using System;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Services;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the help pad model: the Monodoc index format (monodoc.xml + per-node
/// children files) is parsed by HelpTreeService without the Monodoc library.
/// </summary>
public class HelpTreeServiceTests : IDisposable
{
	readonly string root;

	public HelpTreeServiceTests ()
	{
		root = Path.Combine (Path.GetTempPath (), "helpservice-tests-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
	}

	public void Dispose ()
	{
		if (Directory.Exists (root))
			Directory.Delete (root, true);
	}

	static void Write (string path, string content)
	{
		Directory.CreateDirectory (Path.GetDirectoryName (path));
		File.WriteAllText (path, content);
	}

	const string Index = @"<?xml version=""1.0""?>
<root>
  <name>API</name>
  <summary>Top level documentation.</summary>
  <nodes>
    <node>
      <name>Concepts</name>
      <summary>Concepts overview.</summary>
      <nodes>
        <node file=""getting-started.html""><name>Getting started</name><summary>First steps.</summary></node>
        <node><name>Subtopics</name>
          <nodes>
            <node file=""topics/intro.html""><name>Intro</name></node>
          </nodes>
        </node>
      </nodes>
    </node>
    <node file=""api.xml""/>
  </nodes>
</root>";

	[Fact]
	public void LoadTree_merges_sources_under_single_root ()
	{
		Write (Path.Combine (root, "monodoc.xml"), Index);
		var res = HelpTreeService.LoadTree (new[] { root });

		Assert.NotNull (res.Root);
		Assert.Equal (HelpTreeService.RootCaption, res.Root.Caption);
		Assert.Equal (root, res.Root.SourceDir);
		Assert.Empty (res.Errors);
		Assert.Equal (1, res.Searched.Count);
	}

	[Fact]
	public void LoadTree_parses_inline_nodes_with_summary ()
	{
		Write (Path.Combine (root, "monodoc.xml"), Index);
		var res = HelpTreeService.LoadTree (new[] { root });

		var concepts = res.Root.Children[0];
		Assert.Equal ("Concepts", concepts.Caption);
		Assert.Equal ("Concepts overview.", concepts.Summary);
		Assert.Equal (2, concepts.Children.Count);

		var gettingStarted = concepts.Children[0];
		Assert.Equal ("Getting started", gettingStarted.Caption);
		Assert.Equal ("First steps.", gettingStarted.Summary);
		Assert.False (gettingStarted.IsChildrenLoaded);
		Assert.Equal ("getting-started.html", gettingStarted.ChildrenFile);
		Assert.Equal (root, gettingStarted.SourceDir);

		var subtopics = concepts.Children[1];
		Assert.Equal ("Subtopics", subtopics.Caption);
		Assert.True (subtopics.IsChildrenLoaded);
		Assert.Equal ("Intro", Assert.Single (subtopics.Children).Caption);
		Assert.Equal ("topics/intro.html", subtopics.Children[0].ChildrenFile);
	}

	[Fact]
	public void LoadTree_file_nodes_are_lazy ()
	{
		Write (Path.Combine (root, "monodoc.xml"), Index);
		var res = HelpTreeService.LoadTree (new[] { root });

		var lazy = res.Root.Children.Single (n => n.ChildrenFile == "api.xml");
		Assert.False (lazy.IsChildrenLoaded);
		Assert.Equal (root, lazy.SourceDir);
		Assert.Equal (0, lazy.Children.Count);
		Assert.True (lazy.HasChildData);
	}

	[Fact]
	public void LoadChildren_parses_the_children_file_and_marks_loaded ()
	{
		Write (Path.Combine (root, "monodoc.xml"), Index);
		Write (Path.Combine (root, "api.xml"),
			"<nodes><node><name>System</name></node><node><name>Linq</name></node></nodes>");

		var res = HelpTreeService.LoadTree (new[] { root });
		var lazy = res.Root.Children.Single (n => n.ChildrenFile == "api.xml");

		Assert.True (HelpTreeService.LoadChildren (lazy));
		Assert.True (lazy.IsChildrenLoaded);
		Assert.Null (lazy.ChildrenFile);
		Assert.Equal (new[] { "System", "Linq" }, lazy.Children.Select (n => n.Caption));
	}

	[Fact]
	public void LoadChildren_without_a_file_fails_gracefully ()
	{
		var node = new HelpNode ("x");
		// No children file: the node is already complete, nothing to load.
		Assert.True (HelpTreeService.LoadChildren (node));

		Write (Path.Combine (root, "monodoc.xml"),
			"<root><nodes><node file=\"missing.xml\"><name>M</name></node></nodes></root>");
		var res = HelpTreeService.LoadTree (new[] { root });
		var lazy = Assert.Single (res.Root.Children);

		Assert.False (HelpTreeService.LoadChildren (lazy));
		Assert.True (lazy.IsChildrenLoaded);
		Assert.Empty (lazy.Children);
	}

	[Fact]
	public void LoadTree_missing_directory_reports_searched_and_no_root ()
	{
		var missing = Path.Combine (root, "does-not-exist");
		var res = HelpTreeService.LoadTree (new[] { missing });

		Assert.Null (res.Root);
		Assert.Equal (new[] { missing }, res.Searched);
	}

	[Fact]
	public void LoadTree_merges_all_existing_sources_into_one_root ()
	{
		var dirA = Path.Combine (root, "a");
		var dirB = Path.Combine (root, "b");
		Write (Path.Combine (dirA, "monodoc.xml"),
			"<root><name>A</name><nodes><node><name>NodeA</name></node></nodes></root>");
		Write (Path.Combine (dirB, "monodoc.xml"),
			"<root><name>B</name><nodes><node><name>NodeB</name></node></nodes></root>");

		var res = HelpTreeService.LoadTree (new[] { dirA, dirB, Path.Combine (root, "absent") });
		Assert.Equal (HelpTreeService.RootCaption, res.Root.Caption);
		Assert.Equal (new[] { "NodeA", "NodeB" }, res.Root.Children.Select (n => n.Caption));
		Assert.Equal (3, res.Searched.Count);
	}

	[Fact]
	public void LoadTree_malformed_xml_lands_in_errors_not_root ()
	{
		Write (Path.Combine (root, "monodoc.xml"), "<root><name>broken<root>");
		var res = HelpTreeService.LoadTree (new[] { root });

		Assert.Null (res.Root);
		Assert.Single (res.Errors);
	}

	[Fact]
	public void ResolveUrl_resolves_relative_and_keeps_absolute ()
	{
		Write (Path.Combine (root, "monodoc.xml"),
			"<root><nodes>" +
			"<node url=\"pages/gs.html\"><name>GS</name></node>" +
			"<node url=\"https://docs.example.com/index.html\"><name>Abs</name></node>" +
			"</nodes></root>");
		var res = HelpTreeService.LoadTree (new[] { root });

		Assert.Equal (
			Path.GetFullPath (Path.Combine (root, "pages/gs.html")),
			Path.GetFullPath (HelpTreeService.ResolveUrl (res.Root.Children[0])));
		Assert.Equal (
			"https://docs.example.com/index.html",
			HelpTreeService.ResolveUrl (res.Root.Children[1]));
		Assert.Null (HelpTreeService.ResolveUrl (new HelpNode ("bare")));
	}

	[Fact]
	public void ParseIndexFile_falls_back_to_filename_and_tolerates_nameless_nodes ()
	{
		var path = Path.Combine (root, "idx.xml");
		Write (path, "<root><nodes><node><summary>only summary</summary></node></nodes></root>");
		var parsed = HelpTreeService.ParseIndexFile (path);

		Assert.Equal ("idx", parsed.Caption);
		Assert.Equal (root, parsed.SourceDir);
		var child = Assert.Single (parsed.Children);
		Assert.Equal ("", child.Caption);
		Assert.Equal ("only summary", child.Summary);
		Assert.Equal (root, child.SourceDir);
	}
}
