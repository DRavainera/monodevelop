using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Controls;

/// <summary>
/// Base window for every shell dialog: paints the same Win11-style chrome as
/// MainWindow — rounded corners, thin border in the toolbar color inside a small
/// transparent gutter hosting the 8 edge/corner resize zones, and a title bar
/// row that drags the window (SystemDecorations.None, client area only).
/// Subclasses assign Content as usual; it is placed inside the frame, below the
/// title row. Dialogs that paint their own title row can call SkipTitleRow.
/// </summary>
public class DialogWindow : Window
{
	readonly Border frame;
	readonly Border frameContentHost;
	readonly DockPanel frameDock;
	readonly Grid resizeGrip;
	Border? titleRow;

	public DialogWindow ()
	{
		SystemDecorations = Avalonia.Controls.WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
		Background = Brushes.Transparent;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		FontFamily = Application.Current?.Resources.ContainsKey ("ContentFontFamily") == true
			? (FontFamily)Application.Current.Resources["ContentFontFamily"]! : FontFamily.Default;

		resizeGrip = BuildResizeGrip ();
		frameContentHost = new Border { Child = null };

		frameDock = new DockPanel { LastChildFill = true };
		frameDock.Children.Add (frameContentHost);

		frame = new Border {
			Margin = new Thickness (6),
			CornerRadius = new CornerRadius (8),
			Background = (Brush)Application.Current!.FindResource ("IdeWindowBgBrush")!,
			BorderBrush = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			BorderThickness = new Thickness (1),
			Child = frameDock,
		};

		var root = new Panel ();
		root.Children.Add (resizeGrip);
		root.Children.Add (frame);
		base.Content = root;
	}

	bool showTitleRow;
	/// <summary>
	/// Shows the built-in title row (drag handle + close caption button) with the
	/// window Title. Called by subclasses; default off for dialogs that paint
	/// their own header.
	/// </summary>
	protected void ShowTitleRow ()
	{
		if (titleRow is not null) {
			titleRow.IsVisible = true;
			return;
		}
		showTitleRow = true;
		titleRow = BuildTitleRow ();
		DockPanel.SetDock (titleRow, Dock.Top);
		frameDock.Children.Insert (0, titleRow);
	}

	/// <summary>Marks an existing control as a drag handle (custom title rows).</summary>
	protected void EnableDrag (Control handle)
	{
		handle.PointerPressed += (_, e) => {
			if (e.GetCurrentPoint (handle).Properties.IsLeftButtonPressed) {
				try { BeginMoveDrag (e); } catch { /* headless/X11 edge */ }
			}
		};
	}

	/// <summary>Override of Content that places the subclass content inside the frame.</summary>
	public new object? Content {
		get => frameContentHost.Child;
		set => frameContentHost.Child = value as Control;
	}

	protected override void OnOpened (EventArgs e)
	{
		base.OnOpened (e);
		// CanResize == false dialogs hide the resize zones (border stays).
		resizeGrip.IsVisible = CanResize;
	}

	Grid BuildResizeGrip ()
	{
		var grid = new Grid {
			ColumnDefinitions = ColumnDefinitions.Parse ("6,*,6"),
			RowDefinitions = RowDefinitions.Parse ("6,*,6"),
		};
		string [] tags = {
			"NorthWest", "North", "NorthEast",
			"West", "", "East",
			"SouthWest", "South", "SouthEast",
		};
		for (int i = 0; i < 9; i++) {
			if (tags [i].Length == 0)
				continue;
			var b = new Border { Background = Brushes.Transparent, Tag = tags [i] };
			Grid.SetRow (b, i / 3);
			Grid.SetColumn (b, i % 3);
			b.PointerPressed += OnResizeGripPressed;
			b.PointerEntered += AttachResizeCursor;
			grid.Children.Add (b);
		}
		return grid;
	}

