using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

public partial class NewSolutionDialog : Window
{
	// Legacy catalog (NewProjectDialog / dotnet templates core set). Titles mirror
	// the templates offered by the GTK dialog for the C# category. Icon32 is the
	// resource base name of the GTK template icon
	// (main/src/core/MonoDevelop.Ide/templates/images/*.png), the same PNG the
	// GtkNewProjectDialogBackend renders via ImageService.GetIcon(template.IconId, Dnd):
	// Console->md-console-project, Library->md-library-project,
	// UnitTest->md-test-project, Shared->md-crossplatform-shared-project.
	static readonly (string Tag, string Title, string Desc, string Icon32) [] CsTemplates = {
		("console", "Console Application",
			"A project for creating a command-line application that can run on .NET on Windows, Linux and MacOS.",
			"console-project"),
		("library", "Class Library",
			"A project for creating a class library that targets .NET Standard or .NET Core.",
			"library-project"),
		("unittest", "Unit Test Project",
			"A project that contains unit tests that can run with NUnit / xUnit test runners.",
			"test-project"),
		("shared", "Shared Project",
			"A project for sharing source files between multiple projects via implicit linking.",
			"crossplatform-shared-project"),
	};

	// F# category: the four general-purpose templates of the GTK binding
	// (main/external/fsharpbinding/MonoDevelop.FSharpBinding/Templates — the
	// Portable/SharedAssets/Gtk ones were conditionally hidden on non-MSBuild
	// targets, as in the legacy manifest). Titles and descriptions mirror the
	// legacy _Name/_Description fields; icons are the same md-* resource base
	// names the GTK dialog rendered. "fsharp-tutorial" has no dotnet CLI
	// template: it scaffolds the ported Tutorial.fs (40 KB, ported with the
	// minimal F# 10 fixes — see port-fsharp-avalonia.md).
	static readonly (string Tag, string Title, string Desc, string Icon32) [] FSharpTemplates = {
		("fsharp-console", "Console Project",
			"A F# project for creating a command line application.",
			"console-project"),
		("fsharp-library", "Library",
			"An F# project for creating an F# library.",
			"library-project"),
		("fsharp-nunit", "NUnit Library Project",
			"Creates an F# NUnit library.",
			"test-project"),
		("fsharp-tutorial", "F# Tutorial",
			"An F# tutorial project providing a walkthrough of the F# language.",
			"console-project"),
	};

	/// <summary>The F# project type GUID .sln files store in the Project()
	/// entry — the same value dotnet sln writes when adding an .fsproj
	/// (verified against dotnet new sln --format sln + dotnet sln add).
	/// Brace-less, like the other Project() type literals of this file: the
	/// interpolated line adds the braces.</summary>
	const string FSharpProjectTypeGuid = "F2A71F9B-5D33-465A-A702-920D77279786";

	public string? SelectedTemplateId { get; private set; }

	/// <summary>QA hook: --newproject uses a throwaway temp location.</summary>
	string? forcedLocation;

	/// <summary>When true (solution already open) the created project is added to
	/// it (legacy AddSolutionItem flow) instead of opening a new solution.</summary>
	public bool AddToOpenSolution {
		get => AddToSolutionCheck?.IsChecked == true;
		set {
			if (AddToSolutionCheck is not null) {
				AddToSolutionCheck.IsChecked = value;
				AddToSolutionCheck.IsEnabled = value;
			}
		}
	}
	public string? SolutionNameValue => SolutionName?.Text;
	public string? LocationValue => LocationBox?.Text;

	public NewSolutionDialog () : this (null) { }

	// QA hook: --newsolution=<templateId> preselects a template.
	public NewSolutionDialog (string? preselectTemplate) : this (preselectTemplate, null) { }

