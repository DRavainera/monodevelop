//
// EditorMinimap.cs
//
// Minimap of a SkTextEditor: the right-hand overview bar other IDEs show
// (VS Code / Sublime style) — a scaled-down render of the WHOLE document with
// its syntax colors, plus a live overlay: the viewport rectangle, the caret
// row, breakpoint dots and error markers. Clicking (or dragging) on it jumps
// the editor's caret to the clicked line.
//
// Architecture. The syntax-colored scaled raster cannot be computed per frame
// (highlighting is ~0.9 ms/line — 3000 lines would take ~2.7 s on the UI
// thread), so it runs on a background Task with a short coalescing delay: a
// burst of edits cancels the pending work and re-schedules it. The control
// keeps the previous raster while the new one computes (double buffer, the
// same one-frame-late release SkTextEditor uses), and the cheap overlay
// (viewport/caret/dots) is drawn in Render() on every invalidation.
//
// Data source: the SkTextEditor itself — its line model, theme base colors,
// breakpoints, diagnostic bubbles and the ScrollViewer that owns the scroll
// state. The minimap never duplicates the editor's model; it only reads it.
//
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SkiaSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Controls;

/// <summary>
/// Scaled overview of a <see cref="SkTextEditor"/> document (file header).
/// Mount it as a sibling of the editor's ScrollViewer (fixed width, stretched
/// height) and call <see cref="Attach"/> once.
/// </summary>
public class EditorMinimap : Control
{
	/// <summary>Default width in DIPs (VS Code uses 132; compact for the shell).</summary>
	public const double DefaultWidth = 110.0;

	// The editor (the whole data source) and the ScrollViewer that owns the
	// scroll state (the editor's scrollOwner — the minimap reads the same
	// Offset/Viewport the editor reads, so the viewport indicator always
	// matches what the text shows).
	SkTextEditor? editor;
	ScrollViewer? scrollOwner;

	// --- Scaled raster (background, double-buffered) ---
	WriteableBitmap? raster;          // latest finished frame
	WriteableBitmap? pendingDispose;  // previous frame, released one frame later
	int rasterLines = -1;
	int rasterW, rasterH;
	CancellationTokenSource? rasterCts;
	volatile bool rasterDirty = true;

	// Event wiring (the wrapper detaches/reattaches on tab switches).
	bool wired;

	// Interaction
	bool dragging;
	int lastJmpRow = -1;

	// Overlay brushes are INSTANCE fields created in the constructor (UI thread),
	// NOT static: in Avalonia 12 an Avalonia object's Dispatcher is captured at
	// creation time, and a static Brush would be initialized on whichever thread
	// first touches the type (the background raster task reads the static
	// `typeface` field first, running the type initializer off the UI thread) —
	// that left the brushes owned by the background dispatcher and made
	// DrawRectangle in Render throw VerifyAccess. Instance + ctor-init keeps them
	// on the UI thread. (The Skia `typeface` below is fine as static: SkiaSharp
	// objects have no dispatcher affinity, and it is created/used on the raster
	// task thread consistently.)
	readonly Brush dimBrush = new SolidColorBrush (Color.FromArgb (0x52, 0, 0, 0));
	readonly Brush viewportBrush = new SolidColorBrush (Color.FromArgb (0x66, 0xFF, 0xFF, 0xFF));
	readonly Pen viewportPen = new (new SolidColorBrush (Color.FromArgb (0x66, 0xFF, 0xFF, 0xFF)), 1);
	readonly Brush caretBrush = new SolidColorBrush (Color.FromArgb (0xE6, 0x4F, 0x9E, 0xE8));
	readonly Brush breakpointBrush = new SolidColorBrush (Color.FromArgb (0xFF, 0xE0, 0x48, 0x3D));
	readonly Brush breakpointDisabledBrush = new SolidColorBrush (Color.FromArgb (0xFF, 0x8A, 0x8A, 0x8A));
	readonly Brush errorBrush = new SolidColorBrush (Color.FromArgb (0xFF, 0xF4, 0x4B, 0x4B));

	// QA accessors
	public int QaRasterFrames { get; private set; }
	public int QaLastRasterLines => rasterLines;
	public SkTextEditor? QaEditor => editor;
	public bool QaWired => wired;

