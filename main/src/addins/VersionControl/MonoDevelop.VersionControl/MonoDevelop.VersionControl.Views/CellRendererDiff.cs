
using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Text;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.VersionControl.Views
{
	class CellRendererDiff: Gtk.CellRendererText
	{
		Pango.Layout layout;
		bool diffMode;
		int width, height, lineHeight;
		string[] lines;		
		int selectedLine = -1;
		TreePath selctedPath;
		TreePath path;
		int RightPadding = 4;
		
		int RoundedSectionRadius = 4;
		int LeftPaddingBlock = 19;
		
		public CellRendererDiff()
		{
		}
		
		void DisposeLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
		
		bool isDisposed = false;
		protected override void Dispose (bool disposing)
		{
			if (disposing && !isDisposed) {
				isDisposed = true;
				DisposeLayout ();
			}
			base.Dispose (disposing);
		}
		
		public void Reset ()
		{
		}

		public void InitCell (Widget container, bool diffMode, string[] lines, TreePath path)
		{
			if (isDisposed)
				return;
			if (lines == null)
				throw new ArgumentNullException ("lines");
			this.lines = lines;
			this.diffMode = diffMode;
			this.path = path;

			if (diffMode) {
				if (lines != null && lines.Length > 0) {
					int maxlen = -1;
					int maxlin = -1;
					for (int n=0; n<lines.Length; n++) {
						string line = ProcessLine (lines [n]);
						if (line == null)
							throw new Exception ("Line " + n + " from diff was null.");
						if (line.Length > maxlen) {
							maxlen = lines [n].Length;
							maxlin = n;
						}
					}
					DisposeLayout ();
					layout = CreateLayout (container, lines [maxlin]);
					layout.GetPixelSize (out width, out lineHeight);
					height = lineHeight * lines.Length;
					width += LeftPaddingBlock + RightPadding;
				}
				else
					width = height = 0;
			}
			else {
				DisposeLayout ();
				layout = CreateLayout (container, string.Join (Environment.NewLine, lines));
				layout.GetPixelSize (out width, out height);
			}
		}
		
		Pango.Layout CreateLayout (Widget container, string text)
		{
			Pango.Layout layout = new Pango.Layout (container.PangoContext);
			layout.SingleParagraphMode = false;
			if (diffMode) {
				layout.FontDescription = IdeServices.FontService.MonospaceFont;
				layout.SetText (text);
			} else {
				layout.SetMarkup (text);
			}
			return layout;
		}
		
		static string ProcessLine (string line)
		{
			if (line == null)
				return null;
			return line.Replace ("\t", "    ");
		}
		
		const int leftSpace = 16;
		public bool DrawLeft { get; set; }
		
		protected override void OnRender (Cairo.Context cr, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
		{
			if (isDisposed || layout == null)
				return;
			if (diffMode) {
			
				if (path.Equals (selctedPath)) {
					selectedLine = -1;
					selctedPath = null;
				}
			
				int w, maxy;
				w = widget.Allocation.Width;
				maxy = widget.Allocation.Height;
				if (DrawLeft) {
					cell_area.Width += cell_area.X - leftSpace;
					cell_area.X = leftSpace;
				}
				var treeview = widget as FileTreeView;
				var p = treeview != null? treeview.CursorLocation : null;
				cell_area.Width -= RightPadding;
			
				cr.Rectangle (cell_area.X, cell_area.Y, cell_area.Width - 1, cell_area.Height);
				cr.SetSourceColor (widget.GetStyleBackgroundColor (Gtk.StateType.Normal).ToCairoColor ());
				cr.Fill ();
			
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
			
				foreach (BlockInfo block in blocks)
				{
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
					StringBuilder sb = new StringBuilder ();
					for (int n=block.FirstLine; n <= block.LastLine; n++) {
						string s = ProcessLine (lines [n]);
						if (n > block.FirstLine)
							sb.Append ('\n');
						if ((block.Type == BlockType.Added || block.Type == BlockType.Removed) && s.Length > 0) {
							sb.Append (' ');
							sb.Append (s, 1, s.Length - 1);
						} else
							sb.Append (s);
					}
				
					// Draw a special background for the selected line
				
					if (block.Type != BlockType.Info && p.HasValue && p.Value.X >= cell_area.X && p.Value.X <= cell_area.Right && p.Value.Y >= block.YStart && p.Value.Y <= block.YEnd) {
						int row = (p.Value.Y - block.YStart) / lineHeight;
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
						selctedPath = path;
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
					DrawLineBox (normalColor, cr, ((Gtk.TreeView)widget).VisibleRect.Right - 4, selectedLineRowTop, selectedLine, widget);
			} else {
				// Rendering a normal text row
				int y = cell_area.Y + (cell_area.Height - height)/2;
				cr.Save ();
				cr.Translate (cell_area.X, y);
				cr.SetSourceColor (widget.GetStyleColor (GetState (widget, flags)).ToCairoColor ());
				Pango.CairoHelper.ShowLayout (cr, layout);
				cr.Restore ();
			}
		}

		List<BlockInfo> CalculateBlocks (int maxy, int y)
		{
			// cline keeps track of the current source code line (the one to jump to when double clicking)
			int cline = 1;

			BlockInfo currentBlock = null;

			var result = new List<BlockInfo> ();
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
		
		void DrawCodeSegmentBorder (Cairo.Color textcolor, Cairo.Context ctx, double x, int width, BlockInfo firstBlock, BlockInfo lastBlock, string[] lines, Gtk.Widget widget)
		{
			int shadowSize = 2;
			int spacing = 4;
			int bottomSpacing = (lineHeight - spacing) / 2;
			
			ctx.Rectangle (x + shadowSize + 0.5, firstBlock.YStart + bottomSpacing + spacing - shadowSize + 0.5, width - shadowSize*2, shadowSize);
			ctx.SetSourceColor (Styles.LogView.DiffBoxSplitterColor.ToCairoColor ());
			ctx.LineWidth = 1;
			ctx.Fill ();
			
			ctx.Rectangle (x + shadowSize + 0.5, lastBlock.YEnd + bottomSpacing + 0.5, width - shadowSize*2, shadowSize);
			ctx.SetSourceColor (Styles.LogView.DiffBoxSplitterColor.ToCairoColor ());
			ctx.Fill ();
			
			ctx.Rectangle (x + 0.5, firstBlock.YStart + bottomSpacing + spacing + 0.5, width, lastBlock.YEnd - firstBlock.YStart - spacing);
			ctx.SetSourceColor (Styles.LogView.DiffBoxBorderColor.ToCairoColor ());
			ctx.Stroke ();
			
			string text = lines[firstBlock.FirstLine].Replace ("@","").Replace ("-","");
			text = "<span size='x-small'>" + text.Replace ("+","</span><span size='small'>→</span><span size='x-small'> ") + "</span>";
			
			layout.SetText ("");
			layout.SetMarkup (text);
			int tw,th;
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
			int tw,th;
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
				var ix = x + (LeftPaddingBlock/2) - (gutterAdded.Width / 2);
				var iy = block.YStart + ((block.YEnd - block.YStart) / 2 - gutterAdded.Height / 2);
				ctx.DrawImage (widget, gutterAdded, ix, iy);
			} else {
				var ix = x + (LeftPaddingBlock/2) - (gutterRemoved.Width / 2);
				var iy = block.YStart + ((block.YEnd - block.YStart) / 2 - gutterRemoved.Height / 2);
				ctx.DrawImage (widget, gutterRemoved, ix, iy);
			}
		}
		
		protected override void OnGetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int c_width, out int c_height)
		{
			x_offset = y_offset = 0;
			c_width = width;
			c_height = height;
			
			if (diffMode) {
				// Add some spacing for the margin
				c_width += 4;
				c_height += 4;
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
		
		public int GetSelectedLine (TreePath cpath)
		{
			if (cpath.Equals (selctedPath))
				return selectedLine;
			else
				return -1;
		}
	}
}
