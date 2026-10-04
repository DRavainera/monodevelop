using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Mono.Addins;
using Mono.Addins.Setup;

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
	// Legacy AddinStatus flags driving the row icon and the details buttons.
	public bool HasUpdate { get; set; }
	public string Url { get; set; } = "";
	public Addin? Installed { get; set; }
	public AddinRepositoryEntry? Entry { get; set; }

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
}

public partial class AddinManagerDialog : Window
{
	// Minimal console progress monitor for install/uninstall operations.
	class ConsoleProgressStatus : IProgressStatus
	{
		public bool Cancelled { get; set; }
		public bool IsCanceled => Cancelled;
		public int LogLevel { get; set; }
		public void Cancel () => Cancelled = true;
		public void SetMessage (string msg) => ReportMessage (msg);
		public void SetProgress (double progress) { }
		public void ReportError (string message) => Console.WriteLine ("[addins] ERROR: " + message);
		public void ReportError (string message, Exception exception) => Console.WriteLine ("[addins] ERROR: " + message + " " + exception?.Message);
		public void ReportWarning (string message) => Console.WriteLine ("[addins] WARN: " + message);
		public void ReportMessage (string message) => Console.WriteLine ("[addins] " + message);
		public void Log (string msg) => Console.WriteLine ("[addins] " + msg);
	}

	readonly SetupService? setupService;
	string currentTab = "installed";
	bool updatingDetails;

	public AddinManagerDialog ()
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		setupService = CreateSetupService ();

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

