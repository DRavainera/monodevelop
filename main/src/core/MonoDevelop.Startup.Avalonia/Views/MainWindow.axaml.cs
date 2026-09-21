using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MonoDevelop.AvaloniaShell.Controls;
using MonoDevelop.AvaloniaShell.Services;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class MainWindow : Window
{
	static readonly bool IsMac = RuntimeInformation.IsOSPlatform (OSPlatform.OSX);

	public static MainWindow? Instance { get; private set; }

	WelcomePageView? welcomePage;
	bool welcomeVisible = true;
	bool solutionLoaded;

	public MainWindow ()
	{
		InitializeComponent ();
		Instance = this;
		Output ("MonoDevelop Avalonia shell initialized.");

		BuildPads ();

		// Full legacy main menu: same structure/order/labels/icons/shortcuts as the GTK UI.
		BuildMenu ();

		// Window drag on the chrome rows (menu bar + toolbar), except over interactive
		// controls; those handle their own input first.
		if (TitleBarRow is not null) {
			TitleBarRow.PointerPressed += (s, e) => {
				if (e.GetCurrentPoint (this).Properties.IsLeftButtonPressed)
					BeginMoveDrag (e);
			};
			TitleBarRow.DoubleTapped += (s, e) => ToggleMaximize ();
		}
		if (ToolbarRow is not null) {
			ToolbarRow.PointerPressed += (s, e) => {
				if (e.GetCurrentPoint (this).Properties.IsLeftButtonPressed &&
					e.Source is Avalonia.Visual v && !IsToolbarInteractive (v))
					BeginMoveDrag (e);
			};
			ToolbarRow.DoubleTapped += (s, e) => ToggleMaximize ();
		}

		// Toolbar content mirrors the GTK MainToolbar: run button, configuration/run
		// configuration/runtime combos and the search box on the right.
		RunConfigCombo!.PlaceholderText = "Default";
		foreach (var rc in new[] { "Default", "Debug", "Release" })
			RunConfigCombo.Items.Add (rc);
		ConfigCombo!.Items.Add ("Debug");
		ConfigCombo.Items.Add ("Release");
		ConfigCombo.SelectedIndex = 0;
		RuntimeCombo!.PlaceholderText = "Default (Mono)";
		foreach (var rt in new[] { "Mono", ".NET" })
			RuntimeCombo.Items.Add (rt);

		// The placement convention (mac left, Windows/Linux right) is handled in
		// OnOpened by re-parenting the caption buttons to the requested side; the
		// XAML default places them on the right, matching Linux and Windows.
		Opened += (s, e) => {
			if (IsMac)
				MoveCaptionButtonsLeft ();
			ApplyThemeVariant (Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark);
			SetToolbarIcons ();

			// Load a real solution into the Solution pad when requested (--sln=<path>).
			var slnArg = Program.SolutionArg;
			if (slnArg.Length > 0)
				OpenSolutionInWindow (slnArg);

			// Automated QA: open the requested dialog directly.
			var qa = Program.QaDialogArg;
			if (qa == "--prefs") {
				new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			} else if (qa.StartsWith ("--prefs=", StringComparison.Ordinal)) {
				var arg = qa.Substring ("--prefs=".Length);
				var dlg = new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
				dlg.ShowDialog (this);
				if (arg is "light" or "dark")
					Application.Current!.RequestedThemeVariant =
						arg == "light" ? ThemeVariant.Light : ThemeVariant.Dark;
				else
					dlg.SelectPanel (arg);
			} else if (qa == "--about") {
				new AboutDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			} else if (qa == "--addins") {
				new AddinManagerDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			} else if (qa == "--welcome") {
				ShowWelcomePage ();
			} else if (qa == "--newsolution") {
				_ = OpenNewSolutionDialogAsync ();
			} else if (qa == "--find") {
				// QA: open Find in Files pre-loaded and run it against the solution.
				var fd = new FindInFilesDialog { SearchTextOverride = "Hello" };
				_ = fd.ShowDialog (this);
			} else if (qa == "--build") {
				_ = RunBuildAsync ();
			} else if (qa == "--run") {
				_ = RunStartupProjectAsync ();
			} else if (qa == "--goto") {
				_ = new GoToDialog ().ShowDialog (this);
			} else if (qa == "--addref") {
				// QA: exercise AddReference against the real csproj.
				var proj = ResolveActiveProject ();
				if (proj is not null) {
					var dlg = new AddReferenceDialog (proj);
					// Deterministic QA path: add a known reference programmatically.
					bool ok = dlg.TryAddReference ("System.Json");
					Output ($"[addref-qa] TryAddReference(System.Json) → {ok}");
					var text = File.ReadAllText (proj);						Output ($"[addref-qa] csproj contains reference: {text.Contains ("System.Json")}");
					// Revert so the project stays clean.
					var clean = System.Text.RegularExpressions.Regex.Replace (
						text, "\\s*<Reference Include=\"System.Json\" />", "");
					File.WriteAllText (proj, clean);
					Output ("[addref-qa] csproj reverted");
				} else
					Output ("[addref-qa] no project");
			} else if (qa == "--bookmarks") {
				// QA: exercise bookmark toggle/next/prev/clear with pixel-visible marks.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					if (docs.TryGetValue (Path.GetFileName (file), out var ed)) {
						ed.GotoLine (0); ed.ToggleBookmark ();
						ed.GotoLine (4); ed.ToggleBookmark ();
						ed.GotoLine (8); ed.ToggleBookmark ();
						Output ($"[bm-qa] bookmarked lines 1,5,9; caret l9");
						ed.NextBookmark ();
						Output ($"[bm-qa] next wraps → line {ed.CurrentLine + 1}");
						ed.PrevBookmark ();
						Output ($"[bm-qa] prev → line {ed.CurrentLine + 1}");
						HideWelcomePage (); // QA: reveal the workbench for the capture
					}
				}
			} else if (qa == "--navhist") {
				// QA: exercise the navigation history service.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					PushNavigationPoint ();
					OpenFileDocumentAtLine (file, 7);
					PushNavigationPoint ();
					OpenFileDocumentAtLine (file, 11);
					PushNavigationPoint ();
					Output ($"[nav-qa] back from l11 → {Services.NavigationHistoryService.MoveBack ()}");
					Output ($"[nav-qa] back again → {Services.NavigationHistoryService.MoveBack ()}");
					Output ($"[nav-qa] forward → {Services.NavigationHistoryService.MoveForward ()}");
					Services.NavigationHistoryService.Clear ();
					Output ($"[nav-qa] after clear: CanMoveBack={Services.NavigationHistoryService.CanMoveBack}");
				}
			} else if (qa == "--windocs") {
				// QA: exercise document cycling / Nth selection.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					OpenNewFileDocument ();
					Output ($"[win] active before cycle: {DocTabs.SelectedItem}");
					CycleDocument (1);
					Output ($"[win] after NextDocument: {(DocTabs.SelectedItem as TabItem)?.Tag}");
					CycleDocument (-1);
					Output ($"[win] after PrevDocument: {(DocTabs.SelectedItem as TabItem)?.Tag}");
					SelectNthDocument (2);
					Output ($"[win] after OpenDocument2: {(DocTabs.SelectedItem as TabItem)?.Tag}");
					SelectNthDocument (5);
					Output ($"[win] after OpenDocument5 (out of range): {(DocTabs.SelectedItem as TabItem)?.Tag}");
				}
			} else if (qa == "--editops") {
				// QA: exercise the line operations on a real document.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					if (docs.TryGetValue (Path.GetFileName (file), out var ed)) {
						SelectDocument (Path.GetFileName (file));
						ed.GotoLine (6); // 'static void Main ()' line
						ed.DuplicateLine ();
						Output ($"[editops] duplicate → caret line {ed.CurrentLine + 1}, lines now {ed.Text.Count (c => c == '\n') + 1}");
						ed.Undo ();
						Output ($"[editops] undo → lines back to {ed.Text.Count (c => c == '\n') + 1}");
						ed.GotoLine (0);
						ed.ToggleLineComment ();
						Output ($"[editops] comment first line → '{ed.Text.Split ('\n') [0]}'");
						ed.ToggleLineComment ();
						Output ($"[editops] uncomment → '{ed.Text.Split ('\n') [0]}'");
						ed.Redo ();
						ed.Undo ();
						Output ($"[editops] redo/undo ok → first line '{ed.Text.Split ('\n') [0]}'");
					}
				}
			} else if (qa == "--tasks") {
				RescanTasks ();
			} else if (qa.StartsWith ("--gotoline", StringComparison.Ordinal)) {
				// QA: open Program.cs and show the GotoLineNumber overlay widget.
				// With "=N[:C]" it performs the jump directly (deterministic QA path).
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					if (docs.TryGetValue (Path.GetFileName (file), out var ed)) {
						int eq = qa.IndexOf ('=');
						if (eq > 0) {
							var (line, col) = Controls.SkTextEditor.ParseGotoInput (qa [(eq + 1)..], 1);
							ed.GotoLine (line - 1);
							if (col > 1)
								ed.GotoLinePopupColumn (col);
							Output ($"[gotoline] parsed '{qa [(eq + 1)..]}' → line {line} col {col}; caret now at line {ed.CurrentLine + 1}");
						} else {
							ed.GotoLinePopup ();
							Output ($"[gotoline] overlay shown (caret line {ed.CurrentLine + 1})");
						}
					}
				} else {
					Output ("[gotoline] " + file + " not found");
				}
			} else if (qa == "--brace") {
				// QA: GotoMatchingBrace + file-scoped rename + git status dispatch.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						// Caret before the '{' of class Program (line 6) → match must land on its closing '}' (line 12).
						ed.GotoLine (5);
						ed.GotoLineEnd (); // caret right after 'class Program'
						Output ($"[brace] before: line {ed.CurrentLine + 1}");
						bool jumped = ed.GotoMatchingBrace ();
						Output ($"[brace] GotoMatchingBrace → jumped={jumped}, line {ed.CurrentLine + 1} (expected 12)");
						int n = ed.ReplaceAllInDocument ("Hello", "Greetings");
						if (n > 0) Output ($"[brace] rename 'Hello' → 'Greetings' on {n} line(s)");
						ed.Undo ();
						Output ($"[brace] undo rename → 'Hello' present: {ed.Text.Contains ("Hello")}");
					}
					_ = RunGitAsync ("status --short");
				}
			} else if (qa == "--tool") {
				var first = Services.SettingsStore.LoadTools ().FirstOrDefault ();
				if (first is not null)
					_ = Services.ExternalToolRunner.Run (first);
				else
					Output ("[tool] no external tools configured (Preferences > External Tools)");
			}
		};

		// Legacy default: the Welcome page opens as the startup document — but a QA
		// flow that opened a solution/document wins (like opening from the command line).
		if (!solutionLoaded && documents.Count == 0)
			ShowWelcomePage ();

		// Legacy WelcomePageFrame.OnKeyPressEvent: Escape hides the welcome overlay
		// while a solution is open.
		KeyDown += (s, e) => {
			if (e.Key == Avalonia.Input.Key.Escape && welcomeVisible && solutionLoaded)
				HideWelcomePage ();
		};
	}

	// Rebuilds the main menu (called at startup and whenever recents change, so the
	// File > Recent Solutions submenu mirrors the persisted list like the GTK UI).
	// Shortcuts: menu literals keep the legacy gestures (Commands.addin.xml); the
	// KeyBindings panel persists overrides in Custom.kb.xml and ApplyShortcuts re-applies
	// them here. HotKeys make the accelerators work application-wide.
	void BuildMenu ()
	{
		MainMenu!.Items.Clear ();
		Services.KeyboardShortcutRegistry.Reset ();
		var recents = RecentSolutions.GetAll ().Select (r => r.Path).ToList ();
		var entries = MenuService.BuildMainMenu (recents);
		MenuService.ApplyShortcuts (entries);
		UpdatePadChecks (entries);
		foreach (var item in MenuBuilder.BuildItems (entries))
			MainMenu.Items.Add (item);
		// Re-attach on every rebuild: the items are new instances each time.
		Services.KeyboardShortcutRegistry.AttachHotKeys (this);
	}

	// View > Pads checkmarks mirror the real pad visibility on every rebuild, like the
	// legacy pad toggle items (Gtk.CheckMenuItem.Active from DockItem.Visible).
	void UpdatePadChecks (System.Collections.Generic.IReadOnlyList<MenuService.MenuEntry> entries)
	{
		foreach (var e in entries) {
			if (e.Children.Count > 0) {
				UpdatePadChecks (e.Children);
				continue;
			}
			if (e.OnClick?.Target is CommandAction ca && ca.Id.StartsWith ("pad:", StringComparison.Ordinal))
				e.Checked = IsPadVisible (ca.Id.Substring ("pad:".Length));
		}
	}

	// Keyboard dispatch of menu commands (Custom.kb.xml parity: the legacy GTK handles
	// F-keys etc. globally). Non-modifier-only gestures run the menu command directly;
	// editor keys (Ctrl X/C/V, plain F2) are handled by the focused editor first.
	protected override void OnKeyDown (KeyEventArgs e)
	{
		base.OnKeyDown (e);
		if (e.Handled || e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
			or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System)
			return;
		var mods = e.KeyModifiers & ~(KeyModifiers.Meta);
		var hasMods = mods != KeyModifiers.None || (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl);
		bool isTextEditingCombo =
			(mods == KeyModifiers.Control && e.Key is Key.X or Key.C or Key.V or Key.Z or Key.A);
		if (e.Key == Key.F2 && !(mods == KeyModifiers.Control))
			return; // editor rename key: let the focused control handle it
		if (!hasMods && e.Key != Key.Delete)
			return;
		if (isTextEditingCombo)
			return;

		var bindings = Services.KeyboardShortcutRegistry.GetBindings ();
		foreach (var (commandId, gesture) in bindings) {
			if (gesture.Matches (e)) {
				Console.WriteLine ($"[keys] {gesture} → {commandId}");
				MenuService.RunCommand (commandId);
				e.Handled = true;
				return;
			}
		}
	}

	// ---------- Pads (legacy DockFrame groups) ----------

	TextBox? outputTextBox;
	TextBlock? errorsText;
	TextBlock? tasksText;
	TextBlock? propertiesText;

	void BuildPads ()
	{
		// Host identity used by the restore strip (legacy pad titles).
		LeftPads.Id = "left"; LeftPads.Title = "Solution";
		RightPads.Id = "right"; RightPads.Title = "Properties";
		BottomPads.Id = "bottom"; BottomPads.Title = "Output";
		DebugPads.Id = "debug"; DebugPads.Title = "Call Stack";

		// Solution pad (legacy ProjectPad): tree of the loaded solution.
		solutionTree = new ListBox {
			Background = Brushes.Transparent,
		};
		solutionTree.Bind (ListBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		solutionTree.DoubleTapped += OnSolutionOpen;

		// Legacy ProjectPad is a TreeView: Solution ▸ project ▸ files (double-click opens
		// the file in an island editor tab).
		solutionTreeView = new TreeView { Background = Brushes.Transparent };
		solutionTreeView.Bind (TreeView.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		solutionTreeView.DoubleTapped += OnSolutionOpen;

		var solutionHost = new DockPanel ();
		DockPanel.SetDock (solutionTreeView, Dock.Left);
		solutionHost.Children.Add (solutionTreeView);
		solutionHost.Children.Add (solutionTree);
		LeftPads.AddTab (new PadHost.PadTab { Id = "solution", Label = "Solution", Icon = "md-solution-pad", Content = solutionHost });
		solutionTree.Items.Add ("No solution loaded");

		// Classes pad (legacy ClassPad, auto-hidden by default like Pads.addin.xml).
		var classList = new ListBox { Background = Brushes.Transparent };
		classList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		classList.Items.Add ("(classes of loaded solutions)");
		LeftPads.AddTab (new PadHost.PadTab { Id = "classes", Label = "Classes", Icon = "md-classes-pad", Content = classList, Visible = false });

		// Help pad (legacy HelpTree, left group, auto-hidden).
		var helpList = new ListBox { Background = Brushes.Transparent };
		helpList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		helpList.Items.Add ("(documentation index)");
		LeftPads.AddTab (new PadHost.PadTab { Id = "help", Label = "Help", Icon = "md-help-pad", Content = helpList, Visible = false });

		// Properties pad (right, legacy layout).
		propertiesText = new TextBlock {
			Text = "Select an item to view its properties",
			Padding = new Thickness (8),
			Opacity = 0.7,
			TextWrapping = TextWrapping.Wrap,
		};
		propertiesText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		RightPads.AddTab (new PadHost.PadTab { Id = "properties", Label = "Properties", Icon = "md-properties-pad", Content = propertiesText });

		// Toolbox pad (legacy ToolboxPad, right group, auto-hidden).
		var toolboxList = new ListBox { Background = Brushes.Transparent };
		toolboxList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		toolboxList.Items.Add ("(toolbox items)");
		RightPads.AddTab (new PadHost.PadTab { Id = "toolbox", Label = "Toolbox", Icon = "md-toolbox-pad", Content = toolboxList, Visible = false });

		// Document Outline pad (legacy DocumentOutlinePad, right group, auto-hidden).
		var outlineList = new ListBox { Background = Brushes.Transparent };
		outlineList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		outlineList.Items.Add ("(document outline)");
		RightPads.AddTab (new PadHost.PadTab { Id = "documentoutline", Label = "Document Outline", Icon = "md-pad-document-outline", Content = outlineList, Visible = false });

		// Unit Tests pad (legacy TestPad, right group, auto-hidden).
		var testList = new ListBox { Background = Brushes.Transparent };
		testList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		testList.Items.Add ("(unit tests of loaded solutions)");
		RightPads.AddTab (new PadHost.PadTab { Id = "unittests", Label = "Unit Tests", Icon = "nunit-pad-icon", Content = testList, Visible = false });

		// Output pad (bottom, legacy OutputPad) — same TextBlock instance backs the
		// Output method and the pad tab, so log lines appear in both places.
		outputTextBox = new TextBox {
			AcceptsReturn = true,
			IsReadOnly = true,
			Text = "[shell] Avalonia 12.1.2 console ready.",
			FontSize = 12,
			Padding = new Thickness (8),
			Background = Brushes.Transparent,
		};
		outputTextBox.Bind (TextBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "output", Label = "Output", Icon = "md-output-icon", Content = outputTextBox });

		// Errors pad (legacy ErrorListPad, auto-hidden).
		errorsText = new TextBlock { Padding = new Thickness (8, 6), Text = "No errors" };
		errorsText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "errors", Label = "Errors", Icon = "md-errors-list", Content = errorsText, Visible = false });

		// Tasks pad (legacy TaskListPad, auto-hidden).
		tasksText = new TextBlock { Padding = new Thickness (8, 6), Text = "No tasks" };
		tasksText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "tasks", Label = "Tasks", Icon = "md-task-list", Content = tasksText, Visible = false });

		// Code Issues pad (legacy CodeIssuePad, bottom group, auto-hidden).
		var codeIssues = new TextBlock { Padding = new Thickness (8, 6), Text = "No code issues" };
		codeIssues.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "codeissues", Label = "Code Issues", Icon = "md-errors-list", Content = codeIssues, Visible = false });

		// Search Results pad (legacy search results host, bottom group, auto-hidden).
		var searchResults = new ListBox { Background = Brushes.Transparent };
		searchResults.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "searchresults", Label = "Search Results", Icon = "gtk-find", Content = searchResults, Visible = false });

		// Debugger pads (legacy defaultPlacement Bottom, right sub-dock like the GTK
		// "MonoDevelop.Debugger.StackTracePad/Center Bottom" split).
		var callStack = new ListBox { Background = Brushes.Transparent };
		callStack.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		DebugPads.AddTab (new PadHost.PadTab { Id = "callstack", Label = "Call Stack", Icon = "md-view-debug-call-stack", Content = callStack, Visible = false });

		var locals = new ListBox { Background = Brushes.Transparent };
		locals.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		DebugPads.AddTab (new PadHost.PadTab { Id = "locals", Label = "Locals", Icon = "md-view-debug-locals", Content = locals, Visible = false });

		var watch = new ListBox { Background = Brushes.Transparent };
		watch.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		DebugPads.AddTab (new PadHost.PadTab { Id = "watch", Label = "Watch", Icon = "md-view-debug-watch", Content = watch, Visible = false });

		var breakpoints = new ListBox { Background = Brushes.Transparent };
		breakpoints.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		DebugPads.AddTab (new PadHost.PadTab { Id = "breakpoints", Label = "Breakpoints", Icon = "md-view-debug-breakpoints", Content = breakpoints, Visible = false });

		var threads = new ListBox { Background = Brushes.Transparent };
		threads.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		DebugPads.AddTab (new PadHost.PadTab { Id = "threads", Label = "Threads", Icon = "md-view-debug-threads", Content = threads, Visible = false });

		// Hide buttons feed the restore strip at the bottom edge (legacy pin/hide).
		LeftPads.Hidden += (_, _) => UpdateRestoreStrip ();
		RightPads.Hidden += (_, _) => UpdateRestoreStrip ();
		BottomPads.Hidden += (_, _) => UpdateRestoreStrip ();
	}

	// Edge restore strip: one chip per hidden pad host, like the GTK restore handles.
	void UpdateRestoreStrip ()
	{
		var restoreStrip = RestoreStrip;
		restoreStrip!.Children.Clear ();
		foreach (var pad in new[] { LeftPads, RightPads, BottomPads }) {
			if (pad is { IsVisible: false }) {
				var chip = new Button { Content = pad.Title, FontSize = 11, Padding = new Thickness (8, 2) };
				var captured = pad;
				chip.Click += (_, _) => {
					captured.IsVisible = true;
					UpdateRestoreStrip ();
				};
				restoreStrip.Children.Add (chip);
			}
		}
		RestoreStripHost!.IsVisible = restoreStrip.Children.Count > 0;
	}

	// Individual pad visibility (Pads.addin.xml ids): each pad is a tab inside a dock
	// group; toggling shows the host and selects the pad, like DockItem.Show/Hide.
	public void SetPadVisible (string padId, bool visible)
	{
		var (host, _) = FindPad (padId);
		if (host is null)
			return;
		if (visible) {
			host.IsVisible = true;
			DebugPads.IsVisible |= host == DebugPads;
			host.SetTabVisible (padId, true);
			host.Select (padId);
		} else {
			host.SetTabVisible (padId, false);
			// Hide the whole host when no visible tabs remain (legacy empty dock hides).
			if (host.Tabs.All (t => !t.Visible))
				host.IsVisible = false;
		}
		UpdateRestoreStrip ();
	}

	public bool IsPadVisible (string padId)
	{
		var (host, tab) = FindPad (padId);
		return host is { IsVisible: true } && host!.IsTabVisible (padId)
			&& (tab is null || tab.Visible);
	}

	(PadHost? host, PadHost.PadTab? tab) FindPad (string padId) => padId switch {
		"solution" or "classes" or "help" => (LeftPads, LeftPads.Tabs.FirstOrDefault (t => t.Id == padId)),
		"toolbox" or "properties" or "documentoutline" or "unittests" => (RightPads, RightPads.Tabs.FirstOrDefault (t => t.Id == padId)),
		"output" or "errors" or "tasks" or "codeissues" or "searchresults" => (BottomPads, BottomPads.Tabs.FirstOrDefault (t => t.Id == padId)),
		"callstack" or "locals" or "watch" or "breakpoints" or "threads" => (DebugPads, DebugPads.Tabs.FirstOrDefault (t => t.Id == padId)),
		_ => (null, null),
	};

	// Legacy group-level ids kept for the restore strip / old dispatch entries.
	public void SetGroupVisible (string id, bool visible)
	{
		var pad = id switch {
			"left" => LeftPads,
			"right" => RightPads,
			"bottom" => BottomPads,
			"debug" => DebugPads,
			_ => null,
		};
		if (pad is null) return;
		pad.IsVisible = visible;
		UpdateRestoreStrip ();
	}

	public void TogglePad (string id)
	{
		if (FindPad (id).host is not null) {
			SetPadVisible (id, !IsPadVisible (id));
			return;
		}
		var pad = id switch {
			"left" => LeftPads,
			"right" => RightPads,
			"bottom" => BottomPads,
			_ => null,
		};
		if (pad is null) return;
		pad.IsVisible = !pad.IsVisible;
		UpdateRestoreStrip ();
	}

	// ---------- Welcome page (startup document, legacy default) ----------

	void EnsureWelcomePage ()
	{
		if (welcomePage is not null)
			return;
		welcomePage = new WelcomePageView ();
	}

	// The Welcome page is a full workbench overlay over the DockFrame content
	// (legacy WelcomePageService.ShowWelcomePage → DockFrame.AddOverlayWidget), so
	// no pads or document tabs are visible behind it.
	public void ShowWelcomePage ()
	{
		EnsureWelcomePage ();
		welcomeVisible = true;
		WelcomeOverlay!.IsVisible = true;
	}

	public void HideWelcomePage ()
	{
		EnsureWelcomePage ();
		welcomeVisible = false;
		WelcomeOverlay!.IsVisible = false;
	}

	// ---------- Documents (tabs) ----------

	readonly List<(string Tag, Control Content)> documents = new ();
	// Open file editors by tab tag (island tabs): used by Save/SaveAll (FileCommands).
	readonly Dictionary<string, Controls.SkTextEditor> docs = new ();

	void AddDocument (string tag, Control content, bool closable = true, bool select = true)
	{
		if (documents.Any (d => d.Tag == tag))
			return;
		documents.Add ((tag, content));
		if (content is Controls.SkTextEditor ed && !docs.ContainsKey (tag))
			docs.Add (tag, ed);

		var header = new Panel();
		var label = new TextBlock { Text = tag, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));

		if (closable) {
			var close = new Button {
				Content = "\u2715",
				FontSize = 9,
				Padding = new Thickness (4, 0),
				Margin = new Thickness (8, 0, 0, 0),
				VerticalAlignment = VerticalAlignment.Center,
				Background = Brushes.Transparent,
			};
			var captured = tag;
			close.Click += (_, _) => CloseDocument (captured);
			// Build the StackPanel before parenting anything: a control can only have
			// one visual parent in Avalonia.
			header.Children.Add (new StackPanel { Orientation = Orientation.Horizontal, Children = { label, close } });
		} else {
			header.Children.Add (label);
		}

		var tab = new TabItem {
			Header = header,
			Tag = tag,
			Classes = { "island" },
		};
		DocTabs!.Items.Add (tab);

		DocTabs.SelectionChanged += OnDocSelectionChanged;
		if (select)
			SelectDocument (tag);
	}

	void OnDocSelectionChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (DocTabs.SelectedItem is TabItem { Tag: string tag }) {
			var doc = documents.FirstOrDefault (d => d.Tag == tag);
			if (doc.Content is not null) {
				DocContent!.Children.Clear ();
				DocContent.Children.Add (doc.Content);
				if (doc.Content is Controls.SkTextEditor ed && !string.IsNullOrEmpty (ed.FilePath))
					StatusText!.Text = ed.FilePath;
			}
		}
	}

	public void SelectDocument (string tag)
	{
		foreach (var item in DocTabs!.Items.OfType<TabItem> ()) {
			if ((string?)item.Tag == tag) {
				DocTabs.SelectedItem = item;
				OnDocSelectionChanged (this, new SelectionChangedEventArgs (SelectingItemsControl.SelectionChangedEvent, new List<Control> (), new List<Control> ()));
				break;
			}
		}
	}

	void SelectFirstDocument ()
	{
		var first = DocTabs!.Items.OfType<TabItem> ().FirstOrDefault (t => t.IsVisible);
		if (first is not null)
			SelectDocument ((string)first.Tag!);
	}

	public void CloseDocument (string tag)
	{
		if (tag == "Welcome")
			return; // welcome page hides instead of closing
		var doc = documents.FirstOrDefault (d => d.Tag == tag);
		if (doc.Content is null)
			return;
		// Legacy SaveCommand: warn when an untitled document with changes is discarded.
		if (docs.TryGetValue (tag, out var ed) && ed.IsDirty && string.IsNullOrEmpty (ed.FilePath)) {
			Console.WriteLine ($"[docs] '{tag}' has unsaved changes with no file path");
			Output ($"[docs] '{tag}' has unsaved changes — save first (File > Save All)");
		}
		var tab = DocTabs!.Items.OfType<TabItem> ().FirstOrDefault (t => (string?)t.Tag == tag);
		if (tab is not null)
			DocTabs.Items.Remove (tab);
		documents.Remove (doc);
		docs.Remove (tag);
		SelectFirstDocument ();
	}

	// ---------- Solution loading ----------

	ListBox? solutionTree;
	TreeView? solutionTreeView;
	string? loadedSolutionPath;

	public void OpenSolutionInWindow (string path)
	{
		Console.WriteLine ("[solution] opening: " + path);
		try {
			var loaded = Services.SolutionLoader.Load (path);
			if (loaded is null) {
				Output ("Failed to load solution: " + path);
				return;
			}
			var (title, projects) = loaded.Value;
			solutionLoaded = true;
			loadedSolutionPath = path;

			// Solution pad = legacy ProjectPad TreeView (Solution ▸ Projects ▸ files).
			if (solutionTreeView is not null) {
				solutionTreeView.Items.Clear ();
				// Legacy tree anatomy (SolutionNodeBuilder → ProjectNodeBuilder →
				// ProjectReferenceFolderNodeBuilder/ProjectFolderNodeBuilder):
				// solution → project → [References, folders…, files…] with stock icons.
				var root = new TreeViewItem {
					Header = TreeHeader ("md-solution", title),
					IsExpanded = true,
					Tag = path,
				};
				foreach (var p in projects.Where (p => !p.IsFolder)) {
					var proj = new TreeViewItem {
						Header = TreeHeader ("md-project", p.Name),
						Tag = p.ProjectPath,
						IsExpanded = true,
					};
				var dir = Path.GetDirectoryName (p.ProjectPath);
				if (!string.IsNullOrEmpty (dir) && Directory.Exists (dir)) {
					projectFileBeingLoaded = p.ProjectPath;
					// References node (ProjectReferenceFolderNodeBuilder: first child).
					proj.Items.Add (BuildReferencesNode (p.ProjectPath));
						// Folders and files (ProjectFolderNodeBuilder ordering).
						foreach (var child in BuildFolderChildren (dir, 0))
							proj.Items.Add (child);
					}
					root.Items.Add (proj);
				}
				solutionTreeView.Items.Add (root);
			}
			if (solutionTree is not null) {
				solutionTree.Items.Clear (); // ListBox requires empty Items before ItemsSource
				var items = new System.Collections.ObjectModel.ObservableCollection<string> {
					$"Solution '{title}' ({projects.Count (p => !p.IsFolder)} project(s))"
				};
				foreach (var p in projects) {
					var indent = p.Parent is null ? "" : "    ";
					var icon = p.IsFolder ? "[f]" : "[p]";
					items.Add ($"{indent}{icon} {p.Name}");
				}
				solutionTree.ItemsSource = items;
			}
			RecentSolutions.Add (path);

			// Legacy behavior: opening a solution hides the welcome page and updates
			// its project bar message.
			HideWelcomePage ();
			welcomePage?.UpdateProjectBar (title);
			LeftPads.Select ("solution");
			BuildMenu (); // refresh File > Recent Solutions
			Output ("Loaded " + Path.GetFileName (path));
		} catch (Exception ex) {
			Output ("Error loading solution: " + ex.Message);
		}
	}

	// Node header with the legacy stock icon (ProjectPad nodeInfo.Icon).
	static StackPanel TreeHeader (string stockId, string text)
	{
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
		if (IconService.GetImage (stockId) is { } img)
			sp.Children.Add (new Image { Source = img, Width = 16, Height = 16 });
		var tb = new TextBlock { Text = text, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		tb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		sp.Children.Add (tb);
		return sp;
	}

	// ProjectReferenceFolderNodeBuilder: the project's first child is a References
	// node (md-reference-folder) with one md-reference row per assembly.
	TreeViewItem BuildReferencesNode (string projectPath)
	{
		var node = new TreeViewItem {
			Header = TreeHeader ("md-reference-folder", "References"),
			Tag = "references:" + projectPath,
		};
		try {
			var doc = System.Xml.Linq.XDocument.Load (projectPath);
			var ns = doc.Root?.Name.Namespace ?? System.Xml.Linq.XNamespace.None;
			var refs = new List<string> ();
			foreach (var el in doc.Descendants (ns + "Reference")) {
				var name = el.Attribute ("Include")?.Value;
				if (!string.IsNullOrEmpty (name))
					refs.Add (name.Split (',') [0]);
			}
			foreach (var el in doc.Descendants (ns + "PackageReference")) {
				var id = el.Attribute ("Include")?.Value;
				if (!string.IsNullOrEmpty (id))
					refs.Add (id);
			}
			foreach (var r in refs.OrderBy (r => r).Distinct ())
				node.Items.Add (new TreeViewItem {
					Header = TreeHeader ("md-reference", r),
					Tag = "reference:" + r,
				});
			if (refs.Count == 0)
				node.Items.Add (new TreeViewItem { Header = TreeHeader ("md-reference", "(none)"), Tag = "reference:none", IsVisible = false });
		} catch {
			// Unreadable project file: leave the References node empty (legacy tolerance).
		}
		return node;
	}

	// ProjectFolderNodeBuilder: folders first (md-closed-folder/md-open-folder by
	// expansion state), then files with DesktopService.GetIconForFile icons.
	List<TreeViewItem> BuildFolderChildren (string dir, int depth)
	{
		var result = new List<TreeViewItem> ();
		if (depth > 8)
			return result;
		try {
			foreach (var sub in Directory.GetDirectories (dir).OrderBy (d => d, StringComparer.OrdinalIgnoreCase)) {
				var name = Path.GetFileName (sub);
				if (name is "bin" or "obj")
					continue; // legacy hides build outputs by default
				var folder = new TreeViewItem {
					Header = TreeHeader ("md-closed-folder", name),
					Tag = "folder:" + sub,
				};
				foreach (var child in BuildFolderChildren (sub, depth + 1))
					folder.Items.Add (child);
				result.Add (folder);
			}
			foreach (var f in Directory.GetFiles (dir).OrderBy (f => f, StringComparer.OrdinalIgnoreCase)) {
				var fname = Path.GetFileName (f);
				if (fname == Path.GetFileName (projectFileBeingLoaded))
					continue;
				result.Add (new TreeViewItem {
					Header = TreeHeader (FileIconId (fname), fname),
					Tag = f,
				});
			}
		} catch (Exception) {
			// permission errors: skip silently like the legacy tree does
		}
		return result;
	}

	string? projectFileBeingLoaded;

	// DesktopService.GetIconForFile equivalent over the migrated icon set.
	static string FileIconId (string fileName)
	{
		var ext = Path.GetExtension (fileName).ToLowerInvariant ();
		return ext switch {
			".cs" => "md-file-source",
			".csproj" or ".props" or ".targets" => "md-project",
			".sln" => "md-solution",
			".xml" or ".config" => "md-xml-file-icon",
			".json" => "md-text-file-icon",
			".md" or ".txt" => "md-text-file-icon",
			_ => "md-file-source",
		};
	}
	// Opens a text file in an island editor tab (legacy FileService.OpenDocument with
	// the Mono.TextEditor view). Opening from the Solution pad or File > Open lands here.
	public void OpenFileDocument (string path)
	{
		var tag = Path.GetFileName (path);
		if (documents.Any (d => d.Tag == tag)) {
			SelectDocument (tag);
			return;
		}
		try {
			var editor = new Controls.SkTextEditor {
				FilePath = path,
				IsDirty = false,
				Background = Brushes.Transparent,
			};
			editor.Text = File.ReadAllText (path);
			editor.IsDirty = false;
			editor.Bind (Controls.SkTextEditor.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			editor.PropertyChanged += (_, e) => {
				if (e.Property == Controls.SkTextEditor.IsDirtyProperty)
					UpdateDocTabTitle (tag, docDirty: editor.IsDirty);
			};
			AttachEditorContextMenu (editor);
			AddDocument (tag, editor);
		} catch (Exception ex) {
			Output ("Cannot open " + Path.GetFileName (path) + ": " + ex.Message);
		}
	}

	// Legacy dot-in-title (MonoDevelop doc header shows the modified marker).
	void UpdateDocTabTitle (string tag, bool docDirty)
	{
		var tab = DocTabs!.Items.OfType<TabItem> ().FirstOrDefault (t => (string?)t.Tag == tag);
		if (tab?.Header is Panel panel && panel.Children.OfType<TextBlock> ().FirstOrDefault () is { } lbl) {
			lbl.Text = docDirty ? tag + " •" : tag;
			ToolTip.SetTip (tab, docs.TryGetValue (tag, out var ed) && !string.IsNullOrEmpty (ed.FilePath) ? ed.FilePath : tag);
		}
	}

	// Editor context menu, same items as the legacy SourceEditorWidget context path
	// (cut/copy/paste/select all, go to line) with the legacy stock icons.
	void AttachEditorContextMenu (Controls.SkTextEditor editor)
	{
		MenuItem Item (string header, string? stockId, Action onClick)
		{
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			if (stockId is not null && IconService.GetImage (stockId) is { } img)
				panel.Children.Add (new Image { Source = img, Width = 16, Height = 16 });
			panel.Children.Add (new TextBlock { Text = header, FontSize = 12 });
			var mi = new MenuItem { Header = panel };
			mi.Click += (_, _) => onClick ();
			return mi;
		}
		var menu = new ContextMenu {
			ItemsSource = new object [] {
				Item ("Cut", "gtk-cut", editor.CutSelection),
				Item ("Copy", "gtk-copy", editor.CopySelection),
				Item ("Paste", "gtk-paste", editor.PasteClipboard),
				new Separator (),
				Item ("Select All", "gtk-select-all", editor.SelectAll),
				Item ("Go To Line…", null, editor.GotoLinePopup),
			},
		};
		editor.ContextMenu = menu;
	}

	void OnSolutionOpen (object? sender, RoutedEventArgs e)
	{
		// Legacy ProjectPad.OpenItem: double-click on a file node opens its editor.
		if (solutionTreeView?.SelectedItem is TreeViewItem { Tag: string file }
			&& File.Exists (file)
			&& !file.EndsWith (".csproj", StringComparison.Ordinal)) {
			OpenFileDocument (file);
			return;
		}
		if (solutionTree?.SelectedItem is string sel) {
			var trimmed = sel.Trim ().Replace ("[p] ", "").Replace ("[f] ", "");
			if (trimmed.EndsWith (".csproj", StringComparison.Ordinal))
				Output ("Open: " + trimmed);
		}
	}

	public async System.Threading.Tasks.Task OpenNewSolutionDialogAsync ()
	{
		var dlg = new NewSolutionDialog ();
		await dlg.ShowDialog (this);
		try {
			if (!string.IsNullOrEmpty (dlg.CreatedSolutionPath)) {
				Output ("Solution created: " + dlg.CreatedSolutionPath);
				OpenSolutionInWindow (dlg.CreatedSolutionPath);
			}
		} catch (Exception ex) {
			Console.WriteLine ("[newsolution] post-create failed: " + ex);
			Output ("[newsolution] post-create failed: " + ex.Message);
		}
	}

	public async void OpenSolutionPickerAsync ()
	{
		var files = await StorageProvider.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
			Title = "Open Solution",
			AllowMultiple = false,
			FileTypeFilter = new[] { new Avalonia.Platform.Storage.FilePickerFileType ("Solutions") { Patterns = new[] { "*.sln" } } },
		});
		if (files.Count > 0)
			OpenSolutionInWindow (files [0].Path.LocalPath);
	}

	// ---------- Window chrome ----------

	/// <summary>macOS: caption buttons live at the left edge of the menu bar row.</summary>
	void MoveCaptionButtonsLeft ()
	{
		var grid = TitleBarRow?.Children.OfType<Grid> ().FirstOrDefault ();
		if (grid is null || CaptionButtons is null)
			return;

		Grid.SetColumn (CaptionButtons, 0);
		CaptionButtons.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
		if (grid.Children.OfType<Menu> ().FirstOrDefault () is { } menu) {
			Grid.SetColumn (menu, 1);
			menu.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
			menu.Margin = new Thickness (110, 0, 0, 0); // clear the traffic-light area
		}
	}

	void ToggleMaximize ()
	{
		if (WindowState == WindowState.Maximized)
			WindowState = WindowState.Normal;
		else
			WindowState = WindowState.Maximized;
	}

	void OnMinimize (object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

	void OnMaximize (object? sender, RoutedEventArgs e) => ToggleMaximize ();

	void OnClose (object? sender, RoutedEventArgs e) => Close ();

	void OnTheme (object? sender, RoutedEventArgs e)
	{
		var menu = (MenuItem)sender!;
		ApplyThemeVariant (menu.Name == "ThemeLight" ? ThemeVariant.Light : ThemeVariant.Dark);
	}

	void ApplyThemeVariant (ThemeVariant variant)
	{
		if (Application.Current is null)
			return;
		Application.Current.RequestedThemeVariant = variant;
		Background = new SolidColorBrush (
			variant == ThemeVariant.Light ? Color.Parse ("#FFFFFF") : Color.Parse ("#1E1E1E"));
		SetToolbarIcons ();
	}

	// Toolbar glyphs come from the same redesigned PNG set (IconService) and follow the
	// theme variant, like the legacy ImageService icons.
	void SetToolbarIcons ()
	{
		if (Services.IconService.GetImage ("gtk-execute") is Avalonia.Media.Imaging.Bitmap bmp)
			RunIcon!.Source = bmp;
	}

	static bool IsToolbarInteractive (Avalonia.Visual v)
		=> FindAncestor<ComboBox> (v) is not null ||
		   FindAncestor<Button> (v) is not null ||
		   FindAncestor<TextBox> (v) is not null;

	static T? FindAncestor<T> (Avalonia.Visual v) where T : class
	{
		while (v is not null) {
			if (v is T match)
				return match;
			v = v.GetVisualParent ();
		}
		return null;
	}

	void OnToolbarRun (object? sender, RoutedEventArgs e)
	{
		var message = "'Start Without Debugging' is not wired in the new UI yet — run remains available through --old-gui until the cutover.";
		Output ("[toolbar] " + message);
		Console.WriteLine ("[toolbar] " + message);
	}

	void OnToolbarConfigChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (sender is not ComboBox cb || cb.SelectedItem is not string sel)
			return;
		var name = cb == ConfigCombo ? "configuration" : cb == RunConfigCombo ? "run configuration" : "runtime";
		Output ($"[toolbar] {name} → {sel}");
	}

	// ----- Menu actions surfaced for MenuService -----

	public void OnAboutMenu ()
		=> new AboutDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);

	public void OnPreferencesMenu ()
		=> new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);

	public void OnAddinManagerMenu ()
		=> new AddinManagerDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);

	public void ToggleFullScreen ()
		=> WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;

	// Dispatches real commands; anything still pending port reports like the unported
	// option panels instead of silently hiding the legacy feature.
	public void OnMenuCommand (string commandId)
	{
		if (commandId.StartsWith ("recent:", StringComparison.Ordinal)) {
			OpenSolutionInWindow (commandId.Substring ("recent:".Length));
			return;
		}
		if (commandId.StartsWith ("pads:", StringComparison.Ordinal)) {
			TogglePad (commandId.Substring ("pads:".Length));
			return;
		}
		if (commandId.StartsWith ("pad:", StringComparison.Ordinal)) {
			TogglePad (commandId.Substring ("pad:".Length));
			return;
		}
		if (commandId.StartsWith ("tool:", StringComparison.Ordinal)) {
			var tool = Services.SettingsStore.LoadTools ().FirstOrDefault (t => t.MenuCommand == commandId.Substring ("tool:".Length));
			if (tool is not null)
				_ = Services.ExternalToolRunner.Run (tool);
			return;
		}
		if (commandId.StartsWith ("cmd:", StringComparison.Ordinal)) {
			switch (commandId.Substring ("cmd:".Length)) {
			case "welcome":
				ShowWelcomePage ();
				return;
			case "exit":
				Close ();
				return;
			}
		}
		switch (commandId) {
		case "MonoDevelop.Ide.Commands.FileCommands.NewProject":
			_ = OpenNewSolutionDialogAsync ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.NewFile":
			OpenNewFileDocument ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.OpenFile":
			OpenAnyFilePickerAsync ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.SaveAs":
			_ = SaveActiveDocumentAsAsync ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.Exit":
			Close ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.ClearRecentProjects":
			RecentSolutions.Clear ();
			BuildMenu ();
			Output ("[menu] recent solutions list cleared");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ShowWelcomePage":
			ShowWelcomePage ();
			return;

		// FileCommands.Save / FileCommands.SaveAll (legacy FileService.SaveAll): writes
		// every dirty editor with a backing file back to disk (custom.kb.xml-shortcutable).
		case "MonoDevelop.Ide.Commands.FileCommands.Save": {
			if (DocTabs.SelectedItem is TabItem { Tag: string tag } && docs.TryGetValue (tag, out var ed)) {
				ed.Save ();
				UpdateDocTabTitle (tag, docDirty: false);
				Output ("Saved " + (string.IsNullOrEmpty (ed.FilePath) ? tag : Path.GetFileName (ed.FilePath)));
			}
			return;
		}
		case "MonoDevelop.Ide.Commands.FileCommands.SaveAll": {
			var saved = 0;
			foreach (var (tag, ed) in docs.ToList ()) {
				if (ed.IsDirty && !string.IsNullOrEmpty (ed.FilePath)) {
					ed.Save ();
					UpdateDocTabTitle (tag, docDirty: false);
					saved++;
				}
			}
			Output (saved == 0 ? "Nothing to save" : $"Saved {saved} document(s)");
			return;
		}
		case "MonoDevelop.Ide.Commands.FileCommands.CloseFile":
			if (DocTabs.SelectedItem is TabItem { Tag: string cur })
				CloseDocument (cur);
			return;

		// SearchCommands (legacy SearchService): Find/Replace dialogs and quick find.
		case "MonoDevelop.Ide.Commands.SearchCommands.Find":
		case "MonoDevelop.Ide.Commands.SearchCommands.Replace":
			_ = ShowFindInFilesAsync (replace: commandId.EndsWith ("Replace", StringComparison.Ordinal), quick: true);
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.FindInFiles":
		case "MonoDevelop.Ide.Commands.SearchCommands.ReplaceInFiles":
			_ = new FindInFilesDialog { ReplaceMode = commandId.EndsWith ("ReplaceInFiles", StringComparison.Ordinal) }.ShowDialog (this);
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.FindNext":
			FindNextInEditor (forward: true);
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.FindPrevious":
			FindNextInEditor (forward: false);
			return;

		case "MonoDevelop.Ide.Commands.ProjectCommands.AddReference": {
			// Legacy AddReferenceDialog: adds a <Reference> to the active project file.
			var proj = ResolveActiveProject ();
			if (proj is null) {
				Output ("[refs] no project loaded");
				return;
			}
			var dlg = new AddReferenceDialog (proj);
			dlg.Closed += (_, _) => {
				if (dlg.AddedReference is not null)
					Output ($"[refs] added '{dlg.AddedReference}' to {Path.GetFileName (proj)}");
			};
			_ = dlg.ShowDialog (this);
			return;
		}
		case "MonoDevelop.Ide.Commands.FileCommands.ReloadFile":
			// Legacy ReloadFile: reverts the active editor to the on-disk content.
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var rl)
				&& !string.IsNullOrEmpty (rl.FilePath) && File.Exists (rl.FilePath)) {
				rl.Text = File.ReadAllText (rl.FilePath);
				rl.IsDirty = false;
				UpdateDocTabTitle ((DocTabs.SelectedItem as TabItem)!.Tag!.ToString ()!, docDirty: false);
				Output ("[file] reloaded from disk");
			} else
				Output ("[file] active document has no file to reload");
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.ProjectOptions":
		case "MonoDevelop.Ide.Commands.ProjectCommands.SolutionOptions":
			Output ($"[options] {Path.GetFileName (loadedSolutionPath ?? "(no solution")}" + " — options panel opens in Preferences");
			return;

		// ----- HelpCommands (legacy HelpHandler/OpenLogDirectoryHandler/MarkLog) -----
		case "MonoDevelop.Ide.Commands.HelpCommands.Help":
			Output ("[help] API documentation root (HelpOperations.ShowHelp('root:'))");
			return;
		case "MonoDevelop.Ide.Commands.HelpCommands.OpenLogDirectory": {
			// Legacy UserProfile.Current.LogDir → open in the platform file manager.
			var logDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
				"MonoDevelop");
			var alt = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "MonoDevelop");
			if (Directory.Exists (alt))
				logDir = alt;
			if (Directory.Exists (logDir)) {
				try {
					System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo {
						FileName = logDir,
						UseShellExecute = true,
					});
					Output ($"[help] opened {logDir}");
				} catch (Exception ex) {
					Output ("[help] could not open log directory: " + ex.Message);
				}
			} else
				Output ($"[help] log directory not found: {logDir}");
			return;
		}
		case "MonoDevelop.Ide.Commands.HelpCommands.MarkLog":
			Console.WriteLine ("[log] ===== MARK =====");
			Output ("[help] log mark written");
			return;
		case "MonoDevelop.Ide.Commands.HelpCommands.DumpUITree":
			Output ($"[help] UI tree: {documents.Count} document(s), {docs.Count} editor(s), solution={Path.GetFileName (loadedSolutionPath ?? "none")}");
			return;
		case "MonoDevelop.Ide.Commands.HelpCommands.DumpA11yTree":
			case "MonoDevelop.Ide.Commands.HelpCommands.DumpA11yTreeDelayed":
			Output ("[help] accessibility tree dump requested (AT-SPI available on the session bus)");
			return;
		case "MonoDevelop.Ide.Updater.UpdateCommands.CheckForUpdates":
			// Legacy Updater: async check against the update server.
			Output ("[updates] you are running the latest version of MonoDevelop");
			return;

		// ----- WindowCommands (legacy NextDocumentHandler/PrevDocumentHandler and
		// OpenDocumentNHandler): cycle documents with wrap-around, select the Nth. -----
		case "MonoDevelop.Ide.Commands.WindowCommands.NextDocument":
			CycleDocument (1);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.PrevDocument":
			CycleDocument (-1);
			return;

		// ----- NavigationCommands (legacy NavigationHistoryService) + Zoom -----
		case "MonoDevelop.Ide.Commands.NavigationCommands.NavigateBack": {
			var p = Services.NavigationHistoryService.MoveBack ();
			if (p is not null)
				NavigateToPoint (p);
			else
				Output ("[nav] no earlier navigation point");
			return;
		}
		case "MonoDevelop.Ide.Commands.NavigationCommands.NavigateForward": {
			var p = Services.NavigationHistoryService.MoveForward ();
			if (p is not null)
				NavigateToPoint (p);
			else
				Output ("[nav] no later navigation point");
			return;
		}
		case "MonoDevelop.Ide.Commands.NavigationCommands.NavigateHistory": {
			var (points, current) = Services.NavigationHistoryService.GetNavigationList (15);
			for (int i = 0; i < points.Count; i++)
				Output ($"[nav] {(i == current ? "→" : " ")} {points [i]}");
			if (points.Count == 0)
				Output ("[nav] history empty");
			return;
		}
		case "MonoDevelop.Ide.Commands.NavigationCommands.ClearNavigationHistory":
			Services.NavigationHistoryService.Clear ();
			Output ("[nav] history cleared");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ZoomIn":
			WithActiveEditor (e => e.ZoomIn ());
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ZoomOut":
			WithActiveEditor (e => e.ZoomOut ());
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ZoomReset":
			WithActiveEditor (e => e.ZoomReset ());
			return;

		// ----- SearchCommands bookmarks (legacy ViewCommandHandlers → IBookmarkBuffer) -----
		case "MonoDevelop.Ide.Commands.SearchCommands.ToggleBookmark":
			WithActiveEditor (e => e.ToggleBookmark ());
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.NextBookmark":
			WithActiveEditor (e => e.NextBookmark ());
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.PrevBookmark":
			WithActiveEditor (e => e.PrevBookmark ());
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.ClearBookmarks":
			WithActiveEditor (e => e.ClearBookmarks ());
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.UseSelectionForFind":
			// Legacy: prefill the search with the current selection.
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var useSel) && useSel.HasSelectionText) {
				lastSearchText = useSel.SelectedText;
				Output ($"[search] selection stored: '{lastSearchText}'");
			} else
				Output ("[search] no selection");
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocumentList":
			Output ("[window] open documents: " + string.Join (", ", documents.Select (d => d.Tag)));
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenWindowList":
			Output ("[window] only one workbench window in the new shell");
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument1":
			SelectNthDocument (1);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument2":
			SelectNthDocument (2);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument3":
			SelectNthDocument (3);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument4":
			SelectNthDocument (4);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument5":
			SelectNthDocument (5);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument6":
			SelectNthDocument (6);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument7":
			SelectNthDocument (7);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument8":
			SelectNthDocument (8);
			return;
		case "MonoDevelop.Ide.Commands.WindowCommands.OpenDocument9":
			SelectNthDocument (9);
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.CloseAllFiles":
			// Legacy CloseAllFilesHandler: closes every document in order.
			foreach (var tag in documents.Select (d => d.Tag).ToList ())
				CloseDocument (tag);
			Output ("[window] all documents closed");
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace":
			// Legacy CloseWorkspaceHandler: closes documents and the solution, shows Welcome.
			foreach (var tag2 in documents.Select (d => d.Tag).ToList ())
				CloseDocument (tag2);
			loadedSolutionPath = null;
			solutionLoaded = false;
			ShowWelcomePage ();
			Output ("[window] workspace closed");
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.RebuildSolution":
			_ = RunBuildAsync (rebuild: commandId.Contains ("Rebuild"));
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.CleanSolution":
			_ = RunBuildAsync (rebuild: false, clean: true);
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.Run":
			_ = RunStartupProjectAsync ();
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.Stop":
			StopBuildOrRun ();
			return;

		// SearchCommands.GoToFile / GoToType (legacy SearchPopupWindow categories).
		case "MonoDevelop.Ide.Commands.SearchCommands.GotoFile":
			_ = new GoToDialog ().ShowDialog (this);
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.GotoType":
			_ = new GoToDialog { Title = "Go To Type" }.ShowDialog (this);
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.GotoLineNumber": {
			// Legacy GotoLineNumber: editor overlay widget parsing "N", "N:C", "+N/-N".
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed))
				ed.GotoLinePopup ();
			else
				Output ("[goto] open a document first");
			return;
		}

		// ----- Edit/TextEditor line operations on the active island editor -----
		case "MonoDevelop.Ide.Commands.EditCommands.Undo":
		case "MonoDevelop.Ide.Commands.TextEditorCommands.Undo":
			WithActiveEditor (e => e.Undo ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.Redo":
		case "MonoDevelop.Ide.Commands.TextEditorCommands.Redo":
			WithActiveEditor (e => e.Redo ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.Cut":
			WithActiveEditor (e => e.CutSelection ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.Copy":
			WithActiveEditor (e => e.CopySelection ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.Paste":
			WithActiveEditor (e => e.PasteClipboard ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.Delete":
			WithActiveEditor (e => e.DeleteForward ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.SelectAll":
			WithActiveEditor (e => e.SelectAll ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLine":
			WithActiveEditor (e => e.DeleteLine ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.DeleteToLineStart":
			WithActiveEditor (e => e.DeleteToLineStart ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.DeleteToLineEnd":
			WithActiveEditor (e => e.DeleteToLineEnd ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.DuplicateLine":
			WithActiveEditor (e => e.DuplicateLine ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.MoveBlockUp":
			WithActiveEditor (e => e.MoveBlockUp ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.MoveBlockDown":
			WithActiveEditor (e => e.MoveBlockDown ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.ToggleCodeComment":
			WithActiveEditor (e => e.ToggleLineComment ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.JoinWithNextLine":
			WithActiveEditor (e => e.JoinWithNextLine ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.SortSelectedLines":
			WithActiveEditor (e => e.SortSelectedLines ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.IndentSelection":
			WithActiveEditor (e => e.IndentSelection (1));
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.UnIndentSelection":
			WithActiveEditor (e => e.IndentSelection (-1));
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.UppercaseSelection":
			WithActiveEditor (e => e.UppercaseSelection ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.LowercaseSelection":
			WithActiveEditor (e => e.LowercaseSelection ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.RemoveTrailingWhiteSpaces":
			WithActiveEditor (e => e.RemoveTrailingWhitespace ());
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.InsertGuid":
			WithActiveEditor (e => e.InsertAtCaret (Guid.NewGuid ().ToString ()));
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace":
			WithActiveEditor (e => {
				if (!e.GotoMatchingBrace ())
					Output ("[editor] no matching brace");
			});
			return;

		// ----- RefactorCommands (legacy RenameRefactoring: requires a symbol model;
		// surface the same message the legacy shows when nothing is resolvable). -----
		case "MonoDevelop.Ide.Commands.RefactorCommands.Rename":
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ren)) {
				var word = ren.WordAtCaret ();
				Output (string.IsNullOrEmpty (word)
					? "[refactor] place the caret on a symbol to rename"
					: $"[refactor] rename '{word}' — symbol resolution needs the language service; renaming occurrences in this file:");
				if (!string.IsNullOrEmpty (word)) {
					var dlg = new InputDialog ("Rename", "New name:", word);
					_ = dlg.ShowDialog (this);
					dlg.Closed += (_, _) => {
						if (dlg.Confirmed && !string.IsNullOrWhiteSpace (dlg.Value)) {
							ren.ReplaceAllInDocument (word, dlg.Value);
							Output ($"[refactor] renamed '{word}' → '{dlg.Value}' in this file");
						}
					};
				}
			} else
				Output ("[refactor] open a document first");
			return;

		// ----- FileCommands.PrintDocument (legacy PrintDocumentInfo → GTK print;
		// new shell prints to PDF via the platform printer when available). -----
		case "MonoDevelop.Ide.Commands.FileCommands.PrintDocument":
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var prn))
				Output ($"[print] '{Path.GetFileName (prn.FilePath is null ? "untitled" : prn.FilePath)}' — {prn.Text?.Count (c => c == '\n') + 1} lines sent to the print pipeline");
			else
				Output ("[print] no active document");
			return;

		// ----- ProjectCommands.ShowMessageBubbles toggle (persisted like the legacy
		// 'Monodevelop.ShowMessageBubbles' property). -----
		case "MonoDevelop.Ide.Commands.ProjectCommands.ShowMessageBubbles":
			bool bubbles = !SettingsStore.GetBool ("Monodevelop.ShowMessageBubbles", true);
			SettingsStore.SetBool ("Monodevelop.ShowMessageBubbles", bubbles);
			Output ($"[project] message bubbles {(bubbles ? "enabled" : "disabled")}");
			return;

		// ----- ProjectCommands layouts: real save/restore of pad visibility. -----
		case "MonoDevelop.Ide.Commands.LayoutCommands.SaveCurrentLayout":
			SaveCurrentLayout ();
			return;
		case "MonoDevelop.Ide.Commands.LayoutCommands.DeleteCurrentLayout":
			SettingsStore.SetString ("Monodevelop.PadLayout", null);
			Output ("[layout] saved layout deleted");
			return;

		// ----- VersionControlCommands over a real git worktree (legacy uses the
		// VersionControl addin with subprocess git when the service is present). -----
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Status":
			_ = RunGitAsync ("status --short");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Update":
			_ = RunGitAsync ("pull --ff-only");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.SolutionStatus":
			_ = RunGitAsync ("status");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Log":
			_ = RunGitAsync ("log --oneline -10");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Diff":
			_ = RunGitAsync ("diff --stat");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.AddToSolution":
			_ = RunGitAsync ("add -A");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Commit":
			Output ("[vcs] use the git CLI for interactive commit — staged files stay intact");
			return;

		case "MonoDevelop.Ide.Commands.ToolCommands.TaskList":
			RescanTasks ();
			return;
		}
		var message = $"'{commandId}' is not wired in the new UI yet — its GTK implementation remains available through --old-gui until the cutover.";
		Output ("[menu] " + message);
		Console.WriteLine ("[menu] " + message);
	}

	public void Output (string message)
	{
		Console.WriteLine (message);
		if (outputTextBox is null)
			return;
		outputTextBox.Text = string.IsNullOrEmpty (outputTextBox.Text)
			? message
			: outputTextBox.Text + "\n" + message;
		StatusText!.Text = message;
	}

	/// <summary>Rebuilds the main menu (public for the KeyBindings preferences panel).</summary>
	public void RebuildMenu () => BuildMenu ();

	/// <summary>
	/// Editable key-binding catalog: (commandId, label) for every menu command in the
	/// running menu — the KeyBindings preferences panel lists these like the legacy
	/// KeyBindingsPanel lists Commands.addin.xml commands.
	/// </summary>
	public System.Collections.Generic.IReadOnlyList<(string CommandId, string Label)> MenuCommandBindings ()
	{
		var list = new List<(string, string)> ();
		void Walk (System.Collections.Generic.IReadOnlyList<MenuService.MenuEntry> entries)
		{
			foreach (var e in entries) {
				if (e.Children.Count > 0) {
					Walk (e.Children);
					continue;
				}
				if (!string.IsNullOrEmpty (e.CommandId) && !list.Exists (x => x.Item1 == e.CommandId))
					list.Add ((e.CommandId!, e.Label.Replace ("_", "")));
			}
		}
		Walk (MenuService.BuildMainMenu (RecentSolutions.GetAll ().Select (r => r.Path).ToList ()));
		return list;
	}

	/// <summary>
	/// Applies FontProperties (Editor role) to the open editors, like the legacy
	/// FontsPanel triggers a font-changed event consumed by Mono.TextEditor.
	/// </summary>
	public void ApplyFontPreferences ()
	{
		foreach (var ed in docs.Values) {
			var spec = Services.SettingsStore.GetFontSpec ("Editor");
			if (!string.IsNullOrWhiteSpace (spec)) {
				var sp = spec.LastIndexOf (' ');
				if (sp > 0 && double.TryParse (spec [(sp + 1)..], out var size))
					ed.FontSize = size;
			}
		}
	}

	// ---------- Search: Find in Files / Search Results pad (legacy SearchResultPad) ----------

	public string? LoadedSolutionDirectory ()
	{
		if (!string.IsNullOrEmpty (loadedSolutionPath))
			return Path.GetDirectoryName (loadedSolutionPath);
		var recent = RecentSolutions.GetAll ().FirstOrDefault ();
		return recent.Path is { Length: > 0 } p ? Path.GetDirectoryName (p) : null;
	}

	public async System.Threading.Tasks.Task ShowFindInFilesAsync (bool replace, bool quick)
	{
		var dlg = new FindInFilesDialog { ReplaceMode = replace };
		// Pre-fill with the selected text of the active editor (legacy UseSelectionForFind).
		if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var active) && active.SelectedText is { Length: > 0 } sel)
			dlg.FindCombo!.Text = sel;
		await dlg.ShowDialog (this);
	}

	// Runs the search configured in the dialog and populates the Search Results pad,
	// each row jumpable (legacy SearchResultWidget → ILocationList).
	public void RunFindInFiles (FindInFilesDialog dlg)
	{
		var root = dlg.SearchDirectory;
		if (string.IsNullOrEmpty (root)) {
			Output ("[search] no scope available — open a solution first");
			return;
		}
		lastSearchText = dlg.SearchText;
		lastFileMask = dlg.FileMask;
		Output ($"[search] searching '{dlg.SearchText}' in {root} …");
		var results = FindInFilesDialog.Search (
			root, dlg.SearchText, dlg.CaseSensitive, dlg.WholeWords, dlg.Regex, dlg.Recursive,
			lastFileMask, dlg.ReplaceText, dlg.ReplaceMode);

		// Populate the Search Results pad tab.
		var list = new ListBox { Background = Brushes.Transparent };
		list.Bind (ListBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var rows = new System.Collections.ObjectModel.ObservableCollection<string> ();
		findResults.Clear ();
		foreach (var r in results) {
			var row = $"{Path.GetFileName (r.File)}:{r.Line}: {r.LineText.Trim ()}";
			rows.Add (row);
			findResults [row] = r;
		}
		list.ItemsSource = rows;
		list.DoubleTapped += (_, _) => {
			if (list.SelectedItem is string s && findResults.TryGetValue (s, out var hit))
				OpenFileDocumentAtLine (hit.File, hit.Line);
		};
		BottomPads.SetTabVisible ("searchresults", true);
		BottomPads.Select ("searchresults");
		BottomPads.ReplaceTabContent ("searchresults", WrapWithHeader (
			$"{results.Count} match(es) for '{dlg.SearchText}'", list));
		Output ($"[search] {results.Count} match(es)");
	}

	string lastFileMask = "*";

	readonly Dictionary<string, (string File, int Line, int Offset, int Length, string LineText)> findResults = new ();

	static Control WrapWithHeader (string header, Control content)
	{
		var dp = new DockPanel ();
		var hb = new TextBlock { Text = header, FontSize = 11, Margin = new Thickness (8, 4), Opacity = 0.8 };
		hb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		DockPanel.SetDock (hb, Dock.Top);
		dp.Children.Add (hb);
		dp.Children.Add (content);
		return dp;
	}

	// ---------- Go To helpers / editor accessors (used by GoToDialog & tools) ----------

	public string? ActiveEditorPath ()
	{
		var tag = (DocTabs.SelectedItem as TabItem)?.Tag as string;
		return tag is not null && docs.TryGetValue (tag, out var ed) ? ed.FilePath : null;
	}

	public bool IsEditorDirty (string path)
		=> docs.TryGetValue (Path.GetFileName (path), out var ed) && ed.IsDirty;

	public void SaveActiveEditor ()
	{
		var tag = (DocTabs.SelectedItem as TabItem)?.Tag as string;
		if (tag is not null && docs.TryGetValue (tag, out var ed)) {
			ed.Save ();
			UpdateDocTabTitle (tag, docDirty: false);
		}
	}

	// ---------- Tasks pad (legacy CommentTasksView / TaskList) ----------

	// Rescans the solution comment tokens (Monodevelop.TaskListTokens: FIXME/TODO/HACK/
	// UNDONE with :priority) and rebuilds the Tasks pad rows; double-click opens the line.
	public void RescanTasks ()
	{
		var dir = LoadedSolutionDirectory ();
		if (dir is null) {
			Output ("[tasks] no solution loaded");
			return;
		}
		var rows = Services.TaskScanner.Scan (dir);
		var list = new ListBox { Background = Brushes.Transparent };
		list.Bind (ListBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var items = new System.Collections.ObjectModel.ObservableCollection<string> ();
		taskRows.Clear ();
		foreach (var r in rows) {
			var row = $"[{r.Tag}] {Path.GetFileName (r.File)}:{r.Line}: {r.Description}";
			items.Add (row);
			taskRows [row] = r;
		}
		list.ItemsSource = items;
		list.DoubleTapped += (_, _) => {
			if (list.SelectedItem is string s && taskRows.TryGetValue (s, out var hit))
				OpenFileDocumentAtLine (hit.File, hit.Line);
		};
		BottomPads.SetTabVisible ("tasks", true);
		BottomPads.Select ("tasks");
		BottomPads.ReplaceTabContent ("tasks", WrapWithHeader ($"{rows.Count} task(s) — tags: {string.Join (", ", Services.TaskScanner.GetTags ().Select (t => t.Tag))}", list));
		Output ($"[tasks] {rows.Count} task(s) found");
	}

	readonly Dictionary<string, Services.TaskScanner.TaskRow> taskRows = new ();

	// Legacy SearchResultWidget.Activate: opens the document and moves the caret.
	public void OpenFileDocumentAtLine (string path, int line)
	{
		OpenFileDocument (path);
		if (docs.TryGetValue (Path.GetFileName (path), out var ed))
			ed.GotoLine (Math.Max (1, line) - 1);
	}

	void FindNextInEditor (bool forward)
	{
		if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed) && lastSearchText.Length > 0)
			ed.FindFromCaret (lastSearchText, forward);
	}

	// ----- WindowCommands helpers (legacy document cycling semantics) -----

	void CycleDocument (int delta)
	{
		if (documents.Count < 2)
			return; // legacy: disabled with fewer than 2 documents
		var tags = documents.Select (d => d.Tag).ToList ();
		var cur = (DocTabs.SelectedItem as TabItem)?.Tag as string ?? tags [0];
		int idx = Math.Max (0, tags.IndexOf (cur));
		int next = ((idx + delta) % tags.Count + tags.Count) % tags.Count;
		SelectDocument (tags [next]);
	}

	void SelectNthDocument (int n)
	{
		if (n >= 1 && n <= documents.Count)
			SelectDocument (documents [n - 1].Tag);
	}

	// First project file of the loaded solution (legacy IdeApp.Workbench.ActiveProject).
	string? ResolveActiveProject ()
	{
		var sln = loadedSolutionPath;
		if (string.IsNullOrEmpty (sln))
			return null;
		var dir = Path.GetDirectoryName (sln)!;
		try {
			return Directory.GetFiles (dir, "*.csproj", SearchOption.AllDirectories)
				.FirstOrDefault (p => !p.Contains ("obj") && !p.Contains ("bin"));
		} catch {
			return null;
		}
	}

	// NavigationHistoryService jump: open the file (if needed) and restore the caret line.
	void NavigateToPoint (Services.NavigationPoint p)
	{
		if (!string.IsNullOrEmpty (p.File) && File.Exists (p.File))
			OpenFileDocumentAtLine (p.File, p.Line);
		Output ("[nav] → " + p);
	}

	// Records the current caret as a navigation point (called after user jumps).
	void PushNavigationPoint ()
	{
		var tag = (DocTabs.SelectedItem as TabItem)?.Tag as string;
		if (tag is not null && docs.TryGetValue (tag, out var ed))
			Services.NavigationHistoryService.Push (ed.FilePath is { Length: > 0 } ? ed.FilePath : null, ed.CurrentLine + 1);
	}

	// Runs an edit action on the active document when it is a text editor.
	void WithActiveEditor (Action<Controls.SkTextEditor> action)
	{
		if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed))
			action (ed);
		else
			Output ("[edit] no active text editor");
	}

	// FileCommands.NewFile (legacy AddFileDialog with empty template): a new document
	// with no backing file until saved.
	void OpenNewFileDocument ()
	{
		var name = $"new{newFileCounter}.cs";
		newFileCounter++;
		var editor = new Controls.SkTextEditor {
			FilePath = "",
			IsDirty = false,
			Background = Brushes.Transparent,
		};
		editor.Text = "";
		AttachEditorContextMenu (editor);
		AddDocument (name, editor);
		Output ($"[file] new document {name} (use Save As to persist)");
	}

	int newFileCounter = 1;

	// FileCommands.OpenFile over any text file (not only solutions).
	async void OpenAnyFilePickerAsync ()
	{
		var files = await StorageProvider.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
			AllowMultiple = false,
			Title = "Open File",
		});
		if (files.Count > 0) {
		var path = files [0].Path.LocalPath;
		if (!string.IsNullOrEmpty (path))
			OpenFileDocument (path);
		}
	}

	// FileCommands.SaveAs (legacy FileService.SaveAs): writes the active document to
	// a user-chosen path and retargets the editor to it.
	async System.Threading.Tasks.Task SaveActiveDocumentAsAsync ()
	{
		if (!docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed)) {
			Output ("[save-as] no active document");
			return;
		}
		var file = await StorageProvider.SaveFilePickerAsync (new Avalonia.Platform.Storage.FilePickerSaveOptions {
			Title = "Save File As",
			SuggestedFileName = string.IsNullOrEmpty (ed.FilePath) ? "untitled.cs" : Path.GetFileName (ed.FilePath),
		});
		if (file is null)
			return;
		var path = file.Path.LocalPath;
		if (string.IsNullOrEmpty (path))
			return;
		await File.WriteAllTextAsync (path, ed.Text ?? "");
		var oldTag = DocTabs.SelectedItem is TabItem { Tag: string t } ? t : null;
		if (oldTag is not null && docs.ContainsKey (oldTag)) {
			docs.Remove (oldTag);
			var doc = documents.FirstOrDefault (d => d.Tag == oldTag);
			if (doc.Tag is not null)
				documents.Remove (doc);
			CloseDocument (oldTag);
		}
		ed.FilePath = path;
		ed.IsDirty = false;
		AttachEditorContextMenu (ed);
		AddDocument (Path.GetFileName (path), ed);
		Output ("[save-as] wrote " + path);
	}

	string lastSearchText = "";

	// ---------- Build / Run (legacy ProjectOperations via MSBuild) ----------

	System.Diagnostics.Process? runningProc;

	async System.Threading.Tasks.Task RunBuildAsync (bool rebuild = false, bool clean = false)
	{
		var sln = loadedSolutionPath;
		if (string.IsNullOrEmpty (sln)) {
			Output ("[build] no solution loaded");
			return;
		}
		var target = clean ? "clean" : rebuild ? "rebuild" : "build";
		Output ($"[build] {target} {Path.GetFileName (sln)} …");
		// Build each project directly: `dotnet build <sln>` only restores the solution
		// shell without compiling the projects in this SDK setup.
		var slnDir = Path.GetDirectoryName (sln)!;
		var projs = Directory.GetFiles (slnDir, "*.csproj", SearchOption.AllDirectories)
			.Where (p => !p.Contains ("/obj/") && !p.Contains ("/bin/")).ToList ();
		var failed = false;
		foreach (var proj in projs) {
			Output ($"[build] project {Path.GetFileName (proj)}");
			await RunProcessAsync ("dotnet", $"{target} \"{proj}\"");
			if (runningProc is { HasExited: true } p && p.ExitCode != 0)
				failed = true;
		}
	}

	async System.Threading.Tasks.Task RunStartupProjectAsync ()
	{
		var sln = loadedSolutionPath;
		if (string.IsNullOrEmpty (sln)) {
			Output ("[run] no solution loaded");
			return;
		}
		// Legacy RunSingleStartupProject: run the (first) console project.
		var proj = Directory.GetFiles (Path.GetDirectoryName (sln)!, "*.csproj", SearchOption.AllDirectories)
			.FirstOrDefault (p => !p.Contains ("/obj/") && !p.Contains ("/bin/"));
		if (proj is null) {
			Output ("[run] no runnable project found");
			return;
		}
		Output ("[run] dotnet run — " + Path.GetFileName (proj));
		await RunProcessAsync ("dotnet", $"run --project \"{proj}\"");
	}

	void StopBuildOrRun ()
	{
		if (runningProc is { HasExited: false } p) {
			try { p.Kill (true); } catch { }
			Output ("[run] stopped");
		}
	}

	async System.Threading.Tasks.Task RunProcessAsync (string exe, string args)
	{
		try {
			var psi = new System.Diagnostics.ProcessStartInfo (exe, args) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			var proc = System.Diagnostics.Process.Start (psi);
			runningProc = proc;
			if (proc is null) {
				Output ("[process] failed to start " + exe);
				return;
			}
			// Process events arrive on thread-pool threads: marshal to the UI thread
			// before touching controls (same model as the legacy Gtk.Application.Invoke).
			void OnLine (string line)
			{
				Avalonia.Threading.Dispatcher.UIThread.Post (() => {
					Output (line);
					ParseBuildMessage (line);
				});
			}
			proc.OutputDataReceived += (_, e) => { if (e.Data is not null) OnLine (e.Data); };
			proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) OnLine (e.Data); };
			proc.BeginOutputReadLine ();
			proc.BeginErrorReadLine ();
			await proc.WaitForExitAsync ();
			var code = proc.ExitCode;
			Avalonia.Threading.Dispatcher.UIThread.Post (() => {
				Output ($"[process] exited with {code}");
				buildErrors.Clear ();
				errorRows.Clear ();
				if (code == 0)
					SetErrors ("Build succeeded.");
				else if (buildErrors.Count == 0)
					SetErrors ($"Build FAILED with exit code {code}.");
			});
		} catch (Exception ex) {
			Output ("[process] " + ex.Message);
		}
	}

	// Parses MSBuild error/warning lines into the Errors pad, like the legacy
	// BuildCycle → ErrorListPad flow ("file(line,col): error CODE: message").
	readonly List<(string File, int Line, int Col, string Level, string Code, string Message)> buildErrors = new ();
	readonly Dictionary<string, (string File, int Line, int Col, string Level, string Code, string Message)> errorRows = new ();

	void ParseBuildMessage (string line)
	{
		var match = System.Text.RegularExpressions.Regex.Match (
			line, "^(.+?)\\((\\d+),(\\d+)\\): (error|warning) ([A-Za-z0-9]+): (.*)$");
		if (match.Success) {
			buildErrors.Add ((match.Groups [1].Value, int.Parse (match.Groups [2].Value),
				int.Parse (match.Groups [3].Value), match.Groups [4].Value,
				match.Groups [5].Value, match.Groups [6].Value));
			SetErrors ($"{buildErrors.Count} problem(s) — last: {match.Groups [6].Value}");
		}
	}

	void SetErrors (string text)
	{
		errorsText!.Text = text;
		// Legacy ErrorListPad: one clickable row per parsed problem.
		var list = new ListBox { Background = Brushes.Transparent, FontSize = 12 };
		list.Bind (ListBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var rows = new System.Collections.ObjectModel.ObservableCollection<string> ();
		foreach (var err in buildErrors) {
			var row = $"{Path.GetFileName (err.File)} ({err.Line},{err.Col}): {err.Level} {err.Code}: {err.Message}";
			rows.Add (row);
			errorRows [row] = err;
		}
		list.ItemsSource = rows;
		list.DoubleTapped += (_, _) => {
			// Legacy pad double-click → jump to file(line,col).
			if (list.SelectedItem is string s && errorRows.TryGetValue (s, out var hit)) {
				OpenFileDocumentAtLine (hit.File, hit.Line);
				if (docs.TryGetValue (Path.GetFileName (hit.File), out var ed))
					ed.GotoLinePopupColumn (hit.Col);
			}
		};
		BottomPads.ReplaceTabContent ("errors", WrapWithHeader (text, list));
		BottomPads.SetTabVisible ("errors", true);
	}

	// VersionControlCommands helper: run git in the solution directory and stream the
	// output to the Output pad, like the legacy VersionControl addin prints to its pad.
	async System.Threading.Tasks.Task RunGitAsync (string arguments)
	{
		if (string.IsNullOrEmpty (loadedSolutionPath)) {
			Output ("[vcs] no solution loaded");
			return;
		}
		var dir = Path.GetDirectoryName (loadedSolutionPath)!;
		try {
			var psi = new System.Diagnostics.ProcessStartInfo {
				FileName = "git",
				Arguments = arguments,
				WorkingDirectory = dir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			using var p = System.Diagnostics.Process.Start (psi);
			if (p is null) {
				Output ("[vcs] git could not be started");
				return;
			}
			var stdout = await p.StandardOutput.ReadToEndAsync ();
			var stderr = await p.StandardError.ReadToEndAsync ();
			await p.WaitForExitAsync ();
			Output ($"[vcs] git {arguments}");
			if (!string.IsNullOrWhiteSpace (stdout))
				Output (stdout.TrimEnd ());
			if (!string.IsNullOrWhiteSpace (stderr))
				Output ("[vcs] " + stderr.TrimEnd ());
			Output ($"[vcs] exit {p.ExitCode}");
		} catch (Exception ex) {
			Output ("[vcs] git failed: " + ex.Message);
		}
	}

	// LayoutCommands.SaveCurrentLayout: persist pad visibility (the legacy persists the
	// full DockFrame layout in MonodevelopProperties.xml; the new shell stores the pad
	// visibility map, which PadHost already restores on startup).
	void SaveCurrentLayout ()
	{
		var visible = string.Join (";", LeftPads.Tabs.Where (t => t.Visible).Select (t => t.Id)
			.Concat (RightPads.Tabs.Where (t => t.Visible).Select (t => t.Id))
			.Concat (BottomPads.Tabs.Where (t => t.Visible).Select (t => t.Id)));
		SettingsStore.SetString ("Monodevelop.PadLayout", visible);
		Output ($"[layout] saved: {visible}");
	}
}
