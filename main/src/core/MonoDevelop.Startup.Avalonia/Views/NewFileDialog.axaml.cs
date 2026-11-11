using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MonoDevelop.Core;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>Nodo de categoría del árbol New File (legacy NewFileDialog.Category).</summary>
public sealed class NewFileCategory
{
	public string Name { get; init; } = "";
	public List<NewFileCategory> Children { get; } = new ();
	public List<NewFileTemplate> Templates { get; } = new ();
	public NewFileCategory? Parent { get; internal set; }

	public void Add (NewFileCategory child)
	{
		child.Parent = this;
		Children.Add (child);
	}
}

/// <summary>
/// Plantilla de nuevo archivo fiel al catálogo xft de CSharpBinding.addin.xml
/// (los legacy FileTemplate que cargaba FileTemplate.GetFileTemplates).
/// </summary>
public sealed class NewFileTemplate
{
	public string Id { get; init; } = "";
	public string Name { get; init; } = "";
	public string Description { get; init; } = "";
	public string Language { get; init; } = "";
	public string IconId { get; init; } = "";
	public IImage? Icon { get; set; }
	public string DefaultFilename { get; init; } = "";
	public string Extension { get; init; } = "";
	public bool FixedFilename { get; init; }
	/// <summary>Subpath del fichero del template respecto al directorio de creación
	/// (atributo name del File xft): el legacy GetFileName lo preserva al añadir
	/// al proyecto (Assembly Information → Properties/AssemblyInfo.cs).</summary>
	public string FileSubPath { get; init; } = "";
	public bool AddStandardHeader { get; init; } = true;
	/// <summary>Cuerpo CDATA crudo del xft ("" para el template de archivo vacío).</summary>
	public string Body { get; init; } = "";

	/// <summary>Aplica las sustituciones del legacy FileTemplateParser
	/// ($${Tag} y $if$/$else$/$endif$) sobre el cuerpo del template.</summary>
	public string GenerateContent (string projectName, string fileName)
	{
		var tags = new Dictionary<string, string> {
			["ProjectName"] = projectName ?? "",
			["AuthorCompany"] = "",
			["AuthorCopyright"] = "",
			["AuthorTrademark"] = "",
			["AssemblyInfoVersion"] = "",
			["UseCustomAssemblyInfoVersion"] = "false",
			["FileName"] = Path.GetFileName (fileName),
			["FileNameWithoutExtension"] = Path.GetFileNameWithoutExtension (fileName),
			["Directory"] = Path.GetDirectoryName (fileName) ?? "",
			["FullFileName"] = fileName,
			["AuthorName"] = "",
			["AuthorEmail"] = "",
			["CopyrightHolder"] = "",
			// Legacy F# xft templates reference ${Namespace}: the binding sets it
			// from the project's root namespace. In free-document mode (no project)
			// the file name without extension is the sensible default (F# modules
			// are named after their file).
			["Namespace"] = string.IsNullOrEmpty (projectName)
				? Path.GetFileNameWithoutExtension (fileName)
				: projectName,
		};
		return TemplateText.Parse (Body, tags);
	}
}

/// <summary>Port mínimo del legacy MonoDevelop.Ide.Templates.FileTemplateParser:
/// condiciones $if$ (tag == value)A$else$B$endif$ y sustitución $${Tag},
/// que opera sobre los cuerpos CDATA de los xft.</summary>
static class TemplateText
{
	static readonly Regex IfRe = new (
		@"\$if\$ \((\w+)\s*==\s*([^)]*)\)(.*?)(?:\$else\$(.*?))?\$endif\$",
		RegexOptions.Singleline | RegexOptions.Compiled);

	public static string Parse (string input, Dictionary<string, string> tags)
	{
		input = IfRe.Replace (input, m => {
			string? value;
			tags.TryGetValue (m.Groups [1].Value, out value);
			var cond = value is not null && string.Equals (value, m.Groups [2].Value.Trim (), StringComparison.OrdinalIgnoreCase);
			if (cond)
				return m.Groups [3].Value;
			return m.Groups [4].Success ? m.Groups [4].Value : "";
		});
		input = Regex.Replace (input, @"\$\{(\w+)\}", m => tags.TryGetValue (m.Groups [1].Value, out var v) ? v : "");
		return input;
	}
}

