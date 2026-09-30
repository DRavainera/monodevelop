using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Execution Mode Selector — the Avalonia port of the legacy Xwt
/// ExecutionModeSelectorDialog (MonoDevelop.Ide.Execution): a Run
/// Configurations list on top, an Execution Modes tree below (mode sets with
/// their modes as children, single-child sets collapse like the legacy
/// MoveNext/Remove cleanup), and Run/Cancel. Run exposes the selected config +
/// mode pair; the legacy Run button label becomes the selected mode set name.
/// </summary>
public class ExecutionModeSelectorDialog : Window
{
	/// <summary>A selectable run configuration of the target item.</summary>
	public sealed class RunConfig
	{
		public string Name { get; init; } = "";
		/// <summary>Optional display detail (e.g. "Default execution mode").</summary>
		public string? Detail { get; init; }
	}

	/// <summary>An execution mode offered under a mode set.</summary>
	public sealed class ModeEntry
	{
		public string Name { get; init; } = "";
		public string Id { get; init; } = "";
		/// <summary>Mode set this entry belongs to (the legacy TreeStore parent).</summary>
		public string SetName { get; init; } = "";
	}

	readonly ListBox listConfigs = new ();
	readonly TreeView treeModes = new ();
	readonly Button runButton = new ();

	IReadOnlyList<RunConfig> configs = [];
	IReadOnlyList<ModeEntry> modes = [];
	readonly Dictionary<string, TreeViewItem> setNodes = new ();
	readonly Dictionary<string, TreeViewItem> leafItems = new ();
	// The programmatic pre-selection can be dropped when the TreeView materializes
	// its containers on ShowDialog — remember the last valid mode so Run/QA keep
	// working like the legacy SelectRow (which survives the same lifecycle).
	string? lastModeId;
	RunConfig? lastConfig;
	int lastConfigIndex = -1;

	static IBrush DialogBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeWindowBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	static IBrush Fg () =>
		Application.Current?.TryGetResource ("IdeFgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : Brushes.White;

	/// <summary>Result when Run is pressed; null when cancelled.</summary>
	public (RunConfig Config, ModeEntry Mode)? Result { get; private set; }

	/// <summary>QA accessors mirroring the legacy button state and selection.</summary>
	public bool IsRunEnabledForQa => runButton.IsEnabled;
	public string RunLabelForQa => runButton.Content as string ?? "";
	public string? SelectedConfigNameForQa => listConfigs.SelectedItem is RunConfig c ? c.Name : null;
	public string? SelectedModeIdForQa => SelectedModeId ();
	public int ConfigCountForQa => configs.Count;
	public int ModeCountForQa => modes.Count;

	string? SelectedModeId ()
	{
		if (treeModes.SelectedItem is not TreeViewItem item)
			return null;
		// Mode leaves carry the mode id in Tag; set parents carry their name.
		if (item.Tag is string id)
			return id;
		return FindLeafId (item);
	}

	// A set parent selected without an expanded leaf: resolve to its first mode
	// like the legacy select-first-visible fallback.
	string? FindLeafId (TreeViewItem setItem)
	{
		foreach (var child in setItem.Items)
			if (child is TreeViewItem { Tag: string id })
				return id;
		return null;
	}

	public ExecutionModeSelectorDialog ()
	{
		Title = "Execution Mode Selector";
		Width = 500;
		Height = 400;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		Background = DialogBg (Color.Parse ("#2d2d30"));
		FontFamily = Application.Current?.Resources.TryGetValue ("ContentFontFamily", out var ff) == true && ff is Avalonia.Media.FontFamily fam
			? fam : FontFamily.Default;

		var root = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,*,Auto,*,Auto"), Margin = new Thickness (14, 12) };

		root.Children.Add (MakeLabel ("Run Configurations:", 0));
		root.Children.Add (listConfigs);
		Grid.SetRow (listConfigs, 1);

		root.Children.Add (MakeLabel ("Execution Modes:", 2));

		treeModes.Background = Brushes.Transparent;
		treeModes.BorderThickness = new Thickness (1);
		treeModes.BorderBrush = new SolidColorBrush (Color.Parse ("#88888860"));
		treeModes.SelectionChanged += (_, _) => UpdateButtons ();
		Grid.SetRow (treeModes, 3);
		root.Children.Add (treeModes);

		var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness (0, 10, 0, 0) };
		var cancel = new Button { Content = "Cancel", MinWidth = 80 };
		cancel.Click += (_, _) => Close ();
		runButton.Content = "Run";
		runButton.MinWidth = 80;
		runButton.IsEnabled = false; // legacy UpdateButtons: nothing selected → disabled
		runButton.Click += (_, _) => {
			if ((listConfigs.SelectedItem as RunConfig ?? lastConfig) is RunConfig cfg && (SelectedModeId () ?? lastModeId) is { } id) {
				foreach (var m in modes) {
					if (m.Id == id) {
						Result = (cfg, m);
						break;
					}
				}
			}
			Close ();
		};
		buttons.Children.Add (cancel);
		buttons.Children.Add (runButton);
		Grid.SetRow (buttons, 4);
		root.Children.Add (buttons);

		listConfigs.Background = Brushes.Transparent;
		listConfigs.SelectionChanged += (_, _) => UpdateButtons ();

		Content = root;
	}

