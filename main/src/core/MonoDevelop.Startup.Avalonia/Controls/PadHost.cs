using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Controls;

/// <summary>
/// Dockable pad host like the legacy DockFrame pads: a header with the pad title
/// and a hide (✕) button, a row of selectable tabs and one shared content area.
/// Hidden pads are restored from View > View List / the edge restore strip owned
/// by MainWindow (legacy pad hide/pin behavior).
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
	}

	readonly StackPanel tabBar;
	readonly ContentControl content;
	readonly List<PadTab> tabs = new ();

	public event EventHandler? Hidden;

	// Parameterless ctor for XAML instantiation; Id/Title are then set by the owner.
	public PadHost () : this ("", "") { }

	public PadHost (string id, string title, params PadTab [] initialTabs)
	{
		Id = id;
		Title = title;

		// Header: pad title left, hide button right.
		var hideButton = new Button {
			Content = "\u2715",
			FontSize = 10,
			Padding = new Thickness (6, 1),
			VerticalAlignment = VerticalAlignment.Center,
		};
		ToolTip.SetTip (hideButton, "Hide pad");
		hideButton.Click += (_, _) => {
			IsVisible = false;
			Hidden?.Invoke (this, EventArgs.Empty);
		};

		var headerLabel = new TextBlock {
			Text = title,
			FontWeight = FontWeight.SemiBold,
			FontSize = 12,
			Margin = new Thickness (8, 4),
			VerticalAlignment = VerticalAlignment.Center,
		};
		headerLabel.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));

		var header = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (hideButton, Dock.Right);
		header.Children.Add (hideButton);
		header.Children.Add (headerLabel);

		var headerBorder = new Border {
			Child = header,
			BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (0, 0, 0, 1),
		};

		// Tab strip (pad tabs, VS/MD style).
		tabBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
		var tabBarHost = new Border { Child = tabBar, Padding = new Thickness (4, 3) };

		content = new ContentControl { Margin = new Thickness (0) };

		var grid = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,Auto,*") };
		Grid.SetRow (headerBorder, 0);
		Grid.SetRow (tabBarHost, 1);
		Grid.SetRow (content, 2);
		grid.Children.Add (headerBorder);
		grid.Children.Add (tabBarHost);
		grid.Children.Add (content);

		Child = grid;
		Background = (Brush)Application.Current.FindResource ("IdeWindowBgBrush")!;
		BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!;

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

		// Pad tabs carry the pad icon + label, like the legacy DockItem tabstrip.
		object content = tab.Label;
		if (tab.Icon is not null && IconService.GetImage (tab.Icon) is Bitmap bmp) {
			var label = new TextBlock {
				Text = tab.Label,
				FontSize = 11,
				VerticalAlignment = VerticalAlignment.Center,
			};
			label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			content = new StackPanel {
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children = {
					new Avalonia.Controls.Image { Source = bmp, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center },
					label,
				},
			};
		}

		var btn = new ToggleButton {
			Content = content,
			FontSize = 11,
			Padding = new Thickness (8, 3),
			CornerRadius = new CornerRadius (3),
		};
		tab.HeaderButton = btn;
		btn.Click += (_, _) => Select (tab.Id);
		tabBar.Children.Add (btn);
		if (!tab.Visible)
			btn.IsVisible = false;
	}

	public void SetTabVisible (string tabId, bool visible)
	{
		var t = tabs.FirstOrDefault (x => x.Id == tabId);
		if (t is null)
			return;
		t.Visible = visible;
		if (t.HeaderButton is not null)
			t.HeaderButton.IsVisible = visible;
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
		content.Content = t.Content;
		foreach (var x in tabs) {
			if (x.HeaderButton is not null)
				x.HeaderButton.IsChecked = x.Id == tabId;
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
			Select (tabId); // re-select refreshes the content host
	}
}
