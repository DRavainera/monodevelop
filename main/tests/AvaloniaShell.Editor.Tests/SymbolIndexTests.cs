using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Services;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the text-based symbol surfaces that replaced the Roslyn-backed
/// legacy pads: the flat scanner (breadcrumb / Go To Type / search popup), the
/// Document Outline tree and the Classes tree. The scanner is pure text
/// processing, so no Avalonia platform is needed.
/// </summary>
public class SymbolIndexTests
{
	const string Sample = """
		using System;

		namespace Demo.Sample
		{
			public class Program
			{
				static int counter;

				public event EventHandler Changed;

				public int Value { get; set; }

				static void Main (string [] args)
				{
					Console.WriteLine ("hi");
				}

				int Double (int x) => x * 2;
			}

			interface IThing
			{
				void Run ();
			}
		}
		""";

	// ----- ScanSymbols -----

	[Fact]
	public void ScanSymbols_finds_types_and_members_with_lines ()
	{
		var hits = SymbolIndexService.ScanSymbols (Sample);
		var names = hits.Select (h => h.Name).ToList ();

		Assert.Contains ("Program", names);
		Assert.Contains ("IThing", names);
		Assert.Contains ("Main", names);
		Assert.Contains ("Double", names);
		Assert.Contains ("Value", names);
		Assert.Contains ("Changed", names);
		Assert.Contains ("counter", names);
	}

	[Fact]
	public void ScanSymbols_reports_kind_and_container ()
	{
		var hits = SymbolIndexService.ScanSymbols (Sample);

		var program = hits.Single (h => h.Name == "Program");
		Assert.Equal ("class", program.Kind);
		Assert.Null (program.Container);

		var main = hits.Single (h => h.Name == "Main");
		Assert.Equal ("method", main.Kind);
		Assert.Equal ("Program", main.Container);

		var value = hits.Single (h => h.Name == "Value");
		Assert.Equal ("property", value.Kind);
		Assert.Equal ("Program", value.Container);
	}

