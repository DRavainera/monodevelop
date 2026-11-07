// NavigationCommands.cs
//
// Author:
//   Jeffrey Stedfast  <fejj@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Navigation;
#else
	using MonoDevelop.Components.Commands;
	using MonoDevelop.Ide.Services;
#endif

namespace MonoDevelop.Ide.Commands
{
	public enum NavigationCommands
	{
		NavigateBack,
		NavigateForward,
		NavigateHistory,
		ClearNavigationHistory
	}
	
#if !AVALONIA_SHELL
	internal class NavigateBack : CommandHandler
	{
		protected override void Run ()
		{
			IdeServices.NavigationHistoryService.MoveBack ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeServices.NavigationHistoryService.CanMoveBack;
		}
	}
	
	internal class NavigateForward : CommandHandler
	{
		protected override void Run ()
		{
			IdeServices.NavigationHistoryService.MoveForward ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeServices.NavigationHistoryService.CanMoveForward;
		}
	}
	
	internal class NavigateHistory : CommandHandler
	{
		protected override void Run (object ob)
		{
			NavigationHistoryItem nav = ob as NavigationHistoryItem;
			if (nav != null)
				IdeServices.NavigationHistoryService.MoveTo (nav);
		}
		
		protected override void Update (CommandArrayInfo info)
		{
			int currentIndex;
			IList<NavigationHistoryItem> points = IdeServices.NavigationHistoryService.GetNavigationList (15, out currentIndex);
			
			if (points.Count < 1) {
				Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					CommandInfo item = info.Add (doc.Window.Title, null);
					item.Checked = true;
				}
				return;
			}
			
			for (int i = points.Count - 1; i >= 0; i--) {
				CommandInfo item = info.Add (points[i].DisplayName, points[i]);
				item.Checked = (i == currentIndex);
			}
		}
	}
	
	internal class ClearNavigationHistory : CommandHandler
	{
		protected override void Run ()
		{
			IdeServices.NavigationHistoryService.Clear ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeServices.NavigationHistoryService.CanMoveForward || IdeServices.NavigationHistoryService.CanMoveBack;
		}
	}
#else
	// Oleada 2: AVALONIA_SHELL ports of the legacy handlers above. Each one
	// delegates to the shell's menu dispatcher (ShellCommandHandler) — the same
	// OnMenuCommand endpoint the catalog router uses. Oleada 3 (slice 11): the
	// Update overrides are ported on the shell's NavigationHistoryService
	// (MonoDevelop.Ide.Services, static — the legacy IdeServices service is
	// GTK-scoped); the dataItems are the "line|file" strings the shell bridge
	// carries (legacy NavigationHistoryItem, same file+line payload).
	internal class NavigateBack : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (NavigationCommands.NavigateBack);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.CanMoveBack;
		}
	}

	internal class NavigateForward : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (NavigationCommands.NavigateForward);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.CanMoveForward;
		}
	}

	internal class NavigateHistory : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (NavigationCommands.NavigateHistory, dataItem);
		}

		// Legacy Update(CommandArrayInfo): the last 15 points newest-first with
		// the current one checked; with an empty history the active document's
		// title stands in as the single checked entry (the master therefore
		// never renders an empty submenu).
		protected override void Update (CommandArrayInfo info)
		{
			var (points, currentIndex, start) = NavigationHistoryService.GetNavigationList (15);
			if (points.Count < 1) {
				var title = ShellCommandState.ActiveDocumentTitle;
				if (title != null) {
					CommandInfo item = info.Add (title, null);
					item.Checked = true;
				}
				return;
			}
			for (int i = points.Count - 1; i >= 0; i--) {
				// dataItem "navpoint:<fullIndex>|<line>|<file>" — fullIndex
				// (start + windowIndex) names the exact entry so the dispatch
				// resolves it like the legacy reference-based MoveTo; line and
				// file ride along for readability. The dispatch reads the
				// fullIndex (before the first '|').
				CommandInfo item = info.Add (points [i].ToString (), "navpoint:" + (start + i) + "|" + points [i].Line + "|" + (points [i].File ?? string.Empty));
				item.Checked = (i == currentIndex);
			}
		}
	}

	internal class ClearNavigationHistory : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (NavigationCommands.ClearNavigationHistory);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.CanMoveForward || NavigationHistoryService.CanMoveBack;
		}
	}
#endif
}
