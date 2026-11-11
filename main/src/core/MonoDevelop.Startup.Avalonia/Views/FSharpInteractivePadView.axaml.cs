// FSharpInteractivePadView.axaml.cs — port of the legacy F# Interactive pad
// (MonoDevelop.FSharpBinding/FSharpInteractivePad.fs, 612 lines) to the Avalonia
// shell: the same F# session surface (a transcript of the session plus the
// Save/Open/Clear/Reset toolbar) speaking the `dotnet fsi` process protocol
// (FsiProcess) instead of the legacy InteractiveSession service — that
// protocol (SERVER-PROMPT> markers, "input "-prefixed lines and the
// MonoDevelop.FSharpInteractive.Service.exe sidecar) does not exist in the
// SDK's plain FSI.
//
// The legacy pad was a single editor where the prompt, the typed input and the
// results interleaved inline, because the legacy service ECHOED the typed
// lines back (`input <line>` → the service re-emitted them). The plain
// `dotnet fsi` does not echo input, so that inline single-editor layout cannot
// be reproduced byte-for-byte. The faithful adaptation is a terminal-REPL
// layout: a read-only TRANSCRIPT (the F# session output, with each sent
// command echoed as a `> <command>` line) plus a single-line INPUT box. The
// session semantics are preserved:
//  - the session starts lazily when the pad is first shown (legacy
//    editor.RunWhenRealized → setupSession) and is killed when the tab is
//    closed (legacy PadContent.Dispose → Shutdown) or on Restart;
//  - every sent command gets ";;" appended before it reaches the FSI (legacy
//    SendCommand appended ";;"; the typed-line path SendCommandAndStore did
//    not, but that path was multi-line — a single-line REPL appends ";;" so
//    each line is a complete expression; a redundant ";;" is a no-op in the
//    FSI, and ";;" after #r/#load/#q is accepted);
//  - SendFile/SendSelection/SendLine read the active document (selection, or
//    the current line when nothing is selected) and send it;
//  - history Up/Down over the commands already sent (legacy ShellHistory);
//  - Clear empties the transcript (legacy editor.Text <- ""); Restart kills
//    and re-sets-up the session (legacy resetFsi Restart) keeping the
//    transcript on screen;
//  - Save writes the session commands as a .fsx; Open sends #load "<path>".
//
// The FSI output stream glues its "> " prompt to the start of the following
// result line (no newline in between), so the transcript strips a leading
// "> " from each output line — the sent-command echo already provides the
// prompt. Diagnostics (the echoed source line, the ----^^^ marker and the
// error FS####: message) arrive on stderr and interleave in arrival order.
//
// The toolbar stocks are the legacy ones (gtk-save/open/clear/refresh — all
// registered in StockIcons.addin.xml). The legacy pad's tab icon was
// md-project (md-fs-project is not a registered stock — see the port docs);
// the tab icon is set by MainWindow.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Views;

public class FSharpInteractivePadView : UserControl
{
	const string Prompt = "> ";

	readonly TextBox input;
	readonly TextBox transcript;
	FsiProcess? session;
	bool sessionStarting;
	string fsiWorkingDir = "";
	// Legacy ShellHistory (FSharpInteractivePad.fs:143): Up/Down over the
	// commands already sent.
	readonly List<string> history = new ();
	int historyUp = -1;
	int historyDown = -1;

	/// <summary>Called by the shell on the first pad show (legacy
	/// editor.RunWhenRealized → setupSession) and after a Restart.</summary>
	public void StartSession (string workingDirectory)
	{
		if (session is { IsRunning: true } || sessionStarting)
			return;
		sessionStarting = true;
		fsiWorkingDir = workingDirectory;
		AppendTranscript ("[fsi] starting `dotnet fsi` in " + workingDirectory);
		try {
			var proc = new FsiProcess (workingDirectory);
			session = proc;
			proc.LineReceived += OnFsiLine;
			proc.Ready += OnFsiReady;
			proc.Exited += OnFsiExited;
		} catch (Exception ex) {
			sessionStarting = false;
			AppendTranscript ("[fsi] failed to start: " + ex.Message);
			OutputLog ("[fsi] start failed: " + ex.Message);
		}
	}

	void StopSession ()
	{
		sessionStarting = false;
		if (session is null)
			return;
		var proc = session;
		session = null;
		proc.LineReceived -= OnFsiLine;
		proc.Ready -= OnFsiReady;
		proc.Exited -= OnFsiExited;
		proc.Shutdown ();
		proc.Dispose ();
	}

	// Legacy resetFsi Restart: kill the current session and set up a fresh one.
	// Legacy keeps the transcript on a Restart (only Clear empties it).
	public void RestartFsi ()
	{
		if (fsiWorkingDir.Length == 0)
			fsiWorkingDir = Environment.CurrentDirectory;
		StopSession ();
		StartSession (fsiWorkingDir);
	}

	// Legacy ClearFsi: editor.Text <- "" (the whole pad content).
	public void ClearFsi ()
	{
		transcript.Clear ();
		input.Clear ();
	}

