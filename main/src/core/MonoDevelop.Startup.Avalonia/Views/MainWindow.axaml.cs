using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
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
	Border? welcomeDocBorder;

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
			}
		};

		// Legacy default: the Welcome page opens as the startup document.
		ShowWelcomePage ();
	}

	// Rebuilds the main menu (called at startup and whenever recents change, so the
	// File > Recent Solutions submenu mirrors the persisted list like the GTK UI).
	void BuildMenu ()
	{
		MainMenu!.Items.Clear ();
		var recents = RecentSolutions.GetAll ().Select (r => r.Path).ToList ();
		foreach (var item in MenuBuilder.BuildItems (MenuService.BuildMainMenu (recents)))
			MainMenu.Items.Add (item);
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

		// Solution pad (legacy ProjectPad): tree of the loaded solution.
		solutionTree = new ListBox {
			Background = Brushes.Transparent,
		};
		solutionTree.Bind (ListBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		solutionTree.DoubleTapped += OnSolutionOpen;
		LeftPads.AddTab (new PadHost.PadTab { Id = "solution", Label = "Solution", Content = solutionTree });
		solutionTree.Items.Add ("No solution loaded");

		// Classes pad (legacy ClassPad) — placeholder content, real pad pending.
		var classList = new ListBox { Background = Brushes.Transparent };
		classList.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		classList.Items.Add ("(classes of loaded solutions)");
		LeftPads.AddTab (new PadHost.PadTab { Id = "classes", Label = "Classes", Content = classList, Visible = false });

		// Properties pad (right, legacy layout).
		propertiesText = new TextBlock {
			Text = "Select an item to view its properties",
			Padding = new Thickness (8),
			Opacity = 0.7,
			TextWrapping = TextWrapping.Wrap,
		};
		propertiesText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		RightPads.AddTab (new PadHost.PadTab { Id = "properties", Label = "Properties", Content = propertiesText });

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
		BottomPads.AddTab (new PadHost.PadTab { Id = "output", Label = "Output", Content = outputTextBox });

		// Errors pad (legacy ErrorListPad).
		errorsText = new TextBlock { Padding = new Thickness (8, 6), Text = "No errors" };
		errorsText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "errors", Label = "Errors", Content = errorsText, Visible = false });

		// Tasks pad (legacy TaskListPad).
		tasksText = new TextBlock { Padding = new Thickness (8, 6), Text = "No tasks" };
		tasksText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		BottomPads.AddTab (new PadHost.PadTab { Id = "tasks", Label = "Tasks", Content = tasksText, Visible = false });

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

	// View > Pads toggles the dock group visibility (legacy pad toggle behavior).
	public void SetPadVisible (string id, bool visible)
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
		welcomeDocBorder = new Border {
			Child = welcomePage,
			Background = (Brush)Application.Current!.FindResource ("IdeWindowBgBrush")!,
		};
		AddDocument ("Welcome", welcomeDocBorder, closable: false, select: false);
	}

	public void ShowWelcomePage ()
	{
		EnsureWelcomePage ();
		welcomeVisible = true;
		welcomeDocBorder!.IsVisible = true;
		if (DocTabs!.Items.OfType<TabItem> ().All (t => (string?)t.Tag != "Welcome")) {
			var tab = new TabItem { Header = "Welcome", Tag = "Welcome", Classes = { "island" } };
			DocTabs.Items.Insert (0, tab);
		}
		SelectDocument ("Welcome");
	}

	public void HideWelcomePage ()
	{
		if (welcomeDocBorder is null)
			return;
		welcomeVisible = false;
		var tab = DocTabs!.Items.OfType<TabItem> ().FirstOrDefault (t => (string?)t.Tag == "Welcome");
		if (tab is not null)
			DocTabs.Items.Remove (tab);
		SelectFirstDocument ();
	}

	// ---------- Documents (tabs) ----------

	readonly List<(string Tag, Control Content)> documents = new ();

	void AddDocument (string tag, Control content, bool closable = true, bool select = true)
	{
		if (documents.Any (d => d.Tag == tag))
			return;
		documents.Add ((tag, content));

		var header = new Panel();
		var label = new TextBlock { Text = tag, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		header.Children.Add (label);

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
			var sp = new StackPanel { Orientation = Orientation.Horizontal, Children = { label, close } };
			header.Children.Clear ();
			header.Children.Add (sp);
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
		var tab = DocTabs!.Items.OfType<TabItem> ().FirstOrDefault (t => (string?)t.Tag == tag);
		if (tab is not null)
			DocTabs.Items.Remove (tab);
		documents.Remove (doc);
		SelectFirstDocument ();
	}

	// ---------- Solution loading ----------

	ListBox? solutionTree;

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
			solutionTree!.Items.Clear (); // ListBox requires empty Items before ItemsSource
			var items = new System.Collections.ObjectModel.ObservableCollection<string> {
				$"Solution '{title}' ({projects.Count (p => !p.IsFolder)} project(s))"
			};
			foreach (var p in projects) {
				var indent = p.Parent is null ? "" : "    ";
				var icon = p.IsFolder ? "[f]" : "[p]";
				items.Add ($"{indent}{icon} {p.Name}");
			}
			solutionTree!.ItemsSource = items;
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

	void OnSolutionOpen (object? sender, RoutedEventArgs e)
	{
		if (solutionTree?.SelectedItem is string sel && sel.EndsWith (".csproj", StringComparison.Ordinal))
			Output ("Open: " + sel.Trim ());
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
		case "MonoDevelop.Ide.Commands.FileCommands.OpenFile":
			OpenSolutionPickerAsync ();
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
}
