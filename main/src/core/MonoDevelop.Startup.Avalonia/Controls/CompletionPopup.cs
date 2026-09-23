using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Services;

namespace MonoDevelop.AvaloniaShell.Controls;

/// <summary>
/// Code completion popup — the Avalonia port of the legacy CompletionListWindowGtk
/// (core/MonoDevelop.Ide/MonoDevelop.Ide.CodeCompletion): a borderless always-on-top
/// window with a list of completion entries (element icon + text + optional
/// description footer), keyboard navigation and commit semantics.
/// </summary>
public class CompletionPopup : Window
{
	public sealed record Item (string Text, string IconStock, string Description, string Category);

	readonly ListBox list = new ();
	readonly TextBlock descLabel = new ();
	readonly TextBlock catLabel = new ();
	readonly Border catBar = new ();
	List<Item> items = new ();

	/// <summary>Raised after a commit; check <see cref="SelectedText"/> before Hide.</summary>
	public event EventHandler? Committing;
	public event EventHandler? Cancelled;

	static IBrush EditorBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	public CompletionPopup ()
	{
		SystemDecorations = WindowDecorations.None;
		ShowInTaskbar = false;
		Topmost = true;
		Background = EditorBg (Color.Parse ("#252526"));

		var panel = new Grid { RowDefinitions = RowDefinitions.Parse ("*,Auto,Auto") };

		// Completion list (legacy: CompletionList with element icons)
		list.Background = Brushes.Transparent;
		list.BorderThickness = new Thickness (0);
		list.MaxHeight = 200;
		list.ItemTemplate = new FuncDataTemplate<Item> ((it, _) => {
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			var img = IconService.GetResourceImage (it.IconStock, 2);
			if (img is not null)
				row.Children.Add (new Image { Source = img, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center });
			row.Children.Add (new TextBlock { Text = it.Text, VerticalAlignment = VerticalAlignment.Center });
			return row;
		});
		var listBorder = new Border { Child = list, Padding = new Thickness (2, 4) };
		panel.Children.Add (listBorder);
		Grid.SetRow (listBorder, 0);

		// Description footer (legacy: selected item description under a separator)
		var sep = new Border { Height = 1, Background = new SolidColorBrush (Color.Parse ("#88888840")) };
		panel.Children.Add (sep);
		Grid.SetRow (sep, 1);

		var descPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, Margin = new Thickness (6, 3, 6, 3) };
		descLabel.TextWrapping = TextWrapping.Wrap;
		descLabel.FontSize = 11;
		descLabel.Opacity = 0.9;
		catLabel.FontSize = 11;
		catLabel.FontWeight = FontWeight.Bold;
		catBar.Child = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		((StackPanel)catBar.Child!).Children.Add (catLabel);
		((StackPanel)catBar.Child!).Children.Add (descLabel);
		descPanel.Children.Add (catBar);
		var descScroll = new ScrollViewer {
			Content = descPanel,
			MaxHeight = 46,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
		};
		panel.Children.Add (descScroll);
		Grid.SetRow (descScroll, 2);

		Content = new Border {
			Child = panel,
			Background = EditorBg (Color.Parse ("#252526")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			MinWidth = 280,
			MaxWidth = 460,
			ClipToBounds = true,
		};

		list.SelectionChanged += (_, _) => UpdateDescription ();
		LostFocus += OnSelfLostFocus;
		list.PointerReleased += (_, e) => { e.Handled = true; RequestCommit (); };
		Opened += (_, _) => SelectFirst ();
	}

	void UpdateDescription ()
	{
		if (SelectedItem is { } it) {
			descLabel.Text = string.IsNullOrEmpty (it.Description) ? it.Text : it.Description;
			catLabel.Text = it.Category;
			catBar.IsVisible = !string.IsNullOrEmpty (it.Category) || !string.IsNullOrEmpty (it.Description);
		}
	}

	public string? SelectedText => SelectedItem?.Text;
	Item? SelectedItem => list.SelectedItem as Item;

	/// <summary>Sets the entries and applies the current word prefix as filter.
	/// Hides (no-op) when nothing matches, like the legacy window.</summary>
	public void ShowItems (IEnumerable<Item> source, string filter)
	{
		items = source
			.Where (i => filter.Length == 0 || i.Text.StartsWith (filter, StringComparison.OrdinalIgnoreCase))
			.OrderBy (i => i.Text, StringComparer.OrdinalIgnoreCase)
			.Take (40)
			.ToList ();
		list.ItemsSource = items;
		if (items.Count == 0)
			return;
		SelectFirst ();
	}

	public int ItemCount => items.Count;

	void SelectFirst ()
	{
		list.SelectedIndex = items.Count > 0 ? 0 : -1;
		if (list.SelectedItem is not null)
			list.ScrollIntoView (list.SelectedItem);
		UpdateDescription ();
	}

	public void MoveSelection (int delta)
	{
		if (items.Count == 0)
			return;
		list.SelectedIndex = Math.Clamp (list.SelectedIndex + delta, 0, items.Count - 1);
		if (list.SelectedItem is not null)
			list.ScrollIntoView (list.SelectedItem);
		UpdateDescription ();
	}

	/// <summary>Legacy CompletionController PageDown/PageUp step of 8 items.</summary>
	public void PageMove (int pages) => MoveSelection (pages * 8);

	/// <summary>Commits the selected entry into the editor and hides.</summary>
	public void RequestCommit ()
	{
		var txt = SelectedText;
		if (txt is null) {
			Hide ();
			return;
		}
		Committing?.Invoke (this, EventArgs.Empty);
		Hide ();
	}

	public void RequestCancel ()
	{
		Cancelled?.Invoke (this, EventArgs.Empty);
		Hide ();
	}

	void OnSelfLostFocus (object? sender, RoutedEventArgs e) => Hide ();
}
