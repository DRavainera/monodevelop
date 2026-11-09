// 
// MessageBubbleCommands.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
#if AVALONIA_SHELL
using MonoDevelop.Ide.Services;
#endif
	

namespace MonoDevelop.Ide.Editor
{
	enum MessageBubbleCommands
	{
		Toggle,
		HideIssues,
		ToggleIssues
	}
	
#if !AVALONIA_SHELL
	class HideIssuesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Checked = IdeApp.Preferences.DefaultHideMessageBubbles;
		}
		
		protected override void Run (object data)
		{
			IdeApp.Preferences.DefaultHideMessageBubbles.Value = !IdeApp.Preferences.DefaultHideMessageBubbles.Value;
		}
	}
	
	class ToggleIssuesHandler : CommandHandler 
	{
		protected override void Run (object data)
		{
			Action action = data as Action;
			if (action != null)
				action ();
		}
		
		protected override void Update (CommandArrayInfo ainfo)
		{
			CommandInfo info = ainfo.Add (GettextCatalog.GetString ("E_rrors"), new Action (delegate {
				IdeApp.Preferences.ShowMessageBubbles.Value = ShowMessageBubbles.ForErrors;
				IdeApp.Preferences.DefaultHideMessageBubbles.Value = false;
			}));
			info.Checked = !IdeApp.Preferences.DefaultHideMessageBubbles && IdeApp.Preferences.ShowMessageBubbles.Value == ShowMessageBubbles.ForErrors;

			info = ainfo.Add (GettextCatalog.GetString ("_Errors and Warnings"), new Action (delegate {
				IdeApp.Preferences.ShowMessageBubbles.Value = ShowMessageBubbles.ForErrorsAndWarnings;
				IdeApp.Preferences.DefaultHideMessageBubbles.Value = false;
			}));
			info.Checked = !IdeApp.Preferences.DefaultHideMessageBubbles && IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrorsAndWarnings;
		}
	}
#else
	// Ronda 2 (2026-11-09): ports of the two handlers for the Avalonia shell.
	// The catalog resolves the defaultHandler type against the assembly that
	// loads it (CommandCatalogLoader.ResolveHandlerType — the shell assembly),
	// so this file is compiled into the shell csproj and the ports live here
	// under the same names. The shell has no IdeApp.Preferences: the two legacy
	// preferences are persisted in SettingsStore under the keys the shell
	// already reads (preferences dialog, dispatcher toggle), keeping the same
	// unset-default convention:
	//   'Monodevelop.ShowMessageBubbles' (bool, default true) — the negation of
	//   the legacy DefaultHideMessageBubbles (default false);
	//   'MonoDevelop.Ide.NewShowMessageBubbles' ("ForErrors" /
	//   "ForErrorsAndWarnings", default ForErrorsAndWarnings when unset) — the
	//   legacy ShowMessageBubbles preference.
	static class MessageBubbleState
	{
		public static bool HideAll {
			get => !SettingsStore.GetBool ("Monodevelop.ShowMessageBubbles", true);
			set => SettingsStore.SetBool ("Monodevelop.ShowMessageBubbles", !value);
		}
		
		public static string Mode {
			get => SettingsStore.GetString ("MonoDevelop.Ide.NewShowMessageBubbles") ?? "ForErrorsAndWarnings";
			set => SettingsStore.SetString ("MonoDevelop.Ide.NewShowMessageBubbles", value);
		}
		
		public static void ToggleHideAll ()
		{
			HideAll = !HideAll;
			ApplyAndRefresh ();
		}
		
		public static void SelectMode (string mode)
		{
			Mode = mode;
			HideAll = false;
			ApplyAndRefresh ();
		}
		
		// Legacy applied the preference to the editor right away (the
		// preference's Changed hook): mirror the persisted state onto the
		// active editor's BubbleMode and re-run the menu Update handlers so the
		// check glyphs follow the click.
		static void ApplyAndRefresh ()
		{
			var mode = HideAll
				? MonoDevelop.Ide.Controls.SkTextEditor.BubbleMode.Never
				: Mode == "ForErrors"
				? MonoDevelop.Ide.Controls.SkTextEditor.BubbleMode.ForErrors
				: MonoDevelop.Ide.Controls.SkTextEditor.BubbleMode.ForErrorsAndWarnings;
			MonoDevelop.AvaloniaShell.Views.MainWindow.Instance?.SetActiveEditorBubbleMode (mode);
			MonoDevelop.AvaloniaShell.Views.MenuBuilder.RefreshCommandStates ();
		}
	}
	
	class HideIssuesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Checked = MessageBubbleState.HideAll;
		}
		
		protected override void Run (object data)
		{
			MessageBubbleState.ToggleHideAll ();
		}
	}
	
	class ToggleIssuesHandler : CommandHandler 
	{
		protected override void Run (object data)
		{
			Action action = data as Action;
			if (action != null)
				action ();
		}
		
		protected override void Update (CommandArrayInfo ainfo)
		{
			CommandInfo info = ainfo.Add (GettextCatalog.GetString ("E_rrors"), new Action (delegate {
				MessageBubbleState.SelectMode ("ForErrors");
			}));
			info.Checked = !MessageBubbleState.HideAll && MessageBubbleState.Mode == "ForErrors";

			info = ainfo.Add (GettextCatalog.GetString ("_Errors and Warnings"), new Action (delegate {
				MessageBubbleState.SelectMode ("ForErrorsAndWarnings");
			}));
			info.Checked = !MessageBubbleState.HideAll && MessageBubbleState.Mode == "ForErrorsAndWarnings";
		}
	}
#endif
}
