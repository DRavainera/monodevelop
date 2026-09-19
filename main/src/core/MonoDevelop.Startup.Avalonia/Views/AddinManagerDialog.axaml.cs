using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
	public bool Enabled { get; set; } = true;
	public bool IsInstalled { get; set; }
	public Addin? Installed { get; set; }
	public AddinRepositoryEntry? Entry { get; set; }
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
		setupService = CreateSetupService ();
		FilterBox!.TextChanged += (_, _) => ReloadCurrentTab ();
		RefreshButton!.Click += (_, _) => ReloadCurrentTab ();
		UninstallButton!.Click += OnUninstallClicked;
		InstallFromFileButton!.Click += OnInstallFromFileClicked;
		LoadAddins ();
	}

	// Mirrors Runtime.GetAddinRegistryLocation + UserProfile.ForUnix so the shell
	// sees the same add-in registry as the IDE (without referencing MonoDevelop.Core,
	// which is pinned to a different SDK than this Avalonia 12 project).
	static SetupService? CreateSetupService ()
	{
		try {
			string home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty (home))
				home = Environment.GetEnvironmentVariable ("HOME") ?? "";

			var devConfig = Environment.GetEnvironmentVariable ("MONODEVELOP_DEV_CONFIG");
			var devAddins = Environment.GetEnvironmentVariable ("MONODEVELOP_DEV_ADDINS");

			string appId = Path.Combine ("MonoDevelop", "9.0");
			string configDir = devConfig?.Length > 0 ? devConfig : Path.Combine (home, ".config", appId);
			string addinsDir = devAddins?.Length > 0 ? devAddins : Path.Combine (home, ".local", "share", appId, "LocalInstall", "Addins");
			string databaseDir = devAddins?.Length > 0 ? devAddins : Path.Combine (home, ".cache", appId);

			// Use an AddinEngine with an explicit startup directory: the IDE registers
			// its addins via .addins files placed next to the host binaries (net10run),
			// so the scan must start there to share the IDE's add-in catalog.
			var shellDir = Path.GetDirectoryName (typeof (Program).Assembly.Location);
			var hostDir = Path.GetFullPath (Path.Combine (shellDir ?? ".", "..", "..", "..", "..", "..", "..", "build", "net10run"));
			if (!Directory.Exists (hostDir))
				hostDir = shellDir ?? Directory.GetCurrentDirectory ();
			Console.WriteLine ($"[addins] startupDirectory={hostDir}");
			var engine = new AddinEngine ();
			engine.Initialize (configDir, addinsDir, databaseDir, hostDir);
			var registry = engine.Registry;
			registry.Update (new ConsoleProgressStatus ());
			return new SetupService (registry);
		} catch (Exception ex) {
			Console.WriteLine ("[addins] registry init failed: " + ex.Message);
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

		AddinList!.ItemsSource = items;
		TabCountLabel!.Text = items.Count > 0 ? $"({items.Count})" : "";
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
			// Mirrors the GTK dialog: installed addins plus roots (dirs with .addin files).
			foreach (var addin in setupService.Registry.GetAddins ().Union (setupService.Registry.GetAddinRoots ()).DistinctBy (a => a.Id)) {
				if (!InApplicationNamespace (addin.Id))
					continue;
				var desc = addin.Description;
				list.Add (new AddinItem {
					Name = addin.Name,
					Subtitle = "v" + addin.Version + (addin.Enabled ? "" : "  — Disabled"),
					Id = addin.Id,
					Version = addin.Version,
					Description = FirstLine (desc?.Description) ?? "",
					Author = desc?.Author ?? "",
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
				if (setupService.Registry.GetAddin (Addin.GetIdName (entry.Addin.Id)) is not null)
					continue; // already installed
				list.Add (new AddinItem {
					Name = entry.Addin.Name,
					Subtitle = "v" + entry.Addin.Version + " — not installed",
					Id = entry.Addin.Id,
					Version = entry.Addin.Version,
					Description = FirstLine (entry.Addin.Description) ?? "",
					Author = "",
					Entry = entry
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
					Subtitle = $"v{installed.Version} → v{entry.Addin.Version}",
					Id = entry.Addin.Id,
					Version = entry.Addin.Version,
					Description = FirstLine (entry.Addin.Description) ?? "",
					Author = "",
					Entry = entry,
					IsInstalled = true,
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
		DetailsName!.Text = item.Name;
		DetailsVersion!.Text = item.Version.Length > 0 ? "Version " + item.Version : "";
		DetailsAuthor!.Text = item.Author.Length > 0 ? "By " + item.Author : "";
		DetailsDesc!.Text = item.Description;
		EnableCheck!.IsVisible = item.IsInstalled;
		EnableCheck.IsChecked = item.Enabled;
		UninstallButton!.IsVisible = item.IsInstalled;
		updatingDetails = false;
	}

	void ShowEmptyDetails ()
	{
		updatingDetails = true;
		DetailsName!.Text = "";
		DetailsVersion!.Text = "";
		DetailsAuthor!.Text = "";
		DetailsDesc!.Text = currentTab == "updates" ? "No updates available." : "Select an add-in.";
		EnableCheck!.IsVisible = false;
		UninstallButton!.IsVisible = false;
		updatingDetails = false;
	}

	void OnEnableToggled (object? sender, RoutedEventArgs e)
	{
		if (updatingDetails || setupService is null || AddinList?.SelectedItem is not AddinItem item || item.Installed is null)
			return;
		try {
			if (EnableCheck?.IsChecked == true)
				setupService.Registry.EnableAddin (item.Id);
			else
				setupService.Registry.DisableAddin (item.Id);
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

	async void OnInstallFromFileClicked (object? sender, RoutedEventArgs e)
	{
		if (setupService is null)
			return;
		var files = await StorageProvider.OpenFilePickerAsync (new FilePickerOpenOptions {
			Title = "Install add-in package",
			AllowMultiple = false,
			FileTypeFilter = new[] { new FilePickerFileType ("Add-in packages") { Patterns = new[] { "*.mpack" } } }
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