	public EditorMinimap ()
	{
		Focusable = false;
		Cursor = new Cursor (StandardCursorType.Ibeam);
	}

	/// <summary>Binds the minimap to an editor document. The scroll viewer is the
	/// ScrollViewer the editor is mounted in — the shell mount point always passes
	/// it (the same owner the editor reads its scroll state from).</summary>
	public void Attach (SkTextEditor editor, ScrollViewer? scrollViewer = null)
	{
		if (ReferenceEquals (this.editor, editor))
			return;
		if (this.editor is not null)
			Unwire ();
		this.editor = editor;
		this.scrollOwner = scrollViewer;
		raster = null;
		rasterLines = -1;
		lastJmpRow = -1;
		rasterDirty = true;
		// Wire eagerly: it is idempotent (OnAttachedToVisualTree re-calls it as a
		// no-op) and InvalidateVisual on a not-yet-attached control is a safe no-op.
		Wire ();
		RequestRaster ();
		InvalidateVisual ();
	}

	void Wire ()
	{
		var ed = editor;
		if (ed is null || wired)
			return;
		ed.PropertyChanged += OnEditorPropertyChanged;
		ed.CaretMoved += OnEditorCaret;
		ed.BreakpointsChanged += OnEditorDiagnostics;
		ed.DiagnosticsChanged += OnEditorDiagnostics;
		if (scrollOwner is not null)
			scrollOwner.ScrollChanged += OnScrollChanged;
		wired = true;
	}

	void Unwire ()
	{
		if (!wired)
			return;
		var ed = editor;
		if (ed is not null) {
			ed.PropertyChanged -= OnEditorPropertyChanged;
			ed.CaretMoved -= OnEditorCaret;
			ed.BreakpointsChanged -= OnEditorDiagnostics;
			ed.DiagnosticsChanged -= OnEditorDiagnostics;
		}
		if (scrollOwner is not null)
			scrollOwner.ScrollChanged -= OnScrollChanged;
		wired = false;
	}

	protected override void OnAttachedToVisualTree (VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree (e);
		Wire ();
	}

	protected override void OnDetachedFromVisualTree (VisualTreeAttachmentEventArgs e)
	{
		Unwire ();
		rasterCts?.Cancel ();
		base.OnDetachedFromVisualTree (e);
	}

