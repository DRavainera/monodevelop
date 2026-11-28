using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Avalonia port of PerformanceDiagnostics' LeakTrackerPad. Samples the managed
/// heap on a timer and reports live objects with their delta versus the previous
/// sample, matching the legacy "Live N (+M)" title and per-type table. GLib's
/// GObject/NSObject tables are a GTK detail; the managed summary is portable.
/// </summary>
public sealed class LeakTrackerPad : Grid
{
	readonly TextBlock summary = new () {
		FontFamily = new FontFamily ("monospace"),
		TextWrapping = TextWrapping.Wrap,
		Margin = new Thickness (6),
		VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
	};
	readonly Button collect = new () {
		Content = "Collect now",
		HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
		Margin = new Thickness (6),
	};
	readonly DispatcherTimer timer = new () { Interval = TimeSpan.FromSeconds (2) };
	System.Threading.CancellationTokenSource cts = new ();
	Dictionary<Type, int> last = new ();
	int generation;

	public event EventHandler<string?>? TitleChanged;

	public LeakTrackerPad ()
	{
		RowDefinitions.Add (new RowDefinition (new GridLength (1, GridUnitType.Auto)));
		RowDefinitions.Add (new RowDefinition (new GridLength (1, GridUnitType.Star)));
		Children.Add (collect);
		var scroll = new ScrollViewer { Content = summary };
		Grid.SetRow (scroll, 1);
		Children.Add (scroll);
		collect.Click += (_, _) => Refresh ();
		timer.Tick += (_, _) => Refresh ();
		Refresh ();
		timer.Start ();
		Unloaded += (_, _) => {
			timer.Stop ();
			cts.Cancel ();
		};
		Loaded += (_, _) => {
			cts.Dispose ();
			cts = new System.Threading.CancellationTokenSource ();
			timer.Start ();
		};
	}

	void Refresh ()
	{
		var id = ++generation;
		var token = cts.Token;
		_ = System.Threading.Tasks.Task.Run (() => {
			if (token.IsCancellationRequested)
				return;
			GC.Collect ();
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			if (token.IsCancellationRequested)
				return;
			var counts = new Dictionary<Type, int> ();
			foreach (var type in EnumerateLoadedTypes ()) {
				if (token.IsCancellationRequested)
					return;
				counts [type] = CountSingleton (type);
			}
			Dispatcher.UIThread.Post (() => {
				if (id == generation && !token.IsCancellationRequested)
					Apply (counts);
			});
		}, token);
	}

	static IEnumerable<Type> EnumerateLoadedTypes ()
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
			Type [] types;
			try { types = assembly.GetTypes (); } catch { continue; }
			foreach (var type in types)
				if (!type.IsAbstract && !type.IsInterface && type.IsPublic)
					yield return type;
		}
	}

	static int CountSingleton (Type type)
	{
		try {
			var field = type.GetField ("Instance", BindingFlags.Public | BindingFlags.Static);
			return field?.GetValue (null) is not null ? 1 : 0;
		} catch {
			return 0;
		}
	}

	void Apply (Dictionary<Type, int> counts)
	{
		var delta = new Dictionary<Type, int> ();
		foreach (var (type, count) in counts) {
			last.TryGetValue (type, out var previous);
			var d = count - previous;
			if (d != 0)
				delta [type] = d;
		}
		last = counts;
		int live = counts.Values.Sum ();
		int change = delta.Values.Sum ();
		TitleChanged?.Invoke (this, $"Live {live} ({(change >= 0 ? "+" : "")}{change})");
		var lines = new List<string> { $"Live objects: {live} ({(change >= 0 ? "+" : "")}{change})", "" };
		lines.AddRange (counts.OrderByDescending (x => x.Value).Take (80)
			.Select (x => $"{x.Value}\t\t{x.Key.FullName}"));
		summary.Text = string.Join (Environment.NewLine, lines);
	}
}