	TextBlock MakeLabel (string text, int row)
	{
		var l = new TextBlock { Text = text, Margin = new Thickness (0, 6, 0, 2) };
		l.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		Grid.SetRow (l, row);
		return l;
	}

	/// <summary>Populates both lists. Call before ShowDialog.</summary>
	/// <param name="runConfigs">Run configurations of the target (legacy listConfigs.Fill).</param>
	/// <param name="modeEntries">Flattened mode list; entries sharing a SetName are
	/// nested under one set node like the legacy TreeStore.</param>
	/// <param name="selectedModeId">Mode id to preselect (legacy currentMode).</param>
	public void Load (IReadOnlyList<RunConfig> runConfigs, IReadOnlyList<ModeEntry> modeEntries, string? selectedModeId = null)
	{
		configs = runConfigs ?? [];
		modes = modeEntries ?? [];

		listConfigs.Items.Clear ();
		foreach (var c in configs)
			listConfigs.Items.Add (c);
		if (configs.Count > 0)
			listConfigs.SelectedIndex = 0; // legacy Load: SelectedConfiguration = configs.FirstOrDefault()

		treeModes.Items.Clear ();
		setNodes.Clear ();
		leafItems.Clear ();
		foreach (var m in modes) {
			if (!setNodes.TryGetValue (m.SetName, out var setItem)) {
				setItem = new TreeViewItem { Header = m.SetName, Foreground = Fg () };
				setNodes[m.SetName] = setItem;
			}
			var leaf = new TreeViewItem { Header = m.Name, Tag = m.Id, Foreground = Fg () };
			leafItems[m.Id] = leaf;
			setItem.Items.Add (leaf);
		}
		// Legacy cleanup: a mode set with a single mode doesn't need the parent —
		// hoist the lone leaf to the root (legacy MoveNext/pos.Remove()).
		foreach (var (setName, setItem) in setNodes) {
			if (setItem.Items.Count == 1 && setItem.Items [0] is TreeViewItem lone) {
				lone.Header = setName == lone.Header as string ? lone.Header : $"{setName} · {lone.Header}";
				treeModes.Items.Add (lone);
			} else {
				setItem.IsExpanded = true;
				treeModes.Items.Add (setItem);
			}
		}

		// Legacy LoadModes: preselect currentMode, else the first visible row.
		TreeViewItem? toSelect = selectedModeId is not null && leafItems.TryGetValue (selectedModeId, out var sel) ? sel : null;
		toSelect ??= treeModes.Items.Count > 0 ? treeModes.Items [0] as TreeViewItem : null;
		if (toSelect is not null) {
			treeModes.SelectedItem = toSelect;
			if (toSelect.Parent is TreeViewItem p)
				p.IsExpanded = true;
		}
		UpdateButtons ();
		// Re-assert the selection once the visual tree exists (ShowDialog may
		// clear a selection made before attach).
		// Re-assert both selections once the visual tree exists (ShowDialog may
		// clear selections made before attach).
		Opened += (_, _) => {
			if (lastConfigIndex >= 0 && listConfigs.SelectedIndex < 0)
				listConfigs.SelectedIndex = lastConfigIndex;
			if (lastModeId is not null && leafItems.TryGetValue (lastModeId, out var item))
				treeModes.SelectedItem = item;
		};
	}

	/// <summary>QA hook: performs the Run action (capture the selection pair and
	/// close) without synthesizing a button click.</summary>
	public void AcceptForQa ()
	{
		if ((listConfigs.SelectedItem as RunConfig ?? lastConfig) is RunConfig cfg && (SelectedModeId () ?? lastModeId) is { } id) {
			foreach (var m in modes) {
				if (m.Id == id) {
					Result = (cfg, m);
					break;
				}
			}
		}
		Close ();
	}

	// Legacy UpdateButtons: Run enabled only with both selections; label = set name.
	void UpdateButtons ()
	{
		ModeEntry? mode = null;
		if (SelectedModeId () is { } id) {
			foreach (var m in modes) {
				if (m.Id == id) {
					mode = m;
					break;
				}
			}
		}
		runButton.IsEnabled = listConfigs.SelectedItem is RunConfig && mode is not null;
		runButton.Content = mode is not null ? mode.SetName : "Run";
		if (mode is not null)
			lastModeId = mode.Id;
		if (listConfigs.SelectedItem is RunConfig sel) {
			lastConfig = sel;
			lastConfigIndex = listConfigs.SelectedIndex;
		}
	}
}
