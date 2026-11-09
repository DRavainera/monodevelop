// ViewCommands.cs
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
using System.Linq;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.DockNotebook;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
#else
using MonoDevelop.Components.Commands;
#endif

namespace MonoDevelop.Ide.Commands
{
	public enum ViewCommands
	{
		ViewList,
		LayoutList,
		NewLayout,
		DeleteCurrentLayout,
		LayoutSelector,
		FullScreen,
		Open,
		TreeDisplayOptionList,
		ResetTreeDisplayOptions,
		RefreshTree,
		CollapseAllTreeNodes,
		OpenWithList,
		ShowNext,
		ShowPrevious,
		ZoomIn,
		ZoomOut,
		ZoomReset,
		FocusCurrentDocument,
		CenterAndFocusCurrentDocument,
		ShowWelcomePage
	}

#if !AVALONIA_SHELL
	// MonoDevelop.Ide.Commands.ViewCommands.ViewList
	public class ViewListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			string group;
			var lastListGroup = new Dictionary <CommandArrayInfo, string>();
			var descFormat = GettextCatalog.GetString ("Show {0}");
			foreach (Pad pad in IdeApp.Workbench.Pads.OrderBy (p => p.Group, StringComparer.InvariantCultureIgnoreCase)) {

				CommandInfo ci = new CommandInfo(pad.Title);
				ci.Icon = pad.Icon;
				ci.UseMarkup = true;
				ci.Description = string.Format (descFormat, pad.Title);
				// We only want these commands enabled if the main window is visible
				ci.Enabled = IdeApp.Workbench.RootWindow.Visible;

				ActionCommand cmd = IdeApp.CommandService.GetActionCommand ("Pad|" + pad.Id);
				if (cmd != null) ci.AccelKey = cmd.AccelKey; 

				CommandArrayInfo list = info;
				if (pad.Categories != null) {
					for (int j = 0; j < pad.Categories.Length; j++) {
						bool found = false;
						for (int k = list.Count - 1; k >= 0; k--) {
							if (list[k].Text == pad.Categories[j] && list[k] is CommandInfoSet) {
								list = ((CommandInfoSet)list[k]).CommandInfos;
								found = true;
								break;
							}
						}
						if (!found) {
							CommandInfoSet set = new CommandInfoSet();
							set.Text = pad.Categories[j];
							set.Description = string.Format (descFormat, set.Text);
							list.Add (set);
							list = set.CommandInfos;
						}
					}
				}

				int atIndex = 0;
				for (int j = list.Count - 1; j >= 0; j--) {
					if (!(list [j] is CommandInfoSet)) {
						atIndex = j + 1;
						break;
					}
				}

				list.Insert (atIndex, ci, pad);
				lastListGroup.TryGetValue (list, out group);
				if (group != pad.Group) {
					lastListGroup [list] = pad.Group;
					if (atIndex > 0) {
						CommandInfo sep = new CommandInfo ("-");
						sep.IsArraySeparator = true;
						list.Insert (atIndex, sep, null);
					}
				}
			}
		}

		protected override void Run (object dataItem)
		{
			if (dataItem == null) return; 
			Pad pad = (Pad)dataItem;
			pad.Visible = true;
			pad.BringToFront (true);

			Counters.PadShown.Inc (1, null, new Dictionary<string,object> {{ "Pad", pad.Id }});
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.LayoutList
	public class LayoutListHandler : CommandHandler
	{
		static internal readonly Dictionary<string, string> NameMapping;

		static LayoutListHandler ()
		{
			NameMapping = new Dictionary<string, string> ();
			NameMapping ["Solution"] = GettextCatalog.GetString ("Code");
			NameMapping ["Visual Design"] = GettextCatalog.GetString ("Design");
			NameMapping ["Debug"] = GettextCatalog.GetString ("Debug");
			NameMapping ["Unit Testing"] = GettextCatalog.GetString ("Test");
		}

		protected override void Update (CommandArrayInfo info)
		{
			string text;
			foreach (var name in IdeApp.Workbench.Layouts) {
				if (!NameMapping.TryGetValue (name, out text))
					text = name;
				CommandInfo item = new CommandInfo (text);
				item.Checked = IdeApp.Workbench.CurrentLayout == name;
				item.Description = GettextCatalog.GetString ("Switch to layout '{0}'", name);
				info.Add (item, name);
			}
		}

		protected override void Run (object dataItem)
		{
			IdeApp.Workbench.CurrentLayout = (string)dataItem; 
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.NewLayout
	public class NewLayoutHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Visible = IdeApp.Workbench.Visible;
		}

		protected override void Run ()
		{
			string newLayoutName = null;
			var dlg = new NewLayoutDialog();
			try {
				if (MessageService.RunCustomDialog (dlg) == (int)Gtk.ResponseType.Ok) 
					newLayoutName = dlg.LayoutName; 
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
			if (newLayoutName != null) {
				IdeApp.Workbench.CurrentLayout = newLayoutName;
			}
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.DeleteCurrentLayout
	public class DeleteCurrentLayoutHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (!IdeApp.Workbench.Visible) {
				info.Visible = false;
				return;
			}
			info.Enabled = !String.Equals ("Solution", IdeApp.Workbench.CurrentLayout, StringComparison.OrdinalIgnoreCase);
			string itemName;
			if (!LayoutListHandler.NameMapping.TryGetValue (IdeApp.Workbench.CurrentLayout, out itemName))
				itemName = IdeApp.Workbench.CurrentLayout;
			if (info.Enabled)
				info.Text = GettextCatalog.GetString ("_Delete \u201C{0}\u201D Layout", itemName);
			else
				info.Text = GettextCatalog.GetString ("_Delete Current Layout");
		}
		protected override void Run ()
		{
			string itemName;
			if (!LayoutListHandler.NameMapping.TryGetValue (IdeApp.Workbench.CurrentLayout, out itemName))
				itemName = IdeApp.Workbench.CurrentLayout;
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the \u201C{0}\u201D layout?", itemName), AlertButton.Delete)) {
				string clayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = "Solution";
				IdeApp.Workbench.DeleteLayout (clayout);
			}
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.FullScreen
	public class FullScreenHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (Platform.IsMac) {
				info.Text = IdeApp.Workbench.FullScreen
					? GettextCatalog.GetString ("Exit Full Screen")
					: GettextCatalog.GetString ("Enter Full Screen");
				info.Enabled = IdeApp.Workbench.RootWindow.Visible && !WelcomePage.WelcomePageService.WelcomeWindowVisible;
			} else if (Platform.IsWindows) {
				//this is currently a no-op on Windows as it's broken, so hide it
				info.Visible = info.Enabled = false;
			} else {
				info.Checked = IdeApp.Workbench.FullScreen;
			}
		}
		
		protected override void Run ()
		{
			IdeApp.Workbench.FullScreen = !IdeApp.Workbench.FullScreen;
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ShowNext
	public class ShowNextHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowNext ();
		}

		protected override void Update (CommandInfo info)
		{
			ILocationList list = IdeApp.Workbench.ActiveLocationList;
			if (list == null)
				info.Enabled = false;
			else
				info.Text = GettextCatalog.GetString ("Show Next ({0})", list.ItemName);
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ShowPrevious
	public class ShowPreviousHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowPrevious ();
		}

		protected override void Update (CommandInfo info)
		{
			ILocationList list = IdeApp.Workbench.ActiveLocationList;
			if (list == null)
				info.Enabled = false;
			else
				info.Text = GettextCatalog.GetString ("Show Previous ({0})", list.ItemName);
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ZoomIn
	public class ZoomIn : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null)
				info.Enabled = false;
			else {
				IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> (true);
				info.Enabled = zoom != null && zoom.EnableZoomIn;
			}
		}

		protected override void Run ()
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> (true);
			zoom.ZoomIn ();
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.ZoomOut
	public class ZoomOut : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null)
				info.Enabled = false;
			else {
				IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> (true);
				info.Enabled = zoom != null && zoom.EnableZoomOut;
			}
		}

		protected override void Run ()
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> (true);
			zoom.ZoomOut ();
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.ZoomReset
	public class ZoomReset : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null)
				info.Enabled = false;
			else {
				IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> (true);
				info.Enabled = zoom != null && zoom.EnableZoomReset;
			}
		}

		protected override void Run ()
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> (true);
			zoom.ZoomReset ();
		}
	}

	public class SideBySideModeHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Checked = DockNotebook.ActiveNotebook?.Container?.SplitCount > 0;
			info.Enabled = (DockNotebook.ActiveNotebook?.TabCount > 1 &&
			                DockNotebook.ActiveNotebook?.Container?.AllowRightInsert == true) || DockNotebook.ActiveNotebook?.Container?.SplitCount > 0;
		}

		protected override void Run ()
		{
			// Already in 2-column mode?
			if (DockNotebook.ActiveNotebook?.Container?.SplitCount > 0)
				return;
			
			IdeApp.Workbench.LockActiveWindowChangeEvent ();
			var container = DockNotebook.ActiveNotebook.Container;
			var tab = DockNotebook.ActiveNotebook.CurrentTab;
			var window = (SdiWorkspaceWindow)tab.Content;
			DockNotebook.ActiveNotebook.RemoveTab (tab.Index, false);
			container.InsertRight (window);
			window.SelectWindow ();
			IdeApp.Workbench.UnlockActiveWindowChangeEvent ();
		}
	}

	public class SingleModeHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Checked = DockNotebook.ActiveNotebook?.Container?.SplitCount < 1;
			info.Enabled = (DockNotebook.ActiveNotebook?.TabCount > 1 &&
			                DockNotebook.ActiveNotebook?.Container?.AllowRightInsert == true) || DockNotebook.ActiveNotebook?.Container?.SplitCount > 0;
		}

		protected override void Run ()
		{
			IdeApp.Workbench.LockActiveWindowChangeEvent ();

			SdiWorkspaceWindow window = null;
			if (DockNotebook.ActiveNotebook != null) {
				var tab = DockNotebook.ActiveNotebook.CurrentTab;
				if (tab != null)
					window = (SdiWorkspaceWindow)tab.Content;
			}
			DockNotebook.ActiveNotebook.Container.SetSingleMode ();

			if (window != null)
				window.SelectWindow ();

			IdeApp.Workbench.UnlockActiveWindowChangeEvent ();
		}
	}

	public class NextNotebookHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
			} else {
				var window = (SdiWorkspaceWindow)IdeApp.Workbench.ActiveDocument.Window;
				info.Enabled = window.CanMoveToNextNotebook ();
			}
		}

		protected override void Run ()
		{
			var window = (SdiWorkspaceWindow)IdeApp.Workbench.ActiveDocument.Window;
			window.MoveToNextNotebook ();
		}
	}

	public class PreviousNotebookHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
			} else {
				var window = (SdiWorkspaceWindow)IdeApp.Workbench.ActiveDocument.Window;
				info.Enabled = window.CanMoveToPreviousNotebook ();
			}
		}

		protected override void Run ()
		{
			var window = (SdiWorkspaceWindow)IdeApp.Workbench.ActiveDocument.Window;
			window.MoveToPreviousNotebook ();
		}
	}

	public class FocusCurrentDocumentHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<ITextView> () != null;
		}

		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.Select ();
			IdeApp.Workbench.ActiveDocument.Editor?.StartCaretPulseAnimation ();
		}
	}

	public class CenterAndFocusCurrentDocumentHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<ITextView> () != null;
		}

		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.Select ();
			if (IdeApp.Workbench.ActiveDocument.Editor != null) {
				IdeApp.Workbench.ActiveDocument.Editor.CenterToCaret ();
				IdeApp.Workbench.ActiveDocument.Editor.StartCaretPulseAnimation ();
			} else {
				var textView = IdeApp.Workbench.ActiveDocument.GetContent<ITextView> ();
				textView.ViewScroller.EnsureSpanVisible (new SnapshotSpan (textView.Caret.Position.BufferPosition, 0), EnsureSpanVisibleOptions.AlwaysCenter);
			}
		}
	}
