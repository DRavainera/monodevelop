// NewFileCommands.cs
//
// Author:
//   Carlo Kok (ck@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


#if !AVALONIA_SHELL
using System;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;
using System.IO;
using Gtk;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Desktop;
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Components.Extensions;
#else
using MonoDevelop.Components.Commands;
#endif

namespace MonoDevelop.Ide.Commands
{
	/// <summary>
	/// Copied from MonoDevelop.Ide.addin.xml
	/// </summary>
	public enum FileCommands
	{
		OpenFile,
		NewFile,
		Save,
		SaveAll,
		NewProject,
		NewWorkspace,
		CloseFile,
		CloseAllFiles,
		CloseWorkspace,
		CloseWorkspaceItem,
		ReloadFile,
		SaveAs,
		PrintDocument,
		PrintPreviewDocument,
		PrintPageSetup,
		RecentFileList,
		ClearRecentFiles,
		RecentProjectList,
		ClearRecentProjects,
		Exit,
		OpenInTerminal,
		OpenFolder,
		OpenContainingFolder,
		SetBuildAction,
		ShowProperties,
		CopyToOutputDirectory
	}

#if !AVALONIA_SHELL
	// MonoDevelop.Ide.Commands.FileCommands.OpenFile
	public class OpenFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dlg = new OpenFileDialog (GettextCatalog.GetString ("File to Open"), MonoDevelop.Components.FileChooserAction.Open) {
				TransientFor = MessageService.RootWindow,
				ShowEncodingSelector = true,
				ShowViewerSelector = true,
			};
			if (!dlg.Run ())
				return;
			
			var file = dlg.SelectedFile;
			
			if (dlg.SelectedViewer != null) {
				dlg.SelectedViewer.OpenFile (file, dlg.Encoding);
				return;
			}
			
