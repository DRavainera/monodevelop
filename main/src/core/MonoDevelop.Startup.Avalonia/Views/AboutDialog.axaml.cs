using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MonoDevelop.Core;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

// Port of the legacy CommonAboutDialog (MonoDevelop.Ide.Gui.Dialogs): a product
// page (AboutMonoDevelopTabPage) plus a version-information page
// (VersionInformationTabPage), with a Show/Hide Details button and a
// Copy Information button that is revealed once details are shown.
//
// The legacy data sources are not reachable in this shell and are rebuilt from
// shell-safe sources, reusing the engine-free parts of the shared backend:
//   * The legacy version comes from IdeVersionInfo.MonoDevelopVersion, which
//     reads BuildInfo (the version.config path). The shell does not use
//     version.config; GetVersion() is the single seam for the shell's own
//     version source (pending the mechanism the operator will define).
//   * SystemInformation.GetDescription() enumerates ISystemInformationProvider
//     through the Mono.Addins engine, which the shell never starts
//     (CoreGettextShim exists precisely to keep the legacy Runtime out of the
//     shell). The detail sections are therefore rebuilt here from the pure,
//     engine-free static helpers (GetBuildInformation) plus the shell's own
//     add-in host. The Operating System section is enriched beyond the legacy
//     single "uname" line via OperatingSystemInfo (distribution, desktop,
//     graphical server, architecture and packaging kind, per OS).
//   * BrandingService reads through the real GettextCatalog in MonoDevelop.Core,
//     whose static ctor drags in Runtime.Preferences, so the branding values
//     are read straight from the branding directory in the same XML shape.
public partial class AboutDialog : Window
{
	bool showingDetails;
	bool assembliesLoaded;
	string productVersion;
	string[] osBodyLines = Array.Empty<string> ();

	public AboutDialog ()
	{
		InitializeComponent ();
		MonoDevelop.Ide.Controls.DialogWindow.Apply (this);
		productVersion = GetVersion ();

		// Title (legacy: "About {ApplicationName}").
		ChromeTitle.Text = GettextCatalog.GetString ("About {0}", AppName);
		// The chrome row is reused as the drag handle by DialogWindow.Apply; keep
		// the window Title in sync so non-chrome fallbacks read correctly.
		Title = ChromeTitle.Text;

		VersionLabel.Text = GettextCatalog.GetString ("Version");
		VersionText.Text = productVersion;
		LicenseLabel.Text = GettextCatalog.GetString ("License");
		CopyrightLabel.Text = GettextCatalog.GetString ("Copyright");
		DetailsButton.Content = GettextCatalog.GetString ("Show Details");
		CopyButton.Content = GettextCatalog.GetString ("Copy Information");
		ShowAsmButton.Content = GettextCatalog.GetString ("Show loaded assemblies");

		PopulateProductPage ();
		PopulateDetailsPage ();
	}

	// The version is the seam: the legacy reads it from BuildInfo (version.config),
	// which the shell deliberately does not use. Until the shell's own version
	// source is defined, fall back to the shell assembly's version.
	static string GetVersion ()
		=> typeof (AboutDialog).Assembly.GetName ().Version?.ToString () ?? "?";

	// Application name from the branding, same defaults as the legacy
	// BrandingService (the shell ships no Branding.xml, so this stays "MonoDevelop").
	static string AppName
	{
		get
		{
			var name = ReadBrandingValue ("ApplicationName");
			return string.IsNullOrEmpty (name) ? "MonoDevelop" : name!;
		}
	}

	// Read a single branding value straight from branding/Branding.xml, mirroring
	// the legacy BrandingService.GetElement (children of the root, by name) but
	// without going through the real GettextCatalog (which would pull the legacy
	// Runtime into the shell). Returns null when there is no branding file.
	static string? ReadBrandingValue (string key)
	{
		try {
			var path = System.IO.Path.Combine (AppContext.BaseDirectory, "branding", "Branding.xml");
			if (!System.IO.File.Exists (path))
				return null;
			var doc = System.Xml.Linq.XDocument.Load (path);
			return (string?)doc.Root?.Element (key);
		} catch {
			return null;
		}
	}

