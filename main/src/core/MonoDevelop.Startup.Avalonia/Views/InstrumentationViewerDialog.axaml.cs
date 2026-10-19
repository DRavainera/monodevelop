using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Visualizador de instrumentación del shell (Fila C): equivalente in-proceso
/// del legacy mdmonitor (la herramienta Gtk# no se empaqueta). El comando
/// Tools > Instrumentation Monitor publica el InstrumentationService y muestra
/// aquí todos los contadores registrados, actualizándose cada 500 ms. Las
/// columnas siguen las del monitor legacy (Count = nº de muestras, Total =
/// acumulado, Last = último valor).
/// </summary>
public partial class InstrumentationViewerDialog : Window
{
	public class CounterRow
	{
		public string Name { get; init; } = "";
		public string Category { get; init; } = "";
		public int Count { get; init; }
		public int TotalCount { get; init; }
		public int LastValue { get; init; }
		public string LastUpdate { get; init; } = "";
		public string Message { get; init; } = "";
	}

	readonly Avalonia.Threading.DispatcherTimer timer;

	public InstrumentationViewerDialog ()
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		// FuncDataTemplate (codebase pattern): a XAML DataTemplate cannot infer
		// the row type when the ItemsSource is set from code.
		CountersList.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<CounterRow> ((row, _) => {
			var grid = new Grid { Margin = new Thickness (0, 2), ColumnDefinitions = new ColumnDefinitions ("*,120,90,90,90,90,90") };
			var name = new TextBlock { Text = row?.Name, TextTrimming = TextTrimming.CharacterEllipsis };
			var cat = new TextBlock { Text = row?.Category, TextTrimming = TextTrimming.CharacterEllipsis };
			var count = new TextBlock { Text = (row?.Count ?? 0).ToString () };
			var total = new TextBlock { Text = (row?.TotalCount ?? 0).ToString () };
			var last = new TextBlock { Text = (row?.LastValue ?? 0).ToString () };
			var since = new TextBlock { Text = row?.LastUpdate };
			var msg = new TextBlock { Text = row?.Message, TextTrimming = TextTrimming.CharacterEllipsis };
			foreach (var tb in new[] { name, cat, count, total, last, since, msg })
				tb.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
			grid.Children.Add (name);
			grid.Children.Add (cat);
			grid.Children.Add (count);
			grid.Children.Add (total);
			grid.Children.Add (last);
			grid.Children.Add (since);
			grid.Children.Add (msg);
			Grid.SetColumn (cat, 1);
			Grid.SetColumn (count, 2);
			Grid.SetColumn (total, 3);
			Grid.SetColumn (last, 4);
			Grid.SetColumn (since, 5);
			Grid.SetColumn (msg, 6);
			return grid;
		});
		timer = new Avalonia.Threading.DispatcherTimer {
			Interval = TimeSpan.FromMilliseconds (500),
		};
		timer.Tick += (_, _) => Refresh ();
		Opened += (_, _) => {
			Refresh ();
			timer.Start ();
		};
		Closed += (_, _) => timer.Stop ();
	}

	void Refresh ()
	{
		var rows = new List<CounterRow> ();
		foreach (var cat in InstrumentationService.GetCategories ()) {
			foreach (var c in cat.Counters) {
				var last = c.LastValue;
				rows.Add (new CounterRow {
					Name = c.Name,
					Category = cat.Name,
					Count = c.Count,
					TotalCount = c.TotalCount,
					LastValue = last.Value,
					LastUpdate = last.TimeStamp == DateTime.MinValue ? "" : last.TimeStamp.ToString ("HH:mm:ss"),
					Message = last.Message ?? "",
				});
			}
		}
		CountersList.ItemsSource = rows;
		StatusText.Text = rows.Count + " counter(s)";
	}

	/// <summary>QA hook: current row count and status line for log greps.</summary>
	public (int Rows, string Status) StatusForQa () => (CountersList.Items.Count, StatusText.Text);

	void OnClose (object? sender, RoutedEventArgs e) => Close ();
}
