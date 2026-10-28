using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Stetic.Windows
{
	class WindowsTheme
	{
		const string USER32 = "user32.dll";
		const string GDI32 = "gdi32.dll";
		const string LIBGDK = "libgdk-win32-2.0-0.dll";
		const string UXTHEME = "uxtheme";
		const string LIBUXTHEME = "uxtheme.dll";
		IntPtr hWnd;
		IntPtr hTheme;

		public WindowsTheme (Gdk.Window win)
		{
			if (win == null)
				return;
			try {
				hWnd = gdk_win32_drawable_get_handle (win.Handle);
				hTheme = OpenThemeData (hWnd, "WINDOW");
			} catch (DllNotFoundException) {
				hTheme = IntPtr.Zero;
			}
		}

		public void Dipose ( )
		{
			if (hTheme != IntPtr.Zero)
				CloseThemeData (hTheme);
		}

		public void DrawWindowFrame (Gtk.Widget w, string caption, int x, int y, int width, int height)
		{
			var win = w.GdkWindow;
			if (win == null)
				return;
			int captionHeight = GetCaptionHeight ();
			using (var cr = Gdk.CairoHelper.Create (win)) {
				cr.SetSourceColor (ToCairo (w.StyleContext.GetBackgroundColor (w.StateFlags)));
				cr.Rectangle (x, y, width, height);
				cr.Fill ();
				cr.SetSourceColor (ToCairo (w.StyleContext.GetColor (w.StateFlags)));
				cr.MoveTo (x + 4, y + 5);
				using (var layout = new Pango.Layout (w.PangoContext)) {
					layout.SetText (caption);
					Pango.CairoHelper.ShowLayout (cr, layout);
				}
				cr.LineWidth = 1;
				cr.Rectangle (x + 0.5, y + captionHeight + 0.5, width - 1, height - captionHeight - 1);
				cr.Stroke ();
			}
		}

		int GetCaptionHeight ()
		{
			if (hTheme != IntPtr.Zero && hWnd != IntPtr.Zero) {
				IntPtr hdc = GetDC (hWnd);
				RECT r = new RECT (0, 0, 100, 100);
				SIZE size;
				GetThemePartSize (hTheme, hdc, WP_CAPTION, CS_ACTIVE, ref r, 1, out size);
				ReleaseDC (hWnd, hdc);
				if (size.cy > 0)
					return size.cy;
			}
			return 20;
		}

		static Cairo.Color ToCairo (Gdk.RGBA rgba)
		{
			return new Cairo.Color ((float)rgba.Red, (float)rgba.Green, (float)rgba.Blue);
		}

		public Gdk.Rectangle GetWindowClientArea (Gdk.Rectangle allocation)
		{
			if (hTheme == IntPtr.Zero)
				return new Gdk.Rectangle (allocation.X + FrameBorder, allocation.Y + 20, allocation.Width - FrameBorder * 2, allocation.Height - 20 - FrameBorder);

			IntPtr hdc = GetDC (hWnd);
			RECT r = new RECT (allocation.X, allocation.Y, allocation.X + allocation.Width, allocation.Y + allocation.Height);
			SIZE size;
			GetThemePartSize (hTheme, hdc, WP_CAPTION, CS_ACTIVE, ref r, 1, out size);
			ReleaseDC (hWnd, hdc);

			return new Gdk.Rectangle (allocation.X + FrameBorder, allocation.Y + size.cy, allocation.Width - FrameBorder * 2, allocation.Height - size.cy - FrameBorder);
		}

		const int FrameBorder = 8;

		const int WP_CAPTION = 1;
		const int WP_FRAMEBOTTOM = 9;
		const int CS_ACTIVE = 1;
		const int TMT_CAPTIONFONT = 801;

		const int DT_LEFT = 0x0;
		const int DT_WORD_ELLIPSIS = 0x40000;
		const int DT_SINGLELINE = 0x20;
		

		[DllImport (UXTHEME, ExactSpelling = true)]
		extern static Int32 DrawThemeBackground (IntPtr hTheme, IntPtr hdc, int iPartId,
		   int iStateId, ref RECT pRect, ref RECT pClipRect);

		[DllImport (LIBUXTHEME, ExactSpelling = true, CharSet = CharSet.Unicode)]
		static extern IntPtr OpenThemeData (IntPtr hWnd, String classList);

		[DllImport (LIBUXTHEME, ExactSpelling = true)]
		extern static Int32 CloseThemeData (IntPtr hTheme);

		[DllImport (UXTHEME, ExactSpelling = true)]
		extern static Int32 GetThemePartSize (IntPtr hTheme, IntPtr hdc, int part, int state, ref RECT pRect, int eSize, out SIZE size);

		[DllImport (UXTHEME, ExactSpelling = true)]
		extern static Int32 GetThemeBackgroundExtent (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, ref RECT pBoundingRect, out RECT pContentRect);

		[DllImport (UXTHEME, ExactSpelling = true)]
		extern static Int32 GetThemeMargins (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, out MARGINS pMargins);

		[DllImport (UXTHEME, ExactSpelling = true, CharSet = CharSet.Unicode)]
		extern static Int32 DrawThemeText (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, String text, int textLength, UInt32 textFlags, UInt32 textFlags2, ref RECT pRect);

		[DllImport (UXTHEME, ExactSpelling = true, CharSet = CharSet.Unicode)]
		extern static Int32 GetThemeSysFont (IntPtr hTheme, int iFontId, ref LOGFONT plf);

		[DllImport (GDI32)]
		static extern IntPtr CreateFontIndirect ([In] ref LOGFONT lplf);

		[DllImport (GDI32)]
		static extern int SetBkMode (IntPtr hdc, int iBkMode);
	
		[DllImport (GDI32, ExactSpelling = true, PreserveSig = true, SetLastError = true)]
		static extern IntPtr SelectObject (IntPtr hdc, IntPtr hgdiobj);

		[DllImport (GDI32)]
		static extern bool DeleteObject (IntPtr hObject);
	
		[DllImport (USER32)]
		static extern IntPtr GetDC (IntPtr hWnd);

		[DllImport (USER32)]
		static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);

		[DllImport (LIBGDK, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_drawable_get_handle (IntPtr raw);

		[DllImport (LIBGDK, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_hdc_get (IntPtr drawable, IntPtr gc, int usage);

		[DllImport (LIBGDK, CallingConvention = CallingConvention.Cdecl)]
		static extern void gdk_win32_hdc_release (IntPtr drawable, IntPtr gc, int usage);
	}


	[Serializable, StructLayout (LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT (int left_, int top_, int right_, int bottom_)
		{
			Left = left_;
			Top = top_;
			Right = right_;
			Bottom = bottom_;
		}

		public int Height { get { return Bottom - Top; } }
		public int Width { get { return Right - Left; } }
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct SIZE
	{
		public int cx;
		public int cy;

		public SIZE (int cx, int cy)
		{
			this.cx = cx;
			this.cy = cy;
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct MARGINS
	{
		public int leftWidth;
		public int rightWidth;
		public int topHeight;
		public int bottomHeight;
	}

	[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct LOGFONT
	{
		public int lfHeight;
		public int lfWidth;
		public int lfEscapement;
		public int lfOrientation;
		public int lfWeight;
		public byte lfItalic;
		public byte lfUnderline;
		public byte lfStrikeOut;
		public byte lfCharSet;
		public byte lfOutPrecision;
		public byte lfClipPrecision;
		public byte lfQuality;
		public byte lfPitchAndFamily;
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
		public string lfFaceName;
	}
}