	void PopulateProductPage ()
	{
		// Branding image (legacy: BrandingService.GetFile then the embedded
		// resource). The shell ships the image as an Avalonia resource.
		try {
			var uri = new Uri ("avares://MonoDevelop.AvaloniaShell/branding/AboutImage.png");
			AboutImage.Source = new Bitmap (AssetLoader.Open (uri));
		} catch {
			// Branding image is optional; the page keeps its layout without it.
			AboutImage.IsVisible = false;
		}

		// License section (legacy: shown only when LicenseTermsUrl is present;
		// the privacy link only when PrivacyStatementUrl is present too).
		var licenseUrl = ReadBrandingValue ("LicenseTermsUrl");
		var privacyUrl = ReadBrandingValue ("PrivacyStatementUrl");
		if (!string.IsNullOrEmpty (licenseUrl)) {
			LicenseTermsLink.Text = GettextCatalog.GetString ("License Terms");
			LicenseTermsLink.AddHandler (
				PointerPressedEvent,
				(EventHandler<Avalonia.Input.PointerPressedEventArgs>)((_, _) => OpenUrl (licenseUrl!)));
			LicenseSection.IsVisible = true;
			if (!string.IsNullOrEmpty (privacyUrl)) {
				PrivacyLink.Text = GettextCatalog.GetString ("Privacy Statement");
				PrivacyLink.AddHandler (
					PointerPressedEvent,
					(EventHandler<Avalonia.Input.PointerPressedEventArgs>)((_, _) => OpenUrl (privacyUrl!)));
				PrivacyLink.IsVisible = true;
			} else {
				PrivacyLink.IsVisible = false;
			}
		} else {
			LicenseSection.IsVisible = false;
		}

		var year = DateTime.Now.Year;
		// Line order: the shell author first, then the legacy lines.
		Copyright1.Text = "© " + year + " Daniel Ravainera & AI Agents Model";
		Copyright2.Text = (year == 2016 ? "© 2016" : "© 2016–" + year) + " Microsoft Corp.";
		Copyright3.Text = "© 2004–" + year + " Xamarin Inc.";
		Copyright4.Text = "© 2004–" + year + " MonoDevelop contributors";
	}

	// Build the version-information sections (legacy VersionInformationTabPage
	// iterated ISystemInformationProvider; here they are rebuilt from
	// shell-safe sources, in the same order and with the same titles).
	void PopulateDetailsPage ()
	{
		SectionsPanel.Children.Clear ();
		// Legacy: the Build Information section is always shown — when no build
		// info is available the text falls back to "Build information unavailable".
		var buildInfo = SystemInformation.GetBuildInformation ().Trim ();
		if (buildInfo.Length == 0)
			buildInfo = "Build information unavailable";
		// Operating System: enriched report (distribution, desktop, graphical
		// server, architecture, packaging kind) per OS — see
		// OperatingSystemInfo. The legacy SystemInformation body was just a
		// "uname -mrsv" line / Environment.OSVersion.
		var osDescription = OperatingSystemInfo.GetDescription ();
		osBodyLines = osDescription.Split ('\n');
		var sections = new List<(string Title, string Body)> {
			(AppName, BuildProductSection ()),
			(GettextCatalog.GetString ("Build Information"), buildInfo),
			(GettextCatalog.GetString ("Operating System"), osDescription),
		};

		var userAddins = BuildUserAddinsSection ();
		if (userAddins.Length > 0)
			sections.Add ((GettextCatalog.GetString ("Enabled user installed extensions"), userAddins));

		// Keep the plain-text form for the Copy button, in the legacy
		// SystemInformation.GetTextDescription layout.
		var text = new StringBuilder ();
		foreach (var (title, body) in sections) {
			if (body.Length == 0)
				continue;
			SectionsPanel.Children.Add (AddSection (title, body));
			text.Append ("=== ").Append (title.Trim ()).Append (" ===\n\n");
			text.Append (body.Trim ()).Append ("\n\n");
		}
		textDescription = text.ToString ();
	}

	// One section as a selectable text block: a bold title, a blank line, then
	// the body (legacy: a selectable Gtk.Label with <b>title</b> markup).
	Control AddSection (string title, string body)
	{
		var block = new TextBlock {
			TextWrapping = Avalonia.Media.TextWrapping.Wrap,
			Margin = new Thickness (0, 0, 0, 10),
			FontFamily = new FontFamily ("Monospace,DejaVu Sans Mono,Consolas"),
		};
		block.Inlines!.Add (new Run (title) { FontWeight = FontWeight.Bold });
		block.Inlines.Add (new Run (Environment.NewLine));
		block.Inlines.Add (new Run (Environment.NewLine));
		block.Inlines.Add (new Run (body));
		return block;
	}

	string BuildProductSection ()
	{
		// Legacy IdeVersionInfo product block, minus the GTK# / native-runtime
		// lines that do not apply to the Avalonia shell.
		var sb = new StringBuilder ();
		sb.Append ("Version ").AppendLine (productVersion);
		sb.Append ("Installation UUID: ").AppendLine (InstallationUuid);
		return sb.ToString ();
	}

	// Stable per-installation id, persisted with the same key and store the
	// legacy PropertyService uses (MonoDevelopProperties.xml) so both UIs agree.
	static string InstallationUuid
	{
		get
		{
			var key = "MonoDevelop.Core.InstallUuid";
			var value = UserPreferences.Get (key);
			if (string.IsNullOrEmpty (value)) {
				value = Guid.NewGuid ().ToString ();
				UserPreferences.Set (key, value);
			}
			return value;
		}
	}

	// Enabled user-installed extensions from the shell's own add-in host
	// (legacy listed the Mono.Addins registry; the shell host is the equivalent).
	string BuildUserAddinsSection ()
	{
		var host = App.Addins;
		if (host is null)
			return "";
		var lines = host.Addons
			.Where (a => a.IsUserAddin && a.Enabled && !a.Superseded)
			.Select (a => {
				var id = a.Manifest.Identity;
				var name = string.IsNullOrEmpty (id.DisplayName) ? id.Name : id.DisplayName;
				return string.Format ("{0} {1}", string.IsNullOrEmpty (name) ? id.Id : name, id.Version);
			})
			.Where (s => s.Length > 0)
			.ToArray ();
		return string.Join (Environment.NewLine, lines);
	}

