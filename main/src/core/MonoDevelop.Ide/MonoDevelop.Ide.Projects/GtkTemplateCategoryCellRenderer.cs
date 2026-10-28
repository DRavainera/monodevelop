//
// GtkTemplateCategoryCellRenderer.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	class GtkTemplateCategoryCellRenderer : CellRendererText
	{
		public TemplateCategory Category { get; set; }
		public string CategoryName { get; set; }
		public Xwt.Drawing.Image CategoryIcon { get; set; }
		public int CategoryIconWidth { get; set; }

		const int topLevelTemplateHeadingTotalYPadding = 23;
		const int topLevelTemplateHeadingYOffset = 4;
		const int topLevelIconTextXPadding = 6;
		const int iconTextXPadding = 1;
		const int iconYOffset = -1;

		protected override void OnGetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.OnGetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
			if (CategoryIcon != null) {
				height = (int)CategoryIcon.Height + ((int)Ypad * 2) + topLevelTemplateHeadingTotalYPadding;
			}
		}

		protected override void OnRender (Cairo.Context cr, Widget widget, Rectangle background_area, Rectangle cell_area, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);
			var isSelected = state == StateType.Selected || state == StateType.Active;
			int iconTextPadding = iconTextXPadding;
			int textYOffset = 0;
			Rectangle iconRect = GetIconRect (cell_area);

			{
				if (CategoryIcon != null) {
					iconRect = DrawIcon (cr, widget, cell_area, flags);
					iconTextPadding = topLevelIconTextXPadding;
					textYOffset = (Category == null ? 0 : topLevelTemplateHeadingYOffset);
				}

				DrawTemplateCategoryText (cr, widget, cell_area, iconRect, iconTextPadding, textYOffset, flags);
				if (Category == null && !isSelected) {
						cr.MoveTo (cell_area.X + (int)Xpad, cell_area.Y + cell_area.Height + 1);
						cr.SetSourceColor (Gui.Styles.ThinSplitterColor.ToCairoColor ());
						cr.LineWidth = 1;
						cr.LineTo (cell_area.X + cell_area.Width - (int)Xpad, cell_area.Y + cell_area.Height + 1);
						cr.Stroke ();
				}
			}
		}

		Rectangle GetIconRect (Rectangle cell_area)
		{
			return new Rectangle (cell_area.X + (int)Xpad, cell_area.Y + (int)Ypad + iconYOffset, CategoryIconWidth, CategoryIconWidth);
		}

		static StateType GetState (Widget widget, CellRendererState flags)
		{
			StateType stateType = StateType.Normal;
			if ((flags & CellRendererState.Prelit) != 0)
				stateType = StateType.Prelight;
			if ((flags & CellRendererState.Focused) != 0)
				stateType = StateType.Normal;
			if ((flags & CellRendererState.Insensitive) != 0)
				stateType = StateType.Insensitive;
			if ((flags & CellRendererState.Selected) != 0)
				stateType = widget.HasFocus ? StateType.Selected : StateType.Active;
			return stateType;
		}

		Rectangle DrawIcon (Cairo.Context ctx, Widget widget, Rectangle cell_area, CellRendererState flags)
		{
			int iconY = cell_area.Y + ((cell_area.Height - (int)CategoryIcon.Height) / 2) + (Category == null ? 0 : topLevelTemplateHeadingYOffset);
			var iconRect = new Rectangle (cell_area.X + (int)Xpad, iconY, (int)CategoryIcon.Width, (int)CategoryIcon.Height);

			var img = CategoryIcon;
			if ((flags & Gtk.CellRendererState.Selected) != 0)
				img = img.WithStyles ("sel");
			ctx.DrawImage (widget, img, iconRect.X, iconRect.Y);
			return iconRect;
		}

		void DrawTemplateCategoryText (Cairo.Context cr, Widget widget, Rectangle cell_area, Rectangle iconRect, int iconTextPadding, int textYOffset, CellRendererState flags)
		{
			StateType state = GetState (widget, flags);

			using (var layout = new Pango.Layout (widget.PangoContext)) {

				layout.Ellipsize = Pango.EllipsizeMode.End;
				int textPixelWidth = widget.Allocation.Width - ((int)Xpad * 2) - iconRect.Width - iconTextPadding;
				layout.Width = (int)(textPixelWidth * Pango.Scale.PangoScale);

				layout.SetMarkup (CategoryName);

				int w, h;
				layout.GetPixelSize (out w, out h);
				int textY = cell_area.Y + (cell_area.Height - h) / 2 + textYOffset;

				Gdk.RGBA fg = widget.StyleContext.GetColor (ToStateFlags (state));
				cr.SetSourceColor (new Cairo.Color ((float) fg.Red, (float) fg.Green, (float) fg.Blue));
				cr.MoveTo (iconRect.Right + iconTextPadding, textY);
				Pango.CairoHelper.ShowLayout (cr, layout);
			}
		}

		static Gtk.StateFlags ToStateFlags (StateType state)
		{
			switch (state) {
				case StateType.Selected: return Gtk.StateFlags.Selected;
				case StateType.Active: return Gtk.StateFlags.Active;
				case StateType.Prelight: return Gtk.StateFlags.Prelight;
				case StateType.Insensitive: return Gtk.StateFlags.Insensitive;
				default: return Gtk.StateFlags.Normal;
			}
		}
	}
}

