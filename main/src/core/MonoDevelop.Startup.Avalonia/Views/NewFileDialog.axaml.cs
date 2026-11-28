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
/// <summary>Un fichero del template (legacy FileDescriptionTemplate): el
/// atributo name del File xft (con ${Name}), su subpath literal (los templates
/// IsFixed y los ficheros condicionales de Global.asax → App_Start), su
/// extensión por defecto y el cuerpo CDATA (o el contenido del fichero src=
/// inlineado, con su BOM).</summary>
public sealed class NewFileTemplateFile
{
	public string Name { get; init; } = "";
	/// <summary>Subpath fijo respecto al directorio de creación; no vacío en los
	/// templates IsFixed (Properties/AssemblyInfo.cs) y en los ficheros
	/// condicionales de Global.asax (App_Start/WebApiConfig.cs).</summary>
	public string SubPath { get; init; } = "";
	public string DefaultExtension { get; init; } = "";
	public bool AddStandardHeader { get; init; } = true;
	/// <summary>Condición de creación del fichero ("" = siempre): "IsAspMvc" /
	/// "IsAspWebApi" — los Directory if=UsesAspNetMvc/WebApi del xft de
	/// Global.asax (AspNetAppProjectFlavor.IsAspMvcProject/IsAspWebApiProject).</summary>
	public string When { get; init; } = "";
	/// <summary>Cuerpo crudo (CDATA inline o src= externo, BOM incluido).</summary>
	public string Body { get; init; } = "";
}

/// <summary>Contexto de creación para las sustituciones de tags (legacy
/// FileTemplateTagsModifier.ModifyTags + el tag model del proyecto).</summary>
public sealed class NewFileTemplateContext
{
	/// <summary>Nombre del proyecto activo ("" en free-mode).</summary>
	public string ProjectName { get; init; } = "";
	/// <summary>Ruta del proyecto activo (null en free-mode): root namespace,
	/// gating de ficheros condicionales (ProjectHasReference).</summary>
	public string? ProjectPath { get; init; }
	/// <summary>Texto de la entrada Name: origen de Name/EscapedIdentifier.</summary>
	public string EntryName { get; init; } = "";
	/// <summary>Directorio de creación ("" en free-mode).</summary>
	public string Directory { get; init; } = "";
}

public sealed class NewFileTemplate
{
	public string Id { get; init; } = "";
	public string Name { get; init; } = "";
	public string Description { get; init; } = "";
	public string Language { get; init; } = "";
	/// <summary>Categoría raíz del árbol (legacy templateLanguage): "C#" / "F#" /
	/// "VB". Los templates de los addins (parcial NewFileAddinTemplates) lo
	/// fijan; los de core C#/F# también.</summary>
	public string Root { get; init; } = "C#";
	/// <summary>Gate ProjectType del xft (legacy FileTemplate.ProjectTypes vs
	/// project.GetTypeTags()): "AspNetApp" solo entra en proyectos
	/// ConfigurationService.GetProjectKind().IsAspNetApp.</summary>
	public string ProjectType { get; init; } = "";
	/// <summary>Gate de la condición custom &lt;AspNetCore/&gt; (legacy
	/// AspNetCoreFileTemplateCondition): solo proyectos IsAspNetCore.</summary>
	public bool Core { get; init; }
	/// <summary>Atributo minVersion de la condición AspNetCore (null = sin mínimo).</summary>
	public System.Version? CoreMinVersion { get; init; }
	/// <summary>Condición ParentProject ExcludedFiles del xft (":"-separado;
	/// "/" relativo a la base del proyecto, sin "/" al directorio de creación).
	/// Si cualquiera de los ficheros existe, el template se excluye.</summary>
	public string ParentProjectExcludedFiles { get; init; } = "";
	/// <summary>La condición ParentProject exige proyecto (RequireProject=True):
	/// sin proyecto el template no se muestra.</summary>
	public bool RequireProject { get; init; }
	public string IconId { get; init; } = "";
	public IImage? Icon { get; set; }
	public string DefaultFilename { get; init; } = "";
	/// <summary>Extensión por defecto del template single-fichero de core
	/// (los templates de los addins usan Files[].DefaultExtension).</summary>
	public string Extension { get; init; } = "";
	public bool FixedFilename { get; init; }
	/// <summary>Subpath del fichero (templates single de core): el legacy
	/// GetFileName lo preserva al añadir al proyecto (Assembly Information →
	/// Properties/AssemblyInfo.cs).</summary>
	public string FileSubPath { get; init; } = "";
	public bool AddStandardHeader { get; init; } = true;
	/// <summary>Cuerpo CDATA crudo del xft (templates single de core, "" para el
	/// archivo vacío). Los templates de los addins llevan Files.</summary>
	public string Body { get; init; } = "";

