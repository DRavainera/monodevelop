// AttachToProcessPanel.axaml.cs — port of the legacy AttachToProcessDialog
// (MonoDevelop.Debugger) as an in-window pad tab, not an OS-decorated popup
// window: Avalonia chrome only, like every other surface in the shell. Lists
// real attachable processes (PID / Name / Description), a text filter and a
// Refresh button; selecting a row + Attach raises AttachRequested. Processes
// come from /proc like the legacy NetCoreProcessAttacher (enumerates
// /proc/<pid>, skips kernels/self).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

public record AttachableProcess (int Pid, string Name, string Description);

public partial class AttachToProcessPanel : UserControl
{
	readonly ListBox processList;
	readonly TextBox filterBox;
	readonly Button attachButton;
	readonly TextBlock countLabel;
	internal List<AttachableProcess> allProcesses = new ();

	/// <summary>QA accessor: rows currently realized in the list.</summary>
	internal int RealizedRowsForQa => processList.GetRealizedContainers ()?.Count () ?? -1;

	/// <summary>PID picked via Attach (row selected + button or double click).</summary>
	public int? SelectedPid { get; private set; }

	/// <summary>Raised when the user attaches to a process.</summary>
	public event EventHandler<int>? AttachRequested;

	public AttachToProcessPanel ()
	{
		filterBox = new TextBox { Watermark = "Filter processes", FontSize = 12 };
		filterBox.PropertyChanged += (_, e) => {
			if (e.Property == TextBox.TextProperty)
				FillList ();
		};

		var refreshButton = new Button { Content = "Refresh", Padding = new Thickness (10, 3) };
		refreshButton.Click += (_, _) => { ScanProcesses (); FillList (); };

		processList = new ListBox { Background = Brushes.Transparent };
		processList.DoubleTapped += (_, _) => Attach ();

		attachButton = new Button {
			Content = "Attach",
			Padding = new Thickness (16, 4),
			IsEnabled = false,
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		attachButton.Click += (_, _) => Attach ();

		var cancelButton = new Button {
			Content = "Close",
			Padding = new Thickness (16, 4),
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		cancelButton.Click += (_, _) => {
			// Tab equivalent of the legacy dialog Cancel: just clear the pick.
			SelectedPid = null;
		};

		countLabel = new TextBlock { FontSize = 11.5, Opacity = 0.75, VerticalAlignment = VerticalAlignment.Center };

		var topRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto") };
		Grid.SetColumn (filterBox, 0);
		topRow.Children.Add (filterBox);
		Grid.SetColumn (refreshButton, 1);
		topRow.Children.Add (refreshButton);

		var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
		buttons.Children.Add (cancelButton);
		buttons.Children.Add (attachButton);

		var root = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,*,Auto,Auto"), Margin = new Thickness (8, 6) };
		root.Children.Add (topRow);
		Grid.SetRow (processList, 1);
		root.Children.Add (processList);
		Grid.SetRow (countLabel, 2);
		root.Children.Add (countLabel);
		Grid.SetRow (buttons, 3);
		root.Children.Add (buttons);

		Content = root;

		processList.SelectionChanged += (_, _) =>
			attachButton.IsEnabled = processList.SelectedItem is ListBoxItem;

		ScanProcesses ();
		FillList ();
	}

	public void ScanProcesses ()
	{
		var result = new List<AttachableProcess> ();
		try {
			foreach (var pidDir in Directory.GetDirectories ("/proc")) {
				var name = Path.GetFileName (pidDir);
				if (!int.TryParse (name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
					continue;
				if (pid == Environment.ProcessId)
					continue;
				string exeName;
				try {
					var cmdline = File.ReadAllText (Path.Combine (pidDir, "cmdline")).Replace ('\0', ' ').Trim ();
					exeName = cmdline.Length > 0 ? cmdline : File.ReadAllText (Path.Combine (pidDir, "comm")).Trim ();
				} catch {
					continue; // kernel threads and unreadable entries are not attachable
				}
				if (exeName.Length == 0)
					continue;
				string desc = "";
				try {
					var stat = File.ReadAllText (Path.Combine (pidDir, "stat"));
					var state = stat.Length > 0 ? ExtractState (stat) : "";
					desc = state switch {
						"S" => "Sleeping",
						"R" => "Running",
						"D" => "Disk sleep",
						"T" => "Stopped",
						"Z" => "Zombie",
						_ => "",
					};
				} catch { }
				result.Add (new AttachableProcess (pid, exeName, desc));
			}
		} catch { }
		allProcesses = result.OrderBy (p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy (p => p.Pid).ToList ();
	}

	static string ExtractState (string stat)
	{
		// state is the field after the (comm) parenthesized section
		int close = stat.LastIndexOf (')');
		if (close < 0 || close + 2 >= stat.Length)
			return "";
		return stat.Substring (close + 2, 1);
	}

	void FillList ()
	{
		var filter = filterBox.Text?.Trim () ?? "";
		processList.Items.Clear ();
		IEnumerable<AttachableProcess> visible = allProcesses;
		if (filter.Length > 0)
			visible = visible.Where (p =>
				p.Name.Contains (filter, StringComparison.OrdinalIgnoreCase) ||
				p.Pid.ToString (CultureInfo.InvariantCulture).Contains (filter, StringComparison.Ordinal));
		var shown = visible.Take (300).ToList ();
		foreach (var p in shown) {
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
			panel.Children.Add (new TextBlock { Text = p.Pid.ToString (CultureInfo.InvariantCulture), FontSize = 11.5, Width = 70 });
			panel.Children.Add (new TextBlock { Text = p.Name, FontSize = 11.5, Width = 260, TextTrimming = TextTrimming.CharacterEllipsis });
			panel.Children.Add (new TextBlock { Text = p.Description, FontSize = 11.5, Opacity = 0.75 });
			processList.Items.Add (new ListBoxItem { Tag = p, Content = panel });
		}
		countLabel.Text = $"{shown.Count} of {allProcesses.Count} processes";
	}

	void Attach ()
	{
		if (processList.SelectedItem is ListBoxItem { Tag: AttachableProcess p }) {
			SelectedPid = p.Pid;
			AttachRequested?.Invoke (this, p.Pid);
		}
	}
}
