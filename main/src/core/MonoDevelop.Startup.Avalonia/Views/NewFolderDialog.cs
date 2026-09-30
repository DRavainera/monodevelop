using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// New Folder — the Avalonia port of the legacy NewFolderDialog
/// (MonoDevelop.Ide.Gui.Dialogs, Xwt): "Folder Name:" entry prefilled with the
/// first free "New Folder[NN]", inline validation (empty name, illegal
/// characters, name already in use disable Add with a warning message) and
/// Cancel/Add buttons. Add creates the directory and exposes NewFolderCreated.
/// The legacy InformationPopoverWidget is rendered as the same inline amber
/// warning the other ported dialogs use.
/// </summary>
public class NewFolderDialog : Window
{
	readonly TextBox folderNameEntry = new ();
	readonly Button addButton = new ();
	readonly TextBlock warningText = new ();
	readonly string parentFolder;

	static IBrush DialogBg (Color fallback) =>
		Application.Current?.TryGetResource ("IdeWindowBgBrush", Application.Current.ActualThemeVariant, out var v) == true && v is IBrush b
			? b : new SolidColorBrush (fallback);

	/// <summary>Directory created when Add is pressed, null if cancelled.</summary>
	public string? NewFolderCreated { get; private set; }

	/// <summary>QA accessors mirroring the legacy validation state.</summary>
	public bool IsAddEnabledForQa => addButton.IsEnabled;
	public string? WarningForQa => warningText.IsVisible ? warningText.Text : null;
	public string FolderNameForQa {
		get => folderNameEntry.Text ?? "";
		set => folderNameEntry.Text = value;
	}

	public NewFolderDialog (string parentFolder)
	{
		this.parentFolder = parentFolder;

		Title = "New Folder";
		Width = 420;
		SizeToContent = SizeToContent.Height;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		SystemDecorations = WindowDecorations.None;
		ExtendClientAreaToDecorationsHint = true;
		Background = DialogBg (Color.Parse ("#2d2d30"));

		var border = new Border {
			Background = DialogBg (Color.Parse ("#2d2d30")),
			BorderBrush = new SolidColorBrush (Color.Parse ("#88888860")),
			BorderThickness = new Thickness (1),
			Padding = new Thickness (14, 12),
		};
		var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

		// Folder Name row (legacy folderNameHBox: label + TextEntry).
		var nameLabel = new TextBlock { Text = "Folder Name:", VerticalAlignment = VerticalAlignment.Center, MinWidth = 80 };
		nameLabel.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		folderNameEntry.MinWidth = 220;
		folderNameEntry.Bind (TextBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		folderNameEntry.Bind (TextBox.BackgroundProperty, Application.Current.GetResourceObservable ("IdeChromeBgBrush"));
		var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		nameRow.Children.Add (nameLabel);
		nameRow.Children.Add (folderNameEntry);
		root.Children.Add (nameRow);

		// Inline validation warning (legacy InformationPopoverWidget).
		warningText.TextWrapping = TextWrapping.Wrap;
		warningText.FontSize = 11.5;
		warningText.Foreground = new SolidColorBrush (Color.Parse ("#e2b93d"));
		warningText.IsVisible = false;
		root.Children.Add (warningText);

		// Action area (legacy order: Cancel, Add; Add is the default command).
		var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
		var cancel = new Button { Content = "Cancel", MinWidth = 80 };
		cancel.Click += (_, _) => Close ();
		addButton.Content = "Add";
		addButton.MinWidth = 80;
		addButton.Click += (_, _) => AddNewFolder ();
		buttons.Children.Add (cancel);
		buttons.Children.Add (addButton);
		root.Children.Add (buttons);

		border.Child = root;
		Content = border;

		folderNameEntry.Text = GetDefaultFolderName ();
		folderNameEntry.TextChanged += (_, _) => Validate ();
		addButton.IsEnabled = false; // Validate() right below re-enables it
		Validate ();
	}

	// Legacy GetDefaultFolderName: "New Folder", then "New Folder1", "New Folder2"…
	string GetDefaultFolderName ()
	{
		const string baseName = "New Folder";
		if (!Directory.Exists (Path.Combine (parentFolder, baseName)))
			return baseName;
		int index = 1;
		while (Directory.Exists (Path.Combine (parentFolder, baseName + index)))
			index++;
		return baseName + index;
	}

	// Legacy FolderNameTextEntryChanged validation chain.
	void Validate ()
	{
		string name = folderNameEntry.Text ?? "";
		string? warning = null;
		if (name.Length == 0) {
			// empty: legacy hides the popover and disables Add
		} else if (!IsValidFolderName (name)) {
			warning = "The name you have chosen contains illegal characters. Please choose a different name.";
		} else if (Directory.Exists (Path.Combine (parentFolder, name))) {
			warning = "Folder name is already in use. Please choose a different name.";
		}
		warningText.Text = warning ?? "";
		warningText.IsVisible = warning is not null;
		addButton.IsEnabled = name.Length > 0 && warning is null;
	}

	static bool IsValidFolderName (string folderName) =>
		folderName.IndexOfAny ([ '/', '\\' ]) == -1 &&
		folderName.IndexOfAny (Path.GetInvalidFileNameChars ()) == -1;

	/// <summary>QA hooks: re-run the live validation and perform the Add action
	/// (create + close) without synthesizing input events.</summary>
	public void ValidateForQa () => Validate ();
	public void AcceptForQa () => AddNewFolder ();

	// Legacy AddNewFolder + AddButtonClicked: create, report, close; errors surface
	// as the inline warning instead of the legacy popover error.
	void AddNewFolder ()
	{
		try {
			var path = Path.Combine (parentFolder, folderNameEntry.Text ?? "");
			Directory.CreateDirectory (path);
			NewFolderCreated = path;
			Close ();
		} catch (Exception ex) {
			warningText.Text = "An error occurred creating the folder. " + ex.Message;
			warningText.IsVisible = true;
		}
	}
}
