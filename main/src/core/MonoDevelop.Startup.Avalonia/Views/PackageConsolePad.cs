using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy Package Console pad (MonoDevelop.Ide.Gui.Pads.DefaultMonitorPad,
/// GTK) onto the Avalonia shell: the output pad the NuGet add-in registers for package
/// operations (PackageManagementProgressMonitorFactory.CreatePackageConsoleOutputMonitor →
/// GetOutputProgressMonitor ("PackageConsole", "Package Console", Stock.Console, ...)).
///
/// Toolbar (legacy DockItemToolbar, DockPositionType.RIGHT — the distinctive "barra de la
/// derecha" of every monitor pad, the feature this port is named for): Stop (Stock.Stop →
/// md-stop, cancels the running operation), Clear console (Stock.Broom → md-clear) and the
/// Pin toggle (Stock.PinUp/PinDown → md-pin-up/md-pin-down, swaps the icon on toggle, legacy
/// OnButtonPinClick). Icon-only buttons in IconSize.Menu order, as in the legacy Initialize.
///
/// Body (legacy LogView): a read-only monospace console, one line per colored TextBlock.
/// The color scheme is the legacy tag set — errors in the error foreground BOLD (LogView
/// errorTag), info in the secondary gray (consoleLogTag), debug in the information blue
/// (debugTag), the rest in the normal foreground. Lines wrap (legacy LogView wrap). Auto-
/// scroll follows the tail (legacy ShouldAutoScroll/ScrollToMark — for an append-only
/// console, "already at the bottom" is always true). The legacy ANSI sanitizer
/// (consoleTextSanitizerRegex: \e[...m and \b) is kept. The legacy search box (Search:
/// entry + find next/previous + close) is not ported: no shell LogView carries it, matching
/// the ErrorsPad precedent of scoping the port to the DockItemToolbar buttons + the body.
///
/// State (legacy DefaultMonitorPad): the pad opens empty and auto-hidden (the NuGet factory
/// creates it with visible=false → AddPad, not ShowPad); an operation opens it and brings it
/// to front (ShowPackageConsole → pad.BringToFront → ShowRequested → SetPadVisible + select),
/// clearing the console first when the legacy clearConsole flag says so (ClearOnBeginProgress,
/// default true). EndOperation (legacy EndProgress) stops the busy state and yields the last
/// error/warning/success line for the status bar; Stop is only enabled while an operation
/// runs (legacy buttonStop.Sensitive).
/// </summary>
public class PackageConsolePad
{
	/// <summary>The tab icon (legacy Stock.Console → md-console, resolved from the
	/// StockIcons.addin.xml map by IconService; the same PNG the legacy pad tab used).</summary>
	public const string PadIcon = "md-console";

	/// <summary>Default tab label (legacy GettextCatalog.GetString ("Package Console")).</summary>
	public const string PadLabel = "Package Console";

	// ---- Public surface (wired by MainWindow) ----

	public Control Root { get; }

	/// <summary>The line container (one TextBlock per line) — QA asserts + the owner's log.</summary>
	public StackPanel Console { get; }

	public bool IsWorking { get; private set; }
	public bool IsPinned => pinBtn.IsChecked == true;

	/// <summary>Legacy buttonStop.Sensitive: the Stop button is only enabled while an
	/// operation runs (QA assertion on the enable/disable behavior).</summary>
	public bool IsStopEnabled => stopBtn.IsEnabled;

	/// <summary>The console as plain text (lines joined by \n) — owner status / QA dump.</summary>
	public string Text => string.Join ("\n", Console.Children.OfType<TextBlock> ().Select (t => t.Text ?? ""));

	/// <summary>Starts an operation (legacy BeginProgress): clears the console when
	/// clearOnBeginProgress (legacy ClearOnBeginProgress, default true), enables Stop
	/// (legacy buttonStop.Sensitive) and sets the working state.</summary>
	public void BeginOperation (bool clearOnBeginProgress = true)
	{
		if (clearOnBeginProgress)
			Clear ();
		IsWorking = true;
		stopBtn.IsEnabled = true;
	}

	/// <summary>Ends the operation (legacy EndProgress): disables Stop, clears the working
	/// state, and returns the last error/warning/success line for the status bar (legacy
	/// StatusBar.ShowMessage with the last monitor error/warning/success).</summary>
	public string EndOperation ()
	{
		IsWorking = false;
		stopBtn.IsEnabled = false;
		return LastStatusMessage ();
	}

	/// <summary>Legacy OnButtonStopClick → monitor.Cancel: asks the owner to stop the
	/// running operation (the owner owns the cancellation token / process kill).</summary>
	public event Action? StopRequested;

	/// <summary>Legacy OnButtonClearClick: the console was cleared.</summary>
	public event Action? Cleared;

	/// <summary>Legacy pin toggle (Stock.PinUp → Stock.PinDown icon swap).</summary>
	public event Action<bool>? PinToggled;

	/// <summary>Legacy ShowPackageConsole → pad.BringToFront: the owner shows the pad tab
	/// and selects it (the auto-show on the first package operation).</summary>
	public event Action? ShowRequested;

	// ---- Public log API (legacy LogView Write* methods) ----

	/// <summary>Legacy LogView.WriteText: appends a normal-foreground line.</summary>
	public void AppendLine (string line) => AddLine (line, ColorMode.Normal);

	/// <summary>Legacy LogView.WriteError: appends a bold error-foreground line.</summary>
	public void AppendError (string line) => AddLine (line, ColorMode.Error);

	/// <summary>Legacy LogView.WriteConsoleLogText: appends a dim (console-log) line.</summary>
	public void AppendInfo (string line) => AddLine (line, ColorMode.Info);

	/// <summary>Legacy LogView.WriteDebug (empty category): appends a debug-foreground
	/// line (Styles.InformationForegroundColor).</summary>
	public void AppendDebug (string line) => AddLine (line, ColorMode.Debug);

	/// <summary>Appends one colored line (QA / owner color control).</summary>
	public void Append (string text, ColorMode mode) => AddLine (text, mode);

	/// <summary>Legacy LogView.Clear (+ Cookie++): empties the console and fires Cleared.</summary>
	public void Clear ()
	{
		Console.Children.Clear ();
		totalLength = 0;
		Cleared?.Invoke ();
	}

	// ---- QA surface ----

	/// <summary>QA: the last error, else warning, else success line of the buffer ("" when
	/// none) — the owner-side stand-in for the legacy monitor.Errors/Warnings/SuccessMessages
	/// that EndProgress reports to the status bar.</summary>
	public string LastStatusMessage ()
	{
		string? lastError = null, lastWarning = null, lastSuccess = null;
		foreach (var t in Console.Children.OfType<TextBlock> ()) {
			var l = (t.Text ?? "").Trim ();
			if (l.Length == 0)
				continue;
			if (l.StartsWith ("error ", StringComparison.OrdinalIgnoreCase)
				|| l.Contains (": error ", StringComparison.Ordinal)
				|| l.StartsWith ("Build FAILED", StringComparison.OrdinalIgnoreCase))
				lastError = l;
			else if (l.Contains ("warning", StringComparison.OrdinalIgnoreCase))
				lastWarning = l;
			else if (l.Contains ("succeeded", StringComparison.OrdinalIgnoreCase)
				|| l.Contains ("success", StringComparison.OrdinalIgnoreCase))
				lastSuccess = l;
		}
		return lastError ?? lastWarning ?? lastSuccess ?? "";
	}

	/// <summary>QA: number of lines in the buffer.</summary>
	public int LineCount () => Console.Children.Count;

	public string DumpStatus ()
	{
		return $"working={IsWorking} pinned={IsPinned} stop-enabled={stopBtn.IsEnabled} " +
			$"pin-icon={PinIconId} lines={LineCount()} text-len={Text.Length}";
	}

	/// <summary>QA headless: drive the toolbar the same way a click would (Stop →
	/// StopRequested, Clear → Clear, Pin → toggle + icon swap).</summary>
	public void QaStop () => stopBtn.RaiseEvent (new Avalonia.Interactivity.RoutedEventArgs (Button.ClickEvent));
	public void QaClear () => clearBtn.RaiseEvent (new Avalonia.Interactivity.RoutedEventArgs (Button.ClickEvent));
	public void QaTogglePin () => pinBtn.IsChecked = !(pinBtn.IsChecked == true);

	/// <summary>The current pin stock id (QA assertion on the icon swap).</summary>
	public string PinIconId => pinBtn.IsChecked == true ? "md-pin-down" : "md-pin-up";

	/// <summary>Re-resolves the toolbar icons for the current theme variant (the shell
	/// calls this on theme changes, like PadHost.RefreshIcons / ErrorsPad.RefreshIcons).</summary>
	public void RefreshIcons ()
	{
		SetSource (stopImg, "md-stop");
		SetSource (clearImg, "md-clear");
		SetSource (pinImg, pinBtn.IsChecked == true ? "md-pin-down" : "md-pin-up");
	}

	// ---- State ----

	public enum ColorMode { Normal, Error, Info, Debug }

	Image stopImg, clearImg, pinImg;
	Button stopBtn, clearBtn;
	ToggleButton pinBtn;
	ScrollViewer scroll;

	ISolidColorBrush fgBrush, errorBrush, infoBrush, debugBrush;
	bool darkTheme;
	int totalLength;
	const int MaxBufferLength = 4000 * 1024; // legacy LogView.MAX_BUFFER_LENGTH

	static readonly System.Text.RegularExpressions.Regex sanitizer = new (
		"(\\e[[0-9]*m)|(\b)", System.Text.RegularExpressions.RegexOptions.Compiled);

	public PackageConsolePad ()
	{
		darkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

		Console = new StackPanel {
			Orientation = Orientation.Vertical,
		};
		scroll = new ScrollViewer {
			Content = Console,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			BorderThickness = new Thickness (0),
			Padding = new Thickness (0, 6, 0, 6),
		};

		// --- Toolbar (legacy DockItemToolbar, DockPositionType.Right) ---
		stopImg = new Image { Width = 16, Height = 16 };
		stopBtn = new Button {
			Content = stopImg,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
			Padding = new Thickness (5, 3),
			IsEnabled = false, // legacy: sensitive only while an operation runs
		};
		ToolTip.SetTip (stopBtn, "Stop"); // legacy GettextCatalog.GetString ("Stop")
		stopBtn.Click += (_, _) => { if (IsWorking) StopRequested?.Invoke (); };

		clearImg = new Image { Width = 16, Height = 16 };
		clearBtn = new Button {
			Content = clearImg,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
			Padding = new Thickness (5, 3),
		};
		ToolTip.SetTip (clearBtn, "Clear console"); // legacy "Clear console"
		clearBtn.Click += (_, _) => Clear ();

		pinImg = new Image { Width = 16, Height = 16 };
		pinBtn = new ToggleButton {
			Content = pinImg,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness (0),
			Padding = new Thickness (5, 3),
			IsChecked = false, // legacy: un-pinned by default
		};
		ToolTip.SetTip (pinBtn, "Pin output pad"); // legacy "Pin output pad"
		pinBtn.IsCheckedChanged += (_, _) => {
			// Legacy OnButtonPinClick: PinDown while active, PinUp otherwise.
			SetSource (pinImg, pinBtn.IsChecked == true ? "md-pin-down" : "md-pin-up");
			PinToggled?.Invoke (pinBtn.IsChecked == true);
		};

		SetSource (stopImg, "md-stop");
		SetSource (clearImg, "md-clear");
		SetSource (pinImg, "md-pin-up");

		// Legacy DockItemToolbar for DockPositionType.Right uses a VBox (columna
		// vertical de botones apilados de arriba abajo, spacing 3): Stop/Clear/Pin.
		var toolbar = new StackPanel {
			Orientation = Orientation.Vertical,
			Spacing = 3,
			Margin = new Thickness (2, 2, 4, 2),
		};
		toolbar.Children.Add (stopBtn);
		toolbar.Children.Add (clearBtn);
		toolbar.Children.Add (pinBtn);

		// Legacy DefaultMonitorPad: LogView fills the pad, the toolbar is on the RIGHT
		// edge (DockItemToolbar DockPositionType.Right) — the content is to its left.
		var root = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (toolbar, Dock.Right);
		root.Children.Add (toolbar);
		root.Children.Add (scroll);
		Root = root;

		ApplyThemeColors ();
	}

	void AddLine (string text, ColorMode mode)
	{
		text = SanitizeConsoleText (text);
		if (string.IsNullOrEmpty (text))
			return;

		var line = new TextBlock {
			Text = text,
			FontFamily = new FontFamily ("Monospace,DejaVu Sans Mono,Consolas"),
			FontSize = 12,
			TextWrapping = TextWrapping.Wrap,
		};
		if (mode == ColorMode.Error) {
			line.Foreground = ErrorBrush ();
			line.FontWeight = FontWeight.Bold; // legacy errorTag.Weight = Bold
		} else {
			line.Foreground = mode switch {
				ColorMode.Info => InfoBrush (),
				ColorMode.Debug => DebugBrush (),
				_ => ForegroundBrush (),
			};
		}
		Console.Children.Add (line);
		totalLength += text.Length;

		// Legacy MAX_BUFFER_LENGTH on the buffer: drop the OLDEST lines until the
		// tail fits (per-line granularity here; the legacy trimmed characters).
		while (totalLength > MaxBufferLength && Console.Children.Count > 1) {
			var first = Console.Children.First ();
			if (first is TextBlock ft)
				totalLength -= (ft.Text ?? "").Length;
			Console.Children.Remove (first);
		}

		ScrollToEnd ();
	}

	void ScrollToEnd ()
	{
		// Legacy ShouldAutoScroll + ScrollToMark(endMark): follow the tail. Post to the
		// dispatcher so it applies after the new line is measured (headless-safe).
		if (!scroll.IsAttachedToVisualTree ())
			return;
		Dispatcher.UIThread.Post (() => scroll.ScrollToEnd (), DispatcherPriority.Background);
	}

	static void SetSource (Image img, string stockId)
	{
		if (IconService.GetImage (stockId) is { } bmp)
			img.Source = bmp;
	}

	void ApplyThemeColors ()
	{
		// Legacy light (MonoDevelop.Ide.Gui.Styles): error #c42c3e, information
		// #5785bd, secondary (console log) #767676. Legacy dark: information #9cc8ff,
		// error the status-error red. The normal foreground is the theme IdeFgBrush.
		errorBrush = darkTheme
			? new SolidColorBrush (Color.Parse ("#f48771"))
			: new SolidColorBrush (Color.Parse ("#c42c3e"));
		infoBrush = darkTheme
			? new SolidColorBrush (Color.Parse ("#9cc8ff"))
			: new SolidColorBrush (Color.Parse ("#5785bd"));
		debugBrush = darkTheme
			? new SolidColorBrush (Color.Parse ("#9cc8ff"))
			: new SolidColorBrush (Color.Parse ("#5785bd"));
		if (Application.Current is { } app
			&& app.TryGetResource ("IdeFgBrush", app.ActualThemeVariant, out var f)
			&& f is SolidColorBrush sb)
			fgBrush = sb;
	}

	ISolidColorBrush ForegroundBrush () => fgBrush ?? new SolidColorBrush (Colors.Gray);
	ISolidColorBrush ErrorBrush () => errorBrush ?? new SolidColorBrush (Color.Parse ("#c42c3e"));
	ISolidColorBrush InfoBrush () => infoBrush ?? new SolidColorBrush (Color.Parse ("#5785bd"));
	ISolidColorBrush DebugBrush () => debugBrush ?? new SolidColorBrush (Color.Parse ("#5785bd"));

	/// <summary>Legacy LogView.SanitizeConsoleText (consoleTextSanitizerRegex): strip
	/// ANSI color escapes (\e[...m) and the backspace control character.</summary>
	static string SanitizeConsoleText (string text) => sanitizer.Replace (text, string.Empty);
}