	/// <summary>Ficheros del template (addins); los templates single de core se
	/// componen de Body/Extension/FileSubPath/AddStandardHeader.</summary>
	public List<NewFileTemplateFile> Files { get; init; } = new ();

	public bool HasFiles => Files.Count > 0;

	/// <summary>¿El gate del template pasa para el proyecto vigente? (legacy
	/// IsValidForProject: ProjectTypes vs GetTypeTags, condiciones
	/// ShouldEnableFor) + la condición ParentProject (solo con proyecto).</summary>
	public bool IsAllowed (ConfigurationService.ProjectKindInfo? kind, string? directory, string? projectName)
	{
		if (RequireProject && kind is null)
			return false;
		if (kind is not null) {
			if (ProjectType == "AspNetApp" && !kind.IsAspNetApp)
				return false;
			if (Core) {
				if (!kind.IsAspNetCore)
					return false;
				if (CoreMinVersion is { } min && !ConfigurationService.ProjectMeetsVersion (kind.Path, min))
					return false;
			}
			if (!string.IsNullOrEmpty (ParentProjectExcludedFiles) && directory is not null) {
				var baseDir = Path.GetDirectoryName (kind.Path) ?? "";
				foreach (var f in ParentProjectExcludedFiles.Split (new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)) {
					var rooted = f.StartsWith ("/", StringComparison.Ordinal);
					var rel = rooted ? f [1..] : f;
					var full = Path.GetFullPath (rooted
						? Path.Combine (baseDir, rel)
						: Path.Combine (directory, rel));
					if (File.Exists (full))
						return false;
				}
			}
		}
		// LanguageName (legacy MatchLanguagesWithProject): los templates de los
		// addins ya llegan Root = "C#" / "VB" y solo se cuelgan de esa raíz;
		// LanguageName vacío encaja en cualquier proyecto .NET ("" ∈
		// DotNetProject.SupportedLanguages) y no añade restricción aquí.
		return true;
	}

	/// <summary>Construye el modelo de tags del fichero (legacy
	/// FileTemplateTagsModifier.ModifyTags): Name/EscapedIdentifier desde el
	/// nombre de entrada, Namespace/RootNamespace del proyecto (simplificado:
	/// solo el root, sin segmentos de subcarpeta), autor vacío, y los tags de
	/// fichero/assembly que usan los xft.</summary>
	static Dictionary<string, string> BuildTags (NewFileTemplateContext ctx, string fileName)
	{
		// Legacy: identifier = nombre de entrada sin extensiones +
		// CreateIdentifierName (solo [A-Za-z0-9_], primer carácter letra).
		var identifier = ctx.EntryName;
		while (Path.GetExtension (identifier).Length > 0)
			identifier = Path.GetFileNameWithoutExtension (identifier);
		var sb = new StringBuilder (identifier.Length);
		for (var i = 0; i < identifier.Length; i++) {
			var ch = identifier [i];
			sb.Append (i == 0 && Char.IsLetter (ch) || i > 0 && Char.IsLetterOrDigit (ch) || ch == '_' ? ch : '_');
		}
		var name = sb.ToString ();
		// EscapedIdentifier (legacy CodeDomProvider.CreateEscapedIdentifier):
		// para entradas que no son keywords C# es idéntico a Name.
		var ns = ctx.ProjectPath is not null
			? ConfigurationService.GetRootNamespace (ctx.ProjectPath)
			: string.IsNullOrEmpty (ctx.ProjectName)
				? Path.GetFileNameWithoutExtension (fileName)
				: ctx.ProjectName;
		return new Dictionary<string, string> {
			["Name"] = name,
			["EscapedIdentifier"] = name,
			["FullName"] = ns.Length > 0 ? ns + "." + name : name,
			["Namespace"] = ns,
			["RootNamespace"] = ns,
			["ProjectName"] = ctx.ProjectName ?? "",
			["SafeProjectName"] = CreateIdentifierName (ctx.ProjectName ?? ""),
			["ProjectRelativeDirectory"] = string.IsNullOrEmpty (ctx.Directory)
				? ""
				: ctx.Directory,
			["AuthorCompany"] = "",
			["AuthorCopyright"] = "",
			["AuthorTrademark"] = "",
			["AuthorName"] = "",
			["AuthorEmail"] = "",
			["CopyrightHolder"] = "",
			["AssemblyInfoVersion"] = "",
			["UseCustomAssemblyInfoVersion"] = "false",
			["FileName"] = Path.GetFileName (fileName),
			["FileNameWithoutExtension"] = Path.GetFileNameWithoutExtension (fileName),
			["Directory"] = Path.GetDirectoryName (fileName) ?? "",
			["FullFileName"] = fileName,
		};
	}

