using System;

namespace Stetic.Editor {

	public class Color: PropertyEditorCell 
	{
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			width = 16;
			height = 16;
		}
		
		public override void Render (Cairo.Context cr, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			Gdk.Color c = (Gdk.Color) Value;
			cr.SetSourceColor (new Cairo.Color ((float) c.Red / 65535f, (float) c.Green / 65535f, (float) c.Blue / 65535f));
			cr.Rectangle (bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
			cr.Fill ();
			
			Gdk.RGBA border = Container.StyleContext.GetColor (ToStateFlags (state));
			cr.SetSourceColor (new Cairo.Color ((float) border.Red, (float) border.Green, (float) border.Blue));
			cr.LineWidth = 1;
			cr.Rectangle (bounds.X + 0.5, bounds.Y + 0.5, bounds.Width - 2, bounds.Height - 2);
			cr.Stroke ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new ColorEditor ();
		}
	}
	
	[PropertyEditor ("Color", "ColorSet")]
	public class ColorEditor : Gtk.ColorButton, IPropertyEditor
	{
		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(Gdk.Color))
				throw new ApplicationException ("Color editor does not support editing values of type " + descriptor.PropertyType);
		}
		
		public void AttachObject (object obj)
		{
		}
		
		public object Value { 
			get { return Color; } 
			set { Color = (Gdk.Color) value; }
		}
		
		protected override void OnColorSet ()
		{
			base.OnColorSet ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
