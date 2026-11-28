using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MonoDevelop.AvaloniaAddons.ExtensionModel;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Builds the UI for pads contributed by add-ins through
/// <c>/MonoDevelop/Ide/Pads</c>. The content factories are registered by the
/// shell for the pads it can render with real functionality; unknown pads still
/// get a visible tab so an add-in is never silently dropped.
/// </summary>
public static class AddinPadContentFactory
{
	sealed class Registration
	{
		public Func<Control> Create = null!;
		public string? CommandId;
	}

	static readonly Dictionary<string, Registration> registrations = new (StringComparer.Ordinal);

	public static void Register (string padId, Func<Control> create, string? commandId = null)
	{
		// A later registration without a command id keeps the one already mapped
		// (re-registering a pad's content must not drop its command association).
		if (registrations.TryGetValue (padId, out var existing) && commandId is null)
			commandId = existing.CommandId;
		registrations [padId] = new Registration { Create = create, CommandId = commandId };
	}

	public static bool Supports (string padId) => registrations.ContainsKey (padId);

	public static Control Create (ContributedPad pad)
	{
		if (registrations.TryGetValue (pad.Id, out var r))
			return r.Create ();
		return Placeholder (pad);
	}

	public static string? CommandIdFor (string padId)
		=> registrations.TryGetValue (padId, out var r) ? r.CommandId : null;

	static Control Placeholder (ContributedPad pad)
	{
		var text = new TextBlock {
			Text = $"{pad.Label}\n\nThis pad is contributed by add-in '{pad.AddonId}'.\nIts content is not available in this build.",
			TextWrapping = TextWrapping.Wrap,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness (8),
		};
		text.Bind (TextBlock.ForegroundProperty, Application.Current!.GetResourceObservable ("IdeFgBrush"));
		return new ScrollViewer { Content = text };
	}
}
