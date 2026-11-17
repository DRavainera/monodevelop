using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

// ---------- F5: Build > Compiler (C#) — the Code Generation panel ----------
// Port of the legacy MonoDevelop.CSharp.Project.CodeGenerationPanel (its widget
// is CodeGenerationPanelWidget): nine controls in two sections (General Options
// / Warnings) that edit the C# compiler parameters of ONE configuration of the
// ACTIVE project, persisted in the project file's
//   <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Cfg|Plat' ">
// group. This is NOT the global settings store: the legacy panel is a
// MultiConfigItemOptionsPanel over DotNetProjectConfiguration/CSharpCompiler
// Parameters, so its changes land in the .csproj, not in user preferences.
//
// Scope (per the operator): the Avalonia port wires the same build-config fields
// to the active project through the shell's lightweight ConfigurationService
// (no full MonoDevelop.Projects model), editing the project's ACTIVE
// configuration (resolved by GetActiveConfiguration + GetProjectConfigurationFor,
// like the GTK RunConfiguration toolbar). The legacy base adds a configuration/
// platform selector combo; this port follows the operator's "active project"
// scope and shows a read-only header with the project + configuration instead.
//
// Write compatibility: ConfigurationService.SaveCodeGenerationFields replicates
// the legacy serializer element by element (ProjectConfiguration.Write +
// CSharpCompilerParameters.Write), so both UIs produce value-compatible .csprojs:
//   Optimize            always written (legacy 2-arg SetValue, no default);
//   WarningLevel        "sticky" — written iff already present OR != 4;
//   the rest            default-suppressed (removed when back to the default);
//   DebugSymbols/DebugType the base no-churn state machine.
//
// Faithfulness notes / documented divergences:
//  - The legacy XML filename entry defaults its text to
//    configuration.CompiledOutputName.ChangeExtension(".xml"); the shell has no
//    full DotNetProject model, so the entry shows the stored DocumentationFile
//    (or is empty) and the compiled-name default is not fabricated.
//  - Pango bold section headers and mnemonic underlines have no portable
//    meaning here: headers render bold (FontWeight), labels are plain text.
partial class PreferencesDialog
{
	// Legacy CSharpProject.SupportedPlatforms, in combo order; the display labels
	// map one-to-one onto these tokens.
	static readonly string[] CgPlatformTokens = { "anycpu", "x86", "x64", "itanium" };

	// Legacy DEBUG_* combo indices -> (DebugSymbols, DebugType).
	static (bool Symbols, string Type) CgDebugFor (int i) => i switch {
		0 => (true, "full"),
		1 => (true, "pdbonly"),
		2 => (true, "portable"),
		3 => (true, "embedded"),
		_ => (false, "none"),
	};

	string? cgProjectPath;
	string? cgSolutionPath;
	string cgConfig = "";
	string cgPlatform = "";
	bool cgLoaded;
	string? cgOriginalText;
	bool cgCombosFilled;

	// Legacy combo contents, in order. CgDebug indices map through CgDebugFor.
	static readonly string[] CgDebugLabels = { "Full", "Symbols only", "Portable", "Embedded", "None" };
	static readonly string[] CgPlatformLabels = { "Any CPU", "x86", "x64", "Itanium" };

	void CgFillCombos ()
	{
		if (cgCombosFilled)
			return;
		cgCombosFilled = true;
		foreach (var s in CgDebugLabels)
			CgDebug.Items.Add (s);
		foreach (var s in CgPlatformLabels)
			CgPlatform.Items.Add (s);
	}

	/// <summary>Legacy RemoveDuplicateDefinedSymbols: dedupe the symbol list while
	/// preserving the original separators (so a space-separated .csproj is not
	/// rewritten with ';'). Ported verbatim from CodeGenerationPanelWidget.</summary>
	static string CgDedupeSymbols (string original)
	{
		var whitespaces = new char[] { ';', ',', ' ', '\t' };
		var symbols = new System.Collections.Generic.List<string> ();
		string currentSymbol = "";
		bool symbolStarted = false;
		bool eatWhitespaces = false;
		foreach (var c in original) {
			var isWhitespace = whitespaces.Contains (c);
			if (eatWhitespaces && !isWhitespace) {
				symbols.Add (currentSymbol);
				currentSymbol = "";
				symbolStarted = true;
				eatWhitespaces = false;
			} else if (symbolStarted && isWhitespace) {
				eatWhitespaces = true;
			} else if (!symbolStarted && !isWhitespace) {
				symbolStarted = true;
			}
			currentSymbol += c;
		}
		if (currentSymbol.Length > 0)
			symbols.Add (currentSymbol);

		string result = "";
		var symbolDuplicates = new System.Collections.Generic.HashSet<string> ();
		for (int i = symbols.Count - 1; i >= 0; i--) {
			if (symbolDuplicates.Add (symbols[i].Trim (whitespaces)))
				result = symbols[i] + result;
		}
		return result;
	}

