using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.Ide.Controls;

/// <summary>
/// Dockable pad host like the legacy DockFrame pads: a row of selectable tabs and
/// one shared content area. Wave-3 chrome: no header/close bar; a collapse chevron
/// at the tab-row edge collapses the pad to a narrow rail of rotated tabs on its
/// dock edge; clicking the chevron (or any rail tab) restores it. Both tab
/// containers (horizontal strip and vertical rail) are built ONCE per tab and
/// shown/hidden as a whole — nothing is re-parented at collapse time, so collapse
/// and restore are O(1) and cannot trip "already has a parent".
/// </summary>
public class PadHost : Border
{
	/// <summary>Raised when a pad tab's close button hides it (the shell logs and
	/// can update the View > Pads checks like the legacy DockItem.Closed).</summary>
	public event Action<string>? PadTabClosed;

	/// <summary>Raised after the pad collapsed or expanded (true = collapsed) so
	/// the owning shell can resize the dock row: a collapsed bottom pad pins its
	/// row to the 34px rail (no dead gap under the splitter) and expanding
	/// returns the row to its previous height.</summary>
	public event Action<bool>? CollapseChanged;

	public sealed class PadTab
	{
		public string Id = "";
		public string Label = "";
		public string? Icon;
		public object? Content;
		public bool Visible = true;
		internal ToggleButton? HeaderButton;
		internal Control? RailButton;
	}

	readonly StackPanel tabBar;          // horizontal strip (expanded)
	readonly StackPanel railBar;         // vertical rail (collapsed)
	readonly ScrollViewer tabScroller;
	readonly ScrollViewer railScroller;
	readonly ContentControl content;
	readonly List<PadTab> tabs = new ();
	readonly Border tabBarHost;
	readonly Border railHost;
	readonly Border contentHost;
	readonly TextBlock collapseButton;
	readonly Border collapseWrap;
	readonly Border chevronRow;
	readonly Grid rootGrid;
	Grid? collapsedPanelHost;
	DockPanel? tabRowHost;
	TextBlock? restoreButton;

	public event EventHandler? Hidden;

	// Dock edge this host sits on — Horizontal = bottom dock, Vertical = side dock.
	public Orientation DockOrientation { get; set; } = Orientation.Vertical;

	bool collapsed;
	// The ✕ lives INSIDE the tab's ToggleButton, and the toggle's own Click
	// (rail-restore when collapsed) fires after the ✕'s in the same release
	// dispatch — without this flag a last-tab ✕ collapse was instantly undone.
	bool lastClickWasClose;
	double savedWidth = double.NaN;
	double savedHeight = double.NaN;
	public bool IsCollapsed => collapsed;

	// Parameterless ctor for XAML instantiation; Id/Title are then set by the owner.
	public PadHost () : this ("", "") { }

