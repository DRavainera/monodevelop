using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy Go To File/Type (SearchPopupWindow + FileSearchCategory):
/// a popup with a text entry on top and a filtered list below. Items are
/// "Name — relative path" rows with the file/type icon; typing filters with the
/// same subsequence ranking (prefix > word-start > subsequence); Enter/double-click
/// opens the selection; Escape closes.
/// </summary>
public class GoToDialog : Window
{
	readonly record struct Item (string Name, string Path, bool IsType, string Detail, int Line);

	readonly List<Item> allItems = new ();
	readonly ListBox list = new () { Background = Brushes.Transparent };
	readonly TextBlock kindLabel = new () { FontSize = 11, Opacity = 0.7 };
	List<Item> current = new ();

	public string Kind => Title?.Contains ("Type", StringComparison.Ordinal) == true ? "Type" : "File";

	// kind = window title selecting the mode: null → "Go To File", "Go To Type" (Ctrl T).
	public GoToDialog (string? kind = null)
	{
		// The title must be set BEFORE BuildSource: it decides whether the .cs
		// scan for types runs at all.
		Title = string.IsNullOrEmpty (kind) ? "Go To File" : kind;
		Width = 560;
		Height = 420;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		ShowInTaskbar = false;
		Background = (IBrush)Application.Current!.FindResource ("IdeWindowBgBrush")!;

		BuildSource ();

		var entry = new TextBox {
			Watermark = Kind == "Type" ? "Search types…" : "Search files…",
			FontSize = 14,
			Padding = new Thickness (10, 8),
			BorderThickness = new Thickness (0, 0, 0, 1),
		};
		entry.BorderBrush = (IBrush)Application.Current.FindResource ("IdeBorderBrush")!;
		entry.TextChanged += (_, e) => Filter (entry.Text ?? "");
		entry.KeyDown += (_, e) => {
			if (e.Key == Key.Down && list.ItemCount > 0) {
				list.SelectedIndex = Math.Min (list.SelectedIndex + 1, list.ItemCount - 1);
				list.ScrollIntoView (list.SelectedItem);
				e.Handled = true;
			} else if (e.Key == Key.Up && list.ItemCount > 0) {
				list.SelectedIndex = Math.Max (list.SelectedIndex - 1, 0);
				list.ScrollIntoView (list.SelectedItem);
				e.Handled = true;
			} else if (e.Key == Key.Enter) {
				ActivateSelected ();
				e.Handled = true;
			} else if (e.Key == Key.Escape) {
				Close ();
				e.Handled = true;
			}
		};
		list.DoubleTapped += (_, _) => ActivateSelected ();

		var header = new DockPanel { LastChildFill = false };
		var title = new TextBlock {
			Text = Kind == "Type" ? "Go To Type" : "Go To File",
			FontWeight = FontWeight.SemiBold,
			Margin = new Thickness (10, 6),
			FontSize = 12,
		};
		title.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		kindLabel.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		DockPanel.SetDock (kindLabel, Dock.Right);
		header.Children.Add (kindLabel);
		header.Children.Add (title);

		var root = new DockPanel { LastChildFill = true };
		DockPanel.SetDock (header, Dock.Top);
		DockPanel.SetDock (entry, Dock.Top);
		root.Children.Add (header);
		root.Children.Add (entry);
		var contentBorder = new Border { Child = list, Margin = new Thickness (4) };
		root.Children.Add (contentBorder);
		Content = root;

		list.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));

		Opened += (_, _) => {
			entry.Focus ();
			Filter ("");
		};
	}

	// FileSearchCategory.GenerateAllFiles: all files of all projects + open documents;
	// Go To Type lists .cs types (class/interface/struct/enum declarations).
	void BuildSource ()
	{
		var dir = MainWindow.Instance?.LoadedSolutionDirectory ();
		if (dir is null || !Directory.Exists (dir))
			return;
		var files = Directory.EnumerateFiles (dir, "*", SearchOption.AllDirectories)
			.Where (f => !f.Contains ("/bin/") && !f.Contains ("/obj/") && !f.Contains ("/.git/"))
			.Where (f => f.EndsWith (".cs", StringComparison.Ordinal)
				|| f.EndsWith (".xaml", StringComparison.Ordinal)
				|| f.EndsWith (".axaml", StringComparison.Ordinal)
				|| f.EndsWith (".json", StringComparison.Ordinal)
				|| f.EndsWith (".csproj", StringComparison.Ordinal)
				|| f.EndsWith (".sln", StringComparison.Ordinal)
				|| f.EndsWith (".md", StringComparison.Ordinal)
				|| f.EndsWith (".xml", StringComparison.Ordinal))
			.Take (5000);
		foreach (var f in files)
			allItems.Add (new Item (Path.GetFileName (f), f, IsType: false, Path.GetRelativePath (dir, f), 0));

		if (Kind == "Type") {
			// Same scanner the search popup uses (SearchPopupWindow RoslynSearchCategory
			// parity): line numbers + container for the detail column.
			foreach (var f in allItems.Select (i => i.Path).Where (p => p.EndsWith (".cs", StringComparison.Ordinal)).ToList ()) {
				try {
					foreach (var hit in MainWindow.ScanSymbols (File.ReadAllText (f))) {
						if (hit.Kind is not ("class" or "interface" or "struct" or "enum"))
							continue;
						var rel = Path.GetRelativePath (dir, f);
						allItems.Add (new Item (hit.Name, f, IsType: true, $"{hit.Kind} — {rel} : {hit.Line}", hit.Line));
					}
				} catch { }
			}
		}
	}

	void Filter (string pattern)
	{
		current = Rank (pattern).ToList ();
		list.Items.Clear ();
		foreach (var it in current.Take (100)) {
			var row = new Grid { ColumnDefinitions = new ColumnDefinitions ("Auto,*,Auto") };
			var bmp = IconService.GetImage (it.IsType ? "md-class"
				: it.Name.EndsWith (".cs", StringComparison.Ordinal) ? "md-class-file"
				: "md-empty-file-icon");
			if (bmp is not null)
				row.Children.Add (new Avalonia.Controls.Image {
					Source = bmp, Width = 16, Height = 16, Margin = new Thickness (4, 0, 6, 0),
					VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
				});
			var name = new TextBlock { Text = it.Name, FontWeight = FontWeight.SemiBold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
			var detail = new TextBlock { Text = it.Detail, FontSize = 11, Opacity = 0.65, Margin = new Thickness (8, 0, 6, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,				TextTrimming = TextTrimming.CharacterEllipsis };
			Grid.SetColumn (name, 1);
			Grid.SetColumn (detail, 2);
			row.Children.Add (name);
			row.Children.Add (detail);
			list.Items.Add (new ListBoxItem { Tag = it, Content = row });
		}
		kindLabel.Text = $"{current.Count} {Kind.ToLowerInvariant()}(s)";
		if (list.ItemCount > 0)
			list.SelectedIndex = 0;
	}

	// Legacy ranking: prefix matches first, then word-start, then subsequence;
	// alphabetically inside each bucket (SearchCategory.DataItemComparer).
	IEnumerable<Item> Rank (string pattern)
	{
		var src = allItems.Where (i => Kind == "Type" ? i.IsType : !i.IsType);
		pattern = pattern.Trim ();
		if (pattern.Length == 0)
			return src.OrderBy (i => i.Name, StringComparer.OrdinalIgnoreCase);
		IEnumerable<Item> Bucket (Func<Item, bool> pred) => src.Where (pred).OrderBy (i => i.Name, StringComparer.OrdinalIgnoreCase);
		return Bucket (i => i.Name.StartsWith (pattern, StringComparison.OrdinalIgnoreCase))
			.Concat (Bucket (i => !i.Name.StartsWith (pattern, StringComparison.OrdinalIgnoreCase) && i.Name.Contains (pattern, StringComparison.OrdinalIgnoreCase)))
			.Concat (Bucket (i => Subsequence (pattern, i.Name)));
	}

	static bool Subsequence (string pattern, string name)
	{
		int at = 0;
		foreach (var ch in name) {
			if (char.ToLowerInvariant (ch) == char.ToLowerInvariant (pattern [at])) {
				at++;
				if (at == pattern.Length)
					return true;
			}
		}
		return false;
	}

	void ActivateSelected ()
	{
		if (list.SelectedItem is ListBoxItem { Tag: Item it }) {
			if (it.IsType && it.Line > 0)
				MainWindow.Instance?.OpenFileDocumentAtLine (it.Path, it.Line);
			else
				MainWindow.Instance?.OpenFileDocument (it.Path);
			Close ();
		}
	}

	// ----- QA hooks (automated runs drive the dialog without UI navigation) -----

	/// <summary>QA: applies the filter and returns the number of matches.</summary>
	public int QaFilter (string pattern)
	{
		Filter (pattern);
		return current.Count;
	}

	/// <summary>QA: name + detail of the first match after QaFilter.</summary>
	public string QaFirstItem => current.Count > 0 ? $"{current [0].Name} | {current [0].Detail}" : "-";

	/// <summary>QA: selects the first row and runs the activation path.</summary>
	public void QaActivateFirst ()
	{
		if (list.ItemCount > 0)
			list.SelectedIndex = 0;
		ActivateSelected ();
	}
}
