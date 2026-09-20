using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Loads the redesigned PNG icons of MonoDevelop.Ide (main/src/core/MonoDevelop.Ide/icons)
/// mirroring the legacy ImageService conventions: name-16.png base resource, ~dark variant
/// for the dark theme, ~disabled variant for disabled items and @2x for HiDPI. The icons are
/// placed in the same spots where the legacy GTK UI used them (menu items, section tree...).
/// </summary>
public static class IconService
{
	static readonly string? iconsDir = FindIconsDir ();

	// Fallback chain used when a variant does not exist for a given icon.
	// Mirrors ImageService: exact id → base id → missing-image.
	const string FallbackIcon = "missing-image-16";

	// stock id → base file name (from ExtensionModel/StockIcons.addin.xml); only the
	// ids used by the main menu and the dialogs ported so far.
	static readonly Dictionary<string, string> StockToResource = new () {
		["gtk-copy"] = "copy-16",
		["gtk-cut"] = "cut-16",
		["gtk-paste"] = "paste-16",
		["gtk-delete"] = "remove-16",
		["gtk-undo"] = "undo-16",
		["gtk-redo"] = "redo-16",
		["gtk-indent"] = "indent-16",
		["gtk-unindent"] = "unindent-16",
		["gtk-preferences"] = "preferences-16",
		["gtk-execute"] = "execute-16",
		["gtk-stop"] = "stop-16",
		["gtk-save"] = "save-16",
		["gtk-open"] = "open-16",
		["gtk-close"] = "remove-16",
		["gtk-print"] = "print-16",
		["gtk-find"] = "find-16",
		["gtk-find-and-replace"] = "find-and-replace-16",
		["gtk-zoom-in"] = "zoom-in-16",
		["gtk-zoom-out"] = "zoom-out-16",
		["gtk-zoom-100"] = "zoom-actual-16",
		["gtk-fullscreen"] = "fullscreen-16",
		["gtk-home"] = "home-16",
		["gtk-plugin"] = "plugin-menu-16",
		["gtk-go-back"] = "go-back-16",
		["gtk-go-forward"] = "go-forward-16",
		["gtk-help"] = "help-16",
		["gtk-add"] = "add-16",
		["gtk-remove"] = "remove-16",
		["md-regular-file"] = "file-generic-16",
		["md-save-all"] = "save-all-16",
		["md-new-solution"] = "new-solution-16",
		["md-new-workspace"] = "new-workspace-16",
		["md-close-combine-icon"] = "close-solution-16",
		["md-select-all"] = "select-all-16",
		["md-comment"] = "comment-16",
		["md-reference"] = "reference-16",
		["md-navigate-back"] = "breadcrumb-prev-16",
		["md-navigate-forward"] = "breadcrumb-next-16",
		["md-columns-one"] = "columns-one-16",
		["md-columns-two"] = "columns-two-16",
		["md-find-next"] = "find-next-16",
		["md-find-prev"] = "find-prev-16",
		["md-bookmark-toggle"] = "bookmark-toggle-16",
		["md-bookmark-prev"] = "bookmark-prev-16",
		["md-bookmark-next"] = "bookmark-next-16",
		["md-bookmark-clear-all"] = "bookmark-clear-all-16",
		["md-go-to-line"] = "go-to-line-16",
		["md-go-to-matching-brace"] = "go-to-matching-brace-16",
		["md-open-folder"] = "folder-generic-16",
		["md-updates"] = "status-updates-ready-16",
		["md-prefs-visual-style"] = "prefs-visual-style-16",
		["md-prefs-author-information"] = "prefs-author-information-16",
		["md-prefs-key-bindings"] = "prefs-key-bindings-16",
		["md-prefs-fonts"] = "prefs-fonts-16",
		["md-prefs-updates"] = "prefs-updates-16",
		["md-prefs-task-list"] = "prefs-task-list-16",
		["md-prefs-external-tools"] = "prefs-external-tools-16",
		["md-prefs-load-save"] = "prefs-load-save-16",
		["md-prefs-build"] = "prefs-build-16",
		["md-prefs-sdk-locations"] = "prefs-sdk-locations-16",
		["md-prefs-code-formatting"] = "prefs-code-formatting-16",
		["md-prefs-code-templates"] = "prefs-code-templates-16",
		["md-prefs-dotnet-naming-policies"] = "prefs-dotnet-naming-policies-16",
		["md-prefs-header"] = "prefs-header-16",
		["md-prefs-version-control"] = "prefs-solution-16",
		["md-prefs-language"] = "prefs-language-16",
		["md-prefs-generic"] = "prefs-generic-16",

		// Rendered directly by resource name (no StockIcon entry): the About glyph of the
		// Help menu.
		["about-md-16"] = "about-md-16",
	};

