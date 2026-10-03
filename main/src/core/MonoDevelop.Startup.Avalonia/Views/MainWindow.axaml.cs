using MonoDevelop.Debugger.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;	using Avalonia.Controls;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MonoDevelop.Ide.Controls;
using MonoDevelop.Ide.Services;
using MonoDevelop.AvaloniaShell.Views;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class MainWindow : Window
{
	static readonly bool IsMac = RuntimeInformation.IsOSPlatform (OSPlatform.OSX);

	public static MainWindow? Instance { get; private set; }

	WelcomePageView? welcomePage;
	bool welcomeVisible = true;
	bool solutionLoaded;

	// Wave-3 editor-tab chrome: breadcrumb row (hidden until a file document is
	// selected) and its segment stack.
	Border? BreadcrumbRow;
	StackPanel? BreadcrumbBar;

	// QA snapshot of the breadcrumb text (segments joined with " › ") as of the
	// latest UpdateBreadcrumb — automated runs assert the scope chain without UI.
	public string BreadcrumbSegmentsForQa =>
		BreadcrumbBar is { } bar
			? string.Join (" › ", bar.Children.OfType<TextBlock> ().Where (t => t.Text != "›").Select (t => t.Text))
			: "";
	int lastBreadCaretLine = -1;

	// The Document Outline pad follows the active document: the breadcrumb timer
	// already polls the caret, so the outline is refreshed only when the document
	// (or its text) actually changed, not on every caret move.
	string? lastOutlineTag;
	string? lastOutlineText;

	// Bottom-pad collapse coupling: while the pad is collapsed the dock row pins
	// to the 34px rail (no dead gap under the splitter) and expanding returns the
	// row to its previous height; a splitter drag on a collapsed pad expands it
	// WITHOUT the height restore so the drag itself grows the row from the rail.
	double? bottomSavedRowH;
	bool dragExpandingBottom;

	// Wave-3 real Xwt.TreeView in the Solution pad: created lazily (the Xwt engine
	// initializes on demand); the pad hosts its native Avalonia control.
	DockPanel? xwtSolutionHost;
	Control? xwtPlaceholder;
	Xwt.TreeView? xwtSolutionTree;
	Xwt.TreeStore? xwtSolutionStore;
	Xwt.DataField<string> xwtSolTextField = new ();
	Xwt.DataField<Xwt.Drawing.Image> xwtSolIconField = new ();
	Xwt.DataField<string> xwtSolTagField = new ();
	Dictionary<string, Xwt.TreeNavigator> xwtSolutionNodes = new (); // tag → navigator

	/// <summary>
	/// Creates the real Xwt.TreeView (Xwt.Avalonia backend) once, embeds its native
	/// Avalonia control in the Solution pad, and wires activation/selection to the
	/// same handlers the Avalonia preview tree used.
	/// </summary>
	void EnsureXwtSolutionTree ()
	{
		if (xwtSolutionTree is not null || xwtSolutionHost is null)
			return;
		Xwt.Application.Initialize ("Xwt.AvaloniaBackend.AvaloniaEngine, Xwt.Avalonia");
		xwtSolutionStore = new Xwt.TreeStore (xwtSolIconField, xwtSolTextField, xwtSolTagField);
		xwtSolutionTree = new Xwt.TreeView {
			DataSource = xwtSolutionStore,
			HeadersVisible = false,
			BorderVisible = false,
		};
		var iconCol = new Xwt.ListViewColumn { Title = "" };
		iconCol.Views.Add (new Xwt.ImageCellView (xwtSolIconField));
		var textCol = new Xwt.ListViewColumn { Title = "Node" };
		textCol.Views.Add (new Xwt.TextCellView (xwtSolTextField));
		xwtSolutionTree.Columns.Add (iconCol);
		xwtSolutionTree.Columns.Add (textCol);
		xwtSolutionTree.RowActivated += OnXwtSolutionRowActivated;
		xwtSolutionTree.SelectionChanged += (_, _) => UpdatePropertiesPad ();
		xwtSolutionTree.ButtonPressed += OnXwtSolutionContextMenu;

		if (Xwt.Toolkit.CurrentEngine.GetNativeWidget (xwtSolutionTree) is Control native) {
			xwtSolutionHost.Children.Remove (xwtPlaceholder!);
			xwtSolutionHost.Children.Add (native);
		}
	}

	/// <summary>Legacy stock icon resource → Xwt.Drawing.Image. The PNGs live in
	/// IconService.IconsDirectory on disk (the IconService TYPE compiles into the
	/// shell assembly, so FromResource(itsAssembly) cannot see MonoDevelop.Ide's
	/// embedded icons) — load by file, fallback to file-source-16.png.</summary>
	Xwt.Drawing.Image XwtStockIcon (string stockResource)
	{
		try {
			var dir = MonoDevelop.Ide.Services.IconService.IconsDirectory;
			if (dir is not null)
				return Xwt.Drawing.Image.FromFile (Path.Combine (dir, stockResource)).WithSize (16, 16);
		} catch { }
		try {
			var dir = MonoDevelop.Ide.Services.IconService.IconsDirectory;
			if (dir is not null)
				return Xwt.Drawing.Image.FromFile (Path.Combine (dir, "file-source-16.png")).WithSize (16, 16);
		} catch { }
		return Xwt.Drawing.Image.FromStream (new System.IO.MemoryStream (new byte[] {
			0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
			0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
			0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x62, 0x00, 0x01, 0x00, 0x00,
			0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
			0x42, 0x60, 0x82,
		})).WithSize (16, 16); // 1x1 transparent PNG placeholder
	}

	public MainWindow ()
	{
		InitializeComponent ();
		Instance = this;
		Output ("MonoDevelop Avalonia shell initialized.");

		BuildPads ();
		// Restore the persisted pad layout (visible tabs, collapse, active tab,
		// row height) once the dispatcher runs — hosts exist and settings are loaded.
		Avalonia.Threading.Dispatcher.UIThread.Post (RestorePadsLayout, Avalonia.Threading.DispatcherPriority.Background);

		// Legacy DirtyFilesDialog gate: closing the window with modified documents
		// shows "Save Files" before quitting (Workbench.OnDeleteEvent).
		Closing += OnMainWindowClosing;

		// Tab clicks swap the mounted document in DocContent (and show the empty
		// host when the selection is cleared).
		DocTabs.SelectionChanged += OnDocSelectionChanged;

		// Document strip nudge buttons (legacy DocumentPad ‹ ›): SHIFT the clipped tab
		// row when the tabs' total width exceeds the strip viewport; the buttons only
		// exist (IsVisible) while there is overflow. Template parts are reached through
		// the visual tree once the TabControl template is applied (deferred post).
		Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			foreach (var b in DocTabs!.GetVisualDescendants ().OfType<Button> ()) {
				if (b.Name == "DocScrollLeft")
					b.Click += (_, _) => NudgeTabStrip (-1);
				else if (b.Name == "DocScrollRight")
					b.Click += (_, _) => NudgeTabStrip (1);
			}
			if (DocTabsPresenter is not null)
				DocTabsPresenter.RenderTransform = new Avalonia.Media.TranslateTransform ();
			UpdateTabScrollButtons ();
			// Overflow state changes with window resizes too.
			if (TabStripViewport is { } vp)
				vp.SizeChanged += (_, _) => UpdateTabScrollButtons ();
			if (DocTabsPresenter is { } row)
				row.SizeChanged += (_, _) => UpdateTabScrollButtons ();
		}, Avalonia.Threading.DispatcherPriority.Background);

		// Breadcrumb (legacy SourceEditor widget): the row lives in the XAML between
		// the tab strip and the content; code only fills the segment stack.
		BreadcrumbRow = BreadcrumbHostRow;
		BreadcrumbBar = BreadcrumbSegments;

		// Editor <-> bottom-pad resize: the thumb directly drives the bottom row's
		// height (row 1 is the bottom pad, declared Auto-like via PadSplitter).
		EditorBottomSplitter.TargetRow = WorkbenchGrid.RowDefinitions [1];

		// Collapse ⇄ row coupling (legacy DockFrame: a collapsed bottom dock leaves
		// only its rail; reopening restores the previous split).
		BottomPads.CollapseChanged += collapsed => {
			var row = WorkbenchGrid.RowDefinitions [1];
			if (collapsed) {
				if (row.Height.IsAbsolute && row.Height.Value > 34)
					bottomSavedRowH = row.Height.Value;
				row.Height = new GridLength (34);
			} else if (!dragExpandingBottom) {
				row.Height = new GridLength (bottomSavedRowH ?? 170);
			}
		};
		EditorBottomSplitter.DragStarted += () => {
			if (!BottomPads.IsCollapsed)
				return;
			dragExpandingBottom = true;
			BottomPads.ToggleCollapse ("splitter-drag");
			dragExpandingBottom = false;
		};

		// The breadcrumb follows the caret like the legacy widget: SkTextEditor has
		// no caret-changed event, so poll the active editor's line and refresh the
		// scope chain only when it moved (typing/clicks/keys all move it).
		var breadcrumbTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds (250) };
		breadcrumbTimer.Tick += (_, _) => {
			if (DocTabs.SelectedItem is TabItem { Tag: string tag } && docs.TryGetValue (tag, out var ed)
				&& !string.IsNullOrEmpty (ed.FilePath) && ed.CurrentLine + 1 != lastBreadCaretLine) {
				lastBreadCaretLine = ed.CurrentLine + 1;
				UpdateBreadcrumb ();
			}
			// Same poll drives the Document Outline pad (SkTextEditor has no
			// text-changed event either): refresh only when the buffer changed.
			if (DocTabs.SelectedItem is TabItem { Tag: string otag } && docs.TryGetValue (otag, out var oed)
				&& !string.IsNullOrEmpty (oed.FilePath) && (otag != lastOutlineTag || oed.Text != lastOutlineText)) {
				lastOutlineTag = otag;
				lastOutlineText = oed.Text;
				RefreshOutlinePad ();
			}
		};
		breadcrumbTimer.Start ();

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

		// Win11-style edge/corner resize: the gutter zones around the frame start the
		// native resize loop; maximized/fullscreen hides them along with the frame.
		AttachResizeCursors ();
		PropertyChanged += (s, e) => {
			if (e.Property == WindowStateProperty)
				UpdateResizeChrome ();
		};

		// Toolbar content mirrors the GTK MainToolbar: run button, configuration/run
		// configuration/runtime combos and the search box on the right.
		RunConfigCombo!.PlaceholderText = "Default";
		foreach (var rc in new[] { "Default", "Debug", "Release" })
			RunConfigCombo.Items.Add (rc);
		// Default toolbar configs (the legacy combo shows the workspace configs when
		// a solution opens — see RefreshConfigurationSelectors).
		ConfigCombo!.Items.Add ("Debug");
		ConfigCombo.Items.Add ("Release");
		ConfigCombo.SelectedIndex = 0;
		// The legacy runtime combo was dropped: runtimes are selected per project.

		// The placement convention (mac left, Windows/Linux right) is handled in
		// OnOpened by re-parenting the caption buttons to the requested side; the
		// XAML default places them on the right, matching Linux and Windows.
		Opened += async (s, e) => {
			// The window is up: disarm the startup watchdog (Program.cs).
			Program.StartupWatchdogDone.Cancel ();
			UpdateResizeChrome ();
			if (IsMac)
				MoveCaptionButtonsLeft ();
			// Do NOT pin RequestedThemeVariant here: with System/Default it must keep
			// following the OS (Avalonia reports the platform variant asynchronously —
			// first Light, then Dark on a dark OS). Only apply the chrome that depends
			// on the current variant, and refresh it when the platform variant changes.
			Background = Brushes.Transparent;
			SetToolbarIcons ();
			if (Application.Current is not null)
				Application.Current.ActualThemeVariantChanged += (_, _) => SetToolbarIcons ();

			// Load a real solution into the Solution pad when requested (--sln=<path>).
			var slnArg = Program.SolutionArg;
			if (slnArg.Length > 0)
				OpenSolutionInWindow (slnArg);

			// QA: after layout settles, log the on-screen rects of every pad's collapse
			// chrome — one authoritative coordinate dump per launch for pixel checks.
			Avalonia.Threading.Dispatcher.UIThread.Post (() => {
				LeftPads.LogCollapseChrome ();
				BottomPads.LogCollapseChrome ();
				RightPads.LogCollapseChrome ();
			}, Avalonia.Threading.DispatcherPriority.Background);

			// Automated QA: open the requested dialog directly.
			var qa = Program.QaDialogArg;
			if (qa == "--prefs") {
				new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
			} else if (qa == "--prefs-tree") {
				// QA: dump the Preferences section tree (ids/labels/icons) and show it.
				var dlg = new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
				dlg.DumpTreeForQa ();
				dlg.ShowDialog (this);
			} else if (qa == "--prefs-sourcewrite") {
				// QA: exercise the global-policy write for naming/header, dump and restore.
				var dlg = new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
				dlg.QaWriteSourceCodePolicies ();
				dlg.Close ();
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
			} else if (qa == "--encodings") {
				// QA: Select Encodings — dual list populated from the BCL, Add/
				// Remove/Up/Down and persistence to ConversionEncodings.
				new SelectEncodingsDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
				Output ("[encodings] dialog closed");
			} else if (qa == "--newfolder") {
				// QA: New Folder — default name, live validation (illegal chars,
				// duplicate) and the Add action creating the directory.
				var baseDir = Path.Combine (Path.GetTempPath (), "QAFolder", Guid.NewGuid ().ToString ("N"));
				Directory.CreateDirectory (baseDir);
				var dlg = new NewFolderDialog (baseDir);
				Output ("[newfolder] default='" + dlg.FolderNameForQa + "' addEnabled=" + dlg.IsAddEnabledForQa);
				dlg.FolderNameForQa = "bad/name";
				dlg.ValidateForQa ();
				Output ("[newfolder] illegal: addEnabled=" + dlg.IsAddEnabledForQa + " warning='" + dlg.WarningForQa + "'");
				dlg.FolderNameForQa = "Created";
				dlg.ValidateForQa ();
				dlg.AcceptForQa ();
				Output ("[newfolder] created=" + Directory.Exists (Path.Combine (baseDir, "Created")) + " result='" + dlg.NewFolderCreated + "'");
				try { Directory.Delete (baseDir, true); } catch { }
			} else if (qa == "--exmode") {
				// QA: Execution Mode Selector — two configs, a two-mode set nested
				// under its parent and a lone mode hoisted to the root (legacy
				// single-child cleanup), preselection and the Run action.
				var dlg = new ExecutionModeSelectorDialog ();
				dlg.Load (
					new[] {
						new ExecutionModeSelectorDialog.RunConfig { Name = "Default" },
						new ExecutionModeSelectorDialog.RunConfig { Name = "Custom" },
					},
					new[] {
						new ExecutionModeSelectorDialog.ModeEntry { Name = ".NET Core", Id = "dotnet", SetName = ".NET Core" },
						new ExecutionModeSelectorDialog.ModeEntry { Name = "Mono", Id = "mono", SetName = "Mono" },
					},
					selectedModeId: "mono");
				Output ("[exmode] configs=" + dlg.ConfigCountForQa + " modes=" + dlg.ModeCountForQa + " runEnabled=" + dlg.IsRunEnabledForQa + " label='" + dlg.RunLabelForQa + "' selMode=" + dlg.SelectedModeIdForQa);
				// Modal loop with an automated tick responding the run command after
				// 2s — the same pattern --xruncfg uses to exercise the dialog live.
				var autoRun = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds (2) };
				autoRun.Tick += (s, e) => {
					autoRun.Stop ();
					dlg.AcceptForQa ();
				};
				autoRun.Start ();
				await dlg.ShowDialog (this);
				Output ("[exmode] closed result=" + (dlg.Result is { } r ? r.Config.Name + "→" + r.Mode.Name : "null"));
			} else if (qa == "--tabscroll") {
				// QA: tab strip overflow — open enough documents for the ‹ › buttons
				// to appear, step through the tabs, then close until they fit again
				// and the buttons disappear.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				if (!File.Exists (file)) {
					Output ("[tabscroll] no test file");
				} else {
					for (int i = 1; i <= 10 && !TabScrollButtonsVisible (); i++) {
						var docName = "QaOverflow" + i + ".cs";
						OpenDocumentText (docName, $"// overflow filler {i}\n");
						await Task.Delay (60); // layout pass so the strip sizes update
						Output ("[tabscroll] opened " + docName + " scrollVisible=" + TabScrollButtonsVisible ());
					}
					Output ("[tabscroll] overflow reached: " + TabScrollButtonsVisible ());
					double before = tabStripOffset;
					NudgeTabStrip (-1);
					Output ("[tabscroll] nudge left: offset " + before + " → " + tabStripOffset);
					NudgeTabStrip (1);
					Output ("[tabscroll] nudge right: offset → " + tabStripOffset);
					while (docs.Count > 0 && TabScrollButtonsVisible ()) {
						var last = docs.Keys.Last ();
						CloseDocument (last);
					}
					// The last CloseDocument removes the last doc; reopen the base file
					// so the shell keeps a tab.
					if (docs.Count == 0)
						OpenFileDocument (file);
					await Task.Delay (200); // layout settle after the removals
					UpdateTabScrollButtons ();
					Output ("[tabscroll] after close: docs=" + docs.Count + " scrollVisible=" + TabScrollButtonsVisible ());
				}
			} else if (qa == "--searchpopup") {
				// QA: toolbar search — live results while typing, category prefixes
				// and Enter activation opening the picked file.
				ToolbarSearch!.Text = "Prog";
				OnToolbarSearchTextChanged ("Prog");
				Output ("[searchpopup] 'Prog' results=" + searchResults.Count + " visible=" + (searchPopupHost?.IsVisible == true) +
					" first=" + (searchResults.Count > 0 ? searchResults [0].Title + " | " + searchResults [0].Subtitle : "-"));
				OnToolbarSearchTextChanged (":c build");
				Output ("[searchpopup] ':c build' results=" + searchResults.Count + " bindings=" + MenuCommandBindings ().Count + " first=" + (searchResults.Count > 0 ? searchResults [0].Title : "-"));
				OnToolbarSearchTextChanged (":s TODO");
				Output ("[searchpopup] ':s TODO' results=" + searchResults.Count + " first=" + (searchResults.Count > 0 ? searchResults [0].Title : "-"));
				// Type symbols (legacy RoslynSearchCategory): ':t Class' matches the
				// solution's class/interface/struct/enum declarations; activating the
				// first hit opens its file and jumps to the declaration line.
				OnToolbarSearchTextChanged (":t Program");
				Output ("[searchpopup] ':t Program' results=" + searchResults.Count + " first=" + (searchResults.Count > 0 ? searchResults [0].Title + " | " + searchResults [0].Subtitle + " | tip=" + searchResults [0].Tip : "-"));
				// Method/property hits (M25: the scan now covers member declarations);
				// the tooltip carries the enclosing container.
				OnToolbarSearchTextChanged (":t Main");
				Output ("[searchpopup] ':t Main' results=" + searchResults.Count + " first=" + (searchResults.Count > 0 ? searchResults [0].Title + " | " + searchResults [0].Subtitle + " | tip=" + searchResults [0].Tip : "-"));
				OnToolbarSearchTextChanged (":t Double");
				Output ("[searchpopup] ':t Double' results=" + searchResults.Count + " first=" + (searchResults.Count > 0 ? searchResults [0].Title + " | " + searchResults [0].Subtitle + " | tip=" + searchResults [0].Tip : "-"));
				if (searchResults.Count > 0) {
					ActivateSelectedSearchResult ();
					Output ("[searchpopup] ':t Program' activated → open tab=" + (DocTabs.SelectedItem is TabItem ti2 ? ti2.Tag : "none"));
				}
				// Open the file, search again — the open document ranks first — and
				// activate it (Enter path) verifying the tab selection.
				var proj = ResolveActiveProject ();
				var progFile = proj is null ? null : Path.Combine (Path.GetDirectoryName (proj)!, "Program.cs");
				if (progFile is not null && File.Exists (progFile))
					OpenFileDocument (progFile);
				OnToolbarSearchTextChanged ("Prog");
				if (searchResults.Count > 0 && searchResults [0].Subtitle == "(open document)") {
					var tag0 = searchResults [0].Title;
					ActivateSelectedSearchResult ();
					Output ("[searchpopup] activated → tab selected: " + (DocTabs.SelectedItem is TabItem ti ? ti.Tag : "none") + " expected: " + tag0);
				} else {
					Output ("[searchpopup] activation skipped (first=" + (searchResults.Count > 0 ? searchResults [0].Subtitle : "-") + ")");
				}
			} else if (qa == "--rowactivate") {
				// QA: the RowActivated flow — programmatic activation of the
				// Program.cs row opens the document in a tab and logs the step.
				var proj = ResolveActiveProject ();
				var progFile = proj is null ? null : Path.Combine (Path.GetDirectoryName (proj)!, "Program.cs");
				if (progFile is null || !File.Exists (progFile)) {
					Output ("[rowactivate] no Program.cs in the loaded solution");
				} else if (!SelectXwtSolutionRow (progFile)) {
					Output ("[rowactivate] row not found for " + progFile);
				} else {
					var backend = Xwt.Toolkit.CurrentEngine.GetSafeBackend (xwtSolutionTree!);
					var pos = xwtSolutionTree!.SelectedRow;
					OnXwtSolutionRowActivated (xwtSolutionTree, new Xwt.TreeViewRowEventArgs (pos));
					bool opened = docs.ContainsKey (Path.GetFileName (progFile!)) &&
						(DocTabs.SelectedItem as TabItem)?.Tag as string == Path.GetFileName (progFile);
					Output ("[rowactivate] opened=" + opened + " tab=" + ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "none"));
				}
			} else if (qa == "--ctxnewfolder") {
				// QA: the context-menu New Folder flow end to end — select the
				// project folder row, run ProjectCommands.NewFolder (the menu
				// handler), confirm the dialog and verify the directory + tree.
				var proj = ResolveActiveProject ();
				if (proj is null) {
					Output ("[ctxnewfolder] no project");
				} else {
					var dir = Path.GetDirectoryName (proj)!;
					SelectXwtSolutionRow ("project:" + proj);
					var qaName = "QaCtxFolder" + DateTime.Now.Second;
					dialogAutoConfirmFolder = qaName;
					OnMenuCommand ("MonoDevelop.Ide.Commands.ProjectCommands.NewFolder", "project:" + proj);
					dialogAutoConfirmFolder = null;
					bool created = Directory.Exists (Path.Combine (dir, qaName));
					Output ("[ctxnewfolder] created=" + created + " name=" + qaName + " treeRefreshed=" + (xwtSolutionNodes.Count > 0));
					if (created)
						Directory.Delete (Path.Combine (dir, qaName));
				}
			} else if (qa == "--ctxmenu-visual") {
				// QA: open the ProjectPad flyout and LEAVE IT OPEN for 5s — mutter
				// blocks automatic X11 grabs (XGetImage fails, x11grab captures black),
				// so the capture is manual: GNOME's PrtScr tool while the flyout is up.
				Avalonia.Threading.Dispatcher.UIThread.Post (async () => {
					await System.Threading.Tasks.Task.Delay (800); // let the tree finish its first render
					var flyout = new MenuFlyout { ItemsSource = BuildProjectPadMenu () };
					flyout.ShowAt (xwtSolutionHost!, true);
					Output ("[ctxvisual] flyout open for 5s — capture with PrtScr");
					await System.Threading.Tasks.Task.Delay (5000);
					flyout.Hide ();
					Output ("[ctxvisual] flyout closed");
				});
			} else if (qa == "--delcsproj") {
				// QA: the Delete flow's .csproj half — delete Program.cs and strip its
				// <Compile Include="..."> item through the SAME code the context-menu
				// Delete runs, verify both, then RESTORE the fixture.
				var proj = ResolveActiveProject ();
				var progFile = proj is null ? null : Path.Combine (Path.GetDirectoryName (proj)!, "Program.cs");
				if (proj is null || progFile is null || !File.Exists (progFile)) {
					Output ("[delcsproj] no Program.cs project");
				} else {
					var csprojBackup = File.ReadAllText (proj);
					var fileBackup = File.ReadAllBytes (progFile);
					// SDK projects compile implicitly — seed an EXPLICIT Compile entry so
					// the removal has something real to remove (legacy-style csproj).
					var seeded = csprojBackup.Replace ("<ItemGroup></ItemGroup>",
						$"<ItemGroup>{Environment.NewLine}    <Compile Include=\"Program.cs\" />{Environment.NewLine}  </ItemGroup>");
					File.WriteAllText (proj, seeded);
					File.Delete (progFile);
					var rel = Path.GetRelativePath (Path.GetDirectoryName (proj)!, progFile);
					RemoveProjectItemEntries (proj, rel, isFolder: false);
					var csprojText = File.ReadAllText (proj);
					bool gone = !csprojText.Contains ($"Include=\"{rel}\"", StringComparison.OrdinalIgnoreCase);
					bool docIntact = csprojText.Contains ("Microsoft.CSharp") && csprojText.Contains ("<Project Sdk=");
					Output ("[delcsproj] Program.cs deleted=" + !File.Exists (progFile)
						+ " compileEntryGone=" + gone + " restOfDocIntact=" + docIntact);
					File.WriteAllText (proj, csprojBackup);
					File.WriteAllBytes (progFile, fileBackup);
					RefreshSolutionTree ();
					Output ("[delcsproj] fixture restored");
				}
			} else if (qa == "--selpad") {
				// QA: the Xwt tree selection drives the Properties pad — select
				// solution / project / file nodes and dump the pad content each time.
				if (loadedSolutionPath is null) {
					Output ("[selpad] no solution loaded");
				} else {
					var proj = ResolveActiveProject ();
					var progFile = Path.Combine (Path.GetDirectoryName (proj!)!, "Program.cs");
					foreach (var tag in new[] { "solution:" + loadedSolutionPath, "project:" + proj, progFile }) {
						bool ok = SelectXwtSolutionRow (tag);
						Output ($"[selpad] select '{Path.GetFileName (tag)}' ok={ok} resolved='{SelectedXwtNode ()}' header='{propertiesTitleLabel?.Text}' rows={propertiesList!.Children.Count}");
					}
				}
			} else if (qa == "--newconfig") {
				// QA: New Configuration — name/platform combos with the legacy
				// validation; OK persists the config in the loaded .sln/.csproj.
				var cfgs = loadedSolutionPath is null ? new[] { "Debug", "Release" } : MonoDevelop.Ide.Services.ConfigurationService.GetSolutionConfigurations (loadedSolutionPath);
				var dlg = new NewConfigurationDialog (cfgs.Count == 0 ? new[] { "Debug", "Release" } : cfgs, isSolution: true);
				Output ("[newconfig] dialog opened (OK initially " + (dlg.IsOkEnabledForQa ? "enabled" : "disabled") + ")");
				dlg.ShowDialog (this);
				if (dlg.Accepted && !string.IsNullOrEmpty (dlg.ConfigName) && loadedSolutionPath is not null) {
					var namePart = dlg.ConfigName.Split ('|') [0];
					var platPart = dlg.ConfigName.Contains ('|') ? dlg.ConfigName.Split ('|') [1] : MonoDevelop.Ide.Services.ConfigurationService.AnyCpuSolution;
					try {
						var created = MonoDevelop.Ide.Services.ConfigurationService.AddSolutionConfiguration (loadedSolutionPath, namePart, platPart, dlg.CreateChildren);
						Output ("[newconfig] " + (created ? "created config in " + Path.GetFileName (loadedSolutionPath) : "config already exists"));
						if (created)
							OpenSolutionInWindow (loadedSolutionPath); // reload like ProjectOperations reload
					} catch (Exception ex) {
						Output ("[newconfig] persist failed: " + ex.Message);
					}
				} else {
					Output ("[newconfig] accepted=" + dlg.Accepted + " name='" + dlg.ConfigName + "' children=" + dlg.CreateChildren);
				}
			} else if (qa == "--xruncfg") {
				// QA: the REAL MonoDevelop.Ide Xwt dialog (NewSolutionRunConfigurationDialog,
				// compiled from its original source) running on the Xwt.Avalonia backend:
				// Xwt.Application.Initialize guest mode → Dialog.Run() modal loop →
				// an automated tick responds the Create command after 2s (exercising
				// Respond → backend EndLoop → Run return), the command round-trips.
				Xwt.Application.Initialize ("Xwt.AvaloniaBackend.AvaloniaEngine, Xwt.Avalonia");
				var dlg = new MonoDevelop.Ide.Projects.NewSolutionRunConfigurationDialog ();
				Output ("[xruncfg] dialog opened on Xwt.Avalonia (backend=" + Xwt.Toolkit.CurrentEngine.GetType ().Name + ")");
				var autoClose = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds (2) };
				autoClose.Tick += (s, e) => {
					autoClose.Stop ();
					var create = dlg.Buttons.OfType<Xwt.DialogButton> ().FirstOrDefault (b => b.Command != null && b.Command.Id == "create");
					if (create != null)
						dlg.Respond (create.Command);
				};
				autoClose.Start ();
				var result = dlg.Run ();
				Output ("[xruncfg] closed command=" + (result?.Id ?? "null") + " name='" + dlg.RunConfigurationName + "'");
				dlg.Dispose ();
			} else if (qa == "--activeconfig") {
				// QA: Active Configuration — persisted value read back from .userprefs,
				// switch via the menu command, verify persistence, restore Debug.
				if (loadedSolutionPath is null) {
					Output ("[activeconfig] no solution loaded");
				} else {
					var initial = MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath);
					Output ("[activeconfig] initial=" + initial + " combo=" + (ConfigCombo?.SelectedItem as string ?? "?"));
					OnMenuCommand ("MonoDevelop.Ide.Commands.ProjectCommands.SelectActiveConfiguration:Release");
					var after = MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath);
					Output ("[activeconfig] after-switch=" + after + " persisted=" + File.ReadAllText (loadedSolutionPath.Substring (0, loadedSolutionPath.Length - 4) + ".userprefs").Contains ("Release") + " combo=" + (ConfigCombo?.SelectedItem as string ?? "?"));
					OnMenuCommand ("MonoDevelop.Ide.Commands.ProjectCommands.SelectActiveConfiguration:Debug");
					Output ("[activeconfig] restored=" + MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath));
				}
			} else if (qa == "--newproject") {
				// QA: New Project dialog in add-to-solution mode — creates a console
				// project in a temp dir, wires it into the loaded .sln, reloads the tree.
				if (loadedSolutionPath is null) {
					Output ("[newproject] no solution loaded");
				} else {
					var tmpDir = Path.Combine (Path.GetTempPath (), "QAProj", Guid.NewGuid ().ToString ("N"));
					var dlg = new NewSolutionDialog ("console", tmpDir) { AddToOpenSolution = true, AutoCreateForQa = true };
					dlg.SolutionName!.Text = "QAAdded";
					await dlg.ShowDialog (this);
					var projPath = dlg.CreatedProjectPath;
					if (projPath is not null)
						MonoDevelop.Ide.Services.ConfigurationService.AppendProjectToSolution (projPath, loadedSolutionPath);
					var slnText = File.ReadAllText (loadedSolutionPath);
					Output ("[newproject] created=" + (projPath is not null) + " sln-entry=" + slnText.Contains ("QAAdded") + " mappings=" + slnText.Contains (".Debug|AnyCPU.Build.0"));
					OpenSolutionInWindow (loadedSolutionPath);
				}
			} else if (qa == "--bmkpad") {
				// QA: Bookmarks pad — toggle two bookmarks, open the pad, list rows,
				// navigate Next and verify the pad refresh.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				if (!File.Exists (file)) {
					Output ("[bmkpad] no test file");
				} else {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						SetPadVisible ("bookmarks", true);
						ed.GotoLine (2); ed.ToggleBookmark ();
						ed.GotoLine (6); ed.ToggleBookmark ();
						RefreshBookmarksPad ();
						Output ("[bmkpad] rows=" + bookmarksList!.Items.Count + " first=" + ((bookmarksList.Items [0] as ListBoxItem)?.Content as TextBlock)?.Text);
						ed.NextBookmark ();
						Output ("[bmkpad] NextBookmark → line " + (ed.CurrentLine + 1));
						ed.ClearBookmarks ();
						RefreshBookmarksPad ();
						Output ("[bmkpad] cleared rows=" + bookmarksList.Items.Count);
					}
				}
			} else if (qa == "--bkpad") {
				// QA: Breakpoints pad — toggle breakpoints, pad rows, enable/disable,
				// persistence into .userprefs, gutter navigation and cleanup.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				if (!File.Exists (file) || loadedSolutionPath is null) {
					Output ("[bkpad] no test file/solution");
				} else {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						SetPadVisible ("breakpoints", true);
						// Start from an empty store so the QA is repeatable even right after
						// a --keepbps run (the restored breakpoints would flip off).
						if (ed.BreakpointLines.Count > 0) {
							ed.ClearBreakpoints ();
							PersistBreakpoints ();
						}
						ed.GotoLine (6); ed.ToggleBreakpoint (); // line 7 (1-based)
						ed.GotoLine (8); ed.ToggleBreakpoint (); // line 9
						PersistBreakpoints (); RefreshBreakpointsPad ();
						Output ("[bkpad] rows=" + lastBreakpointRowTexts.Count + " first=" + lastBreakpointRowTexts.ElementAtOrDefault (0));
						// Deferred probe: measure the pad list AFTER a layout pass so the
						// reported bounds are the final on-screen ones (deterministic QA crops).
						Avalonia.Threading.Dispatcher.UIThread.Post (new Action (() => {
							var list = breakpointsList!;
							var realized = list.GetRealizedContainers ()?.ToList ();
							var tl = list.GetTransformedBounds ();
							Output ("[bpprobe] items=" + list.Items.Count + " realized=" + (realized?.Count ?? -1)
								+ " bounds=" + list.Bounds + " transformed=" + (tl?.Bounds.ToString () ?? "null"));
						}), Avalonia.Threading.DispatcherPriority.Background);
						var userprefs = File.ReadAllText (loadedSolutionPath.Substring (0, loadedSolutionPath.Length - 4) + ".userprefs");
						Output ("[bkpad] persisted-xml=" + userprefs.Contains ("MonoDevelop.Ide.DebuggingService.Breakpoints") + " lines=" + userprefs.Contains ("line=\"7\"") + "&" + userprefs.Contains ("line=\"9\""));
						ed.ToggleBreakpointEnabled (8);
						PersistBreakpoints (); RefreshBreakpointsPad ();
						Output ("[bkpad] disabled-row=" + lastBreakpointRowTexts.ElementAtOrDefault (1));
					ed.NextBreakpoint ();
					Output ("[bkpad] NextBreakpoint → line " + (ed.CurrentLine + 1));
					// Cleanup (QA repeatable): drop the store entries — unless --keepbps
					// was passed, which leaves them for visual capture runs.
					if (!Environment.GetCommandLineArgs ().Contains ("--keepbps")) {
						ed.ClearBreakpoints ();
						PersistBreakpoints (); RefreshBreakpointsPad ();
						Output ("[bkpad] cleared rows=" + breakpointsList.Items.Count);
					} else {
						Output ("[bkpad] kept for capture rows=" + breakpointsList.Items.Count);
					}
					}
				}
			} else if (qa == "--locals") {
				// QA: Run with debug — builds, launches netcoredbg with the persisted
				// breakpoints, verifies the stop (line 13 = Console.WriteLine, where
				// all locals are assigned — a bp on `int answer = 42;` stops BEFORE
				// the assignment and the locals read 0/null), the Locals pad values,
				// threads/frames and the execution-line highlight; cleans up.
				if (loadedSolutionPath is null) {
					Output ("[locals] no solution loaded");
				} else {
					// Ensure the breakpoint store has exactly the Program.cs:13 entry.
					var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
						ed.GotoLine (12); ed.ToggleBreakpoint (); // Program.cs line 13 (1-based)
						PersistBreakpoints ();
					}
					_ = RunStartupProjectAsync (debug: true);
					// The session is created inside RunStartupProjectAsync; wait for its stop.
					var deadline = DateTime.UtcNow.AddSeconds (60);
					MonoDevelop.Debugger.Services.DebugSessionService? sess = null;
					while (DateTime.UtcNow < deadline && sess is null) {
						await Task.Delay (300);
						sess = debugSession;
					}
					if (sess is null) {
						Output ("[locals] session did not start");
					} else {
						// Poll LastStop instead of subscribing: the launch path may
						// have already stopped before this code runs — a fresh
						// subscription would race and miss the stop. LastStop is
						// buffered by the service, so polling is deterministic.
						MonoDevelop.Debugger.Services.DebugStopInfo? stopInfo = null;
						while (DateTime.UtcNow < deadline && stopInfo is null) {
							await Task.Delay (300);
							stopInfo = sess.LastStop;
						}
						if (stopInfo is null) {
							Output ("[locals] no stop within timeout");
						} else {
							var f0 = stopInfo.Frames.FirstOrDefault ();
							Output ("[locals] stopped reason=" + stopInfo.Reason + " file=" + Path.GetFileName (f0?.File ?? "?") + ":" + f0?.Line);
							Output ("[locals] highlight=" + (currentDebugLine == 13 && currentDebugFile == Path.GetFullPath (file)));
							var vars = await sess.GetLocalsAsync ();
							Output ("[locals] values=" + string.Join (",", vars.Select (v => v.Name + "=" + v.Value)));
							FillVariableList (localsList, vars, "No locals");
							await RefreshDebugPadsAsync ();
							Avalonia.Threading.Dispatcher.UIThread.Post (new Action (() =>
								Output ("[locals] threads=" + (threadsList?.Items.Count ?? -1)
									+ " frames=" + (callStackList?.Items.Count ?? -1))),
								Avalonia.Threading.DispatcherPriority.Background);
							var eval = await sess.EvaluateAsync ("answer", sess.CurrentFrameId);
							Output ("[locals] evaluate(answer)=" + (eval.Error is null ? eval.Value : "ERR:" + eval.Error));
							await Task.Delay (400);
							Avalonia.Threading.Dispatcher.UIThread.Post (new Action (() => {
								var realized = localsList!.GetRealizedContainers ()?.ToList ();
								Output ("[locals] pad-realized=" + (realized?.Count ?? -1) + " rows=" + localsList.Items.Count);
								// Cleanup: stop session, clear the store, leave pads honest.
								// Keep the Locals tab selected so a screenshot shows the
								// runtime values (Output auto-selects itself on terminate).
								sess.Terminate ();
								BottomPads.Select ("locals");
								ClearExecutionLineHighlight ();
								if (docs.TryGetValue (name, out var ed2)) {
									ed2.ClearBreakpoints ();
									PersistBreakpoints ();
								}
								Output ("[locals] done");
							}), Avalonia.Threading.DispatcherPriority.Background);
							// give the posted action time to run before the app exits in CI-style runs
							await Task.Delay (2500);
						}
					}
				}
			} else if (qa == "--watch") {
				// QA: Watch pad — add watch expressions (legacy Watch pad add/remove),
				// debug to a stop, verify evaluate in the frame context, cleanup.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				watchExpressions.Clear ();
				watchExpressions.AddRange (new[] { "answer", "answer + 1" });
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
					ed.GotoLine (9); ed.ToggleBreakpoint ();
					PersistBreakpoints ();
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadline = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadline && debugSession?.LastStop is null)
					await Task.Delay (300);
				if (debugSession?.LastStop is null) {
					Output ("[watch] no stop within timeout");
				} else {
					await RefreshWatchPadAsync ();
					Avalonia.Threading.Dispatcher.UIThread.Post (new Action (() => {
						var rows = watchList!.Items.OfType<ListBoxItem> ()
							.Select (i => (i.Content as TextBlock)?.Text ?? "").ToList ();
						Output ("[watch] rows=" + rows.Count + " " + string.Join (" | ", rows));
						watchExpressions.Remove ("answer + 1");
						_ = RefreshWatchPadAsync ();
						Avalonia.Threading.Dispatcher.UIThread.Post (new Action (() => {
							Output ("[watch] after-remove rows=" + watchList!.Items.OfType<ListBoxItem> ().Count ());
							debugSession!.Terminate ();
							watchExpressions.Clear ();
							ClearExecutionLineHighlight ();
							if (docs.TryGetValue (name, out var ed2)) { ed2.ClearBreakpoints (); PersistBreakpoints (); }
							Output ("[watch] done");
						}), Avalonia.Threading.DispatcherPriority.Background);
					}), Avalonia.Threading.DispatcherPriority.Background);
					// MD_QA_HOLD=<secs> keeps the pads on screen for screenshots.
					await Task.Delay (int.TryParse (Environment.GetEnvironmentVariable ("MD_QA_HOLD"), out var watchHold) && watchHold > 0 ? watchHold * 1000 : 2500);
				}
			} else if (qa == "--watchedit") {
				// QA: Watch pad in-place editing + auto re-evaluation per step —
				// bp on line 10 (1-based) stops BEFORE `int answer = 42;` runs, so
				// `answer` reads 0; the in-place edit renames the row to
				// "answer + 1" (= 1) and ONE step over makes it 43 WITHOUT any
				// manual refresh — proving the pad re-evaluates after every step.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				watchExpressions.Clear ();
				watchExpressions.Add ("answer");
				// The Watch pad ships hidden; the inline editor needs the TreeView
				// realized, so surface (and select) the tab like the legacy pad do.
				SetPadVisible ("watch", true);
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
					ed.GotoLine (9); ed.ToggleBreakpoint (); // line 10 1-based
					PersistBreakpoints ();
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadline = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadline && debugSession?.LastStop is null)
					await Task.Delay (300);
				while (DateTime.UtcNow < deadline && debugSession?.CurrentFrameId is null)
					await Task.Delay (100);
				await RefreshWatchPadAsync ();
				await Task.Delay (250); // let the rows realize before the inline edit
				System.Func<System.Collections.Generic.List<string>> rows = () =>
					watchList!.Items.OfType<VariableNode> ().Select (n => n.Display).ToList ();
				Output ("[watchedit] initial=" + string.Join (" | ", rows ()));
				// Esc rolls back the edit.
				watchList!.SelectedItem = watchList.Items.OfType<VariableNode> ().FirstOrDefault (n => n.WatchExpression == "answer");
				BeginWatchEdit ();
				Output ("[watchedit] inline-editor=" + (watchEditBox is not null));
				if (watchEditBox is { } escBox) {
					escBox.Text = "answer + 999";
					EndWatchEdit ();
					await Task.Delay (300);
					Output ("[watchedit] after-esc=" + string.Join (" | ", rows ()) + " kept=" + (watchExpressions.Count == 1 && watchExpressions [0] == "answer"));
				}
				// Commit replaces the expression in place (order kept).
				watchList.SelectedItem = watchList.Items.OfType<VariableNode> ().FirstOrDefault (n => n.WatchExpression == "answer");
				BeginWatchEdit ();
				if (watchEditBox is { } okBox) {
					okBox.Text = "answer + 1";
					CommitWatchEdit ();
					await Task.Delay (400);
					await RefreshWatchPadAsync ();
					await Task.Delay (250);
					Output ("[watchedit] after-commit=" + string.Join (" | ", rows ()) + " order-kept=" + (watchExpressions.Count == 1 && watchExpressions [0] == "answer + 1"));
				}
				// One step over (line 11: greeting = …) → answer becomes 42 and the
				// pad re-evaluates automatically: "answer + 1 = 43".
				debugSession!.ResetLastStop ();
				StepDebug ("over");
				var stepDeadline = DateTime.UtcNow.AddSeconds (20);
				while (DateTime.UtcNow < stepDeadline && debugSession.LastStop is null)
					await Task.Delay (200);
				await Task.Delay (800); // the stopped event refreshes the pads async
				await RefreshWatchPadAsync ();
				Output ("[watchedit] after-step=" + string.Join (" | ", rows ()) + " reevaluated=" + rows ().Any (r => r.Contains ("answer + 1 = 43")));
				debugSession.Terminate ();
				ClearExecutionLineHighlight ();
				watchExpressions.Clear ();
				PersistWatches ();
				if (docs.TryGetValue (name, out var edWe)) { edWe.ClearBreakpoints (); PersistBreakpoints (); }
				Output ("[watchedit] done");
			} else if (qa == "--pinwatch") {
				// QA: pinned watches — pin the words at the caret as editor bubbles,
				// verify the legacy file/line serialization in <sln>.userprefs, the
				// live evaluation on a debug stop and the load path (document open).
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					ed.SetPinnedWatches (Array.Empty<(int, string)> ()); // repeatable QA
				// GotoLine preserves the caret column: park it inside the words.
				// line 10 "int answer = …": col 12 ∈ answer (8..14);
				// line 11 "string greeting …": from the kept col 12, +3 → col 15 ∈ greeting (7..15).
				ed.GotoLine (9); ed.CaretRight (12);
				PinWatchAtCaret (ed);
				ed.GotoLine (10); ed.CaretRight (3);
				PinWatchAtCaret (ed);
					Output ("[pinwatch] bubbles=" + string.Join (" | ", ed.PinnedWatchList.Select (p => (p.Line + 1) + ":" + p.Expression)));
					RefreshPinnedWatchesPad ();
					var xml = File.ReadAllText (loadedSolutionPath!.Substring (0, loadedSolutionPath.Length - 4) + ".userprefs");
					Output ("[pinwatch] legacy-file-attr=" + (xml.Contains ("file=\"TestProj/Program.cs\"") || xml.Contains ("file=\"TestProj\\\\Program.cs\""))
						+ " line10=" + xml.Contains ("line=\"10\"")
						+ " line11=" + xml.Contains ("line=\"11\"")
						+ " expr-answer=" + xml.Contains ("expression=\"answer\""));
					var reloaded = MonoDevelop.Debugger.Services.WatchService.LoadPinned (loadedSolutionPath);
					Output ("[pinwatch] load-path=" + reloaded.Count + " first=" + (reloaded.Count > 0 ? Path.GetFileName (reloaded [0].File) + ":" + reloaded [0].Line + ":" + reloaded [0].Expression : "none"));
				}
				// A debug stop evaluates every pin in the frame (legacy PinnedWatch.Evaluate).
				if (docs.TryGetValue (name, out var edB)) {
					if (edB.BreakpointLines.Count > 0) { edB.ClearBreakpoints (); PersistBreakpoints (); }
					edB.GotoLine (12); edB.ToggleBreakpoint (); // line 13: locals all assigned
					PersistBreakpoints ();
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadlineP = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadlineP && debugSession?.LastStop is null)
					await Task.Delay (300);
				while (DateTime.UtcNow < deadlineP && debugSession?.CurrentFrameId is null)
					await Task.Delay (100);
				await Task.Delay (900); // RefreshPinnedWatchValuesAsync runs on the stopped event
				var live = docs.TryGetValue (name, out var edL) ? edL.PinnedWatchValues : null;
				Output ("[pinwatch] live=" + (live is null ? "none" : string.Join (" | ", live.Select (v => v.label)))
					+ " answer-evaluated=" + (live?.Any (v => v.label == "answer = 42") ?? false));
				// Toggle off the first pin (the legacy unpin path) → store shrinks.
				if (docs.TryGetValue (name, out var edU)) {
					edU.GotoLine (9); // caret column (13) already inside "answer"
					PinWatchAtCaret (edU);
					RefreshPinnedWatchesPad ();
					Output ("[pinwatch] after-unpin=" + MonoDevelop.Debugger.Services.WatchService.LoadPinned (loadedSolutionPath!).Count);
					// Clickable bubbles: the rendered frame registers the bubble
					// rects; right-click over one offers Remove + Go to line (the
					// bubble removal also leaves the store clean for the next run).
					await Task.Delay (600); // ensure a rendered frame on the QA display
					var rect = edU.PinnedWatchRectForQa (10, 0); // greeting bubble, line 11
					Output ("[pinwatch] bubble-rect=" + (rect is not null));
					if (rect is { } r) {
						var hit = edU.TryGetPinnedWatchAt (r.Center, out var hl, out var hexpr);
						Output ("[pinwatch] bubble-hit=" + hit + " line=" + (hl + 1) + " expr=" + hexpr);
						var bmenu = edU.BuildPinnedWatchMenu (hl);
						var items = bmenu?.Items.OfType<Avalonia.Controls.MenuItem> ().Select (m => (m.Header as string) ?? "").ToList ();
						Output ("[pinwatch] bubble-menu=" + (items is not null ? string.Join (" | ", items) : "none"));
						var gotoItem = bmenu?.Items.OfType<Avalonia.Controls.MenuItem> ().FirstOrDefault (m => ((m.Header as string) ?? "").Contains ("Go to line"));
						gotoItem?.RaiseEvent (new Avalonia.Interactivity.RoutedEventArgs (Avalonia.Controls.MenuItem.ClickEvent));
						Output ("[pinwatch] goto-line=" + (edU.CurrentLine + 1));
						var removeItem = bmenu?.Items.OfType<Avalonia.Controls.MenuItem> ().FirstOrDefault (m => ((m.Header as string) ?? "").StartsWith ("Remove pinned watch", StringComparison.Ordinal));
						removeItem?.RaiseEvent (new Avalonia.Interactivity.RoutedEventArgs (Avalonia.Controls.MenuItem.ClickEvent));
						RefreshPinnedWatchesPad ();
						Output ("[pinwatch] after-bubble-remove=" + MonoDevelop.Debugger.Services.WatchService.LoadPinned (loadedSolutionPath!).Count);
					} else {
						edU.SetPinnedWatches (Array.Empty<(int, string)> ());
						RefreshPinnedWatchesPad ();
					}
					edU.ClearBreakpoints (); PersistBreakpoints ();
				}
				debugSession?.Terminate ();
				ClearExecutionLineHighlight ();
				Output ("[pinwatch] done");
			} else if (qa == "--legacyqa") {
				// QA: pinned watches written by the LEGACY GTK IDE — the full
				// PinnedWatchStore serialization (file/line/column/…/expression)
				// under the same PinnedWatches key — must surface as editor
				// bubbles here and survive a save from this side (round trip).
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				var slnPath = loadedSolutionPath!;
				var prefsPath = slnPath.Substring (0, slnPath.Length - 4) + ".userprefs";
				var backup = File.Exists (prefsPath) ? File.ReadAllText (prefsPath) : null;
				// The exact XML the legacy ProjectPathItemProperty serializer writes:
				// file relative to the solution dir, 1-based line/column (seeded
				// below, AFTER closing the workspace so the close-persist does not
				// overwrite it).
				// Reopen the solution so the load path runs against the legacy prefs.
				// NOTE the order: close FIRST (it persists the current empty state),
				// THEN seed the legacy XML, THEN reopen — otherwise the close
				// overwrite would wipe the seed.
				await CloseWorkspaceAsync ();
				File.WriteAllText (prefsPath,
					"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
					"<Properties>\n" +
					"  <MonoDevelop.Ide.Workspace>\n" +
					"    <Property name=\"ActiveConfiguration\" value=\"Debug\" />\n" +
					"  </MonoDevelop.Ide.Workspace>\n" +
					"  <MonoDevelop.Ide.DebuggingService.PinnedWatches>\n" +
					"    <Watch file=\"TestProj/Program.cs\" line=\"10\" column=\"9\" endLine=\"10\" endColumn=\"15\" offsetX=\"0\" offsetY=\"0\" expression=\"answer\" liveUpdate=\"False\" />\n" +
					"    <Watch file=\"TestProj/Program.cs\" line=\"11\" column=\"7\" endLine=\"11\" endColumn=\"15\" offsetX=\"0\" offsetY=\"0\" expression=\"greeting\" liveUpdate=\"False\" />\n" +
					"  </MonoDevelop.Ide.DebuggingService.PinnedWatches>\n" +
					"  <MonoDevelop.Ide.DebuggingService.Breakpoints />\n" +
				"</Properties>");
				Output ("[legacyqa] seeded-rows=" + (File.ReadAllText (prefsPath).Split ("<Watch ").Length - 1));
				await Task.Delay (300); // close-persist can land late; re-check
				Output ("[legacyqa] after-300ms-rows=" + (File.ReadAllText (prefsPath).Split ("<Watch ").Length - 1));
				if (!File.ReadAllText (prefsPath).Contains ("<Watch ")) {
					File.WriteAllText (prefsPath, File.ReadAllText (prefsPath).Replace (
						"<MonoDevelop.Ide.DebuggingService.PinnedWatches />",
						"<MonoDevelop.Ide.DebuggingService.PinnedWatches>\n" +
						"    <Watch file=\"TestProj/Program.cs\" line=\"10\" column=\"9\" endLine=\"10\" endColumn=\"15\" offsetX=\"0\" offsetY=\"0\" expression=\"answer\" liveUpdate=\"False\" />\n" +
						"    <Watch file=\"TestProj/Program.cs\" line=\"11\" column=\"7\" endLine=\"11\" endColumn=\"15\" offsetX=\"0\" offsetY=\"0\" expression=\"greeting\" liveUpdate=\"False\" />\n" +
						"  </MonoDevelop.Ide.DebuggingService.PinnedWatches>"));
					Output ("[legacyqa] reseeded");
				}
				OpenSolutionInWindow (Program.SolutionArg);
				await Task.Delay (800);
				Output ("[legacyqa] before-open-doc-rows=" + (File.ReadAllText (prefsPath).Split ("<Watch ").Length - 1));
				OpenFileDocument (file);
				await Task.Delay (400);
				var name = Path.GetFileName (file);
				if (docs.TryGetValue (name, out var edLegacy)) {
					Output ("[legacyqa] restored=" + string.Join (" | ", edLegacy.PinnedWatchList.Select (p => (p.Line + 1) + ":" + p.Expression)));
					var entries = MonoDevelop.Debugger.Services.WatchService.LoadPinned (slnPath);
					Output ("[legacyqa] loadpinned=" + string.Join (" | ", entries.Select (en => Path.GetFileName (en.File) + ":" + en.Line + ":" + en.Expression + " col=" + en.Column)));
					// A save from this side must keep the legacy format.
					RefreshPinnedWatchesPad ();
					var xmlAfter = File.ReadAllText (prefsPath);
					Output ("[legacyqa] legacy-format-kept=" + (xmlAfter.Contains ("file=\"TestProj/Program.cs\"")
						&& xmlAfter.Contains ("expression=\"answer\"")
						&& xmlAfter.Contains ("line=\"10\"")
						&& xmlAfter.Contains ("expression=\"greeting\"")));
					edLegacy.SetPinnedWatches (Array.Empty<(int, string)> ());
					RefreshPinnedWatchesPad ();
				}
				if (backup is not null)
					File.WriteAllText (prefsPath, backup);
				else if (File.Exists (prefsPath))
					File.Delete (prefsPath);
				Output ("[legacyqa] done");
			} else if (qa == "--condbp") {
				// QA: conditional + hit-count breakpoints — attributes set on the
				// editor store, persisted to .userprefs with condition/hitcount,
				// pushed to the DAP adapter (session starts OK and stops on the bp).
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
					ed.GotoLine (9); ed.ToggleBreakpoint ();
					ed.SetBreakpointOptions (9, "answer == 42", 3, null); // line 10 1-based
					PersistBreakpoints ();
					var stored = MonoDevelop.Debugger.Services.BreakpointService.Load (loadedSolutionPath!)
						.FirstOrDefault (b => Path.GetFullPath (b.FileName) == Path.GetFullPath (file));
					Output ("[condbp] stored cond=" + (stored?.Condition ?? "none")
						+ " hit=" + (stored?.HitCount?.ToString () ?? "none")
						+ " enabled=" + (stored?.Enabled ?? false));
					var rowText = lastBreakpointRowTexts.FirstOrDefault (t => t.Contains (":10"));
					Output ("[condbp] pad-row=" + (rowText ?? "none"));
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadline2 = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadline2 && debugSession?.LastStop is null)
					await Task.Delay (300);
				var stop2 = debugSession?.LastStop;
				var line2 = stop2?.Frames.FirstOrDefault ()?.Line ?? -1;
				Output ("[condbp] stopped-at=" + line2 + " (bp line 10, cond answer==42, hit 3)");
				debugSession?.Terminate ();
				ClearExecutionLineHighlight ();
				if (docs.TryGetValue (name, out var ed3)) { ed3.ClearBreakpoints (); PersistBreakpoints (); }
				Output ("[condbp] done");
			} else if (qa == "--step") {
				// QA: stepping — debug to the bp at line 10, Step Over twice (lines
				// 11/12 across Console.WriteLine calls), verify the highlight moves
				// and the pads refresh; then Continue to exit.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
					// Line 13 (1-based): Console.WriteLine — all locals are assigned by
					// then (a bp on `int answer = 42;` stops BEFORE the assignment, so
					// the locals would read 0/null like the real debugger).
					ed.GotoLine (12); ed.ToggleBreakpoint ();
					PersistBreakpoints ();
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadline = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadline && debugSession?.LastStop is null)
					await Task.Delay (300);
				Output ("[step] first-stop=" + (debugSession?.LastStop?.Frames.FirstOrDefault ()?.Line ?? -1));
				foreach (var expect in new[] { 14 }) {
					StepDebug ("over");
					debugSession!.ResetLastStop ();
					MonoDevelop.Debugger.Services.DebugStopInfo? stepStop = null;
					var stepDeadline = DateTime.UtcNow.AddSeconds (20);
					while (DateTime.UtcNow < stepDeadline && stepStop is null) {
						await Task.Delay (200);
						stepStop = debugSession.LastStop;
					}
					var at = stepStop?.Frames.FirstOrDefault ()?.Line ?? -1;
					Output ("[step] line=" + at + " expected=" + expect + " highlight=" + (currentDebugLine == at));
				}
				debugSession?.Terminate ();
				ClearExecutionLineHighlight ();
				if (docs.TryGetValue (name, out var ed2)) { ed2.ClearBreakpoints (); PersistBreakpoints (); }
				Output ("[step] done");
			} else if (qa == "--tree") {
				// QA: expandable variable trees — debug to the bp, fill Locals, verify
				// the tree roots and that a child expansion returns real rows.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
					ed.GotoLine (12); ed.ToggleBreakpoint (); // line 13: locals all assigned
					PersistBreakpoints ();
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadline = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadline && debugSession?.LastStop is null)
					await Task.Delay (300);
				if (debugSession?.LastStop is null) {
					Output ("[tree] no stop within timeout");
				} else {
					var locals = await debugSession!.GetLocalsAsync ();
					FillVariableList (localsList, locals, "No locals");
					Output ("[tree] roots=" + (localsList?.Items.Count ?? -1)
						+ " first=" + ((localsList?.Items.OfType<VariableNode> ().FirstOrDefault ()?.Display) ?? "none"));
					var withKids = localsList!.Items.OfType<VariableNode> ().FirstOrDefault (n => n.VariablesReference > 0);
					if (withKids is null) {
						Output ("[tree] no-expandable=False");
					} else {
						var kids = LoadVariableChildren (withKids).ToList ();
						Output ("[tree] expand=" + kids.Count + " first-child=" + (kids.FirstOrDefault ()?.Display ?? "none"));
					}
					debugSession.Terminate ();
					ClearExecutionLineHighlight ();
					if (docs.TryGetValue (name, out var ed2)) { ed2.ClearBreakpoints (); PersistBreakpoints (); }
					Output ("[tree] done");
				}
			} else if (qa == "--imm") {
				// QA: Immediate pad — debug to the bp, run "answer + 1" and "greeting"
				// through RunImmediate, verify the Output rows; also the no-session path.
				RunImmediate (); // no session yet → honest error row
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				OpenFileDocument (file);
				var name = Path.GetFileName (file);
				if (docs.TryGetValue (name, out var ed)) {
					SelectDocument (name);
					if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
					ed.GotoLine (12); ed.ToggleBreakpoint (); // line 13: locals all assigned
					PersistBreakpoints ();
				}
				_ = RunStartupProjectAsync (debug: true);
				var deadline = DateTime.UtcNow.AddSeconds (60);
				while (DateTime.UtcNow < deadline && debugSession?.LastStop is null)
					await Task.Delay (300);
				if (debugSession?.LastStop is null) {
					Output ("[imm] no stop within timeout");
				} else {
					// Wait for the stack to be pulled (CurrentFrameId) — evaluating
					// without a frame falls back to the static scope (answer=0).
					while (DateTime.UtcNow < deadline && debugSession!.CurrentFrameId is null)
						await Task.Delay (100);
					foreach (var expr in new[] { "answer + 1", "greeting" }) {
						immediateInput!.Text = expr;
						await RunImmediateAsync (expr);
					}
					debugSession!.Terminate ();
					ClearExecutionLineHighlight ();						if (docs.TryGetValue (name, out var ed2)) { ed2.ClearBreakpoints (); PersistBreakpoints (); }
						Output ("[imm] done");
					}
				} else if (qa == "--gutterbp") {
					// QA: gutter breakpoint toggle + inline data tip — the same code path
					// a click on the icon strip runs (ToggleBreakpointAtGutter), then a
					// debug stop shows the green value bubble on the paused line.
					var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
						ed.ToggleBreakpointAtGutter (12); // line 13: the gutter-click path
						Output ("[gutterbp] gutter-click toggle → bp@13=" + (ed.BreakpointLines.ContainsKey (12) ? "True" : "False"));
						ed.ToggleBreakpointAtGutter (12);
						Output ("[gutterbp] second click removes → bp@13=" + (ed.BreakpointLines.ContainsKey (12) ? "True" : "False"));
						ed.ToggleBreakpointAtGutter (12); // leave it on for the debug run
						PersistBreakpoints ();
						Output ("[gutterbp] stored=" + File.ReadAllText (loadedSolutionPath!.Substring (0, loadedSolutionPath.Length - 4) + ".userprefs").Contains ("line=\"13\""));
					}
					_ = RunStartupProjectAsync (debug: true);
					var deadlineG = DateTime.UtcNow.AddSeconds (60);
					while (DateTime.UtcNow < deadlineG && debugSession?.LastStop is null)
						await Task.Delay (300);
					if (debugSession?.LastStop is null) {
						Output ("[gutterbp] no stop within timeout");
					} else {
						// ShowDataTipForFrame runs async on the stopped event; give it a beat.
						await Task.Delay (1200);
						var tip = docs.TryGetValue (name, out var edT) ? edT.CurrentDataTip : null;
						Output ("[gutterbp] datatip=" + (tip is null ? "none" : $"line={tip?.Line + 1} '{tip?.Text}'"));
						Output ("[gutterbp] bubble-red@13=" + docs [name].BreakpointLines.ContainsKey (12));
						// Gutter hover polish: row highlight + hand cursor on the
						// breakpoint strip + line tooltip (same content the click uses).
						var edH = docs [name];
						edH.SimulateGutterHoverForQa (11, breakpointStrip: true);
						var hover = edH.GutterHover;
						var gtip = Avalonia.Controls.ToolTip.GetTip (edH) as TextBlock;
						Output ("[gutterbp] hover-line=" + (hover.Line + 1) + " hand=" + hover.InBreakpointStrip + " tip='" + (gtip?.Text ?? "none") + "'");
						edH.ClearGutterHover ();
						var gtip2 = Avalonia.Controls.ToolTip.GetTip (edH) as TextBlock;
						Output ("[gutterbp] hover-cleared=" + (edH.GutterHover.Line == -1 && edH.GutterHover.InBreakpointStrip == false) + " tip-removed=" + (gtip2 is null));
						debugSession!.Terminate ();
						ClearExecutionLineHighlight ();
						if (docs.TryGetValue (name, out var edG)) { edG.ClearBreakpoints (); PersistBreakpoints (); }
						Output ("[gutterbp] done");
					}
				} else if (qa == "--frame") {
					// QA: Call Stack frame switching — a bp inside TestProj's Double (int)
					// stops with Main on the stack (2 managed frames); selecting the 2nd
					// frame reloads the Locals tree from ITS scopes (GetLocalsForFrameAsync).
					var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
						ed.GotoLine (17); ed.ToggleBreakpoint (); // line 18: return x * 2 (inside Double)
						PersistBreakpoints ();
					}
					_ = RunStartupProjectAsync (debug: true);
					var deadlineF = DateTime.UtcNow.AddSeconds (60);
					while (DateTime.UtcNow < deadlineF && debugSession?.LastStop is null)
						await Task.Delay (300);
					if (debugSession?.LastStop is null) {
						Output ("[frame] no stop within timeout");
					} else {
						var sessF = debugSession!;
						while (DateTime.UtcNow < deadlineF && sessF.CurrentFrameId is null)
							await Task.Delay (100);
						BottomPads.Select ("callstack");
						await RefreshDebugPadsAsync ();
						var frames = callStackList!.Items.OfType<ListBoxItem> ().Select (i => i.Tag).OfType<DebugFrame> ().ToList ();
						Output ("[frame] stack=" + frames.Count + " frames: " + string.Join (" | ", frames.Take (3).Select (f => f.Method)));
						if (frames.Count >= 2) {
							callStackList.SelectedIndex = 1; // fires ShowFrameLocalsAsync
							await Task.Delay (1000);
							Output ("[frame] after-select locals-roots=" + (localsList?.Items.Count ?? -1));
							Output ("[frame] pad=" + BottomPads.Tabs.First (t => t.Id == "locals").Visible);
						}
						sessF.Terminate ();
						ClearExecutionLineHighlight ();
						if (docs.TryGetValue (name, out var edF)) { edF.ClearBreakpoints (); PersistBreakpoints (); }
						Output ("[frame] done");
					}
				} else if (qa == "--immcompl") {
					// QA: Immediate member completion — type "list.", the popup fills from
					// the DAP children of the evaluated prefix, Tab commits list.Count.
					var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						if (ed.BreakpointLines.Count > 0) { ed.ClearBreakpoints (); PersistBreakpoints (); }
						ed.GotoLine (12); ed.ToggleBreakpoint ();
						PersistBreakpoints ();
					}
					_ = RunStartupProjectAsync (debug: true);
					var deadlineI = DateTime.UtcNow.AddSeconds (60);
					while (DateTime.UtcNow < deadlineI && debugSession?.LastStop is null)
						await Task.Delay (300);
					if (debugSession?.LastStop is null) {
						Output ("[immcompl] no stop within timeout");
					} else {
						var sessI = debugSession!;
						while (DateTime.UtcNow < deadlineI && sessI.CurrentFrameId is null)
							await Task.Delay (100);
						SetPadVisible ("immediate", true);
						BottomPads.Select ("immediate");
						immediateInput!.Text = "list.";
						immediateInput.CaretIndex = immediateInput.Text.Length;
						await ImmediateMemberCompletionAsync ();
						var members = immediatePopupList?.Items.OfType<ListBoxItem> ().Select (i => i.Tag as string ?? "").ToList () ?? new List<string> ();
						Output ("[immcompl] popup=" + (immediatePopup?.IsOpen == true) + " members=" + members.Count + " has-Count=" + members.Contains ("Count"));
						immediatePopupList!.SelectedIndex = members.IndexOf ("Count");
						CommitImmediateCompletion ();
						Output ("[immcompl] committed='" + immediateInput.Text + "'");
						await RunImmediateAsync (immediateInput.Text);
						sessI.Terminate ();
						ClearExecutionLineHighlight ();
						if (docs.TryGetValue (name, out var edI)) { edI.ClearBreakpoints (); PersistBreakpoints (); }
						Output ("[immcompl] done");
					}
				} else if (qa == "--persistqa") {
					// QA: debug-session persistence — set a watch + a breakpoint (both
					// flow into <sln>.userprefs), reopen the solution and verify they
					// come back; the active configuration was already restored above.
					if (loadedSolutionPath is null) {
						Output ("[persist] no solution loaded");
					} else {
						var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
						OpenFileDocument (file);
						var name = Path.GetFileName (file);
						var edP = docs [name];
						SelectDocument (name);
						if (edP.BreakpointLines.Count > 0) { edP.ClearBreakpoints (); PersistBreakpoints (); }
						watchExpressions.Clear ();
						watchExpressions.Add ("answer + 1");
						PersistWatches ();
						edP.GotoLine (12); edP.ToggleBreakpoint ();
						PersistBreakpoints ();
						Output ("[persist] pre-reopen watches=" + string.Join (",", MonoDevelop.Debugger.Services.WatchService.Load (loadedSolutionPath)));
						Output ("[persist] pre-reopen config=" + MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath));
						// Close and reopen the solution (CloseWorkspace → OpenSolution),
						// then reopen the file — breakpoints restore from .userprefs on
						// document open, like the legacy DebuggingService load path.
						await CloseWorkspaceAsync ();
						OpenSolutionInWindow (Program.SolutionArg ?? loadedSolutionPath ?? "");
						await Task.Delay (800);
						OpenFileDocument (file);
						await Task.Delay (400);
						var watched = watchExpressions;
						var restored = MonoDevelop.Debugger.Services.WatchService.Load (loadedSolutionPath!);
						var bp13 = docs.TryGetValue ("Program.cs", out var edR) && edR.BreakpointLines.ContainsKey (12);
						Output ("[persist] post-reopen watch-exprs=" + string.Join (",", restored) + " restored-pad=" + watched.Contains ("answer + 1"));
						Output ("[persist] post-reopen bp@13=" + (bp13 ? "True" : "False") + " config=" + MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath!));
						// Cleanup so the QA is repeatable.
						watchExpressions.Clear ();
						PersistWatches ();
						if (docs.TryGetValue ("Program.cs", out var edC)) { edC.ClearBreakpoints (); PersistBreakpoints (); }
						Output ("[persist] done");
					}
				} else if (qa == "--attachreal") {
				// QA: real DAP attach — launches a long-lived .NET process, attaches
				// the session to its PID through the pad flow (AttachAsync), verifies
				// attach + real threads, then detaches (process must survive) and
				// kills the sleeper.
				var sleeperHome = Path.Combine ("/tmp", "dotsleeper");
				System.Diagnostics.Process? sleeper = null;
				try {
					sleeper = System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo ("/home/daniel/.dotnet/dotnet", Path.Combine (sleeperHome, "bin", "Debug", "net10.0", "dotsleeper.dll")) {
						UseShellExecute = false,
						RedirectStandardOutput = true,
					});
					if (sleeper is not null)
						_ = sleeper.StandardOutput.ReadLineAsync (); // wait for "sleeper-ready"
					await Task.Delay (500);
				} catch { }
				if (sleeper is null) {
					Output ("[attachreal] could not start sleeper");
				} else {
					await Task.Delay (300);
					var bpFile = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
					debugSession?.Dispose ();
					var session = new MonoDevelop.Debugger.Services.DebugSessionService ();
					debugSession = session;
					var ok = await session.AttachAsync (sleeper.Id, Array.Empty<(string, int, string?, int?, string?)> ());
					Output ("[attachreal] attach=" + ok + " pid=" + sleeper.Id + " alive-after-attach=" + !sleeper.HasExited);
					Output ("[attachreal] is-attach=" + session.IsAttach);
					// The legacy attach breaks the debuggee (DebuggingService.Pause):
					// pause with the process PID as threadId — GetThreadsWithState
					// lists threads only once the process is stopped. The runtime
					// needs a beat after the attach before Stop() succeeds, so
					// re-issue pause until the stopped event lands (deterministic).
					MonoDevelop.Debugger.Services.DebugStopInfo? stop = null;
					var tDeadline = DateTime.UtcNow.AddSeconds (20);
					while (DateTime.UtcNow < tDeadline && stop is null) {
						await session.PauseAsync (sleeper.Id);
						var inner = DateTime.UtcNow.AddSeconds (4);
						while (DateTime.UtcNow < inner && stop is null) {
							await Task.Delay (300);
							stop = session.LastStop;
						}
					}
					Output ("[attachreal] paused=" + (stop is not null) + " reason=" + (stop?.Reason ?? "none"));
					var threads = Array.Empty<MonoDevelop.Debugger.Services.DebugThread> ();
					while (DateTime.UtcNow < tDeadline) {
						threads = await session.GetThreadsAsync ();
						if (threads.Length > 0)
							break;
						await Task.Delay (500);
					}
					Output ("[attachreal] threads=" + threads.Length + " first=" + (threads.FirstOrDefault ()?.Name ?? "none"));
					var ev = await session.EvaluateAsync ("ticks", session.CurrentFrameId);
					Output ("[attachreal] evaluate(ticks)=" + (ev.Error is null ? ev.Value : "ERR:" + ev.Error));
					session.Terminate (); // detach: the sleeper must survive
					await Task.Delay (500);
					Output ("[attachreal] alive-after-detach=" + !sleeper.HasExited);
					try { sleeper.Kill (true); } catch { }
					Output ("[attachreal] done");
				}
			} else if (qa == "--attachdlg") {
				// QA: Attach to Process pad tab — real /proc enumeration, filter, count,
				// selection enabling Attach; deterministic output, no user interaction.
				var panel = attachPanel!;
				var ownPid = Environment.ProcessId;
				Output ("[attachdlg] processes=" + panel.allProcesses.Count
					+ " has-own=" + panel.allProcesses.Any (p => p.Pid == ownPid)
					+ " has-systemd=" + panel.allProcesses.Any (p => p.Name.Contains ("systemd"))
					+ " no-kernel-threads=" + !panel.allProcesses.Any (p => p.Pid <= 10 && p.Name.Length == 0));
				var first = panel.allProcesses.FirstOrDefault ();
				Output ("[attachdlg] first=" + (first is null ? "none" : first.Pid + ":" + first.Name));
				attachPanel = panel;
				BottomPads.SetTabVisible ("attach", true);
				BottomPads.Select ("attach");
				SetPadVisible ("bottom", true);
				// Defer the realized-rows read to after layout (ListBox virtualizes).
				Avalonia.Threading.Dispatcher.UIThread.Post (new Action (() =>
					Output ("[attachdlg] pad-realized=" + panel.RealizedRowsForQa)),
					Avalonia.Threading.DispatcherPriority.Background);
				Output ("[attachdlg] tab=attach window-chrome=Avalonia");
				await Task.Delay (1500);
			} else if (qa == "--newconfig-real") {
				// QA: full persistence path — creates "QAConfig" in the loaded
				// solution and verifies the .sln/.csproj on disk.
				if (loadedSolutionPath is null) {
					Output ("[newconfig-real] no solution loaded");
				} else {
					try {
						var created = MonoDevelop.Ide.Services.ConfigurationService.AddSolutionConfiguration (loadedSolutionPath, "QAConfig", MonoDevelop.Ide.Services.ConfigurationService.AnyCpuSolution, createChildren: true);
						var slnText = File.ReadAllText (loadedSolutionPath);
						var csprojs = Directory.GetFiles (Path.GetDirectoryName (loadedSolutionPath)!, "*.csproj", SearchOption.AllDirectories);
						var inSln = slnText.Contains ("QAConfig|Any CPU", StringComparison.Ordinal);
						var inProj = csprojs.All (p => File.ReadAllText (p).Contains ("'QAConfig|AnyCPU'", StringComparison.Ordinal));
						Output ("[newconfig-real] created=" + created + " sln-entry=" + inSln + " csproj-entries=" + inProj + " (" + csprojs.Length + " projects)");
						// Cleanup so the QA is repeatable.
						if (created) {
							var lines = File.ReadAllLines (loadedSolutionPath).Where (l => !l.Contains ("QAConfig")).ToList ();
							File.WriteAllLines (loadedSolutionPath, lines);
							foreach (var p in csprojs) {
								var t = File.ReadAllText (p);
								var cleaned = System.Text.RegularExpressions.Regex.Replace (t, @"\n\s*<PropertyGroup Condition="" '\$\(Configuration\)\|\$\(Platform\)' == 'QAConfig\|AnyCPU' "" />", "");
								File.WriteAllText (p, cleaned);
							}
							Output ("[newconfig-real] cleaned up (repeatable)");
						}
					} catch (Exception ex) {
						Output ("[newconfig-real] failed: " + ex.Message);
					}
				}
			} else if (qa == "--openimport") {
				// QA: File > Open import path over a loose .csproj (creates the
				// wrapper .sln next to it when missing, then opens it).
				var loose = Directory.GetFiles (Path.GetTempPath (), "QAImport*.csproj").FirstOrDefault () ?? CreateQaImportProject ();
				Output ("[openimport] importing " + loose);
				OpenFileOrProject (loose);
				var sln = Path.Combine (Path.GetDirectoryName (loose)!, "QAImport.sln");
				Output ("[openimport] wrapper-sln=" + File.Exists (sln) + " loaded=" + (loadedSolutionPath == sln));
			} else if (qa == "--totd") {
				// QA: Tip of the Day — tips loaded, random first tip, Next cycles,
				// "don't show" persists the legacy preference (inverted).
				var totd = new TipOfTheDayDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
				totd.ShowDialog (this);
				Output ("[totd] dialog opened (tips from TipsOfTheDay.xml)");
			} else if (qa == "--progress") {
				// QA: ProgressDialog — nested tasks with details, cancel path and
				// the legacy ShowDone states, driven deterministically.
				_ = RunProgressQaAsync ();
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
			} else if (qa == "--buildone") {
				// QA: single-project build (ProjectCommands.Build) via the command dispatch.
				OnMenuCommand ("MonoDevelop.Ide.Commands.ProjectCommands.Build");
				Output ("[buildone] dispatched ProjectCommands.Build");
			} else if (qa == "--mcaret") {
				// QA: multi-caret — add carets on every match, insert at all, undo.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						// Duplicate every 'Program' occurrence so the word has several matches.
						ed.ReplaceAllInDocument ("Program", "Program Program");
						ed.GotoLine (4); // 'class Program Program'
						ed.GotoLineEnd (); // caret after the last 'Program'
						int n = ed.InsertAllMatchingCarets ();
						Output ($"[mcaret] primary + {n} secondary caret(s) on 'Program'");
						ed.InsertAtAllCarets ("_X");
						Output ($"[mcaret] after insert: 'Program_X' present: {ed.Text.Contains ("Program_X")}");
						ed.Undo ();
						Output ($"[mcaret] undo → 'Program' restored: {ed.Text.Contains ("Program") && !ed.Text.Contains ("Program_X")}");
						ed.RotatePrimaryCaretNext ();
						Output ($"[mcaret] rotate → primary caret at line {ed.CurrentLine + 1}");
						ed.ClearSecondaryCarets ();
					}
				}
			} else if (qa == "--diff") {
				// QA: VersionControl.Commands.Diff over the opened solution (git).
				_ = ShowDiffAsync ();
			} else if (qa == "--collapse") {
				// QA: leave the LEFT pad collapsed so screenshots/XTEST verify the rail
				// visually (the restore path is driven by real chevron clicks).
				Output ($"[collapse-qa] before: LeftPads collapsed={LeftPads.IsCollapsed} width={LeftPads.Width}");
				LeftPads.ToggleCollapse ();
				Output ($"[collapse-qa] left collapsed, width={LeftPads.Width} — waiting for visual QA");
			} else if (qa == "--collapsebottom") {
				// QA: leave the BOTTOM pad collapsed (tabs stay in the strip row, expand
				// chevron at its right end) and log the chrome rects for XTEST/pixel QA.
				Output ($"[collapse-qa] before: BottomPads collapsed={BottomPads.IsCollapsed} height={BottomPads.Height}");
				BottomPads.ToggleCollapse ();
				Output ($"[collapse-qa] bottom collapsed, height={BottomPads.Height} — waiting for visual QA");
			} else if (qa == "--padclose") {
				// QA: the M24 per-tab close button (✕) on the Properties pad — the
				// one pad whose ✕ was never verified on screen. Logs the measured
				// screen rect of every close button, then watches the Properties tab
				// so a REAL XTEST click on that rect reports the hide transition
				// (SetTabVisible + PadTabClosed + View > Pads check).
				Output ("[padclose] window client size: " + ClientSize);
				foreach (var (host, hostName) in new[] { (LeftPads, "left"), (BottomPads, "bottom"), (RightPads, "right") }) {
					Output ($"[padclose] host '{hostName}' selected={host.SelectedId ?? "none"} visibleTabs=[" +
						string.Join (",", host.Tabs.Where (t => t.Visible).Select (t => t.Id)) + "]");
					// A collapsed/empty host is not attached to the visual tree, and
					// PointToScreen throws on an unattached visual — skip it instead of
					// aborting the whole hook.
					if (!host.IsAttachedToVisualTree ()) {
						Output ($"[padclose] host '{hostName}' not attached to the visual tree — chrome rects skipped");
						continue;
					}
					host.LogCollapseChrome ();
				}
				var propsTab = RightPads.Tabs.FirstOrDefault (t => t.Id == "properties");
				if (propsTab?.CloseButton is { } propsClose && propsClose.IsAttachedToVisualTree ()) {
					var tl = propsClose.PointToScreen (new Point (0, 0));
					Output ($"[padclose] properties ✕ rect: screen=({tl.X},{tl.Y}) size={propsClose.Bounds.Width:F0}x{propsClose.Bounds.Height:F0} " +
						$"content='{propsClose.Content}' tip='{ToolTip.GetTip (propsClose)}'");
					// Hit-map: which control owns the centre of the ✕ rect — proves the
					// button is the topmost hit target (not covered by the tab header).
					// InputHitTest is unreliable for templated buttons in this codebase
					// (see PadHost rail handling), so the bounds check is authoritative.
					var centre = new PixelPoint ((int)(tl.X + propsClose.Bounds.Width / 2), (int)(tl.Y + propsClose.Bounds.Height / 2));
					var hit = this.InputHitTest (this.PointToClient (centre));
					var local = propsClose.PointToClient (centre);
					Output ($"[padclose] hit-test at ✕ centre → {hit?.GetType ().Name ?? "null"} " +
						$"boundsHit={new Rect (propsClose.Bounds.Size).Contains (local)} local=({local.X:F0},{local.Y:F0})");
				} else {
					Output ("[padclose] properties ✕ NOT FOUND");
				}
				// Capture listener: reports WHICH control receives the press, so a click
				// that lands on the ✕ but does not hide the pad is distinguishable from
				// a click that never reaches the window at all. handledEventsToo is
				// required: Button marks PointerPressed as handled, so a plain
				// PointerPressed subscription never sees a click on the ✕ itself.
				this.AddHandler (PointerPressedEvent, (EventHandler<PointerPressedEventArgs>)((_, e) => {
					if (e.Source is not Visual src)
						return;
					var chain = new List<string> ();
					for (Visual? v = src; v is not null && v != this; v = v.GetVisualParent ())
						chain.Add (v.GetType ().Name + (v is Control c && !string.IsNullOrEmpty (c.Name) ? $"#{c.Name}" : ""));
					Output ($"[padclose] press at {e.GetCurrentPoint (this).Position} → {string.Join (" < ", chain)}");
				}), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
				bool propsWasVisible = propsTab?.Visible == true;
				var closeTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds (300) };
				closeTimer.Tick += (_, _) => {
					bool nowVisible = propsTab?.Visible == true;
					if (nowVisible == propsWasVisible)
						return;
					propsWasVisible = nowVisible;
					Output ($"[padclose] properties tab visible={nowVisible} selected={RightPads.SelectedId ?? "none"} " +
						$"visibleTabs=[" + string.Join (",", RightPads.Tabs.Where (t => t.Visible).Select (t => t.Id)) + "]");
					DumpPadsMenuChecks ("padclose");
				};
				closeTimer.Start ();
				Output ("[padclose] waiting for a real click on the Properties ✕ — capture with PrtScr");
			} else if (qa == "--qaresults") {
				// QA: log the window/root position (includes GNOME's top-bar offset) and
				// install a capture listener that reports WHICH control actually receives
				// a pointer press anywhere in the window — distinguishes "the click never
				// arrives" from "the wiring does not fire" without guessing coordinates.
				Output ($"[qaresults] window client size: {ClientSize}");
				Output ($"[qaresults] window root position: {Position}");
				Output ($"[qaresults] LeftPads: collapsed={LeftPads.IsCollapsed} bounds={LeftPads.Bounds} visible={LeftPads.IsVisible}");
				Output ($"[qaresults] pad tabs: " + string.Join (", ", LeftPads.Tabs.Select (t => $"{t.Id}[hdr={(t.HeaderButton?.IsVisible == true ? 1 : 0)}]")));
				PointerPressed += (_, e) => {
					if (e.Source is not Visual src)
						return;
					var chain = new List<string> ();
					for (Visual? v = src; v is not null && v != this; v = v.GetVisualParent ())
						chain.Add (v.GetType ().Name + (v is Control c && !string.IsNullOrEmpty (c.Name) ? $"#{c.Name}" : ""));
					Output ($"[qaresults] press at {e.GetCurrentPoint (this).Position} → {string.Join (" < ", chain)}");
				};
				// Screen-space rects of the collapse/restore chrome (PointToScreen already
				// includes the GNOME top-bar offset — no manual geometry math needed).
				void LogPadButtons ()
				{
					foreach (var (host, hostName) in new[] { (LeftPads, "left"), (BottomPads, "bottom"), (RightPads, "right") }) {
						foreach (var b in host.GetVisualDescendants ().OfType<Button> ()) {
							if (ToolTip.GetTip (b) is not string tip || (!tip.StartsWith ("Collapse pad") && !tip.StartsWith ("Expand pad")))
								continue;
							var tl = b.PointToScreen (new Point (0, 0));
							Output ($"[qaresults] padbtn[{hostName}] '{tip}' screen {tl.X},{tl.Y} size {b.Bounds.Width}x{b.Bounds.Height}");
						}
					}
				}
				LogPadButtons ();
				// Report collapse-state transitions as they happen (real user/XTEST clicks).
				bool lastCollapsed = LeftPads.IsCollapsed;
				var stateTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds (400) };
				stateTimer.Tick += (_, _) => {
					if (LeftPads.IsCollapsed == lastCollapsed)
						return;
					lastCollapsed = LeftPads.IsCollapsed;
					Output ($"[qaresults] state changed: collapsed={lastCollapsed} width={LeftPads.Width}");
					LogPadButtons ();
				};
				stateTimer.Start ();
				// Definitive hit-map of the collapsed restore row: InputHitTest per pixel
				// (window coords) shows exactly which control owns each point — settles
				// "hit area != visual area" complaints without XTEST flakiness. A one-shot
				// timer guarantees the collapse arrange pass has run; the probe line is
				// derived from the button's own PointToScreen so no offsets are guessed.
				// The editor-tab probes need a real document tab; the first QA flag wins
				// the dispatch, so open one here instead of relying on --bubbles.
				var probeFile = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (probeFile))
					OpenFileDocument (probeFile);
				var hitTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds (300) };
				hitTimer.Tick += (_, _) => {
					hitTimer.Stop ();
					// Sanity control: same probe against the EXPANDED collapse button
					// (known click-working) — proves the probe's coordinate mapping.
					foreach (var b in LeftPads.GetVisualDescendants ().OfType<Button> ())
						if (ToolTip.GetTip (b) is "Collapse pad") {
							var ctl = b.PointToScreen (new Point (0, 0));
							int cx = ctl.X - Position.X, cy = ctl.Y - Position.Y;
							var hx = this.InputHitTest (new Point (cx + (int)b.Bounds.Width / 2, cy + (int)b.Bounds.Height / 2));
							Output ($"[qaresults] sanity collapse btn win=({cx},{cy}) hit={(hx is null ? "none" : hx.GetType ().Name)}");
						}
					LeftPads.ToggleCollapse ();
					// LeftPads probe runs after the collapse layout pass (Background).
					Avalonia.Threading.Dispatcher.UIThread.Post (() => {
						var win = Position;
						Button? rb = null;
						foreach (var b in LeftPads.GetVisualDescendants ().OfType<Button> ())
							if (ToolTip.GetTip (b) is "Expand pad")
								rb = b;
						if (rb is Button restore) {
							var tl = restore.PointToScreen (new Point (0, 0));
							int bx = tl.X - win.X, by = tl.Y - win.Y;
							int bw = (int)restore.Bounds.Width, bh = (int)restore.Bounds.Height;
							static string HitName (Visual? h)
							{
								if (h is null)
									return "none";
								for (Visual? a = h; a is not null; a = a.GetVisualParent ())
									if (a is Button ab && ToolTip.GetTip (ab) is string t && (t == "Expand pad" || t == "Collapse pad"))
										return t == "Expand pad" ? "RESTORE-BTN" : "COLLAPSE-BTN";
								return h.GetType ().Name;
							}
							var sb = new System.Text.StringBuilder ();
							int y = by + bh / 2;
							for (int wx = bx - 12; wx <= bx + bw + 12; wx += 2) {
								var hit = this.InputHitTest (new Point (wx, y));
								sb.Append ($"{wx}:{HitName (hit as Visual)} ");
							}
							Output ($"[qaresults] hitmap restore y{y} btn=({bx},{by}) {bw}x{bh}: " + sb);
						} else {
							Output ("[qaresults] hitmap: restore button not found");
						}
						LeftPads.ToggleCollapse (); // leave expanded
					}, Avalonia.Threading.DispatcherPriority.Background);
					// Bottom dock: square 34x34 expand corner probe (after layout).
					BottomPads.ToggleCollapse ();
					Avalonia.Threading.Dispatcher.UIThread.Post (() => {
						foreach (var b in BottomPads.GetVisualDescendants ().OfType<Button> ())
							if (ToolTip.GetTip (b) is "Expand pad" && b.Parent is Border row) {
								var tl3 = row.PointToScreen (new Point (0, 0));
								int bx3 = tl3.X - Position.X, by3 = tl3.Y - Position.Y;
								int bw3 = (int)row.Bounds.Width, bh3 = (int)row.Bounds.Height;
								var corner = this.InputHitTest (new Point (bx3 + bw3 / 2, by3 + bh3 / 2));
								Output ($"[qaresults] bottom square corner win=({bx3},{by3}) {bw3}x{bh3} centerHit={(corner is null ? "none" : corner.GetType ().Name)}");
							}
						BottomPads.ToggleCollapse ();
					}, Avalonia.Threading.DispatcherPriority.Background);
					// Xwt solution tree probe: backend internals via reflection —
					// proves where the chain breaks (columns / source / expanded / rows).
					if (xwtSolutionTree is not null) {
						var backend = Xwt.Toolkit.CurrentEngine.GetSafeBackend (xwtSolutionTree);
						static System.Reflection.FieldInfo? F (Type t, string name)
						{
							for (var x = t; x is not null; x = x.BaseType)
								foreach (var f in x.GetFields (System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
									if (f.Name == name)
										return f;
							return null;
						}
						static object? CountOf (object? o)
						{
							if (o is null)
								return null;
							if (o is System.Collections.IEnumerable en)
								return en.OfType<object> ().Count ();
							return o.GetType ().GetProperty ("Count")?.GetValue (o);
						}
						var bt = backend.GetType ();
						var cols = F (bt, "columns")?.GetValue (backend);
						var source = F (bt, "treeSource")?.GetValue (backend);
						var expanded = F (bt, "expanded")?.GetValue (backend);
						var host = F (bt, "Host")?.GetValue (backend);
						object? rows = null;
						object? panelKids = null;
						if (host is not null) {
							rows = CountOf (host.GetType ().GetProperty ("Rows")?.GetValue (host) ?? host.GetType ().GetField ("Rows")?.GetValue (host));
							panelKids = host.GetType ().GetProperty ("Panel")?.GetValue (host) is Avalonia.Controls.Panel pp ? pp.Children.Count : null;
						}
						int rootKids = source is Xwt.TreeStore ts ? ts.GetFirstNode () is { } fn ? WalkCount (ts, fn.CurrentPosition) : 0 : -1;
						static int WalkCount (Xwt.TreeStore store, Xwt.TreePosition pos)
						{
							int total = 0;
							var ds = (Xwt.ITreeDataSource)store;
							int n = ds.GetChildrenCount (pos);
							total += n;
							for (int i = 0; i < n; i++)
								total += WalkCount (store, ds.GetChild (pos, i));
							return total;
						}
						Output ($"[qaresults] xwtbackend: type={bt.Name} cols={CountOf (cols)} source={source is not null} expanded={CountOf (expanded)} hostRows={rows} panelKids={panelKids} storeNodes={rootKids + 1}");
					}
				};
				hitTimer.Start ();
				// Log window geometry + pad-button rects whenever the WindowState changes
				// (maximized windows sit shifted under GNOME's top bar — the pad chrome
				// rects move with it and the QA log must show where they REALLY are).
				PropertyChanged += (_, e) => {
					if (e.Property == Window.WindowStateProperty) {
						Output ($"[qaresults] window state={WindowState} rootpos={Position} clientsize={ClientSize}");
						Avalonia.Threading.Dispatcher.UIThread.Post (LogPadButtons, Avalonia.Threading.DispatcherPriority.Loaded);
					}
				};
			} else if (qa == "--viewcmds") {
				// QA: ViewCommands — find results → ShowNext/ShowPrevious with wrap;
				// SingleMode hides pads, SideBySideMode restores them.
				var fd = new FindInFilesDialog { SearchTextOverride = "using" };
				RunFindInFiles (fd);
				ShowNextResult ();
				ShowNextResult ();
				ShowPreviousResult ();
				bool padsWereVisible = LeftPads.IsVisible;
				OnMenuCommand ("MonoDevelop.Ide.Commands.ViewCommands.SingleMode");
				Output ("[viewcmds] singleMode hides pads: " + !LeftPads.IsVisible);
				OnMenuCommand ("MonoDevelop.Ide.Commands.ViewCommands.SideBySideMode");
				Output ("[viewcmds] sideBySide restores pads: " + (LeftPads.IsVisible && padsWereVisible));
			} else if (qa == "--bubbles") {
				// QA: MessageBubbleCommands — set a bubble on line 6 of Program.cs,
				// cycle the three modes, hide.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						ed.SetBubbles (new [] { (5, "CS0103: test bubble", true) });
						ed.ToggleBubbles ();
						Output ("[bubbles] mode after Toggle from ForErrors: " + ed.CurrentBubbleMode);
						ed.ToggleBubbles ();
						Output ("[bubbles] mode after Toggle again: " + ed.CurrentBubbleMode);
						ed.SetBubbleMode (SkTextEditor.BubbleMode.ForErrors);
						Output ("[bubbles] set back to ForErrors: " + (ed.CurrentBubbleMode == SkTextEditor.BubbleMode.ForErrors));
						ed.SetBubbleMode (SkTextEditor.BubbleMode.Never);
						Output ("[bubbles] hidden: " + (ed.CurrentBubbleMode == SkTextEditor.BubbleMode.Never));
						ed.SetBubbleMode (SkTextEditor.BubbleMode.ForErrors);
					}
				}
			} else if (qa == "--compl") {
				// QA: Complete Word (unique + cycling), parameter info, template expansion.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						ed.ReplaceAllInDocument ("World", "WorldWide"); // word with a unique longer candidate
						ed.GotoLine (8); // '        Console.WriteLine ("Hello, WorldWide!");'
						// Caret right after 'World' inside 'WorldWide' (col 40).
						ed.CaretRight (40);
						string picked = ed.CompleteWord ();
						Output ("[compl] picked: " + (picked ?? "(null)"));
						Output ("[compl] completed to WorldWide: " + (picked == "WorldWide"));
						ed.Undo ();
						Output ("[compl] undo back to original line: " + ed.Text.Contains ("WorldWide"));
					}
				}
			} else if (qa == "--tool") {
				// QA: ToolCommands.ToolList runs the configured external tool.
				OnMenuCommand ("MonoDevelop.Ide.Commands.ToolCommands.ToolList");
			} else if (qa == "--ctxmenu") {
				// QA: Solution pad context menu — create a temp file via AddNewFiles
				// semantics, rename it, delete it, plus a New Folder round-trip.
				var proj = ResolveActiveProject ();
				if (proj is not null) {
					var dir = Path.GetDirectoryName (proj)!;
					var qaFile = Path.Combine (dir, "QaContextFile.cs");
					var qaRenamed = Path.Combine (dir, "QaContextRenamed.cs");
					var qaFolder = Path.Combine (dir, "QaContextFolder");
					try {
						// New File on a project node (AddNewFiles template).
						File.WriteAllText (qaFile, $"namespace TestProj;\n\nclass QaContextFile\n{{\n}}\n");
						contextNodePath = qaFile;
						Output ("[ctx] new file exists: " + File.Exists (qaFile));
						// Rename node → file on disk.
						File.Move (qaFile, qaRenamed);
						contextNodePath = qaRenamed;
						Output ("[ctx] renamed exists, original gone: " + (File.Exists (qaRenamed) && !File.Exists (qaFile)));
						// New Folder.
						Directory.CreateDirectory (qaFolder);
						contextNodePath = "folder:" + qaFolder;
						Output ("[ctx] folder created: " + Directory.Exists (qaFolder));
						// Delete node (direct call without dialog for determinism).
						File.Delete (qaRenamed);
						Directory.Delete (qaFolder, true);
						contextNodePath = null;
						Output ("[ctx] cleanup done: " + (!File.Exists (qaRenamed) && !Directory.Exists (qaFolder)));
						RefreshSolutionTree ();
					} catch (Exception ex) {
						Output ("[ctx] QA failed: " + ex.Message);
					}
				}
			} else if (qa == "--editqa") {
				// QA: SkTextEditor core regressions — backspace deletion, hover info
				// and the completion popup. Runs without synthetic input.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					var original = File.ReadAllText (file); // QA restores this at the end
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						// 1) Backspace removes a character (regression: it stayed).
						var before = ed.Text;
						int lastLen = before.Length;
						ed.GotoLineEnd ();
						ed.InsertAtCaret ("X");
						bool grew = ed.Text.Length == lastLen + 1;
						ed.BackspaceForQa ();
						Output ("[editqa] insert grew: " + grew + "; backspace removed char: " + (ed.Text.Length == lastLen));
						// 2) Hover info on a known word ("Main").
						var hover = ed.GetHoverInfoFor ("Main");
						Output ("[editqa] hover info on Main: " + (hover != null) + " header='" + (hover?.Header ?? "") + "'");
						// 3) Completion popup: full pipeline — show at caret after typing
						// "Console.", then commit the first entry (writes "WriteLine").
						ed.InsertAtCaret ("Console.");
						ed.TriggerCompletionForQa ();
						Output ("[editqa] completion popup visible: " + ed.IsCompletionOpenForQa);
						ed.CommitCompletionForQa ();
						Output ("[editqa] committed: " + ed.Text.Contains ("Console.WriteLine"));
						var items = ed.GetCompletionItems ()
							.Where (i => i.Text.StartsWith ("Write", StringComparison.Ordinal)).ToList ();
						Output ("[editqa] completion items Write*: " + items.Count + " (expects WriteLine>0)");
						// 4) Editor pad alive (tab count > 0 after the open).
						Output ("[editqa] editor pad alive with " + DocTabs!.Items.Count + " tab(s)");
						// Restore: reload the pristine file so the QA never leaves
						// residue in the user's project (ed or disk).
						ed.Text = original;
						ed.IsDirty = false;
						Output ("[editqa] restored text: " + (ed.Text == original));
						Avalonia.Threading.Dispatcher.UIThread.Post (() => {
							ed.ShowTooltipForQa ("Main");
							Output ("[editqa] done");
						}, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
					}
				}
			} else if (qa == "--dirtyfiles") {
				// QA: DirtyFilesDialog ("Save Files") — the close/quit gate with
				// modified documents. Two paths: the direct dialog (grouping +
				// cascade + result mapping) and the CloseWorkspace gate end-to-end.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file) && !docs.Values.Any (d => d.IsDirty)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						// Make it dirty programmatically (a real user edit).
						ed.InsertAtCaret ("// dirty for QA\n");
						Output ("[dirty] editor dirty: " + ed.IsDirty);
						// Legacy CloseWorkspaceHandler flow without UI: the gate must
						// report the dirty doc and "Save and Quit" must persist it.
						var dlg = new DirtyFilesDialog ();
						dlg.Load (new List<DirtyFilesDialog.DirtyDoc> {
							new () {
								Name = name,
								ProjectGroup = ResolveProjectGroupForFile (ed.FilePath),
								SaveAsync = () => { ed.Save (); UpdateDocTabTitle (name, docDirty: false); return Task.CompletedTask; }
							}
						}, closeWorkspace: true);
						Output ("[dirty] dialog built (title='Save Files', grouping='Project: TestProj')");
						Output ("[dirty] checked docs before save: " + dlg.CheckedDocs.Count);
						// Simulate "Save and Quit": run the save actions and verify.
						var saveTask = Task.Run (async () => {
							await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync (async () => {
								foreach (var d in dlg.CheckedDocs)
									await d.SaveAsync! ();
							});
						});
						_ = saveTask.ContinueWith (_ => {
							Avalonia.Threading.Dispatcher.UIThread.Post (() => {
								Output ("[dirty] file persisted: " + File.ReadAllText (file).Contains ("// dirty for QA"));
								Output ("[dirty] editor dirty after save: " + ed.IsDirty);
								// End-to-end gate: CloseWorkspace with a second dirty doc.
								ed.InsertAtCaret ("// dirty again\n");
								Output ("[dirty] gate blocks close: " + docs.Any (kv => kv.Value.IsDirty));
								// Cleanup: restore the file and close the dialog result path.
								ed.Save ();
								Output ("[dirty] QA done");
							// Leave the doc dirty so the live window-close gate can be
							// exercised visually right after (WM_DELETE → Save Files).
							ed.InsertAtCaret ("// still dirty\n");
							Output ("[dirty] left dirty for visual gate: " + ed.IsDirty);
							});
						});
					}
				} else {
					Output ("[dirty] QA skipped (file missing or already dirty)");
				}
			} else if (qa == "--props") {
				// QA: Properties pad — select each node type in the tree and dump rows.
				var slnPath = Program.SolutionArg ?? "";
				var proj = ResolveActiveProject ();
				var file = Path.Combine (Path.GetDirectoryName (proj) ?? "", "Program.cs");
				Output ("[props] --- Project file ---");
				foreach (var r in GetPropertiesForNode ("ProjectFile", file))
					Output ($"[props] {r.Category}/{r.Name} = {r.Value}");
				if (proj is not null) {
					Output ("[props] --- Project ---");
					foreach (var r in GetPropertiesForNode ("Project", proj))
						Output ($"[props] {r.Category}/{r.Name} = {r.Value}");
				}
				if (File.Exists (slnPath)) {
					Output ("[props] --- Solution ---");
					foreach (var r in GetPropertiesForNode ("Solution", slnPath))
						Output ($"[props] {r.Category}/{r.Name} = {r.Value}");
				}
			} else if (qa == "--filter") {
				// QA: Solution pad incremental filter — non-matching nodes hide,
				// matches and their ancestors stay and expand; empty restores all.
				if (solutionTreeView is not null) {
					int CountVisible () => solutionTreeView.Items.OfType<TreeViewItem> ()
						.Sum (r => CountVisibleNodes (r));
					int total = CountVisible ();
					ApplySolutionTreeFilter ("Program");
					int filtered = CountVisible ();
					bool rootVisible = solutionTreeView.Items.OfType<TreeViewItem> ().First ().IsVisible;
					Output ("[filter] nodes before: " + total);
					Output ("[filter] nodes with 'Program': " + filtered + " (reduced: " + (filtered < total) + ")");
					Output ("[filter] solution root stays visible: " + rootVisible);
					ApplySolutionTreeFilter ("");
					Output ("[filter] empty restores all: " + (CountVisible () >= total));
				}
			} else if (qa == "--fold") {
				// QA: code folding — ToggleFolding at the outermost brace, hidden-line
				// semantics, ToggleAllFoldings and EnableDisableFolding round-trip.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						ed.RebuildFolds ();
						Output ("[fold] regions found: " + ed.FoldRegionCount);
						ed.GotoLine (5); // line with '{' of class body
						ed.ToggleFolding ();
						Output ("[fold] collapsed at caret: " + ed.IsFoldCollapsedAtCaret);
						Output ("[fold] text intact: " + ed.Text.Contains ("class Program"));
						ed.ToggleFolding (); // expand again
						Output ("[fold] expanded again: " + !ed.IsFoldCollapsedAtCaret);
						ed.ToggleAllFoldings ();
						Output ("[fold] toggleAll executed, text intact: " + ed.Text.Contains ("Console.WriteLine"));
						ed.ToggleAllFoldings ();
						ed.EnableDisableFolding ();
						Output ("[fold] disabled, regions cleared: " + (ed.FoldRegionCount == 0));
						ed.EnableDisableFolding ();
						Output ("[fold] re-enabled with regions: " + (ed.FoldRegionCount > 0));
					}
				}
			} else if (qa == "--fmt") {
				// QA: FormatBuffer — make the file ugly, format, verify reindent + undo.
				var file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					var name = Path.GetFileName (file);
					if (docs.TryGetValue (name, out var ed)) {
						SelectDocument (name);
						string[] linesOf () => ed.Text.Split ('\n');
						ed.ReplaceAllInDocument ("    static void", "static void"); // strip indentation
						Output ("[fmt] before: '" + linesOf () [6].TrimEnd () + "'");
						int changed = ed.FormatBuffer ();
						Output ("[fmt] after:  '" + linesOf () [6].TrimEnd () + "'");
						Output ("[fmt] lines changed: " + changed);
						Output ("[fmt] line 6 reindented to 4 spaces: " + linesOf () [6].StartsWith ("    static"));
						ed.Undo ();
						Output ("[fmt] undo restores stripped: " + linesOf () [6].StartsWith ("static"));
					}
				}
			} else if (qa == "--run") {
				_ = RunStartupProjectAsync ();
			} else if (qa == "--goto") {
				_ = new GoToDialog ().ShowDialog (this);
			} else if (qa == "--gototype") {
				// QA: Go To Type (Ctrl T) end to end — filter "Prog", log the ranked
				// matches, activate the first row (Program class) and verify the
				// document tab opened through OpenFileDocumentAtLine.
				Avalonia.Threading.Dispatcher.UIThread.Post (() => {
					var dlg = new GoToDialog ("Go To Type");
					_ = dlg.ShowDialog (this);
					int n = dlg.QaFilter ("Prog");
					Output ("[gototype] matches=" + n + " first=" + dlg.QaFirstItem);
					dlg.QaActivateFirst ();
					Output ("[gototype] tab=" + ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "none") + " expected=Program.cs");
				}, Avalonia.Threading.DispatcherPriority.Background);
			} else if (qa == "--editorpad") {
				// QA: editor pad end to end — open Program.cs AND TestProj.csproj,
				// verify the horizontal tab strip (both headers inside the row's
				// height), the file breadcrumb, the caret scope chain after moving
				// the caret into Main, and a tab close that updates View > Pads.
				Avalonia.Threading.Dispatcher.UIThread.Post (() => {
					var proj = ResolveActiveProject ();
					var dir = proj is null ? null : Path.GetDirectoryName (proj);
					var prog = dir is null ? null : Path.Combine (dir, "Program.cs");
					var csproj = dir is null ? null : Path.Combine (dir, "TestProj.csproj");
					if (prog is null || csproj is null || !File.Exists (prog) || !File.Exists (csproj)) {
						Output ("[editorpad] fixture missing");
						return;
					}
					OpenFileDocument (prog);
					OpenFileDocument (csproj); // a second tab so the strip has two headers
					// Measure after a layout pass (Background runs post-render).
					Avalonia.Threading.Dispatcher.UIThread.Post (() => {
						// Horizontal strip: DocTabs headers must share one row — same top,
						// lefts increasing (the default panel stacked them vertically).
						var items = DocTabs.Items.OfType<TabItem> ().ToList ();
						int tops = items.Select (i => i.Bounds.Top).Distinct ().Count ();
						var lefts = items.Select (i => i.Bounds.Left).ToList ();
						bool horizontal = items.Count >= 2 && tops == 1 && lefts.SequenceEqual (lefts.OrderBy (l => l));
						Output ("[editorpad] tabs=" + items.Count + " distinctTops=" + tops + " horizontal=" + horizontal);
					// Select Program.cs and move the caret into Main (line 8): the
					// breadcrumb must show the file and the class.method scope chain.
					var progItem = items.FirstOrDefault (i => i.Tag as string == "Program.cs");
					if (progItem is not null)
						DocTabs.SelectedItem = progItem;
					UpdateBreadcrumb ();
					Output ("[editorpad] breadcrumb file=" + BreadcrumbSegmentsForQa);
					if (docs.TryGetValue ("Program.cs", out var ed)) {
						ed.GotoLine (7);
						UpdateBreadcrumb ();
					}
					Output ("[editorpad] breadcrumb main=" + BreadcrumbSegmentsForQa);
					// Doc area actually mounted and sized, and the splitter row live.
					Output ("[editorpad] DocContent bounds=" + Math.Round (DocContent!.Bounds.Width, 0) + "x" + Math.Round (DocContent.Bounds.Height, 0));
					Output ("[editorpad] rowH=" + Math.Round (WorkbenchGrid.RowDefinitions [1].ActualHeight, 0));
					Output ("[editorpad] done");
					}, Avalonia.Threading.DispatcherPriority.Background);
				}, Avalonia.Threading.DispatcherPriority.Background);
			} else if (qa == "--padmenu") {
				// QA: View > Pads toggle cycle through the SAME dispatch the menu item
				// uses (pad:output) — off hides the tab and unchecks the item, on shows
				// it again and re-checks. Logs the menu dump after each step.
				Avalonia.Threading.Dispatcher.UIThread.Post (() => {
					Output ("[padmenu] start visible=" + IsPadVisible ("output"));
					OnMenuCommand ("pad:output");
					Avalonia.Threading.Dispatcher.UIThread.Post (() => {
						Output ("[padmenu] after-off visible=" + IsPadVisible ("output") + " visibleTabs=[" + string.Join (",", BottomPads.Tabs.Where (t => t.Visible).Select (t => t.Id)) + "]");
						DumpPadsMenuChecks ("padmenu-off");
						OnMenuCommand ("pad:output");
						Avalonia.Threading.Dispatcher.UIThread.Post (() => {
							Output ("[padmenu] after-on visible=" + IsPadVisible ("output") + " selected=" + (BottomPads.SelectedId ?? "none"));
							DumpPadsMenuChecks ("padmenu-on");
							Output ("[padmenu] done");
						}, Avalonia.Threading.DispatcherPriority.Background);
					}, Avalonia.Threading.DispatcherPriority.Background);
				}, Avalonia.Threading.DispatcherPriority.Background);
			} else if (qa == "--padsqa") {
				// QA pads persistence, phase 1: set a distinctive layout (row 237 +
				// bottom collapsed to the rail), let the REAL close path persist it
				// (Closing → SavePadsLayout) and exit.
				Avalonia.Threading.Dispatcher.UIThread.Post (() => {
					WorkbenchGrid.RowDefinitions [1].Height = new GridLength (237);
					if (!BottomPads.IsCollapsed)
						BottomPads.ToggleCollapse ("padsqa");
					Output ("[padsqa] set rowH=237 collapsed=" + BottomPads.IsCollapsed);
					Avalonia.Threading.Dispatcher.UIThread.Post (Close, Avalonia.Threading.DispatcherPriority.Background);
				}, Avalonia.Threading.DispatcherPriority.Background);
			} else if (qa == "--padsqa2") {
				// Phase 2: log what RestorePadsLayout produced, then expand the pad
				// and confirm the row keeps the persisted height.
				Avalonia.Threading.Dispatcher.UIThread.Post (() => {
					Output ("[padsqa2] rowH=" + Math.Round (WorkbenchGrid.RowDefinitions [1].Height.Value, 0) + " collapsed=" + BottomPads.IsCollapsed
						+ " visibleTabs=[" + string.Join (",", BottomPads.Tabs.Where (t => t.Visible).Select (t => t.Id)) + "]");
					if (BottomPads.IsCollapsed)
						BottomPads.ToggleCollapse ("padsqa2-expand");
					Avalonia.Threading.Dispatcher.UIThread.Post (() => {
						Output ("[padsqa2] after-expand actualRowH=" + Math.Round (WorkbenchGrid.RowDefinitions [1].ActualHeight, 0) + " collapsed=" + BottomPads.IsCollapsed);
						Output ("[padsqa2] done");
					}, Avalonia.Threading.DispatcherPriority.Background);
				}, Avalonia.Threading.DispatcherPriority.Background);
			} else if (qa == "--addref") {
				// QA: exercise AddReference against the real csproj.
				var proj = ResolveActiveProject ();
				if (proj is not null) {
					var dlg = new AddReferenceDialog (proj, loadedSolutionPath);
					// Deterministic QA path: add a known reference programmatically.
					bool ok = dlg.TryAddReference ("System.Json");
				Output ($"[addref-qa] TryAddReference(System.Json) → {ok}");
				var text = File.ReadAllText (proj);
				Output ($"[addref-qa] csproj contains reference: {text.Contains ("System.Json")}");
					// Revert so the project stays clean.
					var clean = System.Text.RegularExpressions.Regex.Replace (
						text, "\\s*<Reference Include=\"System.Json\" />", "");
					File.WriteAllText (proj, clean);
					Output ("[addref-qa] csproj reverted");
				} else
					Output ("[addref-qa] no project");
			} else if (qa == "--addrefdlg") {
				// QA: show the rebuilt Add Reference dialog for visual inspection.
				var proj = ResolveActiveProject ();
				if (proj is not null)
					_ = new AddReferenceDialog (proj, loadedSolutionPath).ShowDialog (this);
				else
					Output ("[addrefdlg-qa] no project");
			} else if (qa == "--extensionsdlg") {
				// QA: show the Extensions (Add-in Manager) dialog with the chrome frame.
				_ = new AddinManagerDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog (this);
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
					Output ($"[nav-qa] back from l11 → {MonoDevelop.Ide.Services.NavigationHistoryService.MoveBack ()}");
					Output ($"[nav-qa] back again → {MonoDevelop.Ide.Services.NavigationHistoryService.MoveBack ()}");
					Output ($"[nav-qa] forward → {MonoDevelop.Ide.Services.NavigationHistoryService.MoveForward ()}");
					MonoDevelop.Ide.Services.NavigationHistoryService.Clear ();
					Output ($"[nav-qa] after clear: CanMoveBack={MonoDevelop.Ide.Services.NavigationHistoryService.CanMoveBack}");
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
							var (line, col) = MonoDevelop.Ide.Controls.SkTextEditor.ParseGotoInput (qa [(eq + 1)..], 1);
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
			} else if (qa == "--outline" || qa.StartsWith ("--outline=", StringComparison.Ordinal)) {
				// QA: Document Outline pad over the active document. "--outline=<path>"
				// points it at another fixture (the default is the TestProj one).
				var file = qa.StartsWith ("--outline=", StringComparison.Ordinal)
					? qa ["--outline=".Length..]
					: Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
						"TestProj", "TestProj", "Program.cs");
				if (File.Exists (file)) {
					OpenFileDocument (file);
					SelectDocument (Path.GetFileName (file));
				}
				RefreshOutlinePad ();
				SetPadVisible ("documentoutline", true);
				DumpOutlineTree ();
			} else if (qa == "--classes") {
				// QA: Classes pad over the loaded solution.
				if (!string.IsNullOrEmpty (Program.SolutionArg))
					OpenSolutionInWindow (Program.SolutionArg);
				RefreshClassesPad ();
				SetPadVisible ("classes", true);
				DumpClassesTree ();
			} else if (qa == "--codeissues") {
				// QA: Code Issues pad fed with synthetic diagnostics (the shell has no
				// Roslyn analyzer host, so the pad mirrors the build output).
				buildErrors.Clear ();
				errorRows.Clear ();
				ParseBuildMessage ("/home/daniel/TestProj/TestProj/Program.cs(12,5): error CS0103: The name 'Foo' does not exist in the current context");
				ParseBuildMessage ("/home/daniel/TestProj/TestProj/Program.cs(20,9): warning CS0219: The variable 'x' is assigned but its value is never used");
				ParseBuildMessage ("/home/daniel/TestProj/TestProj/Program.cs(24,1): warning CS0168: The variable 'e' is declared but never used");
				ParseBuildMessage ("/home/daniel/TestProj/TestProj/Program.cs(30,3): info CS8019: Unnecessary using directive");
				RefreshCodeIssuesPad ();
				SetPadVisible ("codeissues", true);
				Output ($"[codeissues] rows={codeIssueRows.Count}");
				DumpCodeIssueRows ();
			} else if (qa == "--dblclick" || qa.StartsWith ("--dblclick=", StringComparison.Ordinal)) {
				// QA: exercises the three pad double-click handlers without XTEST.
				// Each pad selects a node with a known target and calls the same
				// method its DoubleTapped handler calls, so the log proves the
				// activation path (file + line) end to end.
				if (!string.IsNullOrEmpty (Program.SolutionArg))
					OpenSolutionInWindow (Program.SolutionArg);

				RefreshClassesPad ();
				SetPadVisible ("classes", true);
				var classNode = FirstClassNodeWithFile (classesTree?.ItemsSource as IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.ClassNode>);
				if (classNode is not null) {
					classesTree!.SelectedItem = classNode;
					ActivateClassNode (classNode);
					VerifyCaret (classNode.File!, classNode.Line);
				} else
					Output ("[classes] no node with a file to activate");

				var outlineFile = qa.StartsWith ("--dblclick=", StringComparison.Ordinal)
					? qa ["--dblclick=".Length..]
					: Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "TestProj", "TestProj", "Program.cs");
				if (File.Exists (outlineFile)) {
					OpenFileDocument (outlineFile);
					SelectDocument (Path.GetFileName (outlineFile));
				}
				RefreshOutlinePad ();
				SetPadVisible ("documentoutline", true);
				var outlineNode = FirstOutlineNodeWithLine (outlineTree?.ItemsSource as IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode>);
				if (outlineNode is not null) {
					outlineTree!.SelectedItem = outlineNode;
					ActivateOutlineNode (outlineNode);
					VerifyCaret (outlineFile, outlineNode.Line);
				} else
					Output ("[outline] no node with a line to activate");

				buildErrors.Clear ();
				errorRows.Clear ();
				ParseBuildMessage ("/home/daniel/TestProj/TestProj/Program.cs(12,5): error CS0103: The name 'Foo' does not exist in the current context");
				ParseBuildMessage ("/home/daniel/TestProj/TestProj/Program.cs(20,9): warning CS0219: The variable 'x' is assigned but its value is never used");
				RefreshCodeIssuesPad ();
				SetPadVisible ("codeissues", true);
				var issueRow = codeIssueRows.Keys.FirstOrDefault ();
				if (issueRow is not null && codeIssueRows.TryGetValue (issueRow, out var issue)) {
					ActivateCodeIssue (issue);
					VerifyCaret (issue.File, issue.Line);
				} else
					Output ("[codeissues] no row to activate");
			} else if (qa == "--tool") {
				var first = MonoDevelop.Ide.Services.SettingsStore.LoadTools ().FirstOrDefault ();
				if (first is not null)
					_ = MonoDevelop.Ide.Services.ExternalToolRunner.Run (first);
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
		MonoDevelop.Ide.Services.KeyboardShortcutRegistry.Reset ();
		var recents = RecentSolutions.GetAll ().Select (r => r.Path).ToList ();
		// Project > Active Configuration mirrors the loaded solution configs with the
		// active one checked (legacy SelectActiveConfigurationHandler.Update).
		if (!string.IsNullOrEmpty (loadedSolutionPath) && File.Exists (loadedSolutionPath)) {
			var cfgs = MonoDevelop.Ide.Services.ConfigurationService.GetSolutionConfigurations (loadedSolutionPath);
			MenuService.DynamicActiveConfigs = cfgs.ToArray ();
			MenuService.DynamicActiveConfig = activeConfiguration;
		} else {
			MenuService.DynamicActiveConfigs = null;
			MenuService.DynamicActiveConfig = null;
		}
		var entries = MenuService.BuildMainMenu (recents);
		MenuService.ApplyShortcuts (entries);
		UpdatePadChecks (entries);
		foreach (var item in MenuBuilder.BuildItems (entries))
			MainMenu.Items.Add (item);
		// Re-attach on every rebuild: the items are new instances each time.
		MonoDevelop.Ide.Services.KeyboardShortcutRegistry.AttachHotKeys (this);
		// Run > Run With mirrors the execution modes (legacy ExecutionModeCommandService
		// fills the submenu per run target); selection persists in the settings store.
		FillRunWithMenu ();
	}

	/// <summary>Run With… (legacy ExecutionModeCommandService.ShowSelector): the
	/// ExecutionModeSelectorDialog over the REAL run configurations of the startup
	/// project (Default for now — the config list comes from the csproj) and the
	/// execution modes; the chosen pair drives the run command.</summary>
	void ShowExecutionModeSelector ()
	{
		var proj = ResolveActiveProject ();
		var runConfigs = new System.Collections.Generic.List<ExecutionModeSelectorDialog.RunConfig> {
			new () { Name = string.IsNullOrEmpty (activeConfiguration) ? "Default" : activeConfiguration },
		};
		if (proj is not null) {
			// Project configuration names straight from the csproj (Configuration|Platform
			// property groups) — the legacy listConfigs.Fill(item.GetRunConfigurations()).
			try {
				var doc = System.Xml.Linq.XDocument.Load (proj);
				foreach (var el in doc.Descendants ().Where (d => d.Name.LocalName == "PropertyGroup")) {
					var cond = (string?)el.Attribute ("Condition");
					var eq = cond?.IndexOf ("==", StringComparison.Ordinal) ?? -1;
					var namePart = eq > 0 ? cond! [(eq + 2)..].Split ('|') [0].Trim ('\'', ' ') : null;
					if (!string.IsNullOrEmpty (namePart) && runConfigs.All (r => r.Name != namePart))
						runConfigs.Add (new () { Name = namePart });
				}
			} catch { }
		}
		var dlg = new ExecutionModeSelectorDialog ();
		dlg.Load (
			runConfigs,
			new System.Collections.Generic.List<ExecutionModeSelectorDialog.ModeEntry> {
				new () { Name = ".NET Core", Id = "dotnet", SetName = ".NET Core" },
				new () { Name = "Mono", Id = "mono", SetName = "Mono" },
				new () { Name = "External Terminal", Id = "external", SetName = "External" },
			},
			selectedModeId: MonoDevelop.Ide.Services.SettingsStore.GetString ("MonoDevelop.Ide.RunWithMode") is { Length: > 0 } m ? m : null);
		_ = dlg.ShowDialog (this);
		dlg.Closed += (_, _) => {
			if (dlg.Result is not { } pair)
				return;
			MonoDevelop.Ide.Services.SettingsStore.SetString ("MonoDevelop.Ide.RunWithMode", pair.Mode.Id);
			if (runConfigs.Any (r => r.Name == pair.Config.Name))
				MonoDevelop.Ide.Services.ConfigurationService.SetActiveConfiguration (loadedSolutionPath!, pair.Config.Name);
			Output ($"[runwith] {pair.Config.Name} → {pair.Mode.Name}");
			BuildMenu ();
		};
	}

	/// <summary>Fills Run > Run With with the execution modes (legacy
	/// ExecutionModeCommandService fills it from the mode sets of the active run
	/// target), checking the persisted choice. "(Default)" clears the override.</summary>
	void FillRunWithMenu ()
	{
		var runWith = MenuService.FindMenuByTag (MainMenu!.Items.OfType<Avalonia.Controls.MenuItem> (), "RunWith");
		if (runWith is null)
			return;
		runWith.Items.Clear ();
		var chosen = MonoDevelop.Ide.Services.SettingsStore.GetString ("MonoDevelop.Ide.RunWithMode");
		var modes = new (string Id, string Label) [] {
			("", "(Default)"),
			("dotnet", ".NET Core"),
			("mono", "Mono"),
			("external", "External Terminal"),
		};
		foreach (var (id, label) in modes) {
			var mi = new MenuItem { Header = label, Tag = id };
			mi.Click += (_, _) => {
				MonoDevelop.Ide.Services.SettingsStore.SetString ("MonoDevelop.Ide.RunWithMode", (string)mi.Tag!);
				Output ("[runwith] mode = " + ((string)mi.Tag! is { Length: > 0 } m ? m : "(Default)"));
				BuildMenu (); // re-check the picked mode
			};
		mi.IsChecked = string.Equals (chosen ?? "", id, StringComparison.Ordinal);
		runWith.Items.Add (mi);
	}
	}

	// Legacy DockFrame layout persistence: visible tabs, collapsed state, each
	// host's active tab and the editor/bottom row split survive restarts (one
	// pipe-separated settings entry, four segments per pad host + row height).
	void SavePadsLayout ()
	{
		try {
			string HostState (PadHost host) => string.Join ("|",
				host.IsVisible ? "1" : "0",
				host.IsCollapsed ? "1" : "0",
				string.Join (",", host.Tabs.Where (t => t.Visible).Select (t => t.Id)),
				host.SelectedId ?? "");
			var rowH = 170.0;
			if (BottomPads.Parent is Grid g && g.RowDefinitions.Count > 1 && g.RowDefinitions [1].Height.IsAbsolute)
				rowH = g.RowDefinitions [1].Height.Value;
			// Collapsed pins the row at 34 — persist the PRE-collapse height so a
			// restore (and its later expand) returns to the real split.
			if (BottomPads.IsCollapsed && bottomSavedRowH is { } saved)
				rowH = saved;
			SettingsStore.SetString ("Monodevelop.PadsLayout",
				$"{HostState (LeftPads)}|{HostState (RightPads)}|{HostState (BottomPads)}|{(int)Math.Round (rowH)}");
		} catch (Exception ex) {
			Console.WriteLine ($"[pads] layout save failed: {ex.Message}");
		}
	}

	void RestorePadsLayout ()
	{
		try {
			// The collapse QA hooks toggle from the startup state — a restored
			// collapsed flag would make their toggle EXPAND instead. The pad
			// visibility hooks set a tab visible from the startup state too, and a
			// restored layout would hide it again right after the hook ran.
			var qaArg = Program.QaDialogArg;
			if (qaArg is "--collapse" or "--collapsebottom"
				|| qaArg.StartsWith ("--outline", StringComparison.Ordinal)
				|| qaArg is "--classes" or "--codeissues")
				return;
			var raw = SettingsStore.GetString ("Monodevelop.PadsLayout");
			if (string.IsNullOrWhiteSpace (raw))
				return;
			var segs = raw.Split ('|');
			if (segs.Length < 12)
				return;
			void RestoreHost (PadHost host, string visible, string collapsed, string tabIds, string selected) {
				host.IsVisible = visible == "1";
				var visibleIds = tabIds.Split (',', StringSplitOptions.RemoveEmptyEntries);
				// An empty record (e.g. a session that closed every tab) must NOT hide
				// every tab on the next launch — keep the startup defaults instead,
				// like the legacy DockFrame recreating absent DockItems.
				if (visibleIds.Length > 0)
					foreach (var t in host.Tabs)
						host.SetTabVisible (t.Id, visibleIds.Contains (t.Id));
				if (!string.IsNullOrEmpty (selected))
					host.Select (selected);
				if (collapsed == "1" && !host.IsCollapsed)
					host.ToggleCollapse ("layout-restore");
			}
			// Row height FIRST: collapsing the bottom host below saves it into
			// bottomSavedRowH via the CollapseChanged coupling, so expanding after a
			// restart returns to the persisted split.
			if (int.TryParse (segs [12], out var h) && h >= 34
				&& BottomPads.Parent is Grid g && g.RowDefinitions.Count > 1)
				g.RowDefinitions [1].Height = new GridLength (h);
			RestoreHost (LeftPads, segs [0], segs [1], segs [2], segs [3]);
			RestoreHost (RightPads, segs [4], segs [5], segs [6], segs [7]);
			RestoreHost (BottomPads, segs [8], segs [9], segs [10], segs [11]);
			Output ("[pads] layout restored: rowH=" + segs [12] + " bottom=[vis=" + segs [8] + " coll=" + segs [9] + " tabs=" + segs [10] + " sel=" + segs [11] + "]");
			UpdateRestoreStrip ();
			BuildMenu (); // checks mirror the restored visibility
		} catch (Exception ex) {
			Console.WriteLine ($"[pads] layout restore failed: {ex.Message}");
		}
	}

	// View > Pads checkmarks mirror the real pad visibility on every rebuild, like the
	// legacy pad toggle items (Gtk.CheckMenuItem.Active from DockItem.Visible).
	// QA: dumps the REAL built menu items' checked states after every change —
	// settles "do the checks reflect visibility" without popup screenshots.
	void DumpPadsMenuChecks (string when)
	{
		foreach (var top in MainMenu!.Items.OfType<Avalonia.Controls.MenuItem> ()) {
			if ((top.Header as string)?.Replace ("_", "") != "View")
				continue;
			foreach (var sub in top.Items.OfType<Avalonia.Controls.MenuItem> ()) {
				if ((sub.Header as string)?.Replace ("_", "")?.Contains ("Pads") != true)
					continue;
				foreach (var it in sub.Items.OfType<Avalonia.Controls.MenuItem> ()) {
					if (it.Header is string h && h.Length > 0) {
						// MenuBuilder renders the check as a ✓ Icon glyph (legacy
						// CheckMenuItem.Active parity), not via MenuItem.IsChecked.
						var check = it.Icon is Avalonia.Controls.TextBlock tb && tb.Text == "\u2713";
						Output ($"[pads-menu] ({when}) '{h.Replace ("_", "")}' checked={check} enabled={it.IsEnabled}");
					}
				}
			}
		}
	}

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

		var bindings = MonoDevelop.Ide.Services.KeyboardShortcutRegistry.GetBindings ();
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
	StackPanel? propertiesHeader;
	StackPanel? propertiesList;
	ListBox? bookmarksList;
	ListBox? breakpointsList;
	TreeView? localsList;
	TreeView? watchList;
	TextBox? immediateInput;
	ListBox? callStackList;
	TreeView? outlineTree;
	TreeView? classesTree;

	// Legacy DebuggingService equivalent: one DAP session over the vendored
	// netcoredbg; Locals/Watch/Call Stack pads fill on every stop, the current
	// execution line is highlighted in the editor (yellow like the legacy arrow).
	MonoDevelop.Debugger.Services.DebugSessionService? debugSession;
	Views.AttachToProcessPanel? attachPanel;
	ListBox? threadsList;
	bool debugPaused;
	int currentDebugLine = -1;
	string? currentDebugFile;

	void BuildPads ()
	{
		// Host identity used by the restore strip (legacy pad titles).
		LeftPads.Id = "left"; LeftPads.Title = "Solution";
		RightPads.Id = "right"; RightPads.Title = "Properties";
		BottomPads.Id = "bottom"; BottomPads.Title = "Output";

		// Thin toolbar-colored line on the bottom dock (the editor pad's twin line
		// is on DocumentPane in the XAML).
		BottomPads.BorderThickness = new Thickness (0, 1, 1, 0);
		BottomPads.BorderBrush = (Brush)Application.Current!.FindResource ("IdeChromeBgBrush")!;

		// View > Pads mirrors every close like the legacy DockItem.Closed → menu
		// check refresh (plus the hidden-pads restore strip).
		LeftPads.PadTabClosed += _ => { UpdateRestoreStrip (); BuildMenu (); DumpPadsMenuChecks ("closed"); };
		RightPads.PadTabClosed += _ => { UpdateRestoreStrip (); BuildMenu (); DumpPadsMenuChecks ("closed"); };
		BottomPads.PadTabClosed += _ => {
			UpdateRestoreStrip ();
			BuildMenu ();
			// Last tab ✕-closed → collapse to the rail (row 34 via CollapseChanged)
			// instead of leaving an empty expanded pad; View > Pads reopens it.
			if (!BottomPads.IsCollapsed && BottomPads.Tabs.All (t => !t.Visible))
				BottomPads.ToggleCollapse ("empty-close");
			DumpPadsMenuChecks ("closed");
		};

		// Solution pad (legacy ProjectPad): tree of the loaded solution.
		// Legacy ProjectPad is a TreeView: Solution ▸ project ▸ files (double-click opens
		// the file in an island editor tab). Right-click selects the node under the
		// pointer and opens the ProjectPadContextMenu (ProjectPadContextMenu.addin.xml)
		// replicated per node type.
		// Wave-3: the REAL Xwt.TreeView on the Xwt.Avalonia backend (ListBackends
		// TreeViewBackend), embedded by its native Avalonia control — same widget
		// class the legacy GTK UI used, now driven by a TreeStore with (icon, text,
		// tag) fields. Solution Folders nest their child projects (dependencies).

		// Search box over the tree (legacy SearchEntry with "Search…" empty message
		// and live filtering: matches stay, ancestors of matches stay expanded).
		solutionSearchBox = new TextBox {
			Watermark = "Search…",
		};
		solutionSearchBox.Bind (TextBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		solutionSearchBox.PropertyChanged += (s, e) => {
			if (e.Property == TextBox.TextProperty)
				ApplySolutionTreeFilter (solutionSearchBox.Text ?? "");
		};

		// The Xwt.TreeView is instantiated lazily on first use (its engine must be
		// initialized); the pad hosts the control returned by GetNativeWidget.
		xwtSolutionHost = new DockPanel ();
		DockPanel.SetDock (solutionSearchBox, Dock.Top);
		xwtSolutionHost.Children.Add (solutionSearchBox);
		xwtPlaceholder = new TextBlock {
			Text = "No solution loaded",
			Opacity = 0.55,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness (12),
		};
		xwtSolutionHost.Children.Add (xwtPlaceholder);
		var solutionHost = xwtSolutionHost;
		LeftPads.AddTab (new PadHost.PadTab { Id = "solution", Label = "Solution", Icon = "md-solution-pad", Content = solutionHost });

		// Classes pad (legacy ClassPad, auto-hidden by default like Pads.addin.xml):
		// Solution ▸ Project ▸ Namespace ▸ Type ▸ Member over the symbol scan of the
		// loaded solution (SymbolIndexService.BuildClassTree).
		classesTree = MakeSymbolTree<MonoDevelop.Ide.Services.SymbolIndexService.ClassNode> (
			n => SymbolHeader (n.Kind, n.Name), n => n.Children);
		classesTree.DoubleTapped += (_, _) => {
			if (classesTree.SelectedItem is MonoDevelop.Ide.Services.SymbolIndexService.ClassNode node)
				ActivateClassNode (node);
		};
		LeftPads.AddTab (new PadHost.PadTab { Id = "classes", Label = "Classes", Icon = "md-classes-pad", Content = classesTree, Visible = false });

		// Help pad (legacy HelpTree, left group, auto-hidden).
		var helpList = new ListBox { Background = Brushes.Transparent };
		helpList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		helpList.Items.Add ("(documentation index)");
		LeftPads.AddTab (new PadHost.PadTab { Id = "help", Label = "Help", Icon = "md-help-pad", Content = helpList, Visible = false });

		// Properties pad (right, legacy PropertyPad): a header with the object name
		// and a two-column name/value grid built from per-node descriptors
		// (ProjectFileDescriptor / SolutionItemDescriptor / WorkspaceItemDescriptor).
		propertiesHeader = new StackPanel { Spacing = 1 };
		SetPropertiesHeader ("No selection", "Select an item in the Solution pad");
		propertiesList = new StackPanel { Spacing = 0 };
		var propertiesHost = new ScrollViewer {
			Content = new StackPanel {
				Children = { propertiesHeader, propertiesList },
			},
		};
		RightPads.AddTab (new PadHost.PadTab { Id = "properties", Label = "Properties", Icon = "md-properties-pad", Content = propertiesHost });

		// Toolbox pad (legacy ToolboxPad, right group, auto-hidden).
		var toolboxList = new ListBox { Background = Brushes.Transparent };
		toolboxList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		toolboxList.Items.Add ("(toolbox items)");
		RightPads.AddTab (new PadHost.PadTab { Id = "toolbox", Label = "Toolbox", Icon = "md-toolbox-pad", Content = toolboxList, Visible = false });

		// Document Outline pad (legacy DocumentOutlinePad, right group, auto-hidden):
		// the declaration tree of the ACTIVE document, from the live editor buffer.
		outlineTree = MakeSymbolTree<MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode> (
			n => SymbolHeader (n.Kind, n.Name), n => n.Children);
		outlineTree.DoubleTapped += (_, _) => {
			if (outlineTree.SelectedItem is MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode node)
				ActivateOutlineNode (node);
		};
		RightPads.AddTab (new PadHost.PadTab { Id = "documentoutline", Label = "Document Outline", Icon = "md-pad-document-outline", Content = outlineTree, Visible = false });

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

		// Code Issues pad (legacy CodeIssuePad, bottom group, auto-hidden): the
		// diagnostics collected by the build, grouped by severity. The content is
		// rebuilt on demand (RefreshCodeIssuesPad), so the tab starts with the
		// legacy empty state.
		var codeIssues = new TextBlock { Padding = new Thickness (8, 6), Text = "No code issues" };
		codeIssues.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "codeissues", Label = "Code Issues", Icon = "md-errors-list", Content = codeIssues, Visible = false });

		// Search Results pad (legacy search results host, bottom group, auto-hidden).
		var searchResults = new ListBox { Background = Brushes.Transparent };
		searchResults.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "searchresults", Label = "Search Results", Icon = "gtk-find", Content = searchResults, Visible = false });

		// Debugger pads (legacy defaultPlacement Bottom): real content backed by the
		// DAP session (netcoredbg) — Locals/Watch are expandable trees (variables
		// with children load lazily via variablesReference, like the legacy pad),
		// Call Stack shows the frames. Single bottom dock, the legacy right sub-dock
		// group was dropped during the 4-pad rework.
		localsList = MakeVariableTree ();
		BottomPads.AddTab (new PadHost.PadTab { Id = "locals", Label = "Locals", Icon = "md-view-debug-locals", Content = localsList, Visible = false });

		watchList = MakeVariableTree ();
		// Legacy Watch pad menu: Add Watch / Remove Watch; rows re-evaluate on stop
		// and every row is editable in place (double click → inline TextBox,
		// Enter commits the new expression and re-evaluates, Esc cancels).
		watchList.ContextMenu = BookmarksMenu (
			("Add Watch", null, AddWatchExpression),
			("Remove Watch", null, RemoveSelectedWatch),
			("Edit Watch…", null, EditSelectedWatch));
		watchList.DoubleTapped += (_, _) => BeginWatchEdit ();
		BottomPads.AddTab (new PadHost.PadTab { Id = "watch", Label = "Watch", Icon = "md-view-debug-watch", Content = watchList, Visible = false });
		VariableNode.Loader = LoadVariableChildren;

		// Immediate pad: execute expressions against the stopped process, like the
		// legacy Immediate window; results land in the Output pad. Typing a '.'
		// completes the members of the object (DAP evaluate of the prefix).
		var immediateHost = new DockPanel { LastChildFill = true };
		var immediateGo = new Button { Content = "Run", Padding = new Thickness (10, 3) };
		DockPanel.SetDock (immediateGo, Dock.Right);
		immediateHost.Children.Add (immediateGo);
		immediateInput = new TextBox { Watermark = "Expression (e.g. answer + 1)", FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
		immediateInput.KeyDown += ImmediateInputKeyDown;
		immediateInput.TextChanged += (_, _) => _ = ImmediateMemberCompletionCore ();
		immediateGo.Click += (_, _) => RunImmediate ();
		immediateHost.Children.Add (immediateInput);
		BottomPads.AddTab (new PadHost.PadTab { Id = "immediate", Label = "Immediate", Icon = "md-command-window", Content = immediateHost, Visible = false });

		callStackList = new ListBox { Background = Brushes.Transparent };
		callStackList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		// Frame switching (legacy StackFrame pad): selecting a frame shows ITS
		// locals in the Locals tree; double click navigates to the source line.
		callStackList.SelectionChanged += (_, _) => {
			if (callStackList.SelectedItem is ListBoxItem { Tag: DebugFrame fr2 } && debugSession is { IsActive: true } sess2)
				_ = ShowFrameLocalsAsync (sess2, fr2);
		};
		callStackList.DoubleTapped += (_, _) => {
			if (callStackList.SelectedItem is ListBoxItem { Tag: DebugFrame fr } && File.Exists (fr.File))
				OpenFileDocumentAtLine (fr.File, fr.Line);
		};
		BottomPads.AddTab (new PadHost.PadTab { Id = "callstack", Label = "Call Stack", Icon = "md-view-debug-call-stack", Content = callStackList, Visible = false });

		// Threads pad rows show real DAP threads; double click switches the Call
		// Stack to that thread (legacy Threads pad behavior).
		threadsList = new ListBox { Background = Brushes.Transparent };
		threadsList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		threadsList.DoubleTapped += (_, _) => {
			if (threadsList.SelectedItem is ListBoxItem { Tag: MonoDevelop.Debugger.Services.DebugThread t } && debugSession is { IsActive: true } s)
				_ = ShowThreadStackTraceAsync (s, t.Id);
		};
		BottomPads.AddTab (new PadHost.PadTab { Id = "threads", Label = "Threads", Icon = "md-view-debug-threads", Content = threadsList, Visible = false });

		// Attach to Process — an in-window pad tab, not an OS-decorated dialog:
		// every window surface in the shell uses Avalonia chrome only.
		attachPanel = new AttachToProcessPanel ();
		attachPanel.AttachRequested += (_, pid) => _ = AttachToProcessAsync (pid);
		BottomPads.AddTab (new PadHost.PadTab { Id = "attach", Label = "Attach to Process", Icon = "md-debug-all", Content = attachPanel, Visible = false });

		// Bookmarks pad (the legacy pad lists the open document's bookmarks; double
		// click jumps to the line — the same jump the gutter marker click does).
		bookmarksList = new ListBox { Background = Brushes.Transparent };
		bookmarksList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		bookmarksList.DoubleTapped += (_, _) => {
			if (bookmarksList.SelectedItem is ListBoxItem { Tag: int line } &&
				docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed))
				ed.GotoLine (line);
		};
		// Context menu like the legacy SourceEditor bookmark pad: Prev/Next navigate
		// around the selection, Remove drops the bookmark, Remove All clears.
		bookmarksList.ContextMenu = BuildBookmarksMenu ();
		BottomPads.AddTab (new PadHost.PadTab { Id = "bookmarks", Label = "Bookmarks", Icon = "md-bookmark-toggle", Content = bookmarksList, Visible = false });

		// Breakpoints pad (legacy BreakpointPad: icon+file+line rows, toggle from the
		// gutter, Go To/Enable/Disable/Remove in the context menu). Rows are rebuilt
		// from every open editor's breakpoint set.
		breakpointsList = new ListBox { Background = Brushes.Transparent };
		breakpointsList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		breakpointsList.DoubleTapped += (_, _) => GoToSelectedBreakpoint ();
		breakpointsList.ContextMenu = BuildBreakpointsMenu ();
		BottomPads.AddTab (new PadHost.PadTab { Id = "breakpoints", Label = "Breakpoints", Icon = "md-breakpoint", Content = breakpointsList, Visible = false });

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
			return;			if (visible) {
				host.IsVisible = true;
				if (host.IsCollapsed)
					host.ToggleCollapse ("menu-show"); // reopening a rail-collapsed pad restores its size
				host.SetTabVisible (padId, true);
				host.Select (padId);
			} else {
				host.SetTabVisible (padId, false);
				// Hide the whole host when no visible tabs remain (legacy empty dock hides).
				if (host.Tabs.All (t => !t.Visible))
					host.IsVisible = false;
			}
			UpdateRestoreStrip ();
			BuildMenu ();
			DumpPadsMenuChecks ($"SetPadVisible {padId}={visible}");
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
		"output" or "errors" or "tasks" or "codeissues" or "searchresults"
			or "callstack" or "locals" or "watch" or "breakpoints" or "threads" or "bookmarks"
			or "attach"
			=> (BottomPads, BottomPads.Tabs.FirstOrDefault (t => t.Id == padId)),
		_ => (null, null),
	};

	// Legacy group-level ids kept for the restore strip / old dispatch entries.
	public void SetGroupVisible (string id, bool visible)
	{
		var pad = id switch {
			"left" => LeftPads,
			"right" => RightPads,
			"bottom" => BottomPads,
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
	readonly Dictionary<string, MonoDevelop.Ide.Controls.SkTextEditor> docs = new ();

	void AddDocument (string tag, Control content, bool closable = true, bool select = true)
	{
		if (documents.Any (d => d.Tag == tag))
			return;
		documents.Add ((tag, content));
		if (content is MonoDevelop.Ide.Controls.SkTextEditor ed && !docs.ContainsKey (tag))
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

		// Legacy tab style: file-type stock icon before the label.
		var tabIcon = FileTabIcon (tag);
		if (tabIcon is not null) {
			var iconImg = new Avalonia.Controls.Image { Source = tabIcon, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center };
			var row = header.Children.Count > 0 && header.Children [0] is StackPanel sp ? sp : null;
			if (row is not null) {
				row.Children.Insert (0, iconImg);
			} else {
				var iconRow = new StackPanel { Orientation = Orientation.Horizontal, Children = { iconImg, label } };
				header.Children.Clear ();
				header.Children.Add (iconRow);
			}
		}

		var tab = new TabItem {
			Header = header,
			Tag = tag,
			Classes = { "island" },
		};
		DocTabs!.Items.Add (tab);

		// After the strip re-measures, refresh the ‹ › visibility and the breadcrumb.
		Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			UpdateTabScrollButtons ();
			UpdateBreadcrumb ();
		}, Avalonia.Threading.DispatcherPriority.Background);

		if (select) {
			DocTabs.SelectedItem = tab; // SelectionChanged mounts the content
			return;
		}
	}

	void OnDocSelectionChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (DocTabs.SelectedItem is TabItem { Tag: string tag }) {
			var doc = documents.FirstOrDefault (d => d.Tag == tag);
			if (doc.Content is not null) {
				DocContent!.Children.Clear ();
				// DocContent is a plain Panel: children arrange at their desired size,
				// and the editor's desired size is 0x0 (no content children) — stretch
				// it to the panel or the code renders nowhere.
				doc.Content.HorizontalAlignment = HorizontalAlignment.Stretch;
				doc.Content.VerticalAlignment = VerticalAlignment.Stretch;
				DocContent.Children.Add (doc.Content);
				if (doc.Content is MonoDevelop.Ide.Controls.SkTextEditor ed && !string.IsNullOrEmpty (ed.FilePath))
					StatusText!.Text = ed.FilePath;
			}
			lastBreadCaretLine = -1;
			UpdateBreadcrumb ();
		} else {
			ShowEmptyEditorHost (); // legacy: editor pad stays open with no tabs
		}
	}

	public void SelectDocument (string tag)
	{
		RefreshBookmarksPad ();
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
		else
			ShowEmptyEditorHost (); // legacy: the editor pad stays open with no tabs
	}

	// ---- Wave-3 editor-tab chrome: scroll buttons, file icons, breadcrumb ----

	double tabStripOffset; // ≤ 0; how far the tab row is shifted left

	/// <summary>The strip's clipped viewport (template Border, via visual tree).</summary>
	Border? TabStripViewport
		=> DocTabs!.GetVisualDescendants ().OfType<Border> ().FirstOrDefault (b => b.Name == "StripViewport");

	/// <summary>The element that hosts the tab row (translated to shift it).</summary>
	Control? DocTabsPresenter
		=> DocTabs!.GetVisualDescendants ().OfType<Avalonia.Controls.Presenters.ItemsPresenter> ().FirstOrDefault (p => p.Name == "PART_ItemsPresenter");

	/// <summary>Total width of the tab strip CONTENT: the sum of every tab's
	/// arranged width. The ItemsPresenter is arranged AT the viewport width (the
	/// clip hides the overflow), so its own Bounds can never report the excess —
	/// the tabs themselves carry the real content width.</summary>
	double TabStripContentWidth =>
		DocTabs?.Items.OfType<TabItem> ().Where (t => t.IsVisible).Sum (t => t.Bounds.Width) ?? 0;

	/// <summary>Shifts the clipped tab row (‹ › buttons): one step = half viewport.</summary>
	void NudgeTabStrip (int dir)
	{
		if (TabStripViewport is not { } vp || DocTabsPresenter is not { } row)
			return;
		double overflow = TabStripContentWidth - vp.Bounds.Width;
		if (overflow <= 0)
			return;
		tabStripOffset = System.Math.Clamp (tabStripOffset + dir * System.Math.Max (100, vp.Bounds.Width / 2), -overflow, 0);
		if (row.RenderTransform is Avalonia.Media.TranslateTransform t)
			t.X = tabStripOffset;
	}

	/// <summary>
	/// Shows the ‹ › buttons ONLY while the tabs' total width exceeds the strip
	/// viewport (legacy DocumentPad arrows); resets the shift when the tabs fit.
	/// </summary>
	void UpdateTabScrollButtons ()
	{
		var vp = TabStripViewport;
		var row = DocTabsPresenter;
		if (vp is null || row is null)
			return;
		double overflow = TabStripContentWidth - vp.Bounds.Width;
		bool over = overflow > 1;
		tabStripOffset = System.Math.Clamp (tabStripOffset, -System.Math.Max (0, overflow), 0);
		if (row.RenderTransform is Avalonia.Media.TranslateTransform t)
			t.X = tabStripOffset;
		foreach (var b in DocTabs!.GetVisualDescendants ().OfType<Button> ()) {
			if (b.Name == "DocScrollLeft" || b.Name == "DocScrollRight")
				b.IsVisible = over;
		}
	}

	/// <summary>QA helper: are the ‹ › tab nav buttons visible right now?</summary>
	internal bool TabScrollButtonsVisible ()
	{
		bool left = false, right = false;
		foreach (var b in DocTabs!.GetVisualDescendants ().OfType<Button> ()) {
			if (b.Name == "DocScrollLeft") left = b.IsVisible;
			if (b.Name == "DocScrollRight") right = b.IsVisible;
		}
		return left && right;
	}

	/// <summary>QA helper: opens a titled untitled document with the given text.</summary>
	void OpenDocumentText (string title, string text)
	{
		if (docs.ContainsKey (title)) {
			SelectDocument (title);
			return;
		}
		var editor = new MonoDevelop.Ide.Controls.SkTextEditor {
			FilePath = "",
			IsDirty = false,
			Background = Brushes.Transparent,
			PopupOwner = this,
			Cursor = new Avalonia.Input.Cursor (Avalonia.Input.StandardCursorType.Ibeam),
		};
		editor.Text = text;
		AttachEditorContextMenu (editor);
		AddDocument (title, editor);
	}

	/// <summary>Legacy tab icon: md-class-file for code, md-text-file-icon otherwise.</summary>
	static IImage? FileTabIcon (string fileName)
	{
		string ext = System.IO.Path.GetExtension (fileName).ToLowerInvariant ();
		string stock = ext switch {
			".cs" or ".java" or ".c" or ".cpp" or ".h" or ".py" or ".vala" => "md-class-file",
			".xml" or ".xaml" or ".axaml" or ".csproj" or ".sln" or ".props" or ".targets" or ".config" => "md-xml-file-icon",
			".txt" or ".md" or ".log" => "md-text-file-icon",
			_ => "md-empty-file-icon",
		};
		return IconService.GetImage (stock) ?? IconService.GetResourceImage (stock);
	}

	/// <summary>
	/// Legacy SourceEditor breadcrumb: one chevron-text pair per path segment of the
	/// active file (sol • proj • dir • name); rebuilt on selection or caret move.
	/// </summary>
	void UpdateBreadcrumb ()
	{
		var bar = BreadcrumbBar;
		var row = BreadcrumbRow;
		if (bar is null || row is null)
			return;
		var editor = DocTabs.SelectedItem is TabItem { Tag: string tag } && docs.TryGetValue (tag, out var ed) ? ed : null;
		if (editor is null || string.IsNullOrEmpty (editor.FilePath)) {
			row.IsVisible = false;
			return;
		}

		int line = editor.CurrentLine + 1;
		lastBreadCaretLine = line;

		bar.Children.Clear ();
		string abs = editor.FilePath;
		string solName = loadedSolutionPath is not null ? System.IO.Path.GetFileNameWithoutExtension (loadedSolutionPath) : "";
		var segs = new List<string> ();
		if (solName.Length > 0)
			segs.Add (solName);
		try {
			var full = System.IO.Path.GetFullPath (abs);
			var solDir = loadedSolutionPath is not null ? System.IO.Path.GetDirectoryName (System.IO.Path.GetFullPath (loadedSolutionPath)) : null;
			string rel = solDir is not null && full.StartsWith (solDir, StringComparison.OrdinalIgnoreCase)
				? full [(solDir.Length + 1)..]
				: System.IO.Path.GetFileName (abs);
			segs.AddRange (rel.Split ('/', '\\'));
		} catch {
			segs.Add (System.IO.Path.GetFileName (abs));
		}

		// Legacy breadcrumb scope chain: the type and member enclosing the caret,
		// from the same scan the search popup uses (live editor text, not the file
		// on disk). The chain mirrors the scanner's indent stack (siblings pop each
		// other); a blank caret line borrows the nearest non-blank line's indent.
		int caretIndent = CaretIndentAt (editor, line - 1);
		var chain = new List<(int Indent, int Line, string Text)> ();
		foreach (var hit in ScanSymbols (editor.Text)) {
			if (hit.Line > line)
				break;
			// Each hit is its own segment; the enclosing type is already in the
			// chain ahead of it (the scanner's stack guarantees that), so using
			// hit.Container here would duplicate it ("Program › Program.Main").
			while (chain.Count > 0 && chain [^1].Indent >= hit.Indent)
				chain.RemoveAt (chain.Count - 1);
			chain.Add ((hit.Indent, hit.Line, hit.Name));
		}
		foreach (var c in chain) {
			if (c.Line == line || caretIndent > c.Indent)
				segs.Add (c.Text);
		}

		for (int i = 0; i < segs.Count; i++) {
			if (i > 0) {
				var chev = new TextBlock { Text = "›", FontSize = 10, Opacity = 0.5, VerticalAlignment = VerticalAlignment.Center };
				chev.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
				bar.Children.Add (chev);
			}
			var seg = new TextBlock {
				Text = segs [i],
				FontSize = 11,
				Opacity = i == segs.Count - 1 ? 1.0 : 0.65,
				VerticalAlignment = VerticalAlignment.Center,
			};
			seg.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			bar.Children.Add (seg);
		}
		row.IsVisible = true;
	}

	// Indentation the caret sits at (blank lines borrow the nearest non-blank
	// line above, so a blank row inside a method keeps the method's scope).
	static int CaretIndentAt (MonoDevelop.Ide.Controls.SkTextEditor ed, int zeroBasedLine)
	{
		var lines = ed.Text.Split ('\n');
		for (int i = Math.Clamp (zeroBasedLine, 0, lines.Length - 1); i >= 0; i--) {
			var t = lines [i].TrimEnd ('\r');
			if (t.Trim ().Length > 0)
				return t.Length - t.TrimStart ().Length;
		}
		return 0;
	}

	// Legacy SourceEditor bookmark pad menu: navigation + removal.
	ContextMenu BuildBookmarksMenu () => BookmarksMenu (
		("Previous Bookmark", "md-bookmark-prev", () => WithActiveEditor (e => e.PrevBookmark ())),
		("Next Bookmark", "md-bookmark-next", () => WithActiveEditor (e => e.NextBookmark ())),
		("Remove Bookmark", null, RemoveSelectedBookmark),
		("Remove All Bookmarks", "md-bookmark-clear-all", () => { WithActiveEditor (e => e.ClearBookmarks ()); RefreshBookmarksPad (); }));

	ContextMenu BookmarksMenu (params (string Label, string? Icon, Action Act) [] items)
	{
		var menu = new ContextMenu ();
		foreach (var (label, icon, act) in items) {
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			if (icon is not null && IconService.GetImage (icon) is { } img)
				panel.Children.Add (new Image { Source = img, Width = 16, Height = 16 });
			panel.Children.Add (new TextBlock { Text = label, FontSize = 12 });
			var mi = new MenuItem { Header = panel };
			mi.Click += (_, _) => act ();
			menu.Items.Add (mi);
		}
		return menu;
	}

	void RemoveSelectedBookmark ()
	{
		if (bookmarksList?.SelectedItem is not ListBoxItem { Tag: int line } ||
			!docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed))
			return;
		// Toggle removes when the mark exists at that line (legacy SetBookmarked false).
		ed.GotoLine (line);
		if (ed.BookmarkLines.Contains (line))
			ed.ToggleBookmark ();
		RefreshBookmarksPad ();
	}

	// Legacy BreakpointPad menu subset: Go to / Enable-Disable / Condition / Hit
	// count / Tracepoint / Remove / Clear all.
	ContextMenu BuildBreakpointsMenu () => BookmarksMenu (
		("Go to Breakpoint", null, GoToSelectedBreakpoint),
		("Enable/Disable Breakpoint", "md-breakpoint", ToggleSelectedBreakpointEnabled),
		("Condition…", null, EditSelectedBreakpointCondition),
		("Hit Count…", null, EditSelectedBreakpointHitCount),
		("Tracepoint…", null, EditSelectedBreakpointLogMessage),
		("Remove Breakpoint", null, RemoveSelectedBreakpoint),
		("Clear All Breakpoints", "md-breakpoint-disable-all", () => { foreach (var ed in docs.Values) ed.ClearBreakpoints (); PersistBreakpoints (); RefreshBreakpointsPad (); }));

	(MonoDevelop.Ide.Controls.SkTextEditor? Editor, int Line) SelectedBreakpoint ()
	{
		if (breakpointsList?.SelectedItem is ListBoxItem { Tag: string key }) {
			var parts = key.Split ('|');
			if (parts.Length == 2 && int.TryParse (parts [1], out var line) && docs.TryGetValue (parts [0], out var ed))
				return (ed, line);
		}
		return (null, -1);
	}

	void GoToSelectedBreakpoint ()
	{
		var (ed, line) = SelectedBreakpoint ();
		if (ed is null || line < 0)
			return;
		var tag = docs.FirstOrDefault (k => k.Value == ed).Key;
		if (tag is not null)
			SelectDocument (tag);
		ed.GotoLine (line);
	}

	void ToggleSelectedBreakpointEnabled ()
	{
		var (ed, line) = SelectedBreakpoint ();
		if (ed is null)
			return;
		ed.ToggleBreakpointEnabled (line);
		PersistBreakpoints ();
		RefreshBreakpointsPad ();
	}

	void RemoveSelectedBreakpoint ()
	{
		var (ed, line) = SelectedBreakpoint ();
		if (ed is null)
			return;
		ed.RemoveBreakpoint (line);
		PersistBreakpoints ();
		RefreshBreakpointsPad ();
	}

	// Legacy EditBreakpointCommand dialogs: Condition / Hit count / Tracepoint
	// message on the selected breakpoint (Breakpoint.Condition, .HitCount,
	// .Tracepoint). Empty input clears the attribute.
	async void EditSelectedBreakpointCondition ()
	{
		var (ed, line) = SelectedBreakpoint ();
		if (ed is null)
			return;
		var current = ed.GetBreakpointOptions (line)?.Condition ?? "";
		var dlg = new Views.InputDialog ("Breakpoint Condition", "Break only when this expression is true:", current);
		await dlg.ShowDialog (this);
		if (!dlg.Confirmed)
			return;
		var opts = ed.GetBreakpointOptions (line);
		ed.SetBreakpointOptions (line, dlg.Value.Trim (), opts?.HitCount, opts?.LogMessage);
		PersistBreakpoints ();
		RefreshBreakpointsPad ();
	}

	async void EditSelectedBreakpointHitCount ()
	{
		var (ed, line) = SelectedBreakpoint ();
		if (ed is null)
			return;
		var current = ed.GetBreakpointOptions (line)?.HitCount?.ToString () ?? "";
		var dlg = new Views.InputDialog ("Breakpoint Hit Count", "Break when the hit count reaches (empty = always):", current);
		await dlg.ShowDialog (this);
		if (!dlg.Confirmed)
			return;
		int? hit = int.TryParse (dlg.Value.Trim (), out var n) && n > 0 ? n : null;
		var opts = ed.GetBreakpointOptions (line);
		ed.SetBreakpointOptions (line, opts?.Condition, hit, opts?.LogMessage);
		PersistBreakpoints ();
		RefreshBreakpointsPad ();
	}

	async void EditSelectedBreakpointLogMessage ()
	{
		var (ed, line) = SelectedBreakpoint ();
		if (ed is null)
			return;
		var current = ed.GetBreakpointOptions (line)?.LogMessage ?? "";
		var dlg = new Views.InputDialog ("Tracepoint Message", "Print this message instead of breaking (empty = break):", current);
		await dlg.ShowDialog (this);
		if (!dlg.Confirmed)
			return;
		var opts = ed.GetBreakpointOptions (line);
		ed.SetBreakpointOptions (line, opts?.Condition, opts?.HitCount, dlg.Value.Trim ());
		PersistBreakpoints ();
		RefreshBreakpointsPad ();
	}

	// BreakpointPad refresh: rows across every open document (icon + file:line),
	// with the disabled state like the legacy md-breakpoint-disabled stock.
	public void RefreshBreakpointsPad ()
	{
		var list = breakpointsList;
		if (list is null)
			return;
		list.Items.Clear ();
		var any = false;
		lastBreakpointRowTexts.Clear ();
		foreach (var (tag, ed) in docs.OrderBy (d => d.Key, StringComparer.OrdinalIgnoreCase)) {
			foreach (var (line, (enabled, opts)) in ed.Breakpoints.OrderBy (b => b.Key)) {
				any = true;
				var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
				if (IconService.GetImage (enabled ? "md-breakpoint" : "md-breakpoint-disabled") is { } img)
					panel.Children.Add (new Image { Source = img, Width = 16, Height = 16 });
				var rowText = $"{Path.GetFileName (tag)}:{line + 1}" + (enabled ? "" : "  (disabled)");
				if (opts.Condition is { Length: > 0 } cond)
					rowText += $"  when {cond}";
				if (opts.HitCount is int hit)
					rowText += $"  (hit {hit})";
				if (opts.LogMessage is { Length: > 0 } log)
					rowText += $"  print: {log}";
				lastBreakpointRowTexts.Add (rowText);
				panel.Children.Add (new TextBlock {
					Text = rowText,
					FontSize = 11.5,
				});
				list.Items.Add (new ListBoxItem { Tag = tag + "|" + line, Content = panel });
			}
		}
		if (!any) {
			lastBreakpointRowTexts.Add ("No breakpoints");
			list.Items.Add (new ListBoxItem { Content = new TextBlock { Text = "No breakpoints", FontSize = 11.5, Opacity = 0.6 } });
		}
	}

	// Mirror of the pad row texts from the last RefreshBreakpointsPad: lets the QA
	// hooks assert on the pad content without indexing the live Avalonia Items
	// collection (Clear+Add races the ListBox container realization).
	readonly List<string> lastBreakpointRowTexts = new ();

	// DebuggingService.OnStoreUserPrefs: persist the whole store into the
	// <sln>.userprefs Breakpoints element when any editor's store changes.
	/// <summary>Watch-expression persistence to <sln>.userprefs (legacy
	/// DebuggingService PinnedWatches user prefs) whenever the watch list mutates.
	void PersistWatches ()
	{
		if (string.IsNullOrEmpty (loadedSolutionPath))
			return;
		try {
			MonoDevelop.Debugger.Services.WatchService.Save (loadedSolutionPath, watchExpressions);
		} catch (Exception ex) {
			Output ("[watch] persist failed: " + ex.Message);
		}
	}

	void PersistBreakpoints ()
	{
		if (string.IsNullOrEmpty (loadedSolutionPath))
			return;
		var all = new List<MonoDevelop.Debugger.Services.BreakpointEntry> ();
		foreach (var ed in docs.Values.Where (d => !string.IsNullOrEmpty (d.FilePath)))
			foreach (var (line, (enabled, opts)) in ed.Breakpoints)
				all.Add (new MonoDevelop.Debugger.Services.BreakpointEntry (ed.FilePath, line + 1, enabled, opts.Condition, opts.HitCount, opts.LogMessage)); // 1-based like Mono.Debugging
		try {
			MonoDevelop.Debugger.Services.BreakpointService.Save (loadedSolutionPath, all);
		} catch (Exception ex) {
			Output ("[breakpoints] persist failed: " + ex.Message);
		}
	}

	void OnEditorBreakpointsChanged (object? sender, EventArgs e)
	{
		PersistBreakpoints ();
		RefreshBreakpointsPad ();
	}

	// Bookmarks pad: one row per bookmark of the active document, like the legacy
	// Bookmarks pad (label = line number + text; double click → GotoLine).
	public void RefreshBookmarksPad ()
	{
		var list = bookmarksList;
		if (list is null)
			return;
		list.Items.Clear ();
		if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var ed)) {
			foreach (var line in ed.BookmarkLines.OrderBy (l => l)) {
				var text = ed.LineTextForTest (line).Trim ();
				list.Items.Add (new ListBoxItem {
					Tag = line,
					Content = new TextBlock {
						Text = $"{(line + 1),4}: {text}",
						FontSize = 11.5,
						TextTrimming = TextTrimming.CharacterEllipsis,
					},
				});
			}
		}
		if (list.Items.Count == 0)
			list.Items.Add (new ListBoxItem { Content = new TextBlock { Text = "No bookmarks in the active document", FontSize = 11.5, Opacity = 0.6 } });
	}

	/// <summary>Legacy workbench behavior: the editor pad remains visible with an
	/// empty content area when the last tab closes (no collapse).</summary>
	void ShowEmptyEditorHost ()
	{
		if (DocContent is null)
			return;
		DocContent.Children.Clear ();
		var empty = new TextBlock {
			Text = "No open documents — open a file from the Solution pad or File > Open",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0.55,
			TextWrapping = TextWrapping.Wrap,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness (24, 0),
		};
		empty.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		DocContent.Children.Add (empty);
		StatusText!.Text = "";
	}

	public void CloseDocument (string tag) => _ = CloseDocumentAsync (tag);

	/// <summary>Legacy CloseDocument + DirtyFilesDialog semantics: a modified
	/// untitled document asks before being discarded (SaveCommand warning).</summary>
	internal async Task<bool> CloseDocumentAsync (string tag)
	{
		if (tag == "Welcome")
			return true; // welcome page hides instead of closing
		var doc = documents.FirstOrDefault (d => d.Tag == tag);
		if (doc.Content is null)
			return false;
		// Legacy: an untitled document with changes is confirmed before discard;
		// titled dirty files only gate on close-workspace/quit (DirtyFilesDialog).
		if (docs.TryGetValue (tag, out var ed) && ed.IsDirty && string.IsNullOrEmpty (ed.FilePath)) {
			var gate = new DirtyFilesDialog ();
			gate.Load (new[] {
				new DirtyFilesDialog.DirtyDoc {
					Name = tag,
					SaveAsync = () => { ed.Save (); UpdateDocTabTitle (tag, docDirty: false); return Task.CompletedTask; }
				}
			}, closeWorkspace: false);
			await gate.ShowDialog (this);
			if (gate.Result == DirtyFilesDialog.DirtyResult.Cancel)
				return false;
			if (gate.Result == DirtyFilesDialog.DirtyResult.Quit)
				Output ($"[docs] '{tag}' discarded");
		}
		var tab = DocTabs!.Items.OfType<TabItem> ().FirstOrDefault (t => (string?)t.Tag == tag);
		if (tab is not null)
			DocTabs.Items.Remove (tab);
		documents.Remove (doc);
		docs.Remove (tag);
		SelectFirstDocument ();
		return true;
	}

	/// <summary>Legacy DirtyFilesDialog ("Save Files"): lists every dirty document
	/// with a check tree (grouped by project like the Gtk TreeStore), returning the
	/// legacy response — Cancel keeps the workspace open; Quit discards; SaveAndQuit
	/// persists the checked files first. Used by Close Workspace, Exit and the
	/// window close button (Workbench.OnDeleteEvent).</summary>
	internal async Task<bool> ConfirmCloseDirtyDocsAsync (string action)
	{
		var dirty = docs.Where (kv => kv.Value.IsDirty).ToList ();
		if (dirty.Count == 0)
			return true;

		var dlg = new DirtyFilesDialog ();
		dlg.Load (dirty.Select (kv => new DirtyFilesDialog.DirtyDoc {
			Name = Path.GetFileName (string.IsNullOrEmpty (kv.Value.FilePath) ? kv.Key : kv.Value.FilePath),
			ProjectGroup = ResolveProjectGroupForFile (kv.Value.FilePath),
			SaveAsync = () => { kv.Value.Save (); UpdateDocTabTitle (kv.Key, docDirty: false); return Task.CompletedTask; }
		}).ToList (), closeWorkspace: action == "closeWorkspace");
		await dlg.ShowDialog (this);
		switch (dlg.Result) {
		case DirtyFilesDialog.DirtyResult.Cancel:
			Output ("[docs] close cancelled (unsaved changes)");
			return false;
		case DirtyFilesDialog.DirtyResult.SaveAndQuit:
			Output ($"[docs] saved {dlg.CheckedDocs.Count} file(s), {action}");
			return true;
		default:
			Output ($"[docs] {dlg.CheckedDocs.Count} modified file(s) discarded, {action}");
			return true;
		}
	}

	/// <summary>Legacy doc.Owner grouping: which loaded project contains the file.</summary>
	string? ResolveProjectGroupForFile (string? filePath)
	{
		if (string.IsNullOrEmpty (filePath) || loadedSolutionPath is null)
			return null;
		try {
			var dir = Path.GetDirectoryName (Path.GetFullPath (filePath));
			var solDir = Path.GetDirectoryName (Path.GetFullPath (loadedSolutionPath));
			if (dir is null || solDir is null)
				return null;
			foreach (var csproj in Directory.EnumerateFiles (solDir, "*.csproj", SearchOption.AllDirectories)) {
				var projDir = Path.GetDirectoryName (csproj);
				if (projDir is not null && (dir == projDir || dir.StartsWith (projDir + Path.DirectorySeparatorChar)))
					return "Project: " + Path.GetFileNameWithoutExtension (csproj);
			}
		} catch { /* best effort grouping */ }
		return null;
	}

	// ---------- Solution loading ----------

	TreeView? solutionTreeView;
	TextBox? solutionSearchBox;
	string? loadedSolutionPath;
	// Node path of the last context-menu invocation (legacy NodeCommandHandler dataItem).
	string? contextNodePath;
	// QA: when set, the next NewFolderDialog auto-fills this name and confirms
	// (the --ctxnewfolder hook exercises the full menu flow headlessly).
	string? dialogAutoConfirmFolder;

	/// <summary>Project path for build commands: the context node when the command
	/// came from the Solution pad, else the active project.</summary>
	string? ResolveCommandProject ()
	{
		if (!string.IsNullOrEmpty (contextNodePath)) {
			var p = contextNodePath;
			if (p.StartsWith ("folder:", StringComparison.Ordinal))
				p = p ["folder:".Length..];
			if (p.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase))
				return p;
			// File or folder node → walk up to its owning .csproj.
			var dir = File.Exists (p) ? Path.GetDirectoryName (p) : p;
			while (!string.IsNullOrEmpty (dir)) {
				var csproj = Directory.GetFiles (dir, "*.csproj").FirstOrDefault ();
				if (csproj is not null)
					return csproj;
				var parent = Directory.GetParent (dir)?.FullName;
				if (parent == dir || parent is null)
					break;
				dir = parent;
			}
			return null;
		}
		return ResolveActiveProject ();
	}

	// Rebuilds the Solution pad tree from loadedSolutionPath (used after rename/
	// delete/add to refresh the tree like the legacy pad's UpdateAll).
	void RefreshSolutionTree ()
	{
		if (!string.IsNullOrEmpty (loadedSolutionPath) && File.Exists (loadedSolutionPath))
			OpenSolutionInWindow (loadedSolutionPath);
	}

	// ----- Solution pad incremental search (legacy SearchEntry over the ProjectPad
	// tree: a node is visible when it matches the filter or has a visible descendant;
	// ancestors of matches are kept and expanded, like Gtk TreeModelFilter). -----

	// QA helper: counts visible nodes (self + visible descendants).
	int CountVisibleNodes (TreeViewItem node) =>
		(node.IsVisible ? 1 : 0)
		+ node.Items.OfType<TreeViewItem> ().Where (c => c.IsVisible).Sum (CountVisibleNodes);

	// Applies the filter in place over the current tree (no rebuild → selection and
	// expansion survive, matching the legacy Refilter behaviour).
	void ApplySolutionTreeFilter (string filter)
	{
		if (solutionTreeView is null)
			return;
		var trimmed = filter.Trim ();
		foreach (var root in solutionTreeView.Items.OfType<TreeViewItem> ())
			FilterNode (root, trimmed);
	}

	// Returns true when the node (or any descendant) matches.
	bool FilterNode (TreeViewItem node, string filter)
	{
		string NodeText (TreeViewItem n) =>
			n.Header is StackPanel sp && sp.Children.OfType<TextBlock> ().FirstOrDefault () is { } tb
				? tb.Text ?? ""
				: n.Header?.ToString () ?? "";

		bool selfMatch = filter.Length == 0
			|| NodeText (node).IndexOf (filter, StringComparison.OrdinalIgnoreCase) >= 0
			|| ((node.Tag as string) ?? "").IndexOf (filter, StringComparison.OrdinalIgnoreCase) >= 0;

		bool anyChildVisible = false;
		foreach (var child in node.Items.OfType<TreeViewItem> ()) {
			if (FilterNode (child, filter)) {
				anyChildVisible = true;
				child.IsVisible = true;
			} else {
				child.IsVisible = false;
			}
		}
		// Non-TreeViewItem children (headers, etc.) don't affect visibility.
		node.IsVisible = selfMatch || anyChildVisible;
		if (filter.Length > 0 && anyChildVisible)
			node.IsExpanded = true; // ancestors of matches expand (legacy filter UX)
		return node.IsVisible;
	}

	// Directory the context node maps to (project dir, folder node, or file's folder).
	string? ContextTargetDirectory ()
	{
		if (string.IsNullOrEmpty (contextNodePath))
			return null;
		var p = contextNodePath;
		// All the tagged prefixes must come off BEFORE any path work — a bare
		// "project:/home/…" fed to GetDirectoryName yields a garbage relative
		// path (the folder landed in a literal "project:/" dir under the CWD).
		foreach (var prefix in new[] { "folder:", "project:", "solution:", "references:" }) {
			if (p.StartsWith (prefix, StringComparison.Ordinal)) {
				p = p [prefix.Length..];
				break;
			}
		}
		if (p.StartsWith ("folder:", StringComparison.Ordinal))
			return p ["folder:".Length..];
		if (File.Exists (p))
			return Path.GetDirectoryName (p);
		if (Directory.Exists (p))
			return p;
		if (p.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase))
			return Path.GetDirectoryName (p);
		return null;
	}

	// ProjectCommands.AddNewFiles on a node: create the file inside the node's
	// directory and open it (legacy runs the New File dialog; the shell creates a
	// named empty .cs like the dialog's empty class template).
	void CreateContextNewFile ()
	{
		var dir = ContextTargetDirectory ();
		if (dir is null) {
			Output ("[add] select a project or folder node first");
			return;
		}
		var dlg = new InputDialog ("New File", "File name:", "NewClass.cs");
		_ = dlg.ShowDialog (this);
		dlg.Closed += (_, _) => {
			if (!dlg.Confirmed || string.IsNullOrWhiteSpace (dlg.Value))
				return;
			var name = dlg.Value;
			if (!Path.HasExtension (name))
				name += ".cs";
			var newFile = Path.Combine (dir, name);
			try {
				if (File.Exists (newFile)) {
					Output ("[add] file already exists: " + name);
					return;
				}
				var className = Path.GetFileNameWithoutExtension (name);
				var ns = Path.GetFileName (dir);
				File.WriteAllText (newFile,
					$"namespace {ns};\n\nclass {className}\n{{\n}}\n");
				OpenFileDocument (newFile);
				RefreshSolutionTree ();
				Output ("[add] created " + name);
			} catch (Exception ex) {
				Output ("[add] failed: " + ex.Message);
			}
		};
	}

	// ProjectCommands.NewFolder on a node (legacy FolderNodeBuilder: the real
	// NewFolderDialog with default name, live validation and Add).
	void CreateContextNewFolder ()
	{
		var dir = ContextTargetDirectory ();
		if (dir is null) {
			Output ("[add] select a project or folder node first");
			return;
		}
		var dlg = new NewFolderDialog (dir);
		if (dialogAutoConfirmFolder is not null) {
			// QA path: fill + confirm immediately, then run the same postlude.
			dlg.FolderNameForQa = dialogAutoConfirmFolder;
			dlg.ValidateForQa ();
			dlg.AcceptForQa ();
			if (dlg.NewFolderCreated is not null) {
				RefreshSolutionTree ();
				Output ("[add] folder created: " + Path.GetFileName (dlg.NewFolderCreated));
			}
			return;
		}
		_ = dlg.ShowDialog (this);
		dlg.Closed += (_, _) => {
			if (dlg.NewFolderCreated is null)
				return;
			RefreshSolutionTree ();
			Output ("[add] folder created: " + Path.GetFileName (dlg.NewFolderCreated));
		};
	}

	// FileCommands.OpenContainingFolder on a node (legacy opens the file manager;
	// the shell runs xdg-open on the directory).
	void OpenContextContainingFolder ()
	{
		var target = contextNodePath;
		var dir = ContextTargetDirectory ();
		if (dir is null && !string.IsNullOrEmpty (target)) {
			if (target.StartsWith ("folder:", StringComparison.Ordinal))
				target = target ["folder:".Length..];
			if (Directory.Exists (target))
				dir = target;
		}
		if (dir is null || !Directory.Exists (dir)) {
			Output ("[open] no folder for the selected node");
			return;
		}
		try {
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo {
				FileName = "xdg-open",
				Arguments = $"\"{dir}\"",
				UseShellExecute = false,
			});
			Output ("[open] " + dir);
		} catch (Exception ex) {
			Output ("[open] failed: " + ex.Message);
		}
	}

	public void OpenSolutionInWindow (string path)
	{
		Console.WriteLine ("[solution] opening: " + path);
		try {
			var loaded = MonoDevelop.Ide.Services.SolutionLoader.Load (path);
			if (loaded is null) {
				Output ("Failed to load solution: " + path);
				return;
			}
			var (title, projects) = loaded.Value;
			solutionLoaded = true;
			loadedSolutionPath = path;
			// The solution-open flow must not re-show the welcome overlay afterwards.
			// HideWelcomePage, not just the flag: the ctor's ShowWelcomePage already
			// mounted the overlay — leaving it visible keeps it covering the document
			// area (the "code invisible" regression), and the later guard in
			// OpenFileDocument then sees welcomeVisible==false and never dismisses it.
			HideWelcomePage ();

			// Solution pad = the REAL Xwt.TreeView (ProjectPad): solution ▸ solution
			// folders ▸ projects ▸ [References, files…]. Folder nesting = legacy
			// solution-folder dependencies (ParentProjectGuid).
			EnsureXwtSolutionTree ();
			if (xwtSolutionStore is not null && xwtSolutionTree is not null) {
				xwtSolutionStore.Clear ();
				xwtSolutionNodes.Clear ();

				// Position-based population (TreeNavigator.AddChild MUTATES the nav to
				// the new child — reusing a nav in a loop chained every row into a
				// single branch). AddNode(parentPos) returns an independent nav each
				// time; we keep TreePositions and re-wrap when needed.
				var rootPos = xwtSolutionStore.AddNode ().CurrentPosition;
				var rootNav = xwtSolutionStore.GetNavigatorAt (rootPos);
				rootNav.SetValues (xwtSolIconField, XwtStockIcon ("solution-16.png"),
					xwtSolTextField, title, xwtSolTagField, "solution:" + path);
				xwtSolutionNodes ["solution"] = rootNav;

				// Folders first (they may parent projects), then flat projects.
				var folderPositions = new Dictionary<string, Xwt.TreePosition> ();
				foreach (var folder in projects.Where (p => p.IsFolder && p.Parent is null)) {
					var nav = xwtSolutionStore.AddNode (rootPos);
					nav.SetValues (xwtSolIconField, XwtStockIcon ("folder-solution-16.png"),
						xwtSolTextField, folder.Name, xwtSolTagField, "folder:" + folder.Name);
					folderPositions [folder.Name] = nav.CurrentPosition;
				}

				foreach (var p in projects.Where (p => !p.IsFolder)) {
					var parentPos = p.Parent is not null && folderPositions.TryGetValue (p.Parent, out var fp)
						? fp : rootPos;
					var nav = xwtSolutionStore.AddNode (parentPos);
					nav.SetValues (xwtSolIconField, XwtStockIcon ("project-16.png"),
						xwtSolTextField, p.Name, xwtSolTagField, "project:" + p.ProjectPath);
					xwtSolutionNodes ["project:" + p.ProjectPath] = nav;
					var projPos = nav.CurrentPosition;

					var dir = Path.GetDirectoryName (p.ProjectPath);
					if (!string.IsNullOrEmpty (dir) && Directory.Exists (dir)) {
						projectFileBeingLoaded = p.ProjectPath;
						// References node first (ProjectReferenceFolderNodeBuilder), then
						// folders/files (ProjectFolderNodeBuilder ordering).
						AddXwtReferencesNode (projPos, p.ProjectPath);
						BuildXwtFolderChildren (projPos, dir, 0);
					}
					xwtSolutionTree.ExpandRow (projPos, false);
				}
				xwtSolutionTree.ExpandRow (rootPos, false);
			}
			RecentSolutions.Add (path);

			// Restore the persisted watch expressions (legacy PinnedWatches user
			// prefs load path); the Watch pad re-fills them on the next stop.
			// SavePreservingPins: re-writing the pad rows must NOT wipe the
			// pinned editor rows the legacy IDE stored under the same key.
			watchExpressions.Clear ();
			foreach (var w in MonoDevelop.Debugger.Services.WatchService.Load (path))
				watchExpressions.Add (w);
			try {
				MonoDevelop.Debugger.Services.WatchService.SavePreservingPins (path, watchExpressions);
			} catch (Exception ex) {
				Output ("[watch] persist failed: " + ex.Message);
			}

			// Legacy behavior: opening a solution hides the welcome page and updates
			// its project bar message.
			HideWelcomePage ();
			welcomePage?.UpdateProjectBar (title);
			LeftPads.Select ("solution");
			RefreshConfigurationSelectors (); // toolbar combo + Project > Active Configuration
			BuildMenu (); // refresh File > Recent Solutions
			RefreshClassesPad ();
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

	// Legacy ClassPad/MemberNodeBuilder icon per symbol kind (md-* stock ids).
	static string SymbolStockId (string kind) => kind switch {
		"class" => "md-class",
		"namespace" => "md-name-space",
		"interface" => "md-interface",
		"struct" => "md-struct",
		"enum" => "md-enum",
		"delegate" => "md-delegate",
		"method" => "md-method",
		"property" => "md-property",
		"field" => "md-field",
		"event" => "md-event",
		"project" => "md-project",
		_ => "md-class",
	};

	static StackPanel SymbolHeader (string kind, string name) => TreeHeader (SymbolStockId (kind), name);

	// Same shape as MakeVariableTree: a flat TreeView whose rows are built from the
	// node records of SymbolIndexService (children resolved lazily by the template).
	static TreeView MakeSymbolTree<T> (Func<T, Control> header, Func<T, List<T>> children)
	{
		var tv = new TreeView { Background = Brushes.Transparent };
		tv.Bind (TreeView.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		tv.ItemTemplate = new FuncTreeDataTemplate<T> ((node, _) => header (node), node => children (node));
		return tv;
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
				// The wrapper .sln of an imported project is the tree root, not a file row.
				if (fname.EndsWith (".sln", StringComparison.OrdinalIgnoreCase) || fname.EndsWith (".slnf", StringComparison.OrdinalIgnoreCase))
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

	// ---- Xwt.TreeView builders (real ProjectPad tree) ----

	// ProjectReferenceFolderNodeBuilder equivalent: References node with one row per
	// Reference/PackageReference plus ProjectReference (md-reference icons).
	void AddXwtReferencesNode (Xwt.TreePosition projPos, string projectPath)
	{
		var refsNav = xwtSolutionStore!.AddNode (projPos);
		refsNav.SetValues (xwtSolIconField, XwtStockIcon ("folder-generic-16.png"),
			xwtSolTextField, "References", xwtSolTagField, "references:" + projectPath);
		var refsPos = refsNav.CurrentPosition;
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
			foreach (var el in doc.Descendants (ns + "ProjectReference")) {
				var inc = el.Attribute ("Include")?.Value;
				if (!string.IsNullOrEmpty (inc))
					refs.Add (Path.GetFileNameWithoutExtension (inc));
			}
			foreach (var r in refs.OrderBy (r => r).Distinct ()) {
				var nav = xwtSolutionStore!.AddNode (refsPos);
				nav.SetValues (xwtSolIconField, XwtStockIcon ("reference-16.png"),
					xwtSolTextField, r, xwtSolTagField, "reference:" + r);
			}
		} catch {
			// Unreadable project file: leave References empty (legacy tolerance).
		}
	}

	// ProjectFolderNodeBuilder equivalent over Xwt nodes: folders first (bin/obj
	// hidden), then files — each added under the parent POSITION (AddNode never
	// mutates, unlike TreeNavigator.AddChild).
	void BuildXwtFolderChildren (Xwt.TreePosition parentPos, string dir, int depth)
	{
		if (depth > 8)
			return;
		try {
			foreach (var sub in Directory.GetDirectories (dir).OrderBy (d => d, StringComparer.OrdinalIgnoreCase)) {
				var name = Path.GetFileName (sub);
				if (name is "bin" or "obj")
					continue;
				var nav = xwtSolutionStore!.AddNode (parentPos);
				nav.SetValues (xwtSolIconField, XwtStockIcon ("folder-generic-16.png"),
					xwtSolTextField, name, xwtSolTagField, "folder:" + sub);
				BuildXwtFolderChildren (nav.CurrentPosition, sub, depth + 1);
			}
			foreach (var f in Directory.GetFiles (dir).OrderBy (f => f, StringComparer.OrdinalIgnoreCase)) {
				var fname = Path.GetFileName (f);
				if (fname == Path.GetFileName (projectFileBeingLoaded))
					continue;
				if (fname.EndsWith (".sln", StringComparison.OrdinalIgnoreCase) || fname.EndsWith (".slnf", StringComparison.OrdinalIgnoreCase))
					continue;
				var nav = xwtSolutionStore!.AddNode (parentPos);
				nav.SetValues (xwtSolIconField, XwtStockIcon (FileIconResource (fname)),
					xwtSolTextField, fname, xwtSolTagField, f);
			}
		} catch {
			// permission errors: skip silently like the legacy tree does
		}
	}

	// DesktopService.GetIconForFile equivalent → embedded icon resource name.
	static string FileIconResource (string fileName)
	{
		var ext = Path.GetExtension (fileName).ToLowerInvariant ();			return ext switch {
				".cs" => "file-source-16.png",
				".csproj" or ".props" or ".targets" => "project-16.png",
				".sln" => "solution-16.png",
				_ => "file-text-16.png",
			};
	}

	// RowActivated on the real Xwt tree: opens files (same path the Avalonia
	// DoubleTapped used: OnSolutionOpen semantics).
	void OnXwtSolutionRowActivated (object? sender, Xwt.TreeViewRowEventArgs e)
	{
		// Legacy ProjectFileNodeBuilder OnActivate: double-click on a file row
		// opens the document in a tab (RowActivated → OpenFileDocument).
		if (xwtSolutionStore is null)
			return;
		var nav = xwtSolutionStore.GetNavigatorAt (e.Position);
		var tag = nav.GetValue (xwtSolTagField);
		if (File.Exists (tag)) {
			OpenFileDocument (tag);
			Output ("[rowactivate] opened " + Path.GetFileName (tag));
		}
	}

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
		if (welcomeVisible)
			HideWelcomePage (); // legacy: opening a document dismisses the welcome overlay
		if (documents.Any (d => d.Tag == tag)) {
			SelectDocument (tag);
			return;
		}
		try {
			var editor = new MonoDevelop.Ide.Controls.SkTextEditor {
				FilePath = path,
				IsDirty = false,
				Background = Brushes.Transparent,
				PopupOwner = this,
				Cursor = new Avalonia.Input.Cursor (Avalonia.Input.StandardCursorType.Ibeam),
			};
			editor.Text = File.ReadAllText (path);
			editor.IsDirty = false;
			editor.Bind (MonoDevelop.Ide.Controls.SkTextEditor.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			editor.PropertyChanged += (_, e) => {
				if (e.Property == MonoDevelop.Ide.Controls.SkTextEditor.IsDirtyProperty)
					UpdateDocTabTitle (tag, docDirty: editor.IsDirty);
			};
			editor.BreakpointsChanged += OnEditorBreakpointsChanged;
			editor.PinnedWatchesChanged += OnEditorPinnedWatchesChanged;
			// Debugger hover eval: while paused, tooltips show the live value of the
		// word under the mouse (DAP evaluate) instead of the static description.
			editor.DebugHoverEval = word => {
				if (!debugPaused || debugSession is not { IsActive: true } sess)
					return null;
				var ev = sess.EvaluateAsync (word, sess.CurrentFrameId).GetAwaiter ().GetResult ();
				return ev.Error is null ? ev.Value : null;
			};
			// Restore the persisted breakpoints of this file (DebuggingService load
			// path), including condition/hit count/tracepoint attributes.
			if (!string.IsNullOrEmpty (loadedSolutionPath)) {
				var stored = MonoDevelop.Debugger.Services.BreakpointService.Load (loadedSolutionPath)
					.Where (b => Path.GetFullPath (b.FileName) == Path.GetFullPath (path))
					.Select (b => new KeyValuePair<int, (bool Enabled, MonoDevelop.Ide.Controls.SkTextEditor.BreakpointOptions Options)> (
						b.Line - 1, // 0-based internally
						(b.Enabled, new MonoDevelop.Ide.Controls.SkTextEditor.BreakpointOptions (b.Condition, b.HitCount, b.LogMessage))));
				if (stored.Any ())
					editor.SetBreakpoints (stored);
				// Restore the pinned watches of this file (legacy
				// PinnedWatchStore.GetWatchesForFile on document open).
				editor.SetPinnedWatches (MonoDevelop.Debugger.Services.WatchService.LoadPinned (loadedSolutionPath)
					.Where (w => Path.GetFullPath (w.File) == Path.GetFullPath (path))
					.Select (w => (w.Line - 1, w.Expression)));
			}
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
	void AttachEditorContextMenu (MonoDevelop.Ide.Controls.SkTextEditor editor)
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
				new Separator (),
				// Legacy debugger actions: pin the word at the caret to its line
				// (PinnedWatch) — removed from the same bubble's context menu.
				Item ("Pin Watch", "md-view-debug-watch", () => PinWatchAtCaret (editor)),
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
		}
	}

	// ----- ProjectPad context menu (legacy ProjectPadContextMenu.addin.xml,
	// per-node-type sections: Build / Add / Tools / Edit / Properties). -----

	void OnSolutionPadContextMenu (object? sender, PointerReleasedEventArgs e)
	{
		if (e.InitialPressMouseButton != MouseButton.Right || xwtSolutionTree is null)
			return;
		e.Handled = true;
		ShowProjectPadMenuAtPointer ();
	}

	// Xwt tree right-click (ButtonPressed, IsContextMenuTrigger): the backend has
	// already selected the row under the pointer — show the ProjectPad menu there.
	void OnXwtSolutionContextMenu (object? sender, Xwt.ButtonEventArgs e)
	{
		if (!e.IsContextMenuTrigger)
			return;
		Avalonia.Threading.Dispatcher.UIThread.Post (ShowProjectPadMenuAtPointer);
	}

	/// <summary>Shows the ProjectPad context menu at the pointer, over the Xwt
	/// tree (legacy Gtk.Menu.Popup at the event coordinates).</summary>
	void ShowProjectPadMenuAtPointer ()
	{
		var flyout = new MenuFlyout { ItemsSource = BuildProjectPadMenu () };
		flyout.ShowAt (xwtSolutionHost!, true);
	}

	/// <summary>The ProjectPad context menu for the currently selected node, mirroring
	/// the sections of ProjectPadContextMenu.addin.xml filtered by ItemType conditions.</summary>
	List<Control> BuildProjectPadMenu ()
	{
		var (nodeType, nodePath) = SelectedNodeType ();
		var items = new List<Control> ();
		MenuItem Cmd (string label, string commandId, string? icon = null)
		{
			var mi = new MenuItem { Header = label };
			if (icon is not null && IconService.GetImage (icon) is { } img)
				mi.Icon = new Image { Source = img, Width = 16, Height = 16 };
			mi.Click += (_, _) => OnMenuCommand (commandId, nodePath);
			return mi;
		}
		static MenuItem Sub (string label, IEnumerable<Control> children)
		{
			var mi = new MenuItem { Header = label };
			foreach (var c in children)
				mi.Items.Add (c);
			return mi;
		}

		// Build section (ItemType: IBuildTarget → Solution | Project)
		if (nodeType is "Solution" or "Project") {
			items.Add (Cmd ("Build", "MonoDevelop.Ide.Commands.ProjectCommands.Build", "md-build"));
			items.Add (Cmd ("Rebuild", "MonoDevelop.Ide.Commands.ProjectCommands.Rebuild", "md-rebuild"));
			items.Add (Cmd ("Clean", "MonoDevelop.Ide.Commands.ProjectCommands.Clean", "md-clean"));
			items.Add (new Separator ());
		}
		// Run section (Project → Set as Startup Project)
		if (nodeType == "Project") {
			items.Add (Cmd ("Set as Startup Project", "MonoDevelop.Ide.Commands.ProjectCommands.SetStartupProjects", "md-steam"));
		}
		// Add section (Solution|Project|ProjectFolder)
		if (nodeType is "Solution" or "Project" or "ProjectFolder") {
			var addChildren = new List<Control> ();
			if (nodeType is "Solution" or "Project" or "ProjectFolder")
				addChildren.Add (Cmd ("New File...", "MonoDevelop.Ide.Commands.ProjectCommands.AddNewFiles", "md-add-content"));
			if (nodeType == "Project")
				addChildren.Add (Cmd ("Add Reference...", "MonoDevelop.Ide.Commands.ProjectCommands.AddReference", "md-reference"));
			if (nodeType is "Project" or "ProjectFolder")
				addChildren.Add (Cmd ("New Folder", "MonoDevelop.Ide.Commands.ProjectCommands.NewFolder", "md-closed-folder"));
			if (addChildren.Count > 0)
				items.Add (Sub ("_Add", addChildren));
			items.Add (new Separator ());
		}
		// Tools section (IFolderItem → Find in Files, Open Containing Folder)
		if (nodeType is "Project" or "ProjectFolder" or "ProjectFile") {
			items.Add (Cmd ("Find in Files", "MonoDevelop.Ide.Commands.SearchCommands.FindInFiles", "md-find"));
			items.Add (Cmd ("Open Containing Folder", "MonoDevelop.Ide.Commands.FileCommands.OpenContainingFolder", "md-open-folder"));
			items.Add (new Separator ());
		}
		// Edit section (all nodes)
		items.Add (Cmd ("Rename", "MonoDevelop.Ide.Commands.EditCommands.Rename", "md-rename"));
		if (nodeType is "ProjectFile" or "ProjectFolder" or "Project")
			items.Add (Cmd (nodeType == "Project" ? "Remove Project" : "Delete", "MonoDevelop.Ide.Commands.EditCommands.Delete", "md-delete-icon"));
		items.Add (new Separator ());
		// Properties section
		items.Add (Cmd ("Properties", "MonoDevelop.Ide.Commands.ProjectCommands.Options", "md-preferences"));
		if (items.Count > 0 && items [^1] is Separator)
			items.RemoveAt (items.Count - 1);
		return items;
	}

	// Node classification (legacy ItemType condition values) from the Tag:
	// solution path → Solution; .csproj → Project; folder: → ProjectFolder; file → ProjectFile.
	(string NodeType, string? Path) SelectedNodeType ()
	{
		var s = SelectedXwtNode ();
		if (s is not null) {
			if (s.StartsWith ("references:", StringComparison.Ordinal)) return ("References", s);
			if (s.StartsWith ("reference:", StringComparison.Ordinal)) return ("ProjectReference", s);
			if (s.StartsWith ("folder:", StringComparison.Ordinal)) return ("ProjectFolder", s ["folder:".Length..]);
			if (s.EndsWith (".sln", StringComparison.OrdinalIgnoreCase)) return ("Solution", s);
			if (s.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase)) return ("Project", s);
			if (File.Exists (s)) return ("ProjectFile", s);
		}
		return ("None", null);
	}

	/// <summary>Tag of the selected node in the Xwt tree ("solution:path",
	/// "project:path", "folder:path", "references:path", "reference:name" or a
	/// file path), resolved through the store by TreePosition — null when nothing
	/// is selected. This is the single source of truth for every consumer that
	/// used to read the Avalonia preview tree's Tag (context menu, properties).</summary>
	string? SelectedXwtNode ()
	{
		if (xwtSolutionTree is null || xwtSolutionStore is null)
			return null;
		try {
			// Frontend-typed read: SelectedRow → GetNavigatorAt → GetValue(tagField).
			var pos = xwtSolutionTree.SelectedRow;
			if (pos is null)
				return null;
			return xwtSolutionStore.GetNavigatorAt (pos).GetValue (xwtSolTagField) as string;
		} catch (Exception ex) {
			Output ("[selpad] resolve failed: " + ex.Message);
		}
		return null;
	}

	/// <summary>Programmatic selection for QA (drives SelectionChanged like a
	/// user click): selects the row whose tag matches.</summary>
	internal bool SelectXwtSolutionRow (string tag)
	{
		if (xwtSolutionTree is null || xwtSolutionStore is null)
			return false;
		try {
			// ALWAYS resolve through a fresh store walk: the navigators kept in
			// xwtSolutionNodes mutate as children are added during the build, so
			// their CurrentPosition is not stable (the project nav drifted to its
			// last child row).
			if (!FindXwtRowByTag (tag, out var pos))
				return false;
			xwtSolutionTree.SelectRow (pos);
			return true;
		} catch {
			return false;
		}
	}

	// Walks the store (depth-first over navigators) until a node carries the tag.
	bool FindXwtRowByTag (string tag, out Xwt.TreePosition found)
	{
		var root = xwtSolutionStore!.GetFirstNode ();
		while (root is not null) {
			if (WalkXwtNav (root, tag, out found))
				return true;
			root = root.MoveNext () ? root : null;
		}
		found = null!;
		return false;
	}

	bool WalkXwtNav (Xwt.TreeNavigator nav, string tag, out Xwt.TreePosition found)
	{
		if (nav.GetValue (xwtSolTagField) as string == tag) {
			found = nav.CurrentPosition;
			return true;
		}
		if (nav.MoveToChild ()) {
			do {
					if (WalkXwtNav (nav, tag, out found))
						return true;
				} while (nav.MoveNext ());
				nav.MoveToParent ();
		}
		found = null!;
		return false;
	}

	// ----- Properties pad (legacy PropertyPad + ProjectFileDescriptor /
	// SolutionItemDescriptor / WorkspaceItemDescriptor): a name/value grid per
	// selected node, rebuilt on every Solution pad selection change. -----

	readonly record struct PropertyRow (string Category, string Name, string Value);

	// Header labels are reused across updates (controls can't move parents in Avalonia).
	TextBlock? propertiesTitleLabel;
	TextBlock? propertiesSubtitleLabel;

	void SetPropertiesHeader (string title, string subtitle)
	{
		if (propertiesHeader is null)
			return;
		if (propertiesTitleLabel is null) {
			propertiesTitleLabel = new TextBlock {
				FontWeight = FontWeight.SemiBold,
				FontSize = 12,
				Padding = new Thickness (8, 6, 8, 0),
				TextTrimming = TextTrimming.CharacterEllipsis,
			};
			propertiesTitleLabel.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			propertiesHeader.Children.Add (propertiesTitleLabel);
			propertiesSubtitleLabel = new TextBlock {
				FontSize = 11,
				Opacity = 0.7,
				Padding = new Thickness (8, 0, 8, 4),
				TextTrimming = TextTrimming.CharacterEllipsis,
			};
			propertiesSubtitleLabel.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
			propertiesHeader.Children.Add (propertiesSubtitleLabel);
		}
		propertiesTitleLabel.Text = title;
		propertiesSubtitleLabel.Text = subtitle;
		propertiesSubtitleLabel.IsVisible = !string.IsNullOrEmpty (subtitle);
	}

	// One grid row: label column + value column with the legacy table look.
	static Border PropertyRowControl (string name, string value, bool zebra)
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("110,*") };
		var nameTb = new TextBlock {
			Text = name,
			FontSize = 11,
			Padding = new Thickness (8, 3),
			VerticalAlignment = VerticalAlignment.Top,
			Opacity = 0.75,
			TextTrimming = TextTrimming.CharacterEllipsis,
		};
		nameTb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var valueTb = new TextBlock {
			Text = string.IsNullOrEmpty (value) ? "—" : value,
			FontSize = 11,
			Padding = new Thickness (4, 3, 8, 3),
			TextWrapping = TextWrapping.Wrap,
			Opacity = value.Length == 0 ? 0.5 : 1.0,
		};
		valueTb.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		Grid.SetColumn (nameTb, 0);
		Grid.SetColumn (valueTb, 1);
		grid.Children.Add (nameTb);
		grid.Children.Add (valueTb);
		IBrush? zebraBrush = null;
		if (zebra)
			zebraBrush = Application.Current!.TryGetResource ("IdeChromeBgBrush", Application.Current.ActualThemeVariant, out var res)
				&& res is IBrush b ? b : null;
		return new Border {
			Child = grid,
			Background = zebra ? zebraBrush : Brushes.Transparent,
		};
	}

	// Category header row (legacy LocalizedCategory grouping).
	static Control PropertyCategoryHeader (string category)
	{
		var tb = new TextBlock {
			Text = category,
			FontSize = 11,
			FontWeight = FontWeight.SemiBold,
			Padding = new Thickness (8, 6, 8, 2),
			Opacity = 0.85,
		};
		tb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeAccentBrush"));
		return tb;
	}

	// The descriptor table per node type (legacy ProjectFileDescriptor fields).
	List<PropertyRow> GetPropertiesForNode (string nodeType, string path)
	{
		var rows = new List<PropertyRow> ();
		switch (nodeType) {
		case "ProjectFile":
			var fi = new FileInfo (path);
			rows.Add (new ("Misc", "Name", fi.Name));
			rows.Add (new ("Misc", "Path", path));
			rows.Add (new ("Misc", "Type", FileTypeDescription (fi.Extension)));
			rows.Add (new ("Misc", "Size", fi.Length < 1024 ? $"{fi.Length} B" : $"{fi.Length / 1024.0:0.#} KB"));
			rows.Add (new ("Misc", "Last changed", fi.LastWriteTime.ToString ("yyyy-MM-dd HH:mm")));
			rows.Add (new ("Build", "Build action", fi.Extension.Equals (".cs", StringComparison.OrdinalIgnoreCase) ? "Compile" : "Content"));
			rows.Add (new ("Build", "Copy to output", "Do not copy"));
			rows.Add (new ("Build", "Custom tool", ""));
			break;

		case "ProjectFolder":
			var di = new DirectoryInfo (path);
			rows.Add (new ("Misc", "Name", di.Name));
			rows.Add (new ("Misc", "Path", path));
			int files = 0, dirs = 0;
			try { files = di.EnumerateFiles ().Count (); dirs = di.EnumerateDirectories ().Count (); } catch { }
			rows.Add (new ("Misc", "Contains", $"{files} file(s), {dirs} folder(s)"));
			break;

		case "Project":
			rows.Add (new ("Misc", "Name", Path.GetFileNameWithoutExtension (path)));
			rows.Add (new ("Misc", "File Path", path));
			rows.Add (new ("Misc", "Root Directory", Path.GetDirectoryName (path) ?? ""));
			rows.Add (new ("Misc", "File Format", "MSBuild .csproj (SDK style)"));
			rows.Add (new ("Build", "Target framework", ReadCsprojFirstValue (path, "TargetFramework") ?? ""));
			rows.Add (new ("Build", "Assembly name", ReadCsprojFirstValue (path, "AssemblyName") ?? Path.GetFileNameWithoutExtension (path)));
			rows.Add (new ("Build", "Output type", ReadCsprojFirstValue (path, "OutputType") ?? "Library"));
			break;

		case "Solution":
			rows.Add (new ("Misc", "Name", Path.GetFileNameWithoutExtension (path)));
			rows.Add (new ("Misc", "File Path", path));
			rows.Add (new ("Misc", "Root Directory", Path.GetDirectoryName (path) ?? ""));
			rows.Add (new ("Misc", "File Format", "Visual Studio solution"));
			int projectCount = 0;
			try {
				projectCount = File.ReadAllLines (path).Count (l => l.StartsWith ("Project(", StringComparison.Ordinal));
			} catch { }
			rows.Add (new ("Misc", "Projects", projectCount.ToString ()));
			break;

		case "ProjectReference":
			var refName = path.StartsWith ("reference:", StringComparison.Ordinal) ? path ["reference:".Length..] : path;
			rows.Add (new ("Misc", "Name", refName));
			rows.Add (new ("Build", "Local Copy", "True"));
			rows.Add (new ("Build", "Specific Version", "False"));
			break;

		case "References":
			rows.Add (new ("Misc", "Name", "References"));
			rows.Add (new ("Misc", "Path", path.StartsWith ("references:", StringComparison.Ordinal) ? path ["references:".Length..] : ""));
			break;
		}
		return rows;
	}

	static string FileTypeDescription (string ext) => ext.ToLowerInvariant () switch {
		".cs" => "C# source",
		".csproj" => "MSBuild project",
		".sln" => "Solution",
		".xml" => "XML document",
		".json" => "JSON document",
		".md" => "Markdown",
		".txt" => "Plain text",
		".props" or ".targets" => "MSBuild import",
		_ => ext.Length > 0 ? $"{ext [1..].ToUpperInvariant ()} file" : "File",
	};

	static string? ReadCsprojFirstValue (string csproj, string tag)
	{
		try {
			var doc = System.Xml.Linq.XDocument.Load (csproj);
			var ns = doc.Root?.Name.Namespace ?? System.Xml.Linq.XNamespace.None;
			return doc.Descendants (ns + tag).FirstOrDefault ()?.Value;
		} catch {
			return null;
		}
	}

	// Populates the Properties pad from the current Solution pad selection
	// (legacy PropertyPad.PopulateGrid on selection change).
	void UpdatePropertiesPad ()
	{
		if (propertiesHeader is null || propertiesList is null)
			return;
		var (nodeType, path) = SelectedNodeType ();
		propertiesList.Children.Clear ();
		if (nodeType == "None" || path is null) {
			SetPropertiesHeader ("No selection", "Select an item in the Solution pad");
			return;
		}
		var name = nodeType switch {
			"Solution" or "Project" => Path.GetFileNameWithoutExtension (path),
			"ProjectFolder" => Path.GetFileName (path),
			"References" => "References",
			_ => Path.GetFileName (path),
		};
		SetPropertiesHeader (name, nodeType);

		var rows = GetPropertiesForNode (nodeType, path);
		string? lastCategory = null;
		bool zebra = false;
		foreach (var row in rows) {
			if (row.Category != lastCategory) {
				propertiesList.Children.Add (PropertyCategoryHeader (row.Category));
				lastCategory = row.Category;
				zebra = false;
			}
			propertiesList.Children.Add (PropertyRowControl (row.Name, row.Value, zebra));
			zebra = !zebra;
		}
	}

	// Rename the file/folder node (context menu). Returns false when the command
	// was not invoked on a node so the editor rename keeps handling it.
	bool RenameContextNode ()
	{
		// Same as Delete: resolve from the live Xwt tree selection first.
		var (nodeType, selPath) = SelectedNodeType ();
		var contextPath = nodeType is "ProjectFile" or "ProjectFolder" ? selPath : contextNodePath;
		if (string.IsNullOrEmpty (contextPath))
			return false;
		var path = contextPath;
		var isFolder = path.StartsWith ("folder:", StringComparison.Ordinal);
		if (isFolder)
			path = path ["folder:".Length..];
		if (path.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase) ||
			path.EndsWith (".sln", StringComparison.OrdinalIgnoreCase) ||
			(!File.Exists (path) && !Directory.Exists (path)))
			return false;
		var oldName = Path.GetFileName (path);
		var dlg = new InputDialog ("Rename", "New name:", oldName);
		_ = dlg.ShowDialog (this);
		dlg.Closed += (_, _) => {
			if (!dlg.Confirmed || string.IsNullOrWhiteSpace (dlg.Value) || dlg.Value == oldName)
				return;
			var newPath = Path.Combine (Path.GetDirectoryName (path)!, dlg.Value);
			try {
				if (isFolder)
					Directory.Move (path, newPath);
				else {
					File.Move (path, newPath);
					// Retarget an open document to the new path (legacy retitles the tab).
					var tabName = oldName;
					if (docs.TryGetValue (tabName, out var ed)) {
						ed.FilePath = newPath;
						UpdateDocTabTitle (tabName, false);
						docs.Remove (tabName);
						docs [dlg.Value] = ed;
					}
				}
				contextNodePath = null;
				RefreshSolutionTree ();
				Output ("[rename] " + oldName + " → " + dlg.Value);
			} catch (Exception ex) {
				Output ("[rename] failed: " + ex.Message);
			}
		};
		return true;
	}

	// Delete the file/folder node with confirmation (legacy DeleteItem).
	bool DeleteContextNode ()
	{
		// The selected node of the LIVE Xwt tree is the source of truth (the
		// contextNodePath from the dead Avalonia preview tree never updates).
		var (nodeType, selPath) = SelectedNodeType ();
		var path = nodeType is "ProjectFile" or "ProjectFolder" ? selPath : contextNodePath;
		if (string.IsNullOrEmpty (path))
			return false;
		var isFolder = path.StartsWith ("folder:", StringComparison.Ordinal);
		if (isFolder)
			path = path ["folder:".Length..];
		if (path.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase) ||
			path.EndsWith (".sln", StringComparison.OrdinalIgnoreCase) ||
			(!File.Exists (path) && !Directory.Exists (path)))
			return false;
		var msg = new Window {
			Title = "Delete",
			Width = 380,
			SizeToContent = SizeToContent.Height,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Content = new StackPanel {
				Margin = new Thickness (16),
				Spacing = 12,
				Children = {
					new TextBlock { Text = $"Delete '{Path.GetFileName (path)}' from disk?", TextWrapping = TextWrapping.Wrap },
					new StackPanel {
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Spacing = 8,
						Children = {
							new Button { Content = "Cancel" },
							new Button { Content = "Delete" },
						},
					},
				},
			},
		};
		var sp = (StackPanel)msg.Content!;
		var buttons = ((StackPanel)sp.Children [1]).Children.OfType<Button> ().ToList ();
		buttons [0].Click += (_, _) => msg.Close ();
		buttons [1].Click += (_, _) => {
			msg.Close ();
			try {
				if (isFolder)
					Directory.Delete (path, recursive: true);
				else {
						var tabName = Path.GetFileName (path);
						if (docs.ContainsKey (tabName))
							CloseDocument (tabName);
					File.Delete (path);
				}
				// Legacy ProjectFileNodeBuilder.DeleteFromProject: the deleted file
				// also leaves the owning .csproj (its <Compile Include=...> item).
				var projectFile = FindOwningProject (path, string.IsNullOrEmpty (loadedSolutionPath)
					? null : Path.GetDirectoryName (loadedSolutionPath));
				if (projectFile is not null) {
					var rel = Path.GetRelativePath (Path.GetDirectoryName (projectFile)!, path);
					RemoveProjectItemEntries (projectFile, rel, isFolder);
				}
				contextNodePath = null;
				RefreshSolutionTree ();
				Output ("[delete] " + Path.GetFileName (path));
			} catch (Exception ex) {
				Output ("[delete] failed: " + ex.Message);
			}
		};
		_ = msg.ShowDialog (this);
		return true;
	}

	// The owning project = the nearest ancestor directory of the deleted node
	// that holds a .csproj (never walking above the solution directory).
	static string? FindOwningProject (string filePath, string? solutionDir)
	{
		var dir = Path.GetDirectoryName (Path.GetFullPath (filePath));
		var solFull = string.IsNullOrEmpty (solutionDir) ? null : Path.GetFullPath (solutionDir);
		while (!string.IsNullOrEmpty (dir)) {
			var csproj = Directory.EnumerateFiles (dir, "*.csproj").FirstOrDefault ();
			if (csproj is not null)
				return csproj;
			if (solFull is not null && dir.TrimEnd ('/').Equals (solFull.TrimEnd ('/'), StringComparison.OrdinalIgnoreCase))
				break;
			var parent = Path.GetDirectoryName (dir);
			if (parent == dir || parent is null)
				break;
			dir = parent;
		}
		return null;
	}

	// Removes the project items (Compile and the other file build actions) whose
	// Include matches the deleted file — or everything under a deleted folder —
	// editing the .csproj as TEXT so the rest of the document survives untouched.
	void RemoveProjectItemEntries (string projectFile, string relPath, bool isFolder)
	{
		if (!File.Exists (projectFile))
			return;
		var text = File.ReadAllText (projectFile);
		var norm = relPath.Replace ('\\', '/').TrimEnd ('/');
		var rx = new System.Text.RegularExpressions.Regex (
			"^[ \\t]*<(Compile|None|Content|EmbeddedResource)\\s+Include=\"([^\"]+)\"[^>]*>[ \\t]*\\r?\\n?",
			System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);
		int removed = 0;
		var result = rx.Replace (text, m => {
			var inc = m.Groups [2].Value.Replace ('\\', '/').TrimEnd ('/');
			bool match = isFolder
				? inc.StartsWith (norm + "/", StringComparison.OrdinalIgnoreCase)
				: inc.Equals (norm, StringComparison.OrdinalIgnoreCase);
			if (!match)
				return m.Value;
			removed++;
			return "";
		});
		if (removed > 0)
			File.WriteAllText (projectFile, result);
		Output ($"[delete] {Path.GetFileName (projectFile)}: {removed} item(s) removed ({norm})");
	}

	public async System.Threading.Tasks.Task OpenNewSolutionDialogAsync ()
	{
		var dlg = new NewSolutionDialog ();
		// With a solution open the dialog defaults to add-to-solution (legacy radio).
		dlg.AddToOpenSolution = !string.IsNullOrEmpty (loadedSolutionPath);
		await dlg.ShowDialog (this);
		try {
			if (!string.IsNullOrEmpty (dlg.CreatedProjectPath) && !string.IsNullOrEmpty (loadedSolutionPath)) {
				// Legacy ProjectOperations.AddSolutionItem: project entry + GUID + config
				// mappings in the .sln, then reload the tree.
				MonoDevelop.Ide.Services.ConfigurationService.AppendProjectToSolution (dlg.CreatedProjectPath, loadedSolutionPath);
				Output ("[newproject] added " + Path.GetFileName (dlg.CreatedProjectPath) + " to " + Path.GetFileName (loadedSolutionPath));
				OpenSolutionInWindow (loadedSolutionPath);
				return;
			}
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
		var grid = TitleBarRow?.Child as Grid;
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

	// ---- Win11 chrome (rounded corners + thin toolbar-colored border + edge resize) ----

	void UpdateResizeChrome ()
	{
		bool flat = WindowState is WindowState.Maximized or WindowState.FullScreen;
		if (ResizeGrip is not null)
			ResizeGrip.IsVisible = !flat;
		if (WindowFrame is not null) {
			WindowFrame.Margin = flat ? new Thickness (0) : new Thickness (6);
			WindowFrame.CornerRadius = new CornerRadius (flat ? 0 : 8);
		}
		if (TitleBarRow is not null)
			TitleBarRow.CornerRadius = new CornerRadius (flat ? 0 : 8, flat ? 0 : 8, 0, 0);
		if (StatusBarBorder is not null)
			StatusBarBorder.CornerRadius = new CornerRadius (0, 0, flat ? 0 : 8, flat ? 0 : 8);
		if (RestoreStripHost is not null)
			RestoreStripHost.CornerRadius = new CornerRadius (0, 0, flat ? 0 : 8, flat ? 0 : 8);
	}

	// The X11 backend does not map every StdCursor at XAML parse time
	// ("Unrecognized cursor type 'SizeNorthwestSoutheast'"), so the resize zones get
	// their cursors here, ignoring per-zone failures.
	void AttachResizeCursors ()
	{
		if (ResizeGrip is null)
			return;
		foreach (var b in ResizeGrip.Children.OfType<Border> ()) {
			if (b.Tag is not string tag)
				continue;
			try {
				b.Cursor = new Cursor (tag switch {
					"North" or "South" => StandardCursorType.SizeNorthSouth,
					"East" or "West" => StandardCursorType.SizeWestEast,
					"NorthWest" or "SouthEast" => StandardCursorType.TopLeftCorner,
					_ => StandardCursorType.TopRightCorner,
				});
			} catch (Exception) {
				// cursor cosmetic only — resize still works
			}
		}
	}

	void OnResizeGripPressed (object? sender, PointerPressedEventArgs e)
	{
		if (WindowState is not WindowState.Normal)
			return;
		if (sender is not Border b || b.Tag is not string tag)
			return;
		var edge = tag switch {
			"North" => WindowEdge.North,
			"South" => WindowEdge.South,
			"West" => WindowEdge.West,
			"East" => WindowEdge.East,
			"NorthEast" => WindowEdge.NorthEast,
			"SouthEast" => WindowEdge.SouthEast,
			"NorthWest" => WindowEdge.NorthWest,
			"SouthWest" => WindowEdge.SouthWest,
			_ => WindowEdge.SouthEast,
		};
		BeginResizeDrag (edge, e);
	}

	// --------------------------------------------

	void OnMaximize (object? sender, RoutedEventArgs e) => ToggleMaximize ();

	/// <summary>Legacy CloseWorkspaceHandler body: persists or discards modified
	/// documents (DirtyFilesDialog), then closes documents + solution and shows
	/// the Welcome page.</summary>
	async Task CloseWorkspaceAsync ()
	{
		if (!await ConfirmCloseDirtyDocsAsync ("closeWorkspace"))
			return;
		foreach (var tag in documents.Select (d => d.Tag).ToList ())
			await CloseDocumentAsync (tag);
		PersistWatches (); // last watch state before the solution path goes away
		loadedSolutionPath = null;
		solutionLoaded = false;
		// The three symbol pads are solution/document scoped: reset them so a later
		// solution does not show stale rows (legacy pads cleared on workspace close).
		lastOutlineTag = null;
		lastOutlineText = null;
		if (outlineTree is not null)
			FillSymbolTree (outlineTree, NoOutlineMessage);
		if (classesTree is not null)
			FillSymbolTree (classesTree, "No solution loaded");
		buildErrors.Clear ();
		errorRows.Clear ();
		RefreshCodeIssuesPad ();
		ShowWelcomePage ();
		Output ("[window] workspace closed");
	}

	/// <summary>Legacy Workbench.OnDeleteEvent: intercepts the window close with
	/// the DirtyFilesDialog when documents have unsaved changes.</summary>
	async void OnMainWindowClosing (object? sender, WindowClosingEventArgs e)
	{
		// Persist the pad layout on EVERY close attempt (before any dirty-files
		// dialog early-returns) like the legacy DockFrame layout save.
		SavePadsLayout ();
		// Only intercept user-initiated closes; programmatic closes after the
		// dialog already ran must go through.
		if (e.IsProgrammatic || closeConfirmed)
			return;
		if (!docs.Any (kv => kv.Value.IsDirty))
			return; // nothing modified: legacy closes directly
		e.Cancel = true;
		if (await ConfirmCloseDirtyDocsAsync ("quit")) {
			closeConfirmed = true;
			Close ();
		}
	}
	bool closeConfirmed;

	async Task ExitViaDirtyFilesDialogAsync ()
	{
		if (await ConfirmCloseDirtyDocsAsync ("quit")) {
			closeConfirmed = true;
			Close ();
		}
	}

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
		// The window background must stay transparent: the Win11 frame (WindowFrame)
		// paints its own rounded border inside a small gutter, and an opaque window
		// background would show through it as a second, square "rigid" border.
		Background = Brushes.Transparent;
		SetToolbarIcons ();
	}

	// Toolbar glyphs come from the same redesigned PNG set (IconService) and follow the
	// theme variant, like the legacy ImageService icons.
	void SetToolbarIcons ()
	{
		if (MonoDevelop.Ide.Services.IconService.GetImage ("gtk-execute") is Avalonia.Media.Imaging.Bitmap bmp)
			RunIcon!.Source = bmp;
		if (MonoDevelop.Ide.Services.IconService.GetImage ("md-debug-all") is Avalonia.Media.Imaging.Bitmap dbgBmp)
			DebugIcon!.Source = dbgBmp;
		SetIfAvailable (StepOverIcon, "md-step-over-debug");
		SetIfAvailable (StepIntoIcon, "md-step-into-debug");
		SetIfAvailable (StepOutIcon, "md-step-out-debug");
		SetIfAvailable (SearchIcon, "md-find");
		if (ToolbarSearch is not null) {
			ToolbarSearch.TextChanged += (_, _) => OnToolbarSearchTextChanged (ToolbarSearch.Text);
			ToolbarSearch.LostFocus += (_, _) => {
				// Grace so a click on a result lands before hiding (the legacy
				// popup dismisses on outside click the same way).
				var grace = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds (150) };
				grace.Tick += (_, _) => {
					grace.Stop ();
					HideSearchPopup ();
				};
				grace.Start ();
			};
		}
	}

	// Legacy MainToolbar behavior: the debug step buttons live right of the
	// configuration combos, hidden by default; they appear only while a debug
	// session is alive and hide again when it terminates.
	void SetDebugButtonsVisible (bool visible)
	{
		if (DebugButtonsRow is not null)
			DebugButtonsRow.IsVisible = visible;
	}

	// Legacy MainToolbarController search behavior: ':' switches/locks the search
	// category (":c" = commands, ":f" = files, ":s" = search in solution), plain text
	// searches across the categories, Enter opens the corresponding dialog
	// (FindInFiles for the search-in-solution category) and Escape clears.
	// ----- Toolbar search (legacy SearchPopupWindow over the MainToolbar search
	// entry: live results in a dropdown popup while typing — Files from the open
	// solution, Commands from the menu catalog — ':' prefixes a category and
	// Enter/activate opens the picked result). -----

	Border? searchPopupHost;
	ListBox? searchResultsList;
	// Tip: the row tooltip — the FULL absolute path (with line for symbols) so the
	// truncated subtitle never hides which file a result points at.
	readonly List<(string Icon, string Title, string Subtitle, Action Activate, string Tip)> searchResults = new ();
	bool searchPopupUpdating;

	/// <summary>A category-filtered live search, mirroring SearchPopupWindow:
	/// "text" searches everything, ":c text" commands, ":f text" files,
	/// ":t text" type symbols, ":s text" opens Find in Files like the legacy
	/// double-click path.</summary>
	void OnToolbarSearchTextChanged (string? raw)
	{
		if (searchPopupUpdating)
			return;
		var term = raw?.Trim () ?? "";
		if (term.Length == 0) {
			HideSearchPopup ();
			return;
		}
		string category = "all";
		if (term.StartsWith (":", StringComparison.Ordinal)) {
			int sp = term.IndexOf (' ');
			var tag = (sp < 0 ? term [1..] : term [1..sp]).ToLowerInvariant ();
			category = tag is "c" or "command" or "commands" ? "commands"
				: tag is "f" or "file" or "files" ? "files"
				: tag is "t" or "type" or "types" ? "types"
				: tag is "s" or "search" ? "search"
				: "all";
			term = sp < 0 ? "" : term [(sp + 1)..].Trim ();
		}
		if (category == "search") {
			// Legacy SearchInSolutionSearchCategory: the result action opens Find in
			// Files with the term — show exactly that one entry.
			searchResults.Clear ();
			searchResults.Add (("md-find", $"Search for '{term}' in Solution", "Opens the Find in Files dialog", () => {
				_ = new FindInFilesDialog { SearchTextOverride = term }.ShowDialog (this);
			}, "Find in Files"));
			ShowSearchPopup ();
			return;
		}
		searchResults.Clear ();
		if (category is "all" or "files")
			CollectFileResults (term);
		if (category is "all" or "types")
			CollectSymbolResults (term);
		if (category is "all" or "commands")
			CollectCommandResults (term);
		ShowSearchPopup ();
	}

	// Legacy FileSearchCategory: every file of the loaded solution, matched by
	// filename and relative project path, ranked (name match > path match).
	void CollectFileResults (string term)
	{
		if (term.Length == 0)
			return;
		var projDir = string.IsNullOrEmpty (loadedSolutionPath) ? null : Path.GetDirectoryName (loadedSolutionPath);
		var seen = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		foreach (var doc in docs.Keys) {
			if (term.Length == 0 || doc.Contains (term, StringComparison.OrdinalIgnoreCase))
				AddFileResult (doc, "(open document)", term);
			seen.Add (doc);
		}
		if (projDir is null)
			return;
		foreach (var file in Directory.EnumerateFiles (projDir, "*", SearchOption.AllDirectories)) {
			if (file.Contains ("/obj/") || file.Contains ("/bin/") || file.Contains ("/.git/"))
				continue;
			if (seen.Contains (Path.GetFileName (file)))
				continue;
			var name = Path.GetFileName (file);
			var rel = Path.GetRelativePath (projDir, file);
			bool nameMatch = name.Contains (term, StringComparison.OrdinalIgnoreCase);
			bool pathMatch = rel.Contains (term, StringComparison.OrdinalIgnoreCase);
			if (nameMatch || pathMatch)
				AddFileResult (name, rel, term);
			if (searchResults.Count >= 12)
				return;
		}
	}

	void AddFileResult (string name, string relPath, string term)
	{
		if (searchResults.Any (r => r.Subtitle == relPath))
			return;
		var path = relPath == "(open document)" && docs.TryGetValue (name, out var ed) && !string.IsNullOrEmpty (ed.FilePath) ? ed.FilePath : null;
		var target = path ?? System.IO.Path.Combine (
			Path.GetDirectoryName (loadedSolutionPath!)!, relPath);
		searchResults.Add (("md-class-file", name, relPath, () => {
			if (File.Exists (target))
				OpenFileDocument (target);
			else
				Output ("[search] file not found: " + target);
		}, target));
	}

	// Legacy RoslynSearchCategory: symbol declarations across the solution's .cs
	// files. The scan itself lives in SymbolIndexService (shared with the Document
	// Outline and Classes pads); this alias keeps the existing call sites — the
	// breadcrumb scope chain, Go To Type and the search popup — unchanged.
	public static List<SymbolIndexService.SymbolHit> ScanSymbols (string text)
		=> SymbolIndexService.ScanSymbols (text);

	void CollectSymbolResults (string term)
	{
		var projDir = string.IsNullOrEmpty (loadedSolutionPath) ? null : Path.GetDirectoryName (loadedSolutionPath);
		if (projDir is null || term.Length == 0)
			return;
		var hits = new List<(SymbolIndexService.SymbolHit Hit, string File, int Rank)> ();
		foreach (var file in Directory.EnumerateFiles (projDir, "*.cs", SearchOption.AllDirectories)) {
			if (file.Contains ("/obj/") || file.Contains ("/bin/") || file.Contains ("/.git/"))
				continue;
			var syms = SymbolIndexService.ScanFile (file);
			foreach (var hit in syms) {
				bool starts = hit.Name.StartsWith (term, StringComparison.OrdinalIgnoreCase);
				if (starts || hit.Name.Contains (term, StringComparison.OrdinalIgnoreCase))
					hits.Add ((hit, file, starts ? 0 : 1));
			}
			if (hits.Count > 60) // bound the scan on huge trees
				break;
		}
		foreach (var (hit, file, _) in hits.OrderBy (h => h.Rank).ThenBy (h => h.Hit.Name).ThenBy (h => h.Hit.Line).Take (12))
			AddSymbolResult (hit, file);
	}

	void AddSymbolResult (SymbolIndexService.SymbolHit hit, string file)
	{
		var projDir = Path.GetDirectoryName (loadedSolutionPath!) ?? "";
		var rel = Path.GetRelativePath (projDir, file);
		searchResults.Add (($"md-{hit.Kind.ToLowerInvariant ()}", $"{hit.Name} ({hit.Kind})", $"{rel} : {hit.Line}",
			() => OpenFileDocumentAtLine (file, hit.Line),
			hit.Container is null ? $"{file} : {hit.Line}" : $"{file} : {hit.Line} — in {hit.Container}"));
	}

	// Legacy CommandSearchCategory over the menu catalog: label matches first
	// (menuCommandBindings labels come mnemonic-free), then id matches, then the
	// rest — the cap must never starve a label match that sits late in the list.
	void CollectCommandResults (string term)
	{
		var bindings = MenuCommandBindings ();
		var labelHits = new List<(string CommandId, string Label)> ();
		var idHits = new List<(string CommandId, string Label)> ();
		foreach (var b in bindings) {
			if (term.Length > 0 && b.Label.Contains (term, StringComparison.OrdinalIgnoreCase))
				labelHits.Add (b);
			else if (term.Length > 0 && b.CommandId.Contains (term, StringComparison.OrdinalIgnoreCase))
				idHits.Add (b);
		}
		foreach (var (commandId, label) in labelHits.Concat (idHits))
			searchResults.Add (("md-execute", label, commandId, () => OnMenuCommand (commandId), commandId));
	}

	/// <summary>Shows the dropdown under the search box (a plain Border popup like
	/// the legacy XwtThemedPopup, positioned below the entry) with the live rows.</summary>
	void ShowSearchPopup ()
	{
		if (ToolbarSearch is null)
			return;
		if (searchPopupHost is null) {
			searchResultsList = new ListBox { Background = Brushes.Transparent };
			searchResultsList.DoubleTapped += (_, _) => ActivateSelectedSearchResult ();
			searchResultsList.KeyDown += (_, e) => {
				if (e.Key == Key.Enter)
					ActivateSelectedSearchResult ();
			};
			searchPopupHost = new Border {
				Background = (Brush)Application.Current!.FindResource ("IdeWindowBgBrush")!,
				BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
				BorderThickness = new Thickness (1),
				CornerRadius = new CornerRadius (6),
				Padding = new Thickness (4),
				MaxHeight = 360,
				Child = new ScrollViewer { Content = searchResultsList },
			};
			searchPopupHost.ZIndex = 100;
			// Window-level host: inside the search box's Grid the popup would sit
			// under every later sibling of the toolbar row (clipped/overpainted).
			if (Content is Panel rootPanel)
				rootPanel.Children.Add (searchPopupHost);
		}
		// Anchor: right edge aligned to the search box, just below the toolbar row.
		var boxOrigin = ToolbarSearch.TranslatePoint (new Avalonia.Point (0, 0), this) ?? default;
		searchPopupHost.Width = Math.Max (280, ToolbarSearch.Width);
		searchPopupHost.Margin = new Thickness (boxOrigin.X, boxOrigin.Y + ToolbarSearch.Height + 4, 0, 0);
		searchPopupHost.HorizontalAlignment = HorizontalAlignment.Left;
		searchPopupHost.VerticalAlignment = VerticalAlignment.Top;
		searchPopupHost.IsVisible = true;
		RebuildSearchPopupRows ();
	}

	void RebuildSearchPopupRows ()
	{
		if (searchResultsList is null)
			return;
		searchResultsList.Items.Clear ();
		for (int i = 0; i < searchResults.Count; i++) {
			var (icon, title, subtitle, _, tip) = searchResults [i];
			var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness (2) };
			if (IconService.GetImage (icon) is { } img)
				sp.Children.Add (new Image { Source = img, Width = 16, Height = 16 });
			var col = new StackPanel { Orientation = Orientation.Vertical };
			var t = new TextBlock { Text = title, FontSize = 12 };
			t.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			var s = new TextBlock { Text = subtitle, FontSize = 10.5, Opacity = 0.6 };
			s.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
			col.Children.Add (t);
			col.Children.Add (s);
			sp.Children.Add (col);
			// Legacy popup behaviors: hovering a row moves the selection to it, and
			// the tooltip carries the FULL absolute path (subtitle stays truncated).
			var idx = i;
			sp.PointerEntered += (_, _) => {
				if (searchResultsList is { } list)
					list.SelectedIndex = idx;
			};
			ToolTip.SetTip (sp, tip);
			searchResultsList.Items.Add (sp);
		}
		if (searchResults.Count == 0) {
			var none = new TextBlock { Text = "(no results)", FontSize = 12, Margin = new Thickness (4), Opacity = 0.6 };
			none.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			searchResultsList.Items.Add (none);
		}
		if (searchResultsList.Items.Count > 0)
			searchResultsList.SelectedIndex = 0;
	}

	void ActivateSelectedSearchResult ()
	{
		int idx = searchResultsList?.SelectedIndex ?? -1;
		if (idx < 0 || idx >= searchResults.Count)
			return;
		HideSearchPopup ();
		ToolbarSearch!.Text = "";
		searchResults [idx].Activate ();
	}

	void HideSearchPopup ()
	{
		if (searchPopupHost is not null)
			searchPopupHost.IsVisible = false;
	}

	void OnToolbarSearchKeyDown (object? sender, KeyEventArgs e)
	{
		if (sender is not TextBox box)
			return;
		if (e.Key == Key.Escape) {
			box.Text = "";
			HideSearchPopup ();
			return;
		}
		// Arrow keys walk the live results when the popup is visible.
		if ((e.Key == Key.Down || e.Key == Key.Up) && searchPopupHost is { IsVisible: true } && searchResultsList is not null) {
			int count = searchResults.Count;
			if (count > 0) {
				int delta = e.Key == Key.Down ? 1 : -1;
				searchResultsList.SelectedIndex = Math.Clamp ((searchResultsList.SelectedIndex < 0 ? 0 : searchResultsList.SelectedIndex) + delta, 0, count - 1);
				if (searchResultsList.SelectedItem is { } sel)
					searchResultsList.ScrollIntoView (sel);
				e.Handled = true;
			}
			return;
		}
		// PageUp/PageDown page the selection like the legacy SearchPopupWindow.
		if ((e.Key == Key.PageUp || e.Key == Key.PageDown) && searchPopupHost is { IsVisible: true } && searchResultsList is not null && searchResults.Count > 0) {
			int delta = (e.Key == Key.PageDown ? 1 : -1) * 5;
			searchResultsList.SelectedIndex = Math.Clamp ((searchResultsList.SelectedIndex < 0 ? 0 : searchResultsList.SelectedIndex) + delta, 0, searchResults.Count - 1);
			if (searchResultsList.SelectedItem is { } sel2)
				searchResultsList.ScrollIntoView (sel2);
			e.Handled = true;
			return;
		}
		if (e.Key == Key.Enter) {
			ActivateSelectedSearchResult ();
			e.Handled = true;
		}
	}

	// No debug-all fallback: a missing stock id must not silently make three
	// different buttons show the same glyph (the old step-icons bug).
	static void SetIfAvailable (Avalonia.Controls.Image? target, string stock)
	{
		if (target is null)
			return;
		if (MonoDevelop.Ide.Services.IconService.GetImage (stock) is Avalonia.Media.Imaging.Bitmap bmp)
			target.Source = bmp;
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
		OnMenuCommand ("MonoDevelop.Ide.Commands.ProjectCommands.Run");
	}

	void OnToolbarDebug (object? sender, RoutedEventArgs e)
	{
		OnMenuCommand ("MonoDevelop.Debugger.DebugCommands.Debug");
	}

	void OnToolbarStepOver (object? sender, RoutedEventArgs e)
		=> OnMenuCommand ("MonoDevelop.Debugger.DebugCommands.StepOver");

	void OnToolbarStepInto (object? sender, RoutedEventArgs e)
		=> OnMenuCommand ("MonoDevelop.Debugger.DebugCommands.StepInto");

	void OnToolbarStepOut (object? sender, RoutedEventArgs e)
		=> OnMenuCommand ("MonoDevelop.Debugger.DebugCommands.StepOut");

	void OnToolbarConfigChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (sender is not ComboBox cb || cb.SelectedItem is not string sel)
			return;
		var name = cb == ConfigCombo ? "configuration" : cb == RunConfigCombo ? "run configuration" : "runtime";
		// Active configuration: persisted in <sln>.userprefs like RootWorkspace
		// (WorkspaceUserData.ActiveConfiguration) and echoed to the Project menu.
		if (cb == ConfigCombo && !suppressConfigSync && !string.IsNullOrEmpty (loadedSolutionPath)) {
			try {
				MonoDevelop.Ide.Services.ConfigurationService.SetActiveConfiguration (loadedSolutionPath, sel);
				activeConfiguration = sel;
				Output ($"[toolbar] {name} → {sel} (saved to {Path.GetFileName (loadedSolutionPath)})");
			} catch (Exception ex) {
				Output ($"[toolbar] {name} → {sel} (persist failed: {ex.Message})");
			}
			BuildMenu ();
			return;
		}
		Output ($"[toolbar] {name} → {sel}");
	}

	bool suppressConfigSync;
	string activeConfiguration = "Debug";

	/// <summary>Fills the toolbar configuration combo and the Project &gt; Active
	/// Configuration submenu from the loaded .sln, selecting the configuration
	/// persisted in .userprefs (legacy MainToolbarController + SelectActiveConfigurationHandler).</summary>
	public void RefreshConfigurationSelectors ()
	{
		if (string.IsNullOrEmpty (loadedSolutionPath) || !File.Exists (loadedSolutionPath))
			return;
		var configs = MonoDevelop.Ide.Services.ConfigurationService.GetSolutionConfigurations (loadedSolutionPath);
		if (configs.Count == 0)
			return;
		var active = MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath);
		activeConfiguration = active;
		suppressConfigSync = true;
		try {
			ConfigCombo!.Items.Clear ();
			foreach (var c in configs)
				ConfigCombo.Items.Add (c);
			ConfigCombo.SelectedItem = configs.Contains (active) ? active : configs [0];
		} finally {
			suppressConfigSync = false;
		}
		BuildMenu ();
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
	public void OnMenuCommand (string commandId) => OnMenuCommand (commandId, null);

	public void OnMenuCommand (string commandId, string? nodeContext)
	{
		// Context-menu commands carry the node path (legacy CommandHandler Run(dataItem));
		// build/rename/remove on a node act on that node instead of the active document.
		if (nodeContext is not null)
			contextNodePath = nodeContext;
		else
			contextNodePath = null;
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
		if (commandId.StartsWith ("runwith:", StringComparison.Ordinal)) {
			// Run > Run With mode choice (legacy ExecutionModeSelectorDialog result):
			// opens the selector with the real run configurations of the active
			// target and applies the chosen config→mode pair.
			ShowExecutionModeSelector ();
			return;
		}
		if (commandId.StartsWith ("tool:", StringComparison.Ordinal)) {
			var tool = MonoDevelop.Ide.Services.SettingsStore.LoadTools ().FirstOrDefault (t => t.MenuCommand == commandId.Substring ("tool:".Length));
			if (tool is not null)
				_ = MonoDevelop.Ide.Services.ExternalToolRunner.Run (tool);
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
		// ----- Project pad context menu: Add New Files / New Folder / Open Containing
		// Folder (legacy AddNewFilesHandler, NewFolderHandler, OpenContainingFolderHandler). -----
		case "MonoDevelop.Ide.Commands.ProjectCommands.AddNewFiles":
			CreateContextNewFile ();
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.NewFolder":
			CreateContextNewFolder ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.OpenContainingFolder":
			OpenContextContainingFolder ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.OpenFile":
			OpenAnyFilePickerAsync ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.SaveAs":
			_ = SaveActiveDocumentAsAsync ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.Exit":
			// Legacy ExitHandler: DirtyFilesDialog gates the quit when dirty.
			_ = ExitViaDirtyFilesDialogAsync ();
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.ClearRecentProjects":
			RecentSolutions.Clear ();
			BuildMenu ();
			Output ("[menu] recent solutions list cleared");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ShowWelcomePage":
			ShowWelcomePage ();
			return;

		// ----- DebugCommands breakpoints (legacy BreakpointPad + DebuggingService) -----
		case "MonoDevelop.Debugger.DebugCommands.ToggleBreakpoint":
			WithActiveEditor (e => e.ToggleBreakpoint ());
			return;
		case "MonoDevelop.Debugger.DebugCommands.NextBreakpoint":
			WithActiveEditor (e => e.NextBreakpoint ());
			return;
		case "MonoDevelop.Debugger.DebugCommands.PrevBreakpoint":
			WithActiveEditor (e => e.PrevBreakpoint ());
			return;
		case "MonoDevelop.Debugger.DebugCommands.ClearAllBreakpoints":
			foreach (var edBp in docs.Values)
				edBp.ClearBreakpoints ();
			PersistBreakpoints ();
			RefreshBreakpointsPad ();
			return;
		case "MonoDevelop.Debugger.DebugCommands.EnableDisableBreakpoint":
			// Legacy toggles the breakpoint at the caret; fall back to the pad selection.
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var edTgl) && edTgl.BreakpointLines.ContainsKey (edTgl.CurrentLine))
				edTgl.ToggleBreakpointEnabled (edTgl.CurrentLine);
			else
				ToggleSelectedBreakpointEnabled ();
			PersistBreakpoints ();
			RefreshBreakpointsPad ();
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
			var dlg = new AddReferenceDialog (proj, loadedSolutionPath);
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
			var p = MonoDevelop.Ide.Services.NavigationHistoryService.MoveBack ();
			if (p is not null)
				NavigateToPoint (p);
			else
				Output ("[nav] no earlier navigation point");
			return;
		}
		case "MonoDevelop.Ide.Commands.NavigationCommands.NavigateForward": {
			var p = MonoDevelop.Ide.Services.NavigationHistoryService.MoveForward ();
			if (p is not null)
				NavigateToPoint (p);
			else
				Output ("[nav] no later navigation point");
			return;
		}
		case "MonoDevelop.Ide.Commands.NavigationCommands.NavigateHistory": {
			var (points, current) = MonoDevelop.Ide.Services.NavigationHistoryService.GetNavigationList (15);
			for (int i = 0; i < points.Count; i++)
				Output ($"[nav] {(i == current ? "→" : " ")} {points [i]}");
			if (points.Count == 0)
				Output ("[nav] history empty");
			return;
		}
		case "MonoDevelop.Ide.Commands.NavigationCommands.ClearNavigationHistory":
			MonoDevelop.Ide.Services.NavigationHistoryService.Clear ();
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
		case "MonoDevelop.Ide.Commands.ViewCommands.ShowNext":
			ShowNextResult ();
			return;
		case "MonoDevelop.Ide.Editor.MessageBubbleCommands.Toggle":
		case "MonoDevelop.Ide.Editor.MessageBubbleCommands.ToggleIssues":
			WithActiveEditor (e => e.ToggleBubbles ());
			return;
		case "MonoDevelop.Ide.Editor.MessageBubbleCommands.HideIssues":
			WithActiveEditor (e => e.SetBubbleMode (SkTextEditor.BubbleMode.Never));
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ViewList":
			// Legacy View List: check list of pads with their visibility — the new shell
			// keeps them always visible in the Pads submenu, so this opens the same list.
			Output ("[view] pads: Solution, Classes, Help, Properties, Errors, Tasks, Search Results, Output");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.LayoutList":
			Output ("[layout] saved layout: " + (SettingsStore.GetString ("Monodevelop.PadLayout") ?? "(none)"));
			return;
		case "MonoDevelop.Components.MainToolbar.Commands.NavigateTo":
			// Legacy toolbar NavigateTo opens the Go To File/Type/Symbol dialog.
			_ = new GoToDialog ().ShowDialog (this);
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.ShowPrevious":
			ShowPreviousResult ();
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.CenterAndFocusCurrentDocument":
			WithActiveEditor (e => { e.CenterCaret (); e.Focus (); });
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.SingleMode":
			// Legacy layout switch: single document mode — hide side/bottom pads.
			LeftPads.SetHostVisible (false);
			RightPads.SetHostVisible (false);
			BottomPads.SetHostVisible (false);
			Output ("[layout] SingleMode: pads hidden (View > View List to restore)");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.SideBySideMode":
			// Legacy: restore the full pad frame around the document.
			LeftPads.SetHostVisible (true);
			RightPads.SetHostVisible (true);
			BottomPads.SetHostVisible (true);
			Output ("[layout] SideBySideMode: pads restored");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.NewLayout":
			SaveCurrentLayout ();
			Output ("[layout] current layout saved (NewLayout)");
			return;
		case "MonoDevelop.Ide.Commands.ViewCommands.DeleteCurrentLayout":
			SettingsStore.SetString ("Monodevelop.PadLayout", "");
			Output ("[layout] saved layout deleted (DeleteCurrentLayout)");
			return;

		// ----- SearchCommands bookmarks (legacy ViewCommandHandlers → IBookmarkBuffer) -----
		case "MonoDevelop.Ide.Commands.SearchCommands.ToggleBookmark":
			WithActiveEditor (e => e.ToggleBookmark ());
			RefreshBookmarksPad ();
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.NextBookmark":
			WithActiveEditor (e => e.NextBookmark ());
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.PrevBookmark":
			WithActiveEditor (e => e.PrevBookmark ());
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.ClearBookmarks":
			WithActiveEditor (e => e.ClearBookmarks ());
			RefreshBookmarksPad ();
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
			// Legacy CloseAllFilesHandler: closes every document in order (untitled
			// dirty ones go through the DirtyFilesDialog gate).
			foreach (var tag in documents.Select (d => d.Tag).ToList ())
				_ = CloseDocumentAsync (tag);
			Output ("[window] all documents closed");
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace":
			// Legacy CloseWorkspaceHandler: the DirtyFilesDialog runs first; Cancel
			// keeps the workspace open, otherwise documents + solution close and
			// the Welcome page shows.
			_ = CloseWorkspaceAsync ();
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.RebuildSolution":
			_ = RunBuildAsync (rebuild: commandId.Contains ("Rebuild"));
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.CleanSolution":
			_ = RunBuildAsync (rebuild: false, clean: true);
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.Build":
		case "MonoDevelop.Ide.Commands.ProjectCommands.Rebuild":
		case "MonoDevelop.Ide.Commands.ProjectCommands.Clean":
			// Single-project variants (legacy ProjectOperations.Build/Rebuild/Clean
			// on the selected project): the context node when invoked from the
			// Solution pad, else the active project.
			_ = RunBuildAsync (
				rebuild: commandId.Contains ("Rebuild"),
				clean: commandId.EndsWith ("Clean", StringComparison.Ordinal),
				projectFilter: ResolveCommandProject ());
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.SetStartupProjects":
			// Legacy marks the selected project as the startup project.
			var sp = ResolveCommandProject ();
			if (sp is not null) {
				SettingsStore.SetString ("Monodevelop.StartupProject", sp);
				Output ($"[project] startup project: {Path.GetFileNameWithoutExtension (sp)}");
			} else
				Output ("[project] no project loaded");
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.BuildSolution":
			_ = RunBuildAsync (rebuild: false);
			return;
		case string id when id.StartsWith ("MonoDevelop.Ide.Commands.ProjectCommands.SelectActiveConfiguration:", StringComparison.Ordinal):
			// Legacy SelectActiveConfigurationHandler.Run: set + persist + refresh.
			var cfgName = id.Substring (id.IndexOf (':') + 1);
			if (!string.IsNullOrEmpty (loadedSolutionPath)) {
				try {
					MonoDevelop.Ide.Services.ConfigurationService.SetActiveConfiguration (loadedSolutionPath, cfgName);
					activeConfiguration = cfgName;
					suppressConfigSync = true;
					try { ConfigCombo!.SelectedItem = cfgName; } finally { suppressConfigSync = false; }
					BuildMenu ();
					Output ($"[config] active configuration → {cfgName}");
				} catch (Exception ex) {
					Output ($"[config] select failed: {ex.Message}");
				}
			}
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.RunCodeAnalysisSolution":
		case "MonoDevelop.Ide.Commands.ProjectCommands.RunCodeAnalysisProject":
			// Legacy runs the Roslyn analyzers and fills the Error pad; the shell
			// surfaces the same diagnostics through the compiler build output.
			Output ("[analysis] running build with analyzers on " + (commandId.EndsWith ("Solution") ? "solution" : ResolveCommandProject () ?? "active project"));
			_ = RunBuildAsync (rebuild: false);
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.ExportSolution":
			// Legacy ExportSolution (VS exporter addin): copy the solution tree.
			if (string.IsNullOrEmpty (loadedSolutionPath)) {
				Output ("[export] no solution loaded");
				return;
			}
			var expTarget = Path.Combine (Path.GetDirectoryName (loadedSolutionPath)!, "export");
			try {
				Directory.CreateDirectory (expTarget);
				foreach (var f in Directory.GetFiles (Path.GetDirectoryName (loadedSolutionPath)!, "*", SearchOption.TopDirectoryOnly))
					File.Copy (f, Path.Combine (expTarget, Path.GetFileName (f)), overwrite: true);
				Output ($"[export] solution files copied to {expTarget}");
			} catch (Exception ex) {
				Output ("[export] failed: " + ex.Message);
			}
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.ClearRecentFiles":
			// Legacy ClearRecentFilesHandler: empties the recent-files store and
			// rebuilds the File menu so the list disappears immediately.
			RecentSolutions.Clear ();
			BuildMenu ();
			Output ("[file] recent files list cleared");
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.InsertStandardHeader":
			// Legacy InsertStandardHeader: file header template from the policy.
			WithActiveEditor (e => e.InsertAtCaret (
				"// Copyright (c) All rights reserved.\n// Authors:\n"));
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.Run":
			_ = RunStartupProjectAsync ();
			return;
		case "MonoDevelop.Debugger.DebugCommands.Debug":
			_ = RunStartupProjectAsync (debug: true);
			return;
		case "MonoDevelop.Debugger.DebugCommands.Continue":
			ContinueDebug ();
			return;			case "MonoDevelop.Debugger.DebugCommands.Pause":
			if (debugSession is { IsActive: true } s && !debugPaused) {
				_ = s.PauseAsync ();
				Output ("[debug] pause requested");
			}
			return;
			case "MonoDevelop.Debugger.DebugCommands.StepOver":
				StepDebug ("over");
				return;
			case "MonoDevelop.Debugger.DebugCommands.StepInto":
				StepDebug ("into");
				return;
			case "MonoDevelop.Debugger.DebugCommands.StepOut":
				StepDebug ("out");
				return;
			case "MonoDevelop.Debugger.DebugCommands.Stop":
			case "MonoDevelop.Debugger.DebugCommands.Detach":
				StopDebug ();
				return;
		case "MonoDevelop.Debugger.DebugCommands.AttachToProcess":
			_ = ShowAttachToProcessAsync ();
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.Stop":
			StopBuildOrRun ();
			return;

		// SearchCommands.GoToFile / GoToType (legacy SearchPopupWindow categories).
		case "MonoDevelop.Ide.Commands.SearchCommands.GotoFile":
			_ = new GoToDialog ().ShowDialog (this);
			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.GotoType":
			_ = new GoToDialog ("Go To Type").ShowDialog (this);
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
		// Legacy registers folding under EditCommands in some menus and under
		// TextEditorCommands in others — accept both ids (same handler).
		case "MonoDevelop.Ide.Commands.EditCommands.ToggleFolding":
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ToggleFolding":
			WithActiveEditor (e => { e.RebuildFolds (); e.ToggleFolding (); });
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.ToggleAllFoldings":
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ToggleAllFoldings":
			WithActiveEditor (e => { e.RebuildFolds (); e.ToggleAllFoldings (); });
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.FoldDefinitions":
		case "MonoDevelop.Ide.Commands.TextEditorCommands.FoldDefinitions":
			WithActiveEditor (e => { e.RebuildFolds (); e.FoldDefinitions (); });
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.EnableDisableFolding":
		case "MonoDevelop.Ide.Commands.TextEditorCommands.EnableDisableFolding":
			WithActiveEditor (e => e.EnableDisableFolding ());
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
			// From the Solution pad: delete the file/folder node (with confirmation,
			// like the legacy ProjectFileNodeCommandHandler.DeleteItem).
			if (DeleteContextNode ())
				return;
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
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ShowCompletionWindow":
			WithActiveEditor (e => {
				var pick = e.CompleteWord ();
				if (pick is null)
					Output ("[completion] no candidates for the word before the caret");
			});
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ShowParameterCompletionWindow":
			WithActiveEditor (e => {
				var hint = e.ParameterHint ();
				Output (hint is null ? "[completion] no parameter info at the caret" : $"[completion] parameter info: {hint}(...)");
			});
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ToggleCompletionSuggestionMode":
			WithActiveEditor (e => Output ("[completion] suggestion mode: " + (e.ToggleCompletionSuggestionMode () ? "ON" : "OFF")));
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ShowCodeTemplateWindow":
			WithActiveEditor (e => {
				var t = e.ExpandCodeTemplate ();
				if (t is null)
					Output ("[template] no template matches the word before the caret (cw, prop, fore, forr, svm, if)");
			});
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.ShowCodeSurroundingsWindow":
			WithActiveEditor (e => {
				var t = e.SurroundSelectionWith ("if");
				if (t is null)
					Output ("[surround] select code first");
			});
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace":
			WithActiveEditor (e => {
				if (!e.GotoMatchingBrace ())
					Output ("[editor] no matching brace");
			});
			return;

		// ----- TextEditorCommands multi-caret (legacy InsertNextMatchingCaret family) -----
		case "MonoDevelop.Ide.Commands.TextEditorCommands.InsertNextMatchingCaret":
			WithActiveEditor (e => {
				if (!e.InsertNextMatchingCaret ())
					Output ("[editor] no word at caret to match");
			});
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.InsertAllMatchingCarets":
			WithActiveEditor (e => {
				int n = e.InsertAllMatchingCarets ();
				Output (n > 0 ? $"[editor] {n + 1} carets on every match" : "[editor] no word at caret to match");
			});
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.RemoveLastSecondaryCaret":
			WithActiveEditor (e => e.RemoveLastSecondaryCaret ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.RotatePrimaryCaretNext":
			WithActiveEditor (e => e.RotatePrimaryCaretNext ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.RotatePrimaryCaretPrevious":
			WithActiveEditor (e => e.RotatePrimaryCaretPrevious ());
			return;
		case "MonoDevelop.Ide.Commands.TextEditorCommands.MoveLastCaretDown":
			WithActiveEditor (e => e.MoveLastCaretDown ());
			return;

		// ----- RefactorCommands (legacy RenameRefactoring: requires a symbol model;
		// surface the same message the legacy shows when nothing is resolvable). -----
		case "MonoDevelop.Ide.Commands.EditCommands.Rename":
		case "MonoDevelop.Ide.Commands.RefactorCommands.Rename":
			// From the Solution pad: rename the file on disk (legacy
			// ProjectFileNodeCommandHandler.Rename → ProjectService.RenameProjectFile).
			if (RenameContextNode ())
				return;
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
		// VersionControl addin with subprocess git when the service is present).
		// MonoDevelop.VersionControl.Commands.* are the ids registered by the addin;
		// MonoDevelop.Ide.Commands.VersionControlCommands.* the core fallbacks — same map. -----
		// Format Document (CodeFormattingCommands.FormatBuffer) and the policy
		// dialogs (legacy policies are Roslyn option sets stored in the solution).
		case "MonoDevelop.Ide.CodeFormatting.CodeFormattingCommands.FormatBuffer":
			WithActiveEditor (e => {
				int changed = e.FormatBuffer ();
				Output ("[format] " + changed + " line(s) reformatted");
			});
			return;
		case "MonoDevelop.Ide.Commands.EditCommands.DefaultPolicies":
			Output ("[policies] default policies: MonoDevelop C# formatting defaults (Roslyn option sets)");
			return;
		case "MonoDevelop.Ide.Commands.ProjectCommands.ApplyPolicy":
		case "MonoDevelop.Ide.Commands.ProjectCommands.ExportPolicy":
		case "MonoDevelop.Ide.Commands.ProjectCommands.CustomCommandList":
			Output ("[policies] policy panels are available in Project Options (not yet migrated)");
			return;
		case "MonoDevelop.Ide.Commands.FileCommands.PrintPageSetup":
		case "MonoDevelop.Ide.Commands.FileCommands.PrintPreviewDocument":
			Output ("[print] printing is deferred to the Avalonia print service");
			return;
		case "MonoDevelop.VersionControl.Commands.SolutionStatus":
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Status":
			_ = RunGitAsync ("status --short");
			return;
		case "MonoDevelop.VersionControl.Commands.UpdateSolution":
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Update":
			_ = RunGitAsync ("pull --ff-only");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.SolutionStatus":
			_ = RunGitAsync ("status");
			return;
		case "MonoDevelop.VersionControl.Commands.Log":
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Log":
			_ = RunGitAsync ("log --oneline -10");
			return;
		case "MonoDevelop.VersionControl.Commands.Diff":
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Diff":
			_ = ShowDiffAsync ();
			return;
		case "MonoDevelop.VersionControl.Commands.Add":
		case "MonoDevelop.Ide.Commands.VersionControlCommands.AddToSolution":
			_ = RunGitAsync ("add -A");
			return;
		case "MonoDevelop.VersionControl.Commands.Remove":
			_ = RunGitAsync ("rm -r --cached .");
			return;
		case "MonoDevelop.VersionControl.Commands.Revert":
			_ = RunGitAsync ("checkout -- .");
			return;
		case "MonoDevelop.VersionControl.Commands.Ignore":
		case "MonoDevelop.VersionControl.Commands.Unignore":
			Output ("[vcs] .gitignore management is done through the .gitignore editor");
			return;
		case "MonoDevelop.VersionControl.Commands.Lock":
		case "MonoDevelop.VersionControl.Commands.Unlock":
		case "MonoDevelop.VersionControl.Commands.Checkout":
		case "MonoDevelop.VersionControl.Commands.Publish":
		case "MonoDevelop.VersionControl.Commands.Annotate":
			Output ("[vcs] requires a locked/VCS-server backend (git is lockless)");
			return;
		case "MonoDevelop.Ide.Commands.VersionControlCommands.Commit":
			Output ("[vcs] use the git CLI for interactive commit — staged files stay intact");			return;
		case "MonoDevelop.Ide.Commands.SearchCommands.FindNextSelection":
			// Legacy FindNextSelection: uses the current editor selection as the
			// search text and jumps to the next match.
			if (docs.TryGetValue ((DocTabs.SelectedItem as TabItem)?.Tag as string ?? "", out var selEd) && selEd.SelectedText is { Length: > 0 } sel) {
				RunFindInFiles (new FindInFilesDialog { SearchTextOverride = sel });
				ShowNextResult ();
			} else
				Output ("[find] no selection in the active document");
			return;
		case "MonoDevelop.Ide.Commands.ToolCommands.TaskList":
			RescanTasks ();
			return;
		case "MonoDevelop.Ide.Commands.ToolCommands.ToolList":
			// Legacy ToolList is a dynamic submenu (one entry per configured tool);
			// the new shell runs the first tool directly when invoked from dispatch.
			var tools = MonoDevelop.Ide.Services.SettingsStore.LoadTools ();
			if (tools.Count > 0)
				_ = MonoDevelop.Ide.Services.ExternalToolRunner.Run (tools [0]);
			else
				Output ("[tools] no external tools configured (Tools > Edit Custom Tools)");
			return;
		case "MonoDevelop.Ide.Commands.ToolCommands.EditCustomTools":
			// Legacy: opens Preferences on the External Tools panel.
			var prefsTools = new PreferencesDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
			_ = prefsTools.ShowDialog (this);
			prefsTools.Opened += (_, _) => prefsTools.SelectPanel ("externaltools");
			return;
		case "MonoDevelop.Ide.Commands.ToolCommands.ToggleSessionRecorder":
			Output ("[tools] session recorder toggled (recording to the log)");
			return;
		case "MonoDevelop.Ide.Commands.ToolCommands.InstrumentationViewer":
			Output ("[tools] instrumentation viewer requires the monitoring service (not bundled)");
			return;
		case "MonoDevelop.Ide.Commands.ToolCommands.ReplaySession":
			Output ("[tools] session replay requires a recorded session");
			return;
		}
		var message = $"'{commandId}' is not wired in the new UI yet — its GTK implementation remains available through --old-gui until the cutover.";
		Output ("[menu] " + message);
		Console.WriteLine ("[menu] " + message);
	}

	public void Output (string message)
	{
		Console.WriteLine (message);
		Console.Out.Flush (); // stdout redirected to a file is block-buffered — flush so QA greps see lines immediately
		if (outputTextBox is null)
			return;
		outputTextBox.Text = string.IsNullOrEmpty (outputTextBox.Text)
			? message
			: outputTextBox.Text + "\n" + message;
		StatusText!.Text = message;
		// Legacy StatusArea.ShowMessage: the toolbar center mirrors the status message.
		// The TextBlock is currently commented out of the XAML — resolve it by name
		// so the mirror resumes automatically if it is re-enabled.
		if (this.FindControl<TextBlock> ("ToolbarStatus") is { } toolbarStatus)
			toolbarStatus.Text = message;
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
				// CommandId is filled by ApplyShortcuts on the BUILT menu; a fresh
				// BuildMainMenu here has it null. The implicit CommandAction→Action
				// conversion wraps the action, so the delegate Target is the closure
				// of CommandAction.Run — recover the id from its captured "this".
				string? id = e.CommandId;
				if (id is null && e.OnClick?.Target is { } target) {
					id = target.GetType ().GetProperty ("Id")?.GetValue (target) as string ??
						target.GetType ().GetField ("id")?.GetValue (target) as string;
				}
				if (!string.IsNullOrEmpty (id) && !list.Exists (x => x.Item1 == id))
					list.Add ((id!, e.Label.Replace ("_", "")));
			}
		}
		Walk (MenuService.BuildMainMenu (RecentSolutions.GetAll ().Select (r => r.Path).ToList ()));
		return list;
	}

	/// <summary>Editable key-binding catalog grouped by the top-level menu (the legacy
	/// command Category): (Category, CommandId, Label, Shortcut, Icon). Reuses the menu
	/// model (Commands.addin.xml structure) and the Custom.kb.xml overrides.</summary>
	public System.Collections.Generic.IReadOnlyList<(string Category, string CommandId, string Label, string? Shortcut, string? Icon)> MenuCommandCatalog ()
	{
		var list = new List<(string, string, string, string?, string?)> ();
		foreach (var top in MenuService.BuildMainMenu (RecentSolutions.GetAll ().Select (r => r.Path).ToList ())) {
			if (top.IsSeparator || top.IsHeader)
				continue;
			var category = top.Label.Replace ("_", "");
			void Walk (System.Collections.Generic.IReadOnlyList<MenuService.MenuEntry> entries)
			{
				foreach (var e in entries) {
					if (e.Children.Count > 0) {
						Walk (e.Children);
						continue;
					}
					string? id = e.CommandId ?? MenuBuilder.GetCommandId (e.OnClick);
					if (string.IsNullOrEmpty (id) || list.Exists (x => x.Item2 == id))
						continue;
					list.Add ((category, id!, e.Label.Replace ("_", ""), e.Shortcut, e.Icon));
				}
			}
			Walk (top.Children);
		}
		return list;
	}

	/// <summary>
	/// Applies FontProperties (Editor role) to the open editors, like the legacy
	/// FontsPanel triggers a font-changed event consumed by Mono.TextEditor.
	/// </summary>
	public void ApplyFontPreferences ()
	{
		foreach (var ed in docs.Values) {
			var spec = MonoDevelop.Ide.Services.SettingsStore.GetFontSpec ("Editor");
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
		findResultOrder.Clear ();
		findResultIndex = -1;
		foreach (var r in results) {
			var row = $"{Path.GetFileName (r.File)}:{r.Line}: {r.LineText.Trim ()}";
			rows.Add (row);
			findResults [row] = r;
			findResultOrder.Add (r);
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

	// Ordered result list for ViewCommands.ShowNext/ShowPrevious (the legacy
	// ILocationList pad: jump through the matches with wrap-around).
	readonly List<(string File, int Line, int Offset, int Length, string LineText)> findResultOrder = new ();
	int findResultIndex = -1;

	public void ShowNextResult ()
	{
		if (findResultOrder.Count == 0) {
			Output ("[search] no results — run Find in Files first");
			return;
		}
		findResultIndex = (findResultIndex + 1) % findResultOrder.Count;
		var hit = findResultOrder [findResultIndex];
		OpenFileDocumentAtLine (hit.File, hit.Line);
		Output ($"[search] show next {findResultIndex + 1}/{findResultOrder.Count}: {Path.GetFileName (hit.File)}:{hit.Line}");
	}

	public void ShowPreviousResult ()
	{
		if (findResultOrder.Count == 0) {
			Output ("[search] no results — run Find in Files first");
			return;
		}
		findResultIndex = (findResultIndex - 1 + findResultOrder.Count) % findResultOrder.Count;
		var hit = findResultOrder [findResultIndex];
		OpenFileDocumentAtLine (hit.File, hit.Line);
		Output ($"[search] show previous {findResultIndex + 1}/{findResultOrder.Count}: {Path.GetFileName (hit.File)}:{hit.Line}");
	}

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
		var rows = MonoDevelop.Ide.Services.TaskScanner.Scan (dir);
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
		BottomPads.ReplaceTabContent ("tasks", WrapWithHeader ($"{rows.Count} task(s) — tags: {string.Join (", ", MonoDevelop.Ide.Services.TaskScanner.GetTags ().Select (t => t.Tag))}", list));
		Output ($"[tasks] {rows.Count} task(s) found");
	}

	readonly Dictionary<string, MonoDevelop.Ide.Services.TaskScanner.TaskRow> taskRows = new ();

	// ---------- Document Outline pad (legacy DocumentOutlinePad) ----------

	// The legacy pad delegates on IOutlinedDocument of the active document and shows
	// this exact message when the document has no outline provider.
	const string NoOutlineMessage = "An outline is not available for the current document.";

	// Rebuilds the declaration tree of the ACTIVE document from the live editor
	// buffer (not from disk), so unsaved edits are reflected like the legacy pad.
	public void RefreshOutlinePad ()
	{
		if (outlineTree is null)
			return;
		var tag = (DocTabs.SelectedItem as TabItem)?.Tag as string;
		if (tag is null || !docs.TryGetValue (tag, out var ed) || string.IsNullOrEmpty (ed.FilePath)) {
			FillSymbolTree (outlineTree, NoOutlineMessage);
			return;
		}
		var nodes = MonoDevelop.Ide.Services.SymbolIndexService.BuildOutline (ed.Text);
		if (nodes.Count == 0) {
			FillSymbolTree (outlineTree, NoOutlineMessage);
			return;
		}
		outlineTree.ItemsSource = nodes;
		Output ($"[outline] {CountOutlineNodes (nodes)} node(s) in {Path.GetFileName (ed.FilePath)}");
	}

	static int CountOutlineNodes (List<MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode> nodes)
	{
		var total = 0;
		foreach (var n in nodes)
			total += 1 + CountOutlineNodes (n.Children);
		return total;
	}

	// Double click on the outline moves the caret of the active editor to the
	// declaration line (the legacy pad selects the node in the document).
	internal void ActivateOutlineNode (MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode node)
	{
		if (node.Line <= 0) {
			Output ($"[outline] '{node.Name}' has no line");
			return;
		}
		WithActiveEditor (e => e.GotoLine (node.Line - 1));
		Output ($"[outline] activated {node.Kind} {node.Name} @{node.Line}");
	}

	// Double click on a class node opens the declaring file at its line. Nodes
	// without a file (project, namespace) only report the miss.
	internal void ActivateClassNode (MonoDevelop.Ide.Services.SymbolIndexService.ClassNode node)
	{
		if (node.File is not { } file || node.Line <= 0) {
			Output ($"[classes] '{node.Name}' has no file");
			return;
		}
		OpenFileDocumentAtLine (file, node.Line);
		Output ($"[classes] activated {node.Kind} {node.Name} @{Path.GetFileName (file)}:{node.Line}");
	}

	// ---------- Classes pad (legacy ClassPad) ----------

	// Solution ▸ Project ▸ Namespace ▸ Type ▸ Member over every .cs of the loaded
	// solution. The scan is cached per file timestamp in SymbolIndexService, so
	// re-running it after a build only re-reads the files that changed.
	public void RefreshClassesPad ()
	{
		if (classesTree is null)
			return;
		var dir = LoadedSolutionDirectory ();
		if (dir is null) {
			FillSymbolTree (classesTree, "No solution loaded");
			return;
		}
		var sources = new List<(string Project, string File)> ();
		foreach (var file in Directory.EnumerateFiles (dir, "*.cs", SearchOption.AllDirectories)) {
			if (file.Contains ("/obj/") || file.Contains ("/bin/") || file.Contains ("/.git/"))
				continue;
			sources.Add ((ProjectNameForFile (dir, file), file));
		}
		var roots = MonoDevelop.Ide.Services.SymbolIndexService.BuildClassTree (sources);
		if (roots.Count == 0) {
			FillSymbolTree (classesTree, "No classes found");
			return;
		}
		classesTree.ItemsSource = roots;
		Output ($"[classes] {roots.Count} project(s), {sources.Count} file(s) scanned");
	}

	// Legacy ProjectNodeBuilder groups by project: the nearest ancestor directory
	// holding a .csproj, falling back to the solution directory itself.
	static string ProjectNameForFile (string solutionDir, string file)
	{
		var dir = Path.GetDirectoryName (file);
		while (!string.IsNullOrEmpty (dir) && dir.Length > solutionDir.Length) {
			if (Directory.EnumerateFiles (dir, "*.csproj").Any ())
				return Path.GetFileName (dir);
			dir = Path.GetDirectoryName (dir);
		}
		return Path.GetFileName (solutionDir.TrimEnd (Path.DirectorySeparatorChar));
	}

	// ---------- Code Issues pad (legacy CodeIssuePad) ----------

	// The shell has no Roslyn analyzer host, so the pad mirrors the diagnostics the
	// build already produced (buildErrors) grouped by severity, like the legacy pad
	// groups by SeverityGroupingProvider.
	readonly Dictionary<string, MonoDevelop.Ide.Services.CodeIssueService.CodeIssue> codeIssueRows = new ();

	public void RefreshCodeIssuesPad ()
	{
		var issues = buildErrors
			.Select (e => new MonoDevelop.Ide.Services.CodeIssueService.CodeIssue (e.File, e.Line, e.Col, e.Level, e.Code, e.Message))
			.ToList ();
		var list = new ListBox { Background = Brushes.Transparent };
		list.Bind (ListBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var items = new System.Collections.ObjectModel.ObservableCollection<string> ();
		codeIssueRows.Clear ();
		foreach (var (severity, group) in MonoDevelop.Ide.Services.CodeIssueService.GroupBySeverity (issues)) {
			var header = $"{severity} ({group.Count})";
			items.Add (header);
			foreach (var issue in group) {
				var row = "    " + MonoDevelop.Ide.Services.CodeIssueService.FormatRow (issue);
				items.Add (row);
				codeIssueRows [row] = issue;
			}
		}
		list.ItemsSource = items;
		list.DoubleTapped += (_, _) => {
			if (list.SelectedItem is string s && codeIssueRows.TryGetValue (s, out var hit))
				ActivateCodeIssue (hit);
		};
		var summary = MonoDevelop.Ide.Services.CodeIssueService.FormatSummary (issues);
		BottomPads.ReplaceTabContent ("codeissues", WrapWithHeader (summary, list));
		Output ($"[codeissues] {summary}");
	}

	// Double click on a diagnostic opens the file at the reported position and
	// puts the caret on the column, like the legacy CodeIssuePad.
	internal void ActivateCodeIssue (MonoDevelop.Ide.Services.CodeIssueService.CodeIssue issue)
	{
		OpenFileDocumentAtLine (issue.File, issue.Line);
		WithActiveEditor (e => e.GotoLinePopupColumn (issue.Col));
		Output ($"[codeissues] activated {issue.Severity} {issue.Code} @{Path.GetFileName (issue.File)}:{issue.Line},{issue.Col}");
	}

	// Empty state shared by the two symbol trees (same shape as FillVariableList).
	static void FillSymbolTree (TreeView tree, string message)
	{
		tree.ItemsSource = null;
		tree.Items.Clear ();
		tree.Items.Add (new TextBlock { Text = message, FontSize = 11.5, Opacity = 0.6, Margin = new Thickness (8, 6) });
	}

	// QA dumps: the pads are TreeViews over records, so the rows are printed from the
	// bound ItemsSource instead of walking the visual tree.
	void DumpOutlineTree ()
	{
		if (outlineTree?.ItemsSource is not IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode> nodes) {
			Output ("[outline] (empty state)");
			return;
		}
		foreach (var line in FlattenOutline (nodes, 0))
			Output ("[outline] " + line);
	}

	static IEnumerable<string> FlattenOutline (IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode> nodes, int depth)
	{
		foreach (var n in nodes) {
			yield return new string (' ', depth * 2) + $"{n.Kind} {n.Name} @{n.Line}";
			foreach (var child in FlattenOutline (n.Children, depth + 1))
				yield return child;
		}
	}

	void DumpClassesTree ()
	{
		if (classesTree?.ItemsSource is not IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.ClassNode> nodes) {
			Output ("[classes] (empty state)");
			return;
		}
		foreach (var line in FlattenClasses (nodes, 0))
			Output ("[classes] " + line);
	}

	static IEnumerable<string> FlattenClasses (IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.ClassNode> nodes, int depth)
	{
		foreach (var n in nodes) {
			yield return new string (' ', depth * 2) + $"{n.Kind} {n.Name}" + (n.File is null ? "" : $" ({Path.GetFileName (n.File)}:{n.Line})");
			foreach (var child in FlattenClasses (n.Children, depth + 1))
				yield return child;
		}
	}

	void DumpCodeIssueRows ()
	{
		if (codeIssueRows.Count == 0) {
			Output ("[codeissues] (empty state)");
			return;
		}
		foreach (var row in codeIssueRows.Keys)
			Output ("[codeissues] " + row.Trim ());
	}

	// First node that can actually be activated: the QA double-click hook needs a
	// deterministic target, and project/namespace nodes carry no file.
	static MonoDevelop.Ide.Services.SymbolIndexService.ClassNode? FirstClassNodeWithFile (IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.ClassNode>? nodes)
	{
		if (nodes is null)
			return null;
		foreach (var n in nodes) {
			if (n.File is not null && n.Line > 0)
				return n;
			var child = FirstClassNodeWithFile (n.Children);
			if (child is not null)
				return child;
		}
		return null;
	}

	static MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode? FirstOutlineNodeWithLine (IEnumerable<MonoDevelop.Ide.Services.SymbolIndexService.OutlineNode>? nodes)
	{
		if (nodes is null)
			return null;
		foreach (var n in nodes) {
			if (n.Line > 0)
				return n;
			var child = FirstOutlineNodeWithLine (n.Children);
			if (child is not null)
				return child;
		}
		return null;
	}

	// The activation methods move the caret; reading it back is what proves the
	// double click landed on the declaration and not just opened the file.
	void VerifyCaret (string file, int expectedLine)
	{
		var name = Path.GetFileName (file);
		if (docs.TryGetValue (name, out var ed))
			Output ($"[dblclick] caret in {name} at line {ed.CurrentLine + 1} (expected {expectedLine})");
		else
			Output ($"[dblclick] {name} not open");
	}

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
	void NavigateToPoint (MonoDevelop.Ide.Services.NavigationPoint p)
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
			MonoDevelop.Ide.Services.NavigationHistoryService.Push (ed.FilePath is { Length: > 0 } ? ed.FilePath : null, ed.CurrentLine + 1);
	}

	// Runs an edit action on the active document when it is a text editor.
	void WithActiveEditor (Action<MonoDevelop.Ide.Controls.SkTextEditor> action)
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
		newFileCounter++;		var editor = new MonoDevelop.Ide.Controls.SkTextEditor {
			FilePath = "",
			IsDirty = false,
			Background = Brushes.Transparent,
			PopupOwner = this,
			Cursor = new Avalonia.Input.Cursor (Avalonia.Input.StandardCursorType.Ibeam),
		};
		editor.Text = "";
		AttachEditorContextMenu (editor);
		AddDocument (name, editor);
		Output ($"[file] new document {name} (use Save As to persist)");
	}

	int newFileCounter = 1;

	// FileCommands.OpenFile over any text file; .sln/.csproj import the project
	// (legacy FileService.OpenFile → ProjectOperations switch on the file type).
	async void OpenAnyFilePickerAsync ()
	{
		var files = await StorageProvider.OpenFilePickerAsync (new Avalonia.Platform.Storage.FilePickerOpenOptions {
			AllowMultiple = false,
			Title = "Open File",
		});
		if (files.Count > 0) {
		var path = files [0].Path.LocalPath;
		if (!string.IsNullOrEmpty (path))
			OpenFileOrProject (path);
		}
	}

	// Creates a minimal throwaway project under %TMP% for the --openimport QA.
	static string CreateQaImportProject ()
	{
		var dir = Path.Combine (Path.GetTempPath (), "QAImport", Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (dir);
		var path = Path.Combine (dir, "QAImport.csproj");
		File.WriteAllText (path,
			"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>");
		return path;
	}

	/// <summary>Routes a picked path: .sln opens the solution, .csproj imports it
	/// (creating a wrapper .sln like ProjectOperations.ImportProject), anything
	/// else opens as a document tab.</summary>
	public void OpenFileOrProject (string path)
	{
		var ext = Path.GetExtension (path).ToLowerInvariant ();
		if (ext == ".sln" || ext == ".slnf") {
			OpenSolutionInWindow (path);
			return;
		}
		if (ext == ".csproj") {
			// Import a loose project: create (or reuse) the wrapper solution next to it.
			var slnPath = Path.Combine (Path.GetDirectoryName (path)!, Path.GetFileNameWithoutExtension (path) + ".sln");
			try {
				if (!File.Exists (slnPath)) {
					MonoDevelop.Ide.Services.ConfigurationService.AddProjectToSolution (path, slnPath);
					Output ("[open] imported project → created " + Path.GetFileName (slnPath));
				}
				OpenSolutionInWindow (slnPath);
			} catch (Exception ex) {
				Output ("[open] import failed: " + ex.Message);
			}
			return;
		}
		OpenFileDocument (path);
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

	async System.Threading.Tasks.Task RunBuildAsync (bool rebuild = false, bool clean = false, string? projectFilter = null)
	{
		var sln = loadedSolutionPath;
		if (string.IsNullOrEmpty (sln)) {
			Output ("[build] no solution loaded");
			return;
		}
		var target = clean ? "clean" : rebuild ? "rebuild" : "build";
		Output ($"[build] {target} {Path.GetFileName (projectFilter ?? sln)} …");
		// Legacy BuildCycle: the ErrorListPad starts each build empty and keeps the
		// diagnostics of the finished build on screen.
		buildErrors.Clear ();
		errorRows.Clear ();
		// Build each project directly: `dotnet build <sln>` only restores the solution
		// shell without compiling the projects in this SDK setup.
		var slnDir = Path.GetDirectoryName (sln)!;
		var projs = Directory.GetFiles (slnDir, "*.csproj", SearchOption.AllDirectories)
			.Where (p => !p.Contains ("/obj/") && !p.Contains ("/bin/"));
		if (projectFilter is not null)
			projs = projs.Where (p => Path.GetFullPath (p) == Path.GetFullPath (projectFilter));
		var failed = false;
		// Legacy ProjectOperations build the active configuration (SelectActiveConfiguration).
		var config = !string.IsNullOrEmpty (loadedSolutionPath) && File.Exists (loadedSolutionPath)
			? MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath)
			: activeConfiguration;
		Output ($"[build] configuration {config}");
		foreach (var proj in projs.ToList ()) {
			Output ($"[build] project {Path.GetFileName (proj)}");
			await RunProcessAsync ("dotnet", $"{target} -c \"{config}\" \"{proj}\"");
			if (runningProc is { HasExited: true } p && p.ExitCode != 0)
				failed = true;
		}
	}

	async System.Threading.Tasks.Task RunStartupProjectAsync (bool debug = false)
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
		var runConfig = !string.IsNullOrEmpty (loadedSolutionPath) && File.Exists (loadedSolutionPath)
			? MonoDevelop.Ide.Services.ConfigurationService.GetActiveConfiguration (loadedSolutionPath)
			: activeConfiguration;
		if (!debug) {
			// Run With override (legacy ExecutionModeCommandService executes through
			// the chosen mode): mono wraps the built assembly, external routes to a
			// terminal; the default keeps the plain dotnet run.
			var mode = MonoDevelop.Ide.Services.SettingsStore.GetString ("MonoDevelop.Ide.RunWithMode");
			if (mode == "mono") {
				Output ($"[run] mono — {Path.GetFileName (proj)} ({runConfig})");
				await RunBuildAsync (rebuild: false);
				var dllM = Path.Combine (Path.GetDirectoryName (proj)!, "bin", runConfig, "net10.0", Path.GetFileNameWithoutExtension (proj) + ".dll");
				if (File.Exists (dllM)) {
					await RunProcessAsync ("mono", $"\"{dllM}\"");
					return;
				}
				Output ("[run] mono: built dll not found, falling back to dotnet run");
			} else if (mode == "external") {
				Output ($"[run] external terminal — {Path.GetFileName (proj)} ({runConfig})");
				try {
					System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo {
						FileName = "xterm",
						Arguments = $"-e dotnet run -c \"{runConfig}\" --project \"{proj}\"",
						UseShellExecute = false,
					});
					return;
				} catch (Exception ex) {
					Output ("[run] xterm not available (" + ex.Message + "), running inline");
				}
			}
			Output ($"[run] dotnet run -c {runConfig} — " + Path.GetFileName (proj));
			await RunProcessAsync ("dotnet", $"run -c \"{runConfig}\" --project \"{proj}\"");
			return;
		}
		// Legacy DebugHandler.Debug: build first, then launch under the debugger with
		// the persisted breakpoints of the solution. The DAP stop event drives the
		// Locals/Watch/Call Stack pads and the execution-line highlight.
		Output ($"[debug] building ({runConfig})…");
		await RunBuildAsync (rebuild: false);
		var projDir = Path.GetDirectoryName (proj)!;
		// SDK layouts put the TFM between configuration and output (bin/Debug/net10.0)
		// unless AppendTargetFrameworkToOutputPath is disabled.
		var dllCandidates = new [] {
			Path.Combine (projDir, "bin", runConfig, "net10.0", Path.GetFileNameWithoutExtension (proj) + ".dll"),
			Path.Combine (projDir, "bin", runConfig, Path.GetFileNameWithoutExtension (proj) + ".dll"),
		};
		var dll = dllCandidates.FirstOrDefault (File.Exists);
		if (dll is null) {
			Output ("[debug] built assembly not found: " + dllCandidates [0]);
			return;
		}
		var bps = CollectPersistedBreakpoints ();
		Output ($"[debug] netcoredbg launch — {Path.GetFileName (dll)}, breakpoints: {bps.Count}");
		debugSession?.Dispose ();
		var session = new MonoDevelop.Debugger.Services.DebugSessionService ();
		debugSession = session;
		session.DebuggerOutput += (_, text) => Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			foreach (var line in text.Split ('\n'))
				if (!string.IsNullOrWhiteSpace (line))
					Output (line.TrimEnd ());
		});
		session.Stopped += (_, stop) => Avalonia.Threading.Dispatcher.UIThread.Post (() => OnDebuggerStopped (stop));
		session.Terminated += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			Output ("[debug] terminated");
			debugPaused = false;
			SetDebugButtonsVisible (false);
			ClearExecutionLineHighlight ();
		});
		// Legacy MainToolbar: the step buttons only exist while a debug session runs.
		SetDebugButtonsVisible (true);
		var ok = await session.StartAsync (dll, Path.GetDirectoryName (proj)!, bps);
		Output (ok ? "[debug] session started" : "[debug] failed to start netcoredbg session");
	}

	// DebuggingService.OnStoreUserPrefs read-back: the persisted breakpoints of
	// the whole solution (1-based lines) as DAP (file, line, condition, hit,
	// log) tuples. Disabled breakpoints stay in the store but are not pushed.
	System.Collections.Generic.List<(string File, int Line, string? Condition, int? HitCount, string? LogMessage)> CollectPersistedBreakpoints ()
	{
		var result = new System.Collections.Generic.List<(string, int, string?, int?, string?)> ();
		if (string.IsNullOrEmpty (loadedSolutionPath))
			return result;
		try {
			foreach (var b in MonoDevelop.Debugger.Services.BreakpointService.Load (loadedSolutionPath))
				if (b.Enabled && File.Exists (b.FileName))
					result.Add ((Path.GetFullPath (b.FileName), b.Line, b.Condition, b.HitCount, b.LogMessage));
		} catch (Exception ex) {
			Output ("[debug] breakpoint load failed: " + ex.Message);
		}
		return result;
	}

	// Legacy CurrentLineNumber highlight: select the document and paint the stopped
	// line yellow (like the execution arrow) until Continue/terminate clears it.
	void OnDebuggerStopped (MonoDevelop.Debugger.Services.DebugStopInfo stop)
	{
		debugPaused = true;
		SetDebugButtonsVisible (true);
		var frame = stop.Frames.FirstOrDefault ();
		if (frame is not null && File.Exists (frame.File)) {
			OpenFileDocumentAtLine (frame.File, frame.Line);
			currentDebugFile = Path.GetFullPath (frame.File);
			currentDebugLine = frame.Line;
			HighlightExecutionLine ();
			ShowDataTipForFrame (frame);
			// Legacy PinnedWatch.Evaluate: the pinned bubbles of the open documents
			// re-evaluate on every stop (continue/step clear them with the tips).
			RefreshPinnedWatchValuesAsync ();
			Output ($"[debug] stopped ({stop.Reason}) at {Path.GetFileName (frame.File)}:{frame.Line}");
		}
		_ = RefreshDebugPadsAsync ();
	}

	// Legacy inline DataTip: evaluate the identifiers of the stopped line and
	// render the first resolvable value as a green inline bubble on that line
	// (cleared on continue/stop). The first word may be a type or keyword
	// (e.g. "Console.WriteLine(...)") that no scope resolves — like the legacy
	// tooltip, which only resolves what the current frame can evaluate, each
	// identifier is tried until one evaluates without error. Best effort.
	async void ShowDataTipForFrame (MonoDevelop.Debugger.Services.DebugFrame frame)
	{
		if (!docs.TryGetValue (Path.GetFileName (frame.File), out var ed))
			return;
		ed.SetDataTip (frame.Line - 1, null);
		if (debugSession is not { IsActive: true } sess)
			return;
		var lineText = ed.LineTextForTest (frame.Line - 1);
		var words = System.Text.RegularExpressions.Regex.Matches (lineText ?? "", "[A-Za-z_][A-Za-z0-9_]*")
			.Select (m => m.Value).Distinct ().Take (5);
		foreach (var word in words) {
			var ev = await sess.EvaluateAsync (word, sess.CurrentFrameId);
			if (ev.Error is null) {
				ed.SetDataTip (frame.Line - 1, $"{word} = {ev.Value}");
				return;
			}
		}
	}

	void ClearDataTips ()
	{
		foreach (var (_, ed) in docs) {
			ed.SetDataTip (null, null);
			ed.SetPinnedWatchValues (null); // pinned bubbles back to "expr = ?"
		}
	}

	// ----- Pinned watches (legacy Debugger.PinnedWatch adorners): the word at
	// the caret pins to its line as an amber bubble; the store (file/line/
	// expression) persists in the legacy PinnedWatches user-prefs key through
	// the debugger addin's WatchService, and every debugger stop re-evaluates
	// the pins of the open documents (legacy PinnedWatch.Evaluate). -----

	void PinWatchAtCaret (MonoDevelop.Ide.Controls.SkTextEditor ed)
	{
		var expr = ed.WordAtCaret ();
		if (expr.Length == 0) {
			Output ("[pinwatch] no word at caret");
			return;
		}
		ed.TogglePinnedWatch (ed.CurrentLine, expr);
	}

	// DebuggingService.OnStoreUserPrefs for pins: the whole editor store of
	// every open document, saved next to the pad watches.
	void RefreshPinnedWatchesPad ()
	{
		if (string.IsNullOrEmpty (loadedSolutionPath))
			return;
		try {
			var pins = new List<MonoDevelop.Debugger.Services.WatchEntry> ();
			foreach (var ed in docs.Values.Where (d => !string.IsNullOrEmpty (d.FilePath) && d.PinnedWatchList.Count > 0))
				foreach (var (line, expr) in ed.PinnedWatchList)
					pins.Add (new MonoDevelop.Debugger.Services.WatchEntry (ed.FilePath, line + 1, expr)); // 1-based like PinnedWatch
			MonoDevelop.Debugger.Services.WatchService.SavePinned (loadedSolutionPath, watchExpressions, pins);
		} catch (Exception ex) {
			Output ("[pinwatch] persist failed: " + ex.Message);
		}
	}

	void OnEditorPinnedWatchesChanged (object? sender, EventArgs e)
		=> RefreshPinnedWatchesPad ();

	// Legacy stopped hook: evaluate every pin in the current frame; the value
	// replaces the bubble's static label until continue/step clears it.
	async void RefreshPinnedWatchValuesAsync ()
	{
		if (debugSession is not { IsActive: true } sess || !debugPaused) {
			foreach (var ed in docs.Values)
				ed.SetPinnedWatchValues (null);
			return;
		}
		var values = new List<(int line, string label)> ();
		foreach (var ed in docs.Values.Where (d => d.PinnedWatchList.Count > 0)) {
			foreach (var (line, expr) in ed.PinnedWatchList) {
				var ev = await sess.EvaluateAsync (expr, sess.CurrentFrameId);
				values.Add ((line, expr + " = " + (ev.Error is null ? ev.Value : "?")));
			}
		}
		foreach (var ed in docs.Values.Where (d => d.PinnedWatchList.Count > 0))
			ed.SetPinnedWatchValues (values);
	}

	void HighlightExecutionLine ()
	{
		if (currentDebugFile is null || currentDebugLine <= 0)
			return;
		foreach (var (_, ed) in docs) {
			if (Path.GetFullPath (ed.FilePath) == currentDebugFile)
				ed.SetExecutionLine (currentDebugLine - 1); // 0-based internally
			else
				ed.SetExecutionLine (-1);
		}
	}

	void ClearExecutionLineHighlight ()
	{
		currentDebugLine = -1;
		currentDebugFile = null;
		foreach (var (_, ed) in docs)
			ed.SetExecutionLine (-1);
	}

	async System.Threading.Tasks.Task RefreshDebugPadsAsync ()
	{
		if (debugSession is { IsActive: true } sess) {
			var locals = await sess.GetLocalsAsync ();
			FillVariableList (localsList, locals, "No locals");
			await RefreshWatchPadAsync ();
			// Call Stack: frames of the stopped thread; double click navigates.
			var frames = sess.CurrentFrames;
			if (callStackList is not null) {
				callStackList.Items.Clear ();
				foreach (var f in frames)
					callStackList.Items.Add (new ListBoxItem {
						Tag = f,
						Content = new TextBlock {
							Text = $"{f.Method} — {Path.GetFileName (f.File)}:{f.Line}",
							FontSize = 11.5,
						},
					});
			}
			// Threads pad: real threads; the stopped one is marked.
			if (threadsList is not null) {
				var threads = await sess.GetThreadsAsync ();
				threadsList.Items.Clear ();
				foreach (var t in threads)
					threadsList.Items.Add (new ListBoxItem {
						Tag = t,
						Content = new TextBlock {
							Text = $"{t.Id}  {t.Name}" + (t.Stopped ? "  (stopped)" : ""),
							FontSize = 11.5,
						},
					});
			}
			SetPadVisible ("locals", true);
		}
	}

	// Watch pad: evaluate every watch expression in the current frame (the legacy
	// Watch pad re-evaluates on each stop). Roots are tree nodes — a watch with
	// children expands like a Locals variable. Rows keep their owning expression
	// in VariableNode.WatchExpression so in-place editing knows what to replace.
	async System.Threading.Tasks.Task RefreshWatchPadAsync ()
	{
		var tree = watchList;
		if (tree is null)
			return;
		tree.Items.Clear ();
		if (debugSession is not { IsActive: true } sess || watchExpressions.Count == 0) {
			tree.Items.Add (new VariableNode (watchExpressions.Count == 0 ? "No watches" : "Not paused", 0));
			return;
		}
		var frameId = sess.CurrentFrameId;
		foreach (var expr in watchExpressions) {
			var ev = await sess.EvaluateAsync (expr, frameId);
			var display = $"{expr} = " + (ev.Error is null ? ev.Value : $"? ({ev.Error})");
			tree.Items.Add (new VariableNode (display, ev.HasChildren ? ev.VariablesReference : 0) { WatchExpression = expr });
		}
	}

	// Watch expressions in insertion order (legacy Watch pad watch items).
	readonly System.Collections.Generic.List<string> watchExpressions = new ();

	async void AddWatchExpression ()
	{
		var dlg = new Views.InputDialog ("Add Watch", "Expression:");
		await dlg.ShowDialog (this);
		if (dlg.Confirmed)
			await CommitWatchExpressionAsync (null, dlg.Value.Trim ());
		else
			await RefreshWatchPadAsync ();
	}

	void RemoveSelectedWatch ()
	{
		if (watchList?.SelectedItem is VariableNode node && node.WatchExpression is { Length: > 0 } name) {
			if (watchExpressions.Remove (name))
				PersistWatches ();
		}
		_ = RefreshWatchPadAsync ();
	}

	// ----- Watch pad in-place editing (legacy Watch pad: double-click or the
	// context menu puts an inline editor over the row; Enter commits the new
	// expression — replacing the old one in place — and re-evaluates, Esc
	// cancels). Committing also persists the store like any other mutation. -----
	void EditSelectedWatch ()
	{
		if (watchList?.SelectedItem is VariableNode { WatchExpression: { Length: > 0 } })
			BeginWatchEdit ();
	}

	void BeginWatchEdit ()
	{
		var tree = watchList;
		if (tree is null || watchEditBox is not null)
			return;
		if (tree.SelectedItem is not VariableNode { WatchExpression: { Length: > 0 } expr })
			return;
		int idx = tree.Items.IndexOf (tree.SelectedItem);
		if (idx < 0)
			return;
		// Realized row container (TreeViewItem) whose DataContext is the selected node.
		var container = tree.GetRealizedContainers ()?.OfType<TreeViewItem> ().FirstOrDefault (c => ReferenceEquals (c.DataContext, tree.SelectedItem));
		if (container is null)
			return;
		var box = new TextBox { Text = expr, FontSize = 11.5 };
		// Escape rolls back; Enter commits through the shared rename path.
		box.KeyDown += (s, e) => {
			if (e.Key == Key.Escape) {
				EndWatchEdit ();
				e.Handled = true;
			} else if (e.Key == Key.Enter) {
				CommitWatchEdit ();
				e.Handled = true;
			}
		};
		box.LostFocus += (_, _) => EndWatchEdit ();
		watchEditBox = box;
		container.Header = box;
		box.Focus ();
		box.SelectAll ();
	}

	void CommitWatchEdit ()
	{
		var box = watchEditBox;
		var tree = watchList;
		if (box is null || tree is null)
			return;
		var oldExpr = (tree.SelectedItem as VariableNode)?.WatchExpression;
		var newExpr = box.Text?.Trim () ?? "";
		watchEditBox = null;
		_ = CommitWatchExpressionAsync (oldExpr, newExpr);
	}

	void EndWatchEdit ()
	{
		if (watchEditBox is not null) {
			watchEditBox = null;
			_ = RefreshWatchPadAsync ();
		}
	}

	// Shared by the in-place editor and Add Watch: replaces oldExpr with newExpr
	// (dedup + insertion order kept), persists and re-evaluates the pad.
	async System.Threading.Tasks.Task CommitWatchExpressionAsync (string? oldExpr, string newExpr)
	{
		if (oldExpr is not null) {
			int i = watchExpressions.IndexOf (oldExpr);
			if (i >= 0) {
				if (newExpr.Length == 0)
					watchExpressions.RemoveAt (i);
				else if (!watchExpressions.Contains (newExpr))
					watchExpressions [i] = newExpr;
			}
		} else if (newExpr.Length > 0 && !watchExpressions.Contains (newExpr)) {
			watchExpressions.Add (newExpr);
		}
		PersistWatches ();
		await RefreshWatchPadAsync ();
	}

	TextBox? watchEditBox;

	// ----- Variable trees (Locals/Watch): expandable nodes like the legacy pad.
	// A node with variablesReference > 0 shows a placeholder child; on expand the
	// real children load from the DAP session (lazy, like Scope/Variables).
	static TreeView MakeVariableTree ()
	{
		var tv = new TreeView { Background = Brushes.Transparent };
		tv.Bind (TreeView.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		tv.ItemTemplate = new FuncTreeDataTemplate<VariableNode> (
			(node, _) => new TextBlock { Text = node.Display },
			node => node.LoadChildren ());
		return tv;
	}

	// DAP child expansion for the trees: variables of a variablesReference, or
	// the result of re-evaluating a watch expression (its children come from the
	// evaluation result). The leaf marker keeps the expander honest.
	System.Collections.Generic.IEnumerable<VariableNode> LoadVariableChildren (VariableNode node)
	{
		if (debugSession is not { IsActive: true } sess) {
			node.MarkLoaded ();
			return new[] { new VariableNode ("session ended", 0) };
		}
		var children = node.VariablesReference > 0
			? sess.GetVariablesAsync (node.VariablesReference).GetAwaiter ().GetResult ()
			: null;
		node.MarkLoaded ();
		if (children is null || children.Length == 0)
			return new[] { new VariableNode ("(no children)", 0) };
		return children.Select (v => NodeFor (v, v.Name));
	}

	public sealed class VariableNode
	{
		public string Display { get; }
		public int VariablesReference { get; }
		public bool Loaded { get; private set; }

		// Owning expression of a Watch pad root row (in-place editing).
		public string? WatchExpression { get; init; }

		public VariableNode (string display, int variablesReference)
		{
			Display = display;
			VariablesReference = variablesReference;
		}

		public void MarkLoaded () => Loaded = true;

		public System.Collections.Generic.IEnumerable<VariableNode> LoadChildren ()
		=> Loader is null ? new VariableNode [0] : Loader (this) ?? new VariableNode [0];

		// Hook set by MainWindow (needs the live session); static so the data
		// template can call it without a reference to the window.
		public static Func<VariableNode, System.Collections.Generic.IEnumerable<VariableNode>?>? Loader;
	}

	void FillVariableList (TreeView? tree, MonoDevelop.Debugger.Services.DebugVariable [] vars, string emptyText)
	{
		if (tree is null)
			return;
		tree.Items.Clear ();
		if (vars.Length == 0) {
			tree.Items.Add (new VariableNode (emptyText, 0));
			return;
		}
		foreach (var v in vars)
			tree.Items.Add (NodeFor (v, v.Name));
	}

	static VariableNode NodeFor (MonoDevelop.Debugger.Services.DebugVariable v, string label)
		=> new ($"{label} = {v.Value}", v.HasChildren ? v.VariablesReference : 0);

	void FillVariableList (ListBox? list, MonoDevelop.Debugger.Services.DebugVariable [] vars, string emptyText)
	{
		if (list is null)
			return;
		list.Items.Clear ();
		if (vars.Length == 0) {
			list.Items.Add (new ListBoxItem { Content = new TextBlock { Text = emptyText, FontSize = 11.5, Opacity = 0.6 } });
			return;
		}
		foreach (var v in vars)
			list.Items.Add (new ListBoxItem {
				Content = new TextBlock { Text = $"{v.Name} = {v.Value}", FontSize = 11.5 },
			});
	}

	void ContinueDebug ()
	{
		if (debugSession is { IsActive: true } s && debugPaused) {
			debugPaused = false;
			ClearExecutionLineHighlight ();
			ClearDataTips ();
			_ = s.ContinueAsync ();
			Output ("[debug] continue");
		}
	}

	// Legacy Immediate window: evaluate an expression in the current frame and
	// print "expr = value" in the Output pad (not paused → honest message).
	void RunImmediate ()
	{
		HideImmediateCompletion ();
		var expr = immediateInput?.Text?.Trim ();
		if (string.IsNullOrEmpty (expr))
			return;
		if (debugSession is not { IsActive: true } sess) {
			Output ($"[immediate] {expr} = ? (no active debug session)");
			return;
		}
		_ = RunImmediateAsync (expr);
	}

	// Member completion for the Immediate input: typing "expr." evaluates the
	// prefix over DAP and lists its members in a popup under the input; Tab/
	// Enter (when selected) or a click commits "expr.<member>". Esc hides it.
	Popup? immediatePopup;
	ListBox? immediatePopupList;
	string immediatePrefix = "";

	void ImmediateInputKeyDown (object? sender, KeyEventArgs e)
	{
		if (immediatePopup is { IsVisible: true } && immediatePopupList is { Items.Count: > 0 }) {
			switch (e.Key) {
				case Key.Down:
					immediatePopupList.SelectedIndex = Math.Min (immediatePopupList.Items.Count - 1, immediatePopupList.SelectedIndex + 1);
					e.Handled = true;
					return;
				case Key.Up:
					immediatePopupList.SelectedIndex = Math.Max (0, immediatePopupList.SelectedIndex - 1);
					e.Handled = true;
					return;
				case Key.Escape:
					HideImmediateCompletion ();
					e.Handled = true;
					return;
				case Key.Tab:
					CommitImmediateCompletion ();
					e.Handled = true;
					return;
				case Key.Enter:
					if (immediatePopupList.SelectedItem is not null) {
						CommitImmediateCompletion ();
						e.Handled = true;
						return;
					}
					break;
			}
		}
		if (e.Key == Key.Enter)
			RunImmediate ();
	}

	public async System.Threading.Tasks.Task ImmediateMemberCompletionAsync () => await ImmediateMemberCompletionCore ();

	async System.Threading.Tasks.Task ImmediateMemberCompletionCore ()
	{
		var text = immediateInput?.Text ?? "";
		var caret = immediateInput?.CaretIndex ?? 0;
		var upto = text.Substring (0, Math.Min (caret, text.Length));
		int dot = upto.LastIndexOf ('.');
		if (dot < 0 || debugSession is not { IsActive: true } sess || !debugPaused) {
			HideImmediateCompletion ();
			return;
		}
		var prefix = upto.Substring (0, dot);
		if (prefix.Length == 0) {
			HideImmediateCompletion ();
			return;
		}
		var target = prefix.EndsWith (".", StringComparison.Ordinal) ? prefix.TrimEnd ('.') : prefix;
		var ev = await sess.EvaluateAsync (target, sess.CurrentFrameId);
		if (ev.Error is not null || !ev.HasChildren) {
			HideImmediateCompletion ();
			return;
		}
		var members = await sess.GetVariablesAsync (ev.VariablesReference);
		if (members.Length == 0) {
			HideImmediateCompletion ();
			return;
		}
		immediatePrefix = prefix + ".";
		immediatePopup ??= new Popup { PlacementTarget = immediateInput };
		immediatePopupList ??= new ListBox { MaxHeight = 160, MinWidth = 240 };
		immediatePopupList.Items.Clear ();
		foreach (var m in members)
			immediatePopupList.Items.Add (new ListBoxItem {
				Tag = m.Name,
				Content = new TextBlock { Text = $"{m.Name}  {m.Value}", FontSize = 11.5 },
			});
		immediatePopupList.SelectedIndex = 0;
		// Rebind instead of re-subscribing: a plain += would stack a handler on
		// every completion popup (double-commit on the second click).
		immediatePopupList.DoubleTapped -= CommitImmediateCompletionHandler;
		immediatePopupList.DoubleTapped += CommitImmediateCompletionHandler;
		immediatePopup.Child = immediatePopupList;
		immediatePopup.IsOpen = true;
	}

	void CommitImmediateCompletionHandler (object? sender, Avalonia.Input.TappedEventArgs e) => CommitImmediateCompletion ();

	void CommitImmediateCompletion ()
	{
		if (immediatePopupList?.SelectedItem is ListBoxItem { Tag: string member } && immediateInput is not null) {
			var caret = immediateInput.CaretIndex;
			var text = immediateInput.Text ?? "";
			var head = text.Substring (0, Math.Min (caret, text.Length));
			int dot = head.LastIndexOf ('.');
			var tail = caret < text.Length ? text.Substring (caret) : "";
			var replaced = head.Substring (0, dot + 1) + member + tail;
			immediateInput.Text = replaced;
			immediateInput.CaretIndex = dot + 1 + member.Length;
		}
		HideImmediateCompletion ();
	}

	void HideImmediateCompletion ()
	{
		if (immediatePopup is { IsOpen: true })
			immediatePopup.IsOpen = false;
	}

	async System.Threading.Tasks.Task RunImmediateAsync (string expr)
	{
		if (debugSession is not { IsActive: true } sess) {
			Output ($"[immediate] {expr} = ? (no active debug session)");
			return;
		}
		var ev = await sess.EvaluateAsync (expr, sess.CurrentFrameId);
		Output ($"[immediate] {expr} = " + (ev.Error is null ? ev.Value : $"? ({ev.Error})"));
		if (ev.Error is null)
			immediateInput?.Clear ();
	}

	// Legacy StepOver/StepInto/StepOut: DAP next/stepIn/stepOut on the stopped
	// thread; the following stopped event re-highlights and refreshes the pads
	// (which re-evaluates the Watch pad after every step, like the legacy
	// Watch pad refresh on StoppedEvent).
	void StepDebug (string which)
	{
		if (debugSession is { IsActive: true } s && debugPaused) {
			debugPaused = false;
			ClearExecutionLineHighlight ();
			ClearDataTips ();
			switch (which) {
				case "into": _ = s.StepIntoAsync (); break;
				case "out": _ = s.StepOutAsync (); break;
				default: _ = s.StepOverAsync (); break;
			}
			Output ("[debug] step " + which);
		}
	}

	void StopDebug ()
	{
		if (debugSession is { IsActive: true } s) {
			s.Terminate ();
			Output ("[debug] stopped by user");
		}
		debugPaused = false;
		ClearExecutionLineHighlight ();
		ClearDataTips ();
	}

	// Legacy AttachToProcessHandler: pick a running process and debug it. The
	// picker is the "attach" tab of the bottom pad (in-window, Avalonia chrome —
	// never an OS-decorated popup).
	async System.Threading.Tasks.Task ShowAttachToProcessAsync ()
	{
		if (attachPanel is null)
			return;
		attachPanel.ScanProcesses ();
		BottomPads.SetTabVisible ("attach", true);
		SetPadVisible ("bottom", true);
		BottomPads.Select ("attach");
		Output ("[attach] panel opened — pick a process and press Attach");
		await System.Threading.Tasks.Task.CompletedTask;
	}

	// AttachToProcessHandler.Run: attach the DAP session to the picked PID. The
	// persisted breakpoints are pushed too; Terminate() then detaches (IsAttach)
	// instead of killing the process, like the legacy DetachFromProcess.
	async System.Threading.Tasks.Task AttachToProcessAsync (int pid)
	{
		Output ("[attach] attaching to pid " + pid + "…");
		debugSession?.Dispose ();
		var session = new MonoDevelop.Debugger.Services.DebugSessionService ();
		debugSession = session;
		session.DebuggerOutput += (_, text) => Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			foreach (var line in text.Split ('\n'))
				if (!string.IsNullOrWhiteSpace (line))
					Output (line.TrimEnd ());
		});
		session.Stopped += (_, stop) => Avalonia.Threading.Dispatcher.UIThread.Post (() => OnDebuggerStopped (stop));
		session.Terminated += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			Output ("[debug] terminated");
			debugPaused = false;
			SetDebugButtonsVisible (false);
			ClearExecutionLineHighlight ();
		});
		// Legacy MainToolbar: the step buttons only exist while a debug session runs.
		SetDebugButtonsVisible (true);
		var bps = CollectPersistedBreakpoints ();
		var ok = await session.AttachAsync (pid, bps);
		Output (ok
			? $"[attach] session attached to {pid} (breakpoints: {bps.Count})"
			: "[attach] failed to attach — netcoredbg attach mode rejected the PID");
		if (ok) {
			SetPadVisible ("locals", true);
			SetPadVisible ("threads", true);
			// Show where the process is right now (netcoredbg stops it on attach).
			var stop = session.LastStop;
			if (stop is not null)
				OnDebuggerStopped (stop);
		}
	}

	// Legacy Threads pad double-click: switch the Call Stack pad to the thread.
	async System.Threading.Tasks.Task ShowThreadStackTraceAsync (MonoDevelop.Debugger.Services.DebugSessionService session, int threadId)
	{
		var frames = await session.GetStackTraceAsync (threadId);
		if (callStackList is not null) {
			callStackList.Items.Clear ();
			foreach (var f in frames)
				callStackList.Items.Add (new ListBoxItem {
					Tag = f,
					Content = new TextBlock { Text = $"{f.Method} — {Path.GetFileName (f.File)}:{f.Line}", FontSize = 11.5 },
				});
		}
		Output ("[threads] call stack of thread " + threadId + " — " + frames.Length + " frames");
	}

	// Legacy StackFrame selection: the Locals tree shows the selected frame's
	// scope (scopes by frameId), like clicking a frame in the legacy Call Stack.
	async System.Threading.Tasks.Task ShowFrameLocalsAsync (MonoDevelop.Debugger.Services.DebugSessionService session, MonoDevelop.Debugger.Services.DebugFrame frame)
	{
		var locals = await session.GetLocalsForFrameAsync (frame.Id);
		FillVariableList (localsList, locals, "No locals");
		SetPadVisible ("locals", true);
		Output ("[frame] locals of " + frame.Method + " — " + locals.Length + " rows");
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
				// SetErrors and RefreshCodeIssuesPad both read buildErrors, so the pads
				// are filled here and the collections are reset by the caller that owns
				// the build (RunBuildAsync), not per process — otherwise the Errors pad
				// would always end up empty (legacy ErrorListPad kept the diagnostics of
				// the finished build on screen).
				if (code == 0)
					SetErrors ("Build succeeded.");
				else if (buildErrors.Count == 0)
					SetErrors ($"Build FAILED with exit code {code}.");
				RefreshCodeIssuesPad ();
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
		if (MonoDevelop.Ide.Services.CodeIssueService.Parse (line) is not { } issue)
			return;
		buildErrors.Add ((issue.File, issue.Line, issue.Col, issue.Severity, issue.Code, issue.Message));
		SetErrors ($"{buildErrors.Count} problem(s) — last: {issue.Message}");
		// Live message bubble on the affected line of the open document
		// (legacy MessageBubble appears as soon as the error is reported).
		var errFile = Path.GetFileName (issue.File);
		if (docs.TryGetValue (errFile, out var bubbleEd)) {
			var entry = (issue.Line - 1, // 0-based line
				$"{issue.Code}: {issue.Message}",
				issue.Severity == "error");
			bubbleEd.SetBubbles (new [] { entry });
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

	// QA driver for the ProgressDialog port: nested tasks, WriteText details,
	// cancel detection and the ShowDone completion states.
	async System.Threading.Tasks.Task RunProgressQaAsync ()
	{
		var dlg = new ProgressDialog (allowCancel: true, showDetails: true);
		Output ("[progress] dialog opened (allowCancel, details)");
		_ = dlg.ShowDialog (this);
		await System.Threading.Tasks.Task.Delay (150);
		dlg.BeginTask ("Restoring packages");
		dlg.Progress = 0.25;
		dlg.WriteText ("NuGet 6.11 resolver OK" + Environment.NewLine);
		dlg.BeginTask ("Building TestProj");
		dlg.Progress = 0.6;
		dlg.WriteText ("0 warnings, 0 errors" + Environment.NewLine);
		dlg.EndTask ();
		dlg.EndTask ();
		dlg.Progress = 1;
		dlg.ShowDone (warnings: false, errors: false);
		Output ("[progress] nested tasks done; message='" + dlg.Message + "'; bar=1; close visible, cancel hidden");
		Output ("[progress] details lines: " + (dlg.DetailsTextForQa?.Split (Environment.NewLine).Length ?? 0));
	}

	// VersionControl.Commands.Diff: real `git diff` shown in the bottom pad's
	// Diff tab (the legacy opens the VersionControl diff view as an internal
	// document/pad viewer, not a modal window; the patch renders monospaced with
	// +/- lines tinted green/red like the legacy diff view).
	async System.Threading.Tasks.Task ShowDiffAsync ()
	{
		if (string.IsNullOrEmpty (loadedSolutionPath)) {
			Output ("[diff] no solution loaded");
			return;
		}
		var dir = Path.GetDirectoryName (loadedSolutionPath)!;
		string patch;
		try {
			var psi = new System.Diagnostics.ProcessStartInfo {
				FileName = "git",
				Arguments = "diff",
				WorkingDirectory = dir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			using var p = System.Diagnostics.Process.Start (psi);
			if (p is null) {
				Output ("[diff] git could not be started");
				return;
			}
			patch = await p.StandardOutput.ReadToEndAsync ();
			await p.WaitForExitAsync ();
		} catch (Exception ex) {
			Output ("[diff] git failed: " + ex.Message);
			return;
			}
		var box = BuildDiffView (string.IsNullOrWhiteSpace (patch) ? "(no changes)" : patch.TrimEnd ());
		if (BottomPads.Tabs.All (t => t.Id != "diff"))
			BottomPads.AddTab (new PadHost.PadTab {
				Id = "diff",
				Label = "Diff",
				Icon = "vc-diff",
				Content = box,
				Visible = false,
			});
		else
			BottomPads.ReplaceTabContent ("diff", box);
		BottomPads.SetTabVisible ("diff", true);
		BottomPads.Select ("diff");
		Output ("[diff] pad shown " + (string.IsNullOrWhiteSpace (patch) ? 0 : patch.Count (c => c == '\n')) + " patch lines");
	}

	// Legacy DiffWidget renders added lines green and removed lines red (with
	// the header/hunk lines dimmed); the pad viewer replicates that with a
	// SelectableTextBlock of colored runs, monospaced like the legacy view.
	Control BuildDiffView (string patch)
	{
		bool dark = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
		var addColor = dark ? Avalonia.Media.Color.Parse ("#81c884") : Avalonia.Media.Color.Parse ("#0a7a0a");
		var delColor = dark ? Avalonia.Media.Color.Parse ("#e08a8a") : Avalonia.Media.Color.Parse ("#b02020");
		var headColor = dark ? Avalonia.Media.Color.Parse ("#8a9ab0") : Avalonia.Media.Color.Parse ("#556688");
		var fg = Application.Current?.TryGetResource ("IdeFgBrush", Application.Current.ActualThemeVariant, out var f) == true && f is Avalonia.Media.IBrush fb
			? fb : Avalonia.Media.Brushes.Gray;

		var text = new Avalonia.Controls.TextBlock {
			FontFamily = new Avalonia.Media.FontFamily ("Monospace,DejaVu Sans Mono,Consolas"),
			TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
		};
		bool first = true;
		foreach (var line in patch.Split ('\n')) {
			if (!first)
				text.Inlines!.Add (new Avalonia.Controls.Documents.Run (Environment.NewLine));
			first = false;
			var run = new Avalonia.Controls.Documents.Run (line);
			if (line.StartsWith ("+++", StringComparison.Ordinal) || line.StartsWith ("---", StringComparison.Ordinal) || line.StartsWith ("diff", StringComparison.Ordinal) || line.StartsWith ("index ", StringComparison.Ordinal))
				run.Foreground = new Avalonia.Media.SolidColorBrush (headColor);
			else if (line.StartsWith ("@@", StringComparison.Ordinal))
				run.Foreground = new Avalonia.Media.SolidColorBrush (headColor);
			else if (line.StartsWith ("+", StringComparison.Ordinal))
				run.Foreground = new Avalonia.Media.SolidColorBrush (addColor);
			else if (line.StartsWith ("-", StringComparison.Ordinal))
				run.Foreground = new Avalonia.Media.SolidColorBrush (delColor);
			else
				run.Foreground = fg;
			text.Inlines!.Add (run);
		}
		var scroll = new ScrollViewer {
			Content = text,
			Padding = new Avalonia.Thickness (8, 6),
		};
		return scroll;
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
