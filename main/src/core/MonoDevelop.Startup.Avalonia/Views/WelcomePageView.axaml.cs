using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class WelcomePageView : UserControl
{
	const int MaxRecents = 10;

	public WelcomePageView ()
	{
		InitializeComponent ();
		NewIcon.Source = IconService.GetImage ("md-new-solution");
		OpenIcon.Source = IconService.GetImage ("gtk-open");
		LoadRecents ();
	}

	// Theme-aware helpers: bind properties to the Ide* palette resources so light/dark
	// switching works for dynamically created controls.
	static void BindFg (TextBlock tb)
		=> tb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));

	static void BindBrush (Border b, string key)
		=> b.Bind (Border.BackgroundProperty, Application.Current!.GetResourceObservable (key));

	void LoadRecents ()
	{
		RecentList!.Items.Clear ();
		var recents = RecentSolutions.GetAll ().Take (MaxRecents);
		int n = 0;
		foreach (var (path, stamp) in recents) {
			n++;
			RecentList.Items.Add (BuildTile (path, stamp));
		}
		if (n == 0) {
			var empty = new TextBlock {
				Text = "No recent solutions",
				Margin = new Thickness (0, 6, 0, 0),
				Opacity = 0.55,
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			BindFg (empty);
			RecentList.Items.Add (empty);
		}
	}

	// Tile per legacy WelcomePageListButton: icon + bold title + small path,
	// hover background + border, pin star on hover, opens the solution.
	Control BuildTile (string path, string stamp)
	{
		var title = Path.GetFileNameWithoutExtension (path);
		var dir = Path.GetDirectoryName (path) ?? "";

		var icon = new Image {
			Width = 16,
			Height = 16,
			Source = IconService.GetImage ("md-new-solution"),
			VerticalAlignment = VerticalAlignment.Center,
		};

		var titleTb = new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 13 };
		var dirTb = new TextBlock { Text = dir, FontSize = 11, Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis };
		BindFg (titleTb);
		BindFg (dirTb);

		var texts = new StackPanel {
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness (8, 0, 0, 0),
			Children = { titleTb, dirTb },
		};

		var star = new TextBlock {
			Text = "\u2606",
			FontSize = 13,
			Opacity = 0,
			VerticalAlignment = VerticalAlignment.Center,
		};
		BindFg (star);

		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("Auto,*,Auto") };
		Grid.SetColumn (icon, 0);
		Grid.SetColumn (texts, 1);
		Grid.SetColumn (star, 2);
		grid.Children.Add (icon);
		grid.Children.Add (texts);
		grid.Children.Add (star);

		var border = new Border {
			Child = grid,
			Padding = new Thickness (10, 7),
			Margin = new Thickness (0, 1),
			CornerRadius = new CornerRadius (3),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0, 1, 0, 1),
			BorderBrush = Brushes.Transparent,
			Cursor = new Cursor (StandardCursorType.Hand),
		};

		bool pinned = false;
		border.PointerEntered += (_, _) => {
			BindBrush (border, "IdeTabHoverBrush");
			star.Opacity = 0.85;
			star.Text = pinned ? "\u2605" : "\u2606";
		};
		border.PointerExited += (_, _) => {
			border.Background = Brushes.Transparent;
			star.Opacity = pinned ? 0.9 : 0;
		};
		border.PointerPressed += (_, e) => {
			if (e.GetCurrentPoint (border).Properties.IsLeftButtonPressed && star.IsPointerOver) {
				pinned = !pinned;
				star.Text = pinned ? "\u2605" : "\u2606";
				e.Handled = true;
			}
		};
		border.DoubleTapped += (_, _) => OpenSolution (path);
		border.Tapped += (_, _) => OpenSolution (path);

		ToolTip.SetTip (border, $"{title}\n{dir}" + (stamp.Length > 0 ? $"\nLast opened: {stamp}" : ""));
		return border;
	}

	void OpenSolution (string path)
	{
		if (!File.Exists (path)) {
			MainWindow.Instance?.Output ("[welcome] solution not found: " + path);
			return;
		}
		RecentSolutions.Add (path);
		MainWindow.Instance?.OpenSolutionInWindow (path);
	}

	// Project bar mirrors WelcomePageProjectBar.UpdateContent: visible while a
	// solution/workspace is open, with the Go Back affordance.
	public void UpdateProjectBar (string? solutionName)
	{
		var has = !string.IsNullOrEmpty (solutionName);
		ProjectBar!.IsVisible = has;
		if (has)
			ProjectBarText!.Text = $"Solution '{solutionName}' is currently open";
	}

	void OnGoBack (object? sender, RoutedEventArgs e)
		=> MainWindow.Instance?.HideWelcomePage ();

	void OnNewSolution (object? sender, RoutedEventArgs e)
		=> _ = MainWindow.Instance?.OpenNewSolutionDialogAsync ();

	void OnOpenSolution (object? sender, RoutedEventArgs e)
		=> MainWindow.Instance?.OpenSolutionPickerAsync ();
}
