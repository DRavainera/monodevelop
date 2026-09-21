using System;
using System.Collections.Generic;

namespace MonoDevelop.AvaloniaShell.Services;

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

	public static void Clear ()
	{
		history.Clear ();
		position = -1;
		Changed?.Invoke ();
	}

	/// <summary>Last up-to-count points oldest→newest with the current index (for the menu).</summary>
	public static (IReadOnlyList<NavigationPoint> Points, int Current) GetNavigationList (int count)
	{
		var start = Math.Max (0, history.Count - count);
		var points = history.GetRange (start, history.Count - start);
		return (points, position - start);
	}
}
