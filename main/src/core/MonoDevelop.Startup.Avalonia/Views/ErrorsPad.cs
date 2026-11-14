using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy ErrorListPad (MonoDevelop.Ide.Gui.Pads.ErrorListPad, GTK) onto the
/// Avalonia shell: same behavior, toolbar, icons and table design.
///
/// Toolbar (legacy DockItemToolbar): the three severity toggle buttons "N Errors" /
/// "N Warnings" / "N Messages" with the legacy icons (Stock.Error/Warning/Information),
/// a "Build Output" toggle that opens the build text log beside the rows (legacy HPaned
/// + LogView), and a search entry that live-filters the rows (legacy TreeModelFilter).
/// The filter/visibility state persists under the legacy keys
/// (SharpDevelop.TaskList.Show* / OutputViewVisible / LogSeparatorPosition) so the two
/// UIs share one setting.
///
/// Table (legacy PadTreeView, 8 columns in the legacy order): ! (severity icon), the
/// "Marked" checkbox, Line, Description, File, Project, Path, Category. Rows are bold
/// until the file position is visited (legacy DataColumns.Read), strikethrough when
/// marked done (legacy CellRendererToggle), the description wraps, Type/Project/File/
/// Category sort by header click (legacy SetSortFunc), selection is multiple (legacy
/// SelectionMode.Multiple) and the context menu is the legacy one: Go to Reference, Go
/// to Task, Copy and the Columns selector (visibility persisted under
/// Monodevelop.ErrorListColumns, the legacy restore id).
///
/// Data source: the shell parses the build output into diagnostics (CodeIssueService).
/// The legacy TaskService/TaskListEntry is GTK-side and not compiled into the shell, so
/// the pad works over the equivalent row model (ErrorRow); the owner (MainWindow) fills
/// every field — including the resolved Project and Path — because it knows the loaded
/// solution's projects.
/// </summary>
public class ErrorsPad
{
	/// <summary>Column catalog in the legacy display order (VisibleColumns) with the
	/// persistence key; "type"/"marked" are the icon/checkbox columns.</summary>
	public static readonly (string Label, string Key)[] Columns = {
		("Type", "type"),
		("Marked", "marked"),
		("Line", "line"),
		("Description", "description"),
		("File", "file"),
		("Project", "project"),
		("Path", "path"),
		("Category", "category"),
	};

	const string ColumnsKey = "Monodevelop.ErrorListColumns";
	// Legacy ErrorListPad setting keys (shared with the GTK UI).
	const string ShowErrorsKey = "SharpDevelop.TaskList.ShowErrors";
	const string ShowWarningsKey = "SharpDevelop.TaskList.ShowWarnings";
	const string ShowMessagesKey = "SharpDevelop.TaskList.ShowMessages";
	const string LogSeparatorKey = "SharpDevelop.TaskList.LogSeparatorPosition";
	const string OutputVisibleKey = "SharpDevelop.TaskList.OutputViewVisible";

	/// <summary>One row of the table — the shell equivalent of the legacy TaskListEntry
	/// for build diagnostics, plus the pad-only state the legacy kept in its TreeStore
	/// (Read flag, Completed checkbox). Every field is owner-filled.</summary>
	public sealed class ErrorRow
	{
		public string File = "";
		public int Line;
		public int Col;
		public string Level = "info"; // error | warning | info | hidden (CodeIssueService severities)
		public string Code = "";
		public string Message = "";
		public string Project = "";   // legacy GetProject (containing project name)
		public string Path = "";      // legacy GetPath (path relative to the project dir)
		public string Category = "";  // legacy GetCategory (the build code, shown in the column)
		public bool Read;             // the document at File:Line was visited (DataColumns.Read)
		public bool Completed;        // the "Marked" checkbox (CellRendererToggle)
	}

	// ---- Public surface (wired by MainWindow) ----

	public Control Root { get; }

	/// <summary>Raised when a row is activated (double click / Go to Task), with the row.</summary>
	public event Action<ErrorRow?>? RowActivated;

	/// <summary>Raised when the "Build Output" pane toggles, with the new visibility.</summary>
	public event Action<bool>? LogToggled;

	/// <summary>Notifies the owner of the current pad-tab icon (legacy UpdatePadIcon:
	/// md-errors-list / -has-errors / -has-warnings).</summary>
	public event Action<string>? PadIconChanged;

	public bool LogVisible => logHost.IsVisible;
	public int ErrorCount => errorCount;
	public int WarningCount => warningCount;
	public int MessageCount => messageCount;
	public int RowCount => allRows.Count;

