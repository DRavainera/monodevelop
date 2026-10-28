using System;
using Gtk;

namespace Stetic 
{
	public class Custom : Gtk.DrawingArea 
	{
		public Custom () {}

		public Custom (IntPtr raw) : base (raw) {}

		string creationFunction, string1, string2;
		int int1, int2;

		public string CreationFunction {
			get {
				return creationFunction;
			}
			set {
				creationFunction = value;
			}
		}

		public string LastModificationTime {
			get {
				return null;
			}
			set {
				;
			}
		}

		public string String1 {
			get {
				return string1;
			}
			set {
				string1 = value;
			}
		}

		public string String2 {
			get {
				return string2;
			}
			set {
				string2 = value;
			}
		}

		public int Int1 {
			get {
				return int1;
			}
			set {
				int1 = value;
			}
		}

		public int Int2 {
			get {
				return int2;
			}
			set {
				int2 = value;
			}
		}

		protected override bool OnDrawn (Cairo.Context cr)
		{
			if (!IsDrawable)
				return false;

			int width = GdkWindow.Width;
			int height = GdkWindow.Height;

			// Fondo gris (equivalente visual al XPM de Gtk2)
			cr.SetSourceColor (new Cairo.Color (0.73f, 0.73f, 0.73f));
			cr.Rectangle (0, 0, width, height);
			cr.Fill ();

			cr.SetSourceColor (new Cairo.Color (0.42f, 0.37f, 1.0f));
			cr.Rectangle (width / 2 - 4, height / 2 - 4, 8, 8);
			cr.Fill ();

			// Borde biselado
			cr.LineWidth = 1;
			cr.SetSourceColor (new Cairo.Color (1.0f, 1.0f, 1.0f));
			cr.MoveTo (0.5, 0);
			cr.LineTo (width, 0);
			cr.MoveTo (0, 0.5);
			cr.LineTo (0, height);
			cr.Stroke ();

			cr.SetSourceColor (new Cairo.Color (0.42f, 0.37f, 1.0f));
			cr.MoveTo (0, height - 0.5);
			cr.LineTo (width, height - 0.5);
			cr.MoveTo (width - 0.5, 0);
			cr.LineTo (width - 0.5, height);
			cr.Stroke ();

			return base.OnDrawn (cr);
		}
	}
}
