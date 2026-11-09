using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaShell.Controls;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Manage NuGet Packages dialog, rebuilt after the legacy ManagePackagesDialog
/// (MonoDevelop.PackageManagement.Gui) on the shell's own stack: a search box,
/// the repository on the left (the shell's offline package repository —
/// AddReferenceDialog.KnownPackages, the legacy NuGet repository list), the
/// packages installed in the project on the right, and the OK / Cancel pair.
/// Double-click (or Add / Remove) moves a package between the two lists; OK
/// rewrites the csproj &lt;PackageReference&gt; items through
/// ConfigurationService.SaveProjectReferences, exactly like AddReferenceDialog
/// does for every reference kind. Project scope, like the legacy
/// ManagePackagesInProjectHandler (the solution-level variant needs the
/// online resolver the shell does not ship).
/// </summary>
public class ManagePackagesDialog : DialogWindow
{
	class PkgRow
	{
		public string Name = "";
		public string Version = "";
		// The repo list binds PkgRow objects directly: the default item
		// template renders them through ToString.
		public override string ToString () => Version.Length > 0 ? $"{Name} ({Version})" : Name;
	}

	readonly string projectPath;
	readonly string solutionPath;
	readonly TextBox searchBox = new ();
	readonly ListBox repoList = new ();
	readonly ListBox installedList = new ();
	readonly Button addButton;
	readonly Button removeButton;
	readonly TextBlock statusLabel = new ();
	readonly List<PkgRow> installed = new ();

	/// <summary>Packages added during the dialog (QA / menu assertions).</summary>
	public List<string> AddedPackages { get; } = new ();

	/// <summary>Packages removed during the dialog (QA / menu assertions).</summary>
	public List<string> RemovedPackages { get; } = new ();

	/// <summary>Package versions keyed by id for the current install state.</summary>
	public IReadOnlyDictionary<string, string> InstalledVersions {
		get {
			var d = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			foreach (var p in installed)
				d[p.Name] = p.Version;
			return d;
		}
	}

	public ManagePackagesDialog (string projectPath) : this (projectPath, null) { }

	public ManagePackagesDialog (string projectPath, string? solutionPath)
	{
		this.projectPath = projectPath;
		this.solutionPath = solutionPath ?? "";

		Title = "Manage NuGet Packages";
		Width = 720;
		Height = 560;
		ShowTitleRow ();

		// ----- Search (legacy searchTextBox: filters the repository list) -----
		searchBox.Watermark = "Search";
		searchBox.Margin = new Thickness (12, 8, 12, 4);
		searchBox.TextChanged += (_, _) => RefreshRepo ();

		// ----- Two-pane body (legacy: packagesSourceTree | projectPackagesTree) -----
		var repoHeader = new TextBlock { Text = "Available packages", FontWeight = FontWeight.SemiBold, FontSize = 12, Margin = new Thickness (12, 8, 12, 0) };
		repoHeader.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		repoList.DoubleTapped += (_, _) => AddSelected ();
		var repoBorder = new Border {
			Child = repoList,
			Margin = new Thickness (12, 4, 12, 0),
			BorderBrush = (Brush)Application.Current!.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (1),
			CornerRadius = new CornerRadius (3),
		};

		addButton = new Button { Content = "Add", MinWidth = 80, IsEnabled = false, Classes = { "chromebtn" } };
		addButton.Click += (_, _) => AddSelected ();

		// Middle move-button column (the Add sits between the two trees).
		var moveButtons = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		moveButtons.Children.Add (addButton);

		var installedHeaderRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto"), Margin = new Thickness (0, 8, 12, 0) };
		var installedHeader = new TextBlock { Text = "Installed packages", FontWeight = FontWeight.SemiBold, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		installedHeader.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		installedHeaderRow.Children.Add (installedHeader);
		removeButton = new Button { Content = "Remove", MinWidth = 80, IsEnabled = false, Classes = { "chromebtn" } };
		removeButton.Click += (_, _) => RemoveSelected ();
		Grid.SetColumn (removeButton, 1);
		installedHeaderRow.Children.Add (removeButton);

		installedList.DoubleTapped += (_, _) => RemoveSelected ();
		installedList.SelectionChanged += (_, _) => removeButton.IsEnabled = installedList.SelectedIndex >= 0;
		var installedBorder = new Border {
			Child = installedList,
			Margin = new Thickness (12, 4, 12, 0),
			BorderBrush = (Brush)Application.Current.FindResource ("IdeBorderBrush")!,
			BorderThickness = new Thickness (1),
			CornerRadius = new CornerRadius (3),
		};
		// Right margin aligned with the list border below (12) so the header
		// Remove button sits on the same right edge as the installed list.
		installedHeaderRow.Margin = new Thickness (12, 8, 12, 0);
		repoList.SelectionChanged += (_, _) => addButton.IsEnabled = repoList.SelectedIndex >= 0;

		var panes = new Grid { ColumnDefinitions = ColumnDefinitions.Parse ("*,Auto,*"), RowDefinitions = RowDefinitions.Parse ("Auto,*") };
		Grid.SetRow (repoHeader, 0); Grid.SetColumn (repoHeader, 0);
		Grid.SetRow (repoBorder, 1); Grid.SetColumn (repoBorder, 0);
		Grid.SetRow (moveButtons, 1); Grid.SetColumn (moveButtons, 1);
		Grid.SetRow (installedHeaderRow, 0); Grid.SetColumn (installedHeaderRow, 2);
		Grid.SetRow (installedBorder, 1); Grid.SetColumn (installedBorder, 2);
		panes.Children.Add (repoHeader);
		panes.Children.Add (repoBorder);
		panes.Children.Add (moveButtons);
		panes.Children.Add (installedHeaderRow);
		panes.Children.Add (installedBorder);
		moveButtons.Margin = new Thickness (8, 0, 8, 0);

		// ----- Status + bottom buttons (legacy statusLabel + Cancel) -----
		statusLabel.FontSize = 12;
		statusLabel.Margin = new Thickness (12, 8, 12, 0);
		statusLabel.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		var ok = new Button { Content = "OK", MinWidth = 80, Classes = { "chromebtn" } };
		ok.Click += (_, _) => Apply ();
		var cancel = new Button { Content = "Cancel", MinWidth = 80, Classes = { "chromebtn" } };
		cancel.Click += (_, _) => Close ();
		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8,
			Margin = new Thickness (12, 8, 12, 12),
		};
		buttons.Children.Add (ok);
		buttons.Children.Add (cancel);

		var root = new Grid { RowDefinitions = RowDefinitions.Parse ("Auto,*,Auto,Auto") };
		Grid.SetRow (searchBox, 0); root.Children.Add (searchBox);
		Grid.SetRow (panes, 1); root.Children.Add (panes);
		Grid.SetRow (statusLabel, 2); root.Children.Add (statusLabel);
		Grid.SetRow (buttons, 3); root.Children.Add (buttons);

		Content = root;

		LoadInstalled ();
		RefreshRepo ();
	}