	// QA hook: --newproject=<template> creates in a temp dir (non-destructive).
	public NewSolutionDialog (string? preselectTemplate, string? forcedLocation)
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);
		// The template list is per category: seed the one holding the
		// preselected template (QA hook --newproject=fs* / --newsolution=fs*),
		// defaulting to C# like the GTK dialog does.
		var category = FSharpTemplates.Any (t => t.Tag == preselectTemplate) ? "F#" : "C#";
		SetCategory (category);

		this.forcedLocation = forcedLocation;
		var idx = Array.FindIndex (CurrentTemplates, t => t.Tag == preselectTemplate);
		TemplateList.SelectedIndex = idx >= 0 ? idx : 0;
		LocationBox.Text = forcedLocation ?? Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
		// "Add to solution" only applies when a solution is actually open (the GTK
		// dialog offers that target in that context); otherwise the option is inert.
		if (AddToSolutionCheck is not null) {
			AddToSolutionCheck.IsChecked = forcedLocation is not null;
			AddToSolutionCheck.IsEnabled = forcedLocation is not null;
		}
	}

	void OnCategorySelected (object? sender, SelectionChangedEventArgs e)
	{
		// Reentrancy is handled by SetCategory's builtCategory idempotence:
		// a programmatic CategoryList.SelectedIndex change raises this event
		// again, but SetCategory then no-ops.
		if (CategoryList?.SelectedItem is not ListBoxItem item)
			return;
		SetCategory (item.Content as string ?? "C#");
	}

	(string Tag, string Title, string Desc, string Icon32) [] CurrentTemplates
		=> CategoryList?.SelectedIndex == 1 ? FSharpTemplates : CsTemplates;

	// Rebuild the template list for the category (the GTK dialog swaps the DND
	// list contents when the category column changes). The constructor seeds the
	// initial category; afterwards the SelectionChanged event drives it — the
	// builtCategory guard collapses the double fire (programmatic selection
	// change + its event) into a single rebuild.
	string? builtCategory;

	void SetCategory (string category)
	{
		if (CategoryList is null || TemplateList is null)
			return;
		if (builtCategory == category)
			return;
		builtCategory = category;
		CategoryList.SelectedIndex = category == "F#" ? 1 : 0;
		TemplateList.Items.Clear ();
		foreach (var t in CurrentTemplates) {
			var li = new ListBoxItem { Tag = t.Tag };
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
			var img = new Image { Width = 32, Height = 32, Source = IconService.GetTemplateImage (t.Icon32) };
			panel.Children.Add (img);
			panel.Children.Add (new TextBlock { Text = t.Title, VerticalAlignment = VerticalAlignment.Center });
			li.Content = panel;
			TemplateList.Items.Add (li);
		}
		if (TemplateList.Items.Count > 0)
			TemplateList.SelectedIndex = 0;
	}

	void OnTemplateSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (TemplateList.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
			return;
		var t = CurrentTemplates.FirstOrDefault (x => x.Tag == tag);
		TemplateTitle.Text = t.Title;
		TemplateDescription.Text = t.Desc;
		SelectedTemplateId = t.Tag;
	}

	// El legacy FolderEntry tiene un botón "..." que abre el selector de carpeta
	// (mismo patrón que NewFileDialog.OnBrowse).
	async void OnBrowseLocation (object? sender, RoutedEventArgs e)
	{
		try {
			var folders = await StorageProvider.OpenFolderPickerAsync (new Avalonia.Platform.Storage.FolderPickerOpenOptions {
				Title = "Select a location for the new solution",
				AllowMultiple = false,
			});
			if (folders.Count > 0)
				LocationBox.Text = folders [0].Path.LocalPath;
		} catch (Exception ex) {
			Console.WriteLine ("[newsolution] folder picker: " + ex.Message);
		}
	}

	protected override void OnOpened (EventArgs e)
	{
		base.OnOpened (e);
		if (AutoCreateForQa)
			OnCreate (this, new RoutedEventArgs ());
	}

	void OnCreate (object? sender, RoutedEventArgs e)
	{
		var template = SelectedTemplateId ?? "console";
		var name = string.IsNullOrWhiteSpace (SolutionName?.Text) ? "Solution" : SolutionName.Text!.Trim ();
		var location = string.IsNullOrWhiteSpace (LocationBox?.Text)
			? Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)
			: LocationBox.Text!.Trim ();

		// Map the dialog's legacy template ids to dotnet CLI short names.
		// "shared" has no CLI equivalent: scaffold the minimal .shproj/.projitems
		// pair directly (legacy SharedAssetsProject format). The F# tags map to
		// the same CLI templates with -lang F# (producing .fsproj); the tutorial
		// has no CLI equivalent and scaffolds the ported Tutorial.fs directly.
		string cliTemplate = template switch {
			"console" => "console",
			"library" => "classlib",
			"unittest" => "nunit",
			"fsharp-console" => "console",
			"fsharp-library" => "classlib",
			"fsharp-nunit" => "nunit",
			_ => "",
		};
		var isFSharp = template.StartsWith ("fsharp-", StringComparison.Ordinal);

		try {
			var dir = CreateDirectoryCheck?.IsChecked == true ? Path.Combine (location, name) : location;
			Directory.CreateDirectory (dir);
			var slnPath = Path.Combine (dir, name + ".sln");
			var projDir = Path.Combine (dir, name);

			if (isFSharp && template == "fsharp-tutorial") {
				ScaffoldFSharpTutorial (projDir, name);
			} else if (cliTemplate != "") {
				// dotnet CLI scaffolding (same tool the legacy templates wrap) run hidden.
				// F# tags pass -lang F# so the CLI emits .fsproj projects.
				var langArg = isFSharp ? " -lang F#" : "";
				var psi = new System.Diagnostics.ProcessStartInfo {
					FileName = "dotnet",
					Arguments = $"new {cliTemplate}{langArg} -n {name} -o \"{projDir}\"",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using var p = System.Diagnostics.Process.Start (psi);
				if (p is not null) {
					var outTask = p.StandardOutput.ReadToEndAsync ();
					var errTask = p.StandardError.ReadToEndAsync ();
					p.WaitForExit (60000);
					var firstErr = (errTask.GetAwaiter ().GetResult () + "\n")
						.Split ('\n').FirstOrDefault (l => !string.IsNullOrWhiteSpace (l));
					Console.WriteLine ("[newsolution] dotnet exit=" + p.ExitCode + (string.IsNullOrWhiteSpace (firstErr) ? "" : " err=" + firstErr.Trim ()));
					_ = outTask;
				} else {
					Console.WriteLine ("[newsolution] dotnet CLI not available");
				}
			} else {
				Directory.CreateDirectory (projDir);
				var shGuid = Guid.NewGuid ().ToString ().ToUpperInvariant ();
				File.WriteAllText (Path.Combine (projDir, name + ".shproj"),
					"<Project ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n" +
					"  <PropertyGroup>\n" +
					"    <SchemaVersion>2.0</SchemaVersion>\n" +
					$"    <ProjectGuid>{{{shGuid}}}</ProjectGuid>\n" +
					"  </PropertyGroup>\n" +
					"  <Import Project=\"$(MSBuildExtensionsPath32)\\Microsoft\\VisualStudio\\v$(VisualStudioVersion)\\CodeSharing\\Microsoft.CodeSharing.Common.props\" />\n" +
					$"  <Import Project=\"{name}.projitems\" Label=\"Shared\" />\n" +
					"  <Import Project=\"$(MSBuildExtensionsPath32)\\Microsoft\\VisualStudio\\v$(VisualStudioVersion)\\CodeSharing\\Microsoft.CodeSharing.CSharp.targets\" />\n" +
					"</Project>\n");
				File.WriteAllText (Path.Combine (projDir, name + ".projitems"),
					"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
					"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n" +
					"  <PropertyGroup>\n" +
					"    <HasSharedItems>true</HasSharedItems>\n" +
					"  </PropertyGroup>\n" +
					"  <ItemGroup>\n" +
					"    <Compile Include=\"$(MSBuildThisFileDirectory)Class1.cs\" />\n" +
					"  </ItemGroup>\n" +
					"</Project>\n");
				File.WriteAllText (Path.Combine (projDir, "Class1.cs"),
					"namespace " + name + "\n{\n\tpublic class Class1\n\t{\n\t}\n}\n");
			}

			// Add-to-solution mode: only the project is created; MainWindow wires it
			// into the loaded .sln (legacy ProjectOperations.AddSolutionItem).
			// F# projects produce .fsproj (dotnet new -lang F#) — detect both.
			var createdProj = Directory.Exists (projDir)
				? Directory.GetFiles (projDir, "*.csproj").FirstOrDefault () ?? Directory.GetFiles (projDir, "*.fsproj").FirstOrDefault () ?? Directory.GetFiles (projDir, "*.shproj").FirstOrDefault ()
				: null;
			if (AddToOpenSolution) {
				CreatedProjectPath = createdProj;
				CreatedSolutionPath = null;
				Close ();
				return;
			}

			// Fallback: minimal valid .sln if dotnet CLI is unavailable/failed.
			// Project type GUID per project kind: C# FAE04EC0..., F# the
			// F2A71F9B... GUID (what dotnet sln writes for .fsproj), shared
			// D954291E.... Brace-less here: the interpolated Project() line
			// adds them via {{{projType}}} (the braced constant lives in
			// ConfigurationService for the SlnFile writer).
			if (!File.Exists (slnPath)) {
				var guid = Guid.NewGuid ().ToString ().ToUpperInvariant ();
				var projFile = createdProj is null ? name + (isFSharp ? ".fsproj" : ".csproj") : Path.GetFileName (createdProj);
				var projType = projFile.EndsWith (".shproj", StringComparison.Ordinal)
					? "D954291E-2A0B-460D-934E-DC6B0785DB48"
					: projFile.EndsWith (".fsproj", StringComparison.Ordinal)
					? "F2A71F9B-5D33-465A-A702-920D77279786"
					: "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
				File.WriteAllText (slnPath,
					"Microsoft Visual Studio Solution File, Format Version 12.00\n" +
					"# Visual Studio Version 17\n" +
					$"Project(\"{{{projType}}}\") = \"{name}\", \"{name}\\{projFile}\", \"{{{guid}}}\"\n" +
					"EndProject\n" +
					"Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|Any CPU = Release|Any CPU\n\tEndGlobalSection\n" +
					"Global\nEndGlobal\n");
			}

			CreatedSolutionPath = File.Exists (slnPath) ? slnPath : null;
			Close ();
		} catch (Exception ex) {
			Console.WriteLine ("[newsolution] create failed: " + ex.Message);
			CreatedSolutionPath = null;
			Close ();
		}
	}

	public string? CreatedSolutionPath { get; private set; }

	/// <summary>Path of the .csproj created when the dialog ran in "add to open
	/// solution" mode (MainWindow wires it into the loaded .sln).</summary>
	public string? CreatedProjectPath { get; private set; }

	/// <summary>QA auto-create: fires OnCreate as soon as the dialog opens
	/// (deterministic --newproject without synthetic input).</summary>
	public bool AutoCreateForQa { get; set; }

	// "F# Tutorial" scaffold: the legacy template (FSharpTutorialProject.xpt)
	// is a console project around the 976-line Tutorial.fs walkthrough. The
	// ported source lives in this project (Assets/Tutorial.fs — the legacy
	// file with the minimal F# 10 fixes, see port-fsharp-avalonia.md) and is
	// copied to the new project; a minimal Program.fs stands in when the asset
	// cannot be found (running from a tree without the source).
	void ScaffoldFSharpTutorial (string projDir, string name)
	{
		Directory.CreateDirectory (projDir);
		var fsproj =
			"<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
			"  <PropertyGroup>\n" +
			"    <OutputType>Exe</OutputType>\n" +
			"    <TargetFramework>net10.0</TargetFramework>\n" +
			"  </PropertyGroup>\n" +
			"  <ItemGroup>\n" +
			"    <Compile Include=\"Tutorial.fs\" />\n" +
			"  </ItemGroup>\n" +
			"</Project>\n";
		File.WriteAllText (Path.Combine (projDir, name + ".fsproj"), fsproj);
		var asset = FindTutorialAsset ();
		if (asset is not null) {
			File.Copy (asset, Path.Combine (projDir, "Tutorial.fs"), overwrite: true);
		} else {
			Console.WriteLine ("[newsolution] Tutorial.fs asset not found — scaffolding fallback Tutorial.fs");
			File.WriteAllText (Path.Combine (projDir, "Tutorial.fs"),
				"// F# Tutorial (fallback source — the full walkthrough asset was not found; see port-fsharp-avalonia.md)\n" +
				$"printfn \"Hello from the F# Tutorial ({name})\"\n");
		}
	}

	static string? FindTutorialAsset ()
	{
		// 1) Repo tree (dev runs): src/core/MonoDevelop.Startup.Avalonia/Assets.
		// 2) Output dir (main/build): the csproj Content entry stages it there.
		// 3) Walk up from AppContext.BaseDirectory like IconService.FindIconsDir.
		try {
			var cwd = Environment.CurrentDirectory;
			var c1 = Path.Combine (cwd, "src", "core", "MonoDevelop.Startup.Avalonia", "Assets", "Tutorial.fs");
			if (File.Exists (c1))
				return c1;
			var baseDir = AppContext.BaseDirectory;
			var c2 = Path.Combine (baseDir, "Assets", "Tutorial.fs");
			if (File.Exists (c2))
				return c2;
			for (int i = 0; i < 8 && baseDir is not null; i++) {
				var c = Path.Combine (baseDir, "src", "core", "MonoDevelop.Startup.Avalonia", "Assets", "Tutorial.fs");
				if (File.Exists (c))
					return c;
				baseDir = Path.GetDirectoryName (baseDir);
			}
		} catch { }
		return null;
	}

	void OnCancel (object? sender, RoutedEventArgs e)
	{
		CreatedSolutionPath = null;
		Close ();
	}
}
