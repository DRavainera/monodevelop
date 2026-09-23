using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Builds the application menu with the same structure, order, labels, mnemonics,
/// icons, shortcuts and separators as the legacy GTK main menu:
///   - main/src/core/MonoDevelop.Ide/ExtensionModel/MainMenu.addin.xml (structure)
///   - main/src/core/MonoDevelop.Ide/ExtensionModel/Commands.addin.xml (labels/icons/shortcuts)
///   - main/src/addins/VersionControl/MonoDevelop.VersionControl/VersionControl.addin.xml (Versio_n Control menu)
/// Every menu entry of the legacy UI exists here in its original location, even when its
/// command still needs porting (those items surface "not yet ported" like the placeholder
/// panels, so functionality is not lost or hidden during the migration).
/// All Item() calls use named arguments (icon:/shortcut:/click:) so the meaning of each
/// parameter is explicit and cannot shift between overloads.
/// </summary>
public static class MenuService
{
	public record MenuEntry
	{
		public string Label = "";
		public string? Icon;
		public string? Shortcut;
		public bool IsSeparator;
		public bool IsHeader;
		public bool Disabled;
		public bool Checked;
		public List<MenuEntry> Children { get; } = new ();
		public Action? OnClick;

		// Legacy command id when the action is Command ("...") — used by the keyboard
		// dispatcher (MenuItem keyboard placement) and the KeyBindings preferences panel.
		public string? CommandId { get; set; }
	}

	public static IReadOnlyList<MenuEntry> BuildMainMenu (IReadOnlyList<string>? recentSolutions = null)
	{
		var menu = new List<MenuEntry> {
			BuildFile (recentSolutions),
			BuildEdit (),
			BuildView (),
			BuildSearch (),
			BuildProject (),
			BuildBuild (),
			BuildRun (),
			BuildVersionControl (),
			BuildToolsWithExternal (),
			BuildWindow (),
			BuildHelp (),
		};
		// gettext translations from the same catalogs the GTK UI loads
		// (build/locale/<lang>/LC_MESSAGES/monodevelop.mo).
		Translate (menu);
		return menu;
	}

	/// <summary>
	/// Fills MenuEntry.Shortcut (display + accelerator) and CommandId for every entry
	/// carrying a CommandAction. Precedence: Custom.kb.xml user binding → the legacy
	/// shortcut from Commands.addin.xml (already in the literal menu).
	/// </summary>
	public static void ApplyShortcuts (IReadOnlyList<MenuEntry> entries)
	{
		Dictionary<string, string>? userBindings = null;
		foreach (var e in entries) {
			if (e.Children.Count > 0) {
				ApplyShortcuts (e.Children);
				continue;
			}
			// OnClick is typed Action (implicit conversion); the CommandAction lives in
			// Delegate.Target, same as MenuBuilder recovers it.
			if (e.OnClick?.Target is CommandAction ca) {
				e.CommandId = ca.Id;
				userBindings ??= Services.SettingsStore.LoadKeyBindings ();
				if (userBindings.TryGetValue (ca.Id, out var custom) && !string.IsNullOrEmpty (custom))
					e.Shortcut = custom;
			}
		}
	}

	// Runs a command directly (keyboard dispatcher). Mirrors OnMenuCommand dispatch.
	public static void RunCommand (string commandId)
		=> MainWindow.Instance?.OnMenuCommand (commandId);

	static void Translate (List<MenuEntry> entries)
	{
		foreach (var e in entries) {
			if (e.Children.Count > 0)
				Translate (e.Children);
			e.Label = GettextService.T (e.Label);
		}
	}