			if (Services.ProjectService.IsWorkspaceItemFile (file) || Services.ProjectService.IsSolutionItemFile (file)) {
				WarnIfWorkspaceItemIsAlreadyOpen (file);
				IdeApp.Workspace.OpenWorkspaceItem (file, dlg.CloseCurrentWorkspace);
			}
			else
				IdeApp.Workbench.OpenDocument (file, null, dlg.Encoding, OpenDocumentOptions.DefaultInternal);
		}

		internal static bool WarnIfWorkspaceItemIsAlreadyOpen (FilePath file)
		{
			var item = IdeApp.Workspace.GetAllItems<WorkspaceItem> ().FirstOrDefault (w => w.FileName == file.FullPath);
			if (item != null) {
				if (IdeApp.IsInitialized)
					IdeApp.Workbench.StatusBar.ShowWarning (GettextCatalog.GetString ("{0} is already opened", item.FileName.FileName));
				return true;
			}
			return false;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.NewFile
	public class NewFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			using (var dlg = new NewFileDialog (null, null)) // new file seems to fail if I pass the project IdeApp.ProjectOperations.CurrentSelectedProject
				MessageService.ShowCustomDialog (dlg, IdeServices.DesktopService.GetFocusedTopLevelWindow ());
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.SaveAll
	public class SaveAllHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.SaveAll ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.Documents.Any (v => v.IsDirty);
		}
	}
	//MonoDevelop.Ide.Commands.FileCommands.NewProject
	public class NewProjectHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.NewSolution ().Ignore ();
		}
	}
	
	//MonoDevelop.Ide.Commands.FileCommands.NewWorkspace
	public class NewWorkspaceHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.NewSolution ("MonoDevelop.Workspace").Ignore ();
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseAllFiles
	public class CloseAllFilesHandler : CommandHandler
	{
		static bool isRunning;

		protected override async void Run ()
		{
			try {
				isRunning = true;
				await IdeApp.Workbench.CloseAllDocuments (false);
			} finally {
				isRunning = false;
			}
		}

		protected override void Update (CommandInfo info)
		{
			// No point in closing all when there are no documents open
			info.Enabled = !isRunning && IdeApp.Workbench.Documents.Count != 0;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseFile
	public class CloseFileHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.Close ().Ignore();
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace
	// MonoDevelop.Ide.Commands.FileCommands.CloseWorkspaceItem
	public class CloseWorkspaceHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workspace.Close ().Ignore();
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace != null && IdeApp.Workspace.Items.Count > 0;

			if (info.Enabled && IdeApp.Workspace.Items.Count == 1) {
				if (IdeApp.Workspace.Items [0] is Solution)
					info.Text = GettextCatalog.GetString ("C_lose Solution");
				else if (IdeApp.Workspace.Items [0] is Workspace)
					info.Text = GettextCatalog.GetString ("C_lose Workspace");
				else
					info.Text = GettextCatalog.GetString ("C_lose Project");
			} else
				info.Text = GettextCatalog.GetString ("C_lose All Solutions");

		}
	}
	// MonoDevelop.Ide.Commands.FileCommands.PrintDocument
	public class PrintHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.GetContent<IPrintable> (true).PrintDocument (PrintingSettings.Instance);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = CanPrint ();
		}
		
		internal static bool CanPrint ()
		{
			IPrintable print;
			return IdeApp.Workbench.ActiveDocument != null
				&& (print = IdeApp.Workbench.ActiveDocument.GetContent<IPrintable> ()) != null
				&& print.CanPrint;
		}
	}
	// MonoDevelop.Ide.Commands.FileCommands.PrintPreviewDocument
	public class PrintPreviewHandler : CommandHandler
	{
		protected override void Run ()
		{
			try {
				IdeApp.Workbench.ActiveDocument.GetContent<IPrintable> ().PrintPreviewDocument (PrintingSettings.Instance);
			} catch (Exception e) {
				LoggingService.LogError ("Error while generating the print preview", e);
				MessageService.ShowError (GettextCatalog.GetString ("Error while generating the print preview"), e.Message);
			}
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = PrintHandler.CanPrint ();

			//HACK: disable print preview on Win32 because it doesn't work
			if (Platform.IsWindows) {
				info.Enabled = false;
				info.Visible = false;
			}
		}
	}
	
	//FIXME: on GTK 2.18.x there is an option to integrate print preview with the print dialog, which makes this unnecessary
	// ideally we could only conditionally show it - this is why we handle the PrintingSettings here
	// MonoDevelop.Ide.Commands.FileCommands.PrintPageSetup
	public class PrintPageSetupHandler : CommandHandler
	{
		protected override void Run ()
		{
			var settings = PrintingSettings.Instance;
			settings.PageSetup = Gtk.Print.RunPageSetupDialog (IdeApp.Workbench.RootWindow, settings.PageSetup, 
			                                                   settings.PrintSettings);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = PrintHandler.CanPrint ();
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.RecentFileList
	public class RecentFileListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var files = IdeServices.DesktopService.RecentFiles.GetFiles ();
			if (files.Count == 0)
				return;
			
			int i = 0;
			var descFormat = GettextCatalog.GetString ("Open {0}");
			foreach (var ri in files) {
				string commandText = ri.DisplayName.Replace ("_", "__");
				if (!Platform.IsMac) {
					string acceleratorKeyPrefix = i < 10 ? "_" + ((i + 1) % 10).ToString() + " " : "";
					commandText = acceleratorKeyPrefix + commandText;
				}
				var cmd = new CommandInfo (commandText) {
					Description = string.Format (descFormat, ri.FileName)
				};
/*				Gdk.Pixbuf icon = IdeServices.DesktopService.GetIconForFile (ri.FileName, IconSize.Menu);
				#pragma warning disable 618
				if (icon != null)
					cmd.Icon = ImageService.GetStockId (icon, IconSize.Menu);
				#pragma warning restore 618*/
				info.Add (cmd, ri.FileName);
				i++;
			}
		}
		
		protected override void Run (object dataItem)
		{
			IdeApp.Workbench.OpenDocument ((string)dataItem, project: null);
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.ClearRecentFiles
	public class ClearRecentFilesHandler : CommandHandler
	{
		protected override void Run ()
		{
			try {
				string title = GettextCatalog.GetString ("Clear recent files");
				string question = GettextCatalog.GetString ("Are you sure you want to clear recent files list?");
				if (MessageService.GenericAlert (
					MonoDevelop.Ide.Gui.Stock.Question,
					title,
					question,
					AlertButton.No,
					AlertButton.Yes) == AlertButton.Yes) {
					IdeServices.DesktopService.RecentFiles.ClearFiles ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error clearing recent files list", ex);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeServices.DesktopService.RecentFiles.GetFiles ().Count > 0;
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.RecentProjectList
	public class RecentProjectListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var projects = IdeServices.DesktopService.RecentFiles.GetProjects ();
			if (projects.Count == 0)
				return;
				
			int i = 0;
			var solutionFormat = GettextCatalog.GetString ("Load solution {0}");
			var ctrlModText = GettextCatalog.GetString ("Hold Control to open in current workspace.");
			foreach (var ri in projects) {
				//getting the icon requires probing the file, so handle IO errors
				IconId icon;
				try {
					if (!File.Exists (ri.FileName))
						continue;

					icon = IdeServices.ProjectService.FileIsObjectOfType (ri.FileName, typeof(Solution)) ? "md-solution": "md-workspace";
				}
				catch (UnauthorizedAccessException exAccess) {
					LoggingService.LogWarning ("Error building recent solutions list (Permissions)", exAccess);
					continue;					
				}
				catch (IOException ex) {
					LoggingService.LogWarning ("Error building recent solutions list", ex);
					continue;
				}

				string commandText = ri.DisplayName.Replace ("_", "__");
				if (!Platform.IsMac) {
					string acceleratorKeyPrefix = i < 10 ? "_" + ((i + 1) % 10).ToString() + " " : "";
					commandText = acceleratorKeyPrefix + commandText;
				}

				string str = string.Format (solutionFormat, ri.ToString ());
				if (IdeApp.Workspace.IsOpen)
					str += " - " + ctrlModText;
				
				var cmd = new CommandInfo (commandText) {
					Icon = icon,
					Description = str,
				};
				
				info.Add (cmd, ri.FileName);
				i++;
			}
		}
		protected override void Run (object dataItem)
		{
			string filename = (string)dataItem;
			Gdk.ModifierType mtype = GtkWorkarounds.GetCurrentKeyModifiers ();
			bool inWorkspace = (mtype & Gdk.ModifierType.ControlMask) != 0;
			OpenFileHandler.WarnIfWorkspaceItemIsAlreadyOpen (filename);
			IdeApp.Workspace.OpenWorkspaceItem (filename, !inWorkspace);
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.ClearRecentProjects
	internal class ClearRecentProjectsHandler : CommandHandler
	{
		protected override void Run ()
		{
			try {
				string title = GettextCatalog.GetString ("Clear recent projects");
				string question = GettextCatalog.GetString ("Are you sure you want to clear recent projects list?");
				if (MessageService.GenericAlert (
					MonoDevelop.Ide.Gui.Stock.Question,
					title,
					question,
					AlertButton.No,
					AlertButton.Yes) == AlertButton.Yes) {
					IdeServices.DesktopService.RecentFiles.ClearProjects ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error clearing recent projects list", ex);
			}
		}
	
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeServices.DesktopService.RecentFiles.GetProjects ().Count > 0;
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.Exit
	public class ExitHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Exit ().Ignore();
		}
	}
	// MonoDevelop.Ide.Commands.FileTabCommands.CloseAllButThis    Implemented in FileTabCommands.cs
	// MonoDevelop.Ide.Commands.CopyPathNameHandler                Implemented in FileTabCommands.cs
	// MonoDevelop.Ide.Commands.FileTabCommands.ToggleMaximize     Implemented in FileTabCommands.cs
	// MonoDevelop.Ide.Commands.FileTabCommands.ReopenClosedTab    Implemented in FileTabCommands.cs
	// MonoDevelop.Ide.Commands.FileTabCommands.CloseAllExceptPinned    Implemented in FileTabCommands.cs
#else
	// Oleada 2: AVALONIA_SHELL ports of the legacy handlers above. Each one
	// delegates to the shell's menu dispatcher (ShellCommandHandler) — the same
	// OnMenuCommand endpoint the catalog router uses — so behavior is
	// unchanged; the Update overrides (enabled/visible state) are not ported
	// yet.
	// MonoDevelop.Ide.Commands.FileCommands.OpenFile
	public class OpenFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.OpenFile);
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.NewFile
	public class NewFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.NewFile);
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.SaveAll
	public class SaveAllHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.SaveAll);
		}

		// Oleada 3: enabled only when a document has unsaved changes (legacy
		// IdeApp.Workbench.Documents.Any(v => v.IsDirty)).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasDirtyDocuments;
		}
	}

	//MonoDevelop.Ide.Commands.FileCommands.NewProject
	public class NewProjectHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.NewProject);
		}
	}

	//MonoDevelop.Ide.Commands.FileCommands.NewWorkspace
	public class NewWorkspaceHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.NewWorkspace);
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseAllFiles
	public class CloseAllFilesHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.CloseAllFiles);
		}

		// Oleada 3: no point in closing all when there are no documents open (legacy
		// IdeApp.Workbench.Documents.Count != 0).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasDocuments;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseFile
	public class CloseFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.CloseFile);
		}

		// Oleada 3: enabled only when there is an active document (legacy
		// IdeApp.Workbench.ActiveDocument != null).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveDocument;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace
	public class CloseWorkspaceHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.CloseWorkspace);
		}

		// Oleada 3: enabled only when a solution/workspace is open (legacy
		// IdeApp.Workspace != null && Items.Count > 0). The dynamic label (Close
		// Solution vs Close All Solutions) is deferred to a later slice.
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.PrintDocument
	public class PrintHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.PrintDocument);
		}

		// Oleada 3 (slice 2): enabled when there is an active document to print
		// (legacy PrintHandler: ActiveDocument != null && GetContent<IPrintable>()).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveDocument;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.PrintPreviewDocument
	public class PrintPreviewHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.PrintPreviewDocument);
		}

		// Oleada 3 (slice 2): same gate as Print (legacy uses CanPrint()).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveDocument;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.PrintPageSetup
	public class PrintPageSetupHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.PrintPageSetup);
		}

		// Oleada 3 (slice 2): same gate as Print (legacy uses CanPrint()).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveDocument;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.RecentFileList
	public class RecentFileListHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (FileCommands.RecentFileList, dataItem);
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.ClearRecentFiles
	public class ClearRecentFilesHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.ClearRecentFiles);
		}

		// Oleada 3 (slice 2): the shell's "Recent Files" submenu is a static
		// "(Empty)" placeholder (it keeps no per-file recent store), so there is
		// never anything to clear — always disabled (legacy: RecentFiles count > 0).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = false;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.RecentProjectList
	public class RecentProjectListHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (FileCommands.RecentProjectList, dataItem);
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.ClearRecentProjects
	internal class ClearRecentProjectsHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.ClearRecentProjects);
		}

		// Oleada 3 (slice 2): enabled when the persisted recent-solutions list is
		// non-empty (legacy: recent projects count > 0).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasRecentSolutions;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.Exit
	public class ExitHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (FileCommands.Exit);
		}
	}
#endif
}
