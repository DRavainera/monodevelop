using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Faithful port of the legacy GTK/Xwt welcome page (MonoDevelop.Ide.WelcomePage):
/// logo strip over the page background, link bar (MonoDevelop.com / Documentation /
/// Support / Q&A) and the three section pads — Solutions (recent projects with
/// pinning star), News (feed slot) and Tip of the day (TipsOfTheDay.xml + Next Tip).
/// Metrics come from WelcomePage Style.cs (tiles 260x46, titles 24px light,
/// links #868686 dark / secondary light, section pads #222222 dark / white light).
/// </summary>
public partial class WelcomePageView : UserControl
{
	const int MaxRecents = 10;
	const string MonoUrl = "http://www.monodevelop.com";
	const string DocsUrl = "http://www.go-mono.com/docs";
	const string SupportUrl = "http://monodevelop.com/index.php?title=Help_%26_Contact";
	const string QaUrl = "http://stackoverflow.com/questions/tagged/monodevelop";
	const string NewsUrl = "https://www.dotnetfoundation.org/about/news";

	string[] tips = Array.Empty<string> ();
	int currentTip = -1;

	public WelcomePageView ()
	{
		InitializeComponent ();

		LogoImage!.Source = LoadBranding ("avares://MonoDevelop.AvaloniaShell/branding/welcome-logo.png");
		NewIcon!.Source = IconService.GetResourceImage ("welcome-new-solution-16");
		OpenIcon!.Source = IconService.GetResourceImage ("welcome-open-solution-16");

		BuildLinkBar ();
		LoadTips ();
		LoadRecents ();
	}

	// ---------- Link bar (DefaultWelcomePage row1: WelcomePageBarButton set) ----------

	void BuildLinkBar ()
	{
		AddLink ("MonoDevelop.com", MonoUrl, "welcome-link-md-16");
		AddLink ("Documentation", DocsUrl, "welcome-link-info-16");
		AddLink ("Support", SupportUrl, "welcome-link-support-16");
		AddLink ("Q&A", QaUrl, "welcome-link-chat-16");
	}

	void AddLink (string label, string url, string iconResource)
	{
		var icon = new Image {
			Width = 16,
			Height = 16,
			Source = IconService.GetResourceImage (iconResource),
			VerticalAlignment = VerticalAlignment.Center,
		};
		// No explicit foreground: the TextBlock inherits the button's (welcomelink style,
		// muted gray, brightening on hover) like the legacy Pango markup did.
		var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };

