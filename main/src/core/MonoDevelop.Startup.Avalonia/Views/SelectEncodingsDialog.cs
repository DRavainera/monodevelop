using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Select Encodings — the Avalonia port of the legacy SelectEncodingsDialog
/// (MonoDevelop.Ide.Gui.Dialogs): two lists (available / selected encodings)
/// with Add/Remove/Up/Down, OK persists the selection to the same property the
/// legacy uses (MonoDevelop.Projects.Text.ConversionEncodings, space-separated
/// WebNames, UTF-16 forced second like TextEncoding.ConversionEncodings).
/// </summary>
public class SelectEncodingsDialog : Window
{
	public sealed record EncodingEntry (string Name, string WebName, int CodePage);

	readonly ListBox availableList = new ();
	readonly ListBox selectedList = new ();
	readonly Button okButton = new ();
	readonly Button addButton = new ();
	readonly Button removeButton = new ();
	readonly Button upButton = new ();
	readonly Button downButton = new ();

	static IBrush DialogBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeWindowBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	public SelectEncodingsDialog ()
	{
		Title = "Encodings";
		Width = 620;
		Height = 420;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		Background = DialogBg (Color.Parse ("#2d2d30"));

		var border = new Border {
			Background = DialogBg (Color.Parse ("#2d2d30")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			Padding = new Thickness (12, 10),
		};

		// Two list panes with the middle button column (legacy Gtk layout).
		var panes = new Grid {
			ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto,*"),
			MinHeight = 250,
		};

		panes.Children.Add (MakeListPane ("Available encodings:", availableList));
		Grid.SetColumn (panes.Children [0], 0);

		var mid = new StackPanel {
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Spacing = 6,
			Margin = new Thickness (8, 0),
		};
		addButton.Content = "Add >";
		addButton.Click += (_, _) => MoveSelected (availableList, selectedList);
		removeButton.Content = "< Remove";
		removeButton.Click += (_, _) => { MoveSelected (selectedList, availableList); EnsureSomethingSelected (); };
		upButton.Content = "Up";
		upButton.Click += (_, _) => MoveVertical (-1);
		downButton.Content = "Down";
		downButton.Click += (_, _) => MoveVertical (1);
		mid.Children.Add (addButton);
		mid.Children.Add (removeButton);
		mid.Children.Add (upButton);
		mid.Children.Add (downButton);
		panes.Children.Add (mid);
		Grid.SetColumn (mid, 1);

		panes.Children.Add (MakeListPane ("Selected encodings:", selectedList));
		Grid.SetColumn (panes.Children [^1], 2);

		// Buttons row: OK persists (legacy OnRespond), Cancel closes.
		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8,
			Margin = new Thickness (0, 10, 0, 0),
		};
		okButton.Content = "OK";
		okButton.MinWidth = 80;
		okButton.Click += (_, _) => { Persist (); Close (); };
		var cancel = new Button { Content = "Cancel", MinWidth = 80 };
		cancel.Click += (_, _) => Close ();
		buttons.Children.Add (cancel);
		buttons.Children.Add (okButton);

		var root = new StackPanel { Orientation = Orientation.Vertical };
		root.Children.Add (panes);
		root.Children.Add (buttons);
		border.Child = root;
		Content = border;

		LoadEncodings ();
	}

	Control MakeListPane (string header, ListBox list)
	{
		var label = new TextBlock { Text = header, FontSize = 12, Margin = new Thickness (0, 0, 0, 4) };
		label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		list.Background = new SolidColorBrush (Color.Parse ("#252528"));
		list.BorderBrush = new SolidColorBrush (Color.Parse ("#88888840"));
		list.BorderThickness = new Thickness (1);
		list.Height = 250;
		list.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<EncodingEntry> ((e, _) => {
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			var name = new TextBlock { Text = e?.Name, Width = 210, TextTrimming = TextTrimming.CharacterEllipsis };
			var web = new TextBlock { Text = e?.WebName, Opacity = 0.75 };
			name.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
			web.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
			row.Children.Add (name);
			row.Children.Add (web);
			return row;
		});
		var panel = new StackPanel { Orientation = Orientation.Vertical };
		panel.Children.Add (label);
		panel.Children.Add (list);
		return panel;
	}

	// Legacy population: all BCL encodings on the left, ConversionEncodings on
	// the right (excluding UTF-16, which the property pipeline re-inserts).
	void LoadEncodings ()
	{
		var selected = ConversionEncodingIds ();
		foreach (var e in Encoding.GetEncodings ().OrderBy (x => x.DisplayName, StringComparer.Ordinal)) {
			var entry = new EncodingEntry (e.DisplayName, e.Name, e.CodePage);
			if (selected.Contains (e.Name, StringComparer.OrdinalIgnoreCase))
				selectedList.Items.Add (entry);
			else
				availableList.Items.Add (entry);
		}
	}

	/// <summary>Reads the persisted ConversionEncodings (space-separated ids in
	/// MonoDevelop-properties.xml, like PropertyService.Get on the legacy).</summary>
	static HashSet<string> ConversionEncodingIds ()
	{
		var raw = Services.UserPreferences.Get ("MonoDevelop.Projects.Text.ConversionEncodings") ?? "";
		return raw.Split (' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet (StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>Legacy OnRespond: write the selected WebNames space-separated
	/// (TextEncoding.ConversionEncodings setter re-inserts UTF-16 in 2nd place).</summary>
	void Persist ()
	{
		var ids = selectedList.Items.OfType<EncodingEntry> ().Select (e => e.WebName).ToList ();
		if (!ids.Contains ("UTF-16", StringComparer.OrdinalIgnoreCase) && ids.Count > 0)
			ids.Insert (Math.Min (1, ids.Count), "UTF-16");
		Services.UserPreferences.Set ("MonoDevelop.Projects.Text.ConversionEncodings", string.Join (" ", ids));
	}

	/// <summary>Legacy MoveItem: move the selection between lists, keeping the
	/// next source row selected (best effort in Avalonia: clear selection).</summary>
	void MoveSelected (ListBox source, ListBox target)
	{
		var item = source.SelectedItem as EncodingEntry;
		if (item is null)
			return;
		source.Items.Remove (item);
		target.Items.Add (item);
		target.SelectedItem = item;
	}

	void EnsureSomethingSelected ()
	{
		if (selectedList.SelectedItem is null && selectedList.Items.Count > 0)
			selectedList.SelectedIndex = selectedList.Items.Count - 1;
	}

	/// <summary>Legacy OnUpClicked/OnDownClicked: swap the selected row with its
	/// neighbor (order matters: it defines the auto-detection priority).</summary>
	void MoveVertical (int delta)
	{
		int idx = selectedList.SelectedIndex;
		int next = idx + delta;
		if (idx < 0 || next < 0 || next >= selectedList.Items.Count)
			return;
		var item = selectedList.Items [idx]!;
		selectedList.Items.RemoveAt (idx);
		selectedList.Items.Insert (next, item);
		selectedList.SelectedIndex = next;
	}
}
