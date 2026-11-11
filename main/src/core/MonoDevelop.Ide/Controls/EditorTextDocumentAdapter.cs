//
// EditorTextDocumentAdapter.cs
//
// Bridges SkTextEditor's List<string> line model to the MonoDevelop.Ide.Editor
// document contract (IReadonlyTextDocument / IDocumentLine) that the legacy
// SyntaxHighlighting highlighter consumes. A snapshot is taken on every
// highlight request, so the highlighter always sees a consistent document
// even while the editor keeps being edited.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.Controls
{
	/// <summary>
	/// Read-only IReadonlyTextDocument over a snapshot of editor lines
	/// (the line model the SkTextEditor already keeps).
	/// </summary>
	sealed class EditorTextDocumentAdapter : IReadonlyTextDocument
	{
		readonly string [] lines;
		readonly int [] startOffsets; // startOffsets[i] = global offset of lines[i]
		readonly int totalLength;
		readonly string fullText; // the whole snapshot joined with '\n', built ONCE
		readonly string filePath_;

		public EditorTextDocumentAdapter (IReadOnlyList<string> editorLines, string filePath)
		{
			int count = editorLines.Count;
			lines = new string [count];
			startOffsets = new int [count + 1];
			for (int i = 0; i < count; i++) {
				lines [i] = editorLines [i] ?? "";
				startOffsets [i] = i == 0 ? 0 : startOffsets [i - 1] + lines [i - 1].Length + 1;
			}
			totalLength = count == 0 ? 0 : startOffsets [count - 1] + lines [count - 1].Length;
			filePath_ = string.IsNullOrEmpty (filePath) ? "Untitled" : filePath;
			// Built once: GetTextAt/GetCharAt are called by the highlighter for every
			// line it walks, so per-call reconstruction would be O(n) each.
			fullText = BuildText (lines);
		}

		static string BuildText (string [] ls)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < ls.Length; i++) {
				sb.Append (ls [i]);
				if (i < ls.Length - 1)
					sb.Append ('\n');
			}
			return sb.ToString ();
		}

		public bool IsReadOnly => true;
		public FilePath FileName => new FilePath (filePath_);
		public string MimeType => "";
		public ITextSourceVersion Version => null;
		public Encoding Encoding => Encoding.UTF8;
		public int LineCount => lines.Length;
		public int Length => totalLength;

		public string Text {
			get {
				return fullText;
			}
		}

		public char this [int offset] => GetCharAt (offset);
		public char GetCharAt (int offset) => fullText [offset];

		public string GetTextAt (int offset, int length)
		{
			if (offset < 0 || offset > totalLength || length < 0 || offset + length > totalLength)
				throw new ArgumentOutOfRangeException (nameof (offset));
			if (length == 0)
				return "";
			return fullText.Substring (offset, length);
		}

		public TextReader CreateReader () => new StringReader (Text);
		public TextReader CreateReader (int offset, int length) => new StringReader (GetTextAt (offset, length));
		public void WriteTextTo (TextWriter writer) => writer.Write (Text);
		public void WriteTextTo (TextWriter writer, int offset, int length) => writer.Write (GetTextAt (offset, length));

		public void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
		{
			fullText.CopyTo (sourceIndex, destination, destinationIndex, count);
		}

		public ITextSource CreateSnapshot () => this;
		public ITextSource CreateSnapshot (int offset, int length) => this;

		public int LocationToOffset (int line, int column)
		{
			if (line < 1 || line > lines.Length)
				throw new ArgumentOutOfRangeException (nameof (line));
			return startOffsets [line - 1] + (column - 1);
		}

		public DocumentLocation OffsetToLocation (int offset)
		{
			if (offset < 0)
				offset = 0;
			// Binary search: the last line whose start offset is <= offset.
			int lo = 1, hi = lines.Length, ans = 1;
			while (lo <= hi) {
				int mid = (lo + hi) / 2;
				if (startOffsets [mid - 1] <= offset) {
					ans = mid;
					lo = mid + 1;
				} else {
					hi = mid - 1;
				}
			}
			return new DocumentLocation (ans, offset - startOffsets [ans - 1] + 1);
		}

		public IDocumentLine GetLine (int lineNumber)
		{
			if (lineNumber < 1 || lineNumber > lines.Length)
				throw new ArgumentOutOfRangeException (nameof (lineNumber));
			int offset = startOffsets [lineNumber - 1];
			int len = lines [lineNumber - 1].Length;
			int delim = lineNumber < lines.Length ? 1 : 0;
			return new Line (this, lineNumber, offset, len, delim);
		}

		public IDocumentLine GetLineByOffset (int offset)
		{
			int clamped = Math.Clamp (offset, 0, Math.Max (0, totalLength - 1));
			return GetLine (OffsetToLocation (clamped).Line);
		}

		sealed class Line : IDocumentLine
		{
			readonly EditorTextDocumentAdapter doc;
			public Line (EditorTextDocumentAdapter doc, int num, int offset, int len, int delim)
			{
				this.doc = doc;
				LineNumber = num;
				Offset = offset;
				Length = len;
				DelimiterLength = delim;
			}

			public int Offset { get; }
			public int Length { get; }
			public int EndOffset => Offset + Length;
			public int LengthIncludingDelimiter => Length + DelimiterLength;
			public int EndOffsetIncludingDelimiter => Offset + LengthIncludingDelimiter;
			public ISegment SegmentIncludingDelimiter => new TextSegment (Offset, LengthIncludingDelimiter);
			public UnicodeNewline UnicodeNewline => DelimiterLength == 0 ? UnicodeNewline.Unknown : UnicodeNewline.LF;
			public int DelimiterLength { get; }
			public int LineNumber { get; }
			public IDocumentLine PreviousLine => LineNumber > 1 ? doc.GetLine (LineNumber - 1) : null;
			public IDocumentLine NextLine => LineNumber < doc.LineCount ? doc.GetLine (LineNumber + 1) : null;
		}
	}
}
