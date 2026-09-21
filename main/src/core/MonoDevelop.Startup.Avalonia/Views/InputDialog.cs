using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Minimal text prompt window styled like the legacy GTK MessageService
/// <see cref="GetText"/> prompts (title, question label, text entry, OK/Cancel).
/// </summary>
public class InputDialog : Window
{
	readonly TextBox _entry = new () { MinWidth = 260 };

	/// <summary>True when OK was pressed.</summary>
	public bool Confirmed { get; private set; }

	/// <summary>The entered value (empty when cancelled).</summary>
	public string Value => _entry.Text ?? "";

	public InputDialog (string title, string label, string initialValue = "")
	{
		Title = title;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		CanResize = false;
		SizeToContent = SizeToContent.WidthAndHeight;
		ShowInTaskbar = false;
		// Legacy prompts run modal; use ExtendClientAreaToDecorationsHint=false so the
		// dialog keeps the Avalonia chrome like every other window in the new shell.
		TransparencyLevelHint = [WindowTransparencyLevel.None];

		var ok = new Button { Content = "_OK" };
		var cancel = new Button { Content = "_Cancel" };
		ok.Click += (_, _) => { Confirmed = true; Close (); };
		cancel.Click += (_, _) => Close ();
		_entry.KeyDown += (_, e) => {
			if (e.Key == global::Avalonia.Input.Key.Enter) { Confirmed = true; Close (); e.Handled = true; }
			else if (e.Key == global::Avalonia.Input.Key.Escape) { Close (); e.Handled = true; }
		};

		Content = new StackPanel {
			Margin = new Thickness (14),
			Spacing = 10,
			Children = {
				new TextBlock { Text = label, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
				_entry,
				new StackPanel {
					Orientation = Orientation.Horizontal,
					HorizontalAlignment = HorizontalAlignment.Right,
					Spacing = 8,
					Children = { cancel, ok },
				},
			},
		};

		Opened += (_, _) => {
			_entry.Text = initialValue;
			_entry.Focus ();
			_entry.SelectAll ();
		};
	}
}
