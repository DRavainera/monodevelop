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
	}
}
#endif
