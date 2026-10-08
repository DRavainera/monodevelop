using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MonoDevelop.AvaloniaAddons;

namespace MonoDevelop.AvaloniaShell.Views;

public class AddinItem
{
	public string Name { get; set; } = "";
	public string Subtitle { get; set; } = "";
	public string Id { get; set; } = "";
	public string Version { get; set; } = "";
	public string Description { get; set; } = "";
	public string Author { get; set; } = "";
	public string Category { get; set; } = "";
	public bool Enabled { get; set; } = true;
	// Row state in the new add-in host (the legacy dialog kept the Mono.Addins
	// Addin / AddinRepositoryEntry here; Mono.Addins is gone from this dialog).
	public AddonLoadState? State { get; set; }
	// The add-in's own icon (identity.icon resolved by the host); null keeps the
	// template's generic plugin-32 / plugin-avail-32 like the legacy rows.
	public IImage? Icon { get; set; }
	// Legacy row state driving the row icon and the details buttons.
	public bool HasUpdate { get; set; }
	public string Url { get; set; } = "";

	// Row rendering: "Name" + secondary description line (the legacy markup cell puts
	// the first description line under the name; the version lives in the details);
	// disabled addins render grey like UpdateRow's <span foreground="grey">.
	public string DisplayName => Subtitle == "__category__" ? Category : Name;
	public string SecondaryText => Subtitle == "__category__" ? "" : Description;
	public Avalonia.Media.IBrush NameBrush =>
		Subtitle == "__category__" ? Avalonia.Media.Brushes.SteelBlue :
		Enabled ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.Gray;
	public bool IsCategory => Subtitle == "__category__";
	public bool IsInstalled { get => !IsCategory && isInstalled; set => isInstalled = value; }
	bool isInstalled;
	public bool IsAvailable => !IsCategory && !isInstalled;
	public bool HasIcon => Icon is not null;
	public bool ShowInstalledIcon => IsInstalled && !HasIcon;
	public bool ShowAvailableIcon => IsAvailable && !HasIcon;
}

public partial class AddinManagerDialog : Window
{
	// Identity icons are resolved once per file and cached for the dialog's lifetime
	// (the list reloads on every filter change; the PNGs don't change underneath us).
	static readonly Dictionary<string, Bitmap> iconCache = new ();

	readonly AddonHost? host;
	string currentTab = "installed";
	bool updatingDetails;

	public AddinManagerDialog ()
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		host = MonoDevelop.AvaloniaShell.App.Addins;

		// Tab icons (same resources the legacy tabs load: plugin-22 / plugin-update-22 / update-16).
		TabIconInstalled!.Source = LoadIcon ("plugin-22.png");
		TabIconUpdates!.Source = LoadIcon ("plugin-update-22.png");
		TabIconGallery!.Source = LoadIcon ("update-16.png");

