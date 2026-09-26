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
	// the templates offered by the GTK dialog for the C# category.
	static readonly (string Tag, string Title, string Desc, string Icon32, string PreviewIcon) [] Templates = {
		("console", "Console Application",
			"A project for creating a command-line application that can run on .NET on Windows, Linux and MacOS.",
			"project-console-32", "project-console-32"),
		("library", "Class Library",
			"A project for creating a class library that targets .NET Standard or .NET Core.",
			"project-library-32", "project-library-32"),
		("unittest", "Unit Test Project",
			"A project that contains unit tests that can run with NUnit / xUnit test runners.",
			"file-unit-test-32", "file-unit-test-32"),
		("shared", "Shared Project",
			"A project for sharing source files between multiple projects via implicit linking.",
			"solution-32", "solution-32"),
	};	public string? SelectedTemplateId { get; private set; }

	/// <summary>QA hook: --newproject uses a throwaway temp location.</summary>
	string? forcedLocation;

	/// <summary>When true (solution already open) the created project is added to
	/// it (legacy AddSolutionItem flow) instead of opening a new solution.</summary>
	public bool AddToOpenSolution {
		get => AddToSolutionCheck?.IsChecked == true;
		set {
			if (AddToSolutionCheck is not null)
				AddToSolutionCheck.IsChecked = value;
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
		IconConsole.Source = IconService.GetImage ("project-console-32");
		IconLibrary.Source = IconService.GetImage ("project-library-32");
		IconTest.Source = IconService.GetImage ("file-unit-test-32");
		IconShared.Source = IconService.GetImage ("solution-32");

		this.forcedLocation = forcedLocation;
		var idx = Array.FindIndex (Templates, t => t.Tag == preselectTemplate);
		TemplateList.SelectedIndex = idx >= 0 ? idx : 0;
		LocationBox.Text = forcedLocation ?? Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
		// With a solution open, "Add to solution" is the default (like the GTK dialog
		// radio group 'Add to solution' when a workspace is open).
		if (AddToSolutionCheck is not null)
			AddToSolutionCheck.IsChecked = forcedLocation is not null;
	}

	void OnCategorySelected (object? sender, SelectionChangedEventArgs e) { }

	void OnTemplateSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (TemplateList.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
			return;
		var t = Templates.FirstOrDefault (x => x.Tag == tag);
		TemplateDescription.Text = t.Desc;
		PreviewImage.Source = IconService.GetImage (t.PreviewIcon);
		PreviewLabel.Text = t.Title;
		SelectedTemplateId = t.Tag;
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
		var name = string.IsNullOrWhiteSpace (SolutionName?.Text) ? "TestProj" : SolutionName.Text!.Trim ();
		var location = string.IsNullOrWhiteSpace (LocationBox?.Text)
			? Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)
			: LocationBox.Text!.Trim ();

		try {
			var dir = CreateDirectoryCheck?.IsChecked == true ? Path.Combine (location, name) : location;
			Directory.CreateDirectory (dir);
			var slnPath = Path.Combine (dir, name + ".sln");
			var projDir = Path.Combine (dir, name);

			// dotnet CLI scaffolding (same tool the legacy templates wrap) run hidden.
			var psi = new System.Diagnostics.ProcessStartInfo {
				FileName = "dotnet",
				Arguments = $"new {template} -n {name} -o \"{projDir}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			using var p = System.Diagnostics.Process.Start (psi);
			p?.WaitForExit (60000);

			// Add-to-solution mode: only the project is created; MainWindow wires it
			// into the loaded .sln (legacy ProjectOperations.AddSolutionItem).
			var createdProj = Directory.GetFiles (projDir, "*.csproj").FirstOrDefault ();
			if (AddToOpenSolution) {
				CreatedProjectPath = createdProj;
				CreatedSolutionPath = null;
				Close ();
				return;
			}

			// Fallback: minimal valid .sln if dotnet CLI is unavailable/failed.
			if (!File.Exists (slnPath)) {
				var guid = Guid.NewGuid ().ToString ().ToUpperInvariant ();
				File.WriteAllText (slnPath,
					"Microsoft Visual Studio Solution File, Format Version 12.00\n" +
					"# Visual Studio Version 17\n" +
					$"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{name}\", \"{name}\\{name}.csproj\", \"{{{guid}}}\"\n" +
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
