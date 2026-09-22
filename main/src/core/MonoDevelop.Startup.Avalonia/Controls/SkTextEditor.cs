using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Avalonia.Threading;
using SkiaSharp;

namespace MonoDevelop.AvaloniaShell.Controls;

/// <summary>
/// Text editor control rendered through SkiaSharp (the replacement of the legacy
/// Mono.Cairo-rendered Mono.TextEditor): gutter with line numbers, caret, keyboard
/// editing, wheel scrolling and simple C# syntax highlighting.
/// </summary>
public class SkTextEditor : Control
{
	const string Keywords = "abstract as async await base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach get goto if implicit in init int interface internal is lock long namespace new null object operator out override params partial private protected public readonly record ref return sbyte sealed set short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var virtual void volatile while nameof typeof value";

	class Segment
	{
		public string Text = "";
		public SKColor Color;
	}

	readonly List<string> lines = new ();
	int caretLine, caretCol;
	double scrollLines;

	// CPU raster buffer: Skia draws straight into the memory of an Avalonia
	// WriteableBitmap, which is then presented via DrawImage. This is the
	// supported interop path in Avalonia 12 (no internal lease APIs needed).
	WriteableBitmap? buffer;
	int bufferW, bufferH;
	bool dirty = true;
	bool caretVisible = true;
	DispatcherTimer? blinkTimer;

	#region Document identity (legacy FileTextLogic: file + dirty state)

	// Backing file path — set when opened from the Solution pad or File > Open.
	// Empty for untitled documents (like the legacy "Untitled" documents).
	public string FilePath { get; set; } = "";

	// Legacy IsDirty: the tab shows the modified marker (dot) and File > Save enables.
	public static readonly StyledProperty<bool> IsDirtyProperty =
		AvaloniaProperty.Register<SkTextEditor, bool> (nameof (IsDirty));

	public bool IsDirty {
		get => GetValue (IsDirtyProperty);
		set => SetValue (IsDirtyProperty, value);
	}

	// 0-based caret line (legacy GetCurrentLine); status bar / goto handlers use it.
	public int CurrentLine => caretLine;

	// Current selection as plain text (empty when collapsed — legacy GetSelectedText).
	public string SelectedText {
		get {
			if (!hasSelection)
				return "";
			var (sl, sc, el, ec) = SelectionRange ();
			return string.Join ("\n", SliceByLines ((sl, sc), (el, ec)));
		}
	}

	// Legacy SaveCommand: FileService.Save. Persisted changes are written to FilePath;
	// untitled docs surface an error in the Output pad.
	public void Save ()
	{
		if (string.IsNullOrEmpty (FilePath)) {
			Console.WriteLine ("[skeditor] cannot save untitled document (no file path)");
			return;
		}
		File.WriteAllText (FilePath, Text);
		IsDirty = false;
		Console.WriteLine ($"[skeditor] saved {FilePath}");
	}

	// Legacy SearchService.FindNext: find from the caret and place it at the match.
	public bool FindFromCaret (string needle, bool forward = true)
	{
		if (string.IsNullOrEmpty (needle))
			return false;
		var text = string.Join ("\n", lines);
		int pos = caretLine >= 0 && caretLine < lines.Count
			? lines.Take (caretLine).Sum (l => l.Length + 1) + Math.Min (caretCol, lines [caretLine].Length)
			: 0;
		int idx = forward
			? text.IndexOf (needle, Math.Min (pos + 1, text.Length), StringComparison.Ordinal)
			: text.LastIndexOf (needle, Math.Max (0, pos - 1), StringComparison.Ordinal);
		if (idx < 0)
			idx = forward ? text.IndexOf (needle, StringComparison.Ordinal)
				: text.LastIndexOf (needle, StringComparison.Ordinal);
		if (idx < 0)
			return false;
		var (l, c) = OffsetToPosition (idx);
		caretLine = l;
		caretCol = c;
		hasSelection = false;
		selAnchorLine = l;
		selAnchorCol = c;
		EnsureCaretVisible ();
		MarkDirty ();
		return true;
	}

	// Legacy SearchManager/GotoLineNumber (Ctrl+G): caret to a 1-based line.
	public void GotoLine (int zeroBasedLine)
	{
		caretLine = Math.Clamp (zeroBasedLine, 0, lines.Count - 1);
		caretCol = Math.Clamp (caretCol, 0, lines [caretLine].Length);
		hasSelection = false;
		EnsureCaretVisible ();
		MarkDirty ();
	}

	// End-key behavior: move the caret to the end of the current line.
	public void GotoLineEnd ()
	{
		caretCol = lines [caretLine].Length;
		hasSelection = false;
		EnsureCaretVisible ();
		MarkDirty ();
	}

	(int Line, int Col) OffsetToPosition (int offset)
	{
		int acc = 0;
		for (int i = 0; i < lines.Count; i++) {
			var len = lines [i].Length + 1;
			if (offset < acc + len)
				return (i, offset - acc);
			acc += len;
		}
		return (lines.Count - 1, lines [^1].Length);
	}

	void EnsureCaretVisible ()
	{
		float lineH = LineHeight;
		if (caretLine < scrollLines)
			scrollLines = caretLine;
		else if (caretLine > scrollLines + Bounds.Height / lineH - 2)
			scrollLines = Math.Max (0, caretLine - Bounds.Height / lineH + 2);
	}

	#endregion

	#region Styled properties

	public static readonly StyledProperty<string> TextProperty =
		AvaloniaProperty.Register<SkTextEditor, string> (nameof (Text), "");

	public static readonly StyledProperty<double> FontSizeProperty =
		AvaloniaProperty.Register<SkTextEditor, double> (nameof (FontSize), 13d);

	public static readonly StyledProperty<IBrush?> BackgroundProperty =
		AvaloniaProperty.Register<SkTextEditor, IBrush?> (nameof (Background));

	public static readonly StyledProperty<IBrush?> ForegroundProperty =
		AvaloniaProperty.Register<SkTextEditor, IBrush?> (nameof (Foreground));

	public static readonly StyledProperty<IBrush?> GutterBackgroundProperty =
		AvaloniaProperty.Register<SkTextEditor, IBrush?> (nameof (GutterBackground));

