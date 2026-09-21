using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Add Reference dialog (legacy AddReferenceDialog, assemblies tab): pick a known
/// assembly/package name and the active project's .csproj gets a &lt;Reference/&gt;.
/// </summary>
public class AddReferenceDialog : Window
{
	static readonly string[] KnownAssemblies = {
		"System.Json", "System.Numerics.Vectors", "System.Runtime.Serialization",
		"System.Transactions", "System.Web", "System.Windows", "System.Xml.Linq",
		"System.Drawing.Common", "Microsoft.CSharp", "Newtonsoft.Json",
	};

	readonly TextBox? customBox;
	readonly ListBox list;
	readonly TextBlock status;
	readonly string projectPath;

	public string? AddedReference { get; private set; }

	public AddReferenceDialog (string projectPath)
	{
		this.projectPath = projectPath;
		Title = "Add Reference";
		Width = 420;
		Height = 420;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;

		var header = new TextBlock {
			Text = "Choose an assembly reference:",
			Margin = new Thickness (12, 12, 12, 6),
			FontSize = 12,
		};
		header.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));

		list = new ListBox { Margin = new Thickness (12, 0) };
		list.ItemsSource = KnownAssemblies;
		list.Bind (ListBox.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));
		list.DoubleTapped += (_, _) => AddSelected ();

		customBox = new TextBox {
			Watermark = "Custom assembly or package name",
			Margin = new Thickness (12, 8, 12, 0),
			FontSize = 12,
		};

		var buttons = new StackPanel {
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness (12, 10, 12, 0),
			Spacing = 8,
		};
		var ok = new Button { Content = "OK", Width = 80 };
		ok.Click += (_, _) => AddSelected ();
		var cancel = new Button { Content = "Cancel", Width = 80 };
		cancel.Click += (_, _) => Close ();
		buttons.Children.Add (ok);
		buttons.Children.Add (cancel);

		status = new TextBlock {
			Text = "",
			Margin = new Thickness (12, 8, 12, 12),
			FontSize = 11,
			Opacity = 0.8,
		};
		status.Bind (TextBlock.ForegroundProperty, Application.Current.GetResourceObservable ("IdeFgBrush"));

		var root = new StackPanel { Spacing = 0 };
		root.Children.Add (header);
		root.Children.Add (list);
		root.Children.Add (customBox);
		root.Children.Add (buttons);
		root.Children.Add (status);
		Content = root;
	}

	void AddSelected ()
	{
		var name = customBox?.Text?.Trim () ?? "";
		if (name.Length == 0 && list.SelectedItem is string sel)
			name = sel;
		if (name.Length == 0) {
			status.Text = "Select an assembly or type a name.";
			return;
		}
		TryAddReference (name);
	}

	/// <summary>Adds the reference to the project file. Returns false if duplicated/failed.</summary>
	public bool TryAddReference (string name)
	{
		try {
			var doc = XDocument.Load (projectPath);
			var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
			if (doc.Descendants (ns + "Reference")
				.Any (r => (r.Attribute ("Include")?.Value ?? "").Split (',') [0] == name)) {
				status.Text = $"'{name}' is already referenced.";
				return false;
			}
			doc.Root!.Add (new XElement (ns + "ItemGroup",
				new XElement (ns + "Reference", new XAttribute ("Include", name))));
			doc.Save (projectPath);
			AddedReference = name;
			Close ();
			return true;
		} catch (Exception ex) {
			status.Text = "Error: " + ex.Message;
			return false;
		}
	}
}

file static class StringExt
{
	public static string OrEmptyIfNull (this string? s) => s ?? "";
}
