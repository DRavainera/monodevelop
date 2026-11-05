#if AVALONIA_SHELL
//
// CommandService.cs
//
// Non-GTK command service for the Avalonia shell (AVALONIA_SHELL): the single
// CommandManager instance the shell dispatches through, fed by the legacy
// command catalog (CommandCatalogLoader). It is the shell counterpart of the
// GTK IDE's IdeApp.CommandService (IdeApp.CommandManager) and keeps the
// activation pipeline (CommandActivating/CommandActivated) that the
// AutoTestService.SessionRecord record/replay built on it can hook.
//

namespace MonoDevelop.Components.Commands
{
	public static class CommandService
	{
		public static CommandManager Manager { get; } = new CommandManager ();

		public static bool CatalogLoaded { get; private set; }

		/// <summary>Commands registered from the catalog (0 before LoadCatalog).</summary>
		public static int LoadedCommands { get; private set; }

		public static int Count => Manager.Count;

		public static void LoadCatalog (string baseDir)
		{
			if (CatalogLoaded)
				return;
			LoadedCommands = CommandCatalogLoader.Load (Manager, baseDir);
			CatalogLoaded = true;
		}

		public static Command GetCommand (object commandId)
		{
			return Manager.GetCommand (commandId);
		}

		/// <summary>
		/// Dispatches a registered command through the manager pipeline;
		/// returns false for unknown command ids (the caller falls back to its
		/// legacy dispatcher, which also owns the synthetic ids like
		/// "recent:"/"pad:").
		/// </summary>
		public static bool Dispatch (object commandId, object dataItem, CommandSource source)
		{
			return Manager.DispatchCommand (commandId, dataItem, null, source);
		}
	}
}
#endif
