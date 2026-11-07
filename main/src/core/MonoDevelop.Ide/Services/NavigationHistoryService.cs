using System;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Navigation history service (legacy IdeServices.NavigationHistoryService):
/// a linear list of navigation points (file + caret line) with MoveBack/MoveForward
/// and CanMoveBack/CanMoveForward, exactly like the legacy CommandHandlers expect.
/// </summary>
public class NavigationPoint
{
	public string? File { get; init; }
	public int Line { get; init; } // 1-based

	public override string ToString () => $"{System.IO.Path.GetFileName (File)}:{Line}";
}

public static class NavigationHistoryService
{
	readonly static List<NavigationPoint> history = new ();
	static int position = -1;

	public const int MaxPoints = 100;

	public static bool CanMoveBack => position > 0;

	public static bool CanMoveForward => position >= 0 && position < history.Count - 1;

	public static event Action? Changed;

	/// <summary>Record a new point after a jump (truncates the forward branch).</summary>
	public static void Push (string? file, int line1Based)
	{
		// Same point repeated is ignored (legacy behavior).
		if (position >= 0 && history [position].File == file && history [position].Line == line1Based)
			return;
		if (position < history.Count - 1)
			history.RemoveRange (position + 1, history.Count - position - 1);
		history.Add (new NavigationPoint { File = file, Line = Math.Max (1, line1Based) });
		if (history.Count > MaxPoints)
			history.RemoveAt (0);
		position = history.Count - 1;
		Changed?.Invoke ();
	}

	public static NavigationPoint? MoveBack ()
	{
		if (!CanMoveBack)
			return null;
		position--;
		Changed?.Invoke ();
		return history [position];
	}

	public static NavigationPoint? MoveForward ()
	{
		if (!CanMoveForward)
			return null;
		position++;
		Changed?.Invoke ();
		return history [position];
	}

	/// <summary>Move the pointer to the point at the given full-history index —
	/// the NavigateHistory submenu pick. The legacy MoveTo resolves the exact
	/// clicked entry by reference; the shell's points carry no identity, so an
	/// entry is named by its index (the submenu encodes it in the dataItem).
	/// A no-op (already the current) fires no Changed, like MoveBack/MoveForward.
	/// Returns the point, or null when the index is out of range.</summary>
	public static NavigationPoint? MoveTo (int index)
	{
		if (index < 0 || index >= history.Count)
			return null;
		if (index == position)
			return history [index];
		position = index;
		Changed?.Invoke ();
		return history [index];
	}

	/// <summary>Caret feed (port of the legacy LogActiveDocument (true) on
	/// ITextView.Caret.PositionChanged): when the caret moves inside the file
	/// of the current point, that point's line is updated IN PLACE — the back
	/// point follows the caret without adding a point or truncating the forward
	/// branch (the legacy transient-point collapse, minus the 10 s promotion:
	/// the shell's points are all permanent from birth). A different file — or
	/// an empty history — records a new point like a jump/activation.
	/// Same-point moves fire no Changed.</summary>
	public static void FeedCaret (string? file, int line1Based)
	{
		int line = Math.Max (1, line1Based);
		if (position >= 0 && history [position].File == file) {
			if (history [position].Line != line) {
				history [position] = new NavigationPoint { File = file, Line = line };
				Changed?.Invoke ();
			}
			return;
		}
		Push (file, line);
	}

	public static void Clear ()
	{
		history.Clear ();
		position = -1;
		Changed?.Invoke ();
	}

	/// <summary>Up-to-count points balanced around the current one (oldest→newest),
	/// with the current's index in the window and the window's start offset in the
	/// full history. Mirrors the legacy HistoryList.GetList balance, so the current
	/// is always inside the window (its index never negative) and each entry maps
	/// back to its exact history index (start + windowIndex).</summary>
	public static (IReadOnlyList<NavigationPoint> Points, int Current, int Start) GetNavigationList (int count)
	{
		if (history.Count == 0)
			return (Array.Empty<NavigationPoint> (), -1, 0);
		int half = (count - 1) / 2;
		int start = Math.Max (0, Math.Min (position - half, history.Count - count));
		int len = Math.Min (count, history.Count - start);
		return (history.GetRange (start, len), position - start, start);
	}
}
