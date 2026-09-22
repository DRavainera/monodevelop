using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy DirtyFilesDialog (MonoDevelop.Ide.Gui.Dialogs): shown when
/// closing the workspace or quitting with modified documents. Lists the dirty
/// documents in a checkable tree (grouped by project like the Gtk TreeStore),
/// with the legacy actions Save and Quit/Close, Quit/Close and Cancel.
/// </summary>
public partial class DirtyFilesDialog : Window
{
	/// <summary>A document reference for the check tree (legacy Document).</summary>
	public sealed class DirtyDoc
	{
		public string Name { get; init; } = "";
		/// <summary>Project group label ("Project: X") or null for ungrouped docs.</summary>
		public string? ProjectGroup { get; init; }
		/// <summary>Action that persists the document (legacy Document.Save).</summary>
		public Func<Task>? SaveAsync { get; init; }
	}

	public enum DirtyResult { Cancel, Quit, SaveAndQuit }

	readonly List<(DirtyDoc Doc, CheckBox Box)> rows = new ();
	readonly Dictionary<string, List<CheckBox>> groups = new ();
	readonly Dictionary<string, CheckBox> groupChecks = new ();

	/// <summary>Result set by the pressed button (legacy Gtk.ResponseType).</summary>
	public DirtyResult Result { get; private set; } = DirtyResult.Cancel;

	public DirtyFilesDialog ()
	{
		InitializeComponent ();
	}

	/// <summary>Populates the tree. Call before ShowDialog.</summary>
	/// <param name="docs">Dirty documents, optionally grouped by project.</param>
	/// <param name="closeWorkspace">true = "close the workspace" wording and Save and Quit;
	/// false = "quit the application" wording (legacy constructor flag).</param>
	public void Load (IReadOnlyList<DirtyDoc> docs, bool closeWorkspace)
	{
		TitleText.Text = "Save Files";
		DescriptionText.Text = closeWorkspace
			? "Select which files should be saved before closing the workspace"
			: "Select which files should be saved before quitting the application";
		SaveAndQuitBtn.Content = closeWorkspace ? "_Save and Quit" : "_Save and Close";
		QuitBtn.Content = closeWorkspace ? "Quit" : "Close";

		// Legacy grouping: docs with an Owner get a "Project: {name}" parent node.
		foreach (var g in docs.Where (d => d.ProjectGroup is not null).GroupBy (d => d.ProjectGroup!)) {
			var gname = g.Key;
			var gcheck = new CheckBox {
				IsChecked = true,
				Content = gname, // legacy: "Project: {name}" label on the group node
				Foreground = Brushes.White,
				FontWeight = FontWeight.SemiBold
			};
			var children = new List<CheckBox> ();
			foreach (var d in g) {
				var box = new CheckBox { IsChecked = true, Content = d.Name };
				rows.Add ((d, box));
				children.Add (box);
			}
			groupChecks[gname] = gcheck;
			groups[gname] = children;
			var item = new TreeViewItem { Header = gcheck, IsExpanded = true };
			foreach (var c in children)
				item.Items.Add (new TreeViewItem { Header = c, IsExpanded = true });
			FilesTree.Items.Add (item);
			// Legacy toggled handler: parent check cascades to children; children
			// recompute the parent (checked if any child checked). Avalonia exposes
			// one IsCheckedChanged route instead of WPF-style Checked/Unchecked.
			gcheck.IsCheckedChanged += (_, _) => SetGroup (gname, gcheck.IsChecked == true);
			foreach (var c in children)
				c.IsCheckedChanged += (_, _) => UpdateGroupState (gname);
		}

		// Ungrouped docs sit at the root level (legacy groupByProject=false path).
		foreach (var d in docs.Where (d => d.ProjectGroup is null)) {
				var box = new CheckBox { IsChecked = true, Content = d.Name };
				rows.Add ((d, box));
				FilesTree.Items.Add (new TreeViewItem { Header = box, IsExpanded = true });
			}
	}

	bool cascading;

	void SetGroup (string gname, bool value)
	{
		if (cascading)
			return; // guard: programmatic sets must not re-trigger the cascade
		cascading = true;
		foreach (var c in groups[gname])
			c.IsChecked = value;
		cascading = false;
	}

	void UpdateGroupState (string gname)
	{
		if (cascading)
			return;
		cascading = true;
		var children = groups[gname];
		// Legacy NewCheckStatus: inconsistent when children disagree; the legacy
		// CheckBox exposes the tri-state, we approximate with null = mixed.
		groupChecks[gname].IsChecked = children.Any (c => c.IsChecked == true)
			? (children.All (c => c.IsChecked == true) ? true : (bool?)null)
			: false;
		cascading = false;
	}

	void OnSaveAndQuit (object? sender, RoutedEventArgs e)
	{
		Result = DirtyResult.SaveAndQuit;
		// Legacy SaveAndQuit: awaits Task.WhenAll of the checked Document.Save calls.
		var pending = rows.Where (r => r.Box.IsChecked == true && r.Doc.SaveAsync is not null)
			.Select (r => r.Doc.SaveAsync! ());
		_ = Task.WhenAll (pending).ContinueWith (_ => Close (), TaskScheduler.FromCurrentSynchronizationContext ());
	}

	void OnQuit (object? sender, RoutedEventArgs e)
	{
		Result = DirtyResult.Quit;
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e)
	{
		Result = DirtyResult.Cancel;
		Close ();
	}

	/// <summary>Checked documents per project group (null group = ungrouped), the
	/// set the caller must save (legacy: TreeStore foreach with checked column).</summary>
	public IReadOnlyList<DirtyDoc> CheckedDocs => rows.Where (r => r.Box.IsChecked == true).Select (r => r.Doc).ToList ();
}
