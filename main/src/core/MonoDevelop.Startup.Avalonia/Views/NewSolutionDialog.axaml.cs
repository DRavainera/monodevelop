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
	// Category labels, in the CategoryList order (the XAML items must match
	// this order; the dialog is built in C# for both the GTK and Avalonia UIs).
	static readonly string [] Categories = { "C#", "F#", "VB", "ASP.NET", "ASP.NET Core" };

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

	// VB category: the two templates the VBNetBinding manifest registers
	// (main/src/addins/VBNetBinding/VBNetBinding.addin.xml, ProjectTemplates —
	// the legacy binding has no wizard, selection goes straight to the final
	// configuration page). Titles/descriptions mirror the xpt _Name/_Description.
	// "vb-console" maps to dotnet new console -lang VB (verified to emit an
	// SDK-style .vbproj); "vb-empty" has no CLI equivalent and scaffolds a
	// minimal .vbproj (the legacy EmptyVBProject xpt is a zero-file project).
	static readonly (string Tag, string Title, string Desc, string Icon32) [] VbTemplates = {
		("vb-console", "Console Project",
			"Creates a console VB.Net project.",
			"console-project"),
		("vb-empty", "Empty Project",
			"Creates an empty VB.Net solution.",
			"generic-project"),
	};

	// ASP.NET (classic, .NET Framework 4.x) category: the three templates the
	// AspNet addin registers (main/src/addins/AspNet/Properties/
	// MonoDevelop.AspNet.addin.xml, ProjectTemplates). The legacy wizard is a
	// single checkbox page (IncludeTestProject + MVC/WebForms/WebApi); the port
	// collapses it into the per-template entries (documented divergence). None
	// has a dotnet CLI equivalent: the classic .csproj (ProjectTypeGuids with
	// the AspNetApp flavor 349C5851 + C# type FAE04EC0, TFM 4.5, System.Web
	// references, Global.asax/Web.config) is scaffolded by hand, mirroring the
	// xpt files. These projects do not build on .NET 10/Linux — their value is
	// file generation + the AspNet XSP options panel (GetProjectKind classifies
	// them IsAspNetApp=true).
	static readonly (string Tag, string Title, string Desc, string Icon32) [] AspNetTemplates = {
		("aspnet-empty", "Empty ASP.NET Project",
			"Creates an empty ASP.NET Web Project.",
			"gui-project"),
		("aspnet-mvc", "ASP.NET MVC Project",
			"Creates an ASP.NET MVC Web Project with Razor Views.",
			"gui-project"),
		("aspnet-webforms", "ASP.NET Web Forms Project",
			"Creates a new ASP.NET Web Forms Project.",
			"gui-project"),
	};

	// ASP.NET Core category: the templates the legacy AspNetCore addin offers
	// via the SDK 3.1 template nupkg (web/mvc/webapi, plus a Blazor one). The
	// port uses the local SDK's equivalent dotnet new templates (documented
	// divergence — same template family, current SDK). The legacy wizard's
	// TFM/authentication page is not ported: the CLI defaults apply.
	static readonly (string Tag, string Title, string Desc, string Icon32) [] AspNetCoreTemplates = {
		("core-web", "ASP.NET Core Empty",
			"A project for creating an empty ASP.NET Core web application.",
			"gui-project"),
		("core-mvc", "ASP.NET Core MVC",
			"A project for creating an ASP.NET Core MVC application.",
			"gui-project"),
		("core-api", "ASP.NET Core Web API",
			"A project for creating an ASP.NET Core web API service.",
			"gui-project"),
	};

	/// <summary>The F# project type GUID .sln files store in the Project()
	/// entry — the same value dotnet sln writes when adding an .fsproj
	/// (verified against dotnet new sln --format sln + dotnet sln add).
	/// Brace-less, like the other Project() type literals of this file: the
	/// interpolated line adds the braces.</summary>
	const string FSharpProjectTypeGuid = "F2A71F9B-5D33-465A-A702-920D77279786";

	/// <summary>The VB project type GUID .sln files store for a .vbproj — the
	/// same value dotnet sln writes when adding a .vbproj (verified against
	/// dotnet new sln --format sln + dotnet sln add, matching the
	/// ConfigurationService VbProjectTypeGuid constant).</summary>
	const string VbProjectTypeGuid = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";

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
		MonoDevelop.Ide.Controls.DialogWindow.Apply (this);
		// The template list is per category: seed the one holding the
		// preselected template (QA hooks --newproject=vb* / --newsolution=fs* /
		// aspnet* / core*), defaulting to C# like the GTK dialog does.
		var category = FindCategoryFor (preselectTemplate) ?? "C#";
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

	(string Tag, string Title, string Desc, string Icon32) [] CurrentTemplates => category switch {
		"F#" => FSharpTemplates,
		"VB" => VbTemplates,
		"ASP.NET" => AspNetTemplates,
		"ASP.NET Core" => AspNetCoreTemplates,
		_ => CsTemplates,
	};

	// Category (by label, matching the CategoryList items) the preselected
	// template belongs to — null when it belongs to none (the constructor then
	// defaults to C#).
	static string? FindCategoryFor (string? tag)
	{
		if (string.IsNullOrEmpty (tag))
			return null;
		return FSharpTemplates.Any (t => t.Tag == tag) ? "F#"
			: VbTemplates.Any (t => t.Tag == tag) ? "VB"
			: AspNetTemplates.Any (t => t.Tag == tag) ? "ASP.NET"
			: AspNetCoreTemplates.Any (t => t.Tag == tag) ? "ASP.NET Core"
			: null;
	}

	// Rebuild the template list for the category (the GTK dialog swaps the DND
	// list contents when the category column changes). The constructor seeds the
	// initial category; afterwards the SelectionChanged event drives it — the
	// builtCategory guard collapses the double fire (programmatic selection
	// change + its event) into a single rebuild.
	string? builtCategory;
	string category = "C#";

	void SetCategory (string category)
	{
		if (CategoryList is null || TemplateList is null)
			return;
		if (builtCategory == category)
			return;
		builtCategory = category;
		this.category = category;
		var catIdx = Categories.IndexOf (category);
		CategoryList.SelectedIndex = catIdx >= 0 ? catIdx : 0;
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
		Console.WriteLine ("[newsolution] OnCreate template=" + template + " selectedId=" + SelectedTemplateId);
		var name = string.IsNullOrWhiteSpace (SolutionName?.Text) ? "Solution" : SolutionName.Text!.Trim ();
		var location = string.IsNullOrWhiteSpace (LocationBox?.Text)
			? Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)
			: LocationBox.Text!.Trim ();

		// Map the dialog's legacy template ids to dotnet CLI short names.
		// "shared" has no CLI equivalent: scaffold the minimal .shproj/.projitems
		// pair directly (legacy SharedAssetsProject format). The F# tags map to
		// the same CLI templates with -lang F# (producing .fsproj); the tutorial
		// has no CLI equivalent and scaffolds the ported Tutorial.fs directly.
		// The VB console maps to dotnet new console -lang VB (an SDK-style
		// .vbproj); vb-empty and the classic ASP.NET templates have no CLI
		// equivalent and are scaffolded by hand (the classic .csproj mirrors
		// the xpt files, .NET Framework 4.x). The core-* tags map to the local
		// SDK's web/mvc/webapi templates (the legacy ones come from the SDK
		// 3.1 nupkg — same family, documented divergence).
		string cliTemplate = template switch {
			"console" => "console",
			"library" => "classlib",
			"unittest" => "nunit",
			"fsharp-console" => "console",
			"fsharp-library" => "classlib",
			"fsharp-nunit" => "nunit",
			"vb-console" => "console",
			"core-web" => "web",
			"core-mvc" => "mvc",
			"core-api" => "webapi",
			_ => "",
		};
		var isFSharp = template.StartsWith ("fsharp-", StringComparison.Ordinal);
		var isVb = template.StartsWith ("vb-", StringComparison.Ordinal);

		try {
			var dir = CreateDirectoryCheck?.IsChecked == true ? Path.Combine (location, name) : location;
			Directory.CreateDirectory (dir);
			var slnPath = Path.Combine (dir, name + ".sln");
			var projDir = Path.Combine (dir, name);

			if (isFSharp && template == "fsharp-tutorial") {
				ScaffoldFSharpTutorial (projDir, name);
			} else if (cliTemplate != "") {
				// dotnet CLI scaffolding (same tool the legacy templates wrap) run hidden.
				// F# tags pass -lang F# (the CLI emits .fsproj); VB console passes
				// -lang VB (the CLI emits an SDK-style .vbproj); core-* pass none
				// (C# web templates).
				var langArg = isFSharp ? " -lang F#" : (isVb ? " -lang VB" : "");
				var argsStr = $"new {cliTemplate}{langArg} -n {name} -o \"{projDir}\"";
				var psi = new System.Diagnostics.ProcessStartInfo {
					FileName = "dotnet",
					Arguments = argsStr,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				Console.WriteLine ("[newsolution] cli args: " + argsStr);
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
			} else if (template == "vb-empty") {
				ScaffoldVbEmpty (projDir, name);
			} else if (template == "aspnet-empty") {
				ScaffoldAspNetClassic (projDir, name, kind: "empty");
			} else if (template == "aspnet-mvc") {
				ScaffoldAspNetClassic (projDir, name, kind: "mvc");
			} else if (template == "aspnet-webforms") {
				ScaffoldAspNetClassic (projDir, name, kind: "webforms");
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
			// F# projects produce .fsproj (dotnet new -lang F#) and VB projects
			// .vbproj (dotnet new -lang VB) — detect all of them.
			var createdProj = Directory.Exists (projDir)
				? Directory.GetFiles (projDir, "*.csproj").FirstOrDefault () ?? Directory.GetFiles (projDir, "*.fsproj").FirstOrDefault () ?? Directory.GetFiles (projDir, "*.vbproj").FirstOrDefault () ?? Directory.GetFiles (projDir, "*.shproj").FirstOrDefault ()
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
				var projFile = createdProj is not null
					? Path.GetFileName (createdProj)
					: name + (isFSharp ? ".fsproj" : (isVb ? ".vbproj" : ".csproj"));
				var projType = projFile.EndsWith (".shproj", StringComparison.Ordinal)
					? "D954291E-2A0B-460D-934E-DC6B0785DB48"
					: projFile.EndsWith (".fsproj", StringComparison.Ordinal)
					? "F2A71F9B-5D33-465A-A702-920D77279786"
					: projFile.EndsWith (".vbproj", StringComparison.Ordinal)
					? VbProjectTypeGuid
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

	// "vb-empty" scaffold: the legacy EmptyVBProject.xpt is a zero-file VB project.
	// The port emits the classic .vbproj the shell's VB options dialog (P1) reads
	// and writes — the same shape as the qa_p1/QaVb.vbproj fixture (a top-level
	// PropertyGroup with the option flags, per-configuration PropertyGroups using
	// the canonical combined condition, and the Imports ItemGroup) — of type
	// Library with no Compile items (an empty project). It does not build on
	// .NET 10/Linux (no mono VB tooling); its value is the .vbproj + the VB
	// options panels (GetProjectKind classifies it IsVBNet=true).
	void ScaffoldVbEmpty (string projDir, string name)
	{
		Directory.CreateDirectory (projDir);
		var guid = Guid.NewGuid ().ToString ().ToUpperInvariant ();
		const string dbgCond = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
		const string relCond = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
		File.WriteAllText (Path.Combine (projDir, name + ".vbproj"),
			"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
			"<Project DefaultTargets=\"Build\" ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n" +
			"  <PropertyGroup>\n" +
			"    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>\n" +
			"    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>\n" +
			$"    <ProjectGuid>{{{guid}}}</ProjectGuid>\n" +
			"    <OutputType>Library</OutputType>\n" +
			$"    <RootNamespace>{name}</RootNamespace>\n" +
			$"    <AssemblyName>{name}</AssemblyName>\n" +
			"    <MyType>Custom</MyType>\n" +
			"    <OptionExplicit>On</OptionExplicit>\n" +
			"    <OptionCompare>Binary</OptionCompare>\n" +
			"    <OptionStrict>Off</OptionStrict>\n" +
			"    <OptionInfer>Off</OptionInfer>\n" +
			"  </PropertyGroup>\n" +
			$"  <PropertyGroup Condition=\"{dbgCond}\">\n" +
			"    <DebugSymbols>true</DebugSymbols>\n" +
			"    <DefineDebug>true</DefineDebug>\n" +
			"    <DefineTrace>true</DefineTrace>\n" +
			"    <DebugType>full</DebugType>\n" +
			"    <PlatformTarget>anycpu</PlatformTarget>\n" +
			"  </PropertyGroup>\n" +
			$"  <PropertyGroup Condition=\"{relCond}\">\n" +
			"    <DebugType>none</DebugType>\n" +
			"    <Optimize>true</Optimize>\n" +
			"    <DefineDebug>false</DefineDebug>\n" +
			"    <DefineTrace>true</DefineTrace>\n" +
			"    <PlatformTarget>anycpu</PlatformTarget>\n" +
			"  </PropertyGroup>\n" +
			"  <ItemGroup>\n" +
			"    <Import Include=\"System\" />\n" +
			"    <Import Include=\"System.Collections.Generic\" />\n" +
			"  </ItemGroup>\n" +
			"</Project>\n");
	}

	// Classic (non-SDK) .csproj shared by the three ASP.NET (classic, .NET
	// Framework 4.x) templates. ProjectTypeGuids carries the AspNetApp flavor
	// (349C5851) plus the C# type (FAE04EC0) — GetProjectKind then classifies it
	// IsAspNetApp=true / IsAspNetCore=false (no Sdk attribute), so the dialog
	// shows the XSP options tab. The per-configuration PropertyGroups use the
	// canonical combined condition the shell's configuration model reads. These
	// projects do not build on .NET 10/Linux (they need mono System.Web + XSP);
	// their value is file generation + the AspNet XSP options panel.
	static void ScaffoldAspNetClassic (string projDir, string name, string kind)
	{
		Directory.CreateDirectory (projDir);
		var guid = Guid.NewGuid ().ToString ().ToUpperInvariant ();
		const string dbgCond = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
		const string relCond = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";

		// The classic .csproj (ToolsVersion 4.0, no Sdk) mirrors the legacy xpt:
		// AspNetApp flavor + C# type GUIDs, TFM v4.5, System.Web references.
		var proj =
			"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
			"<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n" +
			"  <PropertyGroup>\n" +
			"    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>\n" +
			"    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>\n" +
			"    <SchemaVersion>2.0</SchemaVersion>\n" +
			$"    <ProjectGuid>{{{guid}}}</ProjectGuid>\n" +
			"    <ProjectTypeGuids>{349C5851-65DF-11DA-9384-00065B846F21};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>\n" +
			"    <OutputType>Library</OutputType>\n" +
			$"    <RootNamespace>{name}</RootNamespace>\n" +
			$"    <AssemblyName>{name}</AssemblyName>\n" +
			"    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>\n" +
			"    <BaseAddress>983520</BaseAddress>\n" +
			"  </PropertyGroup>\n" +
			$"  <PropertyGroup Condition=\"{dbgCond}\">\n" +
			"    <DebugSymbols>true</DebugSymbols>\n" +
			"    <DebugType>full</DebugType>\n" +
			"    <Optimize>false</Optimize>\n" +
			"    <OutputPath>bin\\Debug\\</OutputPath>\n" +
			"    <DefineConstants>DEBUG;TRACE</DefineConstants>\n" +
			"    <ErrorReport>prompt</ErrorReport>\n" +
			"    <WarningLevel>4</WarningLevel>\n" +
			"  </PropertyGroup>\n" +
			$"  <PropertyGroup Condition=\"{relCond}\">\n" +
			"    <DebugType>pdbonly</DebugType>\n" +
			"    <Optimize>true</Optimize>\n" +
			"    <OutputPath>bin\\Release\\</OutputPath>\n" +
			"    <DefineConstants>TRACE</DefineConstants>\n" +
			"    <ErrorReport>prompt</ErrorReport>\n" +
			"    <WarningLevel>4</WarningLevel>\n" +
			"  </PropertyGroup>\n" +
			"  <ItemGroup>\n" +
			"    <Reference Include=\"System\" />\n" +
			"    <Reference Include=\"System.Web\" />\n" +
			"    <Reference Include=\"System.Web.Extensions\" />\n" +
			"    <Reference Include=\"System.Web.Abstractions\" />\n" +
			"    <Reference Include=\"System.Web.Routing\" />\n" +
			"    <Reference Include=\"System.ComponentModel.DataAnnotations\" />\n" +
			"    <Reference Include=\"System.Xml\" />\n" +
			"    <Reference Include=\"System.Xml.Linq\" />\n" +
			"    <Reference Include=\"System.Core\" />\n" +
			"    <Reference Include=\"Microsoft.CSharp\" />\n" +
			"  </ItemGroup>\n";

		// Web.config (WebConfig-Application.xft) + Global.asax (GlobalAsax.xft)
		// are common to every classic web project.
		var webConfig =
			"<?xml version=\"1.0\"?>\n" +
			"<!-- Web.config file for " + name + " (classic ASP.NET, .NET Framework 4.x). -->\n" +
			"<configuration>\n" +
			"  <system.web>\n" +
			"    <compilation debug=\"true\" targetFramework=\"4.5\" />\n" +
			"    <httpRuntime targetFramework=\"4.5\" />\n" +
			"  </system.web>\n" +
			"</configuration>\n";
		var globalAsax =
			("<%@ Application Inherits=\"" + name + ".Global\" %>\n");

		switch (kind) {
		case "mvc":
			ScaffoldAspNetMvcFiles (projDir, name);
			break;
		case "webforms":
			ScaffoldAspNetWebFormsFiles (projDir, name);
			break;
		}

		// Write the per-kind source files (the .csproj Compile list is derived
		// from what lands on disk), then the common web files, then close the
		// project with a Compile item per .cs under the dir (Global.asax.cs,
		// the controllers/pages, ...).
		File.WriteAllText (Path.Combine (projDir, "Global.asax.cs"),
			"namespace " + name + "\n{\n\tpublic class Global : System.Web.HttpApplication\n\t{\n\t}\n}\n");
		File.WriteAllText (Path.Combine (projDir, "Web.config"), webConfig);
		File.WriteAllText (Path.Combine (projDir, "Global.asax"), globalAsax);

		var compileItems = CollectContentItems (projDir);
		var csproj = proj +
			"  <ItemGroup>\n" +
			compileItems +
			"  </ItemGroup>\n" +
			"  <Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />\n" +
			"</Project>\n";
		File.WriteAllText (Path.Combine (projDir, name + ".csproj"), csproj);
	}

	// MVC (classic) files: the controller, models dir and the Razor views the
	// legacy MvcProject.xpt lays out (HomeController, Index/_Layout/Error views,
	// _ViewStart, the Views/Web.config, and the Content/Scripts/Models folders).
	static void ScaffoldAspNetMvcFiles (string projDir, string name)
	{
		Directory.CreateDirectory (Path.Combine (projDir, "Controllers"));
		Directory.CreateDirectory (Path.Combine (projDir, "Models"));
		Directory.CreateDirectory (Path.Combine (projDir, "Content"));
		Directory.CreateDirectory (Path.Combine (projDir, "Scripts"));
		Directory.CreateDirectory (Path.Combine (projDir, "Views", "Home"));
		Directory.CreateDirectory (Path.Combine (projDir, "Views", "Shared"));
		File.WriteAllText (Path.Combine (projDir, "Controllers", "HomeController.cs"),
			"using System.Web.Mvc;\n\n" +
			"namespace " + name + ".Controllers\n" +
			"{\n" +
			"\tpublic class HomeController : Controller\n" +
			"\t{\n" +
			"\t\tpublic ActionResult Index ()\n" +
			"\t\t{\n" +
			"\t\t\treturn View ();\n" +
			"\t\t}\n" +
			"\t}\n" +
			"}\n");
		File.WriteAllText (Path.Combine (projDir, "Views", "Web.config"),
			"<?xml version=\"1.0\"?>\n" +
			"<configuration>\n" +
			"  <system.web>\n" +
			"    <compilation debug=\"true\" />\n" +
			"  </system.web>\n" +
			"</configuration>\n");
		File.WriteAllText (Path.Combine (projDir, "Views", "_ViewStart.cshtml"),
			"@{ Layout = \"~/Views/Shared/_Layout.cshtml\"; }\n");
		File.WriteAllText (Path.Combine (projDir, "Views", "Home", "Index.cshtml"),
			"@section title { Home }\n" +
			"<h2>" + name + "</h2>\n" +
			"<p>Replace this content with the " + name + " home view.</p>\n");
		File.WriteAllText (Path.Combine (projDir, "Views", "Shared", "_Layout.cshtml"),
			"<!DOCTYPE html>\n" +
			"<html>\n" +
			"<head><title>@ViewData[\"Title\"]</title></head>\n" +
			"<body>\n" +
			"@RenderBody ()\n" +
			"</body>\n" +
			"</html>\n");
		File.WriteAllText (Path.Combine (projDir, "Views", "Shared", "Error.cshtml"),
			"@{ Layout = null; }\n" +
			"<h2>An error occurred while processing your request.</h2>\n");
	}

	// WebForms (classic) files: Default.aspx + code-behind + designer (the
	// legacy WebFormsProject.xpt layout).
	static void ScaffoldAspNetWebFormsFiles (string projDir, string name)
	{
		File.WriteAllText (Path.Combine (projDir, "Default.aspx"),
			"<%@ Page Language=\"C#\" AutoEventWireup=\"true\" CodeBehind=\"Default.aspx.cs\" Inherits=\"" + name + ".Default\" %>\n" +
			"<!DOCTYPE html>\n" +
			"<html>\n" +
			"<head><title>" + name + "</title></head>\n" +
			"<body>\n" +
			"  <form id=\"form1\" runat=\"server\">\n" +
			"    <h1>Welcome to " + name + "</h1>\n" +
			"  </form>\n" +
			"</body>\n" +
			"</html>\n");
		File.WriteAllText (Path.Combine (projDir, "Default.aspx.cs"),
			"namespace " + name + "\n" +
			"{\n" +
			"\tpublic partial class Default : System.Web.UI.Page\n" +
			"\t{\n" +
			"\t\tprotected void Page_Load (object sender, System.EventArgs e)\n" +
			"\t\t{\n" +
			"\t\t}\n" +
			"\t}\n" +
			"}\n");
		File.WriteAllText (Path.Combine (projDir, "Default.aspx.designer.cs"),
			"namespace " + name + "\n" +
			"{\n" +
			"\tpublic partial class Default\n" +
			"\t{\n" +
			"\t}\n" +
			"}\n");
	}

	// The <Compile> items for the .cs files under the project: the per-kind
	// source files the scaffold just wrote (Global.asax.cs, controllers,
	// pages), listed relative to the project dir.
	static string CollectContentItems (string projDir)
	{
		var sb = new System.Text.StringBuilder ();
		foreach (var file in Directory.GetFiles (projDir, "*.cs", SearchOption.AllDirectories)) {
			var rel = Path.GetRelativePath (projDir, file).Replace ('\\', '/');
			sb.Append ("    <Compile Include=\"").Append (rel).Append("\" />\n");
		}
		return sb.ToString ();
	}

	void OnCancel (object? sender, RoutedEventArgs e)
	{
		CreatedSolutionPath = null;
		Close ();
	}
}
