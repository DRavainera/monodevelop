//
// DiffRendererWidget.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using Gtk;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Text;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using System.Linq;

namespace MonoDevelop.VersionControl.Views
{
	class DiffRendererWidget : Gtk.DrawingArea
	{
		Pango.Layout layout;

		int width, height, lineHeight;
		string [] lines;
		int selectedLine = -1;
		int RightPadding = 4;

		int RoundedSectionRadius = 4;
		int LeftPaddingBlock = 19;

		public string[] Lines {
			get => lines;
			set {
				lines = value;
				InitCell ();
				QueueDraw ();
			}
		}

		public int SelectedLine => selectedLine;

		public event EventHandler<int> DiffLineActivated;

		protected virtual void OnDiffLineActivated (int line)
		{
			DiffLineActivated?.Invoke (this, line);
		}

		public DiffRendererWidget ()
		{
			Events |= EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.ButtonPressMask;
			Accessible?.SetRole (AtkCocoa.Roles.AXGroup, GettextCatalog.GetString ("Diff View"));
		}

		void DisposeLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}

		bool isDisposed = false;

		protected override void OnDestroyed ()
		{
			isDisposed = true;
			DisposeLayout ();
			ClearAccessibleLines ();
			base.OnDestroyed ();
		}

		public void InitCell ()
		{
			if (isDisposed)
				return;
			if (lines != null && lines.Length > 0) {
				int maxlen = -1;
				int maxlin = -1;
				for (int n = 0; n < lines.Length; n++) {
					string line = ProcessLine (lines [n]);
					if (line == null)
						throw new Exception ("Line " + n + " from diff was null.");
					if (line.Length > maxlen) {
						maxlen = lines [n].Length;
						maxlin = n;
					}
				}
				DisposeLayout ();
				layout = CreateLayout (lines [maxlin]);
				layout.GetPixelSize (out width, out lineHeight);
				height = lineHeight * lines.Length;
				width += LeftPaddingBlock + RightPadding;
			} else
				width = height = 0;
			QueueResize ();
		}

		protected override void OnGetPreferredWidth (out int minimumWidth, out int naturalWidth)
		{
			minimumWidth = naturalWidth = width;
		}

		protected override void OnGetPreferredHeight (out int minimumHeight, out int naturalHeight)
		{
			minimumHeight = naturalHeight = height;
		}

