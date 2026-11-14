using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace MonoDevelop.Ide.Controls;

/// <summary>
/// The draggable edge between two dock regions of the workbench (legacy
/// DockFrame splitter). A GridSplitter here silently ignored real pointer drags,
/// so the thumb adjusts the target size directly: press, drag, the pad follows.
/// The pointer position is measured against the parent Grid — a FIXED frame: the
/// splitter itself moves while the pad resizes, and self-relative deltas fed the
/// pad its own movement back (a runaway that slammed the pad to the minimum).
///
/// Two orientations:
///  - VERTICAL (TargetRow set): the editor &harr; bottom-pad edge. Dragging up/down
///    resizes the target ROW's height. Cursor: BottomSide.
///  - HORIZONTAL (TargetControl set): the editor &harr; side-pad edge. Dragging
///    left/right resizes the target PAD's Width. Cursor: SizeWestEast. The side
///    pad sits in an Auto column that follows its Width, so resizing the Width
///    resizes the column; the collapse mechanism (which also drives the Width)
///    is left untouched and keeps the user's resized width across collapse.
///
/// Transparent 6px hit strip with a subtle highlight (hover) and a grab cursor;
/// the pads draw their own chrome line at the boundary.
///
/// Alignment contract: the hit strip is fixed in size on its DRAG axis
/// (Height=6 for the bottom/row edge, Width=6 for the side/column edge) and
/// stretches on the cross axis (the default). A fixed-size strip must also be
/// pinned on the drag axis explicitly, because left unaligned it CENTERS in
/// its cell and misses the boundary it is meant to sit on. The shell therefore
/// sets, in XAML: VerticalAlignment=Top on the bottom splitter (the 6px strip
/// at row 1's top = the editor/bottom boundary, not centered in the row) and
/// HorizontalAlignment=Left/Right on the side splitters (each 6px strip at its
/// pad's edge in the editor column). Both alignments are load-bearing: dropping
/// either one silently re-centers that edge's hit strip away from the boundary.
/// </summary>
public class PadSplitter : Border
{
	public static readonly StyledProperty<RowDefinition?> TargetRowProperty =
		AvaloniaProperty.Register<PadSplitter, RowDefinition?> (nameof (TargetRow));

	public RowDefinition? TargetRow {
		get => GetValue (TargetRowProperty);
		set => SetValue (TargetRowProperty, value);
	}

	// Horizontal mode: the pad control whose Width this edge resizes (a side pad
	// in an Auto column). Setting it also selects the horizontal cursor — the
	// orientation is only known once the shell assigns the target.
	Control? targetControl;
	public Control? TargetControl {
		get => targetControl;
		set {
			if (targetControl == value)
				return;
			targetControl = value;
			if (value is not null)
				Cursor = new Cursor (StandardCursorType.SizeWestEast);
		}
	}

	// Horizontal mode: +1 grows the pad when dragging RIGHT (left pad — the pad is
	// to the LEFT of the edge); -1 grows it when dragging LEFT (right pad — the
	// pad is to the RIGHT of the edge).
	public double HorizontalGrowSign { get; set; } = 1;

	// Horizontal mode: the OPPOSITE side pad. Its width is subtracted from the
	// max so the editor (the "*" column between the two pads) keeps ContentMinSize
	// even when both side pads are wide — otherwise the "*" column could go to 0.
	public Control? OppositeControl { get; set; }

	/// <summary>Raised on pointer press before the drag baseline is captured, so
	/// the shell can expand a collapsed pad first (the drag then grows the pad
	/// from the rail instead of fighting the collapse).</summary>
	public event Action? DragStarted;

	/// <summary>Raised once per completed drag (pointer released or capture
	/// lost) so the shell can persist the new size immediately — the layout
	/// must survive a non-graceful exit, and the on-disk record is only
	/// trustworthy if it is written when the size changes, not at shutdown.</summary>
	public event Action? DragEnded;

	const double MinSize = 34;         // collapsed-rail minimum (PadHost chrome)
	const double ContentMinSize = 150; // keep the editor usable on the other side