	// ---------- File ----------
	static MenuEntry BuildFile (IReadOnlyList<string>? recentSolutions)
	{
		var recents = new List<MenuEntry> ();
		if (recentSolutions is { Count: > 0 }) {
			foreach (var path in recentSolutions)
				recents.Add (Item (System.IO.Path.GetFileNameWithoutExtension (path), click: Command ("recent:" + path)));
		} else {
			recents.Add (Item ("(Empty)", disabled: true));
		}
		recents.Add (Sep ());
		recents.Add (Item ("_Clear Recent Solutions List", click: Command ("MonoDevelop.Ide.Commands.FileCommands.ClearRecentProjects")));

		return new () {
			Label = "_File",
			Children = {
				Item ("New _File...", icon: "md-regular-file", shortcut: "Ctrl N", click: Command ("MonoDevelop.Ide.Commands.FileCommands.NewFile")),
				Item ("New _Solution...", icon: "md-new-solution", shortcut: "Ctrl Shift N", click: Command ("MonoDevelop.Ide.Commands.FileCommands.NewProject")),
				Sep (),
				Item ("_Open...", icon: "gtk-open", shortcut: "Ctrl O", click: Command ("MonoDevelop.Ide.Commands.FileCommands.OpenFile")),
				Sep (),
				Sub ("Recent _Files", new List<MenuEntry> {
					Item ("(Empty)", disabled: true),
					Sep (),
					Item ("_Clear Recent Files List", click: Command ("MonoDevelop.Ide.Commands.FileCommands.ClearRecentFiles")),
				}),
				Sub ("Recent Solu_tions", recents),
			Sep (),
			Item ("_Save", icon: "gtk-save", shortcut: "Ctrl S", click: Command ("MonoDevelop.Ide.Commands.FileCommands.Save")),
			Item ("Save _As...", click: Command ("MonoDevelop.Ide.Commands.FileCommands.SaveAs")),
			Item ("Save A_ll", icon: "md-save-all", shortcut: "Ctrl Shift S", click: Command ("MonoDevelop.Ide.Commands.FileCommands.SaveAll")),
			Item ("_Revert", click: Command ("MonoDevelop.Ide.Commands.FileCommands.ReloadFile")),
			Sep (),
			Item ("Page Set_up", click: Command ("MonoDevelop.Ide.Commands.FileCommands.PrintPageSetup")),
			Item ("Print Previe_w", click: Command ("MonoDevelop.Ide.Commands.FileCommands.PrintPreviewDocument")),
			Item ("_Print...", icon: "gtk-print", shortcut: "Ctrl P", click: Command ("MonoDevelop.Ide.Commands.FileCommands.PrintDocument")),
			Sep (),
			Item ("C_lose Workspace", icon: "md-close-combine-icon", shortcut: "Ctrl Alt W", click: Command ("MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace")),
			Item ("_Close", icon: "gtk-close", shortcut: "Ctrl W", click: Command ("MonoDevelop.Ide.Commands.FileCommands.CloseFile")),
			Sep (),
			Item ("_Quit", icon: "gtk-quit", shortcut: "Ctrl Q", click: Command ("MonoDevelop.Ide.Commands.FileCommands.Exit")),
		}
		};
	}