	static string CreateIdentifierName (string identifier)
	{
		var result = new StringBuilder (identifier.Length);
		for (var i = 0; i < identifier.Length; i++) {
			var ch = identifier [i];
			result.Append (i == 0 && Char.IsLetter (ch) || i > 0 && Char.IsLetterOrDigit (ch) || ch == '_' ? ch : '_');
		}
		return result.ToString ();
	}

	/// <summary>Aplica las sustituciones del legacy FileTemplateParser
	/// ($${Tag} y $if$/$else$/$endif$) al cuerpo del fichero con el modelo de
	/// tags del contexto.</summary>
	public string GenerateFile (NewFileTemplateFile file, NewFileTemplateContext ctx)
		=> TemplateText.Parse (file.Body, BuildTags (ctx, file.Name));

	/// <summary>Compat single-fichero de core: aplica las sustituciones sobre
	/// Body con el contexto.</summary>
	public string GenerateContent (NewFileTemplateContext ctx, string fileName)
		=> TemplateText.Parse (Body, BuildTags (ctx, fileName));

	/// <summary>Nombre relativo (subpath) de un fichero del template: el
	/// subpath fijo si lo lleva; si no, el atributo name resuelto (${Name}) o
	/// el nombre de entrada + extensión por defecto (legacy
	/// SingleFileDescriptionTemplate.GetFileName).</summary>
	public string GenerateFileName (NewFileTemplateFile file, NewFileTemplateContext ctx)
	{
		if (!string.IsNullOrEmpty (file.SubPath))
			return file.SubPath;
		if (!string.IsNullOrEmpty (file.Name)) {
			var n = TemplateText.Parse (file.Name, BuildTags (ctx, file.Name));
			if (Path.GetExtension (n).Length == 0 && !string.IsNullOrEmpty (file.DefaultExtension))
				n += file.DefaultExtension;
			return n;
		}
		var entry = ctx.EntryName;
		if (Path.GetExtension (entry).Length == 0 && !string.IsNullOrEmpty (file.DefaultExtension))
			entry += file.DefaultExtension;
		return entry;
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
		Root = "C#",
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
		Root = "C#",
		IconId = "md-file-information",
		DefaultFilename = "AssemblyInfo.cs",
		Extension = ".cs",
		FixedFilename = true,
		FileSubPath = "Properties/AssemblyInfo.cs",
		// Legacy xft: <ParentProject PermittedCreationPaths=""
		// ExcludedFiles="AssemblyInfo.cs" RequireProject="True"/> — sin proyecto
		// no se muestra; con proyecto, se excluye si AssemblyInfo.cs ya existe
		// en el directorio de creación.
		RequireProject = true,
		ParentProjectExcludedFiles = "AssemblyInfo.cs",
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
		Root = "F#",
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
		Root = "F#",
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
		Root = "F#",
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
		Root = "F#",
		IconId = "md-file-source",
		DefaultFilename = "AssemblyInfo.fs",
		Extension = ".fs",
		FixedFilename = true,
		FileSubPath = "AssemblyInfo.fs",
		// Legacy xft: <ParentProject ExcludedFiles="AssemblyInfo.fs"
		// RequireProject="True"/>.
		RequireProject = true,
		ParentProjectExcludedFiles = "AssemblyInfo.fs",
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

	// Raíces de lenguaje (legacy NewFileDialog: la raíz del árbol es el
	// templateLanguage). C#/F# traen sus categorías de core; VB trae las de
	// VBNetBinding (General/Misc). RebuildCatalog añade las categorías de los
	// addins (ASP.NET / ASP.NET Core) según el kind del proyecto activo.
	readonly NewFileCategory csharpRoot = new () { Name = "C#" };
	readonly NewFileCategory generalCategory = new () { Name = "General" };

	// F# category tree (legacy _Category fields of the F# xft templates):
	// General (Source/Script/Signature), Misc (Assembly Information), NUnit (Test Fixture).
	readonly NewFileCategory fsharpRoot = new () { Name = "F#" };
	readonly NewFileCategory fsharpGeneral = new () { Name = "General" };
	readonly NewFileCategory fsharpMisc = new () { Name = "Misc" };
	readonly NewFileCategory fsharpNUnit = new () { Name = "NUnit" };

	// Raíz VB (VBNetBinding: Empty File → General, Assembly Information → Misc).
	readonly NewFileCategory vbRoot = new () { Name = "VB" };
	readonly NewFileCategory vbGeneral = new () { Name = "General" };
	readonly NewFileCategory vbMisc = new () { Name = "Misc" };

	// Las raíces visibles en el árbol: C# / F# / VB (free-mode y proyectos).
	List<NewFileCategory> languageRoots () => new () { csharpRoot, fsharpRoot, vbRoot };

	// Free mode: proyectos de la solución cargada (legacy IdeApp.Workspace.GetAllProjects).
	readonly List<string> projects;
	// Fixed mode (contexto del ProjectPad): directorio del nodo + nombre del proyecto
	// (legacy new NewFileDialog(parentProject, basePath): boxProject oculto).
	readonly string? fixedDirectory;
	readonly string? fixedProjectName;
	readonly string? fixedProjectPath;
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
		: this (projectPaths, activeProjectPath, null, null, null) { }

	/// <summary>Fixed mode (legacy ProjectOperations.CreateProjectFile:
	/// new NewFileDialog(project, basePath), boxProject oculto).
	/// <paramref name="fixedProjectPath"/> (la ruta del .csproj/.vbproj) da el
	/// kind del proyecto para el gating del catálogo; null cuando el nodo no
	/// tiene proyecto (solo el directorio).</summary>
	public NewFileDialog (string fixedDirectory, string? fixedProjectName, string? fixedProjectPath = null)
		: this (null, null, fixedDirectory, fixedProjectName, fixedProjectPath) { }

	public NewFileDialog (List<string>? projectPaths, string? activeProjectPath, string? fixedDirectory, string? fixedProjectName, string? fixedProjectPath)
	{
		InitializeComponent ();
		MonoDevelop.Ide.Controls.DialogWindow.Apply (this);

		this.fixedDirectory = fixedDirectory;
		this.fixedProjectName = fixedProjectName;
		this.fixedProjectPath = fixedProjectPath;
		projects = (projectPaths ?? new List<string> ())
			.Where (p => p is not null && p.Length > 0).ToList ();

		// El legacy TemplateView pinta el stock icon a Dnd (32px); IconService sirve
		// el PNG y la celda 32x32 lo escala (mismo patrón que NewSolutionDialog).
		// Fallback legacy cuando el icono no existe: "md-file-source".
		var allTemplates = new List<NewFileTemplate> {
			EmptyFileTemplate, AssemblyInfoTemplate,
			FSharpSourceTemplate, FSharpScriptTemplate, FSharpSignatureTemplate,
			FSharpAssemblyInfoTemplate, FSharpNUnitTestTemplate,
		};
		allTemplates.AddRange (AddinTemplates ());
		foreach (var t in allTemplates)
			t.Icon = IconService.GetImage (t.IconId) ?? IconService.GetImage ("md-file-source");

		fsharpRoot.Add (fsharpGeneral);
		fsharpRoot.Add (fsharpMisc);
		fsharpRoot.Add (fsharpNUnit);

		vbRoot.Add (vbGeneral);
		vbRoot.Add (vbMisc);
		// csharpRoot.Children (General + addins) lo compone RebuildCatalog.

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

	/// <summary>Ruta del proyecto activo (free mode: el seleccionado en el combo
	/// con "Add to project"; fixed mode: el nodo del ProjectPad). Null sin
	/// proyecto → kind null → gating free-mode (solo raíces de lenguaje).</summary>
	string? ActiveProjectPath ()
	{
		if (fixedMode)
			return fixedProjectPath;
		if (ProjectBox.IsVisible && AddToProjectCheck.IsChecked == true &&
			ProjectCombo.SelectedIndex >= 0 && ProjectCombo.SelectedIndex < projects.Count)
			return projects [ProjectCombo.SelectedIndex];
		return null;
	}

	ConfigurationService.ProjectKindInfo? GetProjectKindOf (string? path)
	{
		if (path is null || !File.Exists (path))
			return null;
		return ConfigurationService.GetProjectKind (path);
	}

	/// <summary>Todos los templates de los 3 addins (parcial
	/// NewFileAddinTemplates), en orden de árbol: AspNet clásico, ASP.NET Core,
	/// VB. El gating por kind vive en NewFileTemplate.IsAllowed.</summary>
	static IEnumerable<NewFileTemplate> AddinTemplates ()
	{
		foreach (var t in AspAspNetCatWebForms)
			yield return t;
		foreach (var t in AspAspNetCatMVC)
			yield return t;
		foreach (var t in AspAspNetCat)
			yield return t;
		foreach (var t in AspTextTemplating)
			yield return t;
		foreach (var t in AspWeb)
			yield return t;
		foreach (var t in CoreAspNetCoreCat)
			yield return t;
		foreach (var t in VbGeneral)
			yield return t;
		foreach (var t in VbMisc)
			yield return t;
	}

	/// <summary>Legacy GetCategoryPropertyKey: el sufijo es el type tag + el
	/// DotNetProject.LanguageName del proyecto activo ("C#/C#" para .csproj,
	/// "C#/C#" para el web flavor net10, "VBNet/VB" para .vbproj).</summary>
	string GetCategoryPropertyKey ()
	{
		var key = "Dialogs.NewFileDialog.LastSelectedCategory";
		if (ProjectActive) {
			var kind = GetProjectKindOf (ActiveProjectPath ());
			if (kind is not null) {
				var typeTag = kind.IsVBNet ? "VBNet" : "C#";
				var lang = kind.IsVBNet ? "VB" : "C#";
				key += "." + typeTag + "." + lang;
			}
		}
		return key;
	}

	/// <summary>La raíz de lenguaje del proyecto activo ("C#" por defecto, "VB"
	/// en proyectos VBNet): es donde se guarda/restaura la última categoría.</summary>
	string ActiveRootName ()
	{
		var kind = GetProjectKindOf (ActiveProjectPath ());
		return kind is { IsVBNet: true } ? "VB" : "C#";
	}

	string LastCategoryPath
	{
		get {
			try {
				return PropertyService.Get<string> (GetCategoryPropertyKey (), ActiveRootName () + "/General");
			} catch {
				return ActiveRootName () + "/General";
			}
		}
	}

	/// <summary>Legacy GetFileTemplates + AddTemplate: el catálogo se recompute
	/// con el proyecto/contexto vigente, aplicando el gating de cada template
	/// (NewFileTemplate.IsAllowed: ProjectType/condición AspNetCore+minVersion/
	/// ParentProject). El árbol es la raíz de lenguaje del proyecto (C#/F#/VB);
	/// con proyecto AspNet clásico/ASP.NET Core se cuelgan sus categorías de
	/// addin. La condición ParentProject (RequireProject + ExcludedFiles)
	/// excluye los Assembly Information existentes y (legacy) los AssemblyInfo
	/// de C#/F# en free-mode.</summary>
	void RebuildCatalog ()
	{
		var kind = GetProjectKindOf (ActiveProjectPath ());
		var directory = ProjectActive ? effectiveDirectory : null;

		// General de C#: Empty File + Assembly Information (con gating).
		generalCategory.Templates.Clear ();
		generalCategory.Templates.Add (EmptyFileTemplate);
		if (AssemblyInfoTemplate.IsAllowed (kind, directory, effectiveProjectName))
			generalCategory.Templates.Add (AssemblyInfoTemplate);

		// F#: categorías de core (Assembly Information con gating ParentProject).
		fsharpGeneral.Templates.Clear ();
		fsharpGeneral.Templates.Add (FSharpSourceTemplate);
		fsharpGeneral.Templates.Add (FSharpScriptTemplate);
		fsharpGeneral.Templates.Add (FSharpSignatureTemplate);
		fsharpMisc.Templates.Clear ();
		fsharpNUnit.Templates.Clear ();
		if (FSharpAssemblyInfoTemplate.IsAllowed (kind, directory, effectiveProjectName))
			fsharpMisc.Templates.Add (FSharpAssemblyInfoTemplate);
		fsharpNUnit.Templates.Add (FSharpNUnitTestTemplate);

		// VB (VBNetBinding): General siempre; Misc/Assembly Information con
		// gating ParentProject (ExcludedFiles="AssemblyInfo.vb", RequireProject).
		vbGeneral.Templates.Clear ();
		vbGeneral.Templates.AddRange (VbGeneral.Where (t => t.IsAllowed (kind, directory, effectiveProjectName)));
		vbMisc.Templates.Clear ();
		vbMisc.Templates.AddRange (VbMisc.Where (t => t.IsAllowed (kind, directory, effectiveProjectName)));

		// Categorías de los addins en la raíz de lenguaje del proyecto (legacy:
		// la raíz es el templateLanguage). AspNet clásico → raíz C# cuando el
		// proyecto IsAspNetApp; ASP.NET Core → raíz C# cuando IsAspNetCore.
		csharpRoot.Children.Clear ();
		csharpRoot.Add (generalCategory);
		if (kind is { IsAspNetApp: true })
			foreach (var arr in new[] { AspAspNetCatWebForms, AspAspNetCatMVC, AspAspNetCat, AspWeb, AspTextTemplating })
				AddCategoryTo (csharpRoot, arr.First ().Name, arr.Where (t => t.IsAllowed (kind, directory, effectiveProjectName)));
		if (kind is { IsAspNetCore: true })
			AddCategoryTo (csharpRoot, "ASP.NET Core",
				CoreAspNetCoreCat.Where (t => t.IsAllowed (kind, directory, effectiveProjectName)));

		// Legacy InsertCategories: las categorías sin templates no aparecen en
		// el árbol (free-mode: F#/Misc sin Assembly Info, VB/Misc sin proyecto).
		fsharpRoot.Children.Clear ();
		fsharpRoot.Add (fsharpGeneral);
		if (fsharpMisc.Templates.Count > 0)
			fsharpRoot.Add (fsharpMisc);
		fsharpRoot.Add (fsharpNUnit);

		vbRoot.Children.Clear ();
		vbRoot.Add (vbGeneral);
		if (vbMisc.Templates.Count > 0)
			vbRoot.Add (vbMisc);

		rebuilding = true;
		try {
			CategoryTree.ItemsSource = languageRoots ();
		} finally {
			rebuilding = false;
		}
		// The FuncTreeDataTemplate creates fresh TextBlocks from English msgids
		// on every rebuild; translate them before they paint.
		MonoDevelop.Ide.Controls.XamlTranslation.Translate (CategoryTree);
	}

	/// <summary>Añade la categoría (con sus templates) a la raíz si no está
	/// vacía; reutiliza la instancia existente si la había (mantiene la
	/// selección estable entre rebuilds).</summary>
	void AddCategoryTo (NewFileCategory root, string name, IEnumerable<NewFileTemplate> templates)
	{
		var ts = templates.ToList ();
		if (ts.Count == 0)
			return;
		var cat = root.Children.FirstOrDefault (c => c.Name == name);
		if (cat is null) {
			cat = new NewFileCategory { Name = name };
			root.Add (cat);
		}
		cat.Templates.Clear ();
		cat.Templates.AddRange (ts);
	}

	NewFileCategory? FindCategory (string catPath)
	{
		// The tree root is a language (C# / F# / VB); the path is "Language/Category...".
		var parts = catPath.Split ('/');
		if (parts.Length == 0)
			return null;
		var root = languageRoots ().FirstOrDefault (r => r.Name == parts [0]);
		if (root is null)
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
	/// (PropertyService, por defecto "General"), si no la primera de la raíz
	/// activa. La raíz por defecto es la del proyecto (C# por defecto, VB en
	/// proyectos VBNet).</summary>
	void InitializeView ()
	{
		var target = FindCategory (LastCategoryPath) ?? FindCategory (ActiveRootName () + "/General")
			?? csharpRoot;
		CategoryTree.SelectedItem = target;
	}

	/// <summary>Tras un RebuildCatalog la selección queda vacía: la legacy
	/// InitializeView se vuelve a ejecutar y SelectTemplate re-elige la plantilla.</summary>
	void RestoreCategorySelection ()
	{
		if (CategoryTree.SelectedItem is NewFileCategory)
			return;
		var target = FindCategory (LastCategoryPath) ?? FindCategory (ActiveRootName () + "/General")
			?? csharpRoot;
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
		MonoDevelop.Ide.Controls.XamlTranslation.Translate (TemplateList);
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

		MonoDevelop.Ide.Controls.XamlTranslation.SetText (TemplateTitle, item.Name);
		MonoDevelop.Ide.Controls.XamlTranslation.SetText (TemplateDescription, item.Description);

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
	/// contiene el template y deja seleccionado el template en la lista
	/// (recorre las 3 raíces de lenguaje).</summary>
	bool SelectTemplateById (string id)
	{
		NewFileCategory? cat = null;
		System.Func<NewFileCategory, bool>? walk = null;
		// The tree root is a language (C# / F# / VB); the template lives under one of them.
		walk = c => {
			if (c.Templates.Any (t => t.Id == id)) {
				cat = c;
				return true;
			}
			return c.Children.Any (walk!);
		};

		if (languageRoots ().FirstOrDefault (root => walk (root)) is null) {
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
		var ctx = new NewFileTemplateContext {
			ProjectName = projectName,
			ProjectPath = ProjectActive ? ActiveProjectPath () : null,
			EntryName = name,
			Directory = directory ?? "",
		};

		// Legacy FileTemplate.Create: itera los ficheros del template (los
		// single-fichero de core se componen del nombre de entrada + su
		// extensión por defecto).
		List<NewFileTemplateFile> files;
		if (item.HasFiles) {
			files = item.Files.Where (f => FileAllowed (f, ctx)).ToList ();
			if (files.Count == 0)
				return;
		} else {
			// Legacy GetFileName: el subpath fijo (FileSubPath) manda; si no, el
			// nombre de la entrada + la extensión por defecto si no llevaba.
			string relPath;
			if (item.FixedFilename && !string.IsNullOrEmpty (item.FileSubPath))
				relPath = item.FileSubPath;
			else if (Path.GetExtension (name).Length == 0 && !string.IsNullOrEmpty (item.Extension))
				relPath = name + item.Extension;
			else
				relPath = name;
			files = new List<NewFileTemplateFile> {
				new () { SubPath = relPath, AddStandardHeader = item.AddStandardHeader, Body = item.Body }
			};
		}

		if (directory is not null) {
			string? first = null;
			try {
				foreach (var f in files) {
					var relPath = item.HasFiles ? item.GenerateFileName (f, ctx) : f.SubPath;
					// Legacy CreateFileContent: BOM UTF8 + (standard header si
					// AddStandardHeader) + contenido con las sustituciones.
					var header = f.AddStandardHeader ? GetStandardHeader (relPath) : "";
					var content = item.HasFiles ? item.GenerateFile (f, ctx) : item.GenerateContent (ctx, relPath);
					var target = Path.GetFullPath (Path.Combine (directory, relPath.Replace ('/', Path.DirectorySeparatorChar)));
					Directory.CreateDirectory (Path.GetDirectoryName (target)!);
					// Legacy SingleFileDescriptionTemplate: BOM UTF8 siempre (el
					// StreamWriter emite el preamble del UTF8Encoding(true));
					// GetBytes() no lo emitiría.
					using (var sw = new StreamWriter (target, false, new UTF8Encoding (true)))
						sw.Write (header + content);
					if (first is null)
						first = target;
				}
				CreatedFilePath = first;
			} catch (Exception ex) {
				// Legacy: MessageService.ShowError y el diálogo se queda abierto.
				MainWindow.Instance?.Output ("[newfile] error creating file: " + ex.Message);
				return;
			}
		} else {
			// Free-mode: documento unsaved (solo llegan aquí los single-fichero de
			// core: los templates de los addins están gateados a proyecto).
			var f = files [0];
			var header = f.AddStandardHeader ? GetStandardHeader (f.SubPath) : "";
			UnsavedFileName = f.SubPath;
			UnsavedContent = header + item.GenerateContent (ctx, f.SubPath);
		}
		Close ();
	}

	// Legacy GlobalAsax <Directory if="UsesAspNetMvc"/UsesAspNetWebApi">: los
	// ficheros App_Start se crean solo si el proyecto referencia la assembly
	// correspondiente (AspNetAppProjectFlavor.IsAspMvcProject/IsAspWebApiProject,
	// mirror ProjectHasReference).
	static bool FileAllowed (NewFileTemplateFile file, NewFileTemplateContext ctx)
	{
		if (file.When.Length == 0)
			return true;
		if (ctx.ProjectPath is null || !File.Exists (ctx.ProjectPath))
			return false;
		return file.When switch {
			"IsAspMvc" => ConfigurationService.IsMvcClassicProject (ctx.ProjectPath),
			"IsAspWebApi" => ConfigurationService.ProjectHasReference (ctx.ProjectPath, "System.Web.Http.WebHost"),
			_ => false,
		};
	}

	void OnCancel (object? sender, RoutedEventArgs e) => Close ();

	/// <summary>QA: preselecciona una template por id (legacy SelectTemplate(id)).</summary>
	public string? TemplateForQa { get; set; }
	/// <summary>QA: nombre a fijar después de la selección (camino de edición de usuario).</summary>
	public string? NameForQa { get; set; }
	/// <summary>QA: fuerza el estado del checkbox "Add to project" (solo free mode).</summary>
	public bool? AddToProjectForQa { get; set; }
	/// <summary>QA: ruta del proyecto a seleccionar en el combo (solo free mode).</summary>
	public string? ProjectForQa { get; set; }
	/// <summary>QA: auto-acepta al abrir (ciclos deterministas --newfile/--ctxnewfile).</summary>
	public bool ConfirmForQa { get; set; }

	/// <summary>QA: switch template/category and dump painted text, to verify
	/// gettext survives a dialog option change.</summary>
	public void QaSwitchAndDump (string templateId)
	{
		SelectTemplateById (templateId);
		Avalonia.Threading.Dispatcher.UIThread.RunJobs ();
		var texts = new List<string> ();
		void Walk (Avalonia.Visual v, List<string> acc)
		{
			if (v is TextBlock { Text.Length: > 0 } tb)
				acc.Add (tb.Text);
			foreach (var c in Avalonia.VisualTree.VisualExtensions.GetVisualChildren (v))
				Walk (c, acc);
		}
		Walk (CategoryTree, texts);
		Walk (TemplateList, texts);
		MainWindow.Instance?.Output ($"[newfile-qa-lang] template={templateId} cat={(CategoryTree.SelectedItem as NewFileCategory)?.Name} title='{TemplateTitle.Text}' desc='{TemplateDescription.Text}' texts=[{string.Join ("|", texts)}]");
		Close ();
	}

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
		if (TemplateForQa is null && NameForQa is null && !ConfirmForQa && AddToProjectForQa is null && ProjectForQa is null)
			return;
		// Orden legacy: estado del proyecto → catálogo → template → nombre → OK.
		if (AddToProjectForQa is not null && ProjectBox.IsVisible &&
			(AddToProjectCheck.IsChecked == true) != AddToProjectForQa == true)
			AddToProjectCheck.IsChecked = AddToProjectForQa == true;
		if (ProjectForQa is not null && ProjectBox.IsVisible) {
			var idx = projects.IndexOf (ProjectForQa);
			if (idx >= 0) {
				if (idx != ProjectCombo.SelectedIndex)
					ProjectCombo.SelectedIndex = idx;
				else
					ApplyProjectComboSelection ();
			}
		}
		if (TemplateForQa is not null)
			SelectTemplateById (TemplateForQa);
		if (NameForQa is not null)
			NameBox.Text = NameForQa;
		UpdateOkStatus ();
		MainWindow.Instance?.Output ($"[newfile-qa] roots={string.Join (",", languageRoots ().Select (r => r.Name + ":" + r.Children.Count))} " +
			$"cat={(CategoryTree.SelectedItem as NewFileCategory)?.Name} " +
			$"tmplCount={TemplateList.Items.Count} selTmpl={(TemplateList.SelectedItem as NewFileTemplate)?.Id} " +
			$"title='{TemplateTitle.Text}' desc='{TemplateDescription.Text}' name='{NameBox.Text}' okEnabled={NewButton.IsEnabled}");
		if (ConfirmForQa)
			OnNew (this, new RoutedEventArgs ());
	}
}
