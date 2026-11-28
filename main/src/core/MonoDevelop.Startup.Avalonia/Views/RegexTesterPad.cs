using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Avalonia port of MonoDevelop.RegexToolkit's RegexToolkitWidget. Keeps the
/// legacy feature set: pattern, replace toggle, options list, input text,
/// match tree and replaced text. Regex evaluation runs off the UI thread with a
/// timeout and results are marshalled back, replacing the legacy abortable Thread.
/// </summary>
public sealed class RegexTesterPad : Grid
{
	readonly TextBox pattern = new () { Watermark = "Regular expression", FontFamily = new FontFamily ("monospace") };
	readonly CheckBox replaceCheck = new () { Content = "Replace" };
	readonly TextBox replacement = new () { IsEnabled = false, FontFamily = new FontFamily ("monospace") };
	readonly CheckBox expandCheck = new () { Content = "Expand matches", IsChecked = true };
	readonly TextBox input = new () { AcceptsReturn = true, FontFamily = new FontFamily ("monospace"), MinHeight = 120 };
	readonly TextBox replaced = new () { IsReadOnly = true, AcceptsReturn = true, FontFamily = new FontFamily ("monospace"), MinHeight = 80 };
	readonly TreeView results = new ();
	readonly List<OptionRow> options = new ();
	uint generation;

	sealed class OptionRow
	{
		public RegexOptions Flag;
		public CheckBox Box = null!;
	}

	public RegexTesterPad ()
	{
		ColumnDefinitions.Add (new ColumnDefinition (new GridLength (1, GridUnitType.Star)));
		ColumnDefinitions.Add (new ColumnDefinition (new GridLength (320, GridUnitType.Pixel)));

		var left = new DockPanel { Margin = new Thickness (6) };
		var top = new StackPanel ();
		top.Children.Add (new TextBlock { Text = "Regular Expression", FontWeight = FontWeight.SemiBold });
		top.Children.Add (pattern);
		var replaceRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness (0, 6, 0, 0) };
		replaceRow.Children.Add (replaceCheck);
		replaceRow.Children.Add (replacement);
		top.Children.Add (replaceRow);
		top.Children.Add (expandCheck);
		DockPanel.SetDock (top, Dock.Top);
		left.Children.Add (top);

		var inputLabel = new TextBlock { Text = "Input", FontWeight = FontWeight.SemiBold, Margin = new Thickness (0, 8, 0, 2) };
		DockPanel.SetDock (inputLabel, Dock.Top);
		left.Children.Add (inputLabel);
		var inputScroll = new ScrollViewer { Content = input, Height = 140 };
		DockPanel.SetDock (inputScroll, Dock.Top);
		left.Children.Add (inputScroll);

		var matchesTab = new TabItem { Header = "Matches" };
		matchesTab.Content = new ScrollViewer { Content = results };
		var replacedTab = new TabItem { Header = "Replaced" };
		replacedTab.Content = new ScrollViewer { Content = replaced };
		var tabs = new TabControl ();
		tabs.Items.Add (matchesTab);
		tabs.Items.Add (replacedTab);
		left.Children.Add (tabs);
		Children.Add (left);

		var right = new StackPanel { Margin = new Thickness (6) };
		Grid.SetColumn (right, 1);
		right.Children.Add (new TextBlock { Text = "Options:", FontWeight = FontWeight.SemiBold });
		var optionsPanel = new StackPanel ();
		foreach (var (flag, label) in new [] {
			(RegexOptions.IgnorePatternWhitespace, "Ignore Whitespace"),
			(RegexOptions.IgnoreCase, "Ignore case"),
			(RegexOptions.Singleline, "Single line"),
			(RegexOptions.Multiline, "Multi line"),
			(RegexOptions.ExplicitCapture, "Explicit Capture"),
			(RegexOptions.RightToLeft, "Right to left"),
		}) {
			var box = new CheckBox { Content = label };
			options.Add (new OptionRow { Flag = flag, Box = box });
			optionsPanel.Children.Add (box);
		}
		right.Children.Add (new ScrollViewer { Content = optionsPanel, MaxHeight = 180 });
		right.Children.Add (new TextBlock {
			Text = "Select a match, group or capture in the results to select it in the input.",
			TextWrapping = TextWrapping.Wrap,
			Opacity = 0.7,
			Margin = new Thickness (0, 8, 0, 0),
		});
		Children.Add (right);