	void OnEditorPropertyChanged (object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == SkTextEditor.TextProperty) {
			// Content generation change: recompute the scaled raster (the slow
			// path) and refresh the overlay (the caret/line-count moved).
			RequestRaster ();
			InvalidateVisual ();
		} else if (e.Property == SkTextEditor.FontSizeProperty) {
			RequestRaster ();
		}
	}

	void OnEditorCaret () => InvalidateVisual ();
	void OnEditorDiagnostics (object? sender, EventArgs e) => InvalidateVisual ();
	void OnScrollChanged (object? sender, ScrollChangedEventArgs e) => InvalidateVisual ();

	protected override void OnSizeChanged (SizeChangedEventArgs e)
	{
		InvalidateVisual ();
		// The raster is size-bound; a resize re-scales the whole document.
		RequestRaster ();
	}

	// ----- Interaction: click / drag jumps the editor to the clicked line -----

	protected override void OnPointerPressed (PointerPressedEventArgs e)
	{
		base.OnPointerPressed (e);
		if (editor is null || !e.GetCurrentPoint (this).Properties.IsLeftButtonPressed)
			return;
		dragging = true;
		e.Pointer.Capture (this);
		JumpToY (e.GetPosition (this).Y);
		e.Handled = true;
	}

	protected override void OnPointerMoved (PointerEventArgs e)
	{
		base.OnPointerMoved (e);
		if (dragging && editor is not null)
			JumpToY (e.GetPosition (this).Y);
	}

	protected override void OnPointerReleased (PointerReleasedEventArgs e)
	{
		base.OnPointerReleased (e);
		if (dragging) {
			dragging = false;
			e.Pointer.Capture (null);
		}
	}

	/// <summary>Maps a minimap Y to a document line and moves the editor's caret
	/// there (EnsureCaretVisible inside GotoLine scrolls it into view).</summary>
	void JumpToY (double y)
	{
		Jump (y);
	}

	/// <summary>QA: drive the exact click→line mapping OnPointerPressed uses,
	/// without a real pointer (the pointer path itself is covered by the Xvfb
	/// capture).</summary>
	public void QaJumpToY (double y) => Jump (y);

	void Jump (double y)
	{
		var ed = editor;
		if (ed is null)
			return;
		int lineCount = ed.MinimapLineCount;
		if (lineCount == 0)
			return;
		double frac = Math.Clamp (y / Math.Max (1, Bounds.Height), 0, 1);
		int row = Math.Clamp ((int)(frac * lineCount), 0, lineCount - 1);
		if (row == lastJmpRow)
			return;
		lastJmpRow = row;
		Console.WriteLine ("[minimap] jump y=" + ((int)y) + " frac=" + frac.ToString ("F3") + " row=" + row);
		ed.GotoLine (row);
	}

	// ----- Scaled raster (background thread, debounced, double-buffered) -----

	void RequestRaster ()
	{
		var ed = editor;
		if (ed is null)
			return;
		int w = Math.Max (8, (int)Math.Ceiling (Bounds.Width));
		int h = Math.Max (8, (int)Math.Ceiling (Bounds.Height));
		if (raster is not null && w == rasterW && h == rasterH && !rasterDirty)
			return; // same size, content unchanged
		rasterDirty = true;

		rasterCts?.Cancel ();
		var cts = new CancellationTokenSource ();
		rasterCts = cts;

		// UI-thread snapshot (cheap): the line model and the theme base colors.
		// The SLOW part (per-line highlighting) happens below, on the task.
		string [] lines = ed.MinimapLinesSnapshot ();
		string path = ed.FilePath;
		SKColor baseFg = ed.MinimapBaseForeground;
		SKColor baseBg = ed.MinimapBaseBackground;
		bool light = ed.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light;
		int lineCount = lines.Length;
		_ = Task.Run (async () => {
			// Coalesce edit bursts: a keystroke cancels the pending work via the
			// CTS; the delay only elapses when the input goes quiet.
			try {
				await Task.Delay (15, cts.Token);
			} catch (OperationCanceledException) {
				return;
			}
			WriteableBitmap? frame = null;
			try {
				frame = RenderRaster (cts.Token, lines, path, baseFg, baseBg, light, w, h);
			} catch (OperationCanceledException) {
				return;
			} catch (Exception ex) {
				Console.WriteLine ("[minimap] raster failed: " + ex.Message);
				return;
			}
			if (cts.IsCancellationRequested) {
				frame?.Dispose ();
				return;
			}
			Dispatcher.UIThread.Post (() => {
				if (cts.IsCancellationRequested)
					return;
				var old = raster;
				raster = frame;
				rasterW = w;
				rasterH = h;
				rasterLines = lineCount;
				rasterDirty = false;
				QaRasterFrames++;
				Console.WriteLine ("[minimap] raster done lines=" + lineCount + " " + w + "x" + h + " frame=" + QaRasterFrames);
				pendingDispose?.Dispose ();
				pendingDispose = old;
				InvalidateVisual ();
			});
		});
	}

	/// <summary>Computes the scaled colored document raster on the calling
	/// (background) thread: one throwaway highlighter over the line snapshot
	/// (the editor's own highlighter is UI-thread state and must not be
	/// touched concurrently), then Skia straight into the WriteableBitmap.</summary>
	static WriteableBitmap RenderRaster (
		CancellationToken cts, string [] lines, string path,
		SKColor baseFg, SKColor baseBg, bool light, int w, int h)
	{
		int lineCount = lines.Length;
		var segments = new IReadOnlyList<(int off, int len, SKColor color)>? [lineCount];
		var def = SyntaxHighlightingService.GetSyntaxHighlightingDefinition (new FilePath (path));
		var hl = def is null ? null : new SyntaxHighlighting (def, new EditorTextDocumentAdapter (lines, path));
		var theme = SyntaxHighlightingService.GetEditorTheme (light ? SyntaxHighlightingService.LightName : SyntaxHighlightingService.DarkName);
		try {
			for (int i = 0; i < lineCount; i++) {
				cts.ThrowIfCancellationRequested ();
				HighlightedLine? hlLine = null;
				if (hl is not null) {
					try {
						hlLine = hl.GetHighlightedLineAsync (hl.Document.GetLine (i + 1), cts).GetAwaiter ().GetResult ();
					} catch (OperationCanceledException) {
						throw;
					} catch {
						hlLine = null; // a broken line falls back to the base color
					}
				}
				segments [i] = MapSegments (hlLine, lines [i], baseFg, theme);
			}
		} finally {
			hl?.Dispose ();
		}

		var frame = new WriteableBitmap (new PixelSize (w, h), new Vector (96, 96), PixelFormats.Bgra8888, AlphaFormat.Opaque);
		using (var fb = frame.Lock ()) {
			var info = new SKImageInfo (w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
			using var surface = SKSurface.Create (info, fb.Address, fb.RowBytes);
			if (surface is not null)
				DrawRaster (surface.Canvas, w, h, lines, segments, baseFg, baseBg);
		}
		return frame;
	}

	/// <summary>Maps a HighlightedLine to (offset, length, color) segments — the
	/// same scope resolution SkTextEditor.ToSegments uses (the palette's
	/// "foreground" of each scope, base color when the scope is unknown).
	/// Returns null for an empty/uncolored line (whole line in base foreground).</summary>
	static IReadOnlyList<(int off, int len, SKColor color)>? MapSegments (
		HighlightedLine? hl, string text, SKColor baseFg, EditorTheme? theme)
	{
		if (hl is null || text.Length == 0)
			return null;
		var list = new List<(int, int, SKColor)> (hl.Segments.Count);
		foreach (var seg in hl.Segments) {
			int off = Math.Max (0, seg.Offset);
			int len = Math.Max (0, seg.Length);
			int end = Math.Min (text.Length, off + len);
			if (off >= text.Length || off >= end)
				continue;
			var color = baseFg;
			var scope = seg.ColorStyleKey ?? "";
			if (scope.Length > 0 && theme is not null && theme.TryGetColor (scope, "foreground", out var c)) {
				var (r, g, b, a) = c.ToRgba ();
				color = new SKColor (r, g, b, a);
			}
			list.Add ((off, end - off, color));
		}
		return list.Count > 0 ? list : null;
	}

	/// <summary>Draws the scaled document: per line either a one-pixel
	/// density bar in its dominant color (long documents: the line is
	/// sub-pixel) or the actual text in its syntax colors (short documents),
	/// clipped to the line band.</summary>
	static void DrawRaster (SKCanvas canvas, int w, int h, string [] lines,
		IReadOnlyList<(int off, int len, SKColor color)>? [] segments, SKColor baseFg, SKColor baseBg)
	{
		int lineCount = lines.Length;
		canvas.Clear (baseBg);
		if (lineCount == 0)
			return;
		float lineH = (float)h / lineCount;
		float textPx = Math.Clamp (lineH * 0.8f, 4f, 12f);
		float charW = textPx * 0.6f;
		using var font = new SKFont (GetMinimapTypeface (), textPx);
		using var paint = new SKPaint { IsAntialias = true };
		for (int i = 0; i < lineCount; i++) {
			float y0 = i * lineH;
			if (lineH < 1.5f) {
				// Density bar: the line's most prominent segment color.
				paint.Color = DominantColor (segments [i], baseFg);
				canvas.DrawRect (2, y0, (float)(w - 4), Math.Max (1f, lineH), paint);
				continue;
			}
			var text = lines [i];
			float baseline = y0 + lineH * 0.72f;
			if (string.IsNullOrEmpty (text))
				continue;
			var segs = segments [i];
			if (segs is null) {
				paint.Color = baseFg;
				canvas.DrawText (text, 2, baseline, font, paint);
				continue;
			}
			foreach (var s in segs) {
				if (s.len <= 0)
					continue;
				paint.Color = s.color;
				canvas.DrawText (text.Substring (s.off, s.len), 2 + s.off * charW, baseline, font, paint);
			}
		}
	}

	static SKColor DominantColor (IReadOnlyList<(int off, int len, SKColor color)>? segs, SKColor baseFg)
	{
		if (segs is null)
			return baseFg;
		int bestLen = -1;
		var best = baseFg;
		foreach (var s in segs) {
			if (s.len > bestLen) {
				bestLen = s.len;
				best = s.color;
			}
		}
		return best;
	}

	static SKTypeface? typeface;
	static readonly object typefaceLock = new ();

	static SKTypeface GetMinimapTypeface ()
	{
		lock (typefaceLock) {
			if (typeface is not null)
				return typeface;
			try {
				using var t = SKTypeface.FromFamilyName ("monospace", SKFontStyle.Normal);
				typeface = (t is not null && t.CountGlyphs ("ABCdef123") > 0) ? t : null;
			} catch {
				typeface = null;
			}
			typeface ??= SKTypeface.Default;
			return typeface;
		}
	}

	// ----- Per-frame overlay (cheap): raster + viewport + caret + dots -----

	public override void Render (DrawingContext context)
	{
		var w = Bounds.Width;
		var h = Bounds.Height;
		if (w < 2 || h < 2)
			return;
		var ed = editor;
		if (raster is null) {
			// Pre-raster placeholder: the editor's surface background.
			var ph = ed is not null && ed.MinimapBaseBackground.Alpha == 0xFF
				? ToAvalonia (ed.MinimapBaseBackground)
				: (ed is not null && ed.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light
					? Colors.White : Color.FromRgb (0x1E, 0x1E, 0x1E));
			context.DrawRectangle (new SolidColorBrush (ph), null, new Rect (0, 0, w, h));
			return;
		}
		context.DrawImage (raster, new Rect (0, 0, w, h));
		if (ed is null)
			return;
		int lineCount = ed.MinimapLineCount;

		// Viewport indicator: the document region the editor currently shows.
		if (scrollOwner is not null) {
			double extent = ed.MinimapScrollExtent;
			if (extent > 0 && lineCount > 0) {
				double off = scrollOwner.Offset.Y;
				double vp = Math.Max (1, scrollOwner.Viewport.Height);
				double y0 = Math.Clamp (off / extent, 0, 1) * h;
				double y1 = Math.Clamp ((off + vp) / extent, 0, 1) * h;
				if (y1 - y0 < h - 1) {
					if (y0 > 0.5)
						context.DrawRectangle (dimBrush, null, new Rect (0, 0, w, y0));
					if (h - y1 > 0.5)
						context.DrawRectangle (dimBrush, null, new Rect (0, y1, w, h - y1));
					context.DrawRectangle (null, viewportPen, new Rect (1, y0 + 0.5, w - 2, Math.Max (2, y1 - y0) - 1));
				}
			}
		}

		if (lineCount == 0)
			return;
		// Caret row.
		if (ed.CurrentLine >= 0 && ed.CurrentLine < lineCount) {
			double yc = (ed.CurrentLine + 0.5) / lineCount * h;
			context.DrawRectangle (caretBrush, null, new Rect (w * 0.18, yc - 1, w * 0.64, 2));
		}
		// Breakpoint dots (left margin, like the editor gutter).
		foreach (var kv in ed.BreakpointLines) {
			if (kv.Key < 0 || kv.Key >= lineCount)
				continue;
			double yb = (kv.Key + 0.5) / lineCount * h;
			context.DrawEllipse (kv.Value ? breakpointBrush : breakpointDisabledBrush, null, new Point (9, yb), 2.5, 2.5);
		}
		// Error markers (the diagnostic bubbles that are errors, right margin —
		// the editor shows them at the end of the line).
		var errors = ed.MinimapErrorLines;
		foreach (var kv in errors) {
			if (!kv.Value || kv.Key < 0 || kv.Key >= lineCount)
				continue;
			double ye = (kv.Key + 0.5) / lineCount * h;
			context.DrawEllipse (errorBrush, null, new Point (w - 9, ye), 2, 2);
		}
	}

	static Color ToAvalonia (SKColor c) => Color.FromArgb (c.Alpha, c.Red, c.Green, c.Blue);
}