/// <summary>
/// Port Avalonia de MonoDevelop.Ide.Projects.NewFileDialog (diálogo "New File"):
/// mismo layout (718x524, paneles 192/292/*), mismo comportamiento de
/// categorías/template/panelfinfo y misma creación de ficheros (BOM UTF8,
/// standard header desde la policy global, sustituciones del xft). El catálogo
/// es el de los xft de CSharpBinding (la shell no carga el host legacy de add-ins).
/// </summary>
public partial class NewFileDialog : Window
{
	// Catálogo: los dos FileTemplate de CSharpBinding.addin.xml (id de nodo igual
	// al legacy) con sus datos xft literales.
	static readonly NewFileTemplate EmptyFileTemplate = new () {
		Id = "CSharpEmptyFile",
		Name = "Empty File",
		Description = "Creates an empty C# file.",
		Language = "C#",
		IconId = "md-text-file-icon",
		DefaultFilename = "EmptyCSharpFile",
		Extension = ".cs",
		AddStandardHeader = true,
		Body = "",
	};

	static readonly NewFileTemplate AssemblyInfoTemplate = new () {
		Id = "CSharpAssemblyInfo",
		Name = "Assembly Information",
		Description = "A file defining assembly information attributes.",
		Language = "C#",
		IconId = "md-file-information",
		DefaultFilename = "AssemblyInfo.cs",
		Extension = ".cs",
		FixedFilename = true,
		FileSubPath = "Properties/AssemblyInfo.cs",
		AddStandardHeader = true,
		// Cuerpo CDATA literal de AssemblyInfo.xft.xml.
		Body = @"using System.Reflection;
using System.Runtime.CompilerServices;

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.

[assembly: AssemblyTitle(""${ProjectName}"")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(""${AuthorCompany}"")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright(""${AuthorCopyright}"")]
[assembly: AssemblyTrademark(""${AuthorTrademark}"")]
[assembly: AssemblyCulture("")]

// The assembly version has the format ""{Major}.{Minor}.{Build}.{Revision}"".
// The form ""{Major}.{Minor}.*"" will automatically update the build and revision,
// and ""{Major}.{Minor}.{Build}.*"" will update just the revision.

$if$ ($UseCustomAssemblyInfoVersion$ == true)[assembly: AssemblyVersion(""${AssemblyInfoVersion}"")]
$else$[assembly: AssemblyVersion(""1.0.*"")]
$endif$
// The following attributes are used to specify the signing key for the assembly, 
// if desired. See the Mono documentation for more information about signing.

//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("""")]",
	};

	// F# category (port of the five FileTemplates of FSharpBinding.addin.xml,
	// registered under /MonoDevelop/Ide/FileTemplates with the xft data below;
	// the category tree mirrors the legacy _Category fields: General / Misc / NUnit).
	static readonly NewFileTemplate FSharpSourceTemplate = new () {
		Id = "EmptyFSharpSource",
		Name = "F# Source File",
		Description = "Creates an empty F# source file.",
		Language = "F#",
		IconId = "md-text-file-icon",
		DefaultFilename = "Class.fs",
		Extension = ".fs",
		AddStandardHeader = true,
		Body = "",
	};

	static readonly NewFileTemplate FSharpScriptTemplate = new () {
		Id = "EmptyFSharpScript",
		Name = "F# Script File",
		Description = "Creates an empty F# script file.",
		Language = "F#",
		IconId = "md-text-file-icon",
		DefaultFilename = "Script.fsx",
		Extension = ".fsx",
		AddStandardHeader = true,
		Body = "",
	};

	static readonly NewFileTemplate FSharpSignatureTemplate = new () {
		Id = "EmptyFSharpSignature",
		Name = "F# Signature File",
		Description = "Creates an empty F# signature file.\n\nA signature file contains information about the public signatures of a set of F# program elements, such as types, namespaces, and modules.\n\nIt can be used to specify the accessibility of these program elements.",
		Language = "F#",
		IconId = "md-text-file-icon",
		DefaultFilename = "Class.fsi",
		Extension = ".fsi",
		AddStandardHeader = true,
		Body = "",
	};

