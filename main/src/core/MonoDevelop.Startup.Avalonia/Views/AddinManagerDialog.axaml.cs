using Avalonia.Controls;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class AddinManagerDialog : Window
{
	public AddinManagerDialog ()
	{
		InitializeComponent ();
		CloseButton!.Click += (_, _) => Close ();
		RefreshButton!.Click += (_, _) => {
			AddinList!.SelectedItem = null;
			CloseButton.Focus ();
		};
	}
}