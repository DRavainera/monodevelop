using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Port of the legacy FindInFilesDialog (MonoDevelop.Ide.FindInFiles): Find/Replace
/// modes, scope store (Whole solution / All solutions / Current project / All open
/// files / Directories / Current document / Selection), path + recursive, file mask
/// and the FilterOptions case/whole-word/regex switches. On accept it runs the search
/// over the resolved scope and delivers the results to the Search Results pad.
/// </summary>
public partial class FindInFilesDialog : Window
{
	public bool ReplaceMode { get; set; }

	// QA hook: pre-loads the find text (used by --find automated runs).
	public string? SearchTextOverride { get; set; }

	public FindInFilesDialog ()
	{
		InitializeComponent ();
		var mask = MaskCombo!;
		foreach (var m in new[] { "*", "*.cs", "*.cs;*.xaml;*.axaml;*.json;*.csproj", "*.md;*.txt" })
			mask.Items.Add (new ComboBoxItem { Content = m });
		mask.SelectedIndex = 0;
		var dir = MainWindow.Instance is { } owner && owner.LoadedSolutionDirectory () is { } d ? d : Environment.GetFolderPath (Environment.SpecialFolder.Personal);
		PathCombo!.Text = dir;
		if (SearchTextOverride is { Length: > 0 } pre)
			FindCombo!.Text = pre;
		Opened += (_, _) => {
			if (SearchTextOverride is { Length: > 0 })
				MainWindow.Instance?.RunFindInFiles (this); // automated run
			else
				FindCombo!.Focus ();
		};
	}

	void OnModeFind (object? sender, RoutedEventArgs e) => SetMode (false);
	void OnModeReplace (object? sender, RoutedEventArgs e) => SetMode (true);

	void SetMode (bool replace)
	{
		ReplaceMode = replace;
		ModeLabel!.Text = replace ? "Replace in Files" : "Find in Files";
		TitleText!.Text = replace ? "Replace in Files" : "Find in Files";
		ReplaceRow!.IsVisible = replace;
		ActionBtn!.Content = replace ? "Replace" : "Find";
	}

	void OnBrowse (object? sender, RoutedEventArgs e) => _ = PickAsync ();

	async Task PickAsync ()
	{
		var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions {
			Title = "Select a folder",
			AllowMultiple = false,
		});
		if (folders.Count > 0)
			PathCombo!.Text = folders [0].Path.LocalPath;
	}

	// ---------- Search execution (legacy Scope/FilterOptions) ----------

	public string SearchText =>
		FindCombo!.Text is { Length: > 0 } t ? t : (SearchTextOverride ?? "");
	public string ReplaceText => ReplaceCombo!.Text ?? "";
	public string FileMask => (MaskCombo!.Text is { Length: > 0 } t ? t : "*");
	public bool CaseSensitive => CaseSensitiveCheck!.IsChecked == true;
	public bool WholeWords => WholeWordCheck!.IsChecked == true;
	public bool Regex => RegexCheck!.IsChecked == true;
	public bool Recursive => RecursiveCheck!.IsChecked == true;

	public string? SearchDirectory => ScopeCombo!.SelectedIndex switch {
		4 => PathCombo!.Text,
		_ => MainWindow.Instance?.LoadedSolutionDirectory (),
	};

	public static List<(string File, int Line, int Offset, int Length, string LineText)> Search (
		string? root, string searchText, bool caseSensitive, bool wholeWords, bool regex,
		bool recursive, string fileMask, string? replaceWith = null, bool doReplace = false)
	{
		var results = new List<(string, int, int, int, string)> ();
		if (string.IsNullOrEmpty (searchText) || string.IsNullOrEmpty (root) || !Directory.Exists (root))
			return results;

		var masks = (fileMask ?? "*").Split (';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

		// Skip bin/obj/.git like the legacy FileProvider excludes.
		static bool Excluded (string path) => path.Contains ("/bin/") || path.Contains ("/obj/") || path.Contains ("/.git/");

		var pattern = regex ? searchText : System.Text.RegularExpressions.Regex.Escape (searchText);
		if (wholeWords)
			pattern = $@"\b(?:{pattern})\b";
		var opts = caseSensitive
			? System.Text.RegularExpressions.RegexOptions.None
			: System.Text.RegularExpressions.RegexOptions.IgnoreCase;

		IEnumerable<string> files;
		try {
			files = masks.SelectMany (m => Directory.EnumerateFiles (root, m, option)).Distinct ();
		} catch (Exception ex) {
			Console.WriteLine ("[findinfiles] enumerate failed: " + ex.Message);
			return results;
		}

		foreach (var file in files) {
			if (Excluded (file))
				continue;
			string text;
			try {
				if (new FileInfo (file).Length > 2_000_000)
					continue;
				text = File.ReadAllText (file);
			} catch {
				continue;
			}
			try {
				var matches = System.Text.RegularExpressions.Regex.Matches (text, pattern, opts);
				if (matches.Count == 0)
					continue;
				if (doReplace && !string.IsNullOrEmpty (replaceWith)) {
					var replaced = System.Text.RegularExpressions.Regex.Replace (text, pattern, replaceWith, opts);
					if (replaced != text)
						File.WriteAllText (file, replaced);
				}
				foreach (System.Text.RegularExpressions.Match m in matches) {
					var lineStart = text.LastIndexOf ('\n', Math.Max (0, m.Index - 1)) + 1;
					var lineEnd = text.IndexOf ('\n', m.Index + m.Length);
					if (lineEnd < 0) lineEnd = text.Length;
					var lineText = text [lineStart..lineEnd].TrimEnd ('\r');
					var line = 1 + text.Substring (0, m.Index).Count (c => c == '\n');
					results.Add ((file, line, m.Index, m.Length, lineText));
					if (results.Count >= 1000)
						return results;
				}
			} catch (System.ArgumentException) {
				return results; // invalid regex
			} catch { }
		}
		return results;
	}

	void OnAction (object? sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty (SearchText)) {
			Close ();
			return;
		}
		MainWindow.Instance?.RunFindInFiles (this);
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e) => Close ();
}
