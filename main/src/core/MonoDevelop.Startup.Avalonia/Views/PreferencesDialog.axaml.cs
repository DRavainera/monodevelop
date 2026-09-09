using Avalonia.Controls;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class PreferencesDialog : Window
{
	readonly string [] titles = { "General", "Source Code", "Build", "Projects", "Version Control", "Add-ins" };

	public PreferencesDialog ()
	{
		InitializeComponent ();
		PanelList!.SelectionChanged += (_, _) => {
			PanelTitle!.Text = PanelList.SelectedIndex >= 0 ? titles [PanelList.SelectedIndex] : "";
		};
		OkButton!.Click += (_, _) => Close ();
		CancelButton!.Click += (_, _) => Close ();
	}
}