	// Stop the session without restarting it (the window/tab-close teardown —
	// legacy PadContent.Dispose → Shutdown).
	public void KillSession () => StopSession ();

	// ----- Session events (FSI reader threads → UI thread) -----

	void OnFsiLine (string line, bool isPrompt)
	{
		Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			// The FSI's own "> " prompt: not shown in the transcript (the
			// sent-command echo provides the prompt); it only signals readiness.
			if (isPrompt)
				return;
			// The FSI glues its "> " prompt to the start of the following
			// result line (no newline in between) — strip it so the result is
			// not misrendered with a prompt prefix.
			if (line.StartsWith (Prompt, StringComparison.Ordinal))
				line = line [Prompt.Length..];
			AppendTranscript (line);
		});
	}

	void OnFsiReady ()
	{
		Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			AppendTranscript ("[fsi] ready — type an F# expression and press Enter");
			OutputLog ("[fsi] ready");
		});
	}

	void OnFsiExited (int code)
	{
		Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			AppendTranscript ("[fsi] session exited with " + code);
			OutputLog ("[fsi] session exited with " + code);
		});
	}

	void AppendTranscript (string line)
	{
		// Avalonia 12 TextBox.Text is string? and defaults to null on a fresh
		// box (the source reads `Text ?? string.Empty` internally), so guard.
		var current = transcript.Text ?? "";
		transcript.Text = current.Length == 0 ? line : current + "\n" + line;
		// Auto-scroll to the latest line — but ONLY once the transcript TextBox
		// is realized. During the first ApplyTemplate the inner TextPresenter's
		// CaretIndex TemplateBinding re-evaluates on TemplatedParentChanged
		// BEFORE the inherited FontFamily has propagated to the transient
		// presenter; publishing a non-zero caret then forces a CreateTextLayout
		// on the raw "$Default" family (plain "Selawik", which the QA fontconfig
		// resolves to a glyph-less face) and throws "Could not create
		// glyphTypeface". The working Output pad appends Text only (caret stays
		// 0) for exactly this reason.
		if (transcript.IsLoaded)
			transcript.CaretIndex = transcript.Text!.Length;
	}

	void OutputLog (string message)
	{
		Console.WriteLine (message);
		Console.Out.Flush ();
	}

	// ----- Sending (legacy SendCommand / SendFile / SendSelection / SendLine) -----

	/// <summary>True while the `dotnet fsi` child process is alive (the
	/// AdvanceToNextLine consumer of Preferences "F# Settings" gates on it: the
	/// caret advances only when the sent line actually reaches a live session,
	/// mirroring the legacy SendCommand's no-op on a dead session).</summary>
	public bool IsSessionRunning => session is { IsRunning: true };

	/// <summary>Sends <paramref name="text"/> to the session: it is echoed into
	/// the transcript as a <c>> text</c> line, stored in the history, and sent
	/// to the FSI with ";;" appended (legacy SendCommand; a redundant ";;" is a
	/// no-op in the FSI). Multi-line text is sent line by line so the FSI sees
	/// complete lines (the plain FSI has no continuation prompt in pipe mode
	/// until the expression is complete).</summary>
	public void SendCommand (string text)
	{
		if (string.IsNullOrEmpty (text))
			return;
		if (session is null || !session.IsRunning) {
			AppendTranscript ("[fsi] no session — open F# Interactive first");
			return;
		}
		history.Add (text);
		historyUp = history.Count - 1;
		historyDown = history.Count - 1;
		// Echo what was sent so the transcript is a full session log.
		AppendTranscript (Prompt + text);
		var payload = text.TrimEnd ().EndsWith (";;", StringComparison.Ordinal)
			? text
			: text + ";;";
		// The FSI processes one line at a time; send the payload as-is (the
		// REPL input is single-line, and #r/#load/#q accept a trailing ";;").
		session.SendLine (payload);
		input.Focus ();
	}

	// Legacy SendFile: the whole active document text.
	public void SendFile (string? path, string text)
	{
		if (string.IsNullOrEmpty (path) || string.IsNullOrEmpty (text))
			return;
		SendCommand (text);
	}

	// Legacy SendSelection: the selection, or the current line when empty.
	public void SendSelection (string selected, string currentLine)
	{
		SendCommand (string.IsNullOrEmpty (selected) ? currentLine : selected);
	}

	// Legacy SendLine: the caret line.
	public void SendLine (string line) => SendCommand (line);

	// Legacy OpenScript: #load "<path>".
	public void LoadScript (string path)
		=> SendCommand ("#load \"" + path + "\"");

	// ----- Input box keyboard (Enter = send, Up/Down = history) -----

	void OnInputKeyDown (object? sender, KeyEventArgs e)
	{
		switch (e.Key) {
		case Key.Enter:
			// Key.Return and Key.Enter are the same value (6) in Avalonia; a
			// single case covers the physical Enter key.
			var text = input.Text ?? "";
			if (text.Length > 0) {
				SendCommand (text);
				input.Clear ();
			}
			e.Handled = true;
			break;
		case Key.Up:
			// Walk the history upward (legacy ProcessCommandHistoryUp).
			if (history.Count == 0)
				break;
			historyDown = historyUp < 0 ? history.Count - 1 : historyUp;
			historyUp = historyUp < 0 ? history.Count - 1 : Math.Max (0, historyUp - 1);
			input.Text = history [historyUp];
			input.CaretIndex = input.Text!.Length;
			e.Handled = true;
			break;
		case Key.Down:
			// Walk the history downward (legacy ProcessCommandHistoryDown).
			if (history.Count == 0 || historyUp < 0)
				break;
			if (historyDown >= history.Count)
				break;
			historyUp = historyDown;
			historyDown++;
			input.Text = historyDown < history.Count ? history [historyDown] : "";
			input.CaretIndex = input.Text!.Length;
			e.Handled = true;
			break;
		}
	}

	// Legacy Save: the sent commands (the history) written as a .fsx. The
	// toolbar Save button runs the file dialog in the shell.
	public string BuildSaveContent ()
		=> string.Join ("\n", history);

	// ----- QA accessors -----

	internal string TranscriptText => transcript.Text ?? "";
	internal string InputText => input.Text ?? "";
	internal bool HasSession => session is { IsRunning: true };
	internal bool SessionReady => session is { IsReady: true };
	internal int HistoryCount => history.Count;

	public FSharpInteractivePadView ()
	{
		// Read-only transcript: the F# session output. AcceptsReturn so it
		// holds the whole session; monospace so the F# source and the ----^^^
		// diagnostic markers align.
		transcript = new TextBox {
			AcceptsReturn = true,
			IsReadOnly = true,
			FontSize = 12,
			// No explicit FontFamily: inherit like the working Output pad
			// (embedded Selawik via ContentControlThemeFontFamily). An explicit
			// name-only family (e.g. "DejaVu Sans Mono") that the restricted QA
			// fontconfig fixture does not carry throws at glyph-typeface
			// creation.
			Background = Brushes.Transparent,
		};
		transcript.Bind (TextBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		// The transcript may already hold session lines when the pad is first
		// shown (StartSession runs on the first SetPadVisible, before Loaded):
		// jump to the end once the TextBox template is realized (fonts resolved).
		transcript.Loaded += (_, _) => transcript.CaretIndex = (transcript.Text ?? "").Length;

		// Single-line input box: type an F# expression, Enter sends it.
		input = new TextBox {
			AcceptsReturn = false,
			PlaceholderText = "F# expression  (e.g.  1 + 1;;   —  Up/Down = history)",
			FontSize = 12.5,
			// No explicit FontFamily: inherit like the working Output pad
			// (embedded Selawik via ContentControlThemeFontFamily) — a name-only
			// family the active fontconfig lacks would throw at glyph creation.
			VerticalContentAlignment = VerticalAlignment.Center,
			Background = Brushes.Transparent,
		};
		input.Bind (TextBox.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		input.KeyDown += OnInputKeyDown;

		// Toolbar (legacy FSharpInteractivePad.Initialize: the four
		// DockToolButtons, this order and tooltips).
		(var saveButton, _) = MakeToolButton ("gtk-save", "Save as script");
		saveButton.Click += (_, _) => SaveRequested?.Invoke (this, EventArgs.Empty);
		(var openButton, _) = MakeToolButton ("gtk-open", "Open");
		openButton.Click += (_, _) => OpenRequested?.Invoke (this, EventArgs.Empty);
		(var clearButton, _) = MakeToolButton ("gtk-clear", "Clear");
		clearButton.Click += (_, _) => ClearFsi ();
		(var resetButton, _) = MakeToolButton ("gtk-refresh", "Reset");
		resetButton.Click += (_, _) => RestartFsi ();
		var toolbar = new StackPanel {
			Orientation = Orientation.Horizontal,
			Spacing = 2,
			Margin = new Thickness (2),
		};
		toolbar.Children.Add (saveButton);
		toolbar.Children.Add (openButton);
		toolbar.Children.Add (clearButton);
		toolbar.Children.Add (resetButton);

		// Layout: toolbar on top, input at the bottom, transcript fills.
		var root = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (toolbar, Dock.Top);
		DockPanel.SetDock (input, Dock.Bottom);
		input.MinHeight = 34;
		root.Children.Add (toolbar);
		root.Children.Add (input);
		root.Children.Add (transcript);
		Content = root;
	}

	static (Button Button, Image Icon) MakeToolButton (string stockId, string tooltip)
	{
		var img = new Image { Width = 16, Height = 16 };
		if (MonoDevelop.Ide.Services.IconService.GetImage (stockId) is { } bmp)
			img.Source = bmp;
		var b = new Button { Content = img, Padding = new Thickness (4) };
		ToolTip.SetTip (b, tooltip);
		return (b, img);
	}

	/// <summary>The toolbar Save button (the file dialog runs in the shell —
	/// the pad has no window context).</summary>
	public event EventHandler? SaveRequested;

	/// <summary>The toolbar Open button (same reason as SaveRequested).</summary>
	public event EventHandler? OpenRequested;
}
