using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering;

namespace MonoDevelop.AvaloniaShell.Controls;

/// <summary>
/// Border whose ENTIRE bounds participate in hit-testing, independent of the
/// drawn content.
///
/// Avalonia 12 hit-tests real pointer input against the composition render
/// tree: a glyph run is always hit-testable, and a drawn rectangle only counts
/// as a hit if it is registered in the render data stream. A templated Button's
/// Background fill is not registered as a topmost hit there, so clicks on the
/// button's margins fall through to whatever sits behind it and ONLY the glyph
/// itself responds. ICustomHitTest is consulted BEFORE the render data stream
/// (CompositionDrawListVisual.HitTest), so declaring the bounds ourselves makes
/// the whole cell the click target — what the tab-strip buttons (‹ › ▾) need.
/// </summary>
public class HitTestableBorder : Border, ICustomHitTest
{
	public bool HitTest (Point point) => Bounds.Contains (point);
}