	void OnResizeGripPressed (object? sender, PointerPressedEventArgs e)
	{
		if (sender is Border { Tag: string tag } && e.GetCurrentPoint (this).Properties.IsLeftButtonPressed) {
			if (Enum.TryParse<WindowEdge> (tag, out var edge)) {
				try { BeginResizeDrag (edge, e); } catch { /* headless/X11 edge */ }
			}
		}
	}

	void AttachResizeCursor (object? sender, PointerEventArgs e)
	{
		if (sender is Border { Tag: string tag } b) {
			b.Cursor = tag switch {
				"North" or "South" => new Cursor (StandardCursorType.SizeNorthSouth),
				"East" or "West" => new Cursor (StandardCursorType.SizeWestEast),
				"NorthEast" or "SouthWest" => new Cursor (StandardCursorType.TopRightCorner),
				"NorthWest" or "SouthEast" => new Cursor (StandardCursorType.TopLeftCorner),
				_ => Cursor.Default,
			};
		}
	}

	Border BuildTitleRow ()
	{
		var titleText = new TextBlock {
			Text = Title,
			Margin = new Thickness (12, 6),
			FontWeight = FontWeight.SemiBold,
			FontSize = 12,
			VerticalAlignment = VerticalAlignment.Center,
		};
		titleText.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));

		var closeButton = new Button {
			Classes = { "caption", "captionclose" },
			Content = new Avalonia.Controls.Shapes.Path {
				Width = 10, Height = 10, Stretch = Stretch.None,
				Stroke = (Brush)Application.Current.FindResource ("IdeFgBrush")!,
				StrokeThickness = 1,
				Data = Geometry.Parse ("M 0,0 L 10,10 M 10,0 L 0,10"),
			},
			VerticalAlignment = VerticalAlignment.Center,
		};
		closeButton.Click += (_, _) => Close ();

		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto") };
		grid.Children.Add (titleText);
		Grid.SetColumn (closeButton, 1);
		grid.Children.Add (closeButton);

		var border = new Border {
			Background = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (0, 0, 0, 1),
			CornerRadius = new CornerRadius (8, 8, 0, 0),
			Child = grid,
		};
		border.EnableDialogDrag ();
		// Keep the close glyph in sync with the theme (the Path Stroke is static).
		border.AttachedToLogicalTree += (_, _) => {
			closeButton.Content = new Avalonia.Controls.Shapes.Path {
				Width = 10, Height = 10, Stretch = Stretch.None,
				StrokeThickness = 1,
				Data = Geometry.Parse ("M 0,0 L 10,10 M 10,0 L 0,10"),
			};
			if (closeButton.Content is Avalonia.Controls.Shapes.Path p)
				p.Bind (Avalonia.Controls.Shapes.Path.StrokeProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		};
		return border;
	}

	/// <summary>
	/// Applies the Win11 dialog chrome (rounded frame, thin toolbar-color border,
	/// edge resize zones, drag handles) to an existing XAML-authored Window: the
	/// window content is re-parented inside the frame. The XAML keeps its own
	/// title row — mark it with Classes="dialogchrome" and it becomes the drag
	/// handle; otherwise a built-in title row is prepended.
	/// </summary>
	public static void Apply (Window window)
	{
		if (window is DialogWindow || window.Content is not Control oldContent)
			return;

		window.SystemDecorations = Avalonia.Controls.WindowDecorations.None;
		window.ExtendClientAreaToDecorationsHint = true;
		window.TransparencyLevelHint = [Avalonia.Controls.WindowTransparencyLevel.Transparent];
		window.Background = Brushes.Transparent;

		// Detach FIRST: assigning it into the frame while it is still the window
		// content would give the control two logical parents.
		window.Content = null;
		var frameContentHost = new Border { Child = oldContent };
		var frameDock = new DockPanel { LastChildFill = true };
		frameDock.Children.Add (frameContentHost);

		// Reuse the dialog's own title row as the drag handle when tagged. The
		// content was just detached from the window, so walk the LOGICAL tree of
		// the detached content (walking the window would find nothing and add a
		// SECOND title row on top of the XAML one).
		Border? ownTitle = null;
		foreach (var d in LogicalExtensions.GetLogicalDescendants (oldContent)) {
			if (d is Border b && b.Classes.Contains ("dialogchrome")) {
				ownTitle = b;
				break;
			}
		}
		if (ownTitle is not null) {
			ownTitle.EnableDialogDrag ();
			ownTitle.CornerRadius = new CornerRadius (8, 8, 0, 0);
		} else {
			// No tagged row: host the window Title in a built-in drag row.
			var titleText = new TextBlock {
				Text = window.Title,
				Margin = new Thickness (12, 6),
				FontWeight = FontWeight.SemiBold,
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center,
			};
			titleText.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			var closeButton = new Button {
				Classes = { "caption", "captionclose" },
				Content = "\u2715",
				FontSize = 11,
			};
			closeButton.Click += (_, _) => window.Close ();
			var titleGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto") };
			titleGrid.Children.Add (titleText);
			Grid.SetColumn (closeButton, 1);
			titleGrid.Children.Add (closeButton);
			var titleRow = new Border {
				Classes = { "dialogchrome" },
				Background = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
				BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
				BorderThickness = new Thickness (0, 0, 0, 1),
				CornerRadius = new CornerRadius (8, 8, 0, 0),
				Child = titleGrid,
			};
			titleRow.EnableDialogDrag ();
			DockPanel.SetDock (titleRow, Dock.Top);
			frameDock.Children.Insert (0, titleRow);
		}

		var frame = new Border {
			Margin = new Thickness (6),
			CornerRadius = new CornerRadius (8),
			Background = (Brush)Application.Current!.FindResource ("IdeWindowBgBrush")!,
			BorderBrush = (Brush)Application.Current.FindResource ("IdeChromeBgBrush")!,
			BorderThickness = new Thickness (1),
			Child = frameDock,
		};

		var root = new Panel ();
		if (window.CanResize)
			root.Children.Add (BuildGrip (window));
		root.Children.Add (frame);
		window.Content = root;
	}

	static Grid BuildGrip (Window window)
	{
		var grid = new Grid {
			ColumnDefinitions = ColumnDefinitions.Parse ("6,*,6"),
			RowDefinitions = RowDefinitions.Parse ("6,*,6"),
		};
		string [] tags = {
			"NorthWest", "North", "NorthEast",
			"West", "", "East",
			"SouthWest", "South", "SouthEast",
		};
		for (int i = 0; i < 9; i++) {
			if (tags [i].Length == 0)
				continue;
			var b = new Border { Background = Brushes.Transparent, Tag = tags [i] };
			Grid.SetRow (b, i / 3);
			Grid.SetColumn (b, i % 3);
			b.PointerPressed += (s, e) => {
				if (s is Border { Tag: string tag2 } && e.GetCurrentPoint (window).Properties.IsLeftButtonPressed && Enum.TryParse<WindowEdge> (tag2, out var edge)) {
					try { window.BeginResizeDrag (edge, e); } catch { }
				}
			};
			b.PointerEntered += (s, _) => {
				if (s is Border { Tag: string tag3 })
					b.Cursor = tag3 switch {
						"North" or "South" => new Cursor (StandardCursorType.SizeNorthSouth),
						"East" or "West" => new Cursor (StandardCursorType.SizeWestEast),
						"NorthEast" or "SouthWest" => new Cursor (StandardCursorType.TopRightCorner),
						"NorthWest" or "SouthEast" => new Cursor (StandardCursorType.TopLeftCorner),
						_ => Cursor.Default,
					};
			};
			grid.Children.Add (b);
		}
		return grid;
	}
}

/// <summary>Drag/marker helpers shared by DialogWindow and DialogChrome.Apply.</summary>
file static class DialogDragExtensions
{
	public static void EnableDialogDrag (this Control handle)
	{
		handle.PointerPressed += (_, e) => {
			var win = TopLevel.GetTopLevel (handle) as Window;
			if (win is not null && e.GetCurrentPoint (handle).Properties.IsLeftButtonPressed) {
				try { win.BeginMoveDrag (e); } catch { }
			}
		};
	}
}
