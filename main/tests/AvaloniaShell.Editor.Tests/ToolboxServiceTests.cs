using System;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Services;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the toolbox pad model: built-in editor code templates + user
/// templates (legacy CodeTemplates v3.0 XML), merged and filtered by
/// ToolboxService the way the legacy DesignerSupport toolbox did.
/// </summary>
public class ToolboxServiceTests : IDisposable
{
	readonly string root;

	public ToolboxServiceTests ()
	{
		root = Path.Combine (Path.GetTempPath (), "toolboxservice-tests-" + Guid.NewGuid ().ToString ("N"));
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

	static ToolboxItem Item (string name, string category = ToolboxService.SnippetsCategory)
		=> new ToolboxItem (name, name, category, null, new[] { name + " code" });

	// ----- GetItems -----

	[Fact]
	public void GetItems_returns_the_builtin_editor_templates ()
	{
		var items = ToolboxService.GetItems (Path.Combine (root, "absent"));
		var builtins = MonoDevelop.Ide.Controls.SkTextEditor.EditorTemplates;

		Assert.Equal (builtins.Count, items.Count);
		Assert.Equal (
			builtins.Select (t => t.Name).OrderBy (n => n, StringComparer.Ordinal),
			items.Select (t => t.Shortcut).OrderBy (n => n, StringComparer.Ordinal));
		Assert.All (items, t => Assert.Equal (ToolboxService.SnippetsCategory, t.Category));
	}

	[Fact]
	public void GetItems_merges_user_templates_and_user_wins_by_shortcut ()
	{
		var dir = Path.Combine (root, "snippets");
		Write (Path.Combine (dir, "a.xml"), @"<?xml version=""1.0""?>
<CodeTemplates Version=""3.0"">
  <CodeTemplate>
    <Header>
      <Shortcut>cw</Shortcut>
      <_Description>user override</_Description>
    </Header>
    <Variables/>
    <Code><![CDATA[using {0};]]></Code>
  </CodeTemplate>
</CodeTemplates>");
		var builtinShortcuts = MonoDevelop.Ide.Controls.SkTextEditor.EditorTemplates
			.Select (t => t.Name).ToHashSet ();
		Assert.Contains ("cw", builtinShortcuts);

		var items = ToolboxService.GetItems (dir);

		// The user template replaces the built-in "cw": total = builtins + 1 - 1.
		Assert.Equal (MonoDevelop.Ide.Controls.SkTextEditor.EditorTemplates.Count, items.Count);
		var cw = Assert.Single (items.Where (t => t.Shortcut == "cw"));
		Assert.Equal ("user override", cw.Summary);
		Assert.Equal (new[] { "using {0};" }, cw.Code);
	}

	// ----- LoadUserTemplates -----

	[Fact]
	public void LoadUserTemplates_reads_group_description_and_code_lines ()
	{
		var dir = Path.Combine (root, "snippets");
		Write (Path.Combine (dir, "t.xml"), @"<?xml version=""1.0""?>
<CodeTemplates Version=""3.0"">
  <CodeTemplate>
    <Header>
      <_Group>My Snippets</_Group>
      <_Description>A console app</_Description>
      <Shortcut>main</Shortcut>
    </Header>
    <Variables/>
    <Code><![CDATA[static void Main()
{
}]]></Code>
  </CodeTemplate>
</CodeTemplates>");

		var item = Assert.Single (ToolboxService.LoadUserTemplates (dir));

		Assert.Equal ("main", item.Name);
		Assert.Equal ("My Snippets", item.Category);
		Assert.Equal ("A console app", item.Summary);
		Assert.Equal (new[] { "static void Main()", "{", "}" }, item.Code);
	}

	[Theory]
	[InlineData ("", "template without header")]
	[InlineData ("<Header><Context>csharp</Context><Shortcut>ok</Shortcut></Header>", "non-Standard context")]
	[InlineData ("<Header><_Description>d</_Description></Header>", "template without shortcut")]
	public void LoadUserTemplates_skips_templates_that_are_not_standard (string header, string _)
	{
		var dir = Path.Combine (root, "snippets");
		Write (Path.Combine (dir, "t.xml"), $@"<?xml version=""1.0""?>
<CodeTemplates Version=""3.0"">
  <CodeTemplate>
    {header}
    <Code><![CDATA[x]]></Code>
  </CodeTemplate>
</CodeTemplates>");

		Assert.Empty (ToolboxService.LoadUserTemplates (dir));
	}

	[Fact]
	public void LoadUserTemplates_skips_malformed_and_wrong_root_files ()
	{
		var dir = Path.Combine (root, "snippets");
		Write (Path.Combine (dir, "broken.xml"), "<CodeTemplates><CodeTemplate>broken");
		Write (Path.Combine (dir, "other.xml"), "<NotTemplates/>");
		Write (Path.Combine (dir, "ok.xml"), @"<?xml version=""1.0""?>
<CodeTemplates Version=""3.0"">
  <CodeTemplate>
    <Header><Shortcut>good</Shortcut></Header>
    <Code><![CDATA[good]]></Code>
  </CodeTemplate>
</CodeTemplates>");

		var items = ToolboxService.LoadUserTemplates (dir);

		Assert.Equal (new[] { "good" }, items.Select (t => t.Shortcut));
	}

	[Fact]
	public void LoadUserTemplates_missing_directory_yields_no_items ()
	{
		Assert.Empty (ToolboxService.LoadUserTemplates (Path.Combine (root, "absent")));
	}

	// ----- GetCategories -----

	[Fact]
	public void GetCategories_sorts_categories_and_items ()
	{
		var items = new[] { Item ("zeta", "B"), Item ("alpha", "B"), Item ("mid", "A") };
		var cats = ToolboxService.GetCategories (items);

		Assert.Equal (new[] { "A", "B" }, cats.Keys.ToArray ());
		Assert.Equal (new[] { "mid" }, cats ["A"].Select (t => t.Name).ToArray ());
		Assert.Equal (new[] { "alpha", "zeta" }, cats ["B"].Select (t => t.Name).ToArray ());
	}

	// ----- Filter -----

	[Fact]
	public void Filter_matches_name_case_insensitively_and_empty_term_matches_all ()
	{
		var items = new[] { Item ("cw"), Item ("fore"), Item ("svm") };

		Assert.Equal (3, ToolboxService.Filter (items, "").Count ());
		Assert.Equal (3, ToolboxService.Filter (items, "   ").Count ());
		Assert.Equal (new[] { "cw" }, ToolboxService.Filter (items, "CW").Select (t => t.Name).ToArray ());
		Assert.Equal (new[] { "fore" }, ToolboxService.Filter (items, "e").Select (t => t.Name).ToArray ());
		Assert.Empty (ToolboxService.Filter (items, "zzz"));
	}
}
