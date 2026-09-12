using System;
using System.Collections.ObjectModel;

// Minimal WPF-compatible shims for the VS editor "view layer" code.
// These subsystems (space reservation agents, popup placement) only carry
// geometry metadata; they were provided by WPF assemblies on Windows. The
// shapes are never rendered by WPF on this platform, so the types are reduced
// to their data role.

namespace System.Windows
{
	public struct Rect
	{
		public Rect (double x, double y, double width, double height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public double X { get; set; }
		public double Y { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public double Left {
			get { return X; }
		}

		public double Top {
			get { return Y; }
		}

		public double Right {
			get { return X + Width; }
		}

		public double Bottom {
			get { return Y + Height; }
		}

		public bool IsEmpty {
			get { return Width <= 0 || Height <= 0; }
		}

		public bool IntersectsWith (Rect rect)
		{
			return !(rect.X >= Right || rect.Right <= X || rect.Y >= Bottom || rect.Bottom <= Y);
		}
	}
}

namespace System.Windows.Media
{
	public abstract class Geometry
	{
		public static Geometry Empty {
			get { return new RectangleGeometry (new Rect (0, 0, 0, 0)); }
		}

		public virtual bool IsEmpty ()
		{
			return this is RectangleGeometry r && r.Rect.IsEmpty;
		}

		public virtual Rect Bounds {
			get { return new Rect (0, 0, 0, 0); }
		}
	}

	public class GeometryGroup : Geometry
	{
		public Collection<Geometry> Children { get; } = new Collection<Geometry> ();
	}

	public class RectangleGeometry : Geometry
	{
		public RectangleGeometry ()
		{
		}

		public RectangleGeometry (Rect rect)
		{
			Rect = rect;
		}

		public Rect Rect { get; set; }

		public override bool IsEmpty ()
		{
			return base.IsEmpty ();
		}

		public override Rect Bounds {
			get { return Rect; }
		}
	}
}

namespace System.Windows.Interop
{
}