	/// <summary>Starts a new build cycle: the legacy ErrorListPad starts each build
	/// empty (BuildCycle) and clears its build output log (OnBuildStarted).</summary>
	public void BeginBuild ()
	{
		allRows.Clear ();
		rowToView.Clear ();
		errorCount = warningCount = messageCount = 0;
		logText.Text = "";
		UpdateCounts ();
		UpdatePadIcon ();
		RebuildRows ();
	}

	/// <summary>Appends one build output line to the "Build Output" text log (legacy
	/// LogView). Capped: the tail is what the user reads.</summary>
	public void AppendLogLine (string line)
	{
		logText.Text += line + "\n";
		if (logText.Text.Length > 300_000)
			logText.Text = logText.Text.Substring (logText.Text.Length - 150_000);
		if (logText.Text.Length > 0)
			logText.CaretIndex = logText.Text.Length; // auto-scroll to the tail, like the LogView
	}

	/// <summary>Adds a row from the live build parsing (legacy TaskAdded →
	/// AddTaskInternal): counters + tab icon update immediately, the table repaints on
	/// the next dispatcher turn (a build emits many diagnostics per second). Rows are
	/// deduplicated by identity — the owner creates one ErrorRow per diagnostic.</summary>
	public void UpdateRow (ErrorRow row)
	{
		if (allRows.Contains (row))
			return;
		allRows.Add (row);
		switch (Bucket (row.Level)) {
		case 0:
			errorCount++;
			break;
		case 1:
			warningCount++;
			break;
		default:
			messageCount++;
			break;
		}
		UpdateCounts ();
		UpdatePadIcon ();
		RowCountsChanged?.Invoke (allRows.Count);
		QueueRebuild ();
	}

	/// <summary>Raised when the total row count grows (the owner shows the pad the
	/// first time a build reports a problem, legacy task → pad auto-show).</summary>
	public event Action<int>? RowCountsChanged;

	/// <summary>Marks every row at File:Line as visited (legacy DataColumns.Read →
	/// the row leaves the bold "unread" state) and repaints those rows.</summary>
	public void MarkRead (string file, int line)
	{
		string full;
		try {
			full = Path.GetFullPath (file);
		} catch {
			return;
		}
		bool changed = false;
		foreach (var row in allRows) {
			if (row.Read || row.Line != line)
				continue;
			string rf;
			try {
				rf = Path.GetFullPath (row.File);
			} catch {
				continue;
			}
			if (rf == full) {
				row.Read = true;
				changed = true;
			}
		}
		if (changed)
			RepaintRows ();
	}

	// ---- QA surface: drive the toolbar headlessly + dump the model ----

	public void SetSeverityFilters (bool errors, bool warnings, bool messages)
	{
		errorBtn.IsChecked = errors;
		warnBtn.IsChecked = warnings;
		msgBtn.IsChecked = messages;
		RebuildRows ();
	}

	public void SetSearch (string pattern)
	{
		searchBox.Text = pattern;
		RebuildRows ();
	}

	public void SetLogVisible (bool visible) => logBtn.IsChecked = visible;

	/// <summary>QA headless equivalent of the user dragging the Build Output
	/// separator: moves it to the given fraction of the pad width and runs the
	/// same persist path the DragCompleted handler runs. Returns the stored
	/// value ("" when the body has no width yet).</summary>
	public string SimulateLogSeparatorMove (double ratio)
	{
		var w = bodyGrid.Bounds.Width;
		if (w <= 0)
			return "";
		bodyGrid.ColumnDefinitions [0].Width =
			new GridLength (Math.Max (200, Math.Min (w * ratio, w - 220)));
		PersistLogSeparator ();
		return SettingsStore.GetString (LogSeparatorKey) ?? "";
	}

	public int VisibleRowCount () => allRows.Count (Matches);

	public string DumpStatus ()
	{
		return $"errors={errorCount} warnings={warningCount} messages={messageCount} " +
			$"rows={allRows.Count} visible={VisibleRowCount()} log={(LogVisible ? "on" : "off")} " +
			$"filters={errorBtn.IsChecked},{warnBtn.IsChecked},{msgBtn.IsChecked} " +
			$"sort={(sortKey ?? "none")}:{(sortAsc ? "asc" : "desc")} " +
			$"cols=[{string.Join (",", visibleKeys)}] " +
			$"logLines={(int)(logText.Text.Length == 0 ? 0 : logText.Text.Split ('\n').Length - 1)}";
	}