	// Resolve the active project + its active configuration (null-safe: any missing
	// piece -> empty state, mirroring the legacy panel needing a configuration).
	bool CgResolveTarget (out string project, out string solution, out string config, out string platform)
	{
		project = ""; solution = ""; config = ""; platform = "";
		var mw = MainWindow.Instance;
		if (mw is null)
			return false;
		var sln = mw.SolutionPath;
		var proj = mw.ActiveProject;
		if (string.IsNullOrEmpty (sln) || string.IsNullOrEmpty (proj))
			return false;
		if (!File.Exists (proj))
			return false;
		var solutionConfig = ConfigurationService.GetActiveConfiguration (sln);
		var mapping = ConfigurationService.GetProjectConfigurationFor (sln, solutionConfig, proj);
		project = proj;
		solution = sln;
		config = mapping.Configuration;
		platform = mapping.Platform;
		return true;
	}

	void LoadCodeGenerationPanel ()
	{
		cgLoaded = false;
		cgOriginalText = null;
		CgFillCombos ();
		if (CgResolveTarget (out var project, out var solution, out var config, out var platform)) {
			cgProjectPath = project;
			cgSolutionPath = solution;
			cgConfig = config;
			cgPlatform = platform;
			var fields = ConfigurationService.ReadCodeGenerationFields (project, config, platform);
			cgOriginalText = File.Exists (project) ? File.ReadAllText (project) : null;
			if (fields is not null) {
				CgOptimize.IsChecked = fields.Optimize;
				CgOverflowChecks.IsChecked = fields.GenerateOverflowChecks;
				CgTreatAsErrors.IsChecked = fields.TreatWarningsAsErrors;

				var pIdx = Array.IndexOf (CgPlatformTokens, fields.PlatformTarget);
				CgPlatform.SelectedIndex = pIdx >= 0 ? pIdx : 0;

				// Legacy Load combo selection (DebugType is normalized to "none" on read).
				int dIdx;
				if (!fields.DebugSymbols || fields.DebugType.Equals ("none", StringComparison.OrdinalIgnoreCase))
					dIdx = 4;
				else if (fields.DebugType.Equals ("pdbonly", StringComparison.OrdinalIgnoreCase))
					dIdx = 1;
				else if (fields.DebugType.Equals ("portable", StringComparison.OrdinalIgnoreCase))
					dIdx = 2;
				else if (fields.DebugType.Equals ("embedded", StringComparison.OrdinalIgnoreCase))
					dIdx = 3;
				else
					dIdx = 0;
				CgDebug.SelectedIndex = dIdx;

				CgXmlDocs.IsChecked = fields.GenerateXmlDocumentation;
				CgXmlFile.Text = fields.DocumentationFile;
				CgSymbols.Text = CgDedupeSymbols (fields.DefineSymbols);
				CgWarningLevel.Value = fields.WarningLevel;
				CgIgnoreWarnings.Text = fields.NoWarnings;
				cgLoaded = true;
			}
		}
		CgRefreshState ();
	}

	void CgRefreshState ()
	{
		bool empty = !cgLoaded;
		CgBody.IsEnabled = !empty;
		CgProjectHeader.Text = empty
			? "No active project: open a solution to edit the C# compiler options of its active configuration."
			: $"Project: {Path.GetFileName (cgProjectPath!)}  —  Configuration: {cgConfig}|{cgPlatform}";
	}

	// Legacy Store(): read the nine controls into the fields and write them. When
	// the panel was never loaded (no active project) it throws; the port skips and
	// reports instead (same observable result: no file is touched).
	void StoreCodeGenerationPanel ()
	{
		if (!cgLoaded) {
			MainWindow.Instance?.Output ("[prefs-codegeneration] stored: skipped (no active project)");
			return;
		}
		var (symbols, type) = CgDebugFor (CgDebug.SelectedIndex);
		var fields = new ConfigurationService.CSharpCodeGenerationFields (
			CgOptimize.IsChecked == true,
			CgOverflowChecks.IsChecked == true,
			CgPlatformTokens[Math.Clamp (CgPlatform.SelectedIndex, 0, CgPlatformTokens.Length - 1)],
			symbols, type,
			CgXmlDocs.IsChecked == true,
			CgXmlDocs.IsChecked == true ? CgXmlFile.Text : "",
			CgSymbols.Text,
			(int)CgWarningLevel.Value,
			CgIgnoreWarnings.Text,
			CgTreatAsErrors.IsChecked == true);
		var ok = ConfigurationService.SaveCodeGenerationFields (cgProjectPath!, cgConfig, cgPlatform, fields);
		MainWindow.Instance?.Output ("[prefs-codegeneration] stored: " + (ok ? "ok" : "FAIL") + " " + cgConfig + "|" + cgPlatform);
	}