	static readonly NewFileTemplate FSharpAssemblyInfoTemplate = new () {
		Id = "FSharpAssemblyInfo",
		Name = "Assembly Information",
		Description = "A file defining assembly information attributes.",
		Language = "F#",
		IconId = "md-file-source",
		DefaultFilename = "AssemblyInfo.fs",
		Extension = ".fs",
		FixedFilename = true,
		FileSubPath = "AssemblyInfo.fs",
		AddStandardHeader = true,
		// Cuerpo CDATA literal de Templates/AssemblyInfo.xft.xml (binding F#).
		Body = @"namespace ${Namespace}
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitle(""${ProjectName}"")>]
[<assembly: AssemblyDescription("""")>]
[<assembly: AssemblyConfiguration("""")>]
[<assembly: AssemblyCompany(""${AuthorCompany}"")>]
[<assembly: AssemblyProduct("""")>]
[<assembly: AssemblyCopyright(""${AuthorCopyright}"")>]
[<assembly: AssemblyTrademark(""${AuthorTrademark}"")>]

// The assembly version has the format {Major}.{Minor}.{Build}.{Revision}

[<assembly: AssemblyVersion(""1.0.0.0"")>]

//[<assembly: AssemblyDelaySign(false)>]
//[<assembly: AssemblyKeyFile("""")>]

()",
	};
	static readonly NewFileTemplate FSharpNUnitTestTemplate = new () {
		Id = "FSharpNUnitTestType",
		Name = "F# Test Fixture",
		Description = "Creates a Test Fixture.",
		Language = "F#",
		IconId = "md-file-unit-test",
		DefaultFilename = "NUnitTest",
		Extension = ".fs",
		AddStandardHeader = true,
		// Cuerpo CDATA literal de Templates/FSharpNUnitTestType.xft.xml.
		Body = @"namespace ${Namespace}
open System
open NUnit.Framework

[<TestFixture>]
type Test() = 

    [<Test>]
    member x.TestCase() =
        Assert.IsTrue(true)",
	};

	readonly NewFileCategory csharpRoot = new () { Name = "C#" };
	readonly NewFileCategory generalCategory = new () { Name = "General" };

	// F# category tree (legacy _Category fields of the F# xft templates):
	// General (Source/Script/Signature), Misc (Assembly Information), NUnit (Test Fixture).
	readonly NewFileCategory fsharpRoot = new () { Name = "F#" };
	readonly NewFileCategory fsharpGeneral = new () { Name = "General" };
	readonly NewFileCategory fsharpMisc = new () { Name = "Misc" };
	readonly NewFileCategory fsharpNUnit = new () { Name = "NUnit" };

	// Free mode: proyectos de la solución cargada (legacy IdeApp.Workspace.GetAllProjects).
	readonly List<string> projects;
	// Fixed mode (contexto del ProjectPad): directorio del nodo + nombre del proyecto
	// (legacy new NewFileDialog(parentProject, basePath): boxProject oculto).
	readonly string? fixedDirectory;
	readonly string? fixedProjectName;
	bool fixedMode => fixedDirectory is not null;

	string? effectiveDirectory;     // basePath: directorio de creación
	string? effectiveProjectName;   // ${ProjectName}
	string? previousProjectDirectory;

	// Legacy userEditedEntryText / previousDefaultEntryText: se guarda el texto
	// editado por el usuario para restaurarlo al volver a un template no fijo.
	string? userEditedEntryText;
	string? previousDefaultEntryText;
	bool rebuilding;

	/// <summary>Free mode (legacy FileCommands.NewFileHandler: new NewFileDialog(null, null)).</summary>
	public NewFileDialog (List<string>? projectPaths, string? activeProjectPath)
		: this (projectPaths, activeProjectPath, null, null) { }

	/// <summary>Fixed mode (legacy ProjectOperations.CreateProjectFile:
	/// new NewFileDialog(project, basePath), boxProject oculto).</summary>
	public NewFileDialog (string fixedDirectory, string? fixedProjectName)
		: this (null, null, fixedDirectory, fixedProjectName) { }

	public NewFileDialog (List<string>? projectPaths, string? activeProjectPath, string? fixedDirectory, string? fixedProjectName)
	{
		InitializeComponent ();
		MonoDevelop.AvaloniaShell.Controls.DialogWindow.Apply (this);

		this.fixedDirectory = fixedDirectory;
		this.fixedProjectName = fixedProjectName;
		projects = (projectPaths ?? new List<string> ())
			.Where (p => p is not null && p.Length > 0).ToList ();

		// El legacy TemplateView pinta el stock icon a Dnd (32px); IconService sirve
		// el PNG y la celda 32x32 lo escala (mismo patrón que NewSolutionDialog).
		// Fallback legacy cuando el icono no existe: "md-file-source".
		EmptyFileTemplate.Icon = IconService.GetImage (EmptyFileTemplate.IconId) ?? IconService.GetImage ("md-file-source");
		AssemblyInfoTemplate.Icon = IconService.GetImage (AssemblyInfoTemplate.IconId) ?? IconService.GetImage ("md-file-source");
		FSharpSourceTemplate.Icon = IconService.GetImage (FSharpSourceTemplate.IconId) ?? IconService.GetImage ("md-file-source");
		FSharpScriptTemplate.Icon = IconService.GetImage (FSharpScriptTemplate.IconId) ?? IconService.GetImage ("md-file-source");
		FSharpSignatureTemplate.Icon = IconService.GetImage (FSharpSignatureTemplate.IconId) ?? IconService.GetImage ("md-file-source");
		FSharpAssemblyInfoTemplate.Icon = IconService.GetImage (FSharpAssemblyInfoTemplate.IconId) ?? IconService.GetImage ("md-file-source");
		FSharpNUnitTestTemplate.Icon = IconService.GetImage (FSharpNUnitTestTemplate.IconId) ?? IconService.GetImage ("md-file-source");

		csharpRoot.Add (generalCategory);

		fsharpRoot.Add (fsharpGeneral);
		fsharpRoot.Add (fsharpMisc);
		fsharpRoot.Add (fsharpNUnit);

		// Árbol de categorías (legacy catView): plantilla FuncTreeDataTemplate,
		// mismo patrón que los árboles de MainWindow (MakeSymbolTree/MakeVariableTree).
		CategoryTree.ItemTemplate = new Avalonia.Controls.Templates.FuncTreeDataTemplate<NewFileCategory> (
			(cat, _) => new TextBlock { Text = cat.Name, Padding = new Avalonia.Thickness (2, 1) },
			cat => cat.Children);
		CategoryTree.SelectionChanged += OnCategorySelected;

		if (!fixedMode) {
			if (projects.Count > 0) {
				ProjectBox.IsVisible = true;
				// Legacy: con una única solución el combo muestra solo el nombre.
				foreach (var p in projects)
					ProjectCombo.Items.Add (Path.GetFileNameWithoutExtension (p));
				var activeIdx = activeProjectPath is null ? -1 : projects.IndexOf (activeProjectPath);
				ProjectCombo.SelectedIndex = activeIdx >= 0 ? activeIdx : 0;
				// Legacy: projectAddCheckbox.Active = (CurrentSelectedProject != null).
				if (activeProjectPath is not null) {
					effectiveDirectory = Path.GetDirectoryName (activeProjectPath)!;
					effectiveProjectName = Path.GetFileNameWithoutExtension (activeProjectPath);
					previousProjectDirectory = effectiveDirectory;
					ProjectPathBox.Text = effectiveDirectory;
					AddToProjectCheck.IsChecked = true;
				}
				AddToProjectCheck.IsCheckedChanged += (_, __) => OnAddToProjectToggled (AddToProjectCheck, AddToProjectCheck.IsChecked == true);
				ProjectCombo.SelectionChanged += OnProjectComboChanged;
				ProjectPathBox.TextChanged += OnProjectPathChanged;
			}
			// Sin proyectos boxProject queda oculto (legacy boxProject.Hide()).
		} else {
			effectiveDirectory = fixedDirectory;
			effectiveProjectName = fixedProjectName;
			previousProjectDirectory = fixedDirectory;
		}

		RebuildCatalog ();
	}

	// La vista inicial (categoría+template por defecto) se monta en OnOpened: durante
	// el ctor el árbol no está realizado y la selección reentrante anula controles.
	bool initialViewDone;

	bool ProjectActive => fixedMode || (ProjectBox.IsVisible && AddToProjectCheck.IsChecked == true);

	/// <summary>Legacy GetCategoryPropertyKey: el sufijo de tipo de proyecto
	/// (GetTypeTags + DotNetProject.LanguageName) es "C#/C#" para el entorno net10
	/// de la shell.</summary>
	string GetCategoryPropertyKey ()
	{
		var key = "Dialogs.NewFileDialog.LastSelectedCategory";
		if (ProjectActive)
			key += ".C#.C#";
		return key;
	}

	string LastCategoryPath
	{
		get {
			try {
				return PropertyService.Get<string> (GetCategoryPropertyKey (), "C#/General");
			} catch {
				return "C#/General";
			}
		}
	}

	/// <summary>Legacy GetFileTemplates + AddTemplate: el catálogo se recompute con
	/// el proyecto/contexto vigente. La condición ParentProject (RequireProject=True,
	/// ExcludedFiles="AssemblyInfo.cs" en C#, "AssemblyInfo.fs" en F#) solo se evalúa
	/// con proyecto: Assembly Information se excluye cuando ya existe AssemblyInfo en
	/// el directorio de creación; sin proyecto la condición no se consulta (se incluye).</summary>
	void RebuildCatalog ()
	{
		generalCategory.Templates.Clear ();
		generalCategory.Templates.Add (EmptyFileTemplate);
		if (!ProjectActive || effectiveDirectory is null ||
			!File.Exists (Path.Combine (effectiveDirectory, "AssemblyInfo.cs")))
			generalCategory.Templates.Add (AssemblyInfoTemplate);

		fsharpGeneral.Templates.Clear ();
		fsharpGeneral.Templates.Add (FSharpSourceTemplate);
		fsharpGeneral.Templates.Add (FSharpScriptTemplate);
		fsharpGeneral.Templates.Add (FSharpSignatureTemplate);
		fsharpMisc.Templates.Clear ();
		fsharpNUnit.Templates.Clear ();
		if (!ProjectActive || effectiveDirectory is null ||
			!File.Exists (Path.Combine (effectiveDirectory, "AssemblyInfo.fs")))
			fsharpMisc.Templates.Add (FSharpAssemblyInfoTemplate);
		fsharpNUnit.Templates.Add (FSharpNUnitTestTemplate);

		rebuilding = true;
		try {
			CategoryTree.ItemsSource = new List<NewFileCategory> { csharpRoot, fsharpRoot };
		} finally {
			rebuilding = false;
		}
	}

	NewFileCategory? FindCategory (string catPath)
	{
		// The tree root is a language (C# / F#); the path is "Language/Category...".
		var parts = catPath.Split ('/');
		if (parts.Length == 0)
			return null;
		var root = parts [0] == "F#" ? fsharpRoot : csharpRoot;
		if (root.Name != parts [0])
			return null;
		var node = root;
		for (int i = 1; i < parts.Length; i++) {
			var child = node.Children.FirstOrDefault (c => c.Name == parts [i]);
			if (child is null)
				return null;
			node = child;
		}
		return node;
	}

	static string GetCategoryPath (NewFileCategory node)
	{
		var parts = new List<string> { node.Name };
		for (var p = node.Parent; p is not null; p = p.Parent)
			parts.Insert (0, p.Name);
		return string.Join ("/", parts);
	}

	/// <summary>Legacy InitializeView: última categoría seleccionada
	/// (PropertyService, por defecto "General"), si no "Misc", si no la primera.</summary>
	void InitializeView ()
	{
		var target = FindCategory (LastCategoryPath) ?? FindCategory ("C#/General") ?? generalCategory;
		CategoryTree.SelectedItem = target;
	}

	/// <summary>Tras un RebuildCatalog la selección queda vacía: la legacy
	/// InitializeView se vuelve a ejecutar y SelectTemplate re-elige la plantilla.</summary>
	void RestoreCategorySelection ()
	{
		if (CategoryTree.SelectedItem is NewFileCategory)
			return;
		var target = FindCategory (LastCategoryPath) ?? FindCategory ("C#/General") ?? generalCategory;
		CategoryTree.SelectedItem = target;
	}

	// Legacy CategoryChange: rellena la lista de templates y la selecciona.
	void OnCategorySelected (object? sender, SelectionChangedEventArgs e)
	{
		if (rebuilding || CategoryTree is null || TemplateList is null || CategoryTree.SelectedItem is not NewFileCategory cat)
			return;
		FillTemplates (cat);
	}

	// Legacy FillCategoryTemplates: dedupe por nombre, selecciona el primero.
	void FillTemplates (NewFileCategory cat)
	{
		if (TemplateList is null || TemplateTitle is null || TemplateDescription is null)
			return;
		var items = new List<NewFileTemplate> ();
		var seen = new HashSet<string> ();
		foreach (var t in cat.Templates)
			if (seen.Add (t.Name))
				items.Add (t);
		TemplateList.ItemsSource = items;
		if (items.Count > 0)
			TemplateList.SelectedIndex = 0;
		else {
			TemplateTitle.Text = "";
			TemplateDescription.Text = "";
			UpdateOkStatus ();
		}
	}

	// Legacy SelectedTemplateChanged: título/descripción + filename por defecto,
	// con el tracking userEditedEntryText/previousDefaultEntryText.
	void OnTemplateSelected (object? sender, SelectionChangedEventArgs e)
	{
		if (TemplateList is null || TemplateTitle is null || TemplateDescription is null || NameBox is null)
			return;
		var item = TemplateList.SelectedItem as NewFileTemplate;
		if (item is null) {
			TemplateTitle.Text = "";
			TemplateDescription.Text = "";
			NameBox.IsEnabled = true;
			UpdateOkStatus ();
			return;
		}

		TemplateTitle.Text = item.Name;
		TemplateDescription.Text = item.Description;

		// En esta versión de Avalonia TextBox.Text puede devolver null antes de
		// inicializar el texto (señal reentrante durante InitializeView en OnOpened).
		var filename = (NameBox.Text ?? "").Trim ();
		string? name = null;

		if (item.FixedFilename) {
			if (userEditedEntryText is null)
				userEditedEntryText = filename;
			name = item.DefaultFilename;
			NameBox.IsEnabled = false;
		} else {
			if (userEditedEntryText is not null) {
				name = userEditedEntryText;
				userEditedEntryText = null;
			}
			NameBox.IsEnabled = true;
		}

		if (string.IsNullOrEmpty (filename) || previousDefaultEntryText == filename) {
			previousDefaultEntryText = item.DefaultFilename;
			name = item.DefaultFilename;
		}

		if (name is not null)
			NameBox.Text = name; // TextChanged → UpdateOkStatus
		else
			UpdateOkStatus ();
	}

	void OnNameChanged (object? sender, TextChangedEventArgs e) => UpdateOkStatus ();

	void OnNameKeyDown (object? sender, KeyEventArgs e)
	{
		// Legacy nameEntry.Activated → OpenEvent.
		if (e.Key == Key.Enter)
			OnNew (this, new RoutedEventArgs ());
	}

	// Legacy UpdateOkStatus. En free-mode sin proyecto la legacy consulta
	// CanCreateUnsavedFiles; la shell siempre abre un documento de texto, así que
	// solo valida el nombre (legacy FileService.IsValidFileName).
	void UpdateOkStatus ()
	{
		try {
			var item = TemplateList.SelectedItem as NewFileTemplate;
			if (item is null) {
				NewButton.IsEnabled = false;
				return;
			}
			var filename = (NameBox.Text ?? "").Trim ();
			NewButton.IsEnabled = item.FixedFilename
				? filename == item.DefaultFilename
				: FileService.IsValidFileName (filename);
		} catch (Exception ex) {
			MainWindow.Instance?.Output ("[newfile] status: " + ex.Message);
		}
	}

	// Legacy AddToProjectToggled: sensibiliza la fila, re-fetch del catálogo y
	// re-selección del template vigente.
	void OnAddToProjectToggled (object? sender, bool isChecked)
	{
		var active = isChecked;
		ProjectCombo.IsEnabled = active;
		ProjectPathLabel.IsEnabled = active;
		ProjectPathBox.IsEnabled = active;
		BrowseButton.IsEnabled = active;

		var titem = TemplateList.SelectedItem as NewFileTemplate;

		if (active) {
			if (ProjectCombo.SelectedIndex >= 0 && ProjectCombo.SelectedIndex < projects.Count) {
				ApplyProjectComboSelection ();
			}
		} else {
			// Legacy: parentProject = null; basePath se conserva en la entrada.
			effectiveProjectName = null;
		}
		RebuildCatalog ();
		RestoreCategorySelection ();

		if (titem is not null)
			SelectTemplateById (titem.Id);

		UpdateOkStatus ();
	}

	// Legacy AddToProjectComboChanged: adopta el directorio del proyecto cuando el
	// basePath actual está vacío o era el directorio del proyecto anterior.
	void ApplyProjectComboSelection ()
	{
		var p = projects [ProjectCombo.SelectedIndex];
		var projDir = Path.GetDirectoryName (p)!;
		effectiveProjectName = Path.GetFileNameWithoutExtension (p);
		if (effectiveDirectory is null || effectiveDirectory.Length == 0 ||
			(previousProjectDirectory is not null && effectiveDirectory == previousProjectDirectory)) {
			effectiveDirectory = projDir;
			ProjectPathBox.Text = projDir;
		}
		previousProjectDirectory = projDir;
	}

	void OnProjectComboChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (!ProjectBox.IsVisible || AddToProjectCheck.IsChecked != true)
			return;
		var titem = TemplateList.SelectedItem as NewFileTemplate;
		ApplyProjectComboSelection ();
		RebuildCatalog ();
		RestoreCategorySelection ();
		if (titem is not null)
			SelectTemplateById (titem.Id);
		UpdateOkStatus ();
	}

	// Legacy AddToProjectPathChanged: basePath = projectFolderEntry.Path.
	void OnProjectPathChanged (object? sender, TextChangedEventArgs e)
	{
		effectiveDirectory = ProjectPathBox.Text.Trim ();
	}

	// El legacy FolderEntry tiene un botón "..." que abre el selector de carpeta.
	async void OnBrowse (object? sender, RoutedEventArgs e)
	{
		try {
			var folders = await StorageProvider.OpenFolderPickerAsync (new Avalonia.Platform.Storage.FolderPickerOpenOptions {
				Title = "Select a folder",
				AllowMultiple = false,
			});
			if (folders.Count > 0)
				ProjectPathBox.Text = folders [0].Path.LocalPath;
		} catch (Exception ex) {
			MainWindow.Instance?.Output ("[newfile] folder picker: " + ex.Message);
		}
	}

	/// <summary>Legacy SelectTemplate(id): expande y selecciona la categoría que
	/// contiene el template y deja seleccionado el template en la lista.</summary>
	bool SelectTemplateById (string id)
	{
		NewFileCategory? cat = null;
		System.Func<NewFileCategory, bool>? walk = null;
		// The tree root is a language; the template lives under C# or F#.
		walk = c => {
			if (c.Templates.Any (t => t.Id == id)) {
				cat = c;
				return true;
			}
			return c.Children.Any (walk!);
		};

		if (!walk (csharpRoot) && !walk (fsharpRoot)) {
			RestoreCategorySelection ();
			return false;
		}

		if (!ReferenceEquals (CategoryTree.SelectedItem, cat))
			CategoryTree.SelectedItem = cat; // → OnCategorySelected → FillTemplates
		else
			FillTemplates (cat!);

		TemplateList.SelectedItem = cat!.Templates.First (t => t.Id == id);
		return true;
	}

	// Legacy StandardHeaderService.GetHeader(policyParent, fileName, newFile: true)
	// con la policy global (Preferencias → Source Code → Standard Header): si el
	// Text está vacío o IncludeInNewFiles=false, el header es "". Para .cs el
	// comment tag es el single-line "//".
	static string GetStandardHeader (string fileName)
	{
		var el = PreferencesDialog.LoadGlobalPolicy ("StandardHeader");
		if (el is null)
			return "";
		var include = el.Element ("IncludeInNewFiles") is { } incEl
			? (!bool.TryParse (incEl.Value, out var inc) || inc)
			: true;
		var text = (string?)el.Element ("Text") ?? "";
		if (string.IsNullOrEmpty (text) || !include)
			return "";
		var sb = new StringBuilder ();
		foreach (var line in text.Split ('\n')) {
			sb.Append (string.IsNullOrWhiteSpace (line) ? "//" : "//" + line);
			sb.Append ('\n');
		}
		var tags = new Dictionary<string, string> {
			["FileName"] = Path.GetFileName (fileName),
			["FileNameWithoutExtension"] = Path.GetFileNameWithoutExtension (fileName),
			["Directory"] = Path.GetDirectoryName (fileName) ?? "",
			["FullFileName"] = fileName,
			["AuthorName"] = "",
			["AuthorEmail"] = "",
			["CopyrightHolder"] = "",
		};
		return TemplateText.Parse (sb.ToString (), tags);
	}

	// Legacy OpenEvent: persiste la última categoría, crea el fichero (o el
	// documento unsaved) y responde Ok.
	void OnNew (object? sender, RoutedEventArgs e)
	{
		if (!NewButton.IsEnabled)
			return;

		try {
			if (CategoryTree.SelectedItem is NewFileCategory cat)
				PropertyService.Set (GetCategoryPropertyKey (), GetCategoryPath (cat));
		} catch {
			// best effort: el perfil no debe bloquear la creación
		}

		var item = TemplateList.SelectedItem as NewFileTemplate;
		var name = (NameBox.Text ?? "").Trim ();
		if (item is null || name.Length == 0)
			return;

		var directory = ProjectActive ? effectiveDirectory : null;
		var projectName = ProjectActive ? (effectiveProjectName ?? "") : "";

		// Legacy GetFileName: el atributo name del File xft (subpath) manda; si no,
		// el nombre de la entrada + la extensión por defecto si no llevaba.
		string relPath;
		if (item.FixedFilename && !string.IsNullOrEmpty (item.FileSubPath))
			relPath = item.FileSubPath;
		else if (Path.GetExtension (name).Length == 0 && !string.IsNullOrEmpty (item.Extension))
			relPath = name + item.Extension;
		else
			relPath = name;

		// Legacy CreateFileContent: BOM UTF8 + (standard header si AddStandardHeader)
		// + contenido con las sustituciones del parser.
		var header = item.AddStandardHeader ? GetStandardHeader (relPath) : "";
		var content = item.GenerateContent (projectName, relPath);
		var full = header + content;

		if (directory is not null) {
			try {
				var target = Path.GetFullPath (Path.Combine (directory, relPath.Replace ('/', Path.DirectorySeparatorChar)));
				Directory.CreateDirectory (Path.GetDirectoryName (target)!);
				// Legacy SingleFileDescriptionTemplate: BOM UTF8 siempre (el StreamWriter
				// emite el preamble del UTF8Encoding(true)); GetBytes() no lo emitiría.
				using (var sw = new StreamWriter (target, false, new UTF8Encoding (true)))
					sw.Write (full);
				CreatedFilePath = target;
			} catch (Exception ex) {
				// Legacy: MessageService.ShowError y el diálogo se queda abierto.
				MainWindow.Instance?.Output ("[newfile] error creating file: " + ex.Message);
				return;
			}
		} else {
			UnsavedFileName = relPath;
			UnsavedContent = full;
		}
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e) => Close ();

	/// <summary>QA: preselecciona una template por id (legacy SelectTemplate(id)).</summary>
	public string? TemplateForQa { get; set; }
	/// <summary>QA: nombre a fijar después de la selección (camino de edición de usuario).</summary>
	public string? NameForQa { get; set; }
	/// <summary>QA: fuerza el estado del checkbox "Add to project" (solo free mode).</summary>
	public bool? AddToProjectForQa { get; set; }
	/// <summary>QA: auto-acepta al abrir (ciclos deterministas --newfile/--ctxnewfile).</summary>
	public bool ConfirmForQa { get; set; }

	/// <summary>Ruta absoluta del fichero escrito al aceptar (null si cancela/unsaved).</summary>
	public string? CreatedFilePath { get; private set; }
	/// <summary>Nombre del documento unsaved al aceptar (título del legacy NewDocument).</summary>
	public string? UnsavedFileName { get; private set; }
	/// <summary>Contenido del documento unsaved (sin BOM: es documento, no fichero).</summary>
	public string? UnsavedContent { get; private set; }

	protected override void OnOpened (EventArgs e)
	{
		base.OnOpened (e);
		if (!initialViewDone) {
			initialViewDone = true;
			InitializeView ();
			UpdateOkStatus ();
			NameBox.Focus ();
		}
		if (TemplateForQa is null && NameForQa is null && !ConfirmForQa && AddToProjectForQa is null)
			return;
		// Orden legacy: estado del proyecto → catálogo → template → nombre → OK.
		if (AddToProjectForQa is not null && ProjectBox.IsVisible &&
			(AddToProjectCheck.IsChecked == true) != AddToProjectForQa == true)
			AddToProjectCheck.IsChecked = AddToProjectForQa == true;
		if (TemplateForQa is not null)
			SelectTemplateById (TemplateForQa);
		if (NameForQa is not null)
			NameBox.Text = NameForQa;
		UpdateOkStatus ();
		MainWindow.Instance?.Output ($"[newfile-qa] cat={(CategoryTree.SelectedItem as NewFileCategory)?.Name} " +
			$"tmplCount={TemplateList.Items.Count} selTmpl={(TemplateList.SelectedItem as NewFileTemplate)?.Id} " +
			$"title='{TemplateTitle.Text}' desc='{TemplateDescription.Text}' name='{NameBox.Text}' okEnabled={NewButton.IsEnabled}");
		if (ConfirmForQa)
			OnNew (this, new RoutedEventArgs ());
	}
}
