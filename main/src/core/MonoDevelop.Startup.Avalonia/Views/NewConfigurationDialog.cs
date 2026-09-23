using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// New Configuration — the Avalonia port of the legacy NewConfigurationDialog
/// (MonoDevelop.Ide.Gui.Dialogs, Xwt): Name combo (editable, existing configs),
/// Platform combo ("Any CPU" = empty id), "Create configurations for all
/// solution items" check (solution-only), inline validation popover and
/// OK/Cancel. OK returns ConfigName ("Name" or "Name|Platform") + CreateChildren.
/// </summary>
public class NewConfigurationDialog : Window
{
	readonly ComboBox nameCombo = new ();
	readonly ComboBox platformCombo = new ();
	readonly CheckBox createChildrenCheck = new ();
	readonly Button okButton = new ();
	readonly TextBlock warningText = new ();
	readonly HashSet<string> existingConfigs;

	static IBrush DialogBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeWindowBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	static IBrush Fg () =>
		Application.Current?.TryGetResource ("IdeFgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : Brushes.White;

	/// <summary>Platform names offered like the legacy project platform list.</summary>
	static readonly string[] Platforms = { "Any CPU", "x86", "x64", "ARM64" };

	public string ConfigName { get; private set; } = "";
	public bool CreateChildren => createChildrenCheck.IsChecked ?? false;
	public bool Accepted { get; private set; }

	/// <summary>QA accessor: mirrors the legacy ValidateText result (OK enabled).</summary>
	public bool IsOkEnabledForQa => okButton.IsEnabled;

	/// <param name="configurations">Existing configuration names, e.g. "Debug",
	/// "Release", "Debug|x86" (legacy ItemConfigurationCollection keys).</param>
	/// <param name="isSolution">Shows the create-children check only for
	/// solutions (the legacy hides it for projects).</param>
	public NewConfigurationDialog (IEnumerable<string> configurations, bool isSolution)
	{
		Title = "New Configuration";
		Width = 460;
		SizeToContent = SizeToContent.Height;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		Background = DialogBg (Color.Parse ("#2d2d30"));

		existingConfigs = new HashSet<string> (configurations ?? Enumerable.Empty<string> (), StringComparer.OrdinalIgnoreCase);

		var border = new Border {
			Background = DialogBg (Color.Parse ("#2d2d30")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			Padding = new Thickness (14, 12),
		};
		var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

		// Name row.
		root.Children.Add (MakeRow ("Name:", BuildCombo (nameCombo, existingConfigs.OrderBy (c => c, StringComparer.Ordinal).Select (NamePart).Distinct (), "")));

		// Platform row.
		root.Children.Add (MakeRow ("Platform:", BuildCombo (platformCombo, Platforms, "Any CPU")));

		// Validation warning (the legacy InformationPopoverWidget rendered as a
		// popover; the same message inline keeps the validation behavior).
		warningText.Text = "";
		warningText.TextWrapping = TextWrapping.Wrap;
		warningText.FontSize = 11.5;
		warningText.Foreground = new SolidColorBrush (Color.Parse ("#e2b93d"));
		warningText.IsVisible = false;
		root.Children.Add (warningText);

		// Solution-only create-children check.
		createChildrenCheck.Content = "Create configurations for all solution items";
		createChildrenCheck.IsChecked = isSolution;
		createChildrenCheck.IsVisible = isSolution;
		createChildrenCheck.Bind (ContentControl.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		root.Children.Add (createChildrenCheck);

		// Buttons.
		var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
		var cancel = new Button { Content = "Cancel", MinWidth = 80 };
		cancel.Click += (_, _) => Close ();
		okButton.Content = "OK";
		okButton.MinWidth = 80;
		okButton.IsEnabled = false; // legacy ValidateText: empty name → disabled
		okButton.Click += (_, _) => {
			ConfigName = BuildConfigName ();
			Accepted = true;
			Close ();
		};
		buttons.Children.Add (cancel);
		buttons.Children.Add (okButton);
		root.Children.Add (buttons);

		border.Child = root;
		Content = border;

		ValidateText ();
	}

	static string NamePart (string config)
	{
		int i = config.LastIndexOf ('|');
		return i == -1 ? config : config.Substring (0, i);
	}

	static Control MakeRow (string labelText, Control editor)
	{
		var label = new TextBlock {
			Text = labelText,
			VerticalAlignment = VerticalAlignment.Center,
			MinWidth = 70,
		};
		label.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		panel.Children.Add (label);
		panel.Children.Add (editor);
		return panel;
	}

	static Control BuildCombo (ComboBox combo, IEnumerable<string> items, string initial)
	{
		foreach (var it in items)
			combo.Items.Add (it);
		combo.SelectedItem = initial;
		combo.MinWidth = 260;
		combo.IsEditable = true; // the legacy uses ComboBoxEntry (editable)
		return combo;
	}

	string BuildConfigName ()
	{
		var name = (nameCombo.SelectedItem as string ?? nameCombo.Text ?? "").Trim ();
		var plat = GetPlatformId ((platformCombo.SelectedItem as string ?? platformCombo.Text ?? "").Trim ());
		return string.IsNullOrEmpty (plat) ? name : name + "|" + plat;
	}

	// Legacy MultiConfigItemOptionsPanel.GetPlatformId/GetPlatformName.
	static string GetPlatformId (string name) => name == "Any CPU" || name.Length == 0 ? "" : name;

	/// <summary>Legacy ValidateText: empty name or '|' in name or duplicate
	/// ConfigName → warning + OK disabled.</summary>
	void ValidateText ()
	{
		var name = (nameCombo.SelectedItem as string ?? nameCombo.Text ?? "").Trim ();
		var configName = BuildConfigName ();
		bool ok;
		if (name.Length == 0 || name.Contains ('|')) {
			ok = false;
			warningText.Text = "Please enter a valid configuration name.";
		} else if (existingConfigs.Contains (configName)) {
			ok = false;
			warningText.Text = $"A configuration with the name '{configName}' already exists.";
		} else {
			ok = true;
		}
		warningText.IsVisible = !ok;
		okButton.IsEnabled = ok;
	}
}
