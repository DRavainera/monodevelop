using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace MonoDevelop.AvaloniaShell.Views;

public class AddinItem
{
	public string Name { get; set; } = "";
	public string Id { get; set; } = "";
	public string Version { get; set; } = "";
	public string Publisher { get; set; } = "";
	public string Description { get; set; } = "";
	public string Category { get; set; } = "";
	public string Tags { get; set; } = "";
	public string Nodes { get; set; } = "";
	public int EpCount { get; set; }
	// Load state of the add-in in the new Avalonia host (AddonLoadState).
	public bool Loaded { get; set; } = true;
	public string Error { get; set; } = "";
	// Row icon: the add-in's own icon (identity.icon resolved by the host) or the
	// generic plugin-32 fallback, exactly like the legacy dialog's StoreIcon.
	public IImage Icon { get; set; }

	// Row rendering: "Name" + secondary line (version + publisher; the legacy markup
	// cell shows the description, which the details panel carries here); category
	// header rows and not-loaded addins render grey like the legacy disabled rows.
	public string DisplayName => Subtitle == "__category__" ? Category : Name;
	public string SecondaryText => Subtitle == "__category__" ? "" : $"{Version} · {Publisher}";
	public Avalonia.Media.IBrush NameBrush =>
		Subtitle == "__category__" ? Avalonia.Media.Brushes.SteelBlue :
		Loaded ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.Gray;
	public bool IsCategory => Subtitle == "__category__";
	public string Subtitle { get; set; } = "";
}

public partial class AddinManagerDialog : Window
{
	// Identity icons are resolved once per file and cached for the dialog's lifetime
	// (the list reloads on every filter change; the PNGs don't change underneath us).
	static readonly Dictionary<string, Bitmap> iconCache = new ();
	Bitmap? genericIcon;

	readonly MonoDevelop.AvaloniaAddons.AddonHost? host;

	public AddinManagerDialog ()
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		host = MonoDevelop.AvaloniaShell.App.Addins;
		genericIcon = LoadIcon ("plugin-32.png") ?? LoadIcon ("plugin-22.png")!;

		// Tab icon (same resource the legacy tab loads: plugin-22).
		TabIconInstalled!.Source = LoadIcon ("plugin-22.png");

		FilterBox!.TextChanged += (_, _) => LoadAddins ();
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

	void LoadAddins ()
	{
		var items = new List<AddinItem> ();
		if (host is not null) {
			foreach (var state in host.Addons) {
				var id = state.Manifest.Identity;
				string category = state.Manifest.Categories.Count > 0 ? state.Manifest.Categories [0] : "Other";
				var nodes = state.Manifest.Extensions
					.Select (kv => $"{kv.Key} ({kv.Value?.Count ?? 0})")
					.OrderBy (s => s, StringComparer.Ordinal);
				Bitmap icon;
				try {
					var file = host.ResolveIconFile (state);
					icon = file is null ? genericIcon : BitmapFrom (file);
				} catch (Exception ex) {
					Console.WriteLine ("[addins] icon for " + id.Id + ": " + ex.Message);
					icon = genericIcon;
				}
				items.Add (new AddinItem {
					Name = string.IsNullOrEmpty (id.DisplayName) ? id.Name : id.DisplayName,
					Id = id.Id,
					Version = id.Version,
					Publisher = id.Publisher,
					Description = id.Description,
					Category = category,
					Tags = string.Join (", ", state.Manifest.Tags),
					Nodes = string.Join (", ", nodes),
					EpCount = state.Manifest.Extensions.Count,
					Loaded = state.Loaded,
					Error = state.Error ?? "",
					Icon = icon
				});
			}
		}
		items = ApplyFilter (items);

		// Page header (legacy labelInstalled style: the count of the current page).
		var loaded = items.Count (i => i.Loaded);
		PageHeaderLabel.Text = host is null
			? "(no add-in host)"
			: items.Count == 0 ? "No add-ins found"
			: items.Count == 1 ? "1 add-in installed"
			: $"{items.Count} add-ins installed ({loaded} loaded)";

		// Category grouping (legacy AddinTreeWidget.ShowCategories): one header row
		// per category, "Other" last.
		List<AddinItem> grouped = new ();
		foreach (var g in items.GroupBy (i => string.IsNullOrEmpty (i.Category) ? "Other" : i.Category)
			.OrderBy (g => g.Key == "Other" ? "\uFFFF" : g.Key)) {
			grouped.Add (new AddinItem { Name = g.Key, Subtitle = "__category__", Icon = genericIcon });
			foreach (var it in g.OrderBy (i => i.Name))
				grouped.Add (it);
		}
		AddinList!.ItemsSource = grouped;
		Console.WriteLine ($"[addins] tab=installed items={items.Count} loaded={loaded} icons={iconCache.Count}");
		if (grouped.Count == 0) {
			ShowEmptyDetails ();
			return;
		}
		// Keep the current selection when the filter still matches it, else select the
		// first add-in row (never a category header).
		var current = AddinList.SelectedItem as AddinItem;
		int idx = -1;
		if (current is { IsCategory: false })
			idx = grouped.FindIndex (i => i.Id == current.Id);
		if (idx < 0)
			idx = grouped.FindIndex (i => !i.IsCategory);
		if (idx >= 0)
			AddinList.SelectedIndex = idx;
		else
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

	void OnAddinSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (AddinList?.SelectedItem is not AddinItem item) {
			ShowEmptyDetails ();
			return;
		}
		if (item.IsCategory) {
			// Category header rows are not selectable content (legacy sets
			// ColAllowSelection=false on non-addin rows).
			ShowEmptyDetails ();
			return;
		}

		// Status header (legacy boxHeader): shown when the add-in did not load,
		// mirroring the disabled/broken notices of the GTK dialog.
		DetailsHeader.IsVisible = !item.Loaded;
		if (!item.Loaded) {
			DetailsHeaderIcon.Source = LoadIcon ("update-16.png");
			DetailsHeaderText.Text = string.IsNullOrEmpty (item.Error)
				? "This add-in is not loaded."
				: "Not loaded: " + item.Error;
		}

		DetailsName!.Text = item.Name;
		DetailsVersion!.Text = $"{item.Id} · v{item.Version}";
		DetailsAuthor!.Text = item.Publisher.Length > 0 ? "By " + item.Publisher : "";
		DetailsDesc!.Text = item.Description;
		DetailsTags!.Text = item.Tags.Length > 0 ? item.Tags : "(none)";
		DetailsNodes!.Text = item.Nodes.Length > 0 ? item.Nodes : "(no contributions)";
		Console.WriteLine ($"[addins] selected {item.Id} loaded={item.Loaded} eps={item.EpCount}");
	}

	void ShowEmptyDetails ()
	{
		DetailsHeader.IsVisible = false;
		DetailsName!.Text = "";
		DetailsVersion!.Text = "";
		DetailsAuthor!.Text = "";
		DetailsDesc!.Text = host is null ? "The add-in host is not running." : "Select an add-in.";
		DetailsTags!.Text = "";
		DetailsNodes!.Text = "";
	}

	void OnCloseClicked (object? sender, RoutedEventArgs e) => Close ();
}
