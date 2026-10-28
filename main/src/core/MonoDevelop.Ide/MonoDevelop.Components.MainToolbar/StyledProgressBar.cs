//
// StyledProgressBar.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
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

namespace MonoDevelop.Components.MainToolbar
{
	class StyledProgressBar : EventBox
	{
		double fraction;
		public double Fraction {
			get {
				return fraction;
			}
			set {
				fraction = value;
				QueueDraw ();
			}
		}

		bool showProgress;
		public bool ShowProgress {
			get {
				return showProgress;
			}
			set {
				showProgress = value;
				QueueDraw ();
			}
		}
		public StyledProgressBar ()
		{
			VisibleWindow = false;
			HeightRequest = 1;
		}

		const int height = 14;

		protected override bool OnDrawn (Cairo.Context cr)
		{
			if (!showProgress)
				return base.OnDrawn (cr);
			cr.LineWidth = 1;
			cr.MoveTo (0.5, 0.5);
			cr.RelLineTo (Allocation.Width, 0);
			cr.SetSourceRGB (0.8, 0.8, 0.8);
			cr.Stroke ();

			cr.MoveTo (0.5, 0.5);
			cr.RelLineTo ((double)Allocation.Width * fraction, 0);
			cr.SetSourceRGB (0.1, 0.1, 0.1);
			cr.Stroke ();
			return base.OnDrawn (cr);
		}

	}
}

