#if AVALONIA_SHELL
//
// ShellCommandState.cs
//
// Oleada 3 bridge (AVALONIA_SHELL only): a read-only view of the shell's
// document/workspace state, consumed by the ported MonoDevelop.Ide.Commands
// handlers' Update overrides to enable/disable menu items the way the legacy
// IdeApp.Workbench-based logic did. The shell keeps this state in its own types
// (MainWindow's document tabs / loaded solution path), so this is the single
// mapping point the handlers consult instead of the GTK-only IdeApp surface.
//

namespace MonoDevelop.Ide.Commands
{
	static class ShellCommandState
	{
		public static bool HasDocuments
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.HasDocuments ?? false;

		public static bool HasActiveDocument
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.HasActiveDocument ?? false;

		public static bool HasDirtyDocuments
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.HasDirtyDocuments ?? false;

		public static bool HasWorkspace
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.HasWorkspace ?? false;

		// Oleada 3 (slice 2): the selected document tab holds a text editor — the
		// shell equivalent of the legacy "the focused widget is an editable
		// Gtk.TextView/IEditable" check that gated Cut/Copy/Paste/Delete/SelectAll.
		public static bool HasActiveEditor
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.HasActiveEditor ?? false;

		// Oleada 3 (slice 2): the persisted recent-solutions list is non-empty —
		// gates "Clear Recent Solutions List" (legacy recent list count > 0).
		public static bool HasRecentSolutions
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.HasRecentSolutions ?? false;

		// Oleada 3 (slice 3): the number of open document tabs — the shell
		// equivalent of the legacy IdeApp.Workbench.Documents.Count that gated
		// Window Next/Previous Document (>= 2) and Open Document 1..9 (>= index).
		public static int DocumentCount
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.DocumentCount ?? 0;

		// Oleada 3 (slice 5): the shared build/run process is still alive — the
		// shell equivalent of the legacy CurrentBuildOperation/CurrentRunOperation
		// in-progress checks. Gates Stop (enabled while running) and the busy
		// guards of Build/Rebuild Solution, Run and Run Entry.
		public static bool IsOperationRunning
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.IsOperationRunning ?? false;

		// Oleada 3 (slice 6): the number of projects of the loaded solution — the
		// shell equivalent of the legacy "a build target is selected" presence
		// check (coarser bridge: the shell has no pad selection, so "the solution
		// has projects" stands in for CurrentSelectedBuildTarget != null).
		public static int ActiveProjectCount
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.ActiveProjectCount ?? 0;

		// Oleada 3 (slice 7): the active editor's undo/redo stacks are non-empty —
		// the shell equivalent of the legacy IUndoHandler.EnableUndo/EnableRedo on
		// the focused document's content. Gates the Undo/Redo menu items.
		public static bool CanUndo
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.CanUndo ?? false;

		public static bool CanRedo
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.CanRedo ?? false;

		// Oleada 3 (slice 8): the solution-pad selected node is a build target
		// (Solution/Project row) — the shell equivalent of the legacy
		// CurrentSelectedBuildTarget != null that gated the context-only Options
		// command (the pad menu "Properties").
		public static bool SelectedNodeIsBuildTarget
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.SelectedNodeIsBuildTarget ?? false;

		// Oleada 3 (slice 8): the solution-pad selected node is a project row —
		// the shell equivalent of the legacy CurrentSelectedProject is DotNetProject
		// that gated the context-only Add Reference command.
		public static bool SelectedNodeIsProject
			=> MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.SelectedNodeIsProject ?? false;
	}
}
#endif
