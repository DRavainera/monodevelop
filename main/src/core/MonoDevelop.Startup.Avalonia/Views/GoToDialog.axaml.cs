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
	readonly record struct Item (string Name, string Path, bool IsType, string Detail);

	readonly List<Item> allItems = new ();
	readonly ListBox list = new () { Background = Brushes.Transparent };
	readonly TextBlock kindLabel = new () { FontSize = 11, Opacity = 0.7 };
	List<Item> current = new ();

	public string Kind => Title?.Contains ("Type", StringComparison.Ordinal) == true ? "Type" : "File";

	public GoToDialog ()
	{
		Title = "Go To File";
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
			allItems.Add (new Item (Path.GetFileName (f), f, IsType: false, Path.GetRelativePath (dir, f)));

		if (Kind == "Type") {
			foreach (var f in allItems.Select (i => i.Path).Where (p => p.EndsWith (".cs", StringComparison.Ordinal)).ToList ()) {
				try {
					foreach (var (name, kind) in ScanTypes (File.ReadAllText (f))) {
						var rel = Path.GetRelativePath (dir, f);
						allItems.Add (new Item (name, f, IsType: true, $"{kind} — {rel}"));
					}
				} catch { }
			}
		}
	}

	// Quick regex scan of type declarations (same purpose as the Roslyn category).
	static IEnumerable<(string Name, string Kind)> ScanTypes (string text)
	{
		var rx = new System.Text.RegularExpressions.Regex (
			@"\b(class|interface|struct|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)",
			System.Text.RegularExpressions.RegexOptions.Compiled);
		foreach (System.Text.RegularExpressions.Match m in rx.Matches (text))
			yield return (m.Groups [2].Value, m.Groups [1].Value);
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
			MainWindow.Instance?.OpenFileDocument (it.Path);
			Close ();
		}
	}
}