	// Version Control addin stock ids → its own icons directory.
	static readonly Dictionary<string, (string Resource, string Dir)> VcStockToResource = new () {
		["vc-add-command"] = ("vcs-added-16", "MonoDevelop.VersionControl"),
		["vc-remove-command"] = ("vcs-removed-16", "MonoDevelop.VersionControl"),
		["vc-revert-command"] = ("revert-16", "MonoDevelop.VersionControl"),
		["vc-diff"] = ("diff-16", "MonoDevelop.VersionControl"),
		["vc-log"] = ("log-16", "MonoDevelop.VersionControl"),
		["vc-status"] = ("local-status-16", "MonoDevelop.VersionControl"),
		["vc-update"] = ("pull-16", "MonoDevelop.VersionControl"),
		["vc-commit"] = ("commit-16", "MonoDevelop.VersionControl"),
	};

	static readonly Dictionary<(string Resource, bool Dark, bool Disabled, int Scale), Bitmap?> cache = new ();

	public static bool Available => iconsDir is not null;

	/// <summary>Resolved directory of the legacy icon PNGs (for QA diagnostics).</summary>
	public static string? IconsDirectory => iconsDir;

	static string? FindIconsDir ()
	{
		try {
			var dir = AppContext.BaseDirectory;
			for (int i = 0; i < 8 && dir is not null; i++) {
				// build/net10run -> repo root is up 2 levels; from bin/Debug/net10.0 walk up to main/
				var candidate = Path.GetFullPath (Path.Combine (dir, "src", "core", "MonoDevelop.Ide", "icons"));
				if (Directory.Exists (candidate))
					return candidate;
				var candidate2 = Path.GetFullPath (Path.Combine (dir, "..", "..", "..", "..", "..", "..", "src", "core", "MonoDevelop.Ide", "icons"));
				if (Directory.Exists (candidate2))
					return candidate2;
				dir = Path.GetDirectoryName (dir);
			}
			// Repo-relative last resort (running from the repo tree).
			var rel = Path.GetFullPath (Path.Combine (Environment.CurrentDirectory, "src", "core", "MonoDevelop.Ide", "icons"));
			if (Directory.Exists (rel))
				return rel;
		} catch { }
		return null;
	}

	/// <summary>Returns the PNG file that ImageService would pick for a stock id.</summary>
	static string? ResolveFile (string stockId, bool dark, bool disabled, int scale)
	{
		if (iconsDir is null)
			return null;

		string resource;
		string dir = iconsDir;
		if (VcStockToResource.TryGetValue (stockId, out var vc)) {
			resource = vc.Resource;
			var addinsRoot = Path.GetFullPath (Path.Combine (iconsDir, "..", "..", "..", "..", "addins", "VersionControl", vc.Dir, "icons"));
			if (Directory.Exists (addinsRoot))
				dir = addinsRoot;
			else
				return null;
		} else if (!StockToResource.TryGetValue (stockId, out resource)) {
			return null;
		}

		var name = resource;
		string? file = null;
		// Exact variants first (dark/disabled/@2x), then relax, mirroring ImageService fallback.
		foreach (var variant in Variants (name, dark, disabled, scale)) {
			var f = Path.Combine (dir, variant);
			if (File.Exists (f)) {
				file = f;
				break;
			}
		}

		if (file is null && name != FallbackIcon)
			return ResolveFile (FallbackIcon, dark, disabled, scale);
		return file;
	}

	static IEnumerable<string> Variants (string name, bool dark, bool disabled, int scale)
	{
		// order: most specific → base
		if (disabled) {
			if (scale == 2) yield return $"{name}~disabled@2x.png";
			yield return $"{name}~disabled.png";
		}
		if (dark) {
			if (scale == 2) yield return $"{name}~dark@2x.png";
			yield return $"{name}~dark.png";
		}
		if (scale == 2) yield return $"{name}@2x.png";
		yield return $"{name}.png";
	}

	/// <summary>Loads a stock icon as an IImage, honoring the current theme variant.</summary>
	public static IImage? GetImage (string stockId, bool disabled = false, int scale = 1)
	{
		var dark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
		var key = (stockId, dark, disabled, scale);
		if (cache.TryGetValue (key, out var cached) && cached is not null)
			return cached;

		var file = ResolveFile (stockId, dark, disabled, scale);
		if (file is null)
			return null;
		try {
			var bmp = new Bitmap (file);
			cache[key] = bmp;
			return bmp;
		} catch {
			return null;
		}
	}

	/// <summary>True if the stock id is known to this service (used to decide whether to show an icon).</summary>
	public static bool IsKnown (string stockId)
		=> StockToResource.ContainsKey (stockId) || VcStockToResource.ContainsKey (stockId);

	/// <summary>Forces a reload on the next GetImage (theme change already handled via ActualThemeVariant key).</summary>
	public static void ClearCache () => cache.Clear ();
}
