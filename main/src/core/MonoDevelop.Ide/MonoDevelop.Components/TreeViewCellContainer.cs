//
// TreeViewCellContainer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Gdk;

namespace MonoDevelop.Components
{
	public class TreeViewCellContainer: Entry
	{
		EventBox box;
		
		public new event EventHandler EditingDone;
		
		public TreeViewCellContainer (Gtk.Widget child)
		{
			box = new EventBox ();
			box.ButtonPressEvent += new ButtonPressEventHandler (OnClickBox);
			box.Add (child);
			child.Show ();
			Show ();
		}
		
		[GLib.ConnectBefore]
		void OnClickBox (object s, ButtonPressEventArgs args)
		{
			// Avoid forwarding the button press event to the
			// tree, since it would hide the cell editor.
			args.RetVal = true;
		}
		
		protected override void OnParentSet (Gtk.Widget parent)
		{
			base.OnParentSet (parent);
			
			if (Parent != null) {
				if (ParentWindow != null)
					box.ParentWindow = ParentWindow;
				box.Parent = Parent;
				box.Show ();
			}
			else {
				box.Unparent ();
				if (EditingDone != null)
					EditingDone (this, EventArgs.Empty);
			}
		}
		
		protected override void OnShown ()
		{
			// Do nothing.
		}

		protected override void OnGetPreferredWidth (out int minimum, out int natural)
		{
			base.OnGetPreferredWidth (out minimum, out natural);
			if (box.Child != null) {
				var r = box.Child.SizeRequest ();
				minimum = natural = r.Width;
			}
			else {
				var r = box.SizeRequest ();
				minimum = natural = r.Width;
			}
		}

		protected override void OnGetPreferredHeight (out int minimum, out int natural)
		{
			base.OnGetPreferredHeight (out minimum, out natural);
			if (box.Child != null) {
				var r = box.Child.SizeRequest ();
				minimum = natural = r.Height + 2 * box.Child.Style?.Ythickness ?? 0;
			}
			else {
				var r = box.SizeRequest ();
				minimum = natural = r.Height;
			}
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			box.SetAllocation (allocation);
		}
	}
}