	[Fact]
	public void ScanSymbols_ignores_comments_and_braces ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			// class NotAType
			/* class AlsoNotAType */
			class Real
			{
			}
			""");

		Assert.Single (hits);
		Assert.Equal ("Real", hits [0].Name);
	}

	[Fact]
	public void ScanSymbols_keeps_container_across_allman_braces ()
	{
		// A lone "{" sits at the same indent as its owner; popping scopes on it
		// would evict the type pushed by the previous line.
		var hits = SymbolIndexService.ScanSymbols ("""
			class Outer
			{
				void Inner ()
				{
				}
			}
			""");

		Assert.Equal ("Outer", hits.Single (h => h.Name == "Inner").Container);
	}

	[Fact]
	public void ScanSymbols_returns_empty_for_plain_text ()
	{
		Assert.Empty (SymbolIndexService.ScanSymbols ("just some words\nand more words\n"));
	}

	// ----- ScanNamespaces -----

	[Fact]
	public void ScanNamespaces_handles_block_and_file_scoped_forms ()
	{
		var block = SymbolIndexService.ScanNamespaces ("namespace A.B\n{\n}\n");
		Assert.Equal (("A.B", 1), (block [0].Name, block [0].Line));

		var fileScoped = SymbolIndexService.ScanNamespaces ("namespace C.D;\nclass X { }\n");
		Assert.Equal ("C.D", fileScoped [0].Name);
	}

	// ----- BuildOutline -----

	[Fact]
	public void BuildOutline_nests_members_under_their_type ()
	{
		var roots = SymbolIndexService.BuildOutline (Sample);

		var program = roots.Single (n => n.Name == "Program");
		var childNames = program.Children.Select (c => c.Name).ToList ();
		Assert.Contains ("Main", childNames);
		Assert.Contains ("Double", childNames);
		Assert.Contains ("Value", childNames);

		// The interface is a sibling of the class, not one of its children.
		Assert.Contains (roots, n => n.Name == "IThing");
		Assert.DoesNotContain (program.Children, n => n.Name == "IThing");
	}

	[Fact]
	public void BuildOutline_preserves_source_order ()
	{
		var roots = SymbolIndexService.BuildOutline (Sample);
		var program = roots.Single (n => n.Name == "Program");
		var lines = program.Children.Select (c => c.Line).ToList ();

		Assert.Equal (lines.OrderBy (l => l).ToList (), lines);
	}

	// ----- BuildClassTree -----

	static List<SymbolIndexService.ClassNode> BuildTreeFromText (string text)
	{
		var dir = Path.Combine (Path.GetTempPath (), "md-symbol-tests-" + Path.GetRandomFileName ());
		Directory.CreateDirectory (dir);
		var file = Path.Combine (dir, "Sample.cs");
		File.WriteAllText (file, text);
		try {
			return SymbolIndexService.BuildClassTree (new [] { ("Demo", file) });
		} finally {
			Directory.Delete (dir, true);
		}
	}

	[Fact]
	public void BuildClassTree_groups_project_namespace_type_member ()
	{
		var tree = BuildTreeFromText (Sample);

		var project = Assert.Single (tree);
		Assert.Equal ("project", project.Kind);
		Assert.Equal ("Demo", project.Name);

		var ns = Assert.Single (project.Children);
		Assert.Equal ("namespace", ns.Kind);
		Assert.Equal ("Demo.Sample", ns.Name);

		var program = ns.Children.Single (c => c.Name == "Program");
		Assert.Equal ("class", program.Kind);
		Assert.NotNull (program.File);
		Assert.True (program.Line > 0);
		Assert.Contains (program.Children, c => c.Name == "Main");
	}

	[Fact]
	public void BuildClassTree_orders_members_by_legacy_kind_rank ()
	{
		var tree = BuildTreeFromText (Sample);
		var program = tree [0].Children [0].Children.Single (c => c.Name == "Program");
		var kinds = program.Children.Select (c => c.Kind).ToList ();

		// Legacy ClassNodeBuilder order: field, event, property, method.
		Assert.Equal (new [] { "field", "event", "property", "method", "method" }, kinds);
	}

	[Fact]
	public void BuildClassTree_skips_unreadable_files ()
	{
		var tree = SymbolIndexService.BuildClassTree (new [] { ("Demo", "/nonexistent/NoSuchFile.cs") });
		Assert.Empty (tree);
	}

	[Fact]
	public void BuildClassTree_ignores_top_level_statements ()
	{
		var tree = BuildTreeFromText ("System.Console.WriteLine (\"hi\");\n");
		Assert.Empty (tree);
	}

	// ----- ScanFile cache -----

	[Fact]
	public void ScanFile_returns_empty_for_missing_file ()
	{
		Assert.Empty (SymbolIndexService.ScanFile ("/nonexistent/NoSuchFile.cs"));
	}

	// ----- Regression tests (QA M26: indentation must not turn statement
	// keywords into members; Allman properties and modifier-less fields must be
	// found; comment/verbatim-string bodies must not become symbols). -----

	[Fact]
	public void ScanSymbols_does_not_treat_indented_control_flow_as_members ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			class C
			{
				public void Run ()
				{
					if (true)
					{
						while (false) { }
					}
					foreach (var v in new int [0])
					{
					}
				}
			}
			""");
		var names = hits.Select (h => h.Name).ToList ();

		Assert.Contains ("C", names);
		Assert.Contains ("Run", names);
		Assert.DoesNotContain ("if", names);
		Assert.DoesNotContain ("while", names);
		Assert.DoesNotContain ("foreach", names);
	}

	[Fact]
	public void ScanSymbols_finds_allman_property_and_ignores_accessors ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			class C
			{
				public int Prop
				{
					get { return 1; }
					set { }
				}
			}
			""");
		var names = hits.Select (h => h.Name).ToList ();

		Assert.Equal ("property", hits.Single (h => h.Name == "Prop").Kind);
		Assert.DoesNotContain ("get", names);
		Assert.DoesNotContain ("set", names);
	}

	[Fact]
	public void ScanSymbols_finds_field_without_modifier ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			class C
			{
				int Field;
			}
			""");

		Assert.Equal ("field", hits.Single (h => h.Name == "Field").Kind);
	}

	[Fact]
	public void ScanSymbols_skips_block_comments_and_verbatim_strings ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			/*
			class Fake1
			*/
			var s = @"line1
			class Fake2
			line3";
			class Real { }
			""");

		Assert.Single (hits);
		Assert.Equal ("Real", hits [0].Name);
	}

	[Fact]
	public void ScanSymbols_does_not_treat_throw_new_or_yield_return_as_methods ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			class C
			{
				void Run ()
				{
					throw new InvalidOperationException ();
					yield return GetItem ();
					return Compute ();
				}
				int GetItem () => 1;
			}
			""");
		var names = hits.Select (h => h.Name).ToList ();

		Assert.Contains ("Run", names);
		Assert.Equal (1, hits.Count (h => h.Name == "GetItem"));
		Assert.DoesNotContain ("InvalidOperationException", names);
		Assert.DoesNotContain ("Compute", names);
	}

	[Fact]
	public void ScanSymbols_recognizes_record_types ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			public record Point (int X, int Y);
			public record struct Vec (int X);
			""");
		var names = hits.Select (h => h.Name).ToList ();

		Assert.Equal ("record", hits.Single (h => h.Name == "Point").Kind);
		Assert.Equal ("record", hits.Single (h => h.Name == "Vec").Kind);
		Assert.DoesNotContain ("X", names);
	}

	[Fact]
	public void ScanSymbols_finds_indexer ()
	{
		var hits = SymbolIndexService.ScanSymbols ("""
			class C
			{
				public int this [int i] => i;
			}
			""");

		Assert.Equal ("property", hits.Single (h => h.Name == "this[]").Kind);
		Assert.DoesNotContain (hits, h => h.Name == "i");
	}

	[Fact]
	public void ScanSymbols_skips_raw_string_bodies ()
	{
		// Outer raw string uses four quotes so the content can hold ```
		var hits = SymbolIndexService.ScanSymbols (""""
			class Real { }
			var s = """
			class Fake
			""";
			"""");

		Assert.Single (hits);
		Assert.Equal ("Real", hits [0].Name);
	}
}
