//
// AnimatedVboxActor.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace Mono.TextEditor.Theatrics
{
	internal enum AnimationState
	{
		Coming,
		Idle,
		IntendingToGo,
		Going
	}

	internal class AnimatedWidget : Container
	{
		public event EventHandler WidgetDestroyed;

		public Widget Widget;
		public Easing Easing;
		public Blocking Blocking;
		public AnimationState AnimationState;
		public uint Duration;
		public double Bias = 1.0;
		public int Width;
		public int Height;
		public int StartPadding;
		public int EndPadding;

		public LinkedListNode<AnimatedWidget> Node;

		private readonly bool horizontal;
		private double percent;
		private Rectangle widget_alloc;
		private Cairo.ImageSurface canvas;

		public AnimatedWidget (Widget widget, uint duration, Easing easing, Blocking blocking, bool horizontal)
		{
			// GTK3: windowless container; declare it so the default realize vfunc
			// doesn't abort (it asserts the widget has no window of its own).
			HasWindow = false;

			GtkWorkarounds.FixContainerLeak (this);
			
			this.horizontal = horizontal;
			Widget = widget;
			Duration = duration;
			Easing = easing;
			Blocking = blocking;
			AnimationState = AnimationState.Coming;
			
			Widget.Parent = this;
			Widget.Destroyed += OnWidgetDestroyed;
			ShowAll ();
		}

		public double Percent {
			get { return percent; }
			set {
				percent = value * Bias;
				QueueResizeNoRedraw ();
			}
		}

		private void OnWidgetDestroyed (object sender, EventArgs args)
		{
			if (!IsRealized) {
				return;
			}

			canvas = new Cairo.ImageSurface (Cairo.Format.Argb32, widget_alloc.Width, widget_alloc.Height);
			using (var c = new Cairo.Context (canvas)) {
				if (Platform.IsMac || GdkWindow == null) {
					//FIXME: quick hack to make less ugly on Mac, because Mac GTK doesn't yet support offscreen drawing
					c.SetSourceColor (this.GetStyleBackgroundColor (StateType.Normal).ToCairoColor ());
					c.Paint ();
				} else {
					using (var pix = new Gdk.Pixbuf (GdkWindow, widget_alloc.X, widget_alloc.Y, widget_alloc.Width, widget_alloc.Height)) {
						Gdk.CairoHelper.SetSourcePixbuf (c, pix, 0, 0);
						c.Paint ();
					}
				}
			}

			if (AnimationState != AnimationState.Going) {
				WidgetDestroyed (this, args);
			}
		}

		#region Overrides

		protected override void OnRemoved (Widget widget)
		{
			if (widget == Widget) {
				widget.Unparent ();
				Widget = null;
			}
		}



		void ComputePrefSize (out int width, out int height)
		{
			if (Widget != null) {
				Requisition req = Widget.SizeRequest ();
				widget_alloc.Width = req.Width;
				widget_alloc.Height = req.Height;
			}

			if (horizontal) {
				width = Choreographer.PixelCompose (percent, widget_alloc.Width + StartPadding + EndPadding, Easing);
				height = widget_alloc.Height;
			} else {
				width = widget_alloc.Width;
				height = Choreographer.PixelCompose (percent, widget_alloc.Height + StartPadding + EndPadding, Easing);
			}
		}

		protected override void OnGetPreferredWidth (out int minimumSize, out int naturalSize)
		{
			base.OnGetPreferredWidth (out minimumSize, out naturalSize);
			int width, height;
			ComputePrefSize (out width, out height);
			WidthRequest = width;
			HeightRequest = height;
			minimumSize = naturalSize = width;
		}

		protected override void OnGetPreferredHeight (out int minimumSize, out int naturalSize)
		{
			base.OnGetPreferredHeight (out minimumSize, out naturalSize);
			int width, height;
			ComputePrefSize (out width, out height);
			minimumSize = naturalSize = height;
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (Widget != null) {
				if (horizontal) {
					widget_alloc.Height = allocation.Height;
					widget_alloc.X = StartPadding;
					if (Blocking == Blocking.Downstage) {
						widget_alloc.X += allocation.Width - widget_alloc.Width;
					}
				} else {
					widget_alloc.Width = allocation.Width;
					widget_alloc.Y = StartPadding;
					if (Blocking == Blocking.Downstage) {
						widget_alloc.Y = allocation.Height - widget_alloc.Height;
					}
				}
				
				if (widget_alloc.Height > 0 && widget_alloc.Width > 0) {
					Widget.SizeAllocate (widget_alloc);
				}
			}
		}

		protected override bool OnDrawn (Cairo.Context cr)
		{
			if (canvas != null) {
				cr.SetSourceSurface (canvas, widget_alloc.X, widget_alloc.Y);
				cr.Paint ();
				return true;
			}

			return base.OnDrawn (cr);
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			if (Widget != null) {
				callback (Widget);
			}
		}
		
		#endregion
		
	}
}