	// ---------- data ----------

	void LoadInstalled ()
	{
		installed.Clear ();
		var model = ConfigurationService.LoadProjectModel (projectPath);
		if (model is not null) {
			foreach (var ri in model.References) {
				if (ri.Kind != "PackageReference")
					continue;
				var name = ri.Include.Split (',') [0];
				installed.Add (new PkgRow { Name = name, Version = ri.Version ?? "" });
			}
		}
		RefreshInstalled ();
		UpdateStatus ();
	}

	void RefreshRepo ()
	{
		var filter = searchBox.Text?.Trim() ?? "";
		repoList.Items.Clear ();
		foreach (var name in AddReferenceDialog.KnownPackages) {
			if (installed.Any (p => p.Name == name))
				continue; // legacy: already-installed packages leave the source tree
			if (filter.Length > 0 && !name.Contains (filter, StringComparison.OrdinalIgnoreCase))
				continue;
			repoList.Items.Add (new PkgRow { Name = name });
		}
	}

	void RefreshInstalled ()
	{
		installedList.Items.Clear ();
		foreach (var p in installed.OrderBy (p => p.Name, StringComparer.OrdinalIgnoreCase))
			installedList.Items.Add (p.Version.Length > 0 ? $"{p.Name} ({p.Version})" : p.Name);
	}

	void UpdateStatus ()
	{
		// Legacy: "Packages: N" in the status row.
		statusLabel.Text = "Packages: " + installed.Count;
	}

	// ---------- actions ----------

	void AddSelected ()
	{
		if (repoList.SelectedItem is not PkgRow row)
			return;
		if (installed.Any (p => p.Name == row.Name))
			return;
		installed.Add (row);
		AddedPackages.Add (row.Name);
		RefreshRepo ();
		RefreshInstalled ();
		UpdateStatus ();
	}

	void RemoveSelected ()
	{
		int idx = installedList.SelectedIndex;
		if (idx < 0)
			return;
		var ordered = installed.OrderBy (p => p.Name, StringComparer.OrdinalIgnoreCase).ToList ();
		if (idx < ordered.Count) {
			installed.Remove (ordered [idx]);
			RemovedPackages.Add (ordered [idx].Name);
			RefreshRepo ();
			RefreshInstalled ();
			UpdateStatus ();
		}
	}

	/// <summary>Programmatic add (QA + menu path): adds the PackageReference and saves.</summary>
	public bool TryAddPackage (string name)
	{
		if (installed.Any (p => p.Name == name)) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ($"[manage-pkgs] '{name}' is already installed");
			return false;
		}
		installed.Add (new PkgRow { Name = name });
		AddedPackages.Add (name);
		return Apply ();
	}

	/// <summary>Programmatic remove (QA + menu path): removes the PackageReference and saves.</summary>
	public bool TryRemovePackage (string name)
	{
		var row = installed.FirstOrDefault (p => p.Name == name);
		if (row is null) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ($"[manage-pkgs] '{name}' is not installed");
			return false;
		}
		installed.Remove (row);
		RemovedPackages.Add (name);
		return Apply ();
	}

	/// <summary>
	/// OK: rewrite the csproj with the edited PackageReference set, keeping every
	/// other reference kind intact (same ConfigurationService writer as
	/// AddReferenceDialog.Apply).
	/// </summary>
	bool Apply ()
	{
		var model = ConfigurationService.LoadProjectModel (projectPath);
		var items = (model?.References ?? new List<ConfigurationService.ReferenceItem> ())
			.Where (r => r.Kind != "PackageReference")
			.ToList ();
		foreach (var p in installed.OrderBy (p => p.Name, StringComparer.OrdinalIgnoreCase))
			items.Add (new ConfigurationService.ReferenceItem ("PackageReference", p.Name, p.Version.Length > 0 ? p.Version : null, null));
		bool ok;
		try {
			ok = ConfigurationService.SaveProjectReferences (projectPath, items);
		} catch (Exception ex) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ("[manage-pkgs] error saving: " + ex.Message);
			return false;
		}
		if (!ok) {
			if (MainWindow.Instance is not null)
				MainWindow.Instance.Output ("[manage-pkgs] error saving");
			return false;
		}
		Close ();
		return true;
	}
}
