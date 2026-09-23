using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Controls;

/// <summary>
/// Hover tooltip — the Avalonia port of the legacy TooltipProvider pipeline
/// (Mono.TextEditor.Gui.TooltipProvider + TooltipItem): a borderless window
/// shown ~10px below the cursor with an icon and the signature/description of
/// the word under the mouse.
/// </summary>
public class EditorTooltipPopup : Window
{
	readonly TextBlock title = new ();
	readonly TextBlock body = new ();

	static IBrush EditorBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	public EditorTooltipPopup ()
	{
		SystemDecorations = WindowDecorations.None;
		ShowInTaskbar = false;
		Topmost = true;
		SizeToContent = SizeToContent.WidthAndHeight;
		Background = EditorBg (Color.Parse ("#252526"));

		// Title row (icon swapped in ShowAt when an element icon resolves) + body.
		titleRow.Children.Add (title);
		title.FontWeight = FontWeight.Bold;
		title.FontSize = 12;
		title.TextWrapping = TextWrapping.Wrap;
		body.FontSize = 12;
		body.Opacity = 0.9;
		body.TextWrapping = TextWrapping.Wrap;
		body.MaxWidth = 420;
		panel.Children.Add (titleRow);
		panel.Children.Add (body);

		Content = new Border {
			Child = panel,
			Background = EditorBg (Color.Parse ("#252526")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			Padding = new Thickness (8, 6),
			MaxWidth = 440,
		};
	}

	readonly StackPanel panel = new () { Orientation = Avalonia.Layout.Orientation.Vertical, Spacing = 2 };
	readonly StackPanel titleRow = new () { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };

	/// <summary>Shows the tooltip at screen pixel coordinates (legacy
	/// ShowAndPositionTooltip: cursor position +10px below).</summary>
	public void ShowAtScreen (Window parent, PixelPoint screenPoint, string? header, string? description, string? iconStock)
	{
		title.Text = header ?? "";
		title.IsVisible = !string.IsNullOrEmpty (header);
		body.Text = description ?? "";
		body.IsVisible = !string.IsNullOrEmpty (description);

		// Icon row: rebuild the title row children (icon + title) each show.
		titleRow.Children.Clear ();
		var img = Services.IconService.GetResourceImage (iconStock ?? "element-other-declaration-16", 2);
		if (img is not null)
			titleRow.Children.Add (new Image { Source = img, Width = 16, Height = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
		titleRow.Children.Add (title);

		Position = new PixelPoint (screenPoint.X + 14, screenPoint.Y + 20);
		Show (parent);
	}

	public void HideTooltip () => Hide ();
}