		FilterBox!.TextChanged += (_, _) => ReloadCurrentTab ();
		RefreshButton!.Click += OnRefreshClicked;
		UpdateAllButton!.Click += OnUpdateAllClicked;
		InstallFromFileButton!.Click += OnInstallFromFileClicked;
		LoadAddins ();
	}

	static Bitmap? LoadIcon (string name)
	{
		try {
			using var s = typeof (AddinManagerDialog).Assembly.GetManifestResourceStream (
				"MonoDevelop.AvaloniaShell.Addins." + name) ?? typeof (AddinManagerDialog).Assembly.GetManifestResourceStream (name);
			if (s is not null)
				return new Bitmap (s);
			return new Bitmap (Avalonia.Platform.AssetLoader.Open (new Uri ("avares://MonoDevelop.AvaloniaShell/Addins/" + name)));
		} catch (Exception ex) {
			Console.WriteLine ("[addins] icon " + name + ": " + ex.Message);
			return null;
		}
	}

	static Bitmap BitmapFrom (string file)
	{
		if (iconCache.TryGetValue (file, out var cached))
			return cached;
		using var fs = new FileStream (file, FileMode.Open, FileAccess.Read);
		var bmp = new Bitmap (fs);
		iconCache [file] = bmp;
		return bmp;
	}

	// Row icon for the Installed page: the add-in's own icon resolved by the host
	// (identity.icon). When nothing resolves, the row keeps the template's generic
	// plugin-32 (the legacy dialog's StoreIcon fallback).
	IImage? LoadAddonIcon (AddonLoadState state)
	{
		try {
			var file = host?.ResolveIconFile (state);
			return file is null ? null : BitmapFrom (file);
		} catch (Exception ex) {
			Console.WriteLine ("[addins] icon for " + state.Manifest.Identity.Id + ": " + ex.Message);
			return null;
		}
	}

	void ReloadCurrentTab () => LoadAddins ();

	void LoadAddins ()
	{
		var items = currentTab switch {
			"updates" => LoadUpdates (),
			"gallery" => LoadGallery (),
			_ => LoadInstalled ()
		};
		items = ApplyFilter (items);

		// Page header (legacy: labelUpdates "N updates available", repo combo in gallery).
		RefreshButton.IsVisible = currentTab == "gallery";
		RepoCombo.IsVisible = currentTab == "gallery";
		UpdateAllButton.IsVisible = currentTab == "updates" && items.Count > 0;
		PageHeaderLabel.Text = currentTab switch {
			"updates" => items.Count == 0 ? "No updates found"
				: items.Count == 1 ? "1 update available"
				: $"{items.Count} updates available",
			_ => "",
		};
		UpdatesTabLabel.Text = "Updates" + (currentTab != "updates" && FilterBox!.Text?.Length > 0 && items.Count > 0 ? $" ({items.Count})" : "");

		// Category grouping (legacy AddinTreeWidget.ShowCategories): one header row
		// per category, "Other" last — implemented as grouped items with a flag.
		List<AddinItem> grouped = new ();
		foreach (var g in items.GroupBy (i => string.IsNullOrEmpty (i.Category) ? "Other" : i.Category)
			.OrderBy (g => g.Key == "Other" ? "\uFFFF" : g.Key)) {
			grouped.Add (new AddinItem { Name = g.Key, Subtitle = "__category__" });
			foreach (var it in g.OrderBy (i => i.Name))
				grouped.Add (it);
		}
		AddinList!.ItemsSource = grouped;
		Console.WriteLine ($"[addins] tab={currentTab} items={items.Count} host={host is not null}");
		if (items.Count == 0)
			ShowEmptyDetails ();
	}

	List<AddinItem> ApplyFilter (List<AddinItem> items)
	{
		var filter = FilterBox?.Text;
		if (string.IsNullOrWhiteSpace (filter))
			return items;
		return items.Where (i =>
			i.Name.Contains (filter, StringComparison.CurrentCultureIgnoreCase) ||
			i.Id.Contains (filter, StringComparison.CurrentCultureIgnoreCase) ||
			i.Description.Contains (filter, StringComparison.CurrentCultureIgnoreCase)).ToList ();
	}

	static string? FirstLine (string? text)
	{
		if (string.IsNullOrEmpty (text))
			return null;
		var idx = text.IndexOfAny (new[] { '\r', '\n' });
		return idx > 0 ? text.Substring (0, idx) : text;
	}

	// Maps an add-in of the new host to a dialog row (the legacy rows mapped the
	// Mono.Addins Addin / AddinRepositoryEntry).
	AddinItem RowFor (AddonLoadState state, bool installed)
	{
		var id = state.Manifest.Identity;
		return new AddinItem {
			Name = string.IsNullOrEmpty (id.DisplayName) ? id.Name : id.DisplayName,
			Subtitle = id.Version,
			Id = id.Id,
			Version = id.Version,
			Description = FirstLine (id.Description) ?? "",
			Author = id.Publisher,
			Category = state.Manifest.Categories.Count > 0 ? state.Manifest.Categories [0] : "Other",
			Enabled = installed ? state.Loaded : true,
			IsInstalled = installed,
			State = state
		};
	}

	List<AddinItem> LoadInstalled ()
	{
		var list = new List<AddinItem> ();
		if (host is null)
			return list;
		try {
			// New add-in host: the add-ins this shell discovered and loaded (the
			// legacy dialog listed the Mono.Addins registry instead).
			foreach (var state in host.Addons) {
				var item = RowFor (state, installed: true);
				item.Icon = LoadAddonIcon (state);
				list.Add (item);
			}
		} catch (Exception ex) {
			Console.WriteLine ("[addins] load installed failed: " + ex.Message);
		}
		return list;
	}

	List<AddinItem> LoadGallery ()
	{
		var list = new List<AddinItem> ();
		if (host is null)
			return list;
		try {
			// New add-in host: the catalog of add-ins this build ships. Loaded
			// add-ins render installed; the rest render available — the legacy
			// gallery's plugin-32 / plugin-avail-32 split.
			foreach (var state in host.Addons)
				list.Add (RowFor (state, installed: state.Loaded));
		} catch (Exception ex) {
			Console.WriteLine ("[addins] load gallery failed: " + ex.Message);
		}
		return list;
	}

	List<AddinItem> LoadUpdates ()
	{
		var list = new List<AddinItem> ();
		if (host is null)
			return list;
		try {
			// New add-in host: an update is a newer version of a running add-in in
			// the discovered catalog (the host stages one manifest per add-in, so
			// this only fires when a newer version is staged next to it).
			foreach (var state in host.Addons) {
				if (!state.Loaded)
					continue;
				var id = state.Manifest.Identity;
				string? best = null;
				foreach (var other in host.Addons) {
					if (ReferenceEquals (other, state) || other.Manifest.Identity.Id != id.Id)
						continue;
					var v = other.Manifest.Identity.Version;
					if (CompareVersions (id.Version, v) < 0 && (best is null || CompareVersions (best, v) < 0))
						best = v;
				}
				if (best is null)
					continue;
				var row = RowFor (state, installed: true);
				row.Subtitle = best;
				row.Version = best;
				row.HasUpdate = true;
				list.Add (row);
			}
		} catch (Exception ex) {
			Console.WriteLine ("[addins] load updates failed: " + ex.Message);
		}
		return list;
	}

	// Numeric version comparison (the versions in play are dotted numerics; this
	// matches Mono.Addins' comparison for well-formed versions).
	static int CompareVersions (string v1, string v2)
	{
		Version.TryParse (v1, out var a);
		Version.TryParse (v2, out var b);
		if (a is null && b is null)
			return string.CompareOrdinal (v1, v2);
		if (a is null)
			return -1;
		if (b is null)
			return 1;
		return a.CompareTo (b);
	}

	void OnTabSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (TabList?.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
			return;
		currentTab = tag;
		LoadAddins ();
	}

	void OnAddinSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (AddinList?.SelectedItem is not AddinItem item) {
			ShowEmptyDetails ();
			return;
		}
		updatingDetails = true;

		// Details buttons exactly like the legacy ShowAddin switch:
		//  installed+update → Update (+Disable/Uninstall); installed → Disable/Uninstall
		//  (CanDisable/CanUninstall); not installed → Install.
		UpdateButton.IsVisible = item.HasUpdate;
		InstallButton.IsVisible = !item.IsInstalled;
		DisableButton.IsVisible = item.IsInstalled && item.Enabled;
		EnableButton.IsVisible = item.IsInstalled && !item.Enabled;
		UninstallButton.IsVisible = item.IsInstalled;

		// Status header (legacy boxHeader: update-available / disabled / broken notices).
		DetailsHeader.IsVisible = item.HasUpdate || (item.IsInstalled && !item.Enabled);
		if (item.HasUpdate) {
			DetailsHeaderIcon.Source = LoadIcon ("update-16.png");
			DetailsHeaderText.Text = item.IsInstalled
				? $"An update is available ({item.Version})."
				: "Update available.";
		} else if (item.IsInstalled && !item.Enabled) {
			DetailsHeaderIcon.Source = LoadIcon ("plugin-32.png");
			DetailsHeaderText.Text = "This add-in is disabled.";
		}

		DetailsName!.Text = item.Name;
		DetailsVersion!.Text = item.Version.Length > 0 ? "Version " + item.Version : "";
		DetailsAuthor!.Text = item.Author.Length > 0 ? "By " + item.Author : "";
		DetailsDesc!.Text = item.Description;
		UrlButton.IsVisible = item.Url.Length > 0;
		updatingDetails = false;
	}

	void ShowEmptyDetails ()
	{
		updatingDetails = true;
		DetailsHeader.IsVisible = false;
		DetailsName!.Text = "";
		DetailsVersion!.Text = "";
		DetailsAuthor!.Text = "";
		DetailsDesc!.Text = currentTab == "updates" ? "No updates available." : "Select an add-in.";
		UpdateButton!.IsVisible = false;
		InstallButton!.IsVisible = false;
		DisableButton!.IsVisible = false;
		EnableButton!.IsVisible = false;
		UninstallButton!.IsVisible = false;
		UrlButton!.IsVisible = false;
		updatingDetails = false;
	}

	void OnEnableDisableClicked (object? sender, RoutedEventArgs e)
	{
		// The new add-in host does not expose enable/disable yet (the legacy toggle
		// edited the Mono.Addins registry).
		if (AddinList?.SelectedItem is AddinItem item)
			Console.WriteLine ("[addins] enable/disable not available in the new add-in host: " + item.Id);
	}

	void OnUninstallClicked (object? sender, RoutedEventArgs e)
	{
		if (AddinList?.SelectedItem is AddinItem item)
			Console.WriteLine ("[addins] uninstall not available in the new add-in host: " + item.Id);
	}

	void OnInstallClicked (object? sender, RoutedEventArgs e)
	{
		InstallSelected ();
	}

	void OnUpdateClicked (object? sender, RoutedEventArgs e)
	{
		InstallSelected ();
	}

	void InstallSelected ()
	{
		if (AddinList?.SelectedItem is AddinItem item)
			Console.WriteLine ("[addins] install not available in the new add-in host: " + item.Id);
	}

	void OnUpdateAllClicked (object? sender, RoutedEventArgs e)
	{
		var pending = (AddinList?.ItemsSource as IEnumerable<AddinItem>)?
			.Count (i => i.HasUpdate) ?? 0;
		if (pending == 0)
			return;
		Console.WriteLine ("[addins] update-all not available in the new add-in host (" + pending + " pending)");
	}

	void OnRefreshClicked (object? sender, RoutedEventArgs e)
	{
		// The new host's catalog is the build's own manifests, so a refresh re-reads
		// the current host state into the list (the legacy refresh downloaded the
		// remote repositories).
		LoadAddins ();
	}

	void OnRepoMenuClicked (object? sender, RoutedEventArgs e)
	{
		// Legacy repoCombo: All repositories / per-repo / Manage Repositories...
		// The new host ships a single local catalog: keep the label static for now.
		LoadAddins ();
	}

	void OnUrlClicked (object? sender, RoutedEventArgs e)
	{
		if (AddinList?.SelectedItem is not AddinItem item || item.Url.Length == 0)
			return;
		try {
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo (item.Url) { UseShellExecute = true });
		} catch (Exception ex) {
			Console.WriteLine ("[addins] url: " + ex.Message);
		}
	}

	void OnInstallFromFileClicked (object? sender, RoutedEventArgs e)
	{
		Console.WriteLine ("[addins] install from file not available in the new add-in host");
	}

	void OnCloseClicked (object? sender, RoutedEventArgs e) => Close ();
}