		int px, py;
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			px = (int)evnt.X;
			py = (int)evnt.Y;
			QueueDraw ();
			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			px = py = -1;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (selectedLine >= 0) {
				if (evnt.Type == Gdk.EventType.TwoButtonPress)
					OnDiffLineActivated (selectedLine);
			}
			return base.OnButtonPressEvent (evnt);
		}

		Pango.Layout CreateLayout (string text)
		{
			var layout = new Pango.Layout (PangoContext);
			layout.SingleParagraphMode = false;
			layout.FontDescription = IdeServices.FontService.MonospaceFont;
			layout.SetText (text);
			return layout;
		}

		protected override bool OnDrawn (Cairo.Context cr)
		{
			var widget = this;
			ClearAccessibleLines ();
			int w, maxy;
			w = widget.Allocation.Width;
			maxy = widget.Allocation.Height;
			var cell_area = Allocation;
		//		if (DrawLeft) {
		//			cell_area.Width += cell_area.X - leftSpace;
		//			cell_area.X = leftSpace;
		//		}

			cell_area.Width -= RightPadding;

			cr.Rectangle (cell_area.X, cell_area.Y, cell_area.Width - 1, cell_area.Height);
			cr.SetSourceColor (widget.GetStyleBackgroundColor (Gtk.StateType.Normal).ToCairoColor ());
			cr.Fill ();
			if (lines == null)
				return true;
			var normalColor = widget.GetStyleColor (StateType.Normal).ToCairoColor ();
			var removedColor = Styles.LogView.DiffRemoveBackgroundColor.AddLight (-0.3).ToCairoColor ();
			var addedColor = Styles.LogView.DiffAddBackgroundColor.AddLight (-0.3).ToCairoColor ();
			var infoColor = widget.GetStyleColor (StateType.Normal).ToGdkColor ().AddLight (0.2).ToCairoColor ();

			// Rendering is done in two steps:
			// 1) Get a list of blocks to render
			// 2) render the blocks

			var blocks = CalculateBlocks (maxy, cell_area.Y + 2);

			// Now render the blocks

			// The y position of the highlighted line
			int selectedLineRowTop = -1;

			BlockInfo lastCodeSegmentStart = null;
			BlockInfo lastCodeSegmentEnd = null;

			foreach (BlockInfo block in blocks) {
				if (block.Type == BlockType.Info) {
					// Finished drawing the content of a code segment. Now draw the segment border and label.
					if (lastCodeSegmentStart != null)
						DrawCodeSegmentBorder (infoColor, cr, cell_area.X, cell_area.Width, lastCodeSegmentStart, lastCodeSegmentEnd, lines, widget);
					lastCodeSegmentStart = block;
				}

				lastCodeSegmentEnd = block;

				if (block.YEnd < 0)
					continue;

				// Draw the block background
				DrawBlockBg (cr, cell_area.X + 1, cell_area.Width - 2, block);

				// Get all text for the current block
				var sb = new StringBuilder ();
				bool replaceFirst = false;
				int subLine = 0;
				for (int n = block.FirstLine; n <= block.LastLine; n++) {
					string s = ProcessLine (lines [n]);
					if (n > block.FirstLine)
						sb.Append ('\n');
					if ((block.Type == BlockType.Added || block.Type == BlockType.Removed) && s.Length > 0) {
						sb.Append (' ');
						sb.Append (s, 1, s.Length - 1);
						replaceFirst = true;
					} else
						sb.Append (s);
					int idx = 0, curIdx = 0;
					while ((idx = s.IndexOf ('\n')) >= 0) {
						var y1 = block.YStart + subLine * lineHeight;
						if (y1 < cell_area.Bottom && y1 + lineHeight >= Allocation.Y && y1 < Allocation.Bottom)
							AddAccessibleLine (cell_area.X + 2 + LeftPaddingBlock, y1, block.Type, block.FirstLine + n + subLine, ref replaceFirst, s.Substring (curIdx, idx));
						subLine++;
						curIdx = idx;
					}
					var y2 = block.YStart + subLine * lineHeight;
					if (y2 < cell_area.Bottom && y2 + lineHeight >= Allocation.Y && y2 < Allocation.Bottom)
						AddAccessibleLine (cell_area.X + 2 + LeftPaddingBlock, y2, block.Type, block.FirstLine + n + subLine, ref replaceFirst, curIdx > 0 ? s.Substring (curIdx) : s);
					subLine++;
				}

				// Draw a special background for the selected line
				if (px < 0) {
					selectedLine = selectedLineRowTop = -1;
				} else if (block.Type != BlockType.Info && px >= cell_area.X && py <= cell_area.Right && py >= block.YStart && py <= block.YEnd) {
					int row = (py - block.YStart) / lineHeight;
					double yrow = block.YStart + lineHeight * row;
					double xrow = cell_area.X + LeftPaddingBlock;
					int wrow = cell_area.Width - 1 - LeftPaddingBlock;
					if (block.Type == BlockType.Added)
						cr.SetSourceColor (Styles.LogView.DiffAddBackgroundColor.AddLight (0.1).ToCairoColor ());
					else if (block.Type == BlockType.Removed)
						cr.SetSourceColor (Styles.LogView.DiffRemoveBackgroundColor.AddLight (0.1).ToCairoColor ());
					else {
						cr.SetSourceColor (Styles.LogView.DiffHighlightColor.ToCairoColor ());
						xrow -= LeftPaddingBlock;
						wrow += LeftPaddingBlock;
					}
					cr.Rectangle (xrow, yrow, wrow, lineHeight);
					cr.Fill ();
					selectedLine = block.SourceLineStart + row;
					//				selctedPath = path;
					selectedLineRowTop = (int)yrow;
				}

				// Draw the line text. Ignore header blocks, since they are drawn as labels in DrawCodeSegmentBorder

				if (block.Type != BlockType.Info) {
					layout.SetMarkup ("");
					layout.SetText (sb.ToString ());
					Cairo.Color textcolor;
					switch (block.Type) {
					case BlockType.Removed: textcolor = removedColor; break;
					case BlockType.Added: textcolor = addedColor; break;
					case BlockType.Info: textcolor = infoColor; break;
					default: textcolor = normalColor; break;
					}
					cr.Save ();
					cr.Translate (cell_area.X + 2 + LeftPaddingBlock, block.YStart);
					cr.SetSourceColor (textcolor);
					Pango.CairoHelper.ShowLayout (cr, layout);
					cr.Restore ();
				}

				// Finally draw the change symbol at the left margin

				DrawChangeSymbol (cr, widget, cell_area.X + 1, cell_area.Width - 2, block);
			}

			// Finish the drawing of the code segment
			if (lastCodeSegmentStart != null)
				DrawCodeSegmentBorder (infoColor, cr, cell_area.X, cell_area.Width, lastCodeSegmentStart, lastCodeSegmentEnd, lines, widget);

			// Draw the source line number at the current selected line. It must be done at the end because it must
			// be drawn over the source code text and segment borders.
			if (selectedLineRowTop != -1)
				DrawLineBox (normalColor, cr, Allocation.Right - 4, selectedLineRowTop, selectedLine, widget);

			Accessible?.SetAccessibleChildren (accessibleLines.Select (l => l.Accessible).ToArray ());
			return true;
		}

		void AddAccessibleLine (int x, int y, BlockType blockType, int lineNumber, ref bool replaceFirst, string text)
		{
			if (Accessible == null)
				return;
			if (replaceFirst) {
				text = ' ' + text.Substring (1);
				replaceFirst = false;
			}
			this.accessibleLines.Add (new DiffLineAccessible (this, x, y, blockType, lineNumber, text));
		}

		static string ProcessLine (string line)
		{
			if (line == null)
				return null;
			return line.Replace ("\t", "    ");
		}

		List<BlockInfo> CalculateBlocks (int maxy, int y)
		{
			// cline keeps track of the current source code line (the one to jump to when double clicking)
			int cline = 1;

			BlockInfo currentBlock = null;

			var result = new List<BlockInfo> ();
			if (lines == null)
				return result;
			int removedLines = 0;
			for (int n = 0; n < lines.Length; n++, y += lineHeight) {

				string line = lines [n];
				if (line.Length == 0) {
					currentBlock = null;
					y -= lineHeight;
					continue;
				}

				char tag = line [0];

				if (line.StartsWith ("---", StringComparison.Ordinal) ||
					line.StartsWith ("+++", StringComparison.Ordinal)) {
					// Ignore this part of the header.
					currentBlock = null;
					y -= lineHeight;
					continue;
				}
				if (tag == '@') {
					int l = ParseCurrentLine (line);
					if (l != -1) cline = l - 1;
				} else
					cline++;

				BlockType type;
				switch (tag) {
				case '-':
					type = BlockType.Removed;
					removedLines++;
					break;
				case '+': type = BlockType.Added; break;
				case '@': type = BlockType.Info; break;
				default: type = BlockType.Unchanged; break;
				}

				if (type != BlockType.Removed && removedLines > 0) {
					cline -= removedLines;
					removedLines = 0;
				}

				if (currentBlock == null || type != currentBlock.Type) {
					if (y > maxy)
						break;

					// Starting a new block. Mark section ends between a change block and a normal code block
					if (currentBlock != null && IsChangeBlock (currentBlock.Type) && !IsChangeBlock (type))
						currentBlock.SectionEnd = true;

					currentBlock = new BlockInfo {
						YStart = y,
						FirstLine = n,
						Type = type,
						SourceLineStart = cline,
						SectionStart = (result.Count == 0 || !IsChangeBlock (result [result.Count - 1].Type)) && IsChangeBlock (type)
					};
					result.Add (currentBlock);
				}
				// Include the line in the current block
				currentBlock.YEnd = y + lineHeight;
				currentBlock.LastLine = n;
			}

			return result;
		}

		static bool IsChangeBlock (BlockType t)
		{
			return t == BlockType.Added || t == BlockType.Removed;
		}

		class BlockInfo
		{
			public BlockType Type;
			public int YEnd;
			public int YStart;
			public int FirstLine;
			public int LastLine;
			public bool SectionStart;
			public bool SectionEnd;
			public int SourceLineStart;
		}

		enum BlockType
		{
			Info,
			Added,
			Removed,
			Unchanged
		}

		void DrawCodeSegmentBorder (Cairo.Color textcolor, Cairo.Context ctx, double x, int width, BlockInfo firstBlock, BlockInfo lastBlock, string [] lines, Gtk.Widget widget)
		{
			int shadowSize = 2;
			int spacing = 4;
			int bottomSpacing = (lineHeight - spacing) / 2;

			ctx.Rectangle (x + shadowSize + 0.5, firstBlock.YStart + bottomSpacing + spacing - shadowSize + 0.5, width - shadowSize * 2, shadowSize);
			ctx.SetSourceColor (Styles.LogView.DiffBoxSplitterColor.ToCairoColor ());
			ctx.LineWidth = 1;
			ctx.Fill ();

			ctx.Rectangle (x + shadowSize + 0.5, lastBlock.YEnd + bottomSpacing + 0.5, width - shadowSize * 2, shadowSize);
			ctx.SetSourceColor (Styles.LogView.DiffBoxSplitterColor.ToCairoColor ());
			ctx.Fill ();

			ctx.Rectangle (x + 0.5, firstBlock.YStart + bottomSpacing + spacing + 0.5, width, lastBlock.YEnd - firstBlock.YStart - spacing);
			ctx.SetSourceColor (Styles.LogView.DiffBoxBorderColor.ToCairoColor ());
			ctx.Stroke ();

			string text = lines [firstBlock.FirstLine].Replace ("@", "").Replace ("-", "");
			text = "<span size='x-small'>" + text.Replace ("+", "</span><span size='small'>→</span><span size='x-small'> ") + "</span>";

			layout.SetText ("");
			layout.SetMarkup (text);
			int tw, th;
			layout.GetPixelSize (out tw, out th);
			th--;

			int dy = (lineHeight - th) / 2;

			ctx.Rectangle (x + 2 + LeftPaddingBlock - 1 + 0.5, firstBlock.YStart + dy - 1 + 0.5, tw + 2, th + 2);
			ctx.LineWidth = 1;
			ctx.SetSourceColor (widget.GetStyleBackgroundColor (StateType.Normal).ToCairoColor ());
			ctx.FillPreserve ();
			ctx.SetSourceColor (Styles.LogView.DiffBoxBorderColor.ToCairoColor ());
			ctx.Stroke ();

			ctx.Save ();
			ctx.Translate ((int)(x + 2 + LeftPaddingBlock), firstBlock.YStart + dy);
			ctx.SetSourceColor (textcolor);
			Pango.CairoHelper.ShowLayout (ctx, layout);
			ctx.Restore ();
		}

		void DrawLineBox (Cairo.Color textcolor, Cairo.Context ctx, int right, int top, int line, Gtk.Widget widget)
		{
			layout.SetText ("");
			layout.SetMarkup ("<small>" + line.ToString () + "</small>");
			int tw, th;
			layout.GetPixelSize (out tw, out th);
			th--;

			int dy = (lineHeight - th) / 2;

			ctx.Rectangle (right - tw - 2 + 0.5, top + dy - 1 + 0.5, tw + 2, th + 2);
			ctx.LineWidth = 1;
			ctx.SetSourceColor (widget.GetStyleBackgroundColor (Gtk.StateType.Normal).ToCairoColor ());
			ctx.FillPreserve ();
			ctx.SetSourceColor (Styles.LogView.DiffBoxBorderColor.ToCairoColor ());
			ctx.Stroke ();

			ctx.Save ();
			ctx.Translate (right - tw - 1, top + dy);
			ctx.SetSourceColor (textcolor);
			Pango.CairoHelper.ShowLayout (ctx, layout);
			ctx.Restore ();
		}

		void DrawBlockBg (Cairo.Context ctx, double x, int width, BlockInfo block)
		{
			if (!IsChangeBlock (block.Type))
				return;

			var color = block.Type == BlockType.Added ? Styles.LogView.DiffAddBackgroundColor : Styles.LogView.DiffRemoveBackgroundColor;
			double y = block.YStart;
			int height = block.YEnd - block.YStart;

			double markerx = x + LeftPaddingBlock;
			double rd = RoundedSectionRadius;
			if (block.SectionStart) {
				ctx.Arc (x + rd, y + rd, rd, 180 * (Math.PI / 180), 270 * (Math.PI / 180));
				ctx.LineTo (markerx, y);
			} else {
				ctx.MoveTo (markerx, y);
			}

			ctx.LineTo (markerx, y + height);

			if (block.SectionEnd) {
				ctx.LineTo (x + rd, y + height);
				ctx.Arc (x + rd, y + height - rd, rd, 90 * (Math.PI / 180), 180 * (Math.PI / 180));
			} else {
				ctx.LineTo (x, y + height);
			}
			if (block.SectionStart) {
				ctx.LineTo (x, y + rd);
			} else {
				ctx.LineTo (x, y);
			}
			ctx.SetSourceColor (color.AddLight (0.1).ToCairoColor ());
			ctx.Fill ();

			ctx.Rectangle (markerx, y, width - markerx, height);

			// FIXME: VV: Remove gradient features
			using (Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x + width, y)) {
				pat.AddColorStop (0, color.AddLight (0.21).ToCairoColor ());
				pat.AddColorStop (1, color.AddLight (0.3).ToCairoColor ());
				ctx.SetSource (pat);
				ctx.Fill ();
			}
		}

		static Xwt.Drawing.Image gutterAdded = Xwt.Drawing.Image.FromResource ("gutter-added-15.png");
		static Xwt.Drawing.Image gutterRemoved = Xwt.Drawing.Image.FromResource ("gutter-removed-15.png");

		void DrawChangeSymbol (Cairo.Context ctx, Widget widget, double x, int width, BlockInfo block)
		{
			if (!IsChangeBlock (block.Type))
				return;

			if (block.Type == BlockType.Added) {
				var ix = x + (LeftPaddingBlock / 2) - (gutterAdded.Width / 2);
				var iy = block.YStart + ((block.YEnd - block.YStart) / 2 - gutterAdded.Height / 2);
				ctx.DrawImage (widget, gutterAdded, ix, iy);
			} else {
				var ix = x + (LeftPaddingBlock / 2) - (gutterRemoved.Width / 2);
				var iy = block.YStart + ((block.YEnd - block.YStart) / 2 - gutterRemoved.Height / 2);
				ctx.DrawImage (widget, gutterRemoved, ix, iy);
			}
		}

		static StateType GetState (Gtk.Widget widget, CellRendererState flags)
		{
			if ((flags & CellRendererState.Selected) != 0)
				return widget.HasFocus ? StateType.Selected : StateType.Active;
			else
				return StateType.Normal;
		}

		static int ParseCurrentLine (string line)
		{
			int i = line.IndexOf ('+');
			if (i == -1) return -1;
			i++;
			int j = line.IndexOf (',', i);
			if (j == -1) return -1;
			int cline;
			if (!int.TryParse (line.Substring (i, j - i), out cline))
				return -1;
			return cline;
		}

		List<DiffLineAccessible> accessibleLines = new List<DiffLineAccessible> ();

		void ClearAccessibleLines ()
		{
			if (Accessible == null)
				return;
			foreach (var button in accessibleLines) {
				button.Dispose ();
			}
			accessibleLines.Clear ();
			Accessible.SetAccessibleChildren (Array.Empty<AccessibilityElementProxy> ());
		}

		class DiffLineAccessible : IDisposable
		{
			DiffRendererWidget widget;
			int line;

			public AccessibilityElementProxy Accessible { get; private set; }
			public bool Visible { get; internal set; }

			public DiffLineAccessible (DiffRendererWidget widget, int x, int y, BlockType blockType, int line, string text)
			{
				this.widget = widget;
				this.line = line;

				Accessible = AccessibilityElementProxy.ButtonElementProxy ();
				Accessible.GtkParent = widget;
				Accessible.PerformPress += PerformPress;
				string msg;
				switch (blockType) {
				case BlockType.Added:
					msg = GettextCatalog.GetString ("Added line");
					break;
				case BlockType.Removed:
					msg = GettextCatalog.GetString ("Removed line");
					break;
				default:
					msg = GettextCatalog.GetString ("Unchanged line");
					break;
				}
				Accessible.SetRole (AtkCocoa.Roles.AXButton, msg);

				Accessible.Label = GettextCatalog.GetString ("Line {0}, Text {1}", line, text);

				SetBounds (x, y, widget.Allocation.Width, widget.lineHeight);
			}

			public void SetBounds (int x, int y, int w, int h)
			{
				Accessible.FrameInGtkParent = new Rectangle (x, y, w, h);
				var cocoaY = widget.Allocation.Height - y - h;
				Accessible.FrameInParent = new Rectangle (x, cocoaY, w, h);
			}

			void PerformPress (object sender, EventArgs e)
			{
				widget.OnDiffLineActivated (this.line);
			}

			public void Dispose ()
			{
				if (Accessible == null)
					return;
				Accessible.PerformPress -= PerformPress;
				Accessible = null;
				widget = null;
			}
		}
	}
}
