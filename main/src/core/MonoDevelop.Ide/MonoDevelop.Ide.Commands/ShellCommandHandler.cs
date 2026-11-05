#if AVALONIA_SHELL
//
// ShellCommandHandler.cs
//
// Oleada 2 bridge (AVALONIA_SHELL only): the ports of the legacy
// MonoDevelop.Ide.Commands handlers delegate the actual work here, to the
// shell's menu dispatcher (MainWindow.OnMenuCommand) — the same endpoint the
// Oleada 1 catalog router used — so every command keeps exactly the behavior
// the shell already implements for its catalog id. The command id is the one
// the CommandManager pipeline was invoked with (an enum member converted
// through CommandManager.ToCommandId, or a literal catalog id for commands
// whose id has no enum member).
//

namespace MonoDevelop.Ide.Commands
{
	static class ShellCommandHandler
	{
		public static void Run (object commandId, object dataItem = null)
		{
			var id = MonoDevelop.Components.Commands.CommandManager.ToCommandId (commandId) as string;
			if (id == null)
				return;
			MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.OnMenuCommand (id, dataItem as string);
		}
	}
}
#endif
