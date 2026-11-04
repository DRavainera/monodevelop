using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
	static readonly (string Tag, string Title, string Desc, string Icon32) [] Templates = {
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
		IconConsole.Source = IconService.GetTemplateImage ("console-project");
		IconLibrary.Source = IconService.GetTemplateImage ("library-project");
		IconTest.Source = IconService.GetTemplateImage ("test-project");
		IconShared.Source = IconService.GetTemplateImage ("crossplatform-shared-project");

		this.forcedLocation = forcedLocation;
		var idx = Array.FindIndex (Templates, t => t.Tag == preselectTemplate);
		TemplateList.SelectedIndex = idx >= 0 ? idx : 0;
		LocationBox.Text = forcedLocation ?? Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
		// "Add to solution" only applies when a solution is actually open (the GTK
		// dialog offers that target in that context); otherwise the option is inert.
		if (AddToSolutionCheck is not null) {
			AddToSolutionCheck.IsChecked = forcedLocation is not null;
			AddToSolutionCheck.IsEnabled = forcedLocation is not null;
		}
	}

	void OnCategorySelected (object? sender, SelectionChangedEventArgs e) { }

	void OnTemplateSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (TemplateList.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
			return;
		var t = Templates.FirstOrDefault (x => x.Tag == tag);
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
		// pair directly (legacy SharedAssetsProject format).
		string cliTemplate = template switch {
			"console" => "console",
			"library" => "classlib",
			"unittest" => "nunit",
			_ => "",
		};

		try {
			var dir = CreateDirectoryCheck?.IsChecked == true ? Path.Combine (location, name) : location;
			Directory.CreateDirectory (dir);
			var slnPath = Path.Combine (dir, name + ".sln");
			var projDir = Path.Combine (dir, name);

			if (cliTemplate != "") {
				// dotnet CLI scaffolding (same tool the legacy templates wrap) run hidden.
				var psi = new System.Diagnostics.ProcessStartInfo {
					FileName = "dotnet",
					Arguments = $"new {cliTemplate} -n {name} -o \"{projDir}\"",
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
			var createdProj = Directory.Exists (projDir)
				? Directory.GetFiles (projDir, "*.csproj").FirstOrDefault () ?? Directory.GetFiles (projDir, "*.shproj").FirstOrDefault ()
				: null;
			if (AddToOpenSolution) {
				CreatedProjectPath = createdProj;
				CreatedSolutionPath = null;
				Close ();
				return;
			}

			// Fallback: minimal valid .sln if dotnet CLI is unavailable/failed.
			if (!File.Exists (slnPath)) {
				var guid = Guid.NewGuid ().ToString ().ToUpperInvariant ();
				var projFile = createdProj is null ? name + ".csproj" : Path.GetFileName (createdProj);
				var projType = projFile.EndsWith (".shproj", StringComparison.Ordinal)
					? "D954291E-2A0B-460D-934E-DC6B0785DB48"
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

	void OnCancel (object? sender, RoutedEventArgs e)
	{
		CreatedSolutionPath = null;
		Close ();
	}
}