	public PadHost (string id, string title, params PadTab [] initialTabs)
	{
		Id = id;
		Title = title;

		// Expanded tab strip: horizontal, scroller for long tab sets.
		tabBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
		tabScroller = new ScrollViewer {
			Content = tabBar,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
		};

		// Collapsed rail: vertical stack of rotated tab buttons, pre-built per tab.
		railBar = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
		railScroller = new ScrollViewer {
			Content = railBar,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
		};
		railHost = new Border {
			Child = railScroller,
			Padding = new Thickness (2, 3),
			IsVisible = false,
		};
		// Rail-wide tap handling: the rail host owns the click (full-width rows,
		// like the restore row) and resolves WHICH tab was tapped by hit-testing
		// the rail buttons' bounds — the toggle templates' own hit region proved
		// unreliable (InputHitTest missed buttons across their whole rect).
		railHost.Tapped += (_, e) => {
			e.Handled = true;
			ToggleCollapse ("rail"); // expand
			var p = e.GetPosition (railBar);
			foreach (var t in tabs) {
				if (t.RailButton is { } rb && rb.Bounds.Contains (p)) {
					Select (t.Id);
					break;
				}
			}
		};

		// Display-only glyph: the Fluent Button template swallowed the '‹' (the
		// wrapper Border painted, the button's content never did), so the glyph is
		// a plain TextBlock with an explicit Foreground — TextBlock does not
		// reliably inherit one across the non-Control Border wrapper.
		collapseButton = new TextBlock {
			Text = "\u2039",
			FontSize = 12,
			Foreground = (Brush)Application.Current.FindResource ("IdeFgBrush")!,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		collapseWrap = new Border {
			Child = collapseButton,
			Background = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			Cursor = new Cursor (StandardCursorType.Hand),
			// Same width as the collapsed state's expand chevron: a 34px-wide,
			// unmissable hit target whether the pad is expanded or collapsed.
			MinWidth = 34,
		};
		ToolTip.SetTip (collapseWrap, "Collapse pad");
		collapseWrap.Tapped += (_, e) => { e.Handled = true; ToggleCollapse ("chevron"); };

		tabBarHost = new Border {
			Child = tabScroller,
			Padding = new Thickness (4, 3),
			BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (0, 0, 0, 1),
		};

		// Collapsed layout: a 2-row panel with a full-width restore button on its own
		// row above the icon rail — a wide, unmissable click target (the docked-right
		// chevron in the shared tab row was compressed to ~10px by the strip and its
		// glyph clipped to 2px, so real user clicks missed it). A SECOND button
		// instance lives here: one control cannot be parented by two containers.
		// Display-only glyph (same TextBlock rationale as collapseButton above);
		// the row Border owns the click across its whole width.
		restoreButton = new TextBlock {
			Text = "\u203a",
			FontSize = 12,
			Foreground = (Brush)Application.Current.FindResource ("IdeFgBrush")!,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		chevronRow = new Border {
			Child = restoreButton,
			Padding = new Thickness (2, 2),
			Background = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Cursor = new Cursor (StandardCursorType.Hand),
		};
		ToolTip.SetTip (chevronRow, "Expand pad");
		chevronRow.Tapped += (_, e) => { e.Handled = true; ToggleCollapse ("restore"); };
		// 2x2 grid: (0,0) expand corner — full-width row on side docks, a SQUARE
		// 34x34 corner on the bottom dock — and the icon rail filling the rest.
		var collapsedPanel = new Grid {
			RowDefinitions = RowDefinitions.Parse ("Auto,*"),
			ColumnDefinitions = ColumnDefinitions.Parse ("Auto,*"),
		};
		Grid.SetRow (chevronRow, 0);
		Grid.SetColumn (chevronRow, 0);
		Grid.SetRow (railHost, 1);
		Grid.SetColumn (railHost, 0);
		collapsedPanel.Children.Add (chevronRow);
		collapsedPanel.Children.Add (railHost);

		// Expanded layout: the tab row is a DockPanel — the collapse chevron docked
		// RIGHT (it must render: the wrapper Border was always painted there, only
		// the old Button template swallowed its glyph), the strip fills the rest.
		// All layout containers are permanent grid children; only IsVisible flips.
		var tabRow = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (collapseWrap, Dock.Right);
		tabRow.Children.Add (collapseWrap);
		tabRow.Children.Add (tabBarHost);

		content = new ContentControl { Margin = new Thickness (0) };
		contentHost = new Border { Child = content, IsVisible = true };

		rootGrid = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,*") };
		Grid.SetRow (tabRow, 0);
		Grid.SetRow (contentHost, 1);
		rootGrid.Children.Add (tabRow);
		rootGrid.Children.Add (contentHost);
		collapsedPanel.IsVisible = false;
		Grid.SetRow (collapsedPanel, 0);
		rootGrid.Children.Add (collapsedPanel);
		collapsedPanelHost = collapsedPanel;
		tabRowHost = tabRow;

		Child = rootGrid;
		Background = (Brush)Application.Current.FindResource ("IdeWindowBgBrush")!;
		BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!;
		BorderThickness = new Thickness (0, 0, 1, 0); // thin toolbar-color frame

		foreach (var t in initialTabs)
			AddTab (t);
		if (tabs.Count > 0)
			Select (tabs [0].Id);
	}

	public string Id { get; set; }
	public string Title { get; set; }

	public IReadOnlyList<PadTab> Tabs => tabs;

	public string? SelectedId { get; private set; }

	public void AddTab (PadTab tab)
	{
		if (tabs.Any (t => t.Id == tab.Id))
			return;
		tabs.Add (tab);
		if (tabs.Count == 1 && tab.Visible)
			Select (tab.Id);

		// One header content per state: icon + label + close (strip) / rotated (rail).
		// The close button HIDES the pad tab (legacy DockItem closed → hidden, not
		// removed — View > Pads brings it back). It exists for EVERY tab — an
		// unresolvable icon stock id (e.g. Properties) must not also remove the ✕.
		var label = new TextBlock {
			Text = tab.Label,
			FontSize = 11,
			VerticalAlignment = VerticalAlignment.Center,
		};
		label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var closeBtn = new Button {
			Content = "\u2715",
			FontSize = 9,
			Padding = new Thickness (2, 0),
			MinWidth = 16,
			MinHeight = 16,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
			VerticalAlignment = VerticalAlignment.Center,
			Tag = tab.Id,
		};
		ToolTip.SetTip (closeBtn, "Close pad");
		closeBtn.Click += (_, _) => {
			lastClickWasClose = true;
			SetTabVisible (tab.Id, false);
			PadTabClosed?.Invoke (tab.Id);
			// The owning ToggleButton (registered for handled events too) raises its
			// own Click AFTER this handler within the same release dispatch — clear
			// the guard only once that pass is done.
			Avalonia.Threading.Dispatcher.UIThread.Post (() => lastClickWasClose = false,
				Avalonia.Threading.DispatcherPriority.Background);
		};
		var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
		if (tab.Icon is not null && IconService.GetImage (tab.Icon) is Bitmap bmp) {
			headerRow.Children.Add (new Avalonia.Controls.Image { Source = bmp, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center });
		}
		headerRow.Children.Add (label);
		headerRow.Children.Add (closeBtn);
		object headerContent = headerRow;

		var btn = new ToggleButton {
			Content = headerContent,
			FontSize = 11,
			Padding = new Thickness (8, 3),
			CornerRadius = new CornerRadius (3),
			Tag = tab.Id,
		};
		tab.HeaderButton = btn;
		btn.Click += (_, _) => {
			if (collapsed && !lastClickWasClose)
				ToggleCollapse ("rail-tab"); // rail click restores (legacy pinned-pad behavior)
			Select (tab.Id);
		};
		tabBar.Children.Add (btn);
		if (!tab.Visible)
			btn.IsVisible = false;

		// Rail twin: icon-only rotated toggle, bound to the same state.
		var rail = new ToggleButton {
			Content = new Avalonia.Controls.Image {
				Width = 16, Height = 16,
				Source = (tab.Icon is not null ? IconService.GetImage (tab.Icon) : null) ?? IconService.GetImage ("md-plugin"),
			},
			FontSize = 11,
			Padding = new Thickness (4, 6),
			CornerRadius = new CornerRadius (3),
			Tag = tab.Id,
		};
		ToolTip.SetTip (rail, tab.Label);
		// Rail-wide Tapped: the rail host owns the click (full-width rows, like the
		// restore row) and resolves WHICH tab was tapped by hit-testing the rail
		// buttons' bounds — the toggle templates' own hit region is unreliable.
		rail.IsHitTestVisible = false;
		tab.RailButton = rail;
		railBar.Children.Add (rail);
		if (!tab.Visible)
			rail.IsVisible = false;
	}

	public void SetTabVisible (string tabId, bool visible)
	{
		var t = tabs.FirstOrDefault (x => x.Id == tabId);
		if (t is null)
			return;
		t.Visible = visible;
		if (t.HeaderButton is not null)
			t.HeaderButton.IsVisible = visible;
		if (t.RailButton is not null)
			t.RailButton.IsVisible = visible;
		if (!visible && SelectedId == tabId) {
			var first = tabs.FirstOrDefault (x => x.Visible);
			if (first is not null)
				Select (first.Id);
		} else if (visible) {
			Select (tabId);
		}
	}

	public void Select (string tabId)
	{
		var t = tabs.FirstOrDefault (x => x.Id == tabId && x.Visible);
		if (t is null)
			return;
		SelectedId = tabId;
		content.Content = t.Content;		foreach (var x in tabs) {
			if (x.HeaderButton is not null)
				x.HeaderButton.IsChecked = x.Id == tabId;
			if (x.RailButton is ToggleButton rb)
				rb.IsChecked = x.Id == tabId;
			if (x.RailButton is not null)
				x.RailButton.Opacity = x.Id == tabId ? 1 : 0.9;
				
		}
	}

	/// <summary>Shows/hides the whole pad host (ViewCommands Single/SideBySide mode).</summary>
	public void SetHostVisible (bool visible) => IsVisible = visible;

	public bool IsTabVisible (string tabId)
		=> tabs.FirstOrDefault (x => x.Id == tabId) is { Visible: var v } && v;

	/// <summary>Replaces the content of an existing tab (e.g. new search results run).</summary>
	public void ReplaceTabContent (string tabId, Control content)
	{
		var t = tabs.FirstOrDefault (x => x.Id == tabId);
		if (t is null)
			return;
		t.Content = content;
		if (SelectedId == tabId)
			Select (tabId);
	}

	/// <summary>
	/// Collapse/restore: swap the strip for the rail and hide the content. Widths
	/// are saved/restored so the pad returns to its exact previous size.
	/// </summary>
	public void ToggleCollapse (string? reason = null)
	{
		collapsed = !collapsed;
		ApplyCollapseState ();
		// QA (log-only when a console exists): every collapse/restore transition
		// with its trigger and the button rects at that moment — distinguishes
		// "the click landed outside the button" from "the handler never ran".
		Console.WriteLine ($"[padhost] '{Id}' toggle({reason ?? "api"}) → collapsed={collapsed} restoreBtn={(restoreButton is null ? "null" : $"{restoreButton.Bounds.Width:F0}x{restoreButton.Bounds.Height:F0} visible={restoreButton.IsVisible}")}");
		Console.Out.Flush ();
	}

	void ApplyCollapseState ()
	{
		// Expanded: tab strip row (chevron docked right). Collapsed on side docks:
		// dedicated panel (expand corner + icon rail). Collapsed on the bottom dock:
		// the SAME tab row stays visible with the chevron at its right end and the
		// content hidden — the tabs never move. Everything is a permanent grid
		// child; only visibility flips.
		var collapsedPanel = collapsedPanelHost;
		var tabRow = tabRowHost;
		var bottom = DockOrientation == Orientation.Horizontal;
		if (collapsedPanel is not null)
			collapsedPanel.IsVisible = collapsed && !bottom;
		if (tabRow is not null)
			tabRow.IsVisible = !collapsed || bottom;
		collapseWrap.IsVisible = !collapsed || bottom;
		railHost.IsVisible = collapsed && !bottom;
		contentHost.IsVisible = !collapsed;

		if (collapsed) {
			if (DockOrientation == Orientation.Vertical) {
				// Side dock: 34px rail column — full-width expand row above a vertical
				// icon stack.
				savedWidth = Width;
				Width = 34;
				chevronRow.Width = double.NaN;
				chevronRow.Height = double.NaN;
				chevronRow.HorizontalAlignment = HorizontalAlignment.Stretch;
				railBar.Orientation = Orientation.Vertical;
				railHost.Width = 34;
				Grid.SetColumn (contentHost, 0);
			} else {
				// Bottom dock: only the content collapses away — the tab row keeps its
				// exact position with the expand chevron at its right end (the wrap's
				// fixed 34px width makes the hit zone identical to the expanded state);
				// no rail, no second row, nothing else changes. The pad lives in a
				// splitter-resized grid row, so "collapsed" pins THIS control to 34px
				// anchored bottom (no fixed XAML height to fight the splitter with).
				savedHeight = Height;
				Height = 34;
				VerticalAlignment = VerticalAlignment.Bottom;
				Grid.SetColumn (contentHost, 0);
			}
		} else {
			if (DockOrientation == Orientation.Vertical) {
				Width = savedWidth;
			} else {
				// Bottom dock restore: back to the full-height pad — Height returns to
				// whatever it was (NaN = follow the splitter-resized grid row) and the
				// alignment back to stretch. The tab row (with its 34px chevron) was
				// never hidden or resized.
				Height = savedHeight;
				VerticalAlignment = VerticalAlignment.Stretch;
				Grid.SetColumn (contentHost, 0);
				chevronRow.Width = double.NaN;
				chevronRow.Height = double.NaN;
				chevronRow.HorizontalAlignment = HorizontalAlignment.Stretch;
			}
		}
		CollapseChanged?.Invoke (collapsed);
		LogCollapseChrome ();
	}

	/// <summary>QA: logs the on-screen rects of both collapse click targets
	/// (strip chevron and collapsed restore row) so XTEST clicks and pixel
	/// checks use measured coordinates instead of guesses.</summary>
	public void LogCollapseChrome ()
	{
		foreach (var b in this.GetVisualDescendants ().OfType<Border> ()) {
			if (ToolTip.GetTip (b) is not string tip || (tip != "Collapse pad" && tip != "Expand pad"))
				continue;
			var tl = b.PointToScreen (new Point (0, 0));
			Console.WriteLine ($"[padchrome] '{Id}' {tip}: bounds={b.Bounds.Width:F0}x{b.Bounds.Height:F0} visible={b.IsVisible} screen=({tl.X},{tl.Y})");
		}
		// QA: layout audit — every direct child of the root grid with its row and
		// measured bounds, plus the tab row's own children, catches "the chevron
		// is present but laid out outside the visible band".
		Console.Out.Flush ();
	}
}