	// Mirrors Runtime.GetAddinRegistryLocation + UserProfile.ForUnix so the shell
	// sees the same add-in registry as the IDE (without referencing MonoDevelop.Core,
	// which is pinned to a different SDK than this Avalonia 12 project).
	static SetupService? CreateSetupService ()
	{
		// Shared engine (also initialised at startup) so the registry is loaded once.
		if (!MonoDevelop.Ide.Services.AddinEngineHost.EnsureInitialized ())
			return null;
		var registry = MonoDevelop.Ide.Services.AddinEngineHost.Engine?.Registry;
		return registry is null ? null : new SetupService (registry);
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
		Console.WriteLine ($"[addins] tab={currentTab} items={items.Count} registry={setupService is not null}");
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

	// Namespace filter mirroring the GTK dialog's Services.InApplicationNamespace.
	bool InApplicationNamespace (string id)
	{
		try {
			var ns = setupService?.ApplicationNamespace;
			if (string.IsNullOrEmpty (ns))
				return true;
			return id == ns || id.StartsWith (ns + ".", StringComparison.Ordinal);
		} catch {
			return true;
		}
	}

	static string? FirstLine (string? text)
	{
		if (string.IsNullOrEmpty (text))
			return null;
		var idx = text.IndexOfAny (new[] { '\r', '\n' });
		return idx > 0 ? text.Substring (0, idx) : text;
	}

	List<AddinItem> LoadInstalled ()
	{
		var list = new List<AddinItem> ();
		if (setupService is null)
			return list;
		try {
			// Mirrors the GTK dialog LoadInstalled: modules + roots, not-hidden, in
			// namespace, with Disabled status when missing dependencies.
			foreach (var addin in setupService.Registry.GetAddins ().Union (setupService.Registry.GetAddinRoots ()).DistinctBy (a => a.Id)) {
				if (!InApplicationNamespace (addin.Id))
					continue;
				var desc = addin.Description;
				list.Add (new AddinItem {
					Name = addin.Name,
					Subtitle = addin.Version,
					Id = addin.Id,
					Version = addin.Version,
					Description = FirstLine (desc?.Description) ?? "",
					Author = desc?.Author ?? "",
					Category = addin.Description?.Category ?? "Other",
					Enabled = addin.Enabled,
					IsInstalled = true,
					Installed = addin
				});
			}
		} catch (Exception ex) {
			Console.WriteLine ("[addins] load installed failed: " + ex.Message);
		}
		return list;
	}


	List<AddinItem> LoadGallery ()
	{
		var list = new List<AddinItem> ();
		if (setupService is null)
			return list;
		try {
			foreach (var entry in setupService.Repositories.GetAvailableAddins ()) {
				if (!InApplicationNamespace (entry.Addin.Id))
					continue;
				var installed = setupService.Registry.GetAddin (Addin.GetIdName (entry.Addin.Id));
				bool hasUpdate = installed is not null && CompareVersions (installed.Version, entry.Addin.Version) < 0;
				list.Add (new AddinItem {
					Name = entry.Addin.Name,
					Subtitle = entry.Addin.Version,
					Id = entry.Addin.Id,
					Version = entry.Addin.Version,
					Description = FirstLine (entry.Addin.Description) ?? "",
					Category = entry.Addin.Category ?? "Other",
					Url = entry.Addin.Url ?? "",
					IsInstalled = installed is not null,
					HasUpdate = hasUpdate,
					Entry = entry,
					Installed = installed
				});
			}
		} catch (Exception ex) {
			Console.WriteLine ("[addins] load gallery failed: " + ex.Message);
		}
		return list;
	}

	List<AddinItem> LoadUpdates ()
	{
		var list = new List<AddinItem> ();
		if (setupService is null)
			return list;
		try {
			foreach (var entry in setupService.Repositories.GetAvailableAddins (RepositorySearchFlags.LatestVersionsOnly)) {
				if (!InApplicationNamespace (entry.Addin.Id))
					continue;
				var installed = setupService.Registry.GetAddin (Addin.GetIdName (entry.Addin.Id));
				if (installed is null || !installed.Enabled)
					continue;
				if (CompareVersions (installed.Version, entry.Addin.Version) <= 0)
					continue;
				list.Add (new AddinItem {
					Name = entry.Addin.Name,
					Subtitle = entry.Addin.Version,
					Id = entry.Addin.Id,
					Version = entry.Addin.Version,
					Description = FirstLine (entry.Addin.Description) ?? "",
					Category = entry.Addin.Category ?? "Other",
					Url = entry.Addin.Url ?? "",
					IsInstalled = true,
					HasUpdate = true,
					Entry = entry,
					Installed = installed
				});
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
		//  (CanDisable/CanUninstall); not installed → Install; broken → Uninstall only.
		bool canDisable = true, canUninstall = true;
		if (item.Installed is not null) {
			try {
				canDisable = item.Installed.Description.CanDisable;
				canUninstall = item.Installed.Description.CanUninstall;
			} catch { /* defaults */ }
		}
		bool broken = item.IsInstalled && item.Installed is not null && !item.Installed.Enabled && item.Id.Length == 0;
		UpdateButton.IsVisible = item.HasUpdate;
		InstallButton.IsVisible = !item.IsInstalled;
		DisableButton.IsVisible = item.IsInstalled && item.Enabled && canDisable;
		EnableButton.IsVisible = item.IsInstalled && !item.Enabled;
		UninstallButton.IsVisible = item.IsInstalled && canUninstall;

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
		if (setupService is null || AddinList?.SelectedItem is not AddinItem item || item.Installed is null)
			return;
		try {
			item.Installed.Enabled = !item.Installed.Enabled;
		} catch (Exception ex) {
			Console.WriteLine ("[addins] enable/disable failed: " + ex.Message);
		}
		LoadAddins ();
	}

	void OnUninstallClicked (object? sender, RoutedEventArgs e)
	{
		if (setupService is null || AddinList?.SelectedItem is not AddinItem item)
			return;
		try {
			setupService.Uninstall (new ConsoleProgressStatus (), item.Id);
		} catch (Exception ex) {
			Console.WriteLine ("[addins] uninstall failed: " + ex.Message);
		}
		LoadAddins ();
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
		if (setupService is null || AddinList?.SelectedItem is not AddinItem item || item.Entry is null)
			return;
		try {
			setupService.Install (new ConsoleProgressStatus (), new[] { item.Entry });
		} catch (Exception ex) {
			Console.WriteLine ("[addins] install failed: " + ex.Message);
		}
		LoadAddins ();
	}

	void OnUpdateAllClicked (object? sender, RoutedEventArgs e)
	{
		if (setupService is null)
			return;
		var entries = (AddinList?.ItemsSource as IEnumerable<AddinItem>)?
			.Where (i => i.Entry is not null).Select (i => i.Entry!).ToArray () ?? Array.Empty<AddinRepositoryEntry> ();
		if (entries.Length == 0)
			return;
		try {
			setupService.Install (new ConsoleProgressStatus (), entries);
		} catch (Exception ex) {
			Console.WriteLine ("[addins] update-all failed: " + ex.Message);
		}
		LoadAddins ();
	}

	void OnRefreshClicked (object? sender, RoutedEventArgs e)
	{
		if (setupService is null)
			return;
		try {
			setupService.Repositories.UpdateAllRepositories (new ConsoleProgressStatus ());
		} catch (Exception ex) {
			Console.WriteLine ("[addins] refresh failed: " + ex.Message);
		}
		LoadAddins ();
	}

	void OnRepoMenuClicked (object? sender, RoutedEventArgs e)
	{
		// Legacy repoCombo: All repositories / per-repo / Manage Repositories...
		// Single upstream repo on this machine: keep the label static for now.
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

	async void OnInstallFromFileClicked (object? sender, RoutedEventArgs e)
	{
		if (setupService is null)
			return;
		var files = await StorageProvider.OpenFilePickerAsync (new FilePickerOpenOptions {
			Title = "Install Extension Package",
			AllowMultiple = false,
			FileTypeFilter = new[] { new FilePickerFileType ("Extension packages") { Patterns = new[] { "*.mpack" } } }
		});
		if (files.Count == 0)
			return;
		try {
			var path = files [0].Path.LocalPath;
			setupService.Install (new ConsoleProgressStatus (), path);
		} catch (Exception ex) {
			Console.WriteLine ("[addins] install failed: " + ex.Message);
		}
		LoadAddins ();
	}

	void OnCloseClicked (object? sender, RoutedEventArgs e) => Close ();
}
