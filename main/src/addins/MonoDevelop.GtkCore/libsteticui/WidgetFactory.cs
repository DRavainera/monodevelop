using Gtk;
using Cairo;
using Gdk;
using System;
using System.Collections;
using System.Reflection;

namespace Stetic {

	// This is the base class for palette items. Implements the basic
	// functionality for showing the icon and label of the item.
	
	internal class PaletteItemFactory : EventBox 
	{
		public PaletteItemFactory ()
		{
		}
		
		public virtual void Initialize (string name, Gdk.Pixbuf icon)
		{
			DND.SourceSet (this);
			AboveChild = true;

			Gtk.HBox hbox = new HBox (false, 6);
			hbox.BorderWidth = 3;

			if (icon != null) {
				icon = icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear);
				hbox.PackStart (new Gtk.Image (icon), false, false, 0);
			}

			Gtk.Label label = new Gtk.Label ("<small>" + name + "</small>");
			label.UseMarkup = true;
			label.Justify = Justification.Left;
			label.Xalign = 0;
			hbox.PackEnd (label, true, true, 0);

			Add (hbox);
		}

		protected override void OnDragBegin (Gdk.DragContext ctx)
		{
			Gtk.Widget ob = CreateItemWidget ();
			if (ob != null)
				DND.Drag (this, ctx, ob);
		}
		
		protected virtual Gtk.Widget CreateItemWidget ()
		{
			return null;
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing ev)
		{
			this.StyleContext.State = Gtk.StateFlags.Prelight;
			return base.OnEnterNotifyEvent (ev);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			this.StyleContext.State = Gtk.StateFlags.Normal;
			return base.OnLeaveNotifyEvent (ev);
		}
		
		protected override bool OnDrawn (Cairo.Context cr)
		{
			bool r = base.OnDrawn (cr);
			if (State == Gtk.StateType.Prelight) {
				int w = Allocation.Width;
				int h = Allocation.Height;
				cr.LineWidth = 1;
				cr.SetSourceColor (new Cairo.Color (1, 1, 1));
				cr.MoveTo (0.5, 0.5);
				cr.LineTo (w - 1, 0.5);
				cr.LineTo (w - 1, h - 1);
				cr.Stroke ();
				cr.SetSourceColor (new Cairo.Color (0, 0, 0));
				cr.MoveTo (0.5, h - 1);
				cr.LineTo (w - 1, h - 1);
				cr.LineTo (0.5, 0.5);
				cr.Stroke ();
			}
			return r;
		}
	}


	// Palette item factory which creates a widget.
	internal class WidgetFactory : PaletteItemFactory {

		protected ProjectBackend project;
		protected ClassDescriptor klass;

		public WidgetFactory (ProjectBackend project, ClassDescriptor klass)
		{
			this.project = project;
			this.klass = klass;
			Initialize (klass.Label, klass.Icon);
			if (project == null)
				Sensitive = false;
		}
		
		protected override Gtk.Widget CreateItemWidget ()
		{
			return klass.NewInstance (project) as Widget;
		}
	}

	internal class WindowFactory : WidgetFactory
	{
		public WindowFactory (ProjectBackend project, ClassDescriptor klass) : base (project, klass) 
		{
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evt)
		{
			Gtk.Window win = klass.NewInstance (project) as Gtk.Window;
			project.AddWindow (win, true);
			return true;
		}

		public override void Initialize (string name, Gdk.Pixbuf icon)
		{
			base.Initialize (name, icon);
			DND.SourceUnset (this);
		}
	}
	
	// Palette item factory which allows dragging an already existing object.
	internal class InstanceWidgetFactory : PaletteItemFactory 
	{
		Gtk.Widget instance;
		
		public InstanceWidgetFactory (string name, Gdk.Pixbuf icon, Gtk.Widget instance)
		{
			this.instance = instance;
			Initialize (name, icon);
		}

		protected override Gtk.Widget CreateItemWidget ()
		{
			return instance;
		}
		
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}
	}
}
