using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.AvaloniaAddons.Marketplace;

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

	// Gallery row sourced from the Visual Studio Marketplace (Phase 2). null for the
	// local add-ins listed in the Installed / Updates tabs. When set, the Install
	// button downloads the .vsix and imports it through the shell's add-in model.
	// (Fully qualified: inside AddinItem the simple name "Marketplace" binds to this
	// property, not to the MonoDevelop.AvaloniaAddons.Marketplace namespace.)
	public MonoDevelop.AvaloniaAddons.Marketplace.MarketplaceExtension? Marketplace { get; set; }

	// Row rendering: "Name" + secondary description line (the legacy markup cell puts
	// the first description line under the name; the version lives in the details).
	// NameBrush is set by the dialog: theme foreground for enabled addins,
	// <span foreground="grey"> equivalent for disabled ones (legacy UpdateRow).
	public string DisplayName => Subtitle == "__category__" ? Category : Name;
	public string SecondaryText => Subtitle == "__category__" ? "" : Description;
	public IBrush? NameBrush { get; set; }
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
	readonly Dictionary<string, HashSet<string>> expandedByTab = new ();
	IBrush? themeFg;
	IDataTemplate? rowTemplate;
	bool expandInstalledAll, expandGalleryAll;
	string currentTab = "installed";
	string renderedTab = "installed";
	bool updatingDetails;

	// ----- Gallery = Visual Studio Marketplace (Phase 2) -----
	// The Gallery tab is a live view of the marketplace: the local add-ins are NOT
	// listed here anymore (a local not-installed add-in stays in the Installed tab,
	// greyed out). The client/importer are created lazily on the UI thread (the
	// gallery is the only consumer). The search is server-side and debounced: a
	// generation counter plus a cancellation token let a new keystroke supersede any
	// in-flight query, so a stale response can never render over a newer one. The
	// network I/O runs off the UI thread; only the final render is posted back to it.
	MarketplaceClient? mktClient;
	VsixImporter? mktImporter;
	int gallerySearchGeneration;
	CancellationTokenSource? gallerySearchCts;

	public AddinManagerDialog ()
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		host = MonoDevelop.AvaloniaShell.App.Addins;
		// Row name color follows the theme (legacy markup cell uses the default
		// foreground; a hardcoded white was invisible on the light theme).
		themeFg = (IBrush?)Avalonia.Application.Current?.FindResource ("IdeFgBrush");
		rowTemplate = (IDataTemplate?)this.FindResource ("AddinRowTemplate");

		// Tab icons (same resources the legacy tabs load: plugin-22 / plugin-update-22 / update-16).
		TabIconInstalled!.Source = LoadIcon ("plugin-22.png");
		TabIconUpdates!.Source = LoadIcon ("plugin-update-22.png");
		TabIconGallery!.Source = LoadIcon ("update-16.png");

		// Legacy repoCombo is a listbox of repositories; the new host ships a single
		// local catalog, so it has one entry ("All repositories").
		RepoCombo!.Items.Add (new ComboBoxItem { Content = "All repositories" });

		// Marketplace client/importer for the Gallery tab, created up front on the
		// UI thread (the search results land on the thread pool, so no lazy init
		// from there). Released in OnClosing.
		mktClient = new MarketplaceClient ();
		mktImporter = host is not null ? new VsixImporter (host) : null;

		FilterBox!.TextChanged += (_, _) => {
			// Legacy: a filter change reloads the pages and expands the category
			// trees (installed + gallery; the updates page has no categories).
			// Gallery is a live marketplace view: its query is server-side and
			// debounced (one keystroke = one search, not one per letter).
			if (currentTab == "gallery") {
				ScheduleGallerySearch ();
				return;
			}
			expandInstalledAll = true;
			expandGalleryAll = true;
			ReloadCurrentTab ();
		};
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
		SaveExpanded ();
		// The Gallery tab is a live view of the Visual Studio Marketplace (Phase 2):
		// its contents come from an async network query, not from a synchronous
		// snapshot of host state, so it is loaded on its own debounced path.
		if (currentTab == "gallery") {
			ScheduleGallerySearch ();
			return;
		}
		var items = currentTab switch {
			"updates" => LoadUpdates (),
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

		BuildTree (items);
		Console.WriteLine ($"[addins] tab={currentTab} items={items.Count} host={host is not null}");
		if (items.Count == 0)
			ShowEmptyDetails ();
	}

	// Legacy AddinTreeWidget.SaveStatus: the category expand state is preserved
	// across reloads (captured before the tree is rebuilt; the tree still shows the
	// previously rendered tab when switching tabs).
	void SaveExpanded ()
	{
		var set = ExpandedFor (renderedTab);
		set.Clear ();
		foreach (var o in AddinList?.Items ?? (IEnumerable)Array.Empty<object?> ())
			if (o is TreeViewItem { IsExpanded: true, Tag: AddinItem { IsCategory: true } cat })
				set.Add (cat.Name);
	}

	HashSet<string> ExpandedFor (string tab) =>
		expandedByTab.TryGetValue (tab, out var s) ? s : (expandedByTab [tab] = new HashSet<string> (StringComparer.Ordinal));

	// Builds the addin tree: collapsible category nodes (legacy ShowCategories) —
	// the updates page is flat like the legacy updatesTree (ShowCategories=false).
	void BuildTree (List<AddinItem> items)
	{
		var list = AddinList!;
		AddinItem? prev = list.SelectedItem is TreeViewItem { Tag: AddinItem ai } && !ai.IsCategory ? ai : null;
		list.Items.Clear ();

		bool useCategories = currentTab != "updates";
		var saved = ExpandedFor (currentTab);
		if (useCategories && ((currentTab == "installed" && expandInstalledAll) || (currentTab == "gallery" && expandGalleryAll))) {
			// Legacy: a filter change expands all category nodes.
			expandInstalledAll = expandGalleryAll = false;
			foreach (var it in items)
				saved.Add (string.IsNullOrEmpty (it.Category) ? "Other" : it.Category);
		}

		if (useCategories) {
			foreach (var g in items.GroupBy (i => string.IsNullOrEmpty (i.Category) ? "Other" : i.Category)
				.OrderBy (g => g.Key == "Other" ? "\uFFFF" : g.Key)) {
				var cat = new TreeViewItem {
					Tag = new AddinItem { Name = g.Key, Subtitle = "__category__" },
					Header = g.Key
				};
				cat.IsExpanded = saved.Contains (g.Key);
				foreach (var it in g.OrderBy (i => i.Name))
					cat.Items.Add (MakeRowItem (it));
				list.Items.Add (cat);
			}
		} else {
			foreach (var it in items.OrderBy (i => i.Name))
				list.Items.Add (MakeRowItem (it));
		}

		// Legacy RestoreStatus: keep the selection (and its category expanded)
		// across reloads.
		if (prev is not null) {
			var target = FindRowItem (list.Items, prev.Id);
			if (target is not null) {
				foreach (var o in list.Items)
					if (o is TreeViewItem cat && cat.Items.Contains (target))
						cat.IsExpanded = true;
				list.SelectedItem = target;
			}
		}
		renderedTab = currentTab;
	}

	AddinItem? SelectedRow () => (AddinList?.SelectedItem as TreeViewItem)?.Tag as AddinItem;

	TreeViewItem MakeRowItem (AddinItem it)
	{
		// Legacy markup cell: default (theme) foreground for enabled addins,
		// <span foreground="grey"> for the disabled ones.
		it.NameBrush = it.Enabled ? themeFg : Brushes.Gray;
		return new TreeViewItem { Tag = it, Header = it, HeaderTemplate = rowTemplate };
	}

	static TreeViewItem? FindRowItem (IEnumerable items, string id)
	{
		foreach (var o in items) {
			if (o is not TreeViewItem tv)
				continue;
			if (tv.Tag is AddinItem { IsCategory: false, Id: string rowId } && rowId == id)
				return tv;
			var nested = FindRowItem (tv.Items, id);
			if (nested is not null)
				return nested;
		}
		return null;
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
		// User-installed manifests may carry explicit nulls the model defaults do not
		// cover; coerce so the row bindings never NRE.
		var version = id.Version ?? "";
		var name = string.IsNullOrEmpty (id.DisplayName) ? id.Name : id.DisplayName;
		return new AddinItem {
			Name = name ?? id.Id ?? "",
			Subtitle = version,
			Id = id.Id ?? "",
			Version = version,
			Description = FirstLine (id.Description) ?? "",
			Author = id.Publisher ?? "",
			Category = state.Manifest.Categories.Count > 0 && state.Manifest.Categories [0] is not null
				? state.Manifest.Categories [0]
				: "Other",
			// Superseded rows (an older staged version of an active add-in) render
			// grey like disabled ones: they are not the running version.
			Enabled = installed ? state.Loaded && state.Enabled && !state.Superseded : true,
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

	// The Gallery is a live view of the Visual Studio Marketplace (Phase 2): a
	// server-side search (filterType 1 text + filterType 8 target
	// "Microsoft.VisualStudio"), so the local add-ins never appear here — a local
	// not-installed add-in stays in the Installed tab, greyed out, and is never
	// lost, it just does not pollute the "available" list.
	void ScheduleGallerySearch ()
	{
		// Debounce: the filter box fires per keystroke; one search per pause of
		// typing. A new keystroke cancels the pending one and supersedes any
		// in-flight query, so a stale response can never render over a newer one.
		gallerySearchCts?.Cancel ();
		var cts = new CancellationTokenSource ();
		gallerySearchCts = cts;
		int generation = Interlocked.Increment (ref gallerySearchGeneration);
		// Capture the term on the UI thread (this runs from TextChanged / tab
		// selection); the delay and the query then run on the thread pool.
		var term = (FilterBox?.Text ?? "").Trim ();
		PageHeaderLabel.Text = "Searching the Visual Studio Marketplace…";
		_ = RunGallerySearchAsync (generation, term, cts.Token);
	}

	async Task RunGallerySearchAsync (int generation, string term, CancellationToken ct)
	{
		// The pause is the debounce: a superseding keystroke cancels it before the
		// search is even issued.
		try {
			await Task.Delay (450, ct);
		} catch (OperationCanceledException) {
			return;
		}
		List<MarketplaceExtension> results = new ();
		try {
			if (mktClient is null)
				return; // window closing while the debounce was pending
			// Server-side search: the marketplace matches name/publisher/tags; the
			// target filter restricts the listing to Visual Studio extensions. An
			// empty term returns the top listing of the target.
			results = (await mktClient.SearchAsync (term, "Microsoft.VisualStudio", 40, ct)).ToList ();
		} catch (OperationCanceledException) {
			return; // superseded or the window closed
		} catch (Exception ex) {
			// Broad on purpose: a network failure (HttpRequestException — refused
			// connection, DNS, proxy), a decode failure (JsonException) or a stale
			// client must degrade to an empty gallery, never fault the task and leave
			// the header stuck on "Searching…". MarketplaceException is one of these.
			if (generation != gallerySearchGeneration)
				return; // a newer search owns the UI now
			Console.WriteLine ("[addins] gallery search failed: " + ex.Message);
		}
		if (generation != gallerySearchGeneration)
			return; // stale response: a newer search superseded this one
		// The render must run on the UI thread (Avalonia controls are not
		// thread-safe); the search returned from the thread pool.
		Avalonia.Threading.Dispatcher.UIThread.Post (() => BuildGalleryTree (results));
	}

	void BuildGalleryTree (List<MarketplaceExtension> results)
	{
		var list = AddinList!;
		list.Items.Clear ();

		// Extensions already installed belong to the Installed tab. A marketplace
		// add-in installed through the gallery is registered under the vsixmanifest
		// "Identity Id", so the gallery row's listing id ("publisher.name") is matched
		// against the manifest's Marketplace.ListingId (persisted at import time); the
		// identity id is also accepted so a hand-imported add-in whose listing id was
		// recorded matches too.
		var installedIds = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		if (host is not null) {
			foreach (var state in host.Addons) {
				installedIds.Add (state.Manifest.Identity.Id ?? "");
				var listingId = state.Manifest.Marketplace?.ListingId;
				if (listingId is { Length: > 0 })
					installedIds.Add (listingId);
			}
		}
		var items = results
			.Where (r => !installedIds.Contains (r.ExtensionId))
			.Select (RowForMarketplace)
			.ToList ();

		RefreshButton.IsVisible = true;
		RepoCombo.IsVisible = false;
		UpdateAllButton.IsVisible = false;
		PageHeaderLabel.Text = results.Count == 0
			? "No Visual Studio extensions found"
			: $"{items.Count} Visual Studio extension(s)";
		UpdatesTabLabel.Text = "Updates";

		if (items.Count == 0) {
			renderedTab = currentTab;
			ShowEmptyDetails ();
			return;
		}
		// One flat category (the marketplace listing is already ordered by the
		// search; a per-category tree is not meaningful for the results).
		var cat = new TreeViewItem {
			Tag = new AddinItem { Name = "Available", Subtitle = "__category__" },
			Header = "Available",
		};
		cat.IsExpanded = true;
		foreach (var it in items.OrderBy (i => i.Name))
			cat.Items.Add (MakeRowItem (it));
		list.Items.Add (cat);
		renderedTab = currentTab;
	}

	AddinItem RowForMarketplace (MarketplaceExtension e)
	{
		var item = new AddinItem {
			Name = e.Title,
			Subtitle = e.Version.Length > 0 ? "v" + e.Version : "",
			Id = e.QualifiedName,
			Version = e.Version,
			Description = e.ShortDescription.Length > 0 ? e.ShortDescription : e.Title,
			Author = e.PublisherName,
			Category = "Available",
			IsInstalled = false,
			Marketplace = e,
		};
		// "More information" links to the marketplace listing for this package.
		item.Url = "https://marketplace.visualstudio.com/items?itemName=" + Uri.EscapeDataString (e.QualifiedName);
		return item;
	}

	protected override void OnClosing (WindowClosingEventArgs e)
	{
		// Stop any in-flight gallery search and release the marketplace client
		// (it owns an HttpClient).
		gallerySearchCts?.Cancel ();
		gallerySearchCts?.Dispose ();
		mktClient?.Dispose ();
		base.OnClosing (e);
	}

	List<AddinItem> LoadUpdates ()
	{
		var list = new List<AddinItem> ();
		if (host is null)
			return list;
		try {
			// New add-in host: an add-in is updatable when several versions of it are
			// staged (shipped tree and/or user root): the newest one is active, the
			// older ones are superseded — the Update action applies the newest and
			// discards the older versions.
			foreach (var state in host.Addons) {
				if (state.Superseded)
					continue;
				if (host.FindVersions (state.Manifest.Identity.Id).Count < 2)
					continue;
				var row = RowFor (state, installed: state.Loaded);
				row.HasUpdate = true;
				list.Add (row);
			}
		} catch (Exception ex) {
			Console.WriteLine ("[addins] load updates failed: " + ex.Message);
		}
		return list;
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
		var item = (AddinList?.SelectedItem as TreeViewItem)?.Tag as AddinItem;
		if (item is null) {
			ShowEmptyDetails ();
			return;
		}
		if (item.IsCategory) {
			// Legacy: category nodes carry no add-in (ColAddin == null) → empty
			// details.
			ShowEmptyDetails ();
			return;
		}
		updatingDetails = true;

		// Details buttons exactly like the legacy ShowAddin switch:
		//  installed+update → Update (+Disable/Uninstall); installed → Disable/Uninstall
		//  (CanDisable/CanUninstall); not installed → Install. Superseded rows (an
		//  older staged version of an active add-in) are information only: the actions
		//  operate on the active version through the Updates page.
		var active = item.State is not { Superseded: true };
		UpdateButton.IsVisible = item.HasUpdate && active;
		InstallButton.IsVisible = !item.IsInstalled && active;
		DisableButton.IsVisible = item.IsInstalled && item.Enabled && active;
		EnableButton.IsVisible = item.IsInstalled && !item.Enabled && active;
		UninstallButton.IsVisible = item.IsInstalled && active;

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
		DetailsDesc!.Text = currentTab switch {
			"updates" => "No updates available.",
			"gallery" => "No add-ins available from the repository.",
			_ => "Select an add-in.",
		};
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
		if (SelectedRow () is not { } item || host is null)
			return;
		var enable = !item.Enabled;
		if (!host.SetEnabled (item.Id, enable, out var error)) {
			Console.WriteLine ("[addins] " + error);
			return;
		}
		// BuildTree keeps the selection across the reload.
		LoadAddins ();
	}

	void OnUninstallClicked (object? sender, RoutedEventArgs e)
	{
		if (SelectedRow () is not { } item || host is null)
			return;
		var name = string.IsNullOrEmpty (item.Name) ? item.Id : item.Name;
		var id = item.Id;
		// Same confirmation shape as MainWindow.DeleteContextNode.
		var msg = new Window {
			Title = "Uninstall Add-in",
			Width = 380,
			SizeToContent = SizeToContent.Height,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Content = new StackPanel {
				Margin = new Thickness (16),
				Spacing = 12,
				Children = {
					new TextBlock { Text = $"Uninstall {name}?", TextWrapping = TextWrapping.Wrap },
					new StackPanel {
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Spacing = 8,
						Children = {
							new Button { Content = "Cancel" },
							new Button { Content = "Uninstall" },
						},
					},
				},
			},
		};
		var sp = (StackPanel)msg.Content!;
		var buttons = ((StackPanel)sp.Children [1]).Children.OfType<Button> ().ToList ();
		buttons [0].Click += (_, _) => msg.Close ();
		buttons [1].Click += (_, _) => {
			msg.Close ();
			if (host.Uninstall (id, out var error))
				LoadAddins ();
			else
				Console.WriteLine ("[addins] " + error);
		};
		_ = msg.ShowDialog (this);
	}

	void OnInstallClicked (object? sender, RoutedEventArgs e)
	{
		_ = InstallSelectedAsync ();
	}

	void OnUpdateClicked (object? sender, RoutedEventArgs e)
	{
		_ = InstallSelectedAsync ();
	}

	// One marketplace import at a time: the download+import takes seconds and the
	// button does not disable while it runs, so a re-entrancy guard keeps a
	// double-click from stacking two installs of the same package.
	bool installingFromMarketplace;

	async Task InstallSelectedAsync ()
	{
		if (SelectedRow () is not { } item)
			return;

		// Gallery (marketplace) row: download the .vsix and import it into the
		// shell's add-in model (the importer generates the shell manifest from
		// extension.vsixmanifest and installs it into the user root).
		if (item.Marketplace is { } ext) {
			await InstallFromMarketplaceAsync (ext);
			return;
		}
		if (host is null || item.State is null)
			return;
		string error;
		// Updates page: apply the newest staged version of the id (the older
		// versions are discarded); Installed page: retry the load of an add-in that
		// did not load at startup.
		var ok = item.IsInstalled
			? host.Update (item.Id, out error)
			: host.LoadAddon (item.State, out error);
		if (!ok) {
			Console.WriteLine ("[addins] " + error);
			return;
		}
		if (!item.IsInstalled)
			SelectTab ("installed");
		LoadAddins ();
	}

	async Task InstallFromMarketplaceAsync (MarketplaceExtension ext)
	{
		if (installingFromMarketplace)
			return;
		if (mktClient is null || mktImporter is null) {
			Console.WriteLine ("[addins] marketplace not ready");
			return;
		}
		// The search results carry the .vsix package URL; fall back to a fresh
		// detail lookup when the listing did not expose it.
		var packageUrl = ext.PackageUrl;
		if (string.IsNullOrEmpty (packageUrl)) {
			try {
				packageUrl = (await mktClient.GetExtensionAsync (ext.ExtensionId, CancellationToken.None))?.PackageUrl ?? "";
			} catch (Exception mex) {
				// Broad: a network/transport failure on the detail lookup must not
				// fault the install task — it only matters when the URL is missing.
				Console.WriteLine ("[addins] gallery detail failed: " + mex.Message);
			}
		}
		if (string.IsNullOrEmpty (packageUrl)) {
			Console.WriteLine ("[addins] no downloadable package for " + ext.ExtensionId);
			return;
		}
		installingFromMarketplace = true;
		PageHeaderLabel.Text = "Installing " + ext.Title + "…";
		try {
			var tmp = Path.Combine (Path.GetTempPath (), "md-avalonia-mkt-" + Guid.NewGuid ().ToString ("N") + ".vsix");
			await mktClient.DownloadPackageAsync (packageUrl, tmp, CancellationToken.None);
			// ext.ExtensionId is the marketplace listing id ("publisher.name"); persisting
			// it lets the gallery de-duplicate this add-in once it is installed.
			var result = mktImporter.Import (tmp, ext.ExtensionId);
			if (result.Success)
				try { File.Delete (tmp); } catch (Exception ex) { Console.WriteLine ("[addins] tmp cleanup: " + ex.Message); }
			if (!result.Success) {
				Console.WriteLine ("[addins] import " + ext.ExtensionId + " failed: " + result.Error);
				PageHeaderLabel.Text = "Install failed: " + ext.Title;
				return;
			}
			Console.WriteLine ("[addins] imported " + ext.ExtensionId + " -> " + result.AddinId
				+ (result.IsVisualStudioDesktop ? " (VS desktop)" : " (VS Code)")
				+ (result.HasEntryPoint ? " [entrypoint]" : " [metadata-only]"));
			// The imported add-in is now a user add-in: switch to Installed so the
			// user sees it registered (and can enable/disable/uninstall it).
			SelectTab ("installed");
			LoadAddins ();
		} catch (Exception ex) {
			// Broad: a transport failure (HttpRequestException — refused connection,
			// DNS, proxy reset) or a bad/renamed package (bad zip, decode error) must
			// surface a visible failure state, not fault the task and leave the header
			// stuck on "Installing…". MarketplaceException is one of these.
			Console.WriteLine ("[addins] marketplace install failed: " + ex.Message);
			PageHeaderLabel.Text = "Install failed: " + ext.Title;
		} finally {
			installingFromMarketplace = false;
		}
	}

	void SelectTab (string tab)
	{
		foreach (var o in TabList?.Items ?? (IEnumerable)Array.Empty<object> ())
			if (o is ListBoxItem { Tag: string t } && t == tab) {
				TabList!.SelectedItem = o;
				return;
			}
	}

	void OnUpdateAllClicked (object? sender, RoutedEventArgs e)
	{
		if (host is null)
			return;
		var pending = CountRowItems (AddinList?.Items, i => i.HasUpdate);
		if (pending == 0)
			return;
		var count = host.UpdateAll (out var error);
		Console.WriteLine (error is null ? $"[addins] updated {count} add-in(s)" : "[addins] " + error);
		LoadAddins ();
	}

	static int CountRowItems (IEnumerable? items, Func<AddinItem, bool> match)
	{
		int n = 0;
		foreach (var o in items ?? (IEnumerable)Array.Empty<object?> ()) {
			if (o is not TreeViewItem tv)
				continue;
			if (tv.Tag is AddinItem { IsCategory: false } it && match (it))
				n++;
			n += CountRowItems (tv.Items, match);
		}
		return n;
	}

	void OnRefreshClicked (object? sender, RoutedEventArgs e)
	{
		// The Refresh button is only shown on the Gallery tab, where it re-runs the
		// marketplace query for the current search term.
		LoadAddins ();
	}

	void OnRepoComboChanged (object? sender, SelectionChangedEventArgs e)
	{
		// Legacy repoCombo: picking a repository reloads the gallery for it; the
		// new host ships a single local catalog, so any selection reloads it.
		if (currentTab != "gallery")
			return;
		LoadAddins ();
	}

	void OnUrlClicked (object? sender, RoutedEventArgs e)
	{
		if (SelectedRow () is not { } item || item.Url.Length == 0)
			return;
		try {
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo (item.Url) { UseShellExecute = true });
		} catch (Exception ex) {
			Console.WriteLine ("[addins] url: " + ex.Message);
		}
	}

	async void OnInstallFromFileClicked (object sender, RoutedEventArgs e)
	{
		if (host is null || TopLevel.GetTopLevel (this)?.StorageProvider is not { } sp)
			return;
		try {
			var files = await sp.OpenFilePickerAsync (new FilePickerOpenOptions {
				AllowMultiple = false,
				Title = "Select the add-in package (.zip)",
				FileTypeFilter = new [] { new FilePickerFileType ("Add-in package") { Patterns = new [] { "*.zip" } } },
			});
			if (files.Count == 0 || files [0].TryGetLocalPath () is not { } path)
				return;
			if (!host.InstallFromFile (path, out var error)) {
				Console.WriteLine ("[addins] install from file: " + error);
				return;
			}
			SelectTab ("installed");
			LoadAddins ();
		} catch (Exception ex) {
			Console.WriteLine ("[addins] install from file: " + ex.Message);
		}
	}

	void OnCloseClicked (object? sender, RoutedEventArgs e) => Close ();
}