		var button = new Button {
			Classes = { "welcomelink" },
			Padding = new Thickness (0),
			Background = Brushes.Transparent,
			Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { icon, text } },
			HorizontalContentAlignment = HorizontalAlignment.Left,
		};
		button.Click += (_, _) => OpenUrl (url);
		LinkBar!.Children.Add (button);
	}

	static void OpenUrl (string url)
	{
		try {
			Process.Start (new ProcessStartInfo (url) { UseShellExecute = true });
		} catch (Exception ex) {
			MainWindow.Instance?.Output ("[welcome] could not open " + url + ": " + ex.Message);
		}
	}

	// ---------- Solutions section (WelcomePageRecentProjectsList) ----------

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
			};
			empty.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			RecentList.Items.Add (empty);
		}
	}

	// Tile per legacy WelcomePageListButton: icon + bold title + small directory path,
	// hover = tile hover color, pin star on hover, click opens the solution.
	Control BuildTile (string path, string stamp)
	{
		var title = Path.GetFileNameWithoutExtension (path);
		var dir = Path.GetDirectoryName (path) ?? "";

		var icon = new Image {
			Width = 16,
			Height = 16,
			Source = IconService.GetImage ("md-solution"),
			VerticalAlignment = VerticalAlignment.Center,
		};

		var titleTb = new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 12 };
		var dirTb = new TextBlock {
			Text = dir,
			FontSize = 10,
			Opacity = 0.75,
			TextTrimming = TextTrimming.CharacterEllipsis,
		};
		titleTb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		dirTb.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));

		var texts = new StackPanel {
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness (38 - 16, 0, 0, 0), // legacy TextLeftPadding=38 from tile edge
			Children = { titleTb, dirTb },
		};

		var star = new Image {
			Width = 16,
			Height = 16,
			Opacity = 0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness (4, 0, 6, 0),
		};

		var grid = new Grid { ColumnDefinitions = Avalonia.Controls.ColumnDefinitions.Parse ("Auto,*,Auto") };
		grid.Children.Add (icon);
		Grid.SetColumn (texts, 1);
		grid.Children.Add (texts);
		Grid.SetColumn (star, 2);
		grid.Children.Add (star);

		bool pinned = RecentSolutions.IsFavorite (path);

		var border = new Border {
			Child = grid,
			Classes = { "wtile" }, // hover background/border from the wtile style
			Padding = new Thickness (10, 0),
			Margin = new Thickness (0, 1),
			Cursor = new Cursor (StandardCursorType.Hand),
		};

		void UpdateStar ()
		{
			var name = pinned ? "star-16" : "unstar-16";
			if (border.IsPointerOver)
				name += "-hover";
			star.Source = IconService.GetResourceImage (name);
			star.Opacity = border.IsPointerOver ? 1 : (pinned ? 0.9 : 0);
		}

		border.PointerEntered += (_, _) => UpdateStar ();
		border.PointerExited += (_, _) => UpdateStar ();
		UpdateStar ();

		border.PointerPressed += (_, e) => {
			// Legacy: the star itself toggles pinning (PinClickHandler → SetFavoriteFile)
			if (e.GetCurrentPoint (border).Properties.IsLeftButtonPressed && star.IsPointerOver) {
				pinned = !pinned;
				UpdateStar ();
				RecentSolutions.SetFavorite (path, pinned);
				e.Handled = true;
			}
		};
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

	// ---------- Tip of the day (WelcomePageTipOfTheDaySection over TipsOfTheDay.xml) ----------

	void LoadTips ()
	{
		try {
			var dataDir = FindDataPath ();
			var xmlPath = dataDir is null ? null : Path.Combine (dataDir, "options", "TipsOfTheDay.xml");
			if (xmlPath is null || !File.Exists (xmlPath))
				return;
			var doc = new XmlDocument ();
			doc.Load (xmlPath);
			tips = doc.DocumentElement?.ChildNodes.Cast<System.Xml.XmlNode> ()
				.Where (n => n.NodeType == System.Xml.XmlNodeType.Element)
				.Select (n => n.InnerText.Trim ())
				.Where (t => t.Length > 0)
				.ToArray () ?? Array.Empty<string> ();
		} catch (Exception ex) {
			MainWindow.Instance?.Output ("[welcome] tips load failed: " + ex.Message);
		}
		if (tips.Length > 0) {
			currentTip = new Random ().Next () % tips.Length;
			ShowTip ();
		} else {
			TipLabel!.Text = "";
			NextTipButton!.IsVisible = false;
		}
	}

	// build/data is the legacy PropertyService.DataPath for the net10run build.
	static string? FindDataPath ()
	{
		try {
			var dir = AppContext.BaseDirectory;
			for (int i = 0; i < 6 && dir is not null; i++) {
				var candidate = Path.GetFullPath (Path.Combine (dir, "data"));
				if (File.Exists (Path.Combine (candidate, "options", "TipsOfTheDay.xml")))
					return candidate;
				dir = Path.GetDirectoryName (dir);
			}
			// Running from the repo tree: fall back to the source options copy.
			dir = AppContext.BaseDirectory;
			for (int i = 0; i < 8 && dir is not null; i++) {
				var candidate = Path.GetFullPath (Path.Combine (dir, "src", "core", "MonoDevelop.Ide"));
				if (File.Exists (Path.Combine (candidate, "options", "TipsOfTheDay.xml")))
					return candidate;
				dir = Path.GetDirectoryName (dir);
			}
		} catch { }
		return null;
	}

	void ShowTip ()
	{
		if (currentTip < 0 || currentTip >= tips.Length)
			return;
		TipLabel!.Text = tips [currentTip];
	}

	void OnNextTip (object? sender, RoutedEventArgs e)
	{
		if (tips.Length == 0)
			return;
		currentTip = (currentTip + 1) % tips.Length;
		ShowTip ();
	}

	void OnNewsOpen (object? sender, RoutedEventArgs e) => OpenUrl (NewsUrl);

	// ---------- Theme-aware helpers ----------

	static IImage? LoadBranding (string uri)
	{
		try {
			if (Avalonia.Platform.AssetLoader.Open (new Uri (uri)) is Stream stream)
				return new Avalonia.Media.Imaging.Bitmap (stream);
		} catch (Exception ex) {
			Console.WriteLine ("[welcome] branding load failed: " + ex.Message);
		}
		return null;
	}

	// ---------- Project bar (WelcomePageFrame.UpdateProjectBar semantics) ----------

	// Project bar mirrors WelcomePageProjectBar.UpdateContent: visible while a
	// solution is open, tooltip-style bar with the Go Back affordance.
	public void UpdateProjectBar (string? solutionName)
	{
		var has = !string.IsNullOrEmpty (solutionName);
		ProjectBar!.IsVisible = has;
		if (has) {
			ProjectBarText!.Text = $"Solution '{solutionName}' is currently open";
			GoBackButton!.Content = "Go Back to Solution";
		}
	}

	void OnGoBack (object? sender, RoutedEventArgs e)
		=> MainWindow.Instance?.HideWelcomePage ();

	void OnNewSolution (object? sender, RoutedEventArgs e)
		=> _ = MainWindow.Instance?.OpenNewSolutionDialogAsync ();

	void OnOpenSolution (object? sender, RoutedEventArgs e)
		=> MainWindow.Instance?.OpenSolutionPickerAsync ();
}
