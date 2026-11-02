// 
// WorkbenchWindow.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	class WorkbenchWindow: Gtk.Window
	{
		List<TopLevelChild> topLevels = new List<TopLevelChild> ();

		public WorkbenchWindow (): base (Gtk.WindowType.Toplevel)
		{
			GtkWorkarounds.FixContainerLeak (this);
			this.Role = "workbench";

			// Sin un WM que dibuja decoraciones server-side (Wayland, Xvfb sin WM)
			// la ventana queda con una barra de titulo negra sin titulo ni botones.
			// Con este binding ademas GTK no genera los botones CSD automaticamente
			// (un HeaderBar con ShowCloseButton queda sin hijos), asi que la barra de
			// titulo se construye a mano: titulo centrado + botones
			// minimizar/maximizar/cerrar, con el estilo del tema (Win11-round-Dark).
			Gtk.HeaderBar headerbar = new Gtk.HeaderBar ();
			headerbar.ShowCloseButton = false;
			headerbar.DecorationLayout = "minimize,maximize,close";

			Gtk.Label titleLabel = new Gtk.Label (Title) { Halign = Gtk.Align.Center };
			headerbar.CustomTitle = titleLabel;
			// En este binding no existe signal de cambio de titulo ni la
			// propiedad Title es virtual, asi que el label se sincroniza con
			// un polling ligero (comparacion de strings cada 300ms).
			GLib.Timeout.Add (300, delegate {
				if (Title != titleLabel.Text)
					titleLabel.Text = Title;
				return true;
			});

			AddTitleBarButton (headerbar, "window-close-symbolic", "Cerrar la ventana", delegate { Close (); });
			AddTitleBarButton (headerbar, "window-maximize-symbolic", "Maximizar/restaurar la ventana", delegate { if (Maximized) Unmaximize (); else Maximize (); });
			AddTitleBarButton (headerbar, "window-minimize-symbolic", "Minimizar la ventana", delegate { Iconify (); });
			Titlebar = headerbar;
			// La ventana se muestra con un simple Show (sin cascada), asi que los
			// hijos del headerbar (titulo y botones) se muestran aqui explicitamente.
			headerbar.ShowAll ();
		}

		void AddTitleBarButton (Gtk.HeaderBar headerbar, string iconName, string tooltip, EventHandler action)
		{
			Gtk.Image image = new Gtk.Image ();
			image.IconName = iconName;
			Gtk.Button button = new Gtk.Button ();
			button.Image = image;
			button.TooltipText = tooltip;
			button.StyleContext.AddClass ("titlebutton");
			button.StyleContext.AddClass ("flat");
			button.Clicked += action;
			headerbar.PackEnd (button);
			button.ShowAll ();
		}

		class TopLevelChild
		{
			public int X;
			public int Y;
			public Gtk.Widget Child;
		}
		
		public void AddTopLevelWidget (Gtk.Widget w, int x, int y)
		{
			w.Parent = this;
			TopLevelChild info = new TopLevelChild ();
			info.X = x;
			info.Y = y;
			info.Child = w;
			topLevels.Add (info);
		}
		
		public void RemoveTopLevelWidget (Gtk.Widget w)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					w.Unparent ();
					topLevels.Remove (info);
					break;
				}
			}
		}
		
		public void MoveTopLevelWidget (Gtk.Widget w, int x, int y)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					info.X = x;
					info.Y = y;
					QueueResize ();
					break;
				}
			}
		}
		
		public Gdk.Rectangle GetTopLevelPosition (Gtk.Widget w)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					Gtk.Requisition req = w.SizeRequest ();
					return new Gdk.Rectangle (info.X, info.Y, req.Width, req.Height);
				}
			}
			return new Gdk.Rectangle (0,0,0,0);
		}
		
		public Gdk.Rectangle GetCoordinates (Gtk.Widget w)
		{
			int px, py;
			if (!w.TranslateCoordinates (this, 0, 0, out px, out py))
				return new Gdk.Rectangle (0,0,0,0);

			Gdk.Rectangle rect = w.Allocation;
			rect.X = px - Allocation.X;
			rect.Y = py - Allocation.Y;
			return rect;
		}
		
		protected override void OnGetPreferredWidth (out int minimum, out int natural)
		{
			base.OnGetPreferredWidth (out minimum, out natural);

			// Ignore the size of top levels. They are supposed to fit the available space
			foreach (TopLevelChild tchild in topLevels)
				tchild.Child.SizeRequest ();
		}

		protected override void OnGetPreferredHeight (out int minimum, out int natural)
		{
			base.OnGetPreferredHeight (out minimum, out natural);

			// Ignore the size of top levels. They are supposed to fit the available space
			foreach (TopLevelChild tchild in topLevels)
				tchild.Child.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			foreach (TopLevelChild child in topLevels) {
				Gtk.Requisition req = child.Child.SizeRequest ();
				child.Child.SizeAllocate (new Gdk.Rectangle (allocation.X + child.X, allocation.Y + child.Y, req.Width, req.Height));
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			if (Child != null)
				callback (Child);
			foreach (TopLevelChild child in topLevels)
				callback (child.Child);
			// El for-all nativo de GtkWindow incluye la Titlebar propia cuando
			// include_internals es true (los mecanismos internos de draw/map/size
			// pasan siempre true). Sin esta línea una Titlebar establecida no
			// llega a mapearse ni a pintarse.
			if (include_internals && Titlebar != null)
				callback (Titlebar);
		}
	}
}