	public static readonly StyledProperty<IBrush?> GutterForegroundProperty =
		AvaloniaProperty.Register<SkTextEditor, IBrush?> (nameof (GutterForeground));

	public static readonly StyledProperty<IBrush?> CaretBrushProperty =
		AvaloniaProperty.Register<SkTextEditor, IBrush?> (nameof (CaretBrush));

	public string Text {
		get => GetValue (TextProperty);
		set => SetValue (TextProperty, value);
	}

	public double FontSize {
		get => GetValue (FontSizeProperty);
		set => SetValue (FontSizeProperty, value);
	}

	public IBrush? Background {
		get => GetValue (BackgroundProperty);
		set => SetValue (BackgroundProperty, value);
	}

	public IBrush? Foreground {
		get => GetValue (ForegroundProperty);
		set => SetValue (ForegroundProperty, value);
	}

	public IBrush? GutterBackground {
		get => GetValue (GutterBackgroundProperty);
		set => SetValue (GutterBackgroundProperty, value);
	}

	public IBrush? GutterForeground {
		get => GetValue (GutterForegroundProperty);
		set => SetValue (GutterForegroundProperty, value);
	}

	public IBrush? CaretBrush {
		get => GetValue (CaretBrushProperty);
		set => SetValue (CaretBrushProperty, value);
	}

	#endregion

	static SkTextEditor ()
	{
		AffectsRender<SkTextEditor> (TextProperty, BackgroundProperty, ForegroundProperty, GutterBackgroundProperty, GutterForegroundProperty, CaretBrushProperty);
		AffectsMeasure<SkTextEditor> (FontSizeProperty);
	}

