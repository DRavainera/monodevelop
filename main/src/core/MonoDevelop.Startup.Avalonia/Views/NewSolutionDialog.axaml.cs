using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MonoDevelop.AvaloniaShell.Services;

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
	};

	public string? SelectedTemplateId { get; private set; }
	public string? SolutionNameValue => SolutionName?.Text;
	public string? LocationValue => LocationBox?.Text;

	public NewSolutionDialog () : this (null) { }

	// QA hook: --newsolution=<templateId> preselects a template.
	public NewSolutionDialog (string? preselectTemplate)
	{
		InitializeComponent ();
		IconConsole.Source = IconService.GetImage ("project-console-32");
		IconLibrary.Source = IconService.GetImage ("project-library-32");
		IconTest.Source = IconService.GetImage ("file-unit-test-32");
		IconShared.Source = IconService.GetImage ("solution-32");

		var idx = Array.FindIndex (Templates, t => t.Tag == preselectTemplate);
		TemplateList.SelectedIndex = idx >= 0 ? idx : 0;
		LocationBox.Text = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
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

			// dotnet CLI scaffolding (same tool the legacy templates wrap) run hidden.
			var psi = new System.Diagnostics.ProcessStartInfo {
				FileName = "dotnet",
				Arguments = $"new {template} -n {name} -o \"{Path.Combine (dir, name)}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			using var p = System.Diagnostics.Process.Start (psi);
			p?.WaitForExit (60000);

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

	void OnCancel (object? sender, RoutedEventArgs e)
	{
		CreatedSolutionPath = null;
		Close ();
	}
}