#else
	// Oleada 2: AVALONIA_SHELL ports of the legacy handlers above. Each one
	// delegates to the shell's menu dispatcher (ShellCommandHandler) — the same
	// OnMenuCommand endpoint the catalog router uses — so behavior is
	// unchanged; the Update overrides (enabled/visible state) are not ported
	// yet. SideBySideMode/SingleMode/NextNotebook/PreviousNotebook forward
	// their literal catalog id: the ViewCommands enum has no members for them.
	public class ViewListHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ViewCommands.ViewList, dataItem);
		}

		// Ronda 3: el legacy ViewList inlineaba aquí la lista de pads (uno
		// checked por pad visible). En la shell Avalonia ese rol lo cubre el
		// submenu "Pads" (exclusivo de la shell, no existe en GTK), así que este
		// array queda deliberadamente vacío para no duplicar los pads.
	}

	public class LayoutListHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ViewCommands.LayoutList, dataItem);
		}

		// Ronda 3: port del legacy Update(CommandArrayInfo) — una entrada por
		// layout de workbench registrado (stock GTK: el único layout "Solution",
		// renombrado "Code" por el NameMapping legacy), checked el actual. La
		// shell tiene un único layout (el mapa de visibilidad de pads que
		// SaveCurrentLayout persiste), por lo que la lista es esa misma entrada
		// única checked; el click es un no-op como el legacy CurrentLayout =
		// "Solution" cuando ya es el actual.
		protected override void Update (CommandArrayInfo info)
		{
			var ci = info.Add ("Code", "layout:Solution");
			ci.Checked = true;
			ci.Description = "Switch to layout 'Solution'";
		}
	}

	public class NewLayoutHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.NewLayout);
		}
	}

	public class DeleteCurrentLayoutHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.DeleteCurrentLayout);
		}
	}

	public class FullScreenHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.FullScreen);
		}
	}

	public class ShowNextHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.ShowNext);
		}
	}

	public class ShowPreviousHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.ShowPrevious);
		}
	}

	public class ZoomIn : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.ZoomIn);
		}

		// Oleada 3 (slice 3): enabled when the active tab holds a zoomable editor
		// (legacy: ActiveDocument != null && content is IZoomable && EnableZoomIn).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveEditor;
		}
	}

	public class ZoomOut : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.ZoomOut);
		}

		// Oleada 3 (slice 3): same gate as ZoomIn (legacy IZoomable.EnableZoomOut).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveEditor;
		}
	}

	public class ZoomReset : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.ZoomReset);
		}

		// Oleada 3 (slice 3): same gate as ZoomIn (legacy IZoomable.EnableZoomReset).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveEditor;
		}
	}

	public class SideBySideModeHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ViewCommands.SideBySideMode");
		}
	}

	public class SingleModeHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ViewCommands.SingleMode");
		}
	}

	public class NextNotebookHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ViewCommands.NextNotebook");
		}
	}

	public class PreviousNotebookHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ViewCommands.PreviousNotebook");
		}
	}

	public class FocusCurrentDocumentHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.FocusCurrentDocument);
		}

		// Oleada 3 (slice 3): enabled when the active document has a text view
		// (legacy: ActiveDocument != null && GetContent<ITextView>() != null).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveEditor;
		}
	}

	public class CenterAndFocusCurrentDocumentHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ViewCommands.CenterAndFocusCurrentDocument);
		}

		// Oleada 3 (slice 3): same gate as FocusCurrentDocument.
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasActiveEditor;
		}
	}
#endif
}
