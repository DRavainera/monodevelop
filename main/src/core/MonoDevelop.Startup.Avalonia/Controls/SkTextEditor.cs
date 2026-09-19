using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
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
			InvalidateVisual ();
			e.Handled = true;
		}
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

	void Commit ()
	{
		SetValue (TextProperty, string.Join ("\n", lines));
		ShowCaret ();
	}

	void ShowCaret ()
	{
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

		// caret
		if (caretVisible) {
			float caretX = gutterW + 4 + caretCol * charW;
			float caretY = (caretLine - (float)scrollLines) * lineH;
			if (caretY >= -lineH && caretY <= (float)Bounds.Height) {
				using var caretPaint = new SKPaint { Color = caretColor, IsAntialias = false };
				canvas.DrawRect (caretX, Math.Max (0f, caretY), 1.5f, Math.Min (lineH, (float)Bounds.Height - Math.Max (0f, caretY)), caretPaint);
			}
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
