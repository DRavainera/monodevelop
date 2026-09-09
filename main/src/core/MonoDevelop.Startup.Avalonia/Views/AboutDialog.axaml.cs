using System;
using System.Reflection;
using Avalonia.Controls;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class AboutDialog : Window
{
	public AboutDialog ()
	{
		InitializeComponent ();
		#if !NETSTANDARD2_0
		var asm = Assembly.GetExecutingAssembly ();
		VersionText!.Text = "Version " + asm.GetName ().Version;
		RuntimeText!.Text = "Runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
			+ " — Avalonia " + typeof (Avalonia.Application).Assembly.GetName ().Version;
		#endif
		CloseButton!.Click += (_, _) => Close ();
	}
}