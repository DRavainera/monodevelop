using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MonoDevelop.AvaloniaShell.Views
{
	/// <summary>
	/// Add-in Manager for the Avalonia shell: lists the add-ins loaded by the new
	/// add-in host (<see cref="MonoDevelop.AvaloniaAddons.AddonHost"/>) with their state
	/// and the extension nodes they contribute. The legacy Mono.Addins catalog and its
	/// dialog stay with the GTK IDE.
	/// </summary>
	public partial class AddonManagerDialog : Window
	{
		public AddonManagerDialog ()
		{
			InitializeComponent ();
			MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);

			var host = MonoDevelop.AvaloniaShell.App.Addins;
			AddonsRoot!.Text = host is null
				? "(no add-in host)"
				: $"{host.RootDirectory}\n{host.Addons.Count} add-in(s) discovered, {host.Addons.Count (a => a.Loaded)} loaded";
			AddonsList!.Items.Clear ();
			foreach (var state in host?.Addons ?? Enumerable.Empty<MonoDevelop.AvaloniaAddons.AddonLoadState> ()) {
				var label = $"{state.Manifest.Identity.Name}  ({state.Manifest.Identity.Id} {state.Manifest.Identity.Version})"
					+ (state.Loaded ? "" : state.Error is null ? "  [not loaded]" : "  [error]");
				AddonsList.Items.Add (new ListBoxItem { Tag = state, Content = label });
			}
			if (host?.Addons is { Count: > 0 } first) {
				AddonsList.SelectedIndex = 0;
				ShowDetail (first [0]);
			}
		}

		void OnAddonSelected (object? sender, SelectionChangedEventArgs e)
		{
			if (AddonsList?.SelectedItem is ListBoxItem { Tag: MonoDevelop.AvaloniaAddons.AddonLoadState s })
				ShowDetail (s);
		}

		void ShowDetail (MonoDevelop.AvaloniaAddons.AddonLoadState s)
		{			var id = s.Manifest.Identity;
			DetailTitle!.Text = id.Name;
			DetailId!.Text = $"{id.Id} {id.Version} · {id.Publisher}";
			DetailState!.Text = s.Error is null
				? (s.Loaded ? "Loaded" : "Not loaded")
				: "Error: " + s.Error;
			DetailDescription!.Text = id.Description;
			DetailTags!.Text = s.Manifest.Tags.Count == 0 ? "(none)" : string.Join (", ", s.Manifest.Tags);
			var nodes = string.Join (Environment.NewLine, s.Manifest.Extensions.Select (kv =>
				kv.Key + ": " + string.Join (", ", (kv.Value ?? new ()).Select (n => string.IsNullOrEmpty (n.Label) ? n.Id : n.Label))));
			DetailNodes!.Text = nodes.Length == 0 ? "(no contributions)" : nodes;
			Console.WriteLine ($"[avalonia-addons-mgr] {id.Id} state={(s.Loaded ? "loaded" : "not-loaded")} nodes={s.Manifest.Extensions.Sum (kv => kv.Value?.Count ?? 0)}");
		}

		void OnClose (object? sender, RoutedEventArgs e) => Close ();
	}
}
