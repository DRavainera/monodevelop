
using System;
using System.Reflection;
using System.Collections;

namespace Stetic.Editor
{
	public class IconSelectorItem: Gtk.EventBox
	{
		ArrayList icons = new ArrayList ();
		ArrayList labels = new ArrayList ();
		ArrayList names = new ArrayList ();
		int columns = 12;
		int iconSize = 16;
		int spacing = 3;
		int selIndex = -1;
		int sectionGap = 10;
		int lastSel = -1;
		int xmax;
		int ymax;
		string title;
		Gtk.Window tipWindow;
		bool inited;
		
		public IconSelectorItem (IntPtr ptr): base (ptr)
		{
		}
		
		public IconSelectorItem (string title)
		{
			this.title = title;
			
			int w, h;
			Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h);
			iconSize = w;
			
			this.Events |= Gdk.EventMask.PointerMotionMask;
		}
		
		protected override void OnGetPreferredWidth (out int minimum, out int natural)
		{
			if (!inited) {
				CreateIcons ();
				inited = true;
			}
			
			base.OnGetPreferredWidth (out minimum, out natural);
			CalcSize ();
			
			minimum = xmax;
			natural = xmax;
		}
		
		protected override void OnGetPreferredHeight (out int minimum, out int natural)
		{
			if (!inited) {
				CreateIcons ();
				inited = true;
			}
			
			base.OnGetPreferredHeight (out minimum, out natural);
			
			minimum = ymax;
			natural = ymax;
		}
		
		protected virtual void CreateIcons ()
		{
		}
		
		public int SelectedIndex {
			get { return selIndex; }
		}
		
		public string SelectedIcon {
			get {
				if (selIndex != -1)
					return (string) names [selIndex];
				else
					return null;
			}
		}
		
		protected void AddIcon (string name, Gdk.Pixbuf pix, string label)
		{
			icons.Add (pix);
			labels.Add (label);
			names.Add (name);
		}
		
		protected void AddSeparator (string separator)
		{
			icons.Add (null);
			labels.Add (null);
			names.Add (separator);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion ev)
		{
			ProcessMotionEvent ((int) ev.X, (int) ev.Y);
			return true;
		}
		
		internal void ProcessMotionEvent (int x, int y)
		{
			int ix, iy;
			GetIconIndex (x, y, out selIndex, out ix, out iy);
			if (selIndex != -1) {
				string name = labels [selIndex] as string;
				if (name == null || name.Length == 0)
					name = names [selIndex] as string;
				if (selIndex != lastSel) {
					HideTip ();
					ShowTip (ix, iy + iconSize + spacing*2, name);
				}
			} else
				HideTip ();
				
			lastSel = selIndex;
			
			QueueDraw ();
		}
		
		void ShowTip (int x, int y, string text)
		{
			if (GdkWindow == null)
				return;
			if (tipWindow == null) {
				tipWindow = new TipWindow ();
				Gtk.Label lab = new Gtk.Label (text);
				lab.Xalign = 0;
				lab.Xpad = 3;
				lab.Ypad = 3;
				tipWindow.Add (lab);
			}
			((Gtk.Label)tipWindow.Child).Text = text;
			int w = tipWindow.Child.SizeRequest().Width;
			int ox, oy;
			GdkWindow.GetOrigin (out ox, out oy);
			tipWindow.Move (ox + x - (w/2) + (iconSize/2), oy + y);
			tipWindow.ShowAll ();
		}
		
		void HideTip ()
		{
			if (tipWindow != null) {
				tipWindow.Destroy ();
				tipWindow = null;
			}
		}
		
		protected override void Dispose (bool disposing)
		{
			if (disposing)
				HideTip ();
			base.Dispose (disposing);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			HideTip ();
			return base.OnLeaveNotifyEvent (ev);
		}
		