	/// <summary>QA double-click path: activate the first visible row (the owner opens
	/// the file at the row position). Returns false when there is no visible row.</summary>
	public bool ActivateFirstRow ()
	{
		var first = OrderedRows ().FirstOrDefault (Matches);
		if (first is null)
			return false;
		RowActivated?.Invoke (first);
		return true;
	}

	/// <summary>Re-resolves the severity icons for the current theme variant and
	/// repaints the toolbar + visible rows (the shell calls this on theme changes,
	/// like PadHost.RefreshIcons for the tab chrome).</summary>
	public void RefreshIcons ()
	{
		severityIcons = (
			IconService.GetImage ("gtk-dialog-error"),
			IconService.GetImage ("gtk-dialog-warning"),
			IconService.GetImage ("gtk-dialog-info"));
		errorImg.Source = severityIcons.Item1;
		warnImg.Source = severityIcons.Item2;
		msgImg.Source = severityIcons.Item3;
		foreach (var view in rowToView.Values)
			view.Apply (view.Tag as ErrorRow, visibleKeys, severityIcons);
	}

	// ---- State ----

	readonly List<ErrorRow> allRows = new ();
	// row → its materialized view (repopulated on every rebuild).
	readonly Dictionary<ErrorRow, ErrorRowView> rowToView = new ();
	int errorCount, warningCount, messageCount;
	(IImage?, IImage?, IImage?) severityIcons = (null, null, null);
	string? sortKey;
	bool sortAsc = true;
	bool rebuildPending;

	ToggleButton errorBtn, warnBtn, msgBtn, logBtn;
	TextBlock errorLbl, warnLbl, msgLbl, logLbl;
	Image errorImg, warnImg, msgImg;
	TextBox searchBox;
	Grid bodyGrid;
	Border headerHost;
	Grid headerGrid;
	readonly Dictionary<string, TextBlock> headerLabels = new ();
	ListBox rowsList;
	Border logHost;
	TextBox logText;
	GridSplitter logSplitter;
	Control tableHost;
	// Legacy initialLogShow: the first REAL allocation of the body applies the
	// saved separator (the ctor runs before any layout, so the ctor-time apply
	// sees width 0 — same problem the legacy solved with SizeAllocated).
	bool initialLogShow = true;
	List<string> visibleKeys = new ();

	public IReadOnlyList<string> VisibleKeys => visibleKeys;