	public SkTextEditor ()
	{
		Focusable = true;
		blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds (530) };
		blinkTimer.Tick += (_, _) => {
			if (IsFocused) {
				caretVisible = !caretVisible;
				MarkDirty ();
			}
		};
		blinkTimer.Start ();
	}

	void MarkDirty ()
	{
		dirty = true;
		InvalidateVisual ();
	}

	protected override void OnGotFocus (FocusChangedEventArgs e)
	{
		base.OnGotFocus (e);
		caretVisible = true;
		MarkDirty ();
	}

	protected override void OnLostFocus (FocusChangedEventArgs e)
	{
		base.OnLostFocus (e);
		MarkDirty ();
	}

	// Text dependency: sync external value into the model.
	protected override void OnPropertyChanged (AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged (e);
		if (e.Property == TextProperty) {
			SetLines (e.NewValue as string ?? "");
		} else if (e.Property == FontSizeProperty) {
			InvalidateVisual ();
		}
	}

	void SetLines (string text)
	{
		lines.Clear ();
		lines.AddRange (text.Replace ("\r\n", "\n").Split ('\n'));
		if (lines.Count == 0)
			lines.Add ("");
		caretLine = Math.Clamp (caretLine, 0, lines.Count - 1);
		caretCol = Math.Clamp (caretCol, 0, lines [caretLine].Length);
		MarkDirty ();
	}

	#region Metrics

	static SKTypeface? typeface;
	static readonly object fontLock = new ();

	static SKTypeface GetMonospaceTypeface ()
	{
		lock (fontLock) {
			typeface ??= SKTypeface.FromFamilyName ("DejaVu Sans Mono", SKFontStyle.Normal)
				?? SKTypeface.FromFamilyName ("Consolas") ?? SKTypeface.Default;
			return typeface;
		}
	}

	SKFont CreateFont () => new (GetMonospaceTypeface (), (float)FontSize);

	float CharWidth (SKFont font)
	{
		var widths = font.GetGlyphWidths ("M", out _);
		return widths.Length > 0 ? widths [0] : (float)FontSize * 0.6f;
	}

	float LineHeight => (float)(FontSize * 1.35);

	#endregion

	#region Input

	protected override void OnTextInput (TextInputEventArgs e)
	{
		base.OnTextInput (e);
		var text = e.Text;
		if (string.IsNullOrEmpty (text))
			return;
		InsertText (text);
		e.Handled = true;
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		base.OnKeyDown (e);
		var ctrl = e.KeyModifiers.HasFlag (KeyModifiers.Control);
		var altShift = e.KeyModifiers.HasFlag (KeyModifiers.Alt) && e.KeyModifiers.HasFlag (KeyModifiers.Shift);
		switch (e.Key) {
		case Key.OemPeriod when altShift:
			// Legacy InsertNextMatchingCaret: Alt+Shift+.
			InsertNextMatchingCaret ();
			e.Handled = true;
			return;
		case Key.OemComma when altShift:
			// Legacy RemoveLastSecondaryCaret: Alt+Shift+,
			RemoveLastSecondaryCaret ();
			e.Handled = true;
			return;
		case Key.A when altShift:
			// Legacy InsertAllMatchingCarets: Alt+Shift+A
			InsertAllMatchingCarets ();
			e.Handled = true;
			return;
		case Key.Escape:
			// Legacy: Escape collapses multi-caret back to the primary caret.
			if (secondaryCarets.Count > 0) {
				ClearSecondaryCarets ();
				e.Handled = true;
				return;
			}
			break;
		case Key.X when ctrl:
			CutSelection ();
			e.Handled = true;
			return;
		case Key.C when ctrl:
			CopySelection ();
			e.Handled = true;
			return;
		case Key.V when ctrl:
			PasteClipboard ();
			e.Handled = true;
			return;
		case Key.A when ctrl:
			SelectAll ();
			e.Handled = true;
			return;
		case Key.K when ctrl:
			// Legacy TextEditorCommands.DeleteToLineEnd (Control|K).
			DeleteToLineEnd ();
			e.Handled = true;
			return;
		case Key.D when ctrl && e.KeyModifiers.HasFlag (KeyModifiers.Shift):
			// Legacy TextEditorCommands.DuplicateLine.
			DuplicateLine ();
			e.Handled = true;
			return;
		case Key.Up when e.KeyModifiers.HasFlag (KeyModifiers.Alt):
			MoveBlockUp ();
			e.Handled = true;
			return;
		case Key.Down when e.KeyModifiers.HasFlag (KeyModifiers.Alt):
			MoveBlockDown ();
			e.Handled = true;
			return;
		}
		switch (e.Key) {
		case Key.Back:
			if (caretCol > 0) {
				var line = lines [caretLine];
				lines [caretLine] = line.Remove (caretCol - 1, 1);
				caretCol--;
			} else if (caretLine > 0) {
				caretCol = lines [caretLine - 1].Length;
				lines [caretLine - 1] += lines [caretLine];
				lines.RemoveAt (caretLine);
				caretLine--;
			}
			Commit ();
			e.Handled = true;
			break;
		case Key.Delete:
			var delLine = lines [caretLine];
			if (caretCol < delLine.Length)
				lines [caretLine] = delLine.Remove (caretCol, 1);
			else if (caretLine < lines.Count - 1) {
				lines [caretLine] += lines [caretLine + 1];
				lines.RemoveAt (caretLine + 1);
			}
			Commit ();
			e.Handled = true;
			break;
		case Key.Enter:
			InsertText ("\n");
			e.Handled = true;
			break;
		case Key.Tab:
			InsertText ("    ");
			e.Handled = true;
			break;
		case Key.Left:
			if (caretCol > 0)
				caretCol--;
			else if (caretLine > 0) {
				caretLine--;
				caretCol = lines [caretLine].Length;
			}
			ShowCaret ();
			e.Handled = true;
			break;
		case Key.Right:
			if (caretCol < lines [caretLine].Length)
				caretCol++;
			else if (caretLine < lines.Count - 1) {
				caretLine++;
				caretCol = 0;
			}
			ShowCaret ();
			e.Handled = true;
			break;
		case Key.Up:
			if (caretLine > 0) {
				caretLine--;
				caretCol = Math.Min (caretCol, lines [caretLine].Length);
			}
			ShowCaret ();
			e.Handled = true;
			break;
		case Key.Down:
			if (caretLine < lines.Count - 1) {
				caretLine++;
				caretCol = Math.Min (caretCol, lines [caretLine].Length);
			}
			ShowCaret ();
			e.Handled = true;
			break;
		case Key.Home:
			caretCol = 0;
			ShowCaret ();
			e.Handled = true;
			break;
		case Key.End:
			caretCol = lines [caretLine].Length;
			ShowCaret ();
			e.Handled = true;
			break;
		}
	}

	// Selection (legacy editor selection model): anchor at press, extend on drag.
	bool hasSelection;
	int selAnchorLine, selAnchorCol;
	bool dragging;

	(int StartLine, int StartCol, int EndLine, int EndCol) SelectionRange ()
	{
		bool anchorFirst = selAnchorLine < caretLine
			|| (selAnchorLine == caretLine && selAnchorCol <= caretCol);
		return anchorFirst
			? (selAnchorLine, selAnchorCol, caretLine, caretCol)
			: (caretLine, caretCol, selAnchorLine, selAnchorCol);
	}

	(IEnumerable<string> lines, (int Line, int Col) Start, (int Line, int Col) End) SelectionSlices ()
	{
		var (sl, sc, el, ec) = SelectionRange ();
		var parts = new List<string> ();
		if (sl == el) {
			parts.Add (lines [sl].Substring (sc, Math.Max (0, ec - sc)));
		} else {
			parts.Add (lines [sl].Substring (sc));
			for (int i = sl + 1; i < el; i++)
				parts.Add (lines [i]);
			parts.Add (lines [el].Substring (0, ec));
		}
		return (parts, (sl, sc), (el, ec));
	}

	string[] SliceByLines ((int Line, int Col) a, (int Line, int Col) b)
	{
		var (sl, sc, el, ec) = (a.Line, a.Col, b.Line, b.Col);
		var parts = new List<string> ();
		if (sl == el) {
			parts.Add (lines [sl].Substring (Math.Min (sc, lines [sl].Length), Math.Max (0, Math.Min (ec, lines [el].Length) - Math.Min (sc, lines [sl].Length))));
		} else {
			parts.Add (lines [sl].Substring (Math.Min (sc, lines [sl].Length)));
			for (int i = sl + 1; i < el; i++)
				parts.Add (lines [i]);
			parts.Add (lines [el].Substring (0, Math.Min (ec, lines [el].Length)));
		}
		return parts.ToArray ();
	}

	(int Start, int End) SelectionOrder ()
	{
		var (sl, sc, el, ec) = SelectionRange ();
		int start = lines.Take (sl).Sum (l => l.Length + 1) + sc;
		int end = lines.Take (el).Sum (l => l.Length + 1) + ec;
		return (start, end);
	}

	protected override void OnPointerWheelChanged (PointerWheelEventArgs e)
	{
		base.OnPointerWheelChanged (e);
		scrollLines = Math.Max (0, scrollLines - e.Delta.Y * 3);
		scrollLines = Math.Min (scrollLines, Math.Max (0, lines.Count - 1));
		InvalidateVisual ();
		e.Handled = true;
	}

	protected override void OnPointerPressed (PointerPressedEventArgs e)
	{
		base.OnPointerPressed (e);
		Focus ();
		var pt = e.GetCurrentPoint (this);
		if (pt.Properties.IsLeftButtonPressed) {
			double charW = FontSize * 0.6;
			int col = Math.Max (0, (int)((pt.Position.X - GutterWidth ()) / charW));
			int line = (int)(pt.Position.Y / LineHeight + scrollLines);
			caretLine = Math.Clamp (line, 0, lines.Count - 1);
			caretCol = Math.Clamp (col, 0, lines [caretLine].Length);
			selAnchorLine = caretLine;
			selAnchorCol = caretCol;
			hasSelection = false;
			dragging = true;
			InvalidateVisual ();
			e.Handled = true;
		}
	}

	protected override void OnPointerMoved (PointerEventArgs e)
	{
		base.OnPointerMoved (e);
		if (!dragging)
			return;
		var pt = e.GetPosition (this);
		double charW = FontSize * 0.6;
		int col = Math.Max (0, (int)((pt.X - GutterWidth ()) / charW));
		int line = (int)(pt.Y / LineHeight + scrollLines);
		caretLine = Math.Clamp (line, 0, lines.Count - 1);
		caretCol = Math.Clamp (col, 0, lines [caretLine].Length);
		hasSelection = caretLine != selAnchorLine || caretCol != selAnchorCol;
		MarkDirty ();
	}

	protected override void OnPointerReleased (PointerReleasedEventArgs e)
	{
		base.OnPointerReleased (e);
		dragging = false;
	}

	void InsertText (string text)
	{
		foreach (var ch in text) {
			if (ch == '\n') {
				var tail = lines [caretLine].Substring (caretCol);
				lines [caretLine] = lines [caretLine].Substring (0, caretCol);
				lines.Insert (caretLine + 1, tail);
				caretLine++;
				caretCol = 0;
			} else {
				var line = lines [caretLine];
				lines [caretLine] = line.Insert (caretCol, ch.ToString ());
				caretCol++;
			}
		}
		Commit ();
	}

	void PushUndo (string before)
	{
		if (undoing)
			return;
		var joined = string.Join ("\n", lines);
		if (before == joined)
			return;
		undoStack.Add ((before, caretLine, caretCol));
		if (undoStack.Count > 200)
			undoStack.RemoveAt (0);
		redoStack.Clear ();
	}

	void Commit ()
	{
		var before = Text ?? "";
		var joined = string.Join ("\n", lines);
		PushUndo (before);
		SetValue (TextProperty, joined);
		if (!IsDirty)
			IsDirty = true; // user edit → legacy modified marker on the tab
		ShowCaret ();
	}

	void ShowCaret ()
	{
		MarkDirty ();
	}

	// ----- Clipboard & selection commands (legacy EditCommands.Cut/Copy/Paste/SelectAll) -----

	public void CutSelection ()
	{
		if (!hasSelection)
			return;
		CopySelection ();
		ReplaceSelection ("");
	}

	public void CopySelection ()
	{
		if (hasSelection)
			TopLevel.GetTopLevel (this)?.Clipboard?.SetTextAsync (SelectedText);
	}

	public async void PasteClipboard ()
	{
		var cb = TopLevel.GetTopLevel (this)?.Clipboard;
		if (cb is null)
			return;
		var text = await cb.TryGetTextAsync ();
		if (string.IsNullOrEmpty (text))
			return;
		ReplaceSelection (text);
	}

	public void SelectAll ()
	{
		selAnchorLine = 0;
		selAnchorCol = 0;
		caretLine = lines.Count - 1;
		caretCol = lines [^1].Length;
		hasSelection = caretLine != 0 || caretCol != 0 || lines.Count > 1;
		ShowCaret ();
	}

	void ReplaceSelection (string text)
	{
		if (hasSelection) {
			var (start, end) = SelectionOrder ();
			var t = Text ?? "";
			Text = t.Substring (0, Math.Min (start, t.Length)) + text + t.Substring (Math.Min (end, t.Length));
			hasSelection = false;
			// Position the caret at the end of the inserted text (legacy behavior).
			var (cl, cc) = OffsetToPosition (start + text.Length);
			caretLine = cl;
			caretCol = cc;
			selAnchorLine = cl;
			selAnchorCol = cc;
		} else {
			InsertText (text);
		}
		Commit ();
	}

	// Legacy parse from GotoLineNumberWidget: "N", "N:C", "N,C", "+N/-N" relative.
	public static (int Line, int Col) ParseGotoInput (string s, int currentLine1Based)
	{
		s = s.Trim ();
		bool relative = s.StartsWith ("+", StringComparison.Ordinal) || s.StartsWith ("-", StringComparison.Ordinal);
		int cut = relative ? 1 : 0;
		int line = currentLine1Based, col = 1;
		var head = s;
		int sep = s.IndexOfAny (new [] { ':', ',' });
		if (sep >= 0) {
			head = s.Substring (0, sep);
			_ = int.TryParse (s.Substring (sep + 1).Trim (), out col);
		}
		if (int.TryParse (head.Length > cut ? head [cut..] : "", out int n) && n != 0)
			line = relative ? currentLine1Based + n : n;
		return (line, col);
	}

	// Legacy GotoLineNumberWidget: "N", "N:C", "+N", "-N" … shown as an in-editor
	// overlay (the legacy is a Gtk.Bin placed over the text area, not a popup window).
	Border? gotoOverlay;

	public event EventHandler? GotoOverlayClosed;

	public void GotoLinePopup ()
	{
		if (gotoOverlay is not null)
			return;
		var currentLine = caretLine + 1;
		var box = new TextBox {
			Text = currentLine.ToString (),
			Width = Math.Max (140, Bounds.Width / 4),
			Height = 26,
			FontSize = 12,
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
			VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
			Margin = new Thickness (0, 6, 8, 0),
		};
		void CloseOverlay ()
		{
			if (gotoOverlay is Border b) {
				((Panel)b.Parent!).Children.Remove (b);
				gotoOverlay = null;
				Focus ();
				GotoOverlayClosed?.Invoke (this, EventArgs.Empty);
			}
		}
		void Accept ()
		{
			var (line, col) = ParseGotoInput (box.Text ?? "", currentLine);
			GotoLine (line - 1);
			if (col > 1) {
				caretCol = Math.Clamp (col - 1, 0, lines [caretLine].Length);
				ShowCaret ();
			}
			CloseOverlay ();
		}
		box.KeyDown += (_, e) => {
			if (e.Key == Key.Enter) {
				Accept ();
				e.Handled = true;
			} else if (e.Key == Key.Escape) {
				CloseOverlay ();
				e.Handled = true;
			}
		};
		box.LostFocus += (_, _) => CloseOverlay ();
		var host = new Border {
			Background = Brushes.Transparent,
			Child = box,
		};
		gotoOverlay = host;
		if (this.GetVisualParent () is Panel p) {
			p.Children.Add (host);
			host.IsVisible = true;
			box.Focus ();
			box.SelectAll ();
			box.CaretIndex = box.Text?.Length ?? 0;
		}
	}

	// Legacy pad jump (ErrorListPad → ILocationList): line + column without popup.
	public void GotoLinePopupColumn (int column)
	{
		if (column > 1) {
			caretCol = Math.Clamp (column - 1, 0, lines [caretLine].Length);
			ShowCaret ();
		}
	}

	// ----- Line operations (legacy TextEditorCommands.DeleteLine/DuplicateLine,
	// EditCommands.ToggleCodeComment/JoinWithNextLine/SortSelectedLines …) -----

	void MutateLines (Action<List<string>> mutator, (int Line, int Col)? newCaret = null)
	{
		var before = Text ?? "";
		mutator (lines);
		PushUndo (before);
		SetValue (TextProperty, string.Join ("\n", lines));
		if (newCaret is { } nc) {
			caretLine = Math.Clamp (nc.Line, 0, lines.Count - 1);
			caretCol = Math.Clamp (nc.Col, 0, lines [caretLine].Length);
		}
		hasSelection = false;
		IsDirty = true;
		EnsureCaretVisible ();
		MarkDirty ();
	}

	public void DeleteLine ()
	{
		if (lines.Count <= 1) {
			lines [0] = "";
			caretCol = 0;
			MarkDirty ();
			return;
		}
		int at = caretLine;
		int col = caretCol;
		MutateLines (ls => ls.RemoveAt (at), (at, col));
	}

	public void DeleteToLineStart ()
	{
		MutateLines (ls => { ls [caretLine] = ls [caretLine].Substring (Math.Min (caretCol, ls [caretLine].Length)); }, (caretLine, 0));
	}

	public void DeleteToLineEnd ()
	{
		MutateLines (ls => { ls [caretLine] = ls [caretLine].Substring (0, Math.Min (caretCol, ls [caretLine].Length)); }, (caretLine, caretCol));
	}

	public void DuplicateLine ()
	{
		int at = caretLine;
		MutateLines (ls => ls.Insert (at + 1, ls [at]), (at + 1, caretCol));
	}

	public void MoveBlockUp ()
	{
		if (caretLine == 0)
			return;
		int at = caretLine;
		MutateLines (ls => (ls [at], ls [at - 1]) = (ls [at - 1], ls [at]), (at - 1, caretCol));
	}

	public void MoveBlockDown ()
	{
		if (caretLine >= lines.Count - 1)
			return;
		int at = caretLine;
		MutateLines (ls => (ls [at], ls [at + 1]) = (ls [at + 1], ls [at]), (at + 1, caretCol));
	}

	public void ToggleLineComment ()
	{
		var (sl, sc, el, ec) = hasSelection ? SelectionRange () : (caretLine, 0, caretLine, 0);
		bool allCommented = true;
		for (int i = sl; i <= el && allCommented; i++) {
			var t = lines [i].TrimStart ();
			if (!t.StartsWith ("//") && t.Length > 0)
				allCommented = false;
		}
		for (int i = sl; i <= el; i++) {
			if (lines [i].Trim ().Length == 0)
				continue;
			if (allCommented) {
				int idx = lines [i].IndexOf ("//", StringComparison.Ordinal);
				if (idx >= 0)
					// Legacy removes the inserted "// " token including its space.
					lines [i] = lines [i].Remove (idx, idx + 2 < lines [i].Length && lines [i] [idx + 2] == ' ' ? 3 : 2);
			}
			else {
				int first = lines [i].Length - lines [i].TrimStart ().Length;
				lines [i] = lines [i].Insert (first, "// ");
			}
		}
		SetValue (TextProperty, string.Join ("\n", lines));
		IsDirty = true;
		MarkDirty ();
	}	public void JoinWithNextLine ()
	{
		if (caretLine >= lines.Count - 1)
			return;
		int at = caretLine;
		MutateLines (ls => {
			ls [at] = ls [at].TrimEnd () + " " + ls [at + 1].TrimStart ();
			ls.RemoveAt (at + 1);
		}, (at, caretCol));
	}

	public void SortSelectedLines ()
	{
		var (sl, sc, el, ec) = hasSelection ? SelectionRange () : (caretLine, 0, caretLine, lines [caretLine].Length);
		var sorted = new List<string> ();
		for (int i = sl; i <= el; i++)
			sorted.Add (lines [i]);
		sorted.Sort (StringComparer.Ordinal);
		for (int i = sl; i <= el; i++)
			lines [i] = sorted [i - sl];
		SetValue (TextProperty, string.Join ("\n", lines));
		IsDirty = true;
		MarkDirty ();
	}

	public void TransformSelection (Func<string, string> transform)
	{
		if (!hasSelection)
			return;
		ReplaceSelection (transform (SelectedText));
	}

	public void UppercaseSelection () => TransformSelection (s => s.ToUpperInvariant ());

	public void LowercaseSelection () => TransformSelection (s => s.ToLowerInvariant ());

	public void DeleteForward ()
	{
		if (hasSelection) {
			ReplaceSelection ("");
			return;
		}
		var line = lines [caretLine];
		if (caretCol < line.Length)
			lines [caretLine] = line.Remove (caretCol, 1);
		else if (caretLine < lines.Count - 1) {
			lines [caretLine] += lines [caretLine + 1];
			lines.RemoveAt (caretLine + 1);
		}
		Commit ();
	}

	public void IndentSelection (int steps = 1)
	{
		var (sl, _, el, _) = hasSelection ? SelectionRange () : (caretLine, 0, caretLine, 0);
		for (int i = sl; i <= el; i++) {
			if (steps > 0)
				lines [i] = new string (' ', 4 * steps) + lines [i];
			else if (lines [i].StartsWith (new string (' ', 4 * -steps), StringComparison.Ordinal))
				lines [i] = lines [i].Substring (4 * -steps);
		}
		SetValue (TextProperty, string.Join ("\n", lines));
		IsDirty = true;
		MarkDirty ();
	}

	public void RemoveTrailingWhitespace ()
	{
		for (int i = 0; i < lines.Count; i++)
			lines [i] = lines [i].TrimEnd ();
		SetValue (TextProperty, string.Join ("\n", lines));
		IsDirty = true;
		MarkDirty ();
	}

	public void InsertAtCaret (string text) => InsertText (text);

	// ----- Zoom (legacy TextEditor.Options.ZoomIn/Out/Reset) -----
	public const double BaseFontSize = 12.0;

	public void ZoomIn ()
	{
		FontSize = Math.Min (FontSize * 1.1, 60);
		InvalidateVisual ();
	}

	public void ZoomOut ()
	{
		FontSize = Math.Max (FontSize / 1.1, 5);
		InvalidateVisual ();
	}

	public void ZoomReset ()
	{
		FontSize = BaseFontSize;
		InvalidateVisual ();
	}

	// ----- Bookmarks (legacy IBookmarkBuffer: SetBookmarked/NextBookmark/PrevBookmark) -----
	readonly HashSet<int> bookmarkLines = new ();

	public bool HasSelectionText => hasSelection && SelectedText.Length > 0;

	public void ToggleBookmark ()
	{
		if (!bookmarkLines.Add (caretLine))
			bookmarkLines.Remove (caretLine);
		MarkDirty ();
	}

	public void ClearBookmarks ()
	{
		bookmarkLines.Clear ();
		MarkDirty ();
	}

	public void NextBookmark ()
	{
		if (bookmarkLines.Count == 0)
			return;
		var next = bookmarkLines.Where (l => l > caretLine).OrderBy (l => l).FirstOrDefault (-1);
		if (next < 0)
			next = bookmarkLines.Min ();
		GotoLine (next);
	}

	public void PrevBookmark ()
	{
		if (bookmarkLines.Count == 0)
			return;
		var prev = bookmarkLines.Where (l => l < caretLine).OrderByDescending (l => l).FirstOrDefault (-1);
		if (prev < 0)
			prev = bookmarkLines.Max ();
		GotoLine (prev);
	}

	// ----- Multi-caret (legacy TextEditorCommands.InsertNextMatchingCaret family:
	// one primary caret plus secondary carets on the next/all word matches). -----

	readonly List<(int line, int col)> secondaryCarets = new ();

	/// <summary>All active carets, primary first.</summary>
	public IReadOnlyList<(int line, int col)> Carets
		=> new [] { (caretLine, caretCol) }.Concat (secondaryCarets).ToList ();

	public bool HasSecondaryCarets => secondaryCarets.Count > 0;

	/// <summary>Adds a caret at the next occurrence of the word at the caret
	/// (legacy InsertNextMatchingCaret). False when there is nothing to match.</summary>
	public bool InsertNextMatchingCaret ()
	{
		var word = WordAtCaret ();
		if (string.IsNullOrEmpty (word))
			return false;
		// Search forward from the primary caret, wrapping the document once.
		var text = string.Join ("\n", lines);
		int pos = lines.Take (caretLine).Sum (l => l.Length + 1) + Math.Min (caretCol, lines [caretLine].Length);
		int idx = text.IndexOf (word, Math.Min (pos + word.Length, text.Length), StringComparison.Ordinal);
		if (idx < 0)
			idx = text.IndexOf (word, StringComparison.Ordinal);
		if (idx < 0 || (secondaryCarets.Count == 0 && idx == pos))
			return false;
		var (l, c) = OffsetToPosition (idx);
		if (Carets.Any (ct => ct.line == l && ct.col == c))
			return false;
		secondaryCarets.Add ((l, c));
		MarkDirty ();
		return true;
	}

	/// <summary>Carets at every occurrence of the word at the caret
	/// (legacy InsertAllMatchingCarets).</summary>
	public int InsertAllMatchingCarets ()
	{
		var word = WordAtCaret ();
		if (string.IsNullOrEmpty (word))
			return 0;
		secondaryCarets.Clear ();
		int count = 0;
		for (int i = 0; i < lines.Count; i++) {
			int from = 0;
			int idx;
			while ((idx = lines [i].IndexOf (word, from, StringComparison.Ordinal)) >= 0) {
				if (i == caretLine && idx == Math.Min (caretCol, lines [i].Length)) {
					from = idx + word.Length;
					continue; // the primary caret
				}
				secondaryCarets.Add ((i, idx));
				count++;
				from = idx + Math.Max (1, word.Length);
			}
		}
		if (count > 0)
			MarkDirty ();
		return count;
	}

	public void RemoveLastSecondaryCaret ()
	{
		if (secondaryCarets.Count > 0) {
			secondaryCarets.RemoveAt (secondaryCarets.Count - 1);
			MarkDirty ();
		}
	}

	public void RotatePrimaryCaretNext ()
	{
		if (secondaryCarets.Count == 0)
			return;
		var next = secondaryCarets [0];
		secondaryCarets.RemoveAt (0);
		secondaryCarets.Add ((caretLine, caretCol));
		caretLine = next.line;
		caretCol = next.col;
		EnsureCaretVisible ();
		MarkDirty ();
	}

	public void RotatePrimaryCaretPrevious ()
	{
		if (secondaryCarets.Count == 0)
			return;
		var last = secondaryCarets [^1];
		secondaryCarets.RemoveAt (secondaryCarets.Count - 1);
		secondaryCarets.Insert (0, (caretLine, caretCol));
		caretLine = last.line;
		caretCol = last.col;
		EnsureCaretVisible ();
		MarkDirty ();
	}

	public void MoveLastCaretDown ()
	{
		if (secondaryCarets.Count > 0) {
			var (l, c) = secondaryCarets [^1];
			if (l + 1 < lines.Count)
				secondaryCarets [^1] = (l + 1, Math.Min (c, lines [l + 1].Length));
		} else if (caretLine + 1 < lines.Count) {
			caretLine++;
			caretCol = Math.Min (caretCol, lines [caretLine].Length);
		}
		EnsureCaretVisible ();
		MarkDirty ();
	}

	/// <summary>Inserts text at every caret (primary first, bottom-up so earlier
	/// offsets stay valid), like the legacy MultiCaretInsertText.</summary>
	public void InsertAtAllCarets (string text)
	{
		if (secondaryCarets.Count == 0) {
			InsertAtCaret (text);
			return;
		}
		var all = Carets.OrderByDescending (c => c.line).ThenByDescending (c => c.col).ToList ();
		foreach (var (l, c) in all) {
			var line = lines [l];
			int col = Math.Min (c, line.Length);
			lines [l] = line.Insert (col, text);
			if (l == caretLine)
				caretCol += text.Length;
			for (int i = 0; i < secondaryCarets.Count; i++) {
				var (sl, sc) = secondaryCarets [i];
				if (sl == l && sc > col)
					secondaryCarets [i] = (sl, sc + text.Length);
			}
		}
		Commit ();
	}

	public void ClearSecondaryCarets ()
	{
		secondaryCarets.Clear ();
		MarkDirty ();
	}

	// Returns the word under the caret (used by RefactorCommands.Rename).
	public string WordAtCaret ()
	{
		var line = lines [caretLine];
		int start = Math.Min (caretCol, line.Length);
		while (start > 0 && IsWordChar (line [start - 1]))
			start--;
		int end = start;
		while (end < line.Length && IsWordChar (line [end]))
			end++;
		return start < end ? line [start..end] : "";
	}

	static bool IsWordChar (char c) => char.IsLetterOrDigit (c) || c == '_';

	// Replace every occurrence in the document (file-scoped rename). Commit () pushes
	// the undo snapshot exactly like a user edit.
	public int ReplaceAllInDocument (string find, string replace)
	{
		if (string.IsNullOrEmpty (find))
			return 0;
		int n = 0;
		for (int i = 0; i < lines.Count; i++) {
			if (lines [i].Contains (find)) {
				lines [i] = lines [i].Replace (find, replace);
				n++;
			}
		}
		if (n > 0)
			Commit ();
		return n;
	}

	// Legacy TextEditorCommands.GotoMatchingBrace: jump to the brace matching the one
	// before the caret ({}, (), []). Returns false when there is no match.
	public bool GotoMatchingBrace ()
	{
		const string Open = "{([";
		const string Close = "})]";
		// Search backwards on the current line for the nearest brace before the caret.
		var line = lines [caretLine];
		int pos = Math.Min (caretCol, line.Length) - 1;
		while (pos >= 0 && Open.IndexOf (line [pos]) < 0 && Close.IndexOf (line [pos]) < 0)
			pos--;
		if (pos < 0)
			return false;
		char ch = line [pos];
		bool forward = Open.IndexOf (ch) >= 0;
		char mate = forward ? Close [Open.IndexOf (ch)] : Open [Close.IndexOf (ch)];
		int depth = 1;
		int cl = caretLine, cc = pos;
		while (forward ? (cl < lines.Count) : (cl >= 0)) {
			var l = lines [cl];
			int i = forward ? (cl == caretLine ? pos + 1 : 0) : (cl == caretLine ? pos - 1 : l.Length - 1);
			while (forward ? (i < l.Length) : (i >= 0)) {
				char c = l [i];
				if (c == ch) depth++;
				else if (c == mate) {
					depth--;
					if (depth == 0) {
						caretLine = cl;
						caretCol = Math.Clamp (i, 0, l.Length);
						hasSelection = false;
						EnsureCaretVisible ();
						MarkDirty ();
						return true;
					}
				}
				i += forward ? 1 : -1;
			}
			cl += forward ? 1 : -1;
			if (cl < 0 || cl >= lines.Count)
				break;
		}
		return false;
	}

	// ----- Undo/Redo (full-text snapshots, the MVP of the legacy undo stack) -----
	readonly List<(string Text, int Line, int Col)> undoStack = new ();
	readonly List<(string Text, int Line, int Col)> redoStack = new ();
	bool undoing;

	public bool CanUndo => undoStack.Count > 0;

	public void Undo ()
	{
		if (undoStack.Count == 0)
			return;
		var (text, line, col) = undoStack [^1];
		undoStack.RemoveAt (undoStack.Count - 1);
		undoing = true;
		redoStack.Add ((Text ?? "", caretLine, caretCol));
		Text = text; // OnPropertyChanged → SetLines
		undoing = false;
		caretLine = Math.Clamp (line, 0, lines.Count - 1);
		caretCol = Math.Clamp (col, 0, lines [caretLine].Length);
		hasSelection = false;
		MarkDirty ();
	}

	public void Redo ()
	{
		if (redoStack.Count == 0)
			return;
		var (text, line, col) = redoStack [^1];
		redoStack.RemoveAt (redoStack.Count - 1);
		undoStack.Add ((Text ?? "", caretLine, caretCol));
		Text = text;
		caretLine = Math.Clamp (line, 0, lines.Count - 1);
		caretCol = Math.Clamp (col, 0, lines [caretLine].Length);
		hasSelection = false;
		MarkDirty ();
	}

	#endregion

	#region Rendering

	float GutterWidth () => (float)FontSize * 0.6f * (lines.Count.ToString ().Length + 1) + 10;

	public override void Render (DrawingContext context)
	{
		int w = Math.Max (1, (int)Math.Ceiling (Bounds.Width));
		int h = Math.Max (1, (int)Math.Ceiling (Bounds.Height));
		if (dirty || buffer is null || bufferW != w || bufferH != h) {
			if (buffer is null || bufferW != w || bufferH != h) {
				buffer?.Dispose ();
				buffer = new WriteableBitmap (new PixelSize (w, h), new Vector (96, 96), PixelFormats.Bgra8888, AlphaFormat.Opaque);
				bufferW = w;
				bufferH = h;
			}
			using var fb = buffer.Lock ();
			var info = new SKImageInfo (w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
			using (var surface = SKSurface.Create (info, fb.Address, fb.RowBytes)) {
				if (surface is null)
					Console.WriteLine ("[skeditor] SKSurface.Create returned null");
				else
					Draw (surface.Canvas);
			}
			dirty = false;
		}
		if (buffer is not null)
			context.DrawImage (buffer, new Rect (0, 0, Bounds.Width, Bounds.Height));
	}

	void Draw (SKCanvas canvas)
	{
		using var font = CreateFont ();
		float charW = CharWidth (font);
		float lineH = LineHeight;
		float gutterW = GutterWidth ();

		var bg = ToSk (Background as ISolidColorBrush) ?? new SKColor (0x1E, 0x1E, 0x1E);
		var fg = ToSk (Foreground as ISolidColorBrush) ?? new SKColor (0xD4, 0xD4, 0xD4);
		var gutterBg = ToSk (GutterBackground as ISolidColorBrush) ?? new SKColor (0x2D, 0x2D, 0x2D);
		var gutterFg = ToSk (GutterForeground as ISolidColorBrush) ?? new SKColor (0x86, 0x86, 0x86);
		var caretColor = ToSk (CaretBrush as ISolidColorBrush) ?? fg;

		var (comment, strColor, keywordColor, numberColor) = GetThemePalette (ActualThemeVariant);
		var keywordSet = new HashSet<string> (Keywords.Split (' '));

		using var bgPaint = new SKPaint { Color = bg, IsAntialias = false };
		canvas.DrawRect (0, 0, (float)Bounds.Width, (float)Bounds.Height, bgPaint);
		using var gutterPaint = new SKPaint { Color = gutterBg, IsAntialias = false };
		canvas.DrawRect (0, 0, gutterW, (float)Bounds.Height, gutterPaint);
		using var gutterLinePaint = new SKPaint { Color = gutterFg.WithAlpha (60), IsAntialias = false };
		canvas.DrawRect (gutterW - 1, 0, 1, (float)Bounds.Height, gutterLinePaint);

		int firstLine = (int)Math.Max (0, scrollLines);
		int visible = (int)Math.Ceiling ((float)Bounds.Height / lineH) + 1;

		using var textPaint = new SKPaint { IsAntialias = true };
		using var textFont = new SKFont (font.Typeface, (float)FontSize);

		for (int i = firstLine; i < Math.Min (lines.Count, firstLine + visible); i++) {
			float y = (i - (float)scrollLines) * lineH;
			float baseline = y + (lineH - (float)FontSize) / 2f + (float)FontSize * 0.85f;

			// gutter: line number
			textPaint.Color = gutterFg;
			canvas.DrawText ((i + 1).ToString (), 6, baseline, textFont, textPaint);
			// bookmark marker in the gutter (legacy gutter-bookmark-15: blue square)
			if (bookmarkLines.Contains (i)) {
				using var bmPaint = new SKPaint { Color = new SKColor (0x2f, 0x78, 0xc8), IsAntialias = true };
				canvas.DrawRect (gutterW - 14, y + lineH / 2 - 5, 10, 10, bmPaint);
				using var bmLine = new SKPaint { Color = new SKColor (0x2f, 0x78, 0xc8).WithAlpha (70), IsAntialias = false };
				canvas.DrawRect (0, y, (float)Bounds.Width, lineH, bmLine);
			}
			// caret line highlight in gutter
			if (i == caretLine) {
				using var activePaint = new SKPaint { Color = gutterFg.WithAlpha (40) };
				canvas.DrawRect (0, y, gutterW, lineH, activePaint);
			}

			// segments with basic highlighting
			var segments = Tokenize (lines [i], fg, comment, strColor, keywordColor, numberColor, keywordSet);
			float x = gutterW + 4;
			foreach (var seg in segments) {
				textPaint.Color = seg.Color;
				canvas.DrawText (seg.Text, x, baseline, textFont, textPaint);
				x += seg.Text.Length * charW;
			}
		}

		// carets — secondary carets render shorter (legacy InsertionCursor), primary last
		if (caretVisible) {
			using var caretPaint = new SKPaint { Color = caretColor, IsAntialias = false };
			using var secPaint = new SKPaint { Color = caretColor.WithAlpha (140), IsAntialias = false };
			foreach (var (sl, sc) in secondaryCarets) {
				float cx = gutterW + 4 + sc * charW;
				float cy = (sl - (float)scrollLines) * lineH;
				if (cy >= -lineH && cy <= (float)Bounds.Height)
					canvas.DrawRect (cx, Math.Max (0f, cy), 1.2f, Math.Min (lineH * 0.6f, (float)Bounds.Height - Math.Max (0f, cy)), secPaint);
			}
			float caretX = gutterW + 4 + caretCol * charW;
			float caretY = (caretLine - (float)scrollLines) * lineH;
			if (caretY >= -lineH && caretY <= (float)Bounds.Height)
				canvas.DrawRect (caretX, Math.Max (0f, caretY), 1.5f, Math.Min (lineH, (float)Bounds.Height - Math.Max (0f, caretY)), caretPaint);
		}
	}

	static SKColor? ToSk (ISolidColorBrush? brush)
		=> brush is null ? null : new SKColor (brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);

	(SKColor, SKColor, SKColor, SKColor) GetThemePalette (Avalonia.Styling.ThemeVariant variant)
		=> variant == Avalonia.Styling.ThemeVariant.Light
			? (new SKColor (0x6A, 0x99, 0x55), new SKColor (0xA3, 0x15, 0x15), new SKColor (0x00, 0x00, 0xFF), new SKColor (0x09, 0x86, 0x58))
			: (new SKColor (0x6A, 0x99, 0x55), new SKColor (0xCE, 0x91, 0x78), new SKColor (0x56, 0x9C, 0xD6), new SKColor (0xB5, 0xCE, 0xA8));

	List<Segment> Tokenize (string line, SKColor defaultColor, SKColor comment, SKColor strColor, SKColor keywordColor, SKColor numberColor, HashSet<string> keywords)
	{
		var segments = new List<Segment> ();
		var current = new System.Text.StringBuilder ();
		SKColor currentColor = defaultColor;
		void Flush ()
		{
			if (current.Length > 0) {
				segments.Add (new Segment { Text = current.ToString (), Color = currentColor });
				current.Clear ();
			}
		}
		for (int i = 0; i < line.Length; i++) {
			char c = line [i];
			if (c == '/' && i + 1 < line.Length && line [i + 1] == '/') {
				Flush ();
				segments.Add (new Segment { Text = line.Substring (i), Color = comment });
				break;
			}
			if (c == '"') {
				Flush ();
				int j = i + 1;
				while (j < line.Length && line [j] != '"') {
					if (line [j] == '\\' && j + 1 < line.Length)
						j++;
					j++;
				}
				segments.Add (new Segment { Text = line.Substring (i, Math.Min (j + 1, line.Length) - i), Color = strColor });
				i = Math.Min (j, line.Length - 1);
				continue;
			}
			if (char.IsLetter (c) || c == '_' || (c == '@' && i + 1 < line.Length && char.IsLetter (line [i + 1]))) {
				int j = i;
				while (j < line.Length && (char.IsLetterOrDigit (line [j]) || line [j] == '_'))
					j++;
				var word = line.Substring (i, j - i);
				Flush ();
				segments.Add (new Segment { Text = word, Color = keywords.Contains (word) ? keywordColor : defaultColor });
				i = j - 1;
				continue;
			}
			if (char.IsDigit (c)) {
				Flush ();
				int j = i;
				while (j < line.Length && (char.IsDigit (line [j]) || line [j] == '.' || char.ToLower (line [j]) is 'f' or 'd' or 'm' or 'x' or 'e' or 'b' or 'o'))
					j++;
				segments.Add (new Segment { Text = line.Substring (i, j - i), Color = numberColor });
				i = j - 1;
				continue;
			}
			current.Append (c);
		}
		Flush ();
		return segments;
	}

	#endregion
}
