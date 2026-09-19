using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class AboutDialog : Window
{
	bool showingDetails;

	public AboutDialog ()
	{
		InitializeComponent ();
		PopulateProductPage ();
		PopulateDetailsPage ();
	}

	void PopulateProductPage ()
	{
		VersionText.Text = typeof (AboutDialog).Assembly.GetName ().Version?.ToString () ?? "?";

		Copyright1.Text = "© 2016–" + DateTime.Now.Year + " Microsoft Corp.";
		Copyright2.Text = "© 2004–" + DateTime.Now.Year + " Xamarin Inc.";
		Copyright3.Text = "© 2004–" + DateTime.Now.Year + " MonoDevelop contributors";

		try {
			var uri = new Uri ("avares://MonoDevelop.AvaloniaShell/branding/AboutImage.png");
			AboutImage.Source = new Bitmap (AssetLoader.Open (uri));
		} catch {
			// Branding image is optional; the page keeps its layout without it.
			AboutImage.IsVisible = false;
		}
	}

	void PopulateDetailsPage ()
	{
		var sb = new StringBuilder ();
		sb.AppendLine ("MonoDevelop " + VersionText.Text);
		sb.AppendLine ("Runtime: .NET " + Environment.Version);
		sb.AppendLine ("OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
		sb.AppendLine ("Architecture: " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);

		SystemInfoText.Text = sb.ToString ();

		var asmSb = new StringBuilder ();
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ().Where (a => !a.IsDynamic).OrderBy (a => a.GetName ().Name, StringComparer.OrdinalIgnoreCase)) {
			var name = asm.GetName ();
			if (name.Name is null || !(name.Name.StartsWith ("MonoDevelop") || name.Name.StartsWith ("Mono.") || name.Name.StartsWith ("Microsoft.CodeAnalysis")))
				continue;
			string location;
			try { location = !string.IsNullOrEmpty (asm.Location) ? System.IO.Path.GetFullPath (asm.Location) : "(in-memory)"; }
			catch { location = "(dynamic)"; }
			asmSb.AppendLine ($"{name.Name,-50} {name.Version,-14} {location}");
		}
		AssembliesText.Text = asmSb.ToString ();
	}

	void OnToggleDetails (object? sender, RoutedEventArgs e)
	{
		showingDetails = !showingDetails;
		ProductPage.IsVisible = !showingDetails;
		DetailsPage.IsVisible = showingDetails;
		CopyButton.IsVisible = showingDetails;
		DetailsButton.Content = showingDetails ? "Hide Details" : "Show Details";
	}

	async void OnCopy (object? sender, RoutedEventArgs e)
	{
		var clipboard = TopLevel.GetTopLevel (this)?.Clipboard;
		if (clipboard is null)
			return;
		await clipboard.SetTextAsync (SystemInfoText.Text + "\n\n" + AssembliesText.Text);
	}

	void OnClose (object? sender, RoutedEventArgs e) => Close ();
}