	/// <summary>QA oracle for the Code Generation port (run with --sln=&lt;fixture&gt; so
	/// there is an active project). Exercises the full read -> edit -> write -> re-read
	/// round-trip of the nine fields against the fixture .csproj: defaults on a
	/// group-less file, writing a non-default set (creating the PropertyGroup), the
	/// default-suppression removals, the sticky WarningLevel, the Debug combo state
	/// machine, and the legacy symbol dedupe. Restores the original file afterwards.</summary>
	public void QaCodeGenerationOracle ()
	{
		LoadCodeGenerationPanel ();
		if (!cgLoaded) {
			MainWindow.Instance?.Output ("[prefs-codegeneration] oracle: no active project (pass --sln=<fixture.sln>)");
			return;
		}
		var proj = cgProjectPath!;
		string Cond (string cfg, string plat) => " '$(Configuration)|$(Platform)' == '" + cfg + "|" + (plat == "Any CPU" ? "AnyCPU" : plat) + "' ";
		System.Xml.Linq.XElement? Group ()
		{
			try {
				var doc = System.Xml.Linq.XDocument.Load (proj);
				var cond = Cond (cgConfig, cgPlatform);
				return doc.Root?.Elements ().FirstOrDefault (e => e.Name.LocalName == "PropertyGroup" && (string?)e.Attribute ("Condition") == cond);
			} catch {
				return null;
			}
		}
		string? Val (string name) => Group()?.Elements ().FirstOrDefault (e => e.Name.LocalName == name)?.Value;

		try {
			// 1) Defaults on the group-less fixture: all legacy defaults.
			MainWindow.Instance?.Output ("[prefs-codegeneration] defaults: optimize=" + (CgOptimize.IsChecked == true)
				+ " overflow=" + (CgOverflowChecks.IsChecked == true)
				+ " platform='" + CgPlatformTokens[Math.Clamp (CgPlatform.SelectedIndex, 0, 3)] + "'"
				+ " debugIdx=" + CgDebug.SelectedIndex
				+ " xml=" + (CgXmlDocs.IsChecked == true)
				+ " symbols='" + CgSymbols.Text + "'"
				+ " warn=" + (int)CgWarningLevel.Value
				+ " ignore='" + CgIgnoreWarnings.Text + "'"
				+ " aserrors=" + (CgTreatAsErrors.IsChecked == true));

			// 2) Write a non-default set and re-read from the file (round-trip).
			CgOptimize.IsChecked = true;
			CgOverflowChecks.IsChecked = true;
			CgPlatform.SelectedIndex = 2;          // x64
			CgDebug.SelectedIndex = 2;             // Portable
			CgXmlDocs.IsChecked = true;
			CgXmlFile.Text = "doc.xml";
			CgSymbols.Text = "TRACE;DEBUG;TRACE;";
			CgWarningLevel.Value = 1;
			CgIgnoreWarnings.Text = "1591,1701";
			CgTreatAsErrors.IsChecked = true;
			StoreCodeGenerationPanel ();

			string cond = Cond (cgConfig, cgPlatform);
			var g = Group ();
			MainWindow.Instance?.Output ("[prefs-codegeneration] group: exists=" + (g is not null) + " cond='" + cond + "'");
			MainWindow.Instance?.Output ("[prefs-codegeneration] roundtrip: optimize='" + Val ("Optimize") + "'"
				+ " overflow='" + Val ("CheckForOverflowUnderflow") + "'"
				+ " platform='" + Val ("PlatformTarget") + "'"
				+ " debugSymbols='" + Val ("DebugSymbols") + "' debugType='" + Val ("DebugType") + "'"
				+ " xmlfile='" + Val ("DocumentationFile") + "'"
				+ " constants='" + Val ("DefineConstants") + "'"
				+ " warn='" + Val ("WarningLevel") + "'"
				+ " ignore='" + Val ("NoWarn") + "'"
				+ " aserrors='" + Val ("TreatWarningsAsErrors") + "'");

			// Re-read through the panel path and confirm the values come back.
			var back = ConfigurationService.ReadCodeGenerationFields (proj, cgConfig, cgPlatform);
			MainWindow.Instance?.Output ("[prefs-codegeneration] re-read: opt=" + (back?.Optimize)
				+ " ovf=" + (back?.GenerateOverflowChecks)
				+ " plat=" + (back?.PlatformTarget)
				+ " dbg=" + (back?.DebugType)
				+ " xml=" + (back?.DocumentationFile)
				+ " sym=" + (back?.DefineSymbols)
				+ " warn=" + (back?.WarningLevel)
				+ " noWarn=" + (back?.NoWarnings)
				+ " err=" + (back?.TreatWarningsAsErrors));

			// 3) Back to defaults -> the default-suppressed elements must be removed
			//    (Optimize stays written; WarningLevel was present so stays; others drop).
			CgOptimize.IsChecked = false;
			CgOverflowChecks.IsChecked = false;
			CgPlatform.SelectedIndex = 0;          // anycpu
			CgDebug.SelectedIndex = 4;             // None
			CgXmlDocs.IsChecked = false;
			CgSymbols.Text = "";
			CgWarningLevel.Value = 4;
			CgIgnoreWarnings.Text = "";
			CgTreatAsErrors.IsChecked = false;
			StoreCodeGenerationPanel ();
			MainWindow.Instance?.Output ("[prefs-codegeneration] suppress: optimize='" + Val ("Optimize") + "'"
				+ " overflow='" + (Val ("CheckForOverflowUnderflow") ?? "removed") + "'"
				+ " platform='" + (Val ("PlatformTarget") ?? "removed") + "'"
				+ " xmlfile='" + (Val ("DocumentationFile") ?? "removed") + "'"
				+ " constants='" + (Val ("DefineConstants") ?? "removed") + "'"
				+ " warn='" + (Val ("WarningLevel") ?? "removed") + "'"
				+ " ignore='" + (Val ("NoWarn") ?? "removed") + "'"
				+ " aserrors='" + (Val ("TreatWarningsAsErrors") ?? "removed") + "'"
				+ " debugSymbols='" + (Val ("DebugSymbols") ?? "removed") + "' debugType='" + (Val ("DebugType") ?? "removed") + "'");

			// 4) Sticky WarningLevel: the element is present now, so writing 4 keeps it.
			MainWindow.Instance?.Output ("[prefs-codegeneration] sticky: warningLevel@4 present='" + (Val ("WarningLevel") ?? "removed") + "'");

			// 5) B1 corner: the legacy's own Full->None residue (DebugSymbols=true +
			//    DebugType=none explicit, the state the legacy leaves when the user
			//    moves Full->None on a group that had DebugSymbols=true and no
			//    DebugType). Saving "None" again must leave BOTH elements untouched
			//    (the legacy no-churn gates close) - the port must not remove them.
			// Seed the file directly (the panel is already loaded; the combo is still
			// "None" from step 3), so cgOriginalText stays the pristine file for the
			// finally-restore.
			try {
				var doc = System.Xml.Linq.XDocument.Load (proj);
				var g2 = doc.Root!.Elements ().First (e => e.Name.LocalName == "PropertyGroup" && (string)e.Attribute ("Condition") == cond);
				void SetEl (System.Xml.Linq.XElement g, string n, string v) {
					var el = g.Elements ().FirstOrDefault (x => x.Name.LocalName == n);
					if (el is null) g.Add (new System.Xml.Linq.XElement (g.Name.Namespace + n, v));
					else el.SetValue (v);
				}
				SetEl (g2, "DebugSymbols", "true");
				SetEl (g2, "DebugType", "none");
				doc.Save (proj);
			} catch { /* oracle precondition; the check below reports the state */ }
			MainWindow.Instance?.Output ("[prefs-codegeneration] b1: seeded ds='" + (Val ("DebugSymbols") ?? "removed") + "' dt='" + (Val ("DebugType") ?? "removed") + "' comboIdx=" + CgDebug.SelectedIndex);
			StoreCodeGenerationPanel ();
			MainWindow.Instance?.Output ("[prefs-codegeneration] b1: after-none-store ds='" + (Val ("DebugSymbols") ?? "removed") + "' dt='" + (Val ("DebugType") ?? "removed") + "'");
		} finally {
			if (cgOriginalText is not null)
				File.WriteAllText (proj, cgOriginalText, new UTF8Encoding (false));
			MainWindow.Instance?.Output ("[prefs-codegeneration] restored original " + Path.GetFileName (proj));
		}
	}
}
