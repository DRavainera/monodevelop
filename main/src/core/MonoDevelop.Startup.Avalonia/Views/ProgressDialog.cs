using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Progress dialog — the Avalonia port of the legacy ProgressDialog
/// (MonoDevelop.Ide.Gui.Dialogs): message label, progress bar, Cancel/Close
/// buttons and an optional details expander with an indented task log
/// (BeginTask/EndTask/WriteText with the legacy ShowDone states).
/// </summary>
public class ProgressDialog : Window
{
	readonly TextBlock messageLabel = new ();
	readonly ProgressBar progressBar = new ();
	readonly Button btnCancel = new ();
	readonly Button btnClose = new ();
	readonly Expander detailsExpander;
	readonly TextBox detailsText = new ();
	readonly Stack<string> indents = new ();
	int indentLevel;

	static IBrush DialogBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeWindowBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	public System.Threading.CancellationTokenSource? CancellationTokenSource { get; set; }

	/// <summary>Set when the user cancels (the legacy OnBtnCancelClicked).</summary>
	public bool Cancelled { get; private set; }

	public ProgressDialog (bool allowCancel, bool showDetails)
	{
		Title = "Progress"; // legacy: BrandingService.ApplicationName
		Width = 560;
		MinHeight = 150;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		Background = DialogBg (Color.Parse ("#2d2d30"));

		var border = new Border {
			Background = DialogBg (Color.Parse ("#2d2d30")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			Padding = new Thickness (14, 12),
		};

		var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

		messageLabel.Text = "";
		messageLabel.TextWrapping = TextWrapping.Wrap;
		messageLabel.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		root.Children.Add (messageLabel);

		progressBar.Minimum = 0;
		progressBar.Maximum = 1;
		progressBar.Height = 14;
		root.Children.Add (progressBar);

		var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
		btnCancel.Content = "Cancel";
		btnCancel.MinWidth = 84;
		btnCancel.Click += (_, _) => {
			Cancelled = true;
			CancellationTokenSource?.Cancel ();
		};
		btnClose.Content = "Close";
		btnClose.MinWidth = 84;
		btnClose.IsVisible = false; // legacy: hidden until ShowDone
		btnClose.Click += (_, _) => Close ();
		buttons.Children.Add (btnCancel);
		buttons.Children.Add (btnClose);
		root.Children.Add (buttons);

		// Details expander with the indented task log (legacy TextView + tags).
		detailsText.IsReadOnly = true;
		detailsText.MinHeight = 100;
		detailsText.FontFamily = new FontFamily ("Monospace,DejaVu Sans Mono,Consolas");
		detailsText.FontSize = 11.5;
		detailsText.AcceptsReturn = true;
		detailsText.TextWrapping = TextWrapping.NoWrap;
		detailsExpander = new Expander {
			Header = "Details",
			Content = detailsText,
			IsVisible = showDetails,
		};
		// Show the log only when expanded (avoids a tall empty box collapsed).
		detailsText.IsVisible = showDetails && detailsExpander.IsExpanded;
		root.Children.Add (detailsExpander);

		btnCancel.IsVisible = allowCancel;
		border.Child = root;
		Content = border;
	}

	public string Message {
		get => messageLabel.Text ?? "";
		set => messageLabel.Text = value;
	}

	public double Progress {
		get => progressBar.Value;
		set => progressBar.Value = Math.Clamp (value, 0, 1);
	}

	/// <summary>Begins an indented task: sets the message and logs the name in
	/// bold at the current indent (legacy BeginTask).</summary>
	public void BeginTask (string name)
	{
		if (!string.IsNullOrEmpty (name)) {
			indentLevel++;
			indents.Push (name);
			Message = name;
			AppendDetail (new string (' ', 2 * (indentLevel - 1)) + name + Environment.NewLine);
		} else {
			indents.Push (null!);
		}
	}

	/// <summary>Ends the innermost task (legacy EndTask: pop + restore message).</summary>
	public void EndTask ()
	{
		if (indents.Count > 0) {
			var msg = indents.Pop ();
			if (msg is not null && indentLevel > 0)
				indentLevel--;
			if (msg is not null)
				Message = msg;
		}
	}

	public void WriteText (string text) => AppendDetail (new string (' ', 2 * Math.Max (0, indentLevel - 1)) + text);

	/// <summary>QA accessor for the details log (verbatim, including indents).</summary>
	public string? DetailsTextForQa => detailsText.Text;

	void AppendDetail (string s)
	{
		detailsText.Text = detailsText.Text + s;
		detailsText.CaretIndex = detailsText.Text?.Length ?? 0;
	}

	/// <summary>Legacy ShowDone: full bar, Cancel hidden, Close shown, and the
	/// final state message (errors / warnings / success).</summary>
	public void ShowDone (bool warnings, bool errors)
	{
		Progress = 1;
		btnCancel.IsVisible = false;
		btnClose.IsVisible = true;
		Message = errors
			? "Operation completed with errors."
			: warnings ? "Operation completed with warnings."
			: "Operation successfully completed.";
	}
}