	// ---------- Edit ----------
	static MenuEntry BuildEdit () => new () {
		Label = "_Edit",
		Children = {
			Item ("_Undo", icon: "gtk-undo", shortcut: "Ctrl Z", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Undo")),
			Item ("_Redo", icon: "gtk-redo", shortcut: "Ctrl Shift Z", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Redo")),
			Sep (),
			Item ("Cu_t", icon: "gtk-cut", shortcut: "Ctrl X", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Cut")),
			Item ("_Copy", icon: "gtk-copy", shortcut: "Ctrl C", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Copy")),
			Item ("_Paste", icon: "gtk-paste", shortcut: "Ctrl V", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Paste")),
			Item ("_Delete", icon: "gtk-delete", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Delete")),
			Item ("Re_name...", shortcut: "F2", click: Command ("MonoDevelop.Ide.Commands.EditCommands.Rename")),
			Sep (),
			Item ("Select _All", icon: "md-select-all", shortcut: "Ctrl A", click: Command ("MonoDevelop.Ide.Commands.EditCommands.SelectAll")),
			Sub ("_Multiple Carets", new List<MenuEntry> {
				Item ("Insert next matching caret", shortcut: "Shift Alt .", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.InsertNextMatchingCaret")),
				Item ("Insert carets at all matching", shortcut: "Shift Alt ;", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.InsertAllMatchingCarets")),
				Item ("Remove last caret", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.RemoveLastSecondaryCaret")),
				Item ("Move last caret down", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.MoveLastCaretDown")),
				Item ("Rotate primary caret down", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.RotatePrimaryCaretNext")),
				Item ("Rotate primary caret up", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.RotatePrimaryCaretPrevious")),
			}),
			Item ("_Surround With...", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.ShowCodeSurroundingsWindow")),
			Sep (),
			Sub ("_Format", new List<MenuEntry> {
				Item ("_Format Document", shortcut: "Ctrl Shift Q", click: Command ("MonoDevelop.Ide.CodeFormatting.CodeFormattingCommands.FormatBuffer")),
				Sep (),
				Item ("_Indent", icon: "gtk-indent", shortcut: "Ctrl Alt End", click: Command ("MonoDevelop.Ide.Commands.EditCommands.IndentSelection")),
				Item ("_Unindent", icon: "gtk-unindent", shortcut: "Ctrl Alt Home", click: Command ("MonoDevelop.Ide.Commands.EditCommands.UnIndentSelection")),
				Sep (),
				Item ("Upper_case", click: Command ("MonoDevelop.Ide.Commands.EditCommands.UppercaseSelection")),
				Item ("_Lowercase", click: Command ("MonoDevelop.Ide.Commands.EditCommands.LowercaseSelection")),
				Sep (),
				Item ("_Toggle Line Comment(s)", icon: "md-comment", shortcut: "Ctrl Alt C", click: Command ("MonoDevelop.Ide.Commands.EditCommands.ToggleCodeComment")),
				Sep (),
				Item ("_Join Lines", click: Command ("MonoDevelop.Ide.Commands.EditCommands.JoinWithNextLine")),
				Item ("_Sort Lines", click: Command ("MonoDevelop.Ide.Commands.EditCommands.SortSelectedLines")),
				Sep (),
				Item ("_Remove Trailing Whitespace", click: Command ("MonoDevelop.Ide.Commands.EditCommands.RemoveTrailingWhiteSpaces")),
			}),
			Sub ("_Insert", new List<MenuEntry> {
				Item ("_Snippet...", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.ShowCodeTemplateWindow")),
				Item ("Standard _Header", click: Command ("MonoDevelop.Ide.Commands.EditCommands.InsertStandardHeader")),
				Sep (),
				Item ("_GUID (Globally Unique Identifier)", click: Command ("MonoDevelop.Ide.Commands.EditCommands.InsertGuid")),
			}),
			Sep (),
			Item ("Complete Word", shortcut: "Ctrl Space", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.ShowCompletionWindow")),
			Item ("Show Parameter List", shortcut: "Ctrl Shift Space", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.ShowParameterCompletionWindow")),
			Item ("Switch Completion/Suggestion Mode", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.ToggleCompletionSuggestionMode")),
			Sep (),
			Item ("Pr_eferences...", icon: "gtk-preferences", click: OpenPrefs ()),
			Item ("Po_licies...", click: Command ("MonoDevelop.Ide.Commands.EditCommands.DefaultPolicies")),
			Sep (),
		}
	};

	// ---------- View ----------
	static MenuEntry BuildView () => new () {
		Label = "_View",
		Children = {
			SepHeader ("Layout"),
			Item ("View List", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.ViewList")),
			Sep (),
			Item ("Layout List", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.LayoutList")),
			Sep (),
			Sub ("_Pads", new List<MenuEntry> {
				// One toggle per pad, same order/labels as Pads.addin.xml + addin pads;
				// MainWindow checks them against actual visibility (pad:<id> dispatch).
				Item ("Solution", isChecked: true, click: Command ("pad:solution")),
				Item ("Classes", click: Command ("pad:classes")),
				Item ("Help", click: Command ("pad:help")),
				Sep (),
				Item ("Toolbox", click: Command ("pad:toolbox")),
				Item ("Properties", isChecked: true, click: Command ("pad:properties")),
				Item ("Document Outline", click: Command ("pad:documentoutline")),
				Item ("Unit Tests", click: Command ("pad:unittests")),
				Sep (),
				Item ("Output", isChecked: true, click: Command ("pad:output")),
				Item ("Errors", click: Command ("pad:errors")),
				Item ("Tasks", click: Command ("pad:tasks")),
				Item ("Code Issues", click: Command ("pad:codeissues")),
				Item ("Search Results", click: Command ("pad:searchresults")),
				Sep (),
				Item ("Call Stack", click: Command ("pad:callstack")),
				Item ("Locals", click: Command ("pad:locals")),
				Item ("Watch", click: Command ("pad:watch")),
				Item ("Breakpoints", click: Command ("pad:breakpoints")),
				Item ("Threads", click: Command ("pad:threads")),
			}),
			Sep (),
			Item ("Save Curre_nt Layout...", icon: "gtk-add", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.NewLayout")),
			Item ("_Delete Current Layout", icon: "gtk-remove", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.DeleteCurrentLayout")),
			Sep (),
			Sub ("Editor Columns", new List<MenuEntry> {
				Item ("One Column", icon: "md-columns-one", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.SingleMode")),
				Item ("Two Columns", icon: "md-columns-two", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.SideBySideMode")),
			}),
			Sub ("Inline _Messages", new List<MenuEntry> {
				Item ("_None", click: Command ("MonoDevelop.Ide.Editor.MessageBubbleCommands.HideIssues")),
				Item ("Toggle Issues", click: Command ("MonoDevelop.Ide.Editor.MessageBubbleCommands.ToggleIssues")),
				Sep (),
				Item ("Hide Current Message", click: Command ("MonoDevelop.Ide.Editor.MessageBubbleCommands.Toggle")),
			}),
			Sub ("F_olding", new List<MenuEntry> {
				Item ("Enable _Folding", click: Command ("MonoDevelop.Ide.Commands.EditCommands.EnableDisableFolding"), isChecked: true),
				Sep (),
				Item ("_Toggle Fold", click: Command ("MonoDevelop.Ide.Commands.EditCommands.ToggleFolding")),
				Item ("Toggle _All Folds", click: Command ("MonoDevelop.Ide.Commands.EditCommands.ToggleAllFoldings")),
				Sep (),
				Item ("Toggle _Definitions", click: Command ("MonoDevelop.Ide.Commands.EditCommands.FoldDefinitions")),
			}),
			Sep (),
			Item ("_Zoom In", icon: "gtk-zoom-in", shortcut: "Ctrl +", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.ZoomIn")),
			Item ("Zoom _Out", icon: "gtk-zoom-out", shortcut: "Ctrl -", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.ZoomOut")),
			Item ("_Normal Size", icon: "gtk-zoom-100", shortcut: "Ctrl 0", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.ZoomReset")),
			Sep (),
			Item ("_Full Screen", icon: "gtk-fullscreen", shortcut: "F11", click: ToggleFull ()),
		}
	};

	// ---------- Search ----------
	static MenuEntry BuildSearch () => new () {
		Label = "_Search",
		Children = {
			Item ("_Find...", icon: "gtk-find", shortcut: "Ctrl F", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.Find")),
			Item ("_Replace...", icon: "gtk-find-and-replace", shortcut: "Ctrl H", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.Replace")),
			Sep (),
			Item ("Find _Next", icon: "md-find-next", shortcut: "Ctrl G", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.FindNext")),
			Item ("Find _Previous", icon: "md-find-prev", shortcut: "Ctrl Shift G", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.FindPrevious")),
			Item ("Find Next Like Selection", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.FindNextSelection")),
			Item ("Use Selection for Find", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.UseSelectionForFind")),
			Sep (),
			Item ("F_ind in Files...", shortcut: "Ctrl Shift F", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.FindInFiles")),
			Item ("R_eplace in Files...", shortcut: "Ctrl Shift H", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.ReplaceInFiles")),
			Sep (),
			Sub ("Bookmarks", new List<MenuEntry> {
				Item ("_Toggle Bookmark", icon: "md-bookmark-toggle", shortcut: "Ctrl F2", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.ToggleBookmark")),
				Sep (),
				Item ("Pre_vious", icon: "md-bookmark-prev", shortcut: "Shift F2", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.PrevBookmark")),
				Item ("Ne_xt", icon: "md-bookmark-next", shortcut: "F2", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.NextBookmark")),
				Sep (),
				Item ("_Remove All Bookmarks", icon: "md-bookmark-clear-all", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.ClearBookmarks")),
			}),
			Sub ("Go To", new List<MenuEntry> {
				Item ("_File...", shortcut: "Alt Shift O", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.GotoFile")),
				Item ("_Type...", shortcut: "Ctrl Shift T", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.GotoType")),
				Item ("_Line...", icon: "md-go-to-line", shortcut: "Ctrl I", click: Command ("MonoDevelop.Ide.Commands.SearchCommands.GotoLineNumber")),
				Sep (),
				Item ("_Cursor Position", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.CenterAndFocusCurrentDocument")),
				Item ("Matching _Brace", icon: "md-go-to-matching-brace", click: Command ("MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace")),
			}),
			Sep (),
			Item ("Show Previous", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.ShowPrevious")),
			Item ("Show Next", click: Command ("MonoDevelop.Ide.Commands.ViewCommands.ShowNext")),
			Sep (),
			Sub ("Navigation _History", new List<MenuEntry> {
				Item ("Navigate _Back", icon: "md-navigate-back", shortcut: "Ctrl Minus", click: Command ("MonoDevelop.Ide.Commands.NavigationCommands.NavigateBack")),
				Item ("Navigate _Forward", icon: "md-navigate-forward", shortcut: "Ctrl Shift Minus", click: Command ("MonoDevelop.Ide.Commands.NavigationCommands.NavigateForward")),
				Sep (),
				Item ("Navigate _History", click: Command ("MonoDevelop.Ide.Commands.NavigationCommands.NavigateHistory")),
				Sep (),
				Item ("_Clear Navigation History", click: Command ("MonoDevelop.Ide.Commands.NavigationCommands.ClearNavigationHistory")),
			}),
			Item ("Navigate To...", shortcut: "Ctrl ,", click: Command ("MonoDevelop.Components.MainToolbar.Commands.NavigateTo")),
		}
	};

	// ---------- Project ----------
	// Project > Active Configuration: one check item per solution configuration
	// (legacy SelectActiveConfigurationHandler; MainWindow substitutes the dynamic
	// children with the real configs and checked state on every BuildMenu).
	internal static string[]? DynamicActiveConfigs;
	internal static string? DynamicActiveConfig;

	static List<MenuEntry> ActiveConfigChildren ()
	{
		if (DynamicActiveConfigs is { Length: > 0 }) {
			var children = new List<MenuEntry> ();
			foreach (var c in DynamicActiveConfigs)
				children.Add (Item (
					c,
					isChecked: c == DynamicActiveConfig,
					click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.SelectActiveConfiguration:" + c)));
			return children;
		}
		return new List<MenuEntry> { Item ("Debug", isChecked: true), Item ("Release", disabled: true) };
	}

	static MenuEntry BuildProject () => new () {
		Label = "_Project",
		Children = {
			Item ("_Add Reference...", icon: "md-reference", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.AddReference")),
			Sep (),
			Item ("Custom command list", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.CustomCommandList")),
			Sep (),
			Item ("Run Code Analysis on Solution", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.RunCodeAnalysisSolution")),
			Item ("Run Code Analysis on Project", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.RunCodeAnalysisProject")),
			Sep (),
			Sub ("Active Configuration", ActiveConfigChildren (), autoHide: true),
			Sep (),
			Item ("Apply Policy...", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.ApplyPolicy")),
			Item ("Export Policy...", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.ExportPolicy")),
			Sep (),
			Item ("Se_t Startup Projects...", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.SetStartupProjects")),
			Item ("_Solution Options", icon: "gtk-preferences", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.SolutionOptions")),
			Item ("Project _Options", icon: "gtk-preferences", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.ProjectOptions")),
			Sep (),
			Item ("Convert Solution Format...", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.ExportSolution")),
		}
	};

	// ---------- Build ----------
	static MenuEntry BuildBuild () => new () {
		Label = "_Build",
		Children = {
			Item ("_Build All", shortcut: "F8", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.BuildSolution")),
			Item ("_Rebuild All", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.RebuildSolution")),
			Item ("_Clean All", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.CleanSolution")),
			Sep (),
			Item ("Buil_d", shortcut: "F7", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.Build")),
			Item ("R_ebuild", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.Rebuild")),
			Item ("C_lean", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.Clean")),
			Sep (),
			Item ("_Stop", icon: "gtk-stop", shortcut: "Shift F5", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.Stop")),
			Sep (),
		}
	};

	// ---------- Run ----------
	static MenuEntry BuildRun () => new () {
		Label = "_Run",
		Children = {
			Item ("Start Without Debugging", icon: "gtk-execute", shortcut: "Ctrl F5", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.Run")),
			Sub ("Run With", new List<MenuEntry> { Item ("(Default)", disabled: true) }, autoHide: true),
			Sep (),
			Item ("_Stop", icon: "gtk-stop", shortcut: "Shift F5", click: Command ("MonoDevelop.Ide.Commands.ProjectCommands.Stop")),
		}
	};

	// ---------- Version Control (inserted after Run, like VersionControl.addin.xml) ----------
	static MenuEntry BuildVersionControl () => new () {
		Label = "Versio_n Control",
		Children = {
			Item ("C_heckout...", icon: "vc-add-command", click: Command ("MonoDevelop.VersionControl.Commands.Checkout")),
			Item ("_Publish in Version Control...", icon: "vc-commit", click: Command ("MonoDevelop.VersionControl.Commands.Publish")),
			Sep (),
			Item ("_Update Solution", icon: "vc-update", click: Command ("MonoDevelop.VersionControl.Commands.UpdateSolution")),
			Item ("_Review Solution and Commit", icon: "vc-commit", click: Command ("MonoDevelop.VersionControl.Commands.SolutionStatus")),
			Sep (),
			Item ("_Add", icon: "vc-add-command", click: Command ("MonoDevelop.VersionControl.Commands.Add")),
			Item ("_Remove", icon: "vc-remove-command", click: Command ("MonoDevelop.VersionControl.Commands.Remove")),
			Item ("_Revert", icon: "vc-revert-command", click: Command ("MonoDevelop.VersionControl.Commands.Revert")),
			Item ("Lock", click: Command ("MonoDevelop.VersionControl.Commands.Lock")),
			Item ("Release Lock", click: Command ("MonoDevelop.VersionControl.Commands.Unlock")),
			Item ("Add to Ignore List", click: Command ("MonoDevelop.VersionControl.Commands.Ignore")),
			Item ("Remove from Ignore List", click: Command ("MonoDevelop.VersionControl.Commands.Unignore")),
			Sep (),
			Item ("_Diff", icon: "vc-diff", click: Command ("MonoDevelop.VersionControl.Commands.Diff")),
			Item ("_Log", icon: "vc-log", click: Command ("MonoDevelop.VersionControl.Commands.Log")),
			Item ("Authors", click: Command ("MonoDevelop.VersionControl.Commands.Annotate")),
		}
	};

	// ---------- Tools ----------
	// The legacy ToolService inserts one menu item per configured external tool
	// (MonoDevelop-tools.xml) before Preferences; each runs with tag expansion.
	static List<MenuEntry> ExternalToolItems ()
	{
		var items = new List<MenuEntry> ();
		foreach (var t in SettingsStore.LoadTools ())
			items.Add (Item (t.MenuCommand, icon: "md-execute", click: Command ("tool:" + t.MenuCommand)));
		return items;
	}

	static MenuEntry BuildTools () => new () {
		Label = "_Tools",
		Children = {
			Item ("_Extensions...", icon: "gtk-plugin", click: OpenAddins ()),
			Sep (),
			Sub ("Session Recorder", new List<MenuEntry> {
				Item ("Start Session Recorder", click: Command ("MonoDevelop.Ide.Commands.ToolCommands.ToggleSessionRecorder")),
				Item ("Replay Session...", icon: "gtk-go-forward", click: Command ("MonoDevelop.Ide.Commands.ToolCommands.ReplaySession")),
			}, autoHide: true),
			Sep (),
			Item ("Task List", icon: "md-task-list", click: Command ("MonoDevelop.Ide.Commands.ToolCommands.TaskList")),
			Item ("Tool List", click: Command ("MonoDevelop.Ide.Commands.ToolCommands.ToolList")),
			Item ("Edit Custom Tools...", click: Command ("MonoDevelop.Ide.Commands.ToolCommands.EditCustomTools")),
		}
	};

	// Builds the Tools menu with the external tool items inserted before Preferences
	// (legacy ToolService.AddMenuItems). Called from BuildMainMenu.
	internal static MenuEntry BuildToolsWithExternal ()
	{
		var tools = BuildTools ();
		var children = new List<MenuEntry> (tools.Children);
		var ext = ExternalToolItems ();
		if (ext.Count > 0) {
			// Insert before the Preferences item, with a separator, like the GTK menu.
			var prefIndex = children.FindIndex (c => c.Label.Contains ("Pr_eferences", StringComparison.Ordinal));
			var insertAt = prefIndex < 0 ? children.Count : prefIndex;
			children.Insert (insertAt, Sep ());
			children.InsertRange (insertAt + 1, ext);
		}
		tools.Children.Clear ();
		tools.Children.AddRange (children);
		return tools;
	}

	// ---------- Window ----------
	static MenuEntry BuildWindow () => new () {
		Label = "_Window",
		Children = {
			Item ("_Next Document", icon: "gtk-go-forward", shortcut: "Ctrl Page Down", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.NextDocument")),
			Item ("_Previous Document", icon: "gtk-go-back", shortcut: "Ctrl Page Up", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.PrevDocument")),
			Sep (),
			Item ("Close _All", shortcut: "Ctrl Shift W", click: Command ("MonoDevelop.Ide.Commands.FileCommands.CloseAllFiles")),
			Sep (),
			Item ("Welcome Page", icon: "gtk-home", click: Command ("cmd:welcome")),
			Sep (),
			Item ("Window List", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenWindowList")),
			Sep (),
			Item ("_1", shortcut: "Alt 1", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument1")),
			Item ("_2", shortcut: "Alt 2", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument2")),
			Item ("_3", shortcut: "Alt 3", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument3")),
			Item ("_4", shortcut: "Alt 4", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument4")),
			Item ("_5", shortcut: "Alt 5", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument5")),
			Item ("_6", shortcut: "Alt 6", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument6")),
			Item ("_7", shortcut: "Alt 7", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument7")),
			Item ("_8", shortcut: "Alt 8", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument8")),
			Item ("_9", shortcut: "Alt 9", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocument9")),
			Item ("Document List", click: Command ("MonoDevelop.Ide.Commands.WindowCommands.OpenDocumentList")),
		}
	};

	// ---------- Help ----------
	static MenuEntry BuildHelp () => new () {
		Label = "_Help",
		Children = {
			Item ("API Documentation", icon: "gtk-help", shortcut: "F1", click: Command ("MonoDevelop.Ide.Commands.HelpCommands.Help")),
			Sep (),
			Item ("MonoDevelop", click: () => OpenLink ("http://www.monodevelop.com")),
			Item ("Mono Project", click: () => OpenLink ("http://www.mono-project.com")),
			Sep (),
			Item ("Report Problem...", click: () => OpenLink ("http://xamar.in/r/file_studio_bug")),
			Sep (),
			Item ("Open Log Directory", icon: "md-open-folder", click: Command ("MonoDevelop.Ide.Commands.HelpCommands.OpenLogDirectory")),
			Item ("Instrumentation Monitor", click: Command ("MonoDevelop.Ide.Commands.ToolCommands.InstrumentationViewer")),
			Sub ("_Diagnostics", new List<MenuEntry> {
				Item ("Dump UI Tree", click: Command ("MonoDevelop.Ide.Commands.HelpCommands.DumpUITree")),
				Item ("Dump Accessibility Tree", click: Command ("MonoDevelop.Ide.Commands.HelpCommands.DumpA11yTree")),
				Item ("Dump Accessibility Tree (10s)", click: Command ("MonoDevelop.Ide.Commands.HelpCommands.DumpA11yTreeDelayed")),
				Item ("Mark Log", click: Command ("MonoDevelop.Ide.Commands.HelpCommands.MarkLog")),
			}),
			Sep (),
			Item ("_Check for Updates...", icon: "md-updates", click: Command ("MonoDevelop.Ide.Updater.UpdateCommands.CheckForUpdates")),
			Item ("_About", icon: "about-md-16", click: OpenAbout ()),
		}
	};

	// ---------- helpers ----------

	// "Not ported" placeholder action: reports through the output pad + status bar,
	// mirroring how unported panels surface a placeholder instead of hiding features.
	// Returns a CommandAction (implicitly convertible to Action) so MenuBuilder can
	// recover the command id for keyboard dispatch and the KeyBindings panel.
	static CommandAction Command (string id) => new (id);

	// helpers returning ready-to-use actions
	static Action OpenAbout () => () => MainWindow.Instance?.OnAboutMenu ();
	static Action OpenPrefs () => () => MainWindow.Instance?.OnPreferencesMenu ();
	static Action OpenAddins () => () => MainWindow.Instance?.OnAddinManagerMenu ();
	static Action ToggleFull () => () => MainWindow.Instance?.ToggleFullScreen ();
	static void OpenLink (string url)
	{
		try {
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo (url) { UseShellExecute = true });
		} catch (Exception ex) {
			Console.WriteLine ("[menu] open link failed: " + ex.Message);
		}
	}

	// Single Item factory: every call site passes icon:/shortcut:/click: by name so the
	// meaning of each argument can never shift positionally.
	static MenuEntry Item (string label, string? icon = null, string? shortcut = null, Action? click = null,
		bool disabled = false, bool isChecked = false)
		=> new () { Label = label, Icon = icon, Shortcut = shortcut, OnClick = click, Disabled = disabled, Checked = isChecked };

	static MenuEntry Sub (string label, List<MenuEntry> children, bool autoHide = false)
	{
		var entry = new MenuEntry {
			Label = label,
			Disabled = autoHide && children.TrueForAll (c => c.Disabled || c.IsSeparator),
		};
		entry.Children.AddRange (children);
		return entry;
	}

	static MenuEntry Sep () => new () { Label = "-", IsSeparator = true };

	static MenuEntry SepHeader (string label) => new () { Label = label, IsHeader = true, Disabled = true };
}

/// <summary>
/// Menu action bound to a legacy command id. The id is recovered by MenuBuilder
/// (delegate Target) for keyboard shortcut dispatch and editable key bindings.
/// </summary>
public sealed class CommandAction
{
	public string Id { get; }
	public Action Run { get; }

	public CommandAction (string id)
	{
		Id = id;
		Run = () => MainWindow.Instance?.OnMenuCommand (id);
	}

	public static implicit operator Action (CommandAction c) => c.Run;
}
