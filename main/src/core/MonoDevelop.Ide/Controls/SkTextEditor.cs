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

namespace MonoDevelop.Ide.Controls;

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
	WriteableBitmap? front; // freshly-rendered frame presented to the compositor
	WriteableBitmap? pendingDispose; // previous frame, released one frame later
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

	/// <summary>Moves the caret right N columns within the current line (public
	/// equivalent of the Right arrow, used by automated tests and macros).</summary>
	public void CaretRight (int count = 1)
	{
		caretCol = Math.Clamp (caretCol + count, 0, lines [caretLine].Length);
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
		// Seed the line model: the TextProperty default ("") never fires
		// OnPropertyChanged, so the model starts empty without this.
		SetLines (Text ?? "");
		blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds (530) };
		blinkTimer.Tick += (_, _) => {
			if (IsFocused) {
				caretVisible = !caretVisible;
				MarkDirty ();
			}
		};
		blinkTimer.Start ();
	}

	// ----- Legacy hover tooltip pipeline (TooltipProvider.GetItem + ShowTooltipWindow)
	// A borderless popup with the signature/description of the word under the mouse.
	EditorTooltipPopup? hoverPopup;
	Point hoverPoint;
	readonly DispatcherTimer hoverTimer = new () { Interval = TimeSpan.FromMilliseconds (500) }; // legacy HOVER_TIME
	bool hoverDismissed; // pointer moved within the same hover: don't re-show instantly

	/// <summary>Owner window for the popups (set by MainWindow).</summary>
	public Window? PopupOwner { get; set; }

	void OnHoverTimer (object? sender, EventArgs e)
	{
		hoverTimer.Stop ();
		if (hoverDismissed || PopupOwner is null || !IsPointerOver)
			return;
		var info = GetHoverInfo (hoverPoint);
		if (info is null)
			return;
		// Debugger hover eval (legacy TooltipProvider + debugger tooltip): while a
		// debug session is paused, the tooltip shows the live value of the word.
		if (DebugHoverEval is { } eval) {
			var (word, _, _) = WordAtPoint (hoverPoint);
			var value = eval (word);
			if (value is not null)
				info = (info.Value.Header, $"{word} = {value}", "md-debug-all");
		}
		hoverPopup ??= new EditorTooltipPopup ();
		// Screen coordinates: the popup is a top-level window, so the editor-relative
		// point must be translated through PointToScreen (showing it at parent.Position
		// + point landed on the wrong pad when the editor is offset).
		var screenPt = this.PointToScreen (hoverPoint);
		hoverPopup.ShowAtScreen (PopupOwner, screenPt, info.Value.Header, info.Value.Description, info.Value.Icon);
	}

	/// <summary>Debugger hover-eval hook: when paused, MainWindow plugs a callback
	/// that returns the live value of a word (DAP evaluate) or null to fall back
	/// to the Roslyn description (legacy debugger tooltip behavior).</summary>
	public Func<string, string?>? DebugHoverEval { get; set; }

	/// <summary>QA pipeline: shows the hover tooltip for a word using the debug
	/// eval hook (same popup as the pointer pipeline).</summary>
	public void ShowDebugTooltipForQa (string word, string value)
	{
		if (PopupOwner is null)
			return;
		hoverPopup ??= new EditorTooltipPopup ();
		for (int i = 0; i < lines.Count; i++) {
			int col = lines [i].IndexOf (word, StringComparison.Ordinal);
			if (col < 0)
				continue;
			var pt = new Point (
				GutterWidth () + 4 + (col + word.Length / 2.0) * FontSize * 0.6,
				(i - scrollLines + 0.5) * LineHeight);
			hoverPopup.ShowAtScreen (PopupOwner, this.PointToScreen (pt), word, $"{word} = {value}", "md-debug-all");
			return;
		}
	}

	/// <summary>Word under an editor-relative point (legacy GetItem: offset → word).</summary>
	public (string Word, int Line, int Col) WordAtPoint (Point p)
	{
		double charW = FontSize * 0.6;
		int col = Math.Max (0, (int)((p.X - GutterWidth ()) / charW));
		int line = Math.Clamp ((int)(p.Y / LineHeight + scrollLines), 0, lines.Count - 1);
		var text = lines [line];
		if (col >= text.Length)
			return ("", line, col);
		int s = col, e2 = col;
		while (s > 0 && (char.IsLetterOrDigit (text [s - 1]) || text [s - 1] == '_'))
			s--;
		while (e2 < text.Length && (char.IsLetterOrDigit (text [e2]) || text [e2] == '_'))
			e2++;
		return (text.Substring (s, e2 - s), line, s);
	}

	void OnPointerLeaveForHover ()
	{
		hoverTimer.Stop ();
		hoverPopup?.HideTooltip ();
		hoverDismissed = false;
	}

	protected override void OnPointerEntered (PointerEventArgs e)
	{
		base.OnPointerEntered (e);
		if (!dragging)
			hoverDismissed = false;
	}

	protected override void OnPointerExited (PointerEventArgs e)
	{
		base.OnPointerExited (e);
		OnPointerLeaveForHover ();
		ClearGutterHover ();
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
		var next = text.Replace ("\r\n", "\n").Split ('\n').ToList ();
		if (next.Count == 0)
			next.Add ("");
		// Ignore echo of our own Commit (SetValue with the joined buffer): the split
		// text equals the current model, so only external changes (file open, undo,
		// SaveFile refresh) rebuild it. Value comparison is safer than a flag, which
		// sticks when SetValue is a no-op (same value) and swallows the next load.
		if (next.Count == lines.Count && next.SequenceEqual (lines))
			return;
		lines.Clear ();
		lines.AddRange (next);
		caretLine = Math.Clamp (caretLine, 0, lines.Count - 1);
		caretCol = Math.Clamp (caretCol, 0, lines [caretLine].Length);
		RebuildFolds ();
		MarkDirty ();
	}

	/// <summary>Signature/description for the word under an editor-relative point
	/// (the legacy TooltipItem): scans the document for declarations of that word.</summary>
	public (string Header, string Description, string Icon)? GetHoverInfo (Point p)
	{
		var (word, line, col) = WordAtPoint (p);
		if (word.Length == 0)
			return null;
		return DescribeWord (word, line);
	}

	(string, string, string) DescribeWord (string word, int fromLine)
	{
		// Find a declaration-like occurrence: "Type name", "Type name =", "name("
		string? decl = null;
		int declLine = -1;
		for (int i = 0; i < lines.Count && decl is null; i++) {
			var l = lines [i];
			int idx = 0;
			while ((idx = l.IndexOf (word, idx, StringComparison.Ordinal)) >= 0) {
				bool wordBoundaryLeft = idx == 0 || !(char.IsLetterOrDigit (l [idx - 1]) || l [idx - 1] == '_');
				int after = idx + word.Length;
				bool wordBoundaryRight = after >= l.Length || !(char.IsLetterOrDigit (l [after]) || l [after] == '_');
				if (wordBoundaryLeft && wordBoundaryRight) {
					var trimmed = l.TrimStart ();
					bool isDecl =
						(after < l.Length && (l [after] == '(' || l [after] == '=' || l [after] == ';')) ||
						trimmed.StartsWith ("public ") || trimmed.StartsWith ("private ") ||
						trimmed.StartsWith ("protected ") || trimmed.StartsWith ("internal ") ||
						trimmed.StartsWith ("static ") || trimmed.StartsWith ("class ") ||
						trimmed.StartsWith ("void ") || trimmed.StartsWith ("var ") ||
						trimmed.StartsWith ("int ") || trimmed.StartsWith ("string ");
					if (isDecl) {
						decl = l.Trim ();
						declLine = i + 1;
						break;
					}
				}
				idx = after;
			}
		}
		// Classify for the legacy element icon.
		string icon = "element-other-declaration-16";
		string kind = "word";
		var kw = new HashSet<string> (Keywords.Split (' '));
		if (kw.Contains (word)) {
			icon = "element-keyword-16";
			kind = "keyword";
		} else if (decl is not null) {
			var d = decl;
			if (d.Contains ("class ") || d.Contains ("struct ") || d.Contains ("interface ") || d.Contains ("enum ")) {
				icon = "element-class-16";
				kind = "type";
			} else if (System.Text.RegularExpressions.Regex.IsMatch (d, @"\w+\s*\(")) {
				icon = "element-method-16";
				kind = "method";
			} else if (d.Contains ("=" ) && !d.Contains ("(")) {
				icon = "element-field-16";
				kind = "field";
			} else if (d.Contains ("{ get") || d.Contains ("{ get;")) {
				icon = "element-property-16";
				kind = "property";
			}
		}
		string header = word + (kind != "word" ? "  (" + kind + ")" : "");
		string desc = decl is not null
			? decl + (declLine > 0 ? "\nline " + declLine : "")
			: "No declaration found in this document.";
		return (header, desc, icon);
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

	// ----- Completion popup wiring (legacy CompletionWindowManager: the window
	// opens on '.' or Ctrl+Space, filters while typing, commits on Enter/Tab/click
	// and cancels on Escape/focus loss) -----
	CompletionPopup? completionPopup;
	bool completionPopupCommitWired;
	string completionPrefix = "";

	/// <summary>Completion entries: document words + C# keywords with their legacy
	/// element icons (the legacy data source aggregates document words when no
	/// language model is available).</summary>
	public IEnumerable<CompletionPopup.Item> GetCompletionItems ()
	{
		var kw = new HashSet<string> (Keywords.Split (' '));
		foreach (var w in DocumentWords ())
			yield return new CompletionPopup.Item (w, "element-field-16", "document word", "");
		foreach (var k in kw)
			yield return new CompletionPopup.Item (k, "element-keyword-16", "C# keyword", "keyword");
	}

	void ShowCompletionAtCaret ()
	{
		if (PopupOwner is null)
			return;
		completionPrefix = WordPrefixBeforeCaret ();
		completionPopup ??= new CompletionPopup ();
		if (!completionPopupCommitWired) {
			completionPopup.Committing += CommitCompletion;
			completionPopupCommitWired = true;
		}
		completionPopup.ShowItems (GetCompletionItems (), completionPrefix);
		if (completionPopup.ItemCount == 0)
			return;
		// Position under the caret (legacy CodeCompletionContext).
		double charW = FontSize * 0.6;
		var topLeft = this.PointToScreen (new Point (
			GutterWidth () + 4 + (caretCol - completionPrefix.Length) * charW,
			(caretLine - scrollLines + 1) * LineHeight));
		completionPopup.Position = topLeft;
		completionPopup.Show (PopupOwner);
	}

	void UpdateCompletionFilter ()
	{
		if (completionPopup is { IsVisible: true }) {
			completionPrefix = WordPrefixBeforeCaret ();
			completionPopup.ShowItems (GetCompletionItems (), completionPrefix);
			if (completionPopup.ItemCount == 0)
				completionPopup.Hide ();
		}
	}

	void CommitCompletion (object? sender, EventArgs e)
	{
		var popup = sender as CompletionPopup;
		var word = popup?.SelectedText;
		if (string.IsNullOrEmpty (word))
			return;
		// Replace the typed prefix with the selected entry (legacy completion commit).
		var line = lines [caretLine];
		int end = Math.Min (caretCol, line.Length);
		int start = end - completionPrefix.Length;
		if (start < 0)
			start = end;
		lines [caretLine] = line.Substring (0, start) + word + line.Substring (end);
		caretCol = start + word.Length;
		Commit ();
	}

	bool HandleCompletionKey (KeyEventArgs e)
	{
		if (completionPopup is not { IsVisible: true })
			return false;
		switch (e.Key) {
		case Key.Down:
			completionPopup.MoveSelection (1);
			e.Handled = true;
			return true;
		case Key.Up:
			completionPopup.MoveSelection (-1);
			e.Handled = true;
			return true;
		case Key.PageDown:
			completionPopup.PageMove (1);
			e.Handled = true;
			return true;
		case Key.PageUp:
			completionPopup.PageMove (-1);
			e.Handled = true;
			return true;
		case Key.Enter:
		case Key.Tab:
			completionPopup.RequestCommit ();
			e.Handled = true;
			return true;
		case Key.Escape:
			completionPopup.RequestCancel ();
			e.Handled = true;
			return true;
		}
		return false;
	}

	protected override void OnTextInput (TextInputEventArgs e)
	{
		base.OnTextInput (e);
		var text = e.Text;
		if (string.IsNullOrEmpty (text))
			return;
		InsertText (text);
		// Legacy trigger: '.' opens the completion window; typing filters it.
		if (text == ".")
			ShowCompletionAtCaret ();
		else
			UpdateCompletionFilter ();
		e.Handled = true;
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		base.OnKeyDown (e);
		// Completion popup swallows navigation keys first (legacy CompletionController).
		if (HandleCompletionKey (e))
			return;
		var ctrl = e.KeyModifiers.HasFlag (KeyModifiers.Control);
		var altShift = e.KeyModifiers.HasFlag (KeyModifiers.Alt) && e.KeyModifiers.HasFlag (KeyModifiers.Shift);
		// Legacy ShowCompletionWindow: Ctrl+Space opens the completion list.
		if (ctrl && e.Key == Key.Space) {
			ShowCompletionAtCaret ();
			e.Handled = true;
			return;
		}
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
			DeleteBackwardAtCarets ();
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
			// Legacy left margin: a click on the gutter marker strip toggles the
			// breakpoint of that line (Mono.TextEditor ActionTextArea
			// "left margin click"). The strip is the icon column next to the line
			// numbers; the rest of the gutter keeps its click-to-move-caret role.
			if (pt.Position.X <= GutterIconStripWidth ()) {
				int gutterLine = Math.Clamp ((int)(pt.Position.Y / LineHeight + scrollLines), 0, lines.Count - 1);
				caretLine = gutterLine;
				ToggleBreakpoint ();
				e.Handled = true;
				return;
			}
			// Legacy: a plain mouse click collapses multi-caret back to the primary
			// caret (only Alt+Shift+Click adds one via InsertNextMatchingCaret).
			if (!e.KeyModifiers.HasFlag (KeyModifiers.Alt))
				secondaryCarets.Clear ();
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
		var pt = e.GetPosition (this);
		// Gutter hover polish: highlight the row under the cursor, hand cursor
		// over the breakpoint strip, tooltip with the line number.
		UpdateGutterHover (pt);
		// Hover tooltip: restart the timer on every move (legacy tooltip pipeline).
		if (!dragging) {
			// Over the gutter the word-hover pipeline stays off (the legacy margin
			// shows its own tooltip, not the language one).
			if (pointerInGutter) {
				hoverTimer.Stop ();
				hoverPopup?.HideTooltip ();
				return;
			}
			var word = WordAtPoint (pt).Word;
			bool sameSpot = hoverPopup?.IsVisible == true &&
				Math.Abs (pt.X - hoverPoint.X) < FontSize * 0.6 && Math.Abs (pt.Y - hoverPoint.Y) < LineHeight;
			if (!sameSpot) {
				hoverPopup?.HideTooltip ();
				hoverDismissed = hoverPopup?.IsVisible == true;
				hoverPoint = pt;
				if (word.Length > 0) {
					hoverTimer.Stop ();
					hoverTimer.Tick -= OnHoverTimer;
					hoverTimer.Tick += OnHoverTimer;
					hoverTimer.Start ();
				} else {
					hoverDismissed = false;
				}
			}
		}
		if (!dragging)
			return;
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
		// Legacy multi-caret editing: the same keystroke applies to every caret
		// (primary last so it keeps its position semantics). With a single caret
		// this is the plain path.
		var all = Carets
			.OrderByDescending (c => c.line)
			.ThenByDescending (c => c.col)
			.ToList (); // bottom-up so line inserts don't shift pending carets
		foreach (var (l, c) in all) {
			int line = l, col = c;
			foreach (var ch in text) {
				if (ch == '\n') {
					var tail = lines [line].Substring (col);
					lines [line] = lines [line].Substring (0, col);
					lines.Insert (line + 1, tail);
					line++;
					col = 0;
				} else {
					var l2 = lines [line];
					lines [line] = l2.Insert (col, ch.ToString ());
					col++;
				}
			}
			if ((line, col) == (caretLine, caretCol))
				(caretLine, caretCol) = (line, col);
			else {
				int idx = secondaryCarets.FindIndex (s => s.line == l && s.col == c);
				if (idx >= 0)
					secondaryCarets [idx] = (line, col);
				else if (all.Count == 1)
					(caretLine, caretCol) = (line, col);
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
		RebuildFolds ();
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

	/// <summary>Programmatic Backspace (the Key.Back handler path without a key
	/// event) — used by QA to exercise deletion deterministically.</summary>
	public void BackspaceForQa () => DeleteBackwardAtCarets ();

	/// <summary>Backspace at every caret, bottom-up (the Key.Back handler path;
	/// shared with QA and tests so the model has a single source of truth).</summary>
	void DeleteBackwardAtCarets ()
	{
		bool changed = false;
		foreach (var (l, c) in Carets
			.OrderByDescending (x => x.line).ThenByDescending (x => x.col).ToList ()) {
			if (c > 0) {
				lines [l] = lines [l].Remove (c - 1, 1);
				if (l == caretLine)
					caretCol--;
				else
					ShiftSecondary (l, c, -1);
				changed = true;
			} else if (l > 0) {
				if (l == caretLine)
					caretCol = lines [l - 1].Length;
				lines [l - 1] += lines [l];
				lines.RemoveAt (l);
				if (l == caretLine)
					caretLine--;
				else
					ShiftSecondaryAfterLineRemoval (l);
				changed = true;
			}
		}
		if (!changed)
			return;
		caretCol = Math.Clamp (caretCol, 0, lines [caretLine].Length);
		Commit ();
	}

	/// <summary>Collapse to a single caret at the primary position (legacy: a
	/// plain click or Escape ends multi-caret editing).</summary>
	public void CollapseToPrimaryCaret () => secondaryCarets.Clear ();

	// Multi-caret bookkeeping after an edit at (line, col).
	void ShiftSecondary (int line, int col, int delta)
	{
		int idx = secondaryCarets.FindIndex (s => s.line == line && s.col == col);
		if (idx >= 0)
			secondaryCarets [idx] = (line, col + delta);
	}

	void ShiftSecondaryAfterLineRemoval (int removedLine)
	{
		for (int i = 0; i < secondaryCarets.Count; i++) {
			var (l, c) = secondaryCarets [i];
			if (l == removedLine)
				secondaryCarets [i] = (removedLine - 1, c);
			else if (l > removedLine)
				secondaryCarets [i] = (l - 1, c);
		}
	}

	/// <summary>QA pipeline: opens the completion popup exactly as the '.' trigger.
	/// Assumes the caret sits right after a '.'.</summary>
	public void TriggerCompletionForQa ()
	{
		ShowCompletionAtCaret ();
	}

	public bool IsCompletionOpenForQa => completionPopup is { IsVisible: true } && completionPopup.ItemCount > 0;

	/// <summary>QA pipeline: commits the currently selected completion entry.</summary>
	public void CommitCompletionForQa ()
	{
		if (completionPopup is { IsVisible: true })
			completionPopup.RequestCommit ();
	}

	/// <summary>Hover info for a word without pointer coordinates (QA + tests).</summary>
	public (string Header, string Description, string Icon)? GetHoverInfoFor (string word)
	{
		if (string.IsNullOrEmpty (word))
			return null;
		var (header, desc, icon) = DescribeWord (word, caretLine);
		return (header, desc, icon);
	}

	/// <summary>Shows the hover tooltip over the first occurrence of a word (QA
	/// visual path — the same popup the pointer pipeline displays).</summary>
	public void ShowTooltipForQa (string word)
	{
		if (PopupOwner is null)
			return;
		for (int i = 0; i < lines.Count; i++) {
			int col = lines [i].IndexOf (word, StringComparison.Ordinal);
			if (col < 0)
				continue;
			// The word's center point in editor coordinates, then to screen pixels
			// (the popup is a top-level window positioned in screen space).
			var pt = new Point (
				GutterWidth () + 4 + (col + word.Length / 2.0) * FontSize * 0.6,
				(i - scrollLines + 0.5) * LineHeight);
			var (header, desc, icon) = DescribeWord (word, i);
			hoverPopup ??= new EditorTooltipPopup ();
			hoverPopup.ShowAtScreen (PopupOwner, this.PointToScreen (pt), header, desc, icon);
			return;
		}
	}

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

	// ----- Code folding (legacy IFoldable: ToggleFold/ToggleAllFolds/FoldDefinitions).
	// Each fold is a brace-delimited block: foldStart = line with the opening brace,
	// foldEnd = line with the matching closing brace. Collapsed folds render the
	// legacy summary marker ("{ … }") on the start line and skip hidden lines.
	readonly List<(int foldStart, int foldEnd)> foldRegions = new ();
	readonly HashSet<int> collapsedFolds = new ();
	bool foldingEnabled = true;

	/// <summary>Number of foldable regions (for tests/status).</summary>
	public int FoldRegionCount => foldRegions.Count;

	/// <summary>True when the fold starting at the caret's line is collapsed.</summary>
	public bool IsFoldCollapsedAtCaret => collapsedFolds.Contains (caretLine);

	// Recomputes the fold regions from brace balance (like the legacy fold parser
	// that rebuilds FoldSegments when the buffer changes).
	public void RebuildFolds ()
	{
		foldRegions.Clear ();
		if (!foldingEnabled)
			return;
		var stack = new Stack<int> ();
		bool inBlockComment = false;
		for (int i = 0; i < lines.Count; i++) {
			var line = lines [i];
			for (int c = 0; c < line.Length - 1; c++) {
				if (!inBlockComment && line [c] == '/' && line [c + 1] == '*') {
					inBlockComment = true;
					c++;
				} else if (inBlockComment && line [c] == '*' && line [c + 1] == '/') {
					inBlockComment = false;
					c++;
				}
			}
			bool hasCode = line.Trim ().Length > 0;
			foreach (char ch in line) {
				if (ch == '{')
					stack.Push (i);
				else if (ch == '}' && stack.Count > 0) {
					int start = stack.Pop ();
					// Multi-line blocks only (the legacy ignores single-line folds).
					if (i > start && hasCode)
						foldRegions.Add ((start, i));
				}
			}
		}
		// Innermost region wins when several share a start line.
		foldRegions.Sort ((a, b) => a.foldStart == b.foldStart
			? b.foldEnd.CompareTo (a.foldEnd)
			: a.foldStart.CompareTo (b.foldStart));
		var seen = new HashSet<int> ();
		for (int i = foldRegions.Count - 1; i >= 0; i--) {
			if (!seen.Add (foldRegions [i].foldStart))
				foldRegions.RemoveAt (i);
		}
		collapsedFolds.RemoveWhere (l => !foldRegions.Any (r => r.foldStart == l));
		MarkDirty ();
	}

	// Lines hidden inside collapsed folds: a line is hidden when it falls strictly
	// inside any collapsed region whose start line is itself visible.
	bool IsLineHidden (int line)
	{
		foreach (var (start, end) in foldRegions) {
			if (collapsedFolds.Contains (start) && line > start && line <= end)
				return true;
		}
		return false;
	}

	int VisibleLineCount => foldRegions.Count == 0 || collapsedFolds.Count == 0
		? lines.Count
		: Enumerable.Range (0, lines.Count).Count (l => !IsLineHidden (l));

	// Maps a rendered row (nth visible line) back to its buffer line index.
	int RowToLine (int row)
	{
		if (collapsedFolds.Count == 0)
			return row;
		int hidden = 0;
		for (int l = 0; l < lines.Count; l++) {
			if (IsLineHidden (l)) {
				hidden++;
				continue;
			}
			if (l - hidden == row)
				return l;
		}
		return lines.Count - 1;
	}

	// Maps a buffer line to its rendered row (-1 when hidden).
	int LineToRow (int line)
	{
		int row = 0;
		for (int l = 0; l < Math.Min (line, lines.Count); l++)
			if (!IsLineHidden (l))
				row++;
		return IsLineHidden (line) ? -1 : row;
	}

	public void ToggleFolding ()
	{
		// Legacy FoldActions.ToggleFold: toggle the innermost fold at the caret.
		int? best = null;
		int bestEnd = int.MaxValue;
		foreach (var (start, end) in foldRegions) {
			if (caretLine >= start && caretLine <= end && end < bestEnd) {
				best = start;
				bestEnd = end;
			}
		}
		if (best is int startLine) {
			if (!collapsedFolds.Add (startLine))
				collapsedFolds.Remove (startLine);
		}
		MarkDirty ();
	}

	public void ToggleAllFoldings ()
	{
		// Legacy FoldActions.ToggleAllFolds: collapse all unless any is collapsed.
		if (collapsedFolds.Count > 0)
			collapsedFolds.Clear ();
		else
			foreach (var (start, _) in foldRegions)
				collapsedFolds.Add (start);
		MarkDirty ();
	}

	public void FoldDefinitions ()
	{
		// Legacy: toggle state of member folds (blocks with lower indentation than
		// the type body); the new shell approximates "definitions" as folds of depth
		// 1+ from the outermost brace (methods inside a class).
		bool anyCollapsed = collapsedFolds.Count > 0;
		collapsedFolds.Clear ();
		if (!anyCollapsed) {
			foreach (var (start, end) in foldRegions)
				collapsedFolds.Add (start);
		}
		MarkDirty ();
	}

	public void EnableDisableFolding ()
	{
		foldingEnabled = !foldingEnabled;
		if (!foldingEnabled) {
			collapsedFolds.Clear ();
			foldRegions.Clear (); // no folding → no fold segments (legacy same)
		} else
			RebuildFolds ();
		MarkDirty ();
	}

	// ViewCommands.CenterAndFocusCurrentDocument: scroll so the caret line sits
	// in the middle of the viewport (legacy centers the caret and focuses the view).
	public void CenterCaret ()
	{
		int visible = Math.Max (1, (int)((Bounds.Height > 0 ? Bounds.Height : 400) / LineHeight));
		scrollLines = Math.Clamp (caretLine - visible / 2, 0, Math.Max (0, lines.Count - visible));
		MarkDirty ();
	}

	// ----- Message bubbles (legacy MessageBubbleCommands: Never/ForErrors/
	// ForErrorsAndWarnings). Inline markers rendered at the end of the affected
	// line, like the legacy MessageBubble over the text view.
	public enum BubbleMode { Never, ForErrors, ForErrorsAndWarnings }
	BubbleMode bubbleMode = BubbleMode.ForErrors;
	readonly Dictionary<int, (string text, bool isError)> lineBubbles = new ();

	/// <summary>Sets the inline diagnostic markers (called from the Errors pad data).</summary>
	public void SetBubbles (IEnumerable<(int line, string text, bool isError)> bubbles)
	{
		lineBubbles.Clear ();
		foreach (var b in bubbles)
			lineBubbles [b.line] = (b.text, b.isError);
		MarkDirty ();
	}

	public void SetBubbleMode (BubbleMode mode)
	{
		bubbleMode = mode;
		MarkDirty ();
	}

	public BubbleMode CurrentBubbleMode => bubbleMode;

	// Toggles between the three legacy states.
	public void ToggleBubbles ()
	{
		bubbleMode = bubbleMode switch {
			BubbleMode.Never => BubbleMode.ForErrors,
			BubbleMode.ForErrors => BubbleMode.ForErrorsAndWarnings,
			_ => BubbleMode.Never,
		};
		MarkDirty ();
	}

	// Renders an error bubble after the line text (red) or warning bubble (amber).
	void DrawBubbles (SKCanvas canvas, int line, float x, float baseline, SKFont font, SKPaint paint)
	{
		if (bubbleMode == BubbleMode.Never || !lineBubbles.TryGetValue (line, out var b))
			return;
		if (b.isError && bubbleMode == BubbleMode.Never)
			return;
		if (!b.isError && bubbleMode == BubbleMode.ForErrors)
			return;
		paint.Color = b.isError ? new SKColor (0xf1, 0x6a, 0x6a) : new SKColor (0xe5, 0xb5, 0x6a);
		canvas.DrawText ("● " + b.text, x + 12, baseline, font, paint);
	}

	// ----- Debug data tip (legacy TextEditor inline value bubble): while paused,
	// MainWindow sets the value of the variable on the stopped line and it renders
	// as a green inline bubble like VS's DataTip.
	(int line, string text)? dataTip;

	/// <summary>Shows the inline data tip on a line (0-based); null clears it.</summary>
	public void SetDataTip (int? line0Based, string? text)
	{
		dataTip = line0Based is int l && text is not null ? (l, text) : null;
		MarkDirty ();
	}

	public (int Line, string Text)? CurrentDataTip => dataTip;

	// ----- Pinned watches (legacy Debugger.PinnedWatch adorners): expressions
	// pinned to a line, rendered as inline bubbles after the text. The store
	// (file/line/expression) lives in the debugger addin (WatchService) and
	// round-trips through the legacy PinnedWatches user-prefs key. -----
	readonly Dictionary<int, List<string>> pinnedWatches = new (); // line (0-based) → expressions

	/// <summary>Raised on any pinned-watch change so the window persists the store.</summary>
	public event EventHandler? PinnedWatchesChanged;

	/// <summary>All pinned watches of this document: (0-based line, expression).</summary>
	public IReadOnlyList<(int Line, string Expression)> PinnedWatchList
		=> pinnedWatches.OrderBy (kv => kv.Key).SelectMany (kv => kv.Value.Select (e => (kv.Key, e))).ToList ();

	public bool HasPinnedWatch (int line0Based, string expression)
		=> pinnedWatches.TryGetValue (line0Based, out var l) && l.Contains (expression);

	/// <summary>Pins (or unpins when already present) an expression to a line.</summary>
	public void TogglePinnedWatch (int line0Based, string expression)
	{
		if (string.IsNullOrEmpty (expression))
			return;
		if (!pinnedWatches.TryGetValue (line0Based, out var list)) {
			list = new List<string> ();
			pinnedWatches [line0Based] = list;
		}
		if (!list.Remove (expression))
			list.Add (expression);
		else if (list.Count == 0)
			pinnedWatches.Remove (line0Based);
		MarkDirty ();
		PinnedWatchesChanged?.Invoke (this, EventArgs.Empty);
	}

	public void RemovePinnedWatch (int line0Based, string expression)
	{
		if (pinnedWatches.TryGetValue (line0Based, out var list) && list.Remove (expression)) {
			if (list.Count == 0)
				pinnedWatches.Remove (line0Based);
			MarkDirty ();
			PinnedWatchesChanged?.Invoke (this, EventArgs.Empty);
		}
	}

	public void SetPinnedWatches (IEnumerable<(int Line, string Expression)> watches)
	{
		pinnedWatches.Clear ();
		foreach (var (line, expr) in watches) {
			if (!pinnedWatches.TryGetValue (line, out var list)) {
				list = new List<string> ();
				pinnedWatches [line] = list;
			}
			if (!list.Contains (expr))
				list.Add (expr);
		}
		MarkDirty ();
	}

	// Live labels per pinned expression — set on every debugger stop (legacy
	// PinnedWatch.Evaluate): "expr = value" replaces the static "expr = ?".
	IReadOnlyList<(int line, string label)>? pinnedWatchValues;

	public IReadOnlyList<(int line, string label)>? PinnedWatchValues => pinnedWatchValues;

	public void SetPinnedWatchValues (IReadOnlyList<(int line, string label)>? values)
	{
		pinnedWatchValues = values;
		MarkDirty ();
	}

	/// <summary>Context menu shown when right-clicking a line with pinned
	/// watches (or anywhere, via Pin Watch): remove the pinned expression(s) of
	/// that line — the pin action is provided by the editor context menu.</summary>
	public Avalonia.Controls.ContextMenu? BuildPinnedWatchMenu (int line0Based)
	{
		if (!pinnedWatches.TryGetValue (line0Based, out var list) || list.Count == 0)
			return null;
		var menu = new Avalonia.Controls.ContextMenu ();
		foreach (var expr in list.ToList ()) {
			var captured = expr;
			var item = new Avalonia.Controls.MenuItem { Header = $"Remove pinned watch '{expr}'" };
			item.Click += (_, _) => RemovePinnedWatch (line0Based, captured);
			menu.Items.Add (item);
		}
		return menu;
	}

	// ----- Completion (legacy TextEditorCommands.ShowCompletionWindow = "Complete Word",
	// ShowParameterCompletionWindow = parameter info, ToggleCompletionSuggestionMode,
	// ShowCodeTemplateWindow, ShowCodeSurroundingsWindow). -----

	// All distinct words of the document with length >= 2 (the legacy completion
	// data source aggregates document words when no language model is available).
	IEnumerable<string> DocumentWords ()
	{
		var set = new HashSet<string> ();
		foreach (var line in lines) {
			int start = -1;
			for (int i = 0; i <= line.Length; i++) {
				bool wordChar = i < line.Length && (char.IsLetterOrDigit (line [i]) || line [i] == '_');
				if (wordChar && start < 0)
					start = i;
				else if (!wordChar && start >= 0) {
					if (i - start >= 2)
						set.Add (line.Substring (start, i - start));
					start = -1;
				}
			}
		}
		return set;
	}

	string WordPrefixBeforeCaret ()
	{
		var line = lines [caretLine];
		int end = Math.Min (caretCol, line.Length);
		int start = end;
		while (start > 0 && (char.IsLetterOrDigit (line [start - 1]) || line [start - 1] == '_'))
			start--;
		return line.Substring (start, end - start);
	}

	/// <summary>Legacy Complete Word: unique match → insert; multiple → cycle
	/// candidates on repeated calls. Returns the inserted text or null.</summary>
	public string? CompleteWord ()
	{
		var prefix = WordPrefixBeforeCaret ();
		if (prefix.Length == 0)
			return null;
		var candidates = DocumentWords ()
			.Where (w => w != prefix && w.StartsWith (prefix, StringComparison.Ordinal))
			.OrderBy (w => w, StringComparer.Ordinal)
			.ToList ();
		if (candidates.Count == 0)
			return null;
		string pick;
		if (candidates.Count == 1) {
			pick = candidates [0]; // legacy: commit the unique completion immediately
		} else {
			int idx = completionCycle.TryGetValue (prefix, out var c) ? (c + 1) % candidates.Count : 0;
			completionCycle [prefix] = idx;
			pick = candidates [idx];
		}
		// Replace the prefix with the completed word.
		var line = lines [caretLine];
		int end = Math.Min (caretCol, line.Length);
		lines [caretLine] = line.Substring (0, end - prefix.Length) + pick + line.Substring (end);
		caretCol = end - prefix.Length + pick.Length;
		Commit ();
		return pick;
	}
	readonly Dictionary<string, int> completionCycle = new ();

	// Legacy parameter info: name of the method whose '(' precedes the caret.
	public string? ParameterHint ()
	{
		var line = lines [caretLine];
		for (int i = Math.Min (caretCol, line.Length) - 1; i >= 0; i--) {
			char c = line [i];
			if (c == '(') {
				int s = i - 1;
				while (s >= 0 && (char.IsLetterOrDigit (line [s]) || line [s] == '_' || line [s] == '.'))
					s--;
				var name = line.Substring (s + 1, i - s - 1);
				return name.Length == 0 ? null : name;
			}
			if (c == ')')
				break;
		}
		return null;
	}

	bool suggestionMode = true;
	/// <summary>Legacy suggestion mode toggle (soft-selection completion).</summary>
	public bool ToggleCompletionSuggestionMode () => suggestionMode = !suggestionMode;
	public bool SuggestionMode => suggestionMode;

	// Legacy code templates (CodeTemplate addin): expand "cw", "prop", "fore"…
	static readonly Dictionary<string, string[]> CodeTemplates = new () {
		["cw"] = new [] { "Console.WriteLine ($end$);" },
		["prop"] = new [] { "public int MyProperty { get; set; }" },
		["fore"] = new [] { "foreach (var item in collection) {", "", "}" },
		["forr"] = new [] { "for (int i = length - 1; i >= 0; i--) {", "", "}" },
		["svm"] = new [] { "static void Main (string[] args)", "{" , "", "}" },
		["if"] = new [] { "if (condition) {", "", "}" },
	};

	/// <summary>Expands the code template named by the word before the caret.
	/// Returns the template name or null when there is no match.</summary>
	public string? ExpandCodeTemplate ()
	{
		var prefix = WordPrefixBeforeCaret ();
		if (prefix.Length == 0 || !CodeTemplates.TryGetValue (prefix, out var template))
			return null;
		// Remove the trigger word.
		var line = lines [caretLine];
		int end = Math.Min (caretCol, line.Length);
		int start = end - prefix.Length;
		var indent = new string (' ', start - (line.Length - line.TrimStart ().Length) > 0
			? 0 : 0); // templates carry their own formatting
		var expanded = template.ToList ();
		expanded [0] = line.Substring (0, start) + expanded [0];
		for (int i = 1; i < expanded.Count; i++)
			expanded [i] = new string (' ', Math.Max (0, start)) + expanded [i];
		lines [caretLine] = expanded [0];
		for (int i = expanded.Count - 1; i >= 1; i--)
			lines.Insert (caretLine + 1, expanded [i]);
		Commit ();
		return prefix;
	}

	public static readonly string[] SurroundTemplates = { "if", "while", "for", "foreach", "try" };

	/// <summary>Surrounds the selection with a block (legacy ShowCodeSurroundingsWindow
	/// offers the same list). Returns the used template or null without selection.</summary>
	public string? SurroundSelectionWith (string template)
	{
		if (!hasSelection)
			return null;
		var (sl, sc, el, ec) = SelectionRange ();
		string head = template switch {
			"while" => "while (condition) {",
			"for" => "for (int i = 0; i < length; i++) {",
			"foreach" => "foreach (var item in collection) {",
			"try" => "try {",
			_ => "if (condition) {",
		};
		string indent = new string (' ', sc);
		// Extract selected text
		var selected = new List<string> ();
		if (sl == el)
			selected.Add (lines [sl].Substring (sc, ec - sc));
		else {
			selected.Add (lines [sl].Substring (sc));
			for (int l = sl + 1; l < el; l++)
				selected.Add (lines [l]);
			selected.Add (lines [el].Substring (0, ec));
		}
		var replacement = new List<string> { indent + head };
		foreach (var l in selected)
			replacement.Add ("    " + l.TrimStart ());
		replacement.Add (indent + (template == "try" ? "} catch (Exception ex) {" : "}"));
		if (template == "try")
			replacement.Add (indent + "}");
		// Splice the replacement back into the buffer.
		lines [sl] = lines [sl].Substring (0, sc) + replacement [0];
		lines [el] = replacement [^1] + lines [el].Substring (ec);
		var middle = replacement.Skip (1).Take (replacement.Count - 2).ToList ();
		if (middle.Count > 0)
			lines.InsertRange (sl + 1, middle);
		RebuildFolds ();
		Commit ();
		return template;
	}

	public bool HasSelectionText => hasSelection && SelectedText.Length > 0;

	/// <summary>Bookmarked lines (0-based) for the Bookmarks pad
	/// (the legacy pad lists the store's marks per document).</summary>
	public IReadOnlyCollection<int> BookmarkLines => bookmarkLines;

	// ----- Breakpoints (Mono.Debugging BreakpointStore parity: toggle at line,
	// enable/disable per entry, gutter marker like the legacy red circle) -----
	readonly Dictionary<int, bool> breakpointLines = new (); // line → enabled

	// Current execution line (DebuggingService yellow arrow): -1 when not paused.
	int executionLine = -1;

	/// <summary>Sets/clears the execution-line highlight (0-based; -1 clears it).</summary>
	public void SetExecutionLine (int line0Based)
	{
		executionLine = line0Based;
		MarkDirty ();
	}

	/// <summary>Raised on any breakpoint change so MainWindow can persist the store.</summary>
	public event EventHandler? BreakpointsChanged;

	public IReadOnlyDictionary<int, bool> BreakpointLines => breakpointLines;

	// ----- Advanced breakpoint attributes (legacy Breakpoint.Condition / HitCount /
	// Tracepoint): line → (condition expression, hit count, log message). Sent to
	// the DAP adapter on every (re)launch and shown in the Breakpoints pad. -----
	public sealed record BreakpointOptions (string? Condition = null, int? HitCount = null, string? LogMessage = null);
	readonly Dictionary<int, BreakpointOptions> breakpointOptions = new (); // line (0-based) → attributes

	/// <summary>Full store info: line (0-based) → enabled + attributes.</summary>
	public IReadOnlyDictionary<int, (bool Enabled, BreakpointOptions Options)> Breakpoints
		=> breakpointLines.ToDictionary (kv => kv.Key, kv => (kv.Value, breakpointOptions.TryGetValue (kv.Key, out var o) ? o : new BreakpointOptions ()));

	/// <summary>Sets condition/hit count/log message for the breakpoint at a line
	/// (0-based). Passing all-null clears the attributes.</summary>
	public void SetBreakpointOptions (int line, string? condition, int? hitCount, string? logMessage)
	{
		if (!breakpointLines.ContainsKey (line))
			return;
		if (string.IsNullOrWhiteSpace (condition) && hitCount is null && string.IsNullOrWhiteSpace (logMessage))
			breakpointOptions.Remove (line);
		else
			breakpointOptions [line] = new BreakpointOptions (
				string.IsNullOrWhiteSpace (condition) ? null : condition.Trim (),
				hitCount,
				string.IsNullOrWhiteSpace (logMessage) ? null : logMessage.Trim ());
		MarkDirty ();
		BreakpointsChanged?.Invoke (this, EventArgs.Empty);
	}

	public BreakpointOptions? GetBreakpointOptions (int line)
		=> breakpointOptions.TryGetValue (line, out var o) ? o : null;

	/// <summary>Legacy left-margin click path: toggles the breakpoint of a line
	/// through the exact code a gutter click runs (Mono.TextEditor ActionTextArea
	/// "left margin hit test"), including BreakpointsChanged + persistence hooks.</summary>
	public void ToggleBreakpointAtGutter (int line0Based)
	{
		caretLine = Math.Clamp (line0Based, 0, lines.Count - 1);
		ToggleBreakpoint ();
	}

	public void ToggleBreakpoint ()
	{
		if (!breakpointLines.Remove (caretLine))
			breakpointLines [caretLine] = true;
		breakpointOptions.Remove (caretLine);
		MarkDirty ();
		BreakpointsChanged?.Invoke (this, EventArgs.Empty);
	}

	public void ToggleBreakpointEnabled (int line)
	{
		if (breakpointLines.TryGetValue (line, out var en))
			breakpointLines [line] = !en;
		MarkDirty ();
		BreakpointsChanged?.Invoke (this, EventArgs.Empty);
	}

	public void RemoveBreakpoint (int line)
	{
		if (breakpointLines.Remove (line)) {
			breakpointOptions.Remove (line);
			MarkDirty ();
			BreakpointsChanged?.Invoke (this, EventArgs.Empty);
		}
	}

	public void ClearBreakpoints ()
	{
		if (breakpointLines.Count == 0)
			return;
		breakpointLines.Clear ();
		breakpointOptions.Clear ();
		MarkDirty ();
		BreakpointsChanged?.Invoke (this, EventArgs.Empty);
	}

	/// <summary>Restores a persisted store (0-based lines → enabled + attributes).</summary>
	public void SetBreakpoints (IEnumerable<KeyValuePair<int, (bool Enabled, BreakpointOptions Options)>> lines)
	{
		breakpointLines.Clear ();
		breakpointOptions.Clear ();
		foreach (var (l, (en, opts)) in lines) {
			breakpointLines [l] = en;
			if (opts.Condition is not null || opts.HitCount is not null || opts.LogMessage is not null)
				breakpointOptions [l] = opts;
		}
		MarkDirty ();
	}

	public void NextBreakpoint ()
	{
		if (breakpointLines.Count == 0)
			return;
		var next = breakpointLines.Keys.Where (l => l > caretLine).OrderBy (l => l).FirstOrDefault (-1);
		if (next < 0)
			next = breakpointLines.Keys.Min ();
		GotoLine (next);
	}

	public void PrevBreakpoint ()
	{
		if (breakpointLines.Count == 0)
			return;
		var prev = breakpointLines.Keys.Where (l => l < caretLine).OrderByDescending (l => l).FirstOrDefault (-1);
		if (prev < 0)
			prev = breakpointLines.Keys.Max ();
		GotoLine (prev);
	}

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

	// ----- Test/QA accessors (the document model is testable without a running
	// Avalonia platform; they expose state the public API cannot observe) -----
	public int LineCountForTest => lines.Count;
	public string LineTextForTest (int line) => lines [Math.Clamp (line, 0, lines.Count - 1)];
	public int CurrentLineForTest => caretLine;
	public int CurrentColumnForTest => caretCol;

	public void AddSecondaryCaretForTest (int line, int col)
	{
		line = Math.Clamp (line, 0, lines.Count - 1);
		col = Math.Clamp (col, 0, lines [line].Length);
		if (!secondaryCarets.Contains ((line, col)))
			secondaryCarets.Add ((line, col));
	}

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

	// ----- Code formatting (legacy MonoDevelop.CSharpFormatting: brace-driven
	// reindent plus blank-line normalization; the new shell applies the structural
	// pass that does not require the Roslyn workspace). -----

	/// <summary>Reindents the whole buffer by brace depth and normalizes the
	/// spacing around braces, mirroring the structural half of FormatBuffer.</summary>
	public int FormatBuffer ()
	{
		PushUndo (string.Join ("\n", lines));
		int indent = 0;
		var outLines = new List<string> (lines.Count);
		foreach (var raw in lines) {
			var trimmed = raw.Trim ();
			if (trimmed.Length == 0) {
				outLines.Add ("");
				continue;
			}
			// Closing braces outdent before the line, opening braces indent after.
			if (trimmed.StartsWith ("}", StringComparison.Ordinal))
				indent = Math.Max (0, indent - 1);
			var body = trimmed;
			// Normalize: collapse whitespace runs, space inside braces.
			body = System.Text.RegularExpressions.Regex.Replace (body, "\\s+", " ");
			body = body.Replace (" { ", " {").Replace ("{ ", "{ ").Replace (" }", " }");
			outLines.Add (new string (' ', indent * 4) + body);
			int opens = body.Count (c => c == '{');
			int closes = body.Count (c => c == '}');
			if (closes > opens)
				indent = Math.Max (0, indent - (closes - opens));
			indent += Math.Max (0, opens - closes);
		}
		int changed = 0;
		for (int i = 0; i < Math.Min (lines.Count, outLines.Count); i++)
			if (lines [i] != outLines [i])
				changed++;
		lines.Clear ();
		lines.AddRange (outLines);
		caretLine = Math.Clamp (caretLine, 0, lines.Count - 1);
		caretCol = Math.Clamp (caretCol, 0, lines [caretLine].Length);
		if (changed > 0)
			Commit ();
		return changed;
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

	#region Rendering

	float GutterWidth () => (float)FontSize * 0.6f * (lines.Count.ToString ().Length + 1) + 10;

	/// <summary>Width of the clickable breakpoint strip: the red circle sits at
	/// gutterW - 9, so the strip is the last 18px of the gutter (like the legacy
	/// left margin icon column).</summary>
		float GutterIconStripWidth () => Math.Max (0, GutterWidth () - 18);

	// ----- Gutter hover polish (legacy Mono.TextEditor gutter area): the row
	// under the pointer highlights, the breakpoint strip shows the hand cursor
	// (like ActionTextArea's margin cursors) and a tooltip announces the line
	// the click would toggle. Pure hover state — no caret changes. -----
	bool pointerInGutter;
	bool pointerInBpStrip;
	int hoverGutterLine = -1;

	/// <summary>Live gutter hover state for QA: line under the pointer
	/// (0-based, -1 when outside the gutter) and whether the hand cursor
	/// (breakpoint strip) is active.</summary>
	public (int Line, bool InBreakpointStrip) GutterHover => (hoverGutterLine, pointerInBpStrip);

	bool IsInBreakpointStrip (double x) => x > GutterIconStripWidth () && x <= GutterWidth ();

	void UpdateGutterHover (Point p)
	{
		bool inGutter = p.X <= GutterWidth ();
		bool inStrip = inGutter && IsInBreakpointStrip (p.X);
		int line = inGutter
			? Math.Clamp ((int)(p.Y / LineHeight + scrollLines), 0, Math.Max (0, lines.Count - 1))
			: -1;
		if (inGutter == pointerInGutter && inStrip == pointerInBpStrip && line == hoverGutterLine)
			return;
		pointerInGutter = inGutter;
		pointerInBpStrip = inStrip;
		hoverGutterLine = line;
		Cursor = inStrip ? new Cursor (StandardCursorType.Hand) : Cursor.Default;
		ShowGutterTip (line);
		MarkDirty ();
	}

	/// <summary>QA: clears the gutter hover state without a pointer exit event.</summary>
	public void ClearGutterHover ()
	{
		if (!pointerInGutter && hoverGutterLine < 0)
			return;
		pointerInGutter = false;
		pointerInBpStrip = false;
		hoverGutterLine = -1;
		Cursor = Cursor.Default;
		ShowGutterTip (-1);
		MarkDirty ();
	}

	/// <summary>QA-only: drives the gutter hover pipeline without a pointer —
	/// the same state a real hover over the breakpoint strip produces
	/// (highlight row, hand cursor, "Line N" tooltip).</summary>
	public void SimulateGutterHoverForQa (int line0Based, bool breakpointStrip)
	{
		hoverGutterLine = Math.Clamp (line0Based, 0, Math.Max (0, lines.Count - 1));
		pointerInGutter = true;
		pointerInBpStrip = breakpointStrip;
		Cursor = breakpointStrip ? new Cursor (StandardCursorType.Hand) : Cursor.Default;
		ShowGutterTip (hoverGutterLine);
		MarkDirty ();
	}

	// Tooltip with the line number the gutter click would act on (0-based
	// internal, 1-based in the message, like the legacy margin tooltip).
	void ShowGutterTip (int line)
	{
		var tip = ToolTip.GetTip (this);
		if (line < 0) {
			if (tip is TextBlock tb && tb.Text is { Length: > 0 } t && t.StartsWith ("Line ", StringComparison.Ordinal)) {
				ToolTip.SetTip (this, null);
				return;
			}
		}
		var text = pointerInBpStrip
			? $"Line {line + 1} — click to toggle breakpoint"
			: $"Line {line + 1}";
		if (tip is TextBlock existing && existing.Text == text)
			return;
		ToolTip.SetTip (this, new TextBlock { Text = text, FontSize = 11 });
	}

	public override void Render (DrawingContext context)
	{
		int w = Math.Max (1, (int)Math.Ceiling (Bounds.Width));
		int h = Math.Max (1, (int)Math.Ceiling (Bounds.Height));
		if (dirty || front is null || bufferW != w || bufferH != h) {
			// Render each frame into a FRESH bitmap: the compositor may still read
			// the previous frame's bitmap while Avalonia re-runs Render (typing
			// invalidates faster than the compositor consumes), and repainting that
			// shared memory smeared the old frame under the new one — the ghost/
			// duplicated-line artifacts. A new bitmap per frame is immutable from
			// the compositor's point of view; the old one is released after.
			var next = new WriteableBitmap (new PixelSize (w, h), new Vector (96, 96), PixelFormats.Bgra8888, AlphaFormat.Opaque);
			using (var fb = next.Lock ()) {
				var info = new SKImageInfo (w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
				using (var surface = SKSurface.Create (info, fb.Address, fb.RowBytes)) {
					if (surface is null)
						Console.WriteLine ("[skeditor] SKSurface.Create returned null");
					else
						Draw (surface.Canvas);
				}
			}
			var old = front;
			front = next;
			bufferW = w;
			bufferH = h;
			dirty = false;
			pendingDispose?.Dispose (); // release the frame from two renders ago
			pendingDispose = old; // the just-replaced frame may still be in flight
		}
		if (front is not null)
			context.DrawImage (front, new Rect (0, 0, Bounds.Width, Bounds.Height));
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
		// gutter hover row (legacy Mono.TextEditor gutter hover highlight)
		if (pointerInGutter && hoverGutterLine >= 0) {
			float hoverY = (hoverGutterLine - (float)scrollLines) * lineH;
			if (hoverY >= -lineH && hoverY <= (float)Bounds.Height) {
				using var hoverRow = new SKPaint { Color = gutterFg.WithAlpha (28), IsAntialias = false };
				canvas.DrawRect (0, hoverY, (float)Bounds.Width, lineH, hoverRow);
				// breakpoint strip gets its own stronger band (the hand-cursor zone)
				if (pointerInBpStrip) {
					using var stripBand = new SKPaint { Color = gutterFg.WithAlpha (26), IsAntialias = false };
					canvas.DrawRect (GutterIconStripWidth (), hoverY, gutterW - GutterIconStripWidth (), lineH, stripBand);
				}
			}
		}
		using var gutterLinePaint = new SKPaint { Color = gutterFg.WithAlpha (60), IsAntialias = false };
		canvas.DrawRect (gutterW - 1, 0, 1, (float)Bounds.Height, gutterLinePaint);

		int firstLine = (int)Math.Max (0, scrollLines);
		int visible = (int)Math.Ceiling ((float)Bounds.Height / lineH) + 1;

		using var textPaint = new SKPaint { IsAntialias = true };
		using var textFont = new SKFont (font.Typeface, (float)FontSize);

		int drawn = 0;
		int line = firstLine;
		while (line < lines.Count && drawn < visible) {
			int i = RowToLine (line); // skip folded (hidden) lines
			line++;
			if (i >= lines.Count)
				break;
			float y = (drawn - (float)scrollLines) * lineH;
			float baseline = y + (lineH - (float)FontSize) / 2f + (float)FontSize * 0.85f;
			drawn++;

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
			// breakpoint marker (legacy gutter breakpoint-15: red circle, hollow when
			// disabled like CellRendererImage stock md-breakpoint/md-breakpoint-disabled)
			if (breakpointLines.TryGetValue (i, out var bpEnabled)) {
				using var bpPaint = new SKPaint {
					Color = bpEnabled ? new SKColor (0xd6, 0x33, 0x2c) : new SKColor (0x66, 0x66, 0x66),
					IsAntialias = true,
					Style = bpEnabled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
					StrokeWidth = 1.6f,
				};
				canvas.DrawCircle (gutterW - 9, y + lineH / 2, 4.6f, bpPaint);
			}
			// caret line highlight in gutter
			if (i == caretLine) {
				using var activePaint = new SKPaint { Color = gutterFg.WithAlpha (40) };
				canvas.DrawRect (0, y, gutterW, lineH, activePaint);
			}

			// fold marker (legacy fold marker: [+] collapsed / [−] expanded) in gutter
			bool hasFold = foldRegions.Any (r => r.foldStart == i);
			if (hasFold && foldingEnabled) {
				bool collapsed = collapsedFolds.Contains (i);
				textPaint.Color = gutterFg;
				canvas.DrawText (collapsed ? "+" : "−", gutterW - 22, baseline, textFont, textPaint);
				if (collapsed) {
					// legacy collapsed-fold summary: dim "… }" after the start line text
					var endLine = foldRegions.First (r => r.foldStart == i).foldEnd;
					var summary = "  { … }  // " + (endLine - i) + " lines";
					textPaint.Color = gutterFg.WithAlpha (150);
					float sx = gutterW + 4 + lines [i].Length * charW;
					canvas.DrawText (summary, sx, baseline, textFont, textPaint);
				}
			}

			// selection highlight behind the text (legacy #3a5098 selection band)
			if (hasSelection) {
				var (selStart, selEnd) = SelectionOrder ();
				var (sLine, sCol, eLine, eCol) = SelectionRange ();
				if (i >= sLine && i <= eLine) {
					int c0 = i == sLine ? sCol : 0;
					int c1 = i == eLine ? eCol : lines [i].Length;
					using var selPaint = new SKPaint { Color = new SKColor (0x3a, 0x50, 0x98, 160), IsAntialias = false };
					canvas.DrawRect (gutterW + 4 + c0 * charW, y, Math.Max (2, (c1 - c0) * charW), lineH, selPaint);
				}
			}

			// execution line highlight (legacy CurrentLineNumber yellow arrow line)
			if (i == executionLine) {
				using var execPaint = new SKPaint { Color = new SKColor (0xe6, 0xd8, 0x3a, 90), IsAntialias = false };
				canvas.DrawRect (gutterW, y, (float)Bounds.Width - gutterW, lineH, execPaint);
			}

			// segments with basic highlighting
			var segments = Tokenize (lines [i], fg, comment, strColor, keywordColor, numberColor, keywordSet);
			float x = gutterW + 4;
			foreach (var seg in segments) {
				textPaint.Color = seg.Color;
				canvas.DrawText (seg.Text, x, baseline, textFont, textPaint);
				x += seg.Text.Length * charW;
			}
			// inline message bubble (legacy MessageBubble) after the line text
			DrawBubbles (canvas, i, x, baseline, textFont, textPaint);
			// pinned-watch bubbles (legacy PinnedWatch adorner): one amber bubble
			// per expression pinned to the line; on a debug stop each shows its
			// evaluated "expr = value" (static "expr = ?" between sessions).
			if (pinnedWatches.TryGetValue (i, out var pins)) {
				float px = x + 10;
				foreach (var pin in pins) {
					var label = pin;
					if (pinnedWatchValues is not null) {
						var live = pinnedWatchValues.FirstOrDefault (v => v.line == i && v.label.StartsWith (pin + " ", StringComparison.Ordinal) || v.label == pin).label;
						if (live is { Length: > 0 })
							label = live;
					}
					float pw = label.Length * charW + 14;
					using var pinBg = new SKPaint { Color = new SKColor (0x50, 0x3f, 0x1a), IsAntialias = false };
					canvas.DrawRect (px, y + 1, pw, lineH - 2, pinBg);
					using var pinBorder = new SKPaint { Color = new SKColor (0x8f, 0x74, 0x2e), IsAntialias = false };
					canvas.DrawRect (px, y + 1, pw, lineH - 2, pinBorder);
					textPaint.Color = new SKColor (0xe8, 0xcf, 0x9a);
					canvas.DrawText (label, px + 7, baseline, textFont, textPaint);
					px += pw + 6;
				}
			}
			// debug data tip (legacy inline value bubble) after the bubbles
			if (dataTip is { } tip && tip.line == i) {
				using var tipBg = new SKPaint { Color = new SKColor (0x2a, 0x4d, 0x2e), IsAntialias = false };
				float tw = tip.text.Length * charW + 14;
				canvas.DrawRect (x + 10, y + 1, tw, lineH - 2, tipBg);
				using var tipBorder = new SKPaint { Color = new SKColor (0x4e, 0x8f, 0x55), IsAntialias = false };
				canvas.DrawRect (x + 10, y + 1, tw, lineH - 2, tipBorder);
				textPaint.Color = new SKColor (0xa6, 0xd9, 0xaa);
				canvas.DrawText (tip.text, x + 17, baseline, textFont, textPaint);
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
