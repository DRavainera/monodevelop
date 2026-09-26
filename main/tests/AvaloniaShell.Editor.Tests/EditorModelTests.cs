using System;
using MonoDevelop.Ide.Controls;
using Xunit;

namespace AvaloniaShell.Editor.Tests;

/// <summary>
/// Tests for the SkTextEditor document model: line splitting, caret movement
/// and multi-caret editing (backspace/delete across all carets), mirroring the
/// legacy Mono.TextEditor behaviors the shell depends on.
///
/// SkTextEditor is an Avalonia Control; the model (lines/carets/text) works
/// without a running Avalonia platform, so these tests construct the control
/// directly and use the public editing API.
/// </summary>
public class EditorModelTests
{
	static SkTextEditor NewEditor (string text)
	{
		var ed = new SkTextEditor ();
		ed.Text = text;
		ed.IsDirty = false;
		return ed;
	}

	// ----- Lines model -----

	[Fact]
	public void Text_splits_lines_on_newline ()
	{
		var ed = NewEditor ("using System;\n\nclass C\n{\n}\n");
		// Trailing \n produces a final empty line (like the legacy document).
		Assert.Equal (6, ed.LineCountForTest);
		Assert.Equal ("class C", ed.LineTextForTest (2));
	}

	[Fact]
	public void Empty_text_has_one_empty_line ()
	{
		var ed = NewEditor ("");
		Assert.Equal (1, ed.LineCountForTest);
		Assert.Equal ("", ed.LineTextForTest (0));
	}

	[Fact]
	public void Windows_line_endings_are_normalized ()
	{
		var ed = NewEditor ("a\r\nb\r\nc");
		Assert.Equal (3, ed.LineCountForTest);
		Assert.Equal ("b", ed.LineTextForTest (1));
	}

	[Fact]
	public void Insert_grows_text_and_updates_line_model ()
	{
		var ed = NewEditor ("hello\nworld");
		ed.GotoLine (0);
		ed.GotoLineEnd ();
		ed.InsertAtCaret (" there");
		Assert.Equal ("hello there\nworld", ed.Text);
		Assert.Equal (11, ed.CurrentColumnForTest); // after " there"
	}

	// ----- Caret movement -----

	[Fact]
	public void GotoLine_positions_caret_at_line_start ()
	{
		var ed = NewEditor ("one\ntwo\nthree");
		ed.GotoLine (2);
		Assert.Equal (2, ed.CurrentLineForTest);
		Assert.Equal (0, ed.CurrentColumnForTest);
	}

	[Fact]
	public void GotoLineEnd_moves_to_end_of_line ()
	{
		var ed = NewEditor ("one\ntwo\nthree");
		ed.GotoLine (1);
		ed.GotoLineEnd ();
		Assert.Equal (1, ed.CurrentLineForTest);
		Assert.Equal (3, ed.CurrentColumnForTest);
	}

	[Fact]
	public void CaretRight_steps_through_text ()
	{
		var ed = NewEditor ("abc");
		ed.CaretRight (2);
		Assert.Equal (2, ed.CurrentColumnForTest);
	}

	// ----- Backspace (single caret) -----

	[Fact]
	public void Backspace_removes_char_before_caret ()
	{
		var ed = NewEditor ("hello\nworld");
		ed.GotoLine (1);
		ed.CaretRight (5); // end of "world"
		ed.BackspaceForQa ();
		Assert.Equal ("hello\nworl", ed.Text);
		Assert.Equal (4, ed.CurrentColumnForTest);
	}

	[Fact]
	public void Backspace_at_line_start_joins_with_previous_line ()
	{
		var ed = NewEditor ("one\ntwo");
		ed.GotoLine (1);
		ed.BackspaceForQa ();
		Assert.Equal ("onetwo", ed.Text);
		Assert.Equal (0, ed.CurrentLineForTest);
		Assert.Equal (3, ed.CurrentColumnForTest);
	}

	// ----- Multi-caret editing -----

	[Fact]
	public void InsertText_applies_to_all_carets ()
	{
		var ed = NewEditor ("ab\ncd");
		ed.GotoLine (0);
		ed.GotoLineEnd ();              // primary at end of "ab"
		ed.AddSecondaryCaretForTest (1, 2); // secondary at end of "cd"
		ed.InsertAtCaret ("X");
		Assert.Equal ("abX\ncdX", ed.Text);
	}

	[Fact]
	public void Backspace_multi_removes_char_at_every_caret ()
	{
		var ed = NewEditor ("ab\ncd");
		ed.GotoLine (0);
		ed.GotoLineEnd ();
		ed.AddSecondaryCaretForTest (1, 2);
		ed.BackspaceForQa ();
		Assert.Equal ("a\nc", ed.Text);
	}

	[Fact]
	public void Backspace_multi_joins_all_lines_when_carets_at_line_start ()
	{
		var ed = NewEditor ("one\ntwo\nthree");
		ed.GotoLine (0);
		// Carets at the start of lines 1 and 2 (primary sits at 0,0 where
		// backspace is a no-op — nothing before it on the first line).
		ed.AddSecondaryCaretForTest (1, 0);
		ed.AddSecondaryCaretForTest (2, 0);
		ed.BackspaceForQa ();
		Assert.Equal ("onetwothree", ed.Text);
		// The primary caret stays on line 0.
		Assert.Equal (0, ed.CurrentLineForTest);
	}

	[Fact]
	public void Plain_click_collapse_clears_secondary_carets ()
	{
		var ed = NewEditor ("ab\ncd");
		ed.AddSecondaryCaretForTest (1, 0);
		Assert.True (ed.HasSecondaryCarets);
		ed.CollapseToPrimaryCaret ();
		Assert.False (ed.HasSecondaryCarets);
		Assert.Equal (1, ed.Carets.Count);
	}

	// ----- Undo/Redo -----

	[Fact]
	public void Undo_restores_previous_text ()
	{
		var ed = NewEditor ("base");
		ed.GotoLineEnd ();
		ed.InsertAtCaret ("+1");
		Assert.Equal ("base+1", ed.Text);
		ed.Undo ();
		Assert.Equal ("base", ed.Text);
	}

	[Fact]
	public void Redo_reapplies_undone_edit ()
	{
		var ed = NewEditor ("base");
		ed.GotoLineEnd ();
		ed.InsertAtCaret ("+1");
		ed.Undo ();
		ed.Redo ();
		Assert.Equal ("base+1", ed.Text);
	}

	// ----- Dirty tracking -----

	[Fact]
	public void Edit_marks_editor_dirty ()
	{
		var ed = NewEditor ("x");
		Assert.False (ed.IsDirty);
		ed.GotoLineEnd ();
		ed.InsertAtCaret ("y");
		Assert.True (ed.IsDirty);
	}
}
