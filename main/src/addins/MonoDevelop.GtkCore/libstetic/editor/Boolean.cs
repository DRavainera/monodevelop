using System;

namespace Stetic.Editor {

	public class Boolean : PropertyEditorCell 
	{
		static int indicatorSize;
		static int indicatorSpacing;
		
		static Boolean ()
		{
			Gtk.CheckButton cb = new Gtk.CheckButton ();
			indicatorSize = (int) cb.StyleGetProperty ("indicator-size");
			indicatorSpacing = (int) cb.StyleGetProperty ("indicator-spacing");
		}
		
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			width = 20;
			height = 20;
		}
		
		public override void Render (Cairo.Context cr, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			int s = indicatorSize - 1;
			if (s > bounds.Height)
				s = bounds.Height;
			if (s > bounds.Width)
				s = bounds.Width;
			
			int x = bounds.X + indicatorSpacing - 1;
			int y = bounds.Y + (bounds.Height - s) / 2;
			
			// Check (equivalente a PaintCheck)
			Gtk.StateFlags flags = ToStateFlags (state);
			Gdk.RGBA bg = Container.StyleContext.GetBackgroundColor (flags);
			Gdk.RGBA fg = Container.StyleContext.GetColor (flags);
			cr.SetSourceColor (new Cairo.Color ((float) bg.Red, (float) bg.Green, (float) bg.Blue));
			cr.Rectangle (x, y, s, s);
			cr.Fill ();
			cr.SetSourceColor (new Cairo.Color ((float) fg.Red, (float) fg.Green, (float) fg.Blue));
			cr.LineWidth = 1;
			cr.Rectangle (x + 0.5, y + 0.5, s - 1, s - 1);
			cr.Stroke ();
			
			if ((bool) Value) {
				cr.MoveTo (x + s / 3.0, y + s / 2.0);
				cr.LineTo (x + s / 2.0, y + 2 * s / 3.0);
				cr.LineTo (x + 2 * s / 3.0, y + s / 3.0);
				cr.Stroke ();
			}
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new BooleanEditor ();
		}
	}
	
	[PropertyEditor ("Active", "Toggled")]
	public class BooleanEditor : Gtk.CheckButton, IPropertyEditor 
	{
		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(bool))
				throw new ApplicationException ("Boolean editor does not support editing values of type " + descriptor.PropertyType);
		}
		
		public void AttachObject (object obj)
		{
		}
		
		public object Value { 
			get { return Active; } 
			set { Active = (bool) value; }
		}
		
		protected override void OnToggled ()
		{
			base.OnToggled ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