		pattern.PropertyChanged += (_, e) => { if (e.Property.Name == TextBox.TextProperty.Name) Schedule (); };
		input.PropertyChanged += (_, e) => { if (e.Property.Name == TextBox.TextProperty.Name) Schedule (); };
		replacement.PropertyChanged += (_, e) => { if (e.Property.Name == TextBox.TextProperty.Name) Schedule (); };
		foreach (var o in options)
			o.Box.IsCheckedChanged += (_, _) => Schedule ();
		replaceCheck.IsCheckedChanged += (_, _) => {
			replacement.IsEnabled = replaceCheck.IsChecked == true;
			Schedule ();
		};
		expandCheck.IsCheckedChanged += (_, _) => Schedule ();
		results.SelectionChanged += (_, _) => SelectMatch ();
		Schedule ();
	}

	void Schedule ()
	{
		var id = ++generation;
		string p = pattern.Text ?? "", i = input.Text ?? "", r = replacement.Text ?? "";
		var flags = GetOptions ();
		bool doReplace = replaceCheck.IsChecked == true;
		bool expand = expandCheck.IsChecked == true;
		_ = System.Threading.Tasks.Task.Run (() => Evaluate (id, p, i, r, flags, doReplace));
		generationHoldExpand = expand;
	}

	bool generationHoldExpand;

	void Evaluate (uint id, string p, string i, string r, RegexOptions flags, bool doReplace)
	{
		var root = new List<TreeViewItem> ();
		string replacedText = "";
		string? error = null;
		try {
			var regex = new Regex (p, flags, TimeSpan.FromSeconds (2));
			foreach (Match match in regex.Matches (i)) {
				var item = new TreeViewItem { Header = $"Match '{match.Value}'", Tag = (match.Index, match.Length) };
				int n = 0;
				foreach (Group group in match.Groups) {
					var name = regex.GroupNameFromNumber (n);
					var gi = new TreeViewItem {
						Header = group.Success ? $"Group '{name}':'{group.Value}'" : $"Group '{name}' not found",
						Tag = group.Success ? (group.Index, group.Length) : (-1, 0),
					};
					if (group.Success)
						foreach (Capture capture in match.Captures)
							gi.Items.Add (new TreeViewItem { Header = $"Capture '{capture.Value}'", Tag = (capture.Index, capture.Length) });
					item.Items.Add (gi);
					n++;
				}
				root.Add (item);
			}
			if (root.Count == 0)
				root.Add (new TreeViewItem { Header = "No matches", Tag = (-1, 0) });
			if (doReplace)
				replacedText = regex.Replace (i, r);
		} catch (ArgumentException ex) {
			error = "Invalid expression: " + ex.Message;
		} catch (RegexMatchTimeoutException) {
			error = "Regex timed out";
		}

		Avalonia.Threading.Dispatcher.UIThread.Post (() => {
			if (id != generation)
				return;
			results.Items.Clear ();
			foreach (var item in root)
				results.Items.Add (item);
			if (generationHoldExpand)
				foreach (var item in root)
					item.IsExpanded = true;
			replaced.Text = replacedText;
			if (error is not null)
				results.Items.Add (new TreeViewItem { Header = error, Tag = (-1, 0) });
		});
	}

	void SelectMatch ()
	{
		if (results.SelectedItem is not TreeViewItem { Tag: (int index, int length) })
			return;
		if (index < 0)
			return;
		input.SelectionStart = index;
		input.SelectionEnd = Math.Min (index + length, input.Text?.Length ?? 0);
		input.Focus ();
	}

	RegexOptions GetOptions ()
	{
		var flags = RegexOptions.None;
		foreach (var o in options)
			if (o.Box.IsChecked == true)
				flags |= o.Flag;
		return flags;
	}
}