	// The thumb is a transparent hit strip; a subtle highlight makes the grab
	// edge discoverable (hover) and confirms the grab (drag).
	static readonly IBrush hoverBrush = new SolidColorBrush (Color.FromArgb (50, 170, 170, 170));
	static readonly IBrush dragBrush = new SolidColorBrush (Color.FromArgb (100, 170, 170, 170));

	double dragStartPos, dragStartSize, dragMaxSize;
	Visual? dragFrame;
	bool dragging;

	bool IsHorizontal => TargetControl is not null;

	public PadSplitter ()
	{
		Background = Brushes.Transparent; // hit-test only
		// Default to the vertical (editor/bottom) cursor; assigning TargetControl
		// flips it to the horizontal (side-pad) cursor.
		Cursor = new Cursor (StandardCursorType.BottomSide);
	}

	protected override void OnPointerPressed (PointerPressedEventArgs e)
	{
		base.OnPointerPressed (e);
		if (!e.GetCurrentPoint (this).Properties.IsLeftButtonPressed)
			return;
		if (IsHorizontal ? TargetControl is null : TargetRow is null)
			return;
		dragging = true;
		dragFrame = this.FindAncestorOfType<Grid> () ?? (Visual)this;
		Background = dragBrush;
		var p = e.GetPosition (dragFrame);
		dragStartPos = IsHorizontal ? p.X : p.Y;
		// Subscribers may resize the target (a collapsed pad expands before the
		// drag) — run them first and then read the SET size: ActualWidth/Height
		// lag the layout pass and would start the drag stale.
		DragStarted?.Invoke ();
		if (IsHorizontal) {
			var tw = TargetControl!.Width;
			// Width is normally set by XAML/restore; fall back to the laid-out
			// bounds if it was ever left NaN (e.g. a pad whose Width was cleared).
			dragStartSize = double.IsNaN (tw) ? TargetControl.Bounds.Width : tw;
			// Leave room for the editor ("*" column) and the opposite side pad.
			var ow = OppositeControl is { } opp ? opp.Width : double.NaN;
			var opposite = double.IsNaN (ow) ? (OppositeControl is { } o2 ? o2.Bounds.Width : 0) : ow;
			dragMaxSize = Math.Max (MinSize, dragFrame.Bounds.Width - opposite - ContentMinSize);
		} else {
			dragStartSize = TargetRow!.Height.IsAbsolute ? TargetRow.Height.Value : TargetRow.ActualHeight;
			dragMaxSize = Math.Max (MinSize, dragFrame.Bounds.Height - ContentMinSize);
		}
		e.Pointer.Capture (this);
		e.Handled = true;
	}

	protected override void OnPointerMoved (PointerEventArgs e)
	{
		base.OnPointerMoved (e);
		if (!dragging || dragFrame is null)
			return;
		var p = e.GetPosition (dragFrame);
		if (IsHorizontal) {
			// The boundary follows the pointer along X; the sign selects which
			// direction grows the pad (left pad: +, right pad: -).
			var dx = p.X - dragStartPos;
			var w = Math.Clamp (dragStartSize + dx * HorizontalGrowSign, MinSize, dragMaxSize);
			TargetControl!.Width = w;
		} else {
			// The boundary follows the pointer along Y: up grows the bottom pad,
			// down shrinks it.
			var dy = p.Y - dragStartPos;
			var h = Math.Clamp (dragStartSize - dy, MinSize, dragMaxSize);
			TargetRow!.Height = new GridLength (h);
		}
		e.Handled = true;
	}

	protected override void OnPointerEntered (PointerEventArgs e)
	{
		base.OnPointerEntered (e);
		if (!dragging)
			Background = hoverBrush;
	}

	protected override void OnPointerExited (PointerEventArgs e)
	{
		base.OnPointerExited (e);
		if (!dragging)
			Background = Brushes.Transparent;
	}

	protected override void OnPointerReleased (PointerReleasedEventArgs e)
	{
		base.OnPointerReleased (e);
		if (dragging) {
			dragging = false;
			Background = IsPointerOver ? hoverBrush : Brushes.Transparent;
			e.Pointer.Capture (null);
			e.Handled = true;
			DragEnded?.Invoke ();
		}
	}

	protected override void OnPointerCaptureLost (PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost (e);
		if (dragging)
			DragEnded?.Invoke ();
		dragging = false;
	}
}