		internal void ProcessLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			HideTip ();
		}
		
		protected override bool OnDrawn (Cairo.Context cr)
		{
			Draw (cr);
			return base.OnDrawn (cr);
		}
		
		void Draw (Cairo.Context cr)
		{
			int a,b;
			Expose  (true, -1, -1, cr, out a, out b);
		}
		
		void CalcSize ()
		{
			int a,b;
			Expose (false, -1, -1, null, out a, out b);
		}
		
		void GetIconIndex (int x, int y, out int index, out int ix, out int iy)
		{
			index = Expose (false, x, y, null, out ix, out iy);
		}
		
		int Expose (bool draw, int testx, int testy, Cairo.Context cr, out int ix, out int iy)
		{
			int x = spacing;
			int y = spacing;
			int sx = spacing;
			int maxx = columns * (iconSize + spacing) + spacing;
			bool calcSize = (testx == -1);
			int w, h, tborder = 1;
			using (var layout = new Pango.Layout (this.PangoContext)) { 
				Pango.FontDescription des = this.PangoContext.FontDescription.Copy ();
				des.Size = 10 * (int)Pango.Scale.PangoScale;
				layout.FontDescription = des;
				layout.SetMarkup (title);
				layout.Width = -1;
				layout.GetPixelSize (out w, out h);
				if (cr != null) {
					Gdk.RGBA dark = StyleContext.GetColor (Gtk.StateFlags.Normal);
					cr.SetSourceColor (new Cairo.Color ((float) dark.Red, (float) dark.Green, (float) dark.Blue));
					cr.Rectangle (x, y, Allocation.Width + tborder * 2, h + tborder * 2);
					cr.Fill ();
					Gdk.RGBA fg = StyleContext.GetColor (Gtk.StateFlags.Normal);
					cr.SetSourceColor (new Cairo.Color ((float) fg.Red, (float) fg.Green, (float) fg.Blue));
					cr.MoveTo (x + tborder + 2, y + tborder);
					Pango.CairoHelper.ShowLayout (cr, layout);
				}
			}
			
			if (calcSize)
				xmax = 0;

			y += h + spacing*2 + tborder*2;
			
			for (int n=0; n<icons.Count; n++) {
				string cmd = names [n] as string;
				Gdk.Pixbuf pix = icons [n] as Gdk.Pixbuf;
				
				if (cmd == "-") {
					if (x > sx) {
						y += iconSize + spacing;
					}
					x = sx;
					y -= spacing;
					if (cr != null) {
						Gdk.RGBA c = StyleContext.GetColor (Gtk.StateFlags.Normal);
						cr.SetSourceColor (new Cairo.Color ((float) c.Red, (float) c.Green, (float) c.Blue));
						cr.LineWidth = 1;
						cr.MoveTo (x + 0.5, y + (sectionGap / 2) + 0.5);
						cr.LineTo (Allocation.Width + 0.5, y + (sectionGap / 2) + 0.5);
						cr.Stroke ();
					}
					y += sectionGap;
					continue;
				}
				
				if (cmd == "|") {
					if (x == sx)
						continue;
					x += spacing;
					if (cr != null) {
						Gdk.RGBA c = StyleContext.GetColor (Gtk.StateFlags.Normal);
						cr.SetSourceColor (new Cairo.Color ((float) c.Red, (float) c.Green, (float) c.Blue));
						cr.LineWidth = 1;
						cr.MoveTo (x + 0.5, y);
						cr.LineTo (x + 0.5, y + iconSize);
						cr.Stroke ();
					}
					x += spacing*2;
					continue;
				}
				
				if (testx != -1 && testx >= (x - spacing/2) && testx < (x + iconSize + spacing) && testy >= (y - spacing/2) && testy < (y + iconSize + spacing)) {
					ix = x;
					iy = y;
					return n;
				}
					
				if (cr != null) {
					if (n == selIndex) {
						Gdk.RGBA selbg = StyleContext.GetBackgroundColor (Gtk.StateFlags.Selected);
						cr.SetSourceColor (new Cairo.Color ((float) selbg.Red, (float) selbg.Green, (float) selbg.Blue));
						cr.Rectangle (x - spacing, y - spacing, iconSize + spacing*2, iconSize + spacing*2);
						cr.Fill ();
					}
					DrawPixbuf (cr, pix, x, y);
				}
				
				x += (iconSize + spacing);
				if (calcSize && x > xmax)
					xmax = x;
					
				if (x >= maxx) {
					x = sx;
					y += iconSize + spacing;
				}
			}
			if (calcSize) {
				if (x > sx)
					y += iconSize + spacing;
				ymax = y;
			}
			
			ix = iy = 0;
			return -1;
		}
		
		internal static void DrawPixbuf (Cairo.Context cr, Gdk.Pixbuf pix, int dx, int dy)
		{
			if (pix == null || pix.Width <= 0 || pix.Height <= 0)
				return;
			
			int stride = pix.Rowstride;
			byte[] data = new byte [stride * pix.Height];
			System.Runtime.InteropServices.Marshal.Copy (pix.Pixels, data, 0, data.Length);
			
			Cairo.Format fmt = pix.HasAlpha ? Cairo.Format.Argb32 : Cairo.Format.Rgb24;
			using (var surface = new Cairo.ImageSurface (data, fmt, pix.Width, pix.Height, stride)) {
				cr.Save ();
				cr.Translate (dx, dy);
				cr.SetSource (surface);
				cr.Paint ();
				cr.Restore ();
			}
		}
	}
	
	class TipWindow: Gtk.Window
	{
		public TipWindow (): base (Gtk.WindowType.Popup)
		{
		}
		
		protected override bool OnDrawn (Cairo.Context cr)
		{
			bool r = base.OnDrawn (cr);
			
			int w = Allocation.Width;
			int h = Allocation.Height;
			
			Gdk.RGBA bg = StyleContext.GetBackgroundColor (Gtk.StateFlags.Normal);
			cr.SetSourceColor (new Cairo.Color ((float) bg.Red, (float) bg.Green, (float) bg.Blue));
			cr.Rectangle (0, 0, w, h);
			cr.Fill ();
			
			Gdk.RGBA fg = StyleContext.GetColor (Gtk.StateFlags.Normal);
			cr.SetSourceColor (new Cairo.Color ((float) fg.Red, (float) fg.Green, (float) fg.Blue));
			cr.LineWidth = 1;
			cr.Rectangle (0.5, 0.5, w - 1, h - 1);
			cr.Stroke ();
			
			return r;
		}
	}
}
