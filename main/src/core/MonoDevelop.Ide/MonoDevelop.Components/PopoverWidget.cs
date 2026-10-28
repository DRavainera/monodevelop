//
// PopoverWidget.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System;
using Gtk;
using Gdk;
using Xwt.Motion;

namespace MonoDevelop.Components
{
	public class PopoverWidget: Gtk.EventBox, IAnimatable
	{
		PopoverWindowTheme theme;

		PopupPosition position;
		Gtk.Alignment alignment;

		Gdk.Size targetSize;
		Gdk.Size paintSize;

		bool disableSizeCheck;

		const int MinArrowSpacing = 5;

		public PopoverWidget ()
		{
			AppPaintable = true;
			VisibleWindow = false;

			alignment = new Alignment (0, 0, 1f, 1f);
			alignment.Show ();
			Add (alignment);

			disableSizeCheck = false;

			UpdatePadding ();
		}
		
		protected override void OnGetPreferredWidth (out int minimum, out int natural)
		{
			base.OnGetPreferredWidth (out minimum, out natural);
			if (this.AnimationIsRunning ("Resize") && !disableSizeCheck) {
				int w = Math.Max (Allocation.Width, targetSize.Width);
				if (minimum < w) minimum = w;
				if (natural < w) natural = w;
			}
		}
		
		protected override void OnGetPreferredHeight (out int minimum, out int natural)
		{
			base.OnGetPreferredHeight (out minimum, out natural);
			if (this.AnimationIsRunning ("Resize") && !disableSizeCheck) {
				int h = Math.Max (Allocation.Height, targetSize.Height);
				if (minimum < h) minimum = h;
				if (natural < h) natural = h;
			}
		}
		
		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		public bool EnableAnimation { get; set; }

		public PopoverWindowTheme Theme { 
			get { 
				if (theme == null) {
					theme = new PopoverWindowTheme ();
					theme.RedrawNeeded += OnRedrawNeeded;
				}
				return theme; 
			}
			set {
				if (theme == value)
					return;

				theme.RedrawNeeded -= OnRedrawNeeded;
				theme = value;
				theme.RedrawNeeded += OnRedrawNeeded;

				UpdatePadding ();
				QueueDraw ();
			}
		}

		public bool ShowArrow {
			get { return Theme.ShowArrow; }
			set { Theme.ShowArrow = value; }
		}

		public Gtk.Alignment ContentBox {
			get { return alignment; }
		}

		public PopupPosition PopupPosition {
			get { return position; }
			set { Theme.TargetPosition = position = value; }
		}
		
		public void AnimatedResize ()
		{
			if (!EnableAnimation || !GtkUtil.ScreenSupportsARGB ()) {
				QueueResize();
				return;
			}

			disableSizeCheck = true;
			int minw, natw, minh, nath;
			// use GetPreferredWidth/Height instead of SizeRequest to bypass internal GTK caching
			GetPreferredWidth (out minw, out natw);
			GetPreferredHeight (out minh, out nath);
			disableSizeCheck = false;

			Gdk.Size size = new Gdk.Size (natw, nath);

			// ensure that our apint area is big enough for our padding
			if (paintSize.Width <= 15 || paintSize.Height <= 15)
				paintSize = size;

			targetSize = size;
			Gdk.Size start = paintSize;
			Func<double, Gdk.Size> transform = x => new Gdk.Size ((int)(start.Width + (size.Width - start.Width) * x),
			                                                     (int)(start.Height + (size.Height - start.Height) * x));
			this.Animate ("Resize",
			              transform,
			              s => paintSize = s,
			              length: 150,
			              easing: Easing.SinInOut,
			              finished: (x, aborted) => { if (!aborted) MaybeReanimate(); });
			QueueResize ();
		}

		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("Resize");
			base.OnDestroyed ();
		}

		void MaybeReanimate ()
		{
			disableSizeCheck = true;
			int minw, natw, minh, nath;
			GetPreferredWidth (out minw, out natw);
			GetPreferredHeight (out minh, out nath);
			disableSizeCheck = false;

			if (natw == paintSize.Width && nath == paintSize.Height)
				QueueResize ();
			else
				AnimatedResize (); //Desired size changed mid animation
		}

		protected override bool OnDrawn (Cairo.Context context)
		{
			if ((position & PopupPosition.Top) != 0 || (position & PopupPosition.Bottom) != 0)
				theme.ArrowOffset = Allocation.Width / 2;
			else
				theme.ArrowOffset = Allocation.Height / 2;

			context.Save ();
			Theme.SetBorderPath (context, BorderAllocation, position);
			context.Clip ();
			OnDrawContent (context); // Draw content first so we can easily clip it
			context.Restore ();


			// protect against overriden methods which leave in a bad state
			context.Save ();
			if (Theme.DrawPager) {
				Theme.RenderPager (context, 
				                   PangoContext,
				                   BorderAllocation);
			}

			Theme.RenderShadow (context, BorderAllocation, position);
			context.Restore ();

			return base.OnDrawn (context);
		}

		protected virtual void OnDrawContent (Cairo.Context context)
		{
			Theme.RenderBackground (context, new Gdk.Rectangle (0, 0, paintSize.Width, paintSize.Height));
		}

		void UpdatePadding ()
		{
			uint top,left,bottom,right;
			top = left = bottom = right = (uint)Theme.Padding + 1;

			if (ShowArrow) {
				if ((position & PopupPosition.Top) != 0)
					top += (uint)Theme.ArrowLength;
				else if ((position & PopupPosition.Bottom) != 0)
					bottom += (uint)Theme.ArrowLength;
				else if ((position & PopupPosition.Left) != 0)
					left += (uint)Theme.ArrowLength;
				else if ((position & PopupPosition.Right) != 0)
					right += (uint)Theme.ArrowLength;
			}
			alignment.SetPadding (top, bottom, left, right);
		}

		void OnRedrawNeeded (object sender, EventArgs args)
		{
			UpdatePadding ();
			QueueDraw ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			if (!this.AnimationIsRunning ("Resize"))
				paintSize = new Gdk.Size (allocation.Width, allocation.Height);

			base.OnSizeAllocated (allocation);
		}

		protected Rectangle ChildAllocation {
			get {
				var rect = BorderAllocation;
				rect.Inflate (-Theme.Padding - 1, -Theme.Padding - 1);
				return rect;
			}
		}

		Rectangle BorderAllocation {
			get {
				var rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, paintSize.Width, paintSize.Height);
				if (ShowArrow) {
					if ((position & PopupPosition.Top) != 0) {
						rect.Y += Theme.ArrowLength;
						rect.Height -= Theme.ArrowLength;
					}
					else if ((position & PopupPosition.Bottom) != 0) {
						rect.Height -= Theme.ArrowLength;
					}
					else if ((position & PopupPosition.Left) != 0) {
						rect.X += Theme.ArrowLength;
						rect.Width -= Theme.ArrowLength;
					}
					else if ((position & PopupPosition.Right) != 0) {
						rect.Width -= Theme.ArrowLength;
					}
				}
				return rect;
			}
		}	}
}