	// Expanded by the "Show loaded assemblies" button (legacy CreateAssembliesTable).
	void OnShowAssemblies (object? sender, RoutedEventArgs e)
	{
		if (assembliesLoaded)
			return;
		assembliesLoaded = true;
		ShowAsmButton.IsVisible = false;

		var sb = new StringBuilder ();
		sb.AppendLine (GettextCatalog.GetString ("Loaded Assemblies"));
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()
			.Where (a => !a.IsDynamic)
			.OrderBy (a => a.FullName, StringComparer.OrdinalIgnoreCase)) {
			try {
				var name = asm.GetName ();
				string location;
				try { location = !string.IsNullOrEmpty (asm.Location) ? System.IO.Path.GetFullPath (asm.Location) : "(in-memory)"; }
				catch { location = "(dynamic)"; }
				sb.AppendLine ($"{name.Name,-50} {name.Version,-14} {location}");
			} catch {
			}
		}

		var block = new TextBlock {
			TextWrapping = Avalonia.Media.TextWrapping.Wrap,
			FontFamily = new FontFamily ("Monospace,DejaVu Sans Mono,Consolas"),
			FontSize = 11,
			Text = sb.ToString (),
		};
		AsmPanel.Children.Add (block);
		AsmPanel.IsVisible = true;
	}

	void OnToggleDetails (object? sender, RoutedEventArgs e)
	{
		showingDetails = !showingDetails;
		ProductPage.IsVisible = !showingDetails;
		DetailsPage.IsVisible = showingDetails;
		CopyButton.IsVisible = showingDetails;
		DetailsButton.Content = showingDetails
			? GettextCatalog.GetString ("Hide Details")
			: GettextCatalog.GetString ("Show Details");
	}

	// Copy the version-information text in the legacy GetTextDescription layout.
	async void OnCopy (object? sender, RoutedEventArgs e)
	{
		var clipboard = TopLevel.GetTopLevel (this)?.Clipboard;
		if (clipboard is null)
			return;
		await clipboard.SetTextAsync (textDescription);
	}

	static void OpenUrl (string url)
	{
		try {
			System.Diagnostics.Process.Start (
				new System.Diagnostics.ProcessStartInfo (url) { UseShellExecute = true });
		} catch {
		}
	}

	void OnClose (object? sender, RoutedEventArgs e) => Close ();

	string textDescription = "";

	// QA oracle: dump the About dialog's content to the Output pad so automated
	// runs can validate the port without UI navigation. One line per field,
	// plus one line per detail section and the assembly count.
	public void QaAbout ()
	{
		MainWindow.Instance?.Output ("[about] title=" + ChromeTitle.Text
			+ " version=" + VersionText.Text
			+ " licenseVisible=" + (LicenseSection.IsVisible ? "True" : "False"));
		MainWindow.Instance?.Output ("[about] copyright1=" + Copyright1.Text);
		MainWindow.Instance?.Output ("[about] copyright2=" + Copyright2.Text);
		MainWindow.Instance?.Output ("[about] copyright3=" + Copyright3.Text);
		MainWindow.Instance?.Output ("[about] copyright4=" + Copyright4.Text);

		// Detail sections in the same order as they are painted.
		var i = 0;
		foreach (var child in SectionsPanel.Children) {
			if (child is TextBlock tb && tb.Inlines is { Count: > 0 } inl
				&& inl[0] is Run { Text: { } title }) {
				MainWindow.Instance?.Output ($"[about] section{i}={title}");
				// The body is the last inline (after title, blank, blank).
				if (inl[inl.Count - 1] is Run { Text: { } body })
					foreach (var line in body.Split ('\n'))
						if (line.Trim ().Length > 0)
							MainWindow.Instance?.Output ($"[about] section{i} body: {line.TrimEnd ()}");
			}
			i++;
		}

		// Enriched Operating System section, one oracle line per body line so
		// the QA can validate distribution / desktop / graphical server /
		// architecture / packaging kind field by field.
		var j = 0;
		foreach (var line in osBodyLines) {
			MainWindow.Instance?.Output ($"[about] os{j}={line.TrimStart ('\t')}");
			j++;
		}

		// Force the assemblies table so the count is stable for the oracle.
		if (!assembliesLoaded)
			OnShowAssemblies (null, new RoutedEventArgs ());
		int asmCount = 0;
		if (AsmPanel.Children.FirstOrDefault () is TextBlock asmTb) {
			asmCount = asmTb.Text?.Split ('\n').Count (l => l.Trim ().Length > 0) ?? 0;
		}
		MainWindow.Instance?.Output ("[about] assemblies=" + (asmCount > 0 ? asmCount - 1 : asmCount)
			+ " copyLen=" + textDescription.Length
			+ " copyFirstLine=" + (textDescription.Length > 0
				? textDescription.Split ('\n')[0].Trim ()
				: "(empty)"));
	}
}