	public ErrorsPad ()
	{
		severityIcons = (
			IconService.GetImage ("gtk-dialog-error"),
			IconService.GetImage ("gtk-dialog-warning"),
			IconService.GetImage ("gtk-dialog-info"));

		visibleKeys = LoadColumnVisibility ();

		// --- Toolbar (legacy DockItemToolbar) ---
		errorBtn = MakeToolbarToggle ("gtk-dialog-error", "Show Errors", true, out errorLbl, out errorImg);
		warnBtn = MakeToolbarToggle ("gtk-dialog-warning", "Show Warnings", true, out warnLbl, out warnImg);
		msgBtn = MakeToolbarToggle ("gtk-dialog-info", "Show Messages", true, out msgLbl, out msgImg);
		// Legacy MakeButton ("md-message-log", ..., false, ...): the log toggle
		// starts UNCHECKED (the pad opens with only the table). The persisted
		// state is applied at the end of the ctor; because the factory default
		// differs from the "on" case, a previously-enabled log fires the change
		// handler there and reopens the panel (legacy restores it at startup).
		logBtn = MakeToolbarToggle ("md-message-log", "Build Output", false, out logLbl, out var logImg);
		logLbl.Text = "Build Output";

		// The persisted toolbar state is applied at the END of the ctor (after the
		// body exists — the log toggle handler resizes the body grid), so here only
		// the change handlers are attached.
		errorBtn.IsCheckedChanged += (_, _) => { SettingsStore.SetBool (ShowErrorsKey, errorBtn.IsChecked == true); RebuildRows (); };
		warnBtn.IsCheckedChanged += (_, _) => { SettingsStore.SetBool (ShowWarningsKey, warnBtn.IsChecked == true); RebuildRows (); };
		msgBtn.IsCheckedChanged += (_, _) => { SettingsStore.SetBool (ShowMessagesKey, msgBtn.IsChecked == true); RebuildRows (); };
		logBtn.IsCheckedChanged += (_, _) => SetLogPanelVisible (logBtn.IsChecked == true);

		searchBox = new TextBox {
			Watermark = "Search",
			Width = 200,
			VerticalAlignment = VerticalAlignment.Center,
		};
		searchBox.Bind (TextBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		searchBox.TextChanged += (_, _) => RebuildRows ();

		var sep = new Border {
			Width = 1,
			MinHeight = 18,
			Margin = new Thickness (6, 5),
			Background = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
		};
		var toolbar = new DockPanel { LastChildFill = false, Margin = new Thickness (2) };
		DockPanel.SetDock (searchBox, Dock.Right);
		toolbar.Children.Add (searchBox);
		var leftTools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		leftTools.Children.Add (errorBtn);
		leftTools.Children.Add (warnBtn);
		leftTools.Children.Add (msgBtn);
		leftTools.Children.Add (sep);
		leftTools.Children.Add (logBtn);
		toolbar.Children.Add (leftTools);

		// --- Body (legacy HPaned: table | build log; log hidden until "Build Output") ---
		logText = new TextBox {
			AcceptsReturn = true,
			IsReadOnly = true,
			FontSize = 12,
			Padding = new Thickness (8),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
		};
		logText.Bind (TextBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		logHost = new Border {
			Child = logText,
			IsVisible = false,
			MinWidth = 120,
			BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (1, 0, 0, 0),
		};
		logSplitter = new GridSplitter {
			Width = 4,
			Background = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			IsVisible = false,
			VerticalAlignment = VerticalAlignment.Stretch,
		};

		tableHost = MakeTableHost ();
		bodyGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto,*") };
		Grid.SetColumn (tableHost, 0);
		Grid.SetColumn (logSplitter, 1);
		Grid.SetColumn (logHost, 2);
		bodyGrid.Children.Add (tableHost);
		bodyGrid.Children.Add (logSplitter);
		bodyGrid.Children.Add (logHost);
		bodyGrid.SizeChanged += (_, _) => {
			// Legacy HandleControlSizeAllocated: on the FIRST real allocation,
			// apply the saved separator (the ctor ran with width 0).
			if (initialLogShow && logHost.IsVisible && bodyGrid.Bounds.Width > 0) {
				ApplyLogSeparator ();
				initialLogShow = false;
			}
		};
		// Legacy HandleSwSizeAllocated: after the user moves the separator, the
		// new position is persisted (as a fraction of the pad width) so the next
		// start restores it. The Thumb commits the column widths before this fires.
		logSplitter.DragCompleted += (_, _) => PersistLogSeparator ();

		var root = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (toolbar, Dock.Top);
		root.Children.Add (toolbar);
		root.Children.Add (bodyGrid);
		Root = root;

		// Legacy: apply the persisted toolbar state (the legacy pad reads the same
		// keys), then start empty: rows fill as the build runs (TaskAdded).
		errorBtn.IsChecked = SettingsStore.GetBool (ShowErrorsKey, true);
		warnBtn.IsChecked = SettingsStore.GetBool (ShowWarningsKey, true);
		msgBtn.IsChecked = SettingsStore.GetBool (ShowMessagesKey, true);
		logBtn.IsChecked = SettingsStore.GetBool (OutputVisibleKey, false);
		UpdateCounts ();
		RebuildRows ();
	}

	// ---- Table (header + rows) ----

	Control MakeTableHost ()
	{
		headerHost = new Border {
			BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (0, 0, 0, 1),
		};
		headerGrid = new Grid ();
		foreach (var (label, key) in Columns) {
			var cell = new Border {
				Padding = new Thickness (5, 3),
				BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
				BorderThickness = new Thickness (0, 0, 1, 0),
				Background = Brushes.Transparent,
			};
			var lbl = new TextBlock {
				Text = HeaderText (key),
				FontSize = 11,
				FontWeight = FontWeight.SemiBold,
				Opacity = 0.85,
				VerticalAlignment = VerticalAlignment.Center,
			};
			lbl.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
			cell.Child = lbl;
			if (IsSortable (key)) {
				// Legacy: these columns have a SortColumnId — clicking the header
				// sorts (first click ascending, next click flips the direction).
				cell.Cursor = new Cursor (StandardCursorType.Hand);
				var k = key;
				cell.PointerPressed += (_, _) => ToggleSort (k);
			}
			Grid.SetColumn (cell, Array.FindIndex (Columns, c => c.Key == key));
			headerGrid.Children.Add (cell);
			headerLabels [key] = lbl;
		}
		headerHost.Child = headerGrid;

		rowsList = new ListBox {
			Background = Brushes.Transparent,
			SelectionMode = SelectionMode.Multiple,
			BorderThickness = new Thickness (0),
		};
		rowsList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		// Legacy RowActivated (double click) → OnTaskJumpto.
		rowsList.DoubleTapped += (_, _) => {
			var row = rowsList.SelectedItems.OfType<ErrorRowView> ()
				.Select (v => v.Tag as ErrorRow).FirstOrDefault (r => r is not null);
			if (row is not null)
				RowActivated?.Invoke (row);
		};
		rowsList.ContextMenu = BuildContextMenu ();

		var table = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (headerHost, Dock.Top);
		table.Children.Add (headerHost);
		table.Children.Add (rowsList);
		ApplyColumnDefinitions ();
		return table;
	}

	static string HeaderText (string key) => key switch {
		"type" => "!",
		"marked" => "",
		"line" => "Line",
		"description" => "Description",
		"file" => "File",
		"project" => "Project",
		"path" => "Path",
		"category" => "Category",
		_ => "",
	};

	static bool IsSortable (string key) => key is "type" or "project" or "file" or "category";

	/// <summary>Rebuilds the shared (icon, check, line, description, file, project,
	/// path, category) column shape for the header and every visible row, hiding any
	/// column the user toggled off (zero width keeps the grid indexes aligned).</summary>
	void ApplyColumnDefinitions ()
	{
		headerGrid.ColumnDefinitions = MakeColumnDefinitions ();
		foreach (var view in rowToView.Values)
			view.SetColumns (MakeColumnDefinitions ());
	}

	ColumnDefinitions MakeColumnDefinitions ()
	{
		// New instance per grid: the header and each row must not share a collection.
		var defs = ColumnDefinitions.Parse ("24,28,56,3*,2*,2*,2*,2*");
		for (int i = 0; i < defs.Count; i++) {
			if (!visibleKeys.Contains (Columns [i].Key))
				defs [i] = new ColumnDefinition (new GridLength (0));
		}
		return defs;
	}

	void ToggleSort (string key)
	{
		// Legacy TreeModelSort: same column flips direction, a new column starts
		// ascending.
		if (sortKey == key)
			sortAsc = !sortAsc;
		else {
			sortKey = key;
			sortAsc = true;
		}
		RebuildRows ();
	}

	// Legacy sortable headers show a direction indicator on the active column.
	void UpdateHeaderSortIndicators ()
	{
		foreach (var (key, lbl) in headerLabels) {
			if (!IsSortable (key))
				continue;
			string arrow = key == sortKey ? (sortAsc ? "\u25B2 " : "\u25BC ") : "";
			lbl.Text = arrow + key switch {
				"type" => "!",
				"project" => "Project",
				"file" => "File",
				"category" => "Category",
				_ => "",
			};
		}
	}

	// ---- Rows ----

	void QueueRebuild ()
	{
		// Coalesce to one rebuild per dispatcher turn (a build emits many rows).
		if (rebuildPending)
			return;
		rebuildPending = true;
		Dispatcher.UIThread.Post (() => {
			rebuildPending = false;
			RebuildRows ();
		}, DispatcherPriority.Background);
	}

	void RebuildRows ()
	{
		rowToView.Clear ();
		var items = new List<ErrorRowView> ();
		foreach (var row in OrderedRows ()) {
			if (!Matches (row))
				continue;
			var view = new ErrorRowView (row, visibleKeys, severityIcons, MakeColumnDefinitions ());
			// Legacy CellRendererToggle → ItemToggled: the checkbox flips
			// task.Completed (struck-through row) without selecting the row.
			view.Check.Click += (_, _) => {
				row.Completed = view.Check.IsChecked == true;
				view.Apply (row, visibleKeys, severityIcons);
			};
			rowToView [row] = view;
			items.Add (view);
		}
		rowsList.ItemsSource = items;
		UpdateHeaderSortIndicators ();
	}

	void RepaintRows ()
	{
		foreach (var view in rowToView.Values)
			view.Apply (view.Tag as ErrorRow, visibleKeys, severityIcons);
	}

	IEnumerable<ErrorRow> OrderedRows ()
	{
		if (sortKey is null)
			return allRows;
		string key (ErrorRow r) => sortKey switch {
			"type" => Bucket (r.Level).ToString ("D3"),
			"project" => r.Project,
			"file" => r.File,
			"category" => r.Category,
			_ => "",
		};
		// Stable: ties keep arrival order (the user's expectation within a severity).
		var indexed = allRows.Select ((r, i) => (r, i)).ToList ();
		return sortAsc
			? indexed.OrderBy (x => key (x.r), StringComparer.Ordinal).ThenBy (x => x.i).Select (x => x.r)
			: indexed.OrderByDescending (x => key (x.r), StringComparer.Ordinal).ThenBy (x => x.i).Select (x => x.r);
	}

	static int Bucket (string level) => level switch {
		"error" => 0,
		"warning" => 1,
		_ => 2,
	};

	// ---- Filtering (legacy FilterTasks + searchPatternChanged) ----

	bool Matches (ErrorRow row)
	{
		int b = Bucket (row.Level);
		bool show = b == 0 ? errorBtn.IsChecked == true
			: b == 1 ? warnBtn.IsChecked == true
			: msgBtn.IsChecked == true;
		if (!show)
			return false;
		var pattern = searchBox.Text;
		if (string.IsNullOrWhiteSpace (pattern))
			return true;
		// Legacy FilterTasks matches description/code/file/project/category.
		return (row.Message?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
		       (row.Code?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
		       (row.File?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
		       (row.Project?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
		       (row.Category?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
	}

	// ---- Toolbar counts / pad icon (legacy Update*Num / UpdatePadIcon) ----

	void UpdateCounts ()
	{
		errorLbl.Text = $" {Plural (errorCount, "Error", "Errors")}";
		warnLbl.Text = $" {Plural (warningCount, "Warning", "Warnings")}";
		msgLbl.Text = $" {Plural (messageCount, "Message", "Messages")}";
	}

	static string Plural (int n, string one, string many) => n == 1 ? $"{n} {one}" : $"{n} {many}";

	void UpdatePadIcon ()
	{
		string icon;
		if (errorCount > 0)
			icon = "md-errors-list-has-errors";
		else if (warningCount > 0)
			icon = "md-errors-list-has-warnings";
		else
			icon = "md-errors-list";
		PadIconChanged?.Invoke (icon);
	}

	void SetLogPanelVisible (bool visible)
	{
		if (logHost.IsVisible == visible)
			return;
		logHost.IsVisible = visible;
		logSplitter.IsVisible = visible;
		// Log column 2: 0 when hidden (table takes everything), 1* when shown
		// (shares the width with the table, column 0 also 1*).
		bodyGrid.ColumnDefinitions [2].Width = visible ? new GridLength (1, GridUnitType.Star) : new GridLength (0);
		if (visible) {
			// Legacy SetInitialOutputViewSize: the saved separator applies now
			// when the body already has a width (runtime toggle); otherwise it
			// waits for the first layout pass (initialLogShow).
			initialLogShow = bodyGrid.Bounds.Width <= 0;
			ApplyLogSeparator ();
		} else {
			bodyGrid.ColumnDefinitions [0].Width = new GridLength (1, GridUnitType.Star);
		}
		SettingsStore.SetBool (OutputVisibleKey, visible);
		LogToggled?.Invoke (visible);
	}

	// Legacy SetInitialOutputViewSize: apply the saved separator (0.5 default)
	// as a fraction of the pad width, clamped to a usable band. No-op when the
	// body has not been laid out yet.
	void ApplyLogSeparator ()
	{
		var w = bodyGrid.Bounds.Width;
		if (w <= 0 || !logHost.IsVisible)
			return;
		var r = double.TryParse (SettingsStore.GetString (LogSeparatorKey),
			System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.5;
		bodyGrid.ColumnDefinitions [0].Width = new GridLength (Math.Max (200, Math.Min (w * r, w - 220)));
	}

	// Legacy HandleSwSizeAllocated: persist the current separator as a fraction
	// of the pad width. The GridSplitter commits column 0 to a pixel width before
	// DragCompleted fires, so the committed Width (not the last layout's
	// ActualWidth) is the new position; a star width means the user never moved
	// the separator (nothing to save).
	void PersistLogSeparator ()
	{
		var len = bodyGrid.ColumnDefinitions [0].Width;
		if (len.IsStar)
			return;
		var w = bodyGrid.Bounds.Width;
		if (w <= 0)
			return;
		SettingsStore.SetString (LogSeparatorKey,
			(len.Value / w).ToString ("R", System.Globalization.CultureInfo.InvariantCulture));
	}

	ToggleButton MakeToolbarToggle (string stock, string tooltip, bool isChecked, out TextBlock label, out Image img)
	{
		img = new Image { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center };
		if (IconService.GetImage (stock) is Bitmap bmp)
			img.Source = bmp;
		label = new TextBlock {
			FontSize = 11,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness (4, 0, 2, 0),
		};
		label.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
		content.Children.Add (img);
		content.Children.Add (label);
		var btn = new ToggleButton {
			Content = content,
			IsChecked = isChecked,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
			Padding = new Thickness (5, 3),
		};
		ToolTip.SetTip (btn, tooltip);
		return btn;
	}

	// ---- Columns (legacy ColumnSelectorMenu, persisted per the restore id) ----

	List<string> LoadColumnVisibility ()
	{
		var stored = SettingsStore.GetString (ColumnsKey);
		if (stored is not null) {
			var tokens = stored.Split (';', StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == Columns.Length) {
				var keys = new List<string> ();
				for (int i = 0; i < tokens.Length; i++) {
					if (bool.TryParse (tokens [i], out var visible) && visible)
						keys.Add (Columns [i].Key);
				}
				if (keys.Count > 0)
					return keys;
			}
		}
		// Legacy LoadColumnsVisibility default: every column TRUE.
		return Columns.Select (c => c.Key).ToList ();
	}

	void ToggleColumn (string key)
	{
		if (visibleKeys.Contains (key)) {
			// Never leave the table without a column.
			if (visibleKeys.Count == 1)
				return;
			visibleKeys.Remove (key);
		} else {
			var idx = Array.FindIndex (Columns, c => c.Key == key);
			visibleKeys.Insert (idx < 0 ? visibleKeys.Count : idx, key);
		}
		// Persist in the legacy token form: one TRUE/FALSE per column, in order.
		SettingsStore.SetString (ColumnsKey,
			string.Join (";", Columns.Select (c => visibleKeys.Contains (c.Key) ? "TRUE" : "FALSE")));
		ApplyColumnDefinitions ();
	}

	// ---- Context menu (legacy ShowPopup) ----

	ContextMenu BuildContextMenu ()
	{
		var miRef = new MenuItem { Header = "Go to Reference" };
		var miJump = new MenuItem { Header = "Go to Task" };
		var miCopy = new MenuItem { Header = "Copy" };
		var miCols = new MenuItem { Header = "Columns" };
		foreach (var (label, key) in Columns) {
			var item = new MenuItem { Header = label, IsChecked = visibleKeys.Contains (key) };
			item.Click += (_, _) => {
				ToggleColumn (key);
				item.IsChecked = visibleKeys.Contains (key);
			};
			miCols.Items.Add (item);
		}
		miRef.Click += (_, _) => GoToReference ();
		miJump.Click += (_, _) => {
			var row = SelectedRow ();
			if (row is not null)
				RowActivated?.Invoke (row);
		};
		miCopy.Click += (_, _) => CopySelected ();
		var menu = new ContextMenu {
			Items = { miRef, miJump, new Separator (), miCopy, new Separator (), miCols },
		};
		// Legacy: Copy enabled with a selection; Go to Reference/Go to Task only
		// with exactly one row selected (multiple rows cannot jump).
		menu.Opened += (_, _) => {
			var sel = SelectedRows ();
			miCopy.IsEnabled = sel.Count > 0;
			miJump.IsEnabled = sel.Count == 1;
			miRef.IsEnabled = sel.Count == 1 && !string.IsNullOrEmpty (sel [0].Code);
		};
		return menu;
	}

	List<ErrorRow> SelectedRows ()
		=> rowsList.SelectedItems.OfType<ErrorRowView> ().Select (v => v.Tag as ErrorRow).Where (r => r is not null).Cast<ErrorRow> ().ToList ();

	ErrorRow? SelectedRow ()
	{
		var rows = SelectedRows ();
		return rows.Count == 1 ? rows [0] : null;
	}

	void GoToReference ()
	{
		var row = SelectedRow ();
		if (row is null || string.IsNullOrEmpty (row.Code))
			return;
		// Legacy GetSelectedErrorReference: the code becomes a google search link,
		// opened through the platform hooks (xdg-open / open / shell-execute).
		var url = "http://google.com/search?q=" + Uri.EscapeDataString (row.Code);
		try {
			if (App.Platform is { } platform)
				platform.ShowUrl (url);
			else
				System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo (url) { UseShellExecute = true });
		} catch (Exception ex) {
			Console.WriteLine ("[errors] open reference failed: " + ex.Message);
		}
	}

	void CopySelected ()
	{
		var rows = SelectedRows ();
		if (rows.Count == 0)
			return;
		// Legacy OnTaskCopied: "path(line,col): Severity CODE: message (project) category".
		var sb = new StringBuilder ();
		for (int i = 0; i < rows.Count; i++) {
			if (i > 0)
				sb.Append (Environment.NewLine);
			var row = rows [i];
			if (!string.IsNullOrEmpty (row.File)) {
				sb.Append (row.File);
				if (row.Line >= 1)
					sb.Append ("(").Append (row.Line).Append (",").Append (row.Col).Append (")");
				sb.Append (": ");
			}
			sb.Append (row.Level == "error" ? "Error" : row.Level == "warning" ? "Warning" : "Information");
			if (!string.IsNullOrEmpty (row.Code))
				sb.Append (" ").Append (row.Code);
			sb.Append (": ").Append (row.Message);
			if (!string.IsNullOrEmpty (row.Project))
				sb.Append (" (").Append (row.Project).Append (")");
			if (!string.IsNullOrEmpty (row.Category))
				sb.Append (" ").Append (row.Category);
		}
		var clipboard = TopLevel.GetTopLevel (Root)?.Clipboard;
		_ = clipboard?.SetTextAsync (sb.ToString ());
	}
}

/// <summary>One visible row of the Errors table: a Grid sharing the (icon, check,
/// line, description, file, project, path, category) shape of the header so the
/// columns line up, inside a Border (the rows have a bottom rule like the legacy
/// RulesHint tree). Applies the legacy cell rules — severity icon, "Marked" checkbox,
/// line number, wrapped description, file name, project, relative path, category —
/// plus the bold-for-unread (DataColumns.Read) and strikethrough-for-completed
/// (CellRendererToggle) states.</summary>
sealed class ErrorRowView : Border
{
	Grid grid;

	public Image Icon { get; }
	public CheckBox Check { get; }
	public TextBlock LineText { get; }
	public TextBlock DescText { get; }
	public TextBlock FileText { get; }
	public TextBlock ProjectText { get; }
	public TextBlock PathText { get; }
	public TextBlock CategoryText { get; }

	public object? Tag { get; set; }

	public ErrorRowView (ErrorsPad.ErrorRow row, IReadOnlyList<string> visible, (IImage?, IImage?, IImage?) icons, ColumnDefinitions defs)
	{
		Tag = row;
		grid = new Grid { ColumnDefinitions = defs };
		Child = grid;
		BorderThickness = new Thickness (0, 0, 0, 1);
		BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!;

		Icon = new Image { Width = 16, Height = 16, Margin = new Thickness (0, 2), VerticalAlignment = VerticalAlignment.Center };
		AddCell (Icon, 0, visible);

		// The "Marked" checkbox toggles the row's completed state; the click is
		// owned by the checkbox so the row is not selected too. The ListBox item
		// container would otherwise forward the click as a selection change.
		Check = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness (0, 2) };
		Check.PointerPressed += (_, e) => e.Handled = true;
		AddCell (Check, 1, visible);

		LineText = MakeText ();
		AddCell (LineText, 2, visible);
		DescText = MakeText (wrap: true);
		AddCell (DescText, 3, visible);
		FileText = MakeText ();
		AddCell (FileText, 4, visible);
		ProjectText = MakeText ();
		AddCell (ProjectText, 5, visible);
		PathText = MakeText ();
		AddCell (PathText, 6, visible);
		CategoryText = MakeText ();
		AddCell (CategoryText, 7, visible);

		Apply (row, visible, icons);
	}

	public void SetColumns (ColumnDefinitions defs) => grid.ColumnDefinitions = defs;

	TextBlock MakeText (bool wrap = false)
	{
		var tb = new TextBlock {
			FontSize = 12,
			Margin = new Thickness (4, 2),
			VerticalAlignment = VerticalAlignment.Center,
			TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
		};
		tb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		return tb;
	}

	void AddCell (Control cell, int col, IReadOnlyList<string> visible)
	{
		cell.IsVisible = col < ErrorsPad.Columns.Length && visible.Contains (ErrorsPad.Columns [col].Key);
		Grid.SetColumn (cell, col);
		grid.Children.Add (cell);
	}

	public void Apply (ErrorsPad.ErrorRow? row, IReadOnlyList<string> visible, (IImage?, IImage?, IImage?) icons)
	{
		if (row is null)
			return;
		Tag = row;
		Icon.Source = row.Level switch {
			"error" => icons.Item1,
			"warning" => icons.Item2,
			_ => icons.Item3,
		};
		Check.IsChecked = row.Completed;
		LineText.Text = row.Line != 0 ? row.Line.ToString () : "";
		DescText.Text = row.Message;
		FileText.Text = Path.GetFileName (row.File);
		ProjectText.Text = row.Project;
		PathText.Text = row.Path;
		CategoryText.Text = row.Category;
		bool unread = !row.Read;
		foreach (var tb in new[] { LineText, DescText, FileText, ProjectText, CategoryText }) {
			tb.FontWeight = unread ? FontWeight.Bold : FontWeight.Normal;
			tb.TextDecorations = row.Completed ? TextDecorations.Strikethrough : null;
		}
	}
}
