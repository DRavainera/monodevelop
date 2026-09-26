using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Tip of the Day — the Avalonia port of the legacy TipOfTheDayWindow
/// (MonoDevelop.Ide.Gui.Dialogs): info icon + "Did you know...?" header, the
/// tip text read from TipsOfTheDay.xml, a "Don't show this at startup" check
/// (persisted in the same preference the legacy uses) and Next Tip / Close.
/// </summary>
public class TipOfTheDayDialog : Window
{
	const string TipsPreferenceKey = "MonoDevelop.Ide.ShowTipsAtStartup";

	readonly TextBlock tipText = new ();
	readonly CheckBox noShowCheck = new ();
	readonly List<string> tips = new ();
	int currentTip;

	static IBrush DialogBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeWindowBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	static IBrush Fg () =>
		Application.Current?.TryGetResource ("IdeFgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : Brushes.White;

	public TipOfTheDayDialog ()
	{
		Title = "Tip of the Day";
		Width = 520;
		Height = 240;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None; // chrome Avalonia, no OS
		ExtendClientAreaToDecorationsHint = true;
		Background = DialogBg (Color.Parse ("#2d2d30"));

		LoadTips ();
		if (tips.Count > 0)
			currentTip = new Random ().Next () % tips.Count; // legacy: random first tip
		else
			currentTip = -1;

		var border = new Border {
			Background = DialogBg (Color.Parse ("#2d2d30")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			Padding = new Thickness (14, 12),
		};

		var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

		// Header: info icon + "Did you know...?" (the legacy ImageView gtk-dialog-info).
		var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		var icon = IconService.GetResourceImage ("file-information-16", 2);
		if (icon is not null)
			header.Children.Add (new Image { Source = icon, Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center });
		var cat = new TextBlock {
			Text = "Did you know...?",
			FontSize = 15,
			FontWeight = FontWeight.Bold,
			VerticalAlignment = VerticalAlignment.Center,
		};
		cat.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		header.Children.Add (cat);
		root.Children.Add (header);

		// Tip body (read-only view, wraps).
		tipText.Text = currentTip >= 0 ? tips [currentTip] : "No tips found.";
		tipText.TextWrapping = TextWrapping.Wrap;
		tipText.FontSize = 12.5;
		tipText.MinHeight = 80;
		tipText.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		root.Children.Add (tipText);

		// "Don't show at startup" (legacy: noshowCheckbutton checked means the
		// preference becomes false — it mirrors ShowTipsAtStartup inverted).
		noShowCheck.Content = "Don't show tips at startup";
		noShowCheck.IsChecked = !UserPreferences.GetBool (TipsPreferenceKey, defaultValue: true);
		noShowCheck.IsCheckedChanged += (_, _) =>
			UserPreferences.SetBool (TipsPreferenceKey, !(noShowCheck.IsChecked ?? false));
		noShowCheck.Bind (ContentControl.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		root.Children.Add (noShowCheck);

		// Buttons: "_Next Tip" + Close (legacy HButtonBox, right-aligned).
		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8,
		};
		var next = new Button { Content = "_Next Tip", MinWidth = 88 };
		next.Click += (_, _) => NextTip ();
		var close = new Button { Content = "Close", MinWidth = 88 };
		close.Click += (_, _) => Close ();
		buttons.Children.Add (next);
		buttons.Children.Add (close);
		root.Children.Add (buttons);

		border.Child = root;
		Content = border;
	}

	// Loads <TIP> entries from TipsOfTheDay.xml (the legacy file, searched like
	// PropertyService.DataPath: build/data/options first, then source options/).
	void LoadTips ()
	{
		var candidates = new[] {
			Path.Combine ("build", "data", "options", "TipsOfTheDay.xml"),
			Path.Combine ("src", "core", "MonoDevelop.Ide", "options", "TipsOfTheDay.xml"),
		};
		// Walk up from the app base to the repo root (bin/Debug/net10.0 → main/).
		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 8 && dir is not null; i++) {
			foreach (var rel in candidates) {
				var full = Path.GetFullPath (Path.Combine (dir, rel));
				if (!File.Exists (full))
					continue;
				try {
					var doc = new XmlDocument ();
					doc.Load (full);
					foreach (XmlNode node in doc.DocumentElement!.ChildNodes) {
						var text = node.InnerText?.Trim ();
						if (!string.IsNullOrEmpty (text))
							tips.Add (text);
					}
					return;
				} catch { /* fall through to next candidate */ }
			}
			dir = Path.GetDirectoryName (dir);
		}
	}

	void NextTip ()
	{
		if (tips.Count == 0)
			return;
		currentTip = currentTip + 1;
		if (currentTip >= tips.Count)
			currentTip = 0;
		tipText.Text = tips [currentTip];
	}
}
