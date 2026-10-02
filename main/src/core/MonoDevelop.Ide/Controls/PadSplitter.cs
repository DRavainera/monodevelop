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
/// The draggable edge between the editor and the bottom pad (legacy DockFrame
/// splitter). A GridSplitter here silently ignored real pointer drags, so the
/// thumb adjusts the target row's height directly: press, drag, the row follows.
/// The pointer position is measured against the parent Grid — a FIXED frame: the
/// splitter itself moves while the row resizes, and self-relative deltas fed the
/// row its own movement back (a runaway that slammed the pad to the minimum).
/// Transparent 6px hit strip with a bottom-side resize cursor; the pads draw
/// their own chrome line at the boundary.
/// </summary>
public class PadSplitter : Border
{
	public static readonly StyledProperty<RowDefinition?> TargetRowProperty =
		AvaloniaProperty.Register<PadSplitter, RowDefinition?> (nameof (TargetRow));

	public RowDefinition? TargetRow {
		get => GetValue (TargetRowProperty);
		set => SetValue (TargetRowProperty, value);
	}

	/// <summary>Raised on pointer press before the drag baseline is captured, so
	/// the shell can expand a collapsed pad first (the drag then grows the row
	/// from the rail instead of fighting the collapse).</summary>
	public event Action? DragStarted;

	const double MinHeight = 34;  // collapsed-rail minimum (PadHost chrome)
	const double EditorMinHeight = 150; // keep the editor usable at the top

	// The thumb is a transparent hit strip; a subtle highlight makes the grab
	// edge discoverable (hover) and confirms the grab (drag).
	static readonly IBrush hoverBrush = new SolidColorBrush (Color.FromArgb (50, 170, 170, 170));
	static readonly IBrush dragBrush = new SolidColorBrush (Color.FromArgb (100, 170, 170, 170));

	double dragStartY, dragStartHeight, dragMaxHeight;
	Visual? dragFrame;
	bool dragging;

	public PadSplitter ()
	{
		Height = 6;
		VerticalAlignment = VerticalAlignment.Top;
		Cursor = new Cursor (StandardCursorType.BottomSide);
		Background = Brushes.Transparent; // hit-test only
	}

	protected override void OnPointerPressed (PointerPressedEventArgs e)
	{
		base.OnPointerPressed (e);
		if (!e.GetCurrentPoint (this).Properties.IsLeftButtonPressed || TargetRow is null)
			return;
		dragging = true;
		dragFrame = this.FindAncestorOfType<Grid> () ?? (Visual)this;
		Background = dragBrush;
		dragStartY = e.GetPosition (dragFrame).Y;
		// Subscribers may resize the target row (a collapsed bottom pad expands
		// before the drag) — run them first and then read the row's SET height:
		// ActualHeight lags the layout pass and would start the drag stale.
		DragStarted?.Invoke ();
		dragStartHeight = TargetRow.Height.IsAbsolute ? TargetRow.Height.Value : TargetRow.ActualHeight;
		dragMaxHeight = Math.Max (MinHeight, dragFrame.Bounds.Height - EditorMinHeight);
		e.Pointer.Capture (this);
		e.Handled = true;
	}

	protected override void OnPointerMoved (PointerEventArgs e)
	{
		base.OnPointerMoved (e);
		if (!dragging || TargetRow is null || dragFrame is null)
			return;
		// The boundary follows the pointer: up grows the bottom pad, down shrinks it.
		var dy = e.GetPosition (dragFrame).Y - dragStartY;
		var h = Math.Clamp (dragStartHeight - dy, MinHeight, dragMaxHeight);
		TargetRow.Height = new GridLength (h);
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
		}
	}

	protected override void OnPointerCaptureLost (PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost (e);
		dragging = false;
	}
}
