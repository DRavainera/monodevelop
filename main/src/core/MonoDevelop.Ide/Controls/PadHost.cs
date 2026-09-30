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
	readonly Button collapseButton;
	readonly Border collapseWrap;
	readonly Border chevronRow;
	readonly Grid rootGrid;
	Grid? collapsedPanelHost;
	DockPanel? tabRowHost;
	Button? restoreButton;

	public event EventHandler? Hidden;

	// Dock edge this host sits on — Horizontal = bottom dock, Vertical = side dock.
	public Orientation DockOrientation { get; set; } = Orientation.Vertical;

	bool collapsed;
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
			ToggleCollapse (); // expand
			var p = e.GetPosition (railBar);
			foreach (var t in tabs) {
				if (t.RailButton is { } rb && rb.Bounds.Contains (p)) {
					Select (t.Id);
					break;
				}
			}
		};

		collapseButton = new Button {
			Content = "\u2039",
			FontSize = 11,
			Padding = new Thickness (2, 0),
			MinHeight = 20,
			MinWidth = 18,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Classes = { "chromebtn" },
		};
		ToolTip.SetTip (collapseButton, "Collapse pad");
		collapseButton.Click += (_, _) => ToggleCollapse ();
		// Same fix as the restore row: the Fluent button template only hit-tests
		// at its glyph corners, so a full-surface wrapper owns the click and the
		// glyph button is display-only.
		collapseButton.IsHitTestVisible = false;
		collapseWrap = new Border {
			Child = collapseButton,
			Background = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			Cursor = new Cursor (StandardCursorType.Hand),
		};
		ToolTip.SetTip (collapseWrap, "Collapse pad");
		collapseWrap.Tapped += (_, _) => ToggleCollapse ();

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
		restoreButton = new Button {
			Content = "\u203a",
			FontSize = 11,
			Padding = new Thickness (2, 0),
			MinHeight = 20,
			// The whole row must be the click target: measured bounds proved the
			// button was arranged at its glyph width (~10px) inside the visually
			// wide row, so clicks beside the glyph did nothing.
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Classes = { "chromebtn" },
		};
		ToolTip.SetTip (restoreButton, "Expand pad");
		// The row Border (full width) owns the click: the Fluent button template's
		// hit region proved unreliable (InputHitTest missed the button across its
		// whole rect while the row Border was hittable), so the glyph button is
		// display-only and the row handles Tapped for the entire width.
		restoreButton.IsHitTestVisible = false;
		chevronRow = new Border {
			Child = restoreButton,
			Padding = new Thickness (2, 2),
			Background = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Cursor = new Cursor (StandardCursorType.Hand),
		};
		ToolTip.SetTip (chevronRow, "Expand pad");
		chevronRow.Tapped += (_, _) => ToggleCollapse ();
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

		// Expanded tab row: chevron pinned at the right; the fill slot hosts the
		// strip. Both layouts live in the grid for the whole lifetime — only
		// IsVisible flips, nothing is re-parented at collapse time.
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

		// One header content per state: icon + label (strip) / rotated (rail).
		object headerContent = tab.Label;
		if (tab.Icon is not null && IconService.GetImage (tab.Icon) is Bitmap bmp) {
			var label = new TextBlock {
				Text = tab.Label,
				FontSize = 11,
				VerticalAlignment = VerticalAlignment.Center,
			};
			label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			headerContent = new StackPanel {
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children = {
					new Avalonia.Controls.Image { Source = bmp, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center },
					label,
				},
			};
		}

		var btn = new ToggleButton {
			Content = headerContent,
			FontSize = 11,
			Padding = new Thickness (8, 3),
			CornerRadius = new CornerRadius (3),
			Tag = tab.Id,
		};
		tab.HeaderButton = btn;
		btn.Click += (_, _) => {
			if (collapsed)
				ToggleCollapse (); // rail click restores (legacy pinned-pad behavior)
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
	public void ToggleCollapse ()
	{
		collapsed = !collapsed;
		ApplyCollapseState ();
		// QA (log-only when a console exists): every collapse/restore transition,
		// with the button rects at that moment — distinguishes "the click landed
		// outside the button" from "the handler never ran".
		Console.WriteLine ($"[padhost] '{Id}' toggle → collapsed={collapsed} restoreBtn={(restoreButton is null ? "null" : $"{restoreButton.Bounds.Width:F0}x{restoreButton.Bounds.Height:F0} visible={restoreButton.IsVisible}")}");
		Console.Out.Flush ();
	}

	void ApplyCollapseState ()
	{
		// Expanded: tab strip row (chevron docked right). Collapsed: dedicated panel
		// (expand corner + icon rail). Both are permanent grid children; only
		// visibility flips.
		var collapsedPanel = collapsedPanelHost;
		var tabRow = tabRowHost;
		if (collapsedPanel is not null)
			collapsedPanel.IsVisible = collapsed;
		if (tabRow is not null)
			tabRow.IsVisible = !collapsed;
		railHost.IsVisible = collapsed;
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
				// Bottom dock: the collapsed strip keeps the pad's full width — the
				// expand corner is a SQUARE 34x34 block at the LEFT edge, with the
				// horizontal icon rail beside it (was a full-width squashed row).
				savedHeight = Height;
				Height = 34;
				chevronRow.Width = 34;
				chevronRow.Height = 34;
				chevronRow.HorizontalAlignment = HorizontalAlignment.Left;
				railBar.Orientation = Orientation.Horizontal;
				railHost.Width = 34;
				Grid.SetColumn (contentHost, 1);
			}
		} else {
			if (DockOrientation == Orientation.Vertical) {
				Width = savedWidth;
			} else {
				Height = savedHeight;
				Grid.SetColumn (contentHost, 0);
			}
		}
	}
}
