using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Reads and persists configurations exactly where the legacy ProjectService
/// stores them: SolutionConfigurationPlatforms/ProjectConfigurationPlatforms in
/// the .sln global section, and PropertyGroup Condition entries in each .csproj
/// ('Name|Platform'). "Any CPU" maps to the MSBuild platform "AnyCPU" in project
/// files and "Any CPU" in the solution file (the legacy ItemConfiguration mapping).
/// Oleada 4: the .sln side runs on the real legacy model
/// (MonoDevelop.Projects.MSBuild.SlnFile — the same reader/writer the GTK
/// ProjectService uses; non-GTK, no Mono.Addins), so the shell never hand-edits
/// solution file lines. A no-op Read+Write round-trip reproduces the file
/// byte-identical (format info — BOM/newlines — is preserved by the model).
/// </summary>
public static class ConfigurationService
{
	public const string AnyCpuSolution = "Any CPU";
	public const string AnyCpuProject = "AnyCPU";

	// C# project type GUID (the FAE04EC0 the legacy CSharpBinding registers),
	// the F# project type GUID (what dotnet sln writes when adding an .fsproj)
	// and the solution folder type GUID: folders reference no project file and
	// the legacy writer skips them for build configurations (SupportsBuild == false).
	// SlnFile stores TypeGuid verbatim as the quoted text of the Project line —
	// braces included, exactly as the .sln format writes it.
	const string CSharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
	const string FSharpProjectTypeGuid = "{F2A71F9B-5D33-465A-A702-920D77279786}";
	const string SolutionFolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D4EEDE}";
	// The other GUID spelling real .sln files carry (the one the legacy
	// MSBuildProjectService.FolderTypeGuid uses): both mark a solution folder.
	const string SolutionFolderTypeGuidAlt = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

	static string ProjectTypeGuidFor (string projPath)
		=> projPath.EndsWith (".fsproj", StringComparison.OrdinalIgnoreCase)
			? FSharpProjectTypeGuid
			: projPath.EndsWith (".vbproj", StringComparison.OrdinalIgnoreCase)
			? VbProjectTypeGuid
			: CSharpProjectTypeGuid;

	static SlnFile ReadSln (string slnPath)
	{
		if (!File.Exists (slnPath))
			throw new FileNotFoundException (slnPath);
		var sln = new SlnFile ();
		sln.Read (slnPath);
		return sln;
	}

	static bool IsSolutionFolder (SlnProject p)
		=> p.TypeGuid != null
			&& (p.TypeGuid.Equals (SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase)
				|| p.TypeGuid.Equals (SolutionFolderTypeGuidAlt, StringComparison.OrdinalIgnoreCase));

	static string ResolveProjectPath (string slnPath, string relPath)
	{
		var dir = Path.GetDirectoryName (slnPath)!;
		return Path.GetFullPath (Path.Combine (dir, relPath.Replace ('\\', '/')));
	}

	/// <summary>Solution configuration ids, e.g. "Debug|Any CPU" keys minus the
	/// platform → "Debug", "Debug|x86" (legacy ItemConfigurationCollection keys).</summary>
	public static IReadOnlyList<string> GetSolutionConfigurations (string slnPath)
	{
		var result = new List<string> ();
		if (!File.Exists (slnPath))
			return result;
		// Real model: the SolutionConfigurationPlatforms section of the .sln.
		foreach (var key in ReadSln (slnPath).SolutionConfigurationsSection.Keys) {
			var bar = key.IndexOf ('|');
			var name = bar < 0 ? key : key.Substring (0, bar);
			if (!result.Contains (name, StringComparer.OrdinalIgnoreCase))
				result.Add (name);
		}
		return result;
	}

	/// <summary>True for the project file extensions the model recognizes —
	/// .csproj, .fsproj (the F# SDK-style project, Phase 3 of the F# port: the
	/// project model and build/run gates see F# projects the same way as C#)
	/// and .vbproj (the VB(.NET) port: the ProjectPad node classification, the
	/// build-target resolution and the rename/delete guards see VB projects the
	/// same way as C#/F#).</summary>
	public static bool IsProjectFile (string path)
	{
		if (string.IsNullOrEmpty (path))
			return false;
		return path.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith (".fsproj", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith (".vbproj", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Absolute paths of the .csproj/.fsproj projects referenced by the .sln
	/// (real model: SlnFile.Projects, paths resolved against the solution
	/// directory). A directory scan is only a fallback for slns that yield
	/// nothing — a referenced project may live outside the solution dir.</summary>
	public static List<string> GetSolutionProjects (string slnPath)
	{
		var slnDir = Path.GetDirectoryName (slnPath)!;
		var projs = new List<string> ();
		try {
			foreach (var p in ReadSln (slnPath).Projects) {
				if (p.FilePath is null || !IsProjectFile (p.FilePath))
					continue;
				var full = ResolveProjectPath (slnPath, p.FilePath);
				if (File.Exists (full))
					projs.Add (full);
			}
		} catch (Exception) {
			projs.Clear ();
		}
		if (projs.Count == 0)
			projs = Directory.GetFiles (slnDir, "*.csproj", SearchOption.AllDirectories)
				.Concat (Directory.GetFiles (slnDir, "*.fsproj", SearchOption.AllDirectories))
				.Where (p => !p.Contains ("/obj/") && !p.Contains ("/bin/"))
				.ToList ();
		return projs;
	}

	/// <summary>Entries for the solution pad tree — projects AND solution
	/// folders, in .sln file order, with the NestedProjects parent relation —
	/// read through the real <see cref="SlnFile"/> model. Returns null when the
	/// .sln cannot be read at all; an (empty) list otherwise. Deliberately NOT
	/// SolutionLoader.Load: Microsoft.Build's SolutionFile.Parse recurses
	/// through parent GUIDs and StackOverflows (uncatchable; SIGABRTs the
	/// whole process) when the NestedProjects section declares a parent cycle,
	/// while the SlnFile reader is line-based and tolerates that corruption.</summary>
	public static List<SolutionEntry>? GetSolutionEntries (string slnPath)
	{
		SlnFile sln;
		try {
			sln = ReadSln (slnPath);
		} catch (Exception ex) {
			Console.WriteLine ("[solution] GetSolutionEntries failed: " + ex.Message);
			return null;
		}

		try {
			return BuildEntriesFromSln (sln, slnPath);
		} catch (Exception ex) {
			Console.WriteLine ("[solution] GetSolutionEntries failed: " + ex.Message);
			return null;
		}
	}

	static List<SolutionEntry> BuildEntriesFromSln (SlnFile sln, string slnPath)
	{
		// NestedProjects: "child-guid = parent-guid" (braced GUIDs, the same
		// shape SlnProject.Id carries).
		var parentOf = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		var nested = sln.Sections.GetSection ("NestedProjects");
		if (nested is not null) {
			foreach (var kv in nested.Properties) {
				var child = kv.Key.Trim ();
				var parent = kv.Value.Trim ();
				if (child.Length > 0 && parent.Length > 0)
					parentOf [child] = parent;
			}
		}

		var firstPass = new List<SlnProject> ();
		var guidToName = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		foreach (var p in sln.Projects) {
			if (p.Id is not null)
				guidToName [p.Id] = p.Name ?? string.Empty;
			firstPass.Add (p);
		}

		var entries = new List<SolutionEntry> (firstPass.Count);
		foreach (var p in firstPass) {
			var isFolder = IsSolutionFolder (p);
			if (!isFolder && string.IsNullOrEmpty (p.FilePath))
				continue; // not a real project entry (mirrors SolutionLoader.Load)

			string? parentGuid = null, parentName = null;
			if (p.Id is not null && parentOf.TryGetValue (p.Id, out var pg)) {
				parentGuid = pg;
				if (guidToName.TryGetValue (pg, out var pn))
					parentName = pn;
			}

			entries.Add (new SolutionEntry (
				p.Name ?? string.Empty,
				isFolder ? string.Empty : ResolveProjectPath (slnPath, p.FilePath!),
				isFolder,
				parentName,
				p.Id,
				parentGuid));
		}
		return entries;
	}

	/// <summary>Creates the configuration in the .sln (solution config + project
	/// config mappings for every project GUID) and in each project's .csproj
	/// (PropertyGroup Condition), like ProjectOperations.AddConfiguration.
	/// Returns false when the config already exists.</summary>
	public static bool AddSolutionConfiguration (string slnPath, string name, string platformDisplayName, bool createChildren)
	{
		var sln = ReadSln (slnPath);
		var platform = platformDisplayName == AnyCpuSolution ? AnyCpuSolution : platformDisplayName;
		var slnId = $@"{name}|{platform}";
		var projPlatform = platform == AnyCpuSolution ? AnyCpuProject : platform;
		var projId = $@"{name}|{projPlatform}";

		// The real "already exists" test (legacy ProjectOperations.AddConfiguration):
		// the solution configuration id, or a per-project ActiveCfg entry for it.
		if (sln.SolutionConfigurationsSection.ContainsKey (slnId))
			return false;
		foreach (var set in sln.ProjectConfigurationsSection)
			if (set.ContainsKey ($"{projId}.ActiveCfg"))
				return false;

		sln.SolutionConfigurationsSection [slnId] = slnId;
		foreach (var p in sln.Projects) {
			if (IsSolutionFolder (p))
				continue; // folders don't support build configurations
			var set = sln.ProjectConfigurationsSection.GetOrCreatePropertySet (p.Id, true);
			set [$"{projId}.ActiveCfg"] = projId;
			set [$"{projId}.Build.0"] = projId;
		}
		sln.Write (slnPath);

		if (createChildren)
			foreach (var p in sln.Projects.Where (p => !IsSolutionFolder (p) && p.FilePath != null
				&& p.FilePath.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase)))
				AddProjectConfiguration (ResolveProjectPath (slnPath, p.FilePath), name, projPlatform);
		return true;
	}

	/// <summary>Adds &lt;PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Name|AnyCPU' " /&gt;
	/// (plus OutputPath/DebugType defaults like the legacy templates) to the .csproj.</summary>
	public static bool AddProjectConfiguration (string csprojPath, string name, string platform)
	{
		if (!File.Exists (csprojPath))
			return false;
		var text = File.ReadAllText (csprojPath);
		var cond = $" '$(Configuration)|$(Platform)' == '{name}|{platform}' ";
		if (text.Contains (cond, StringComparison.OrdinalIgnoreCase))
			return false;
		int insertAt = text.LastIndexOf ("</Project>", StringComparison.Ordinal);
		if (insertAt < 0)
			return false;
		var sb = new StringBuilder (text);
		var block = $"\n  <PropertyGroup Condition=\"{cond}\" />\n";
		sb.Insert (insertAt, block);
		File.WriteAllText (csprojPath, sb.ToString (), Encoding.UTF8);
		return true;
	}

	/// <summary>Active configuration persisted like the legacy RootWorkspace:
	/// the &lt;sln&gt;.userprefs 'Workspace' data item ActiveConfiguration property.
	/// Reading falls back to the first available configuration.</summary>
	public static string GetActiveConfiguration (string slnPath)
	{
		var configs = GetSolutionConfigurations (slnPath);
		var prefs = slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
		if (File.Exists (prefs)) {
			try {
				var doc = System.Xml.Linq.XDocument.Load (prefs);
				var stored = doc.Descendants ("Property")
					.Where (p => (string?)p.Attribute ("name") == "ActiveConfiguration")
					.Select (p => (string?)p.Attribute ("value"))
					.FirstOrDefault ();
				if (stored is not null && configs.Contains (stored, StringComparer.OrdinalIgnoreCase))
					return configs.First (c => c.Equals (stored, StringComparison.OrdinalIgnoreCase));
			} catch { /* corrupt prefs: fall through to the default */ }
		}
		return configs.Count > 0 ? configs [0] : "Debug";
	}

	/// <summary>Stores the active configuration in &lt;sln&gt;.userprefs with the
	/// legacy shape (&lt;Properties&gt;&lt;MonoDevelop.Ide.Workspace&gt;&lt;Property
	/// name="ActiveConfiguration" value="…" /&gt;).</summary>
	public static void SetActiveConfiguration (string slnPath, string configuration)
	{
		var prefs = slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
		System.Xml.Linq.XDocument doc;
		if (File.Exists (prefs)) {
			try {
				doc = System.Xml.Linq.XDocument.Load (prefs);
			} catch {
				doc = new System.Xml.Linq.XDocument (new System.Xml.Linq.XElement ("Properties"));
			}
		} else
			doc = new System.Xml.Linq.XDocument (new System.Xml.Linq.XElement ("Properties"));
		var root = doc.Root!;
		var ws = root.Element ("MonoDevelop.Ide.Workspace");
		if (ws is null) {
			ws = new System.Xml.Linq.XElement ("MonoDevelop.Ide.Workspace");
			root.Add (ws);
		}
		var prop = ws.Elements ("Property").FirstOrDefault (p => (string?)p.Attribute ("name") == "ActiveConfiguration");
		if (prop is null) {
			prop = new System.Xml.Linq.XElement ("Property");
			prop.SetAttributeValue ("name", "ActiveConfiguration");
			ws.Add (prop);
		}
		prop.SetAttributeValue ("value", configuration);
		doc.Save (prefs);
	}

	/// <summary>Stores the active target framework in &lt;sln&gt;.userprefs as
	/// &lt;MonoDevelop.Ide.Workspace&gt;&lt;Property name="PreferredExecutionTarget"
	/// value="…"/&gt; — the legacy MainToolbarController persists the selected
	/// runtime target per workspace the same way (TargetFrameworkExecutionTarget
	/// Id for a multi-TFM .NET project).</summary>
	public static void SetPreferredExecutionTarget (string slnPath, string targetFramework)
	{
		var prefs = slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
		System.Xml.Linq.XDocument doc;
		if (File.Exists (prefs)) {
			try {
				doc = System.Xml.Linq.XDocument.Load (prefs);
			} catch {
				doc = new System.Xml.Linq.XDocument (new System.Xml.Linq.XElement ("Properties"));
			}
		} else
			doc = new System.Xml.Linq.XDocument (new System.Xml.Linq.XElement ("Properties"));
		var root = doc.Root!;
		var ws = root.Element ("MonoDevelop.Ide.Workspace");
		if (ws is null) {
			ws = new System.Xml.Linq.XElement ("MonoDevelop.Ide.Workspace");
			root.Add (ws);
		}
		var prop = ws.Elements ("Property").FirstOrDefault (p => (string?)p.Attribute ("name") == "PreferredExecutionTarget");
		if (prop is null) {
			prop = new System.Xml.Linq.XElement ("Property");
			prop.SetAttributeValue ("name", "PreferredExecutionTarget");
			ws.Add (prop);
		}
		prop.SetAttributeValue ("value", targetFramework);
		doc.Save (prefs);
	}

	/// <summary>Reads the persisted active target framework (see
	/// <see cref="SetPreferredExecutionTarget"/>); null when absent or when it is
	/// not one of the project's declared target frameworks.</summary>
	public static string? GetPreferredExecutionTarget (string slnPath, IReadOnlyList<string> targetFrameworks)
	{
		if (targetFrameworks.Count == 0)
			return null;
		var prefs = slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";
		if (!File.Exists (prefs))
			return null;
		try {
			var doc = System.Xml.Linq.XDocument.Load (prefs);
			var stored = doc.Descendants ("Property")
				.Where (p => (string?)p.Attribute ("name") == "PreferredExecutionTarget")
				.Select (p => (string?)p.Attribute ("value"))
				.FirstOrDefault ();
			if (stored is not null)
				return targetFrameworks.FirstOrDefault (tf => tf.Equals (stored, StringComparison.OrdinalIgnoreCase));
		} catch { /* corrupt prefs: no persisted target */ }
		return null;
	}

	/// <summary>Adds an existing .csproj/.fsproj into an existing .sln (legacy
	/// ProjectOperations.AddSolutionItem): Project entry with a fresh GUID, the
	/// per-language project type GUID (C# vs F#), plus the ActiveCfg/Build.0
	/// mappings per existing configuration.</summary>
	public static void AppendProjectToSolution (string csprojPath, string slnPath)
	{
		var sln = ReadSln (slnPath);
		var guid = Guid.NewGuid ().ToString ("B").ToUpperInvariant (); // braced, as the .sln format stores it
		var projName = Path.GetFileNameWithoutExtension (csprojPath);
		var projRel = Path.GetRelativePath (Path.GetDirectoryName (slnPath)!, csprojPath).Replace ('\\', '/');
		sln.Projects.Add (new SlnProject {
			TypeGuid = ProjectTypeGuidFor (csprojPath),
			Name = projName,
			FilePath = projRel,
			Id = guid
		});
		// Project-level mappings use the MSBuild platform (AnyCPU), like the legacy
		// ItemConfiguration mapping for the "Any CPU" solution platform. A solution
		// without configurations gets the Debug/Release defaults (previous behavior).
		var cfgs = GetSolutionConfigurations (slnPath).ToList ();
		if (cfgs.Count == 0)
			cfgs = new List<string> { "Debug", "Release" };
		var set = sln.ProjectConfigurationsSection.GetOrCreatePropertySet (guid, true);
		foreach (var cfg in cfgs) {
			set [$"{cfg}|AnyCPU.ActiveCfg"] = $@"{cfg}|AnyCPU";
			set [$"{cfg}|AnyCPU.Build.0"] = $@"{cfg}|AnyCPU";
		}
		sln.Write (slnPath);
	}

	/// <summary>Imports a loose .csproj as a new .sln next to it (legacy
	/// ProjectOperations.ImportProject → creates the solution file with one
	/// project entry, its GUID, and Debug/Release config mappings). Returns the
	/// created .sln path. The file is produced by the legacy SlnFile writer with
	/// the VS2012 format (12.00) and its product comment, like SlnFileFormat.</summary>
	public static string AddProjectToSolution (string csprojPath, string slnPath)
	{
		var guid = Guid.NewGuid ().ToString ("B").ToUpperInvariant (); // braced, as the .sln format stores it
		var projName = Path.GetFileNameWithoutExtension (csprojPath);
		var projRel = Path.GetRelativePath (Path.GetDirectoryName (slnPath)!, csprojPath).Replace ('\\', '/');

		var sln = new SlnFile {
			FileName = slnPath,
			FormatVersion = MSBuildFileFormat.DefaultFormat.SlnVersion,
			ProductDescription = MSBuildFileFormat.DefaultFormat.ProductDescriptionComment
		};
		sln.Projects.Add (new SlnProject {
			TypeGuid = ProjectTypeGuidFor (csprojPath),
			Name = projName,
			FilePath = projRel,
			Id = guid
		});
		foreach (var cfg in new[] { "Debug", "Release" }) {
			sln.SolutionConfigurationsSection [$@"{cfg}|Any CPU"] = $@"{cfg}|Any CPU";
			var set = sln.ProjectConfigurationsSection.GetOrCreatePropertySet (guid, true);
			set [$"{cfg}|AnyCPU.ActiveCfg"] = $@"{cfg}|AnyCPU";
			set [$"{cfg}|AnyCPU.Build.0"] = $@"{cfg}|AnyCPU";
		}
		var props = sln.Sections.GetOrCreateSection ("SolutionProperties", SlnSectionType.PreProcess);
		props.Properties ["HideSolutionNode"] = "FALSE";
		sln.Write (slnPath);
		return slnPath;
	}

	// =====================================================================
	// Oleada 4 (slice 2): modelo de proyecto .csproj.
	//
	// El modelo real legacy (MonoDevelop.Projects.MSBuild.MSBuildProject)
	// NO se puede instanciar sin Mono.Addins: el constructor de
	// WorkspaceObject (base de todo proyecto) llama a
	// AddinManager.CreateExtensionContext() y los constructores estáticos de
	// MSBuildProjectService/ProjectService leen extension points vía
	// AddinManager.GetExtensionNodes/GetExtensionObjects — en la shell
	// Avalonia (que no inicializa el motor Mono.Addins) lanzan
	// InvalidOperationException("Add-in engine not initialized."). Verificado
	// contra las fuentes de main/external/mono-addins (AddinEngine.
	// CheckInitialized) y MonoDevelop.Projects/WorkspaceObject.cs +
	// MSBuildProjectService.cs. Por eso la shell mantiene su propio modelo
	// ligero de proyecto sobre el XML del .csproj: los mismos datos que
	// consume el pad (árbol de dependencias) y Build/Run (TFM, OutputType,
	// configuraciones, referencias), sin motor MSBuild y sin Mono.Addins.
	// =====================================================================

	/// <summary>Modelo de un proyecto .csproj (nivel XML, SDK-style): TFM,
	/// tipo de salida, nombre, rutas de salida, configuraciones, referencias
	/// (framework/NuGet/ensamblaje/proyecto) y conteo de items declarados en
	/// el fichero. Fichero no legible → null (tolerancia legacy: un proyecto
	/// roto no rompe el pad).</summary>
	public sealed class ProjectFileModel
	{
		public string Path { get; }
		/// <summary>AssemblyName, y si no RootNamespace, y si no el nombre del
		/// fichero sin extensión.</summary>
		public string Name { get; }
		/// <summary>&lt;OutputType&gt; ("Exe", "WinExe", "Library"...) o null si
		/// no se declara (asume lib, como MSBuild).</summary>
		public string? OutputType { get; }
		/// <summary>Console/GUI executable (OutputType Exe/WinExe, case-insensitive:
		/// MSBuild treats the value case-insensitively and hand-written files may
		/// carry an unusual casing).</summary>
		public bool IsExecutable =>
			string.Equals (OutputType, "Exe", StringComparison.OrdinalIgnoreCase)
			|| string.Equals (OutputType, "WinExe", StringComparison.OrdinalIgnoreCase);
		/// <summary>&lt;TargetFrameworks&gt; (orden y sin duplicados) o el único
		/// &lt;TargetFramework&gt;; vacío si el proyecto no declara TFM.</summary>
		public IReadOnlyList<string> TargetFrameworks { get; }
		public string? OutputPath { get; }
		public string? BaseOutputPath { get; }
		/// <summary>Nombres de configuración leídos de los
		/// &lt;PropertyGroup Condition=&quot;'$(Configuration)'=='X'&quot;&gt;;
		/// Debug/Release si el fichero no declara ninguna.</summary>
		public IReadOnlyList<string> Configurations { get; }
		public IReadOnlyList<string> FrameworkReferences { get; }
		public IReadOnlyList<string> PackageReferences { get; }
		public IReadOnlyList<string> AssemblyReferences { get; }
		/// <summary>ProjectReference: ruta absoluta si el fichero existe, si no
		/// el valor de Include literal.</summary>
		public IReadOnlyList<string> ProjectReferences { get; }
		/// <summary>Oleada 4 (slice F): reference items editables (los tres
		/// tipos que edita el diálogo Add Reference) en orden de documento,
		/// con el Include RAW (un ProjectReference conserva su include
		/// relativo, nunca resuelto). Distinto de las listas resueltas que
		/// consume el árbol de dependencias.</summary>
		public IReadOnlyList<ReferenceItem> References { get; }
		/// <summary>Conteo de items DECLARADOS en el fichero (no evaluados por
		/// MSBuild): Compile/None/Content/EmbeddedResource, solo los > 0.</summary>
		public IReadOnlyDictionary<string, int> ItemCounts { get; }
		/// <summary>Ronda 6 (BUG 3): los <c>CustomCommand</c> de
		/// <c>Type="Custom"</c> declarados en el fichero (los que el menú
		/// Project Commands &gt; Custom command list lista y ejecuta). Vacío si
		/// el proyecto no declara ninguno — el mismo resultado que la GTK
		/// cuando <c>conf.CustomCommands</c> no tiene entrada Custom.</summary>
		public IReadOnlyList<CustomCommandEntry> CustomCommands { get; }

		internal ProjectFileModel (string path, string name, string? outputType,
			IReadOnlyList<string> targetFrameworks, string? outputPath, string? baseOutputPath,
			IReadOnlyList<string> configurations, IReadOnlyList<string> frameworkRefs,
			IReadOnlyList<string> packageRefs, IReadOnlyList<string> assemblyRefs,
			IReadOnlyList<string> projectRefs, IReadOnlyList<ReferenceItem> references,
			IReadOnlyDictionary<string, int> itemCounts,
			IReadOnlyList<CustomCommandEntry> customCommands)
		{
			Path = path;
			Name = name;
			OutputType = outputType;
			TargetFrameworks = targetFrameworks;
			OutputPath = outputPath;
			BaseOutputPath = baseOutputPath;
			Configurations = configurations;
			FrameworkReferences = frameworkRefs;
			PackageReferences = packageRefs;
			AssemblyReferences = assemblyRefs;
			ProjectReferences = projectRefs;
			References = references;
			ItemCounts = itemCounts;
			CustomCommands = customCommands;
		}
	}

	/// <summary>Oleada 4 (slice F): one editable reference item (the three
	/// kinds the legacy Add Reference dialog edits). <see cref="Include"/> is
	/// the RAW value declared in the file (a ProjectReference keeps its
	/// relative include, never resolved); <see cref="Version"/> only applies
	/// to PackageReference; <see cref="HintPath"/> only to Reference
	/// (display — the save does not write it, like the legacy rewrite).</summary>
	public sealed record ReferenceItem (string Kind, string Include, string? Version = null, string? HintPath = null);

	/// <summary>Ronda 6 (BUG 3): one legacy <c>CustomCommand</c> of
	/// <c>Type="Custom"</c> read straight from the .csproj (the Project
	/// Commands &gt; Custom command list). Reduced to the properties the
	/// execution layer needs — <c>Name</c> is the menu label and the
	/// <c>Command</c> string the legacy <c>CustomCommand.ParseCommand</c>
	/// splits into file + args ("\"exe arg\" rest" → exe="exe arg",
	/// args="rest"; otherwise the first whitespace token). <c>WorkingDir</c>
	/// is the raw tag value (null = the project directory); the
	/// <c>ExternalConsole</c>/<c>PauseExternalConsole</c> flags mirror the
	/// legacy ItemProperty booleans and <c>Environment</c> the
	/// <c>EnvironmentVariables</c> block. Read-only: the shell lists and runs
	/// these, it never writes them back (the legacy Add/Remove dialogs are
	/// not migrated).</summary>
	public sealed record CustomCommandEntry (
		string Name, string Command, string? WorkingDir, bool ExternalConsole, bool PauseExternalConsole,
		IReadOnlyDictionary<string, string> Environment);

	/// <summary>Oleada 4 (slice F): rewrites the project's editable reference
	/// items (PackageReference / ProjectReference / Reference) exactly as the
	/// legacy ReferenceInformations save: every item of the three kinds is
	/// removed (an ItemGroup is dropped only when it becomes empty AND keeps
	/// no attributes — a group with other items, e.g. FrameworkReference,
	/// survives) and one ItemGroup with the new set, in the given order, is
	/// appended to the root. The rest of the file is untouched; the document
	/// keeps its detected settings on save (no BOM / LF on the fixture).
	/// Returns false without touching the file when it cannot be loaded or
	/// saved (slice D policy: corrupt contributes nothing, never throws).</summary>
	public static bool SaveProjectReferences (string csprojPath, IReadOnlyList<ReferenceItem> refs)
	{
		System.Xml.Linq.XDocument doc;
		try {
			doc = System.Xml.Linq.XDocument.Load (csprojPath);
		} catch {
			return false;
		}
		var root = doc.Root;
		if (root is null)
			return false;
		var ns = root.Name.Namespace;
		foreach (var el in doc.Descendants ()
			.Where (e => e.Name.LocalName is "PackageReference" or "Reference" or "ProjectReference")
			.ToList ()) {
			var parent = el.Parent;
			el.Remove ();
			if (parent is not null && parent.Name.LocalName == "ItemGroup" && !parent.Nodes ().Any () && !parent.HasAttributes)
				parent.Remove ();
		}
		if (refs.Count > 0) {
			var itemGroup = new System.Xml.Linq.XElement (ns + "ItemGroup");
			foreach (var r in refs) {
				var attrs = new List<System.Xml.Linq.XAttribute> { new ("Include", r.Include) };
				if (r.Version is not null)
					attrs.Add (new ("Version", r.Version));
				itemGroup.Add (new System.Xml.Linq.XElement (ns + r.Kind, attrs.ToArray ()));
			}
			root.Add (itemGroup);
		}
		// Save with the file's detected format (BOM / XML declaration /
		// newlines), not the XmlWriter defaults: doc.Save(path) would add a
		// UTF-8 BOM and an <?xml ...?> declaration the fixture does not have.
		byte [] orig;
		try {
			orig = File.ReadAllBytes (csprojPath);
		} catch {
			return false;
		}
		bool bom = orig.Length >= 3 && orig [0] == 0xEF && orig [1] == 0xBB && orig [2] == 0xBF;
		var head = System.Text.Encoding.UTF8.GetString (orig, bom ? 3 : 0, Math.Min (orig.Length - (bom ? 3 : 0), 200));
		bool decl = head.StartsWith ("<?xml", StringComparison.Ordinal);
		string newlines = Array.IndexOf (orig, (byte)'\r') >= 0 ? "\r\n" : "\n";
		try {
			using (var stream = new FileStream (csprojPath, FileMode.Create, FileAccess.Write))
			using (var writer = System.Xml.XmlWriter.Create (stream, new System.Xml.XmlWriterSettings {
				Encoding = new System.Text.UTF8Encoding (bom),
				OmitXmlDeclaration = !decl,
				Indent = false,
				NewLineChars = newlines
			}))
				doc.Save (writer);
		} catch {
			return false;
		}
		return true;
	}

	// Model cache keyed by full path + last-write time: the pad loads every
	// project when opening the solution and re-loads after project edits —
	// without the cache each pad refresh would re-parse the whole solution.
	static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (long MTime, ProjectFileModel Model)> projectModelCache
		= new (StringComparer.OrdinalIgnoreCase);

	static long LastWriteTicksSafe (string path)
	{
		try {
			return File.GetLastWriteTimeUtc (path).Ticks;
		} catch {
			return 0;
		}
	}

	/// <summary>Loads (or reuses, when the file is unchanged) the model of a
	/// .csproj. Returns null when the file does not exist or cannot be parsed
	/// (legacy tolerance).</summary>
	public static ProjectFileModel? LoadProjectModel (string csprojPath)
	{
		var full = Path.GetFullPath (csprojPath);
		if (!File.Exists (full))
			return null;
		var mtime = LastWriteTicksSafe (full);
		if (mtime != 0 && projectModelCache.TryGetValue (full, out var cached) && cached.MTime == mtime)
			return cached.Model;
		ProjectFileModel? model;
		try {
			model = ParseProjectFile (full);
		} catch {
			return null;
		}
		if (model is not null && mtime != 0)
			projectModelCache [full] = (mtime, model);
		return model;
	}

	/// <summary>Forwards the cache after external edits (tests/QA touch
	/// fixtures between reads).</summary>
	public static void ClearProjectModelCache () => projectModelCache.Clear ();

	static ProjectFileModel ParseProjectFile (string full)
	{
		var doc = System.Xml.Linq.XDocument.Load (full);
		var root = doc.Root ?? throw new InvalidDataException ("no root element in " + full);
		var ns = root.Name.Namespace;

		static string? FirstValue (System.Xml.Linq.XElement r, System.Xml.Linq.XNamespace n, string name)
		{
			var v = (string?)r.Descendants (n + name).FirstOrDefault ();
			return string.IsNullOrWhiteSpace (v) ? null : v.Trim ();
		}

		string[] ItemIncludes (string name)
		{
			var list = new List<string> ();
			foreach (var el in root.Descendants (ns + name)) {
				var v = (string?)el.Attribute ("Include");
				if (!string.IsNullOrWhiteSpace (v))
					list.Add (v.Trim ());
			}
			return list.ToArray ();
		}

		// Target frameworks: the multi-TFM list wins, the single one is the fallback.
		var tfms = new List<string> ();
		var tfmList = FirstValue (root, ns, "TargetFrameworks");
		foreach (var t in (tfmList ?? "").Split (';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			if (!tfms.Contains (t, StringComparer.OrdinalIgnoreCase))
				tfms.Add (t);
		var tfmSingle = FirstValue (root, ns, "TargetFramework");
		if (tfms.Count == 0 && tfmSingle is not null)
			tfms.Add (tfmSingle);

		// Configurations declared as PropertyGroup conditions ('$(Configuration)'=='X';
		// the quote between ')' and '==' is tolerated).
		var cfgRegex = new System.Text.RegularExpressions.Regex (
			@"\$\(\s*Configuration\s*\)['""]?\s*==\s*['""]([^'""]+)['""]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		var cfgs = new List<string> ();
		foreach (var pg in root.Elements (ns + "PropertyGroup")) {
			var cond = (string?)pg.Attribute ("Condition");
			if (cond is null)
				continue;
			var m = cfgRegex.Match (cond);
			if (m.Success && !cfgs.Contains (m.Groups [1].Value, StringComparer.OrdinalIgnoreCase))
				cfgs.Add (m.Groups [1].Value);
		}
		if (cfgs.Count == 0)
			cfgs.AddRange (new[] { "Debug", "Release" });

		// Project references resolved against the project directory when they exist.
		var projDir = Path.GetDirectoryName (full)!;
		var projRefs = new List<string> ();
		foreach (var inc in ItemIncludes ("ProjectReference")) {
			var normalized = inc.Replace ('\\', Path.DirectorySeparatorChar);
			var resolved = Path.IsPathRooted (normalized)
				? Path.GetFullPath (normalized)
				: Path.GetFullPath (Path.Combine (projDir, normalized));
			projRefs.Add (File.Exists (resolved) ? resolved : inc);
		}

		var itemCounts = new Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);
		foreach (var kind in new[] { "Compile", "None", "Content", "EmbeddedResource" }) {
			var count = root.Descendants (ns + kind).Count ();
			if (count > 0)
				itemCounts [kind] = count;
		}

		var name = FirstValue (root, ns, "AssemblyName")
			?? FirstValue (root, ns, "RootNamespace")
			?? Path.GetFileNameWithoutExtension (full);

		// Oleada 4 (slice F): the three item kinds the Add Reference dialog
		// edits, in document order, raw Include (ProjectReference keeps its
		// relative include); Version/HintPath kept for the dialog display.
		var references = new List<ReferenceItem> ();
		foreach (var el in root.Descendants ()) {
			string kind;
			switch (el.Name.LocalName) {
			case "PackageReference":
				kind = "PackageReference";
				break;
			case "ProjectReference":
				kind = "ProjectReference";
				break;
			case "Reference":
				kind = "Reference";
				break;
			default:
				continue;
			}
			var inc = (string?)el.Attribute ("Include");
			if (string.IsNullOrWhiteSpace (inc))
				continue;
			references.Add (new ReferenceItem (kind, inc.Trim (),
				(string?)el.Attribute ("Version"),
				(string?)el.Element (ns + "HintPath")?.Value));
		}

		// Ronda 6 (BUG 3): the legacy CustomCommand entries (Type="Custom") the
		// Project Commands > Custom command list shows and runs.
		var customCommands = ParseCustomCommands (root);

		return new ProjectFileModel (
			path: full,
			name: name,
			outputType: FirstValue (root, ns, "OutputType"),
			targetFrameworks: tfms,
			outputPath: FirstValue (root, ns, "OutputPath"),
			baseOutputPath: FirstValue (root, ns, "BaseOutputPath"),
			configurations: cfgs,
			frameworkRefs: ItemIncludes ("FrameworkReference"),
			packageRefs: ItemIncludes ("PackageReference"),
			assemblyRefs: ItemIncludes ("Reference"),
			projectRefs: projRefs,
			references: references,
			itemCounts: itemCounts,
			customCommands: customCommands);
	}

	/// <summary>Ronda 6 (BUG 3): reads the project's legacy <c>CustomCommand</c>
	/// entries of <c>Type="Custom"</c> from the .csproj — the same data the GTK
	/// <c>CustomCommandListHandler</c> pulls from
	/// <c>IConfigurationTarget.DefaultConfiguration.CustomCommands</c>.
	///
	/// The legacy <c>ItemProperty</c> writer (CollectionDataType) nests the
	/// commands as a <c>CustomCommands</c> group inside each configuration
	/// <c>PropertyGroup</c>, so we walk every <c>CustomCommands</c> descendant
	/// (namespace- and casing-tolerant). Each command item carries
	/// <c>Type</c>/<c>Command</c>/<c>WorkingDir</c>/<c>Name</c>/
	/// <c>ExternalConsole</c>/<c>PauseExternalConsole</c>; a field is read from
	/// the matching attribute first, then the same-named child element (the two
	/// spellings the legacy writer can emit), case-insensitively. Non-Custom
	/// types (Build/Clean/…) and entries without a command are skipped, exactly
	/// as the legacy handler filters <c>cmd.Type == CustomCommandType.Custom</c>.
	/// An unreadable/absent block yields an empty list — a broken custom-command
	/// section must not break the whole project model (the legacy tolerance).</summary>
	static List<CustomCommandEntry> ParseCustomCommands (System.Xml.Linq.XElement root)
	{
		var result = new List<CustomCommandEntry> ();
		try {
			// The legacy collection (CollectionDataType) wraps the command items in a
			// CustomCommands group whose element name is the SAME as the items
			// (CollectionDataType.OnSerialize: the wrapper's name is the property name
			// and each item's name is the CustomCommand class's serialized name — both
			// "CustomCommands", pinned by MSBuildXmlNamespaceNotAddedToCustomCommand).
			// So Descendants() named "CustomCommands" matches BOTH the wrapper and each
			// item. The wrapper is a pure container (no Command field); only the items
			// carry a Command. Filtering to the command-bearing nodes therefore yields
			// exactly the items, with no duplicates and no need to walk children.
			var items = root.Descendants ().Where (e =>
				string.Equals (e.Name.LocalName, "CustomCommands", StringComparison.OrdinalIgnoreCase)
				&& HasCommandField (e)).ToList ();
			foreach (var el in items) {
				var type = StringField (el, "Type");
				// The legacy filters Type == CustomCommandType.Custom; the enum
				// serializes as the value name ("Custom"). Case-insensitive.
				if (type is not null && !string.Equals (type, "Custom", StringComparison.OrdinalIgnoreCase))
					continue;
				var command = StringField (el, "Command");
				if (string.IsNullOrWhiteSpace (command))
					continue;
				result.Add (new CustomCommandEntry (
					Name: StringField (el, "Name") ?? string.Empty,
					Command: command,
					WorkingDir: StringField (el, "WorkingDir"),
					ExternalConsole: BoolField (el, "ExternalConsole"),
					PauseExternalConsole: BoolField (el, "PauseExternalConsole"),
					Environment: EnvVars (el)));
			}
		} catch {
			// A malformed block must not break the model — same tolerance as the
			// rest of ParseProjectFile (return what we already have).
		}
		return result;
	}

	static bool HasCommandField (System.Xml.Linq.XElement el)
		=> FieldValue (el, "Command") is not null;

	/// <summary>Reads a field as a string from the matching attribute first, then
	/// the same-named child element (case-insensitive) — the two spellings the
	/// legacy ItemProperty writer can emit. null when absent or blank.</summary>
	static string? StringField (System.Xml.Linq.XElement el, string field)
	{
		var v = FieldValue (el, field);
		return string.IsNullOrWhiteSpace (v) ? null : v.Trim ();
	}

	static bool BoolField (System.Xml.Linq.XElement el, string field)
		=> bool.TryParse (FieldValue (el, field), out var b) && b;

	/// <summary>Ronda 6 (BUG 3): the command's <c>EnvironmentVariables</c> block —
	/// the legacy writer nests <c>Variable</c> items with <c>name</c>/<c>value</c>
	/// (case-tolerant here). An empty dictionary when the block is absent.</summary>
	static Dictionary<string, string> EnvVars (System.Xml.Linq.XElement el)
	{
		var vars = new Dictionary<string, string> ();
		foreach (var block in el.Elements ().Where (x =>
			string.Equals (x.Name.LocalName, "EnvironmentVariables", StringComparison.OrdinalIgnoreCase))) {
			foreach (var v in block.Elements ()) {
				var name = v.Attribute ("name")?.Value ?? v.Element ("name")?.Value;
				var value = v.Attribute ("value")?.Value ?? v.Element ("value")?.Value;
				if (!string.IsNullOrEmpty (name))
					vars [name] = value ?? string.Empty;
			}
		}
		return vars;
	}

	static string? FieldValue (System.Xml.Linq.XElement el, string field)
	{
		var attr = el.Attributes ().FirstOrDefault (a =>
			string.Equals (a.Name.LocalName, field, StringComparison.OrdinalIgnoreCase));
		if (attr is not null)
			return attr.Value;
		var elem = el.Elements ().FirstOrDefault (e =>
			string.Equals (e.Name.LocalName, field, StringComparison.OrdinalIgnoreCase));
		return elem?.Value;
	}

	// ------------------------------------------------------------------
	// Oleada 4 (slice D): run configurations via model. The legacy stores a
	// project's run configurations as <PropertyGroup
	// Condition="'$(RunConfiguration)' == 'X'"> in the .csproj AND the
	// .csproj.user (Project.OnReadProject reads both; same name -> the user
	// group wins, "set the new group"), and the solution's startup item as
	// MonoDevelop.Ide.Workspace/Property name="StartupItem" in the solution
	// user file (Solution.OnEndLoad) - here .userprefs, like ActiveConfiguration.
	// ------------------------------------------------------------------

	/// <summary>One run configuration of a project, reduced to the properties
	/// the execution layer consumes (legacy ProcessRunConfiguration [ItemProperty]
	/// StartArguments/StartWorkingDirectory/ConsolePause/ExternalConsole).</summary>
	public sealed class RunConfigurationEntry
	{
		public string Name { get; }
		public string? StartArguments { get; internal set; }
		public string? WorkingDirectory { get; internal set; }
		public bool? PauseConsoleOutput { get; internal set; }
		public bool? ExternalConsole { get; internal set; }

		public RunConfigurationEntry (string name) => Name = name;
	}

	/// <summary>Legacy ParseRunConfigurationCondition + ExtractConfigName:
	/// the left side of '==' must be exactly '$(RunConfiguration)' (trimmed,
	/// single-quoted) and the right side a single-quoted name without an inner
	/// quote. Anything else (build groups, double quotes, bare $()...) -> null.</summary>
	static string? ParseRunCondition (string? cond)
	{
		if (string.IsNullOrEmpty (cond))
			return null;
		int i = cond.IndexOf ("==", StringComparison.Ordinal);
		if (i == -1)
			return null;
		if (cond.Substring (0, i).Trim () != "'$(RunConfiguration)'")
			return null;
		var rhs = cond.Substring (i + 2).Trim (' ');
		if (rhs.Length <= 2 || rhs [0] != '\'' || rhs [rhs.Length - 1] != '\'')
			return null;
		var name = rhs.Substring (1, rhs.Length - 2);
		return name.IndexOf ('\'') == -1 ? name : null;
	}

	/// <summary>Run configurations of a project, in document order: the .csproj
	/// groups first, then the .csproj.user groups (legacy read order). A name
	/// present in both is a single entry whose properties are merged as the
	/// legacy evaluation does: per property the later group (user file) wins,
	/// earlier groups provide fallbacks. A corrupt or missing file contributes
	/// nothing (legacy tolerance).</summary>
	public static IReadOnlyList<RunConfigurationEntry> GetProjectRunConfigurations (string csprojPath)
	{
		var full = Path.GetFullPath (csprojPath);
		var entries = new List<RunConfigurationEntry> ();
		var groups = new Dictionary<string, Dictionary<string, string?>> (StringComparer.Ordinal);

		void ScanFile (string path)
		{
			if (!File.Exists (path))
				return;
			System.Xml.Linq.XDocument doc;
			try {
				doc = System.Xml.Linq.XDocument.Load (path);
			} catch {
				return; // corrupt: the file contributes no run configurations
			}
			var root = doc.Root;
			if (root is null)
				return;
			var ns = root.Name.Namespace;
			foreach (var pg in root.Elements (ns + "PropertyGroup")) {
				var name = ParseRunCondition ((string?)pg.Attribute ("Condition"));
				if (name is null)
					continue;
				// Legacy evaluation: with RunConfiguration=name both files' groups
				// are active, so per property the later group (user file, scanned
				// second) overrides and earlier groups provide fallbacks.
				if (!groups.TryGetValue (name, out var group)) {
					group = new Dictionary<string, string?> (StringComparer.Ordinal);
					groups [name] = group;
					entries.Add (new RunConfigurationEntry (name));
				}
				foreach (var el in pg.Elements ())
					group [el.Name.LocalName] = (string?)el;
			}
		}

		ScanFile (full);
		ScanFile (full + ".user");

		foreach (var entry in entries) {
			var g = groups [entry.Name];
			if (g.TryGetValue ("StartArguments", out var args) && !string.IsNullOrWhiteSpace (args))
				entry.StartArguments = args;
			if (g.TryGetValue ("StartWorkingDirectory", out var wd) && !string.IsNullOrWhiteSpace (wd))
				entry.WorkingDirectory = wd;
			if (g.TryGetValue ("ConsolePause", out var pause))
				entry.PauseConsoleOutput = pause?.Trim () is "true" ? true : pause?.Trim () is "false" ? false : null;
			if (g.TryGetValue ("ExternalConsole", out var ext))
				entry.ExternalConsole = ext?.Trim () is "true" ? true : ext?.Trim () is "false" ? false : null;
		}
		return entries;
	}

	static string PrefsPath (string slnPath)
		=> slnPath.Substring (0, slnPath.Length - Path.GetExtension (slnPath).Length) + ".userprefs";

	/// <summary>The startup project of the solution as a path relative to the
	/// .sln (legacy Solution.OnEndLoad: MonoDevelop.Ide.Workspace/Property
	/// name="StartupItem" in the solution user file - here .userprefs).
	/// null when absent or the file is corrupt.</summary>
	public static string? GetSolutionStartupItem (string slnPath)
	{
		var prefs = PrefsPath (slnPath);
		if (!File.Exists (prefs))
			return null;
		try {
			var doc = System.Xml.Linq.XDocument.Load (prefs);
			return doc.Descendants ("Property")
				.Where (p => (string?)p.Attribute ("name") == "StartupItem")
				.Select (p => (string?)p.Attribute ("value"))
				.FirstOrDefault ();
		} catch {
			return null;
		}
	}

	/// <summary>Persists the startup project (legacy WorkspaceUserData
	/// StartupItem) into <sln>.userprefs; replaces any previous value.</summary>
	public static void SetSolutionStartupItem (string slnPath, string relativePath)
	{
		var prefs = PrefsPath (slnPath);
		System.Xml.Linq.XDocument doc;
		if (File.Exists (prefs)) {
			try {
				doc = System.Xml.Linq.XDocument.Load (prefs);
			} catch {
				doc = new System.Xml.Linq.XDocument (new System.Xml.Linq.XElement ("Properties"));
			}
		} else
			doc = new System.Xml.Linq.XDocument (new System.Xml.Linq.XElement ("Properties"));
		var root = doc.Root!;
		var ws = root.Element ("MonoDevelop.Ide.Workspace");
		if (ws is null) {
			ws = new System.Xml.Linq.XElement ("MonoDevelop.Ide.Workspace");
			root.Add (ws);
		}
		var prop = ws.Elements ("Property").FirstOrDefault (p => (string?)p.Attribute ("name") == "StartupItem");
		if (prop is null) {
			prop = new System.Xml.Linq.XElement ("Property");
			prop.SetAttributeValue ("name", "StartupItem");
			ws.Add (prop);
		}
		prop.SetAttributeValue ("value", relativePath);
		doc.Save (prefs);
	}

	/// <summary>Drops the persisted startup item; returns whether it existed.</summary>
	public static bool RemoveSolutionStartupItem (string slnPath)
	{
		var prefs = PrefsPath (slnPath);
		if (!File.Exists (prefs))
			return false;
		System.Xml.Linq.XDocument doc;
		try {
			doc = System.Xml.Linq.XDocument.Load (prefs);
		} catch {
			return false;
		}
		var prop = doc.Descendants ("Property")
			.FirstOrDefault (p => (string?)p.Attribute ("name") == "StartupItem");
		if (prop is null)
			return false;
		prop.Remove ();
		doc.Save (prefs);
		return true;
	}

	/// <summary>Run configurations of every project of the solution, in .sln
	/// order (projects without run configurations contribute nothing).</summary>
	public static IReadOnlyList<RunConfigurationEntry> GetSolutionRunConfigurations (string slnPath)
	{
		var list = new List<RunConfigurationEntry> ();
		foreach (var proj in GetSolutionProjects (slnPath))
			list.AddRange (GetProjectRunConfigurations (proj));
	return list;
	}

	/// <summary>Project configuration (name + platform) that a solution
	/// configuration maps to for one project, through the .sln
	/// ProjectConfigurationPlatforms section (the mapping the legacy solution
	/// build consumes: "{projGuid}.{slnCfg}|{slnPlat}.ActiveCfg =
	/// {projCfg}|{projPlat}"). When no mapping entry exists the project
	/// adopts the solution configuration name and "Any CPU" (a missing or
	/// corrupt .sln never breaks the build — it contributes nothing, slice D
	/// policy).</summary>
	public static ProjectConfigurationMapping GetProjectConfigurationFor (string slnPath, string solutionConfig, string projectPath)
	{
		var fallback = new ProjectConfigurationMapping (solutionConfig, "Any CPU");
		if (!File.Exists (slnPath) || string.IsNullOrEmpty (solutionConfig) || string.IsNullOrEmpty (projectPath))
			return fallback;
		SlnFile sln;
		try {
			sln = ReadSln (slnPath);
		} catch (Exception) {
			return fallback;
		}
		// Project GUID of the project file (same path resolution as GetSolutionProjects).
		string? projId = null;
		var target = Path.GetFullPath (projectPath);
		foreach (var p in sln.Projects) {
			if (p.FilePath is null || !IsProjectFile (p.FilePath))
				continue;
			if (Path.GetFullPath (Path.Combine (Path.GetDirectoryName (slnPath)!, p.FilePath.Replace ('\\', '/'))) == target) {
				projId = p.Id;
				break;
			}
		}
		if (projId is null)
			return fallback;
		SlnPropertySet? set = null;
		foreach (var s in sln.ProjectConfigurationsSection) {
			if (SameGuid (s.Id, projId)) {
				set = s;
				break;
			}
		}
		if (set is null)
			return fallback;
		// solutionConfig is normally a bare name ("Debug", GetSolutionConfigurations
		// shape); a full "Cfg|Platform" key compares against the whole key.
		var cfgName = solutionConfig.Contains ('|') ? null : solutionConfig;
		foreach (var kv in set) {
			var key = kv.Key;
			if (key is null || !key.EndsWith (".ActiveCfg", StringComparison.OrdinalIgnoreCase))
				continue;
			var slnCKey = key.Substring (0, key.Length - ".ActiveCfg".Length);
			bool match = cfgName is null
				? slnCKey.Equals (solutionConfig, StringComparison.OrdinalIgnoreCase)
				: SlnCfgPart (slnCKey).Equals (cfgName, StringComparison.OrdinalIgnoreCase);
			if (!match)
				continue;
			var val = kv.Value ?? string.Empty;
			var bar = val.IndexOf ('|');
			if (bar < 0)
				return new ProjectConfigurationMapping (val, "Any CPU");
			return new ProjectConfigurationMapping (val.Substring (0, bar), val.Substring (bar + 1));
		}
		return fallback;
	}

	static bool SameGuid (string? a, string? b)
		=> a is not null && b is not null
			&& a.Replace ("{", "").Replace ("}", "").Replace (" ", "").Equals (
				b.Replace ("{", "").Replace ("}", "").Replace (" ", ""), StringComparison.OrdinalIgnoreCase);

	static string SlnCfgPart (string slnCKey)
	{
		var bar = slnCKey.IndexOf ('|');
		return bar < 0 ? slnCKey : slnCKey.Substring (0, bar);
	}

	/// <summary>Per-project configuration (name + platform) resolved from a
	/// solution configuration (see <see cref="GetProjectConfigurationFor"/>).</summary>
	public sealed record ProjectConfigurationMapping (string Configuration, string Platform);

	// ---------- Code Generation panel (F5): the C# compiler fields of ONE
	// configuration of a project, exactly as the legacy full model
	// (ProjectConfiguration.Write + CSharpCompilerParameters.Write, i.e. the GTK
	// Code Generation panel) reads and writes them in
	// <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Cfg|Plat' ">.
	// The shell has no full DotNetProject model, so this pair of methods is the
	// surgical equivalent: it touches only the elements the panel edits and
	// leaves the rest of the file (and the group) untouched, with the same
	// per-field default suppression the legacy serializer applies.

	/// <summary>The C# code generation fields of one configuration, with the
	/// legacy defaults for absent elements (Optimize/overflow/TreatWarningsAs
	/// Errors false, PlatformTarget "anycpu", empty strings, WarningLevel 4;
	/// GenerateXmlDocumentation is the legacy
	/// !IsNullOrEmpty(DocumentationFile) view). Returns null when the project
	/// file does not exist or cannot be parsed; a group-less file reads as all
	/// defaults (like the legacy configuration with no PropertyGroup).</summary>
	public sealed record CSharpCodeGenerationFields (
		bool Optimize, bool GenerateOverflowChecks, string PlatformTarget,
		bool DebugSymbols, string DebugType,
		bool GenerateXmlDocumentation, string DocumentationFile,
		string DefineSymbols, int WarningLevel, string NoWarnings, bool TreatWarningsAsErrors);

	static string ProjectConfigurationCondition (string configuration, string platform)
		=> " '$(Configuration)|$(Platform)' == '" + configuration + "|" + (platform == AnyCpuSolution ? AnyCpuProject : platform) + "' ";

	static XElement? FindConfigurationGroup (XElement? root, string configuration, string platform)
	{
		if (root is null)
			return null;
		var cond = ProjectConfigurationCondition (configuration, platform);
		return root.Elements ().FirstOrDefault (e => e.Name.LocalName == "PropertyGroup" && (string?)e.Attribute ("Condition") == cond);
	}

	public static CSharpCodeGenerationFields? ReadCodeGenerationFields (string csprojPath, string configuration, string platform)
	{
		if (!File.Exists (csprojPath))
			return null;
		XDocument doc;
		try {
			doc = XDocument.Load (csprojPath);
		} catch {
			return null;
		}
		var pg = FindConfigurationGroup (doc.Root, configuration, platform);
		string S (string name) => pg?.Elements ().FirstOrDefault (e => e.Name.LocalName == name) is { } el ? el.Value : "";
		bool B (string name) => S (name).Trim ().Equals ("true", StringComparison.OrdinalIgnoreCase);
		int I (string name) => int.TryParse (S (name).Trim (), out var v) ? v : 4;
		// Legacy getter normalization (ProjectConfiguration.Read): an absent or empty
		// <DebugType> reads back as "none", which the panel's combo Load relies on.
		var debugType = S ("DebugType");
		if (debugType.Length == 0)
			debugType = "none";
		return new CSharpCodeGenerationFields (
			B ("Optimize"), B ("CheckForOverflowUnderflow"), S ("PlatformTarget"),
			B ("DebugSymbols"), debugType,
			S ("DocumentationFile").Length > 0, S ("DocumentationFile"),
			S ("DefineConstants"), I ("WarningLevel"), S ("NoWarn"), B ("TreatWarningsAsErrors"));
	}

	/// <summary>Writes the panel's fields into the configuration's PropertyGroup
	/// with the legacy write semantics, element by element:
	///   Optimize               - always written (legacy 2-arg SetValue, no default);
	///   WarningLevel           - "sticky": written iff the element was already
	///                            present OR the value != 4 (the legacy nullable
	///                            warninglevel never becomes 4 while absent);
	///   CheckForOverflowUnderflow/PlatformTarget/DefineConstants/NoWarn/
	///   TreatWarningsAsErrors/DocumentationFile - written iff different from the
	///                            ItemProperty default, removed when back to it
	///                            (legacy 3-arg SetValue default suppression);
	///   DebugSymbols/DebugType - the base ProjectConfiguration.Write state
	///                            machine: full/pdbonly/portable/embedded writes
	///                            DebugSymbols=true + the DebugType; "None"
	///                            removes DebugSymbols only when it is "true"
	///                            (a "false" element is left, like the legacy
	///                            "already the default" no-churn rule) and clears
	///                            DebugType only when it was not "none".
	/// New elements are appended to the group (MSBuild does not care about
	/// element order inside a PropertyGroup; the legacy SetPropertyOrder only
	/// affects the full model's own churn, not the values). The group is created
	/// before </Project> when absent, like AddProjectConfiguration. Bool elements
	/// use lowercase true/false, the format of the repo's .csproj files
	/// (MSBuildProperty default). The file is saved preserving its detected BOM /
	/// XML declaration / newline format (the SaveProjectReferences policy).
	/// Returns false without touching the file when it cannot be loaded or saved.</summary>
	public static bool SaveCodeGenerationFields (string csprojPath, string configuration, string platform, CSharpCodeGenerationFields f)
	{
		if (!File.Exists (csprojPath))
			return false;
		XDocument doc;
		try {
			doc = XDocument.Load (csprojPath);
		} catch {
			return false;
		}
		var root = doc.Root;
		if (root is null)
			return false;
		var ns = root.Name.Namespace;
		var pg = FindConfigurationGroup (root, configuration, platform);
		if (pg is null) {
			pg = new XElement (ns + "PropertyGroup", new XAttribute ("Condition", ProjectConfigurationCondition (configuration, platform)));
			root.Add (pg);
		}
		XElement? Find (string name) => pg.Elements ().FirstOrDefault (e => e.Name.LocalName == name);
		void Set (string name, string value)
		{
			var el = Find (name);
			if (el is null)
				pg.Add (new XElement (ns + name, value));
			else
				el.SetValue (value);
		}
		void Suppressed (string name, string value, string def)
		{
			if (value == def) {
				var el = Find (name);
				if (el is not null)
					el.Remove ();
				return;
			}
			Set (name, value);
		}

		Set ("Optimize", f.Optimize ? "true" : "false");
		if (Find ("WarningLevel") is not null || f.WarningLevel != 4)
			Set ("WarningLevel", f.WarningLevel.ToString (CultureInfo.InvariantCulture));
		Suppressed ("CheckForOverflowUnderflow", f.GenerateOverflowChecks ? "true" : "false", "false");
		Suppressed ("PlatformTarget", f.PlatformTarget, "anycpu");
		Suppressed ("DefineConstants", f.DefineSymbols, "");
		Suppressed ("NoWarn", f.NoWarnings, "");
		Suppressed ("TreatWarningsAsErrors", f.TreatWarningsAsErrors ? "true" : "false", "false");
		Suppressed ("DocumentationFile", f.GenerateXmlDocumentation ? f.DocumentationFile : "", "");

		// Replicate the legacy ProjectConfiguration.Read/Write effective state,
		// which normalizes an absent or empty <DebugType> to "none" and keeps the
		// two read flags (debugTypeWasNone / debugTypeReadAsEmpty) that gate the
		// "avoid churn" rules. Without the normalization the writers would diverge
		// on a group-less file (GTK reads it as none; a raw read would not).
		var rawDebugType = Find ("DebugType")?.Value?.Trim () ?? "";
		var effectiveDebugType = rawDebugType.Length == 0 || rawDebugType.Equals ("none", StringComparison.OrdinalIgnoreCase) ? "none" : rawDebugType;
		var debugTypeWasNone = effectiveDebugType == "none";
		var debugTypeReadAsEmpty = rawDebugType.Length == 0;
		if (f.DebugSymbols) {
			Set ("DebugSymbols", "true");
			Set ("DebugType", f.DebugType);
		} else {
			// Combo "None". The legacy in-memory debugType after Store() is "none"
			// when the file already read as "none", else "" (the panel blanks it).
			// The two write gates (ProjectConfiguration.Write L135-136 / L147-148)
			// use that value plus the read flags; both must be replicated - not just
			// the SetValue call behind each gate - or a file holding the legacy's
			// own Full->None residue (DebugSymbols=true + DebugType=none explicit)
			// would churn where the legacy leaves it untouched.
			var dbg = effectiveDebugType == "none" ? "none" : "";
			if (dbg != "none" || !debugTypeWasNone) {
				// Legacy SetValue("DebugSymbols", false, false): default suppression -
				// remove only a non-default ("true") element; a "false"/absent stays.
				var ds = Find ("DebugSymbols");
				if (ds is not null && ds.Value.Trim ().Equals ("true", StringComparison.OrdinalIgnoreCase))
					ds.Remove ();
			}
			if (dbg != "none" || !debugTypeReadAsEmpty) {
				if (dbg == "none") {
					// Legacy SetValue("DebugType","none",""): "none" is not the ""
					// default, so the explicit element is kept (already "none" when
					// the gate is true, i.e. readAsEmpty=false -> no change).
				} else {
					// Legacy SetValue("DebugType","",""): default suppression - remove
					// the (non-empty) element.
					var dt = Find ("DebugType");
					if (dt is not null && dt.Value.Trim ().Length > 0)
						dt.Remove ();
				}
			}
		}

		// Save with the file's detected format (BOM / XML declaration /
		// newlines), not the XmlWriter defaults (SaveProjectReferences policy).
		byte[] orig;
		try {
			orig = File.ReadAllBytes (csprojPath);
		} catch {
			return false;
		}
		bool bom = orig.Length >= 3 && orig [0] == 0xEF && orig [1] == 0xBB && orig [2] == 0xBF;
		var head = System.Text.Encoding.UTF8.GetString (orig, bom ? 3 : 0, Math.Min (orig.Length - (bom ? 3 : 0), 200));
		bool decl = head.StartsWith ("<?xml", StringComparison.Ordinal);
		string newlines = Array.IndexOf (orig, (byte)'\r') >= 0 ? "\r\n" : "\n";
		try {
			using (var stream = new FileStream (csprojPath, FileMode.Create, FileAccess.Write))
			using (var writer = System.Xml.XmlWriter.Create (stream, new System.Xml.XmlWriterSettings {
				Encoding = new System.Text.UTF8Encoding (bom),
				OmitXmlDeclaration = !decl,
				Indent = false,
				NewLineChars = newlines
			}))
				doc.Save (writer);
		} catch {
			return false;
		}
		return true;
	}

	// ---------- ASP.NET / ASP.NET Core / VB(.NET) options panels (port) ----------
	// The legacy item options panels persist through the full MonoDevelop.Projects
	// model (VBProject/VBCompilerParameters/AspNetAppProjectFlavor/LaunchProfileData).
	// The shell has no such model, so this block is the surgical equivalent: it reads
	// and writes the exact elements/sections the legacy serializers emit, so both UIs
	// produce value-compatible files. The legacy write model maps, per project:
	//   - VB project options      -> the top-level (unconditional) PropertyGroup of the
	//                                .vbproj (ItemProperty on VBProject);
	//   - VB Imports              -> <Import Include="..."/> items in a top-level
	//                                ItemGroup (VBProject.OnGetDefaultImports adds
	//                                "$(MSBuildBinPath)\Microsoft.VisualBasic.targets");
	//   - VB compiler options     -> the per-configuration
	//                                <PropertyGroup Condition=" '$(Configuration)' == 'X' ">
	//                                group (ItemProperty on VBCompilerParameters);
	//   - AspNet XSP options      -> a <MonoDevelop><Properties><XspParameters> section
	//                                appended to the .csproj (ItemProperty
	//                                XspParameters IsExternal=true);
	//   - AspNetCore run-config   -> the launchSettings.json profile
	//                                (LaunchProfileData: launchBrowser/launchUrl and
	//                                applicationUrl under otherSettings).

	// Project type / flavor detection, mirroring the legacy conditions:
	//   ProjectTypeId=VBNet         -> the VBNet type GUID in ProjectTypeGuids (or a .vbproj).
	//   FlavorType=AspNetAppProjectFlavor -> the AspNetApp flavor GUID in ProjectTypeGuids.
	//   AspNetCore                   -> an SDK-style .csproj on Microsoft.NET.Sdk.Web.
	// The GUIDs are the ones the legacy add-ins register (MSBuildItemTypes / ProjectModelExtensions).
	const string VbProjectTypeGuid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
	const string AspNetAppFlavorGuid = "{349C5851-65DF-11DA-9384-00065B846F21}";
	// The IL assembly project type GUID (legacy ILAsmBinding MSBuildItemTypes):
	// the per-project options dialog evaluates ProjectTypeId=ILAsmProject against
	// it, like it does the VBNet/C#/F# type aliases.
	const string IlAsmProjectTypeGuid = "{B4EC64DC-6D44-11DD-AAB0-C9A155D89593}";

	/// <summary>What kind of project a project file is, by the same signals the legacy
	/// model uses: the file extension, the ProjectTypeGuids GUIDs, and (for web) the
	/// SDK. Drives the options dialog's ProjectTypeId/FlavorType condition evaluation.
	/// Returns null when the file cannot be read or is not a recognizable project.</summary>
	public static ProjectKindInfo? GetProjectKind (string projPath)
	{
		if (string.IsNullOrEmpty (projPath) || !File.Exists (projPath))
			return null;
		XDocument doc;
		try {
			doc = XDocument.Load (projPath);
		} catch {
			return null;
		}
		var root = doc.Root;
		if (root is null)
			return null;

		var isVbFile = projPath.EndsWith (".vbproj", StringComparison.OrdinalIgnoreCase);
		var isWebFile = projPath.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase);
		var isIlFile = projPath.EndsWith (".ilproj", StringComparison.OrdinalIgnoreCase);

		// ProjectTypeGuids: a semicolon list of GUIDs in any PropertyGroup.
		var typeGuids = new List<string> ();
		foreach (var el in root.Descendants ()
			.Where (e => e.Name.LocalName == "ProjectTypeGuids"))
			foreach (var g in (el.Value ?? "")
				.Split (new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				if (g.Trim ().Length > 0)
					typeGuids.Add (g.Trim ());

		// The SDK is declared as the Project's Sdk attribute (SDK-style:
		// <Project Sdk="Microsoft.NET.Sdk.Web">) or as <Sdk> elements.
		var sdkAttr = root.Attribute ("Sdk")?.Value ?? "";
		var sdks = new[] { sdkAttr }.Concat (root.Descendants ()
			.Where (e => e.Name.LocalName == "Sdk")
			.Select (e => (string?)e ?? "")).Where (s => s.Length > 0);
		var isSdkStyle = sdks.Any ();
		var isWeb = isWebFile && isSdkStyle && sdks.Any (s =>
			s.Contains (".Sdk.Web", StringComparison.OrdinalIgnoreCase));

		bool Has (string guid) => typeGuids.Any (g => SameGuid (g, guid));

		// The type alias of a classic .csproj is ambiguous between C# (the common
		// case, the FAE04EC0 GUID) and the web flavors; the extension + GUID split
		// below resolves it the way the legacy model does. An .ilproj is the IL
		// assembly project (the B4EC64DC type GUID).
		var isCSharp = (isWebFile && !isWeb) || Has (CSharpProjectTypeGuid);
		var isFSharp = projPath.EndsWith (".fsproj", StringComparison.OrdinalIgnoreCase) || Has (FSharpProjectTypeGuid);
		var isIlAsm = isIlFile || Has (IlAsmProjectTypeGuid);

		return new ProjectKindInfo (
			projPath,
			isVbFile || Has (VbProjectTypeGuid),
			Has (AspNetAppFlavorGuid),
			isWeb,
			isCSharp,
			isIlAsm,
			isFSharp);
	}

	/// <summary>Project classification used to evaluate the item options panels'
	/// ProjectTypeId/FlavorType conditions. The boolean fields mirror the legacy
	/// condition keys: <see cref="IsVBNet"/> (ProjectTypeId=VBNet, the .vbproj or its
	/// type GUID), <see cref="IsAspNetApp"/> (FlavorType=
	/// MonoDevelop.AspNet.Projects.AspNetAppProjectFlavor), <see cref="IsAspNetCore"/>
	/// (an SDK-style web .csproj), <see cref="IsCSharp"/>/.<see cref="IsFSharp"/>/
	/// <see cref="IsILAsm"/> (the ProjectTypeId aliases C# / F# / ILAsmProject, by
	/// extension — the .csproj/.fsproj/.ilproj signals the legacy model reads) and
	/// <see cref="IsDotNetProject"/> (the ItemType=DotNetProject tag: every classic
	/// .csproj/.fsproj/.vbproj/.ilproj, excluding the SDK-style web flavor which the
	/// legacy model classifies separately).</summary>
	public sealed record ProjectKindInfo (
		string Path, bool IsVBNet, bool IsAspNetApp, bool IsAspNetCore,
		bool IsCSharp, bool IsILAsm, bool IsFSharp)
	{
		public bool IsDotNetProject => IsVBNet || IsCSharp || IsFSharp || IsILAsm;
	}

	/// <summary>Classic ASP.NET MVC project — the file-level mirror of
	/// AspNetAppProjectFlavor.IsAspMvcProject (AspNetApp flavor + a
	/// "System.Web.Mvc" reference). Gates the legacy ProjectPad Add section
	/// items Controller... / View... (AspNetFolderNodeCommandHandler).</summary>
	public static bool IsMvcClassicProject (string projPath)
		=> GetProjectKind (projPath) is { IsAspNetApp: true } &&
			ProjectHasReference (projPath, "System.Web.Mvc");

	/// <summary>Whether the classic project references an assembly whose
	/// Include starts with <paramref name="prefix"/> — the file-level mirror of
	/// AspNetAppProjectFlavor.IsAspMvcProject / IsAspWebApiProject
	/// (Project.References.Any(r => r.Reference.StartsWith(...))). Used by the
	/// ProjectPad gates and by the Global.asax file template's UsesAspNetMvc /
	/// UsesAspNetWebApi conditional files.</summary>
	public static bool ProjectHasReference (string projPath, string prefix)
	{
		try {
			var root = XDocument.Load (projPath).Root;
			return root.Descendants ().Any (e =>
				e.Name.LocalName == "Reference" &&
				((string?)e.Attribute ("Include") ?? "").StartsWith (prefix, StringComparison.Ordinal));
		} catch {
			return false;
		}
	}

	/// <summary>Whether the project's TargetFramework(s) satisfy the minimum
	/// version — the file-level mirror of the AspNetCore file template
	/// condition (AspNetCoreFileTemplateCondition, minVersion="X.0"). The TFM
	/// is parsed as "net" / "netcoreapp" / "netstandard" + major version; when
	/// no TFM is declared or it cannot be parsed the check passes (the legacy
	/// condition only rejects versions strictly below the minimum).</summary>
	public static bool ProjectMeetsVersion (string projPath, System.Version minVersion)
	{
		try {
			var root = XDocument.Load (projPath).Root;
			if (root is null)
				return true;
			var tfms = root.Descendants ()
				.Where (e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
				.Select (e => (string?)e ?? "")
				.SelectMany (s => (s ?? "").Split (';'))
				.Select (s => s.Trim ())
				.Where (s => s.Length > 0);
			if (!tfms.Any ())
				return true; // no explicit TFM — the SDK default (current) meets any minimum
			return tfms.Any (tfm => {
				string rest = tfm;
				foreach (var alias in new[] { "netcoreapp", "netstandard", "net" }) {
					if (rest.StartsWith (alias, StringComparison.OrdinalIgnoreCase)) {
						rest = rest[alias.Length..];
						break;
					}
				}
				var major = rest.Split ('.', '-', 'a', 'b', 'r').FirstOrDefault ();
				return int.TryParse (major, out var v) && v >= minVersion.Major;
			});
		} catch {
			return true; // unreadable project: show the template (legacy "show all")
		}
	}

	/// <summary>The project's root namespace — the file-level mirror of
	/// DotNetProject.GetDefaultNamespace(null): the RootNamespace property when
	/// declared, otherwise the project file name without extension. Simplified
	/// relative to the legacy: no subfolder name-segment concatenation (new
	/// files of the New File dialog are created in the project root).</summary>
	public static string GetRootNamespace (string projPath)
	{
		try {
			var root = XDocument.Load (projPath).Root;
			var ns = root.Descendants ()
				.FirstOrDefault (e => e.Name.LocalName == "RootNamespace")?.Value?.Trim ();
			if (ns is { Length: > 0 })
				return ns;
			var name = Path.GetFileNameWithoutExtension (projPath);
			return string.IsNullOrEmpty (name) ? "Application" : name;
		} catch {
			var name = Path.GetFileNameWithoutExtension (projPath);
			return string.IsNullOrEmpty (name) ? "Application" : name;
		}
	}

	/// <summary>Whether `dotnet publish` / `dotnet pack` apply to the project —
	/// the file-level mirror of PublishToFolderBaseCommandHandler.
	/// ProjectSupportsFolderPublishing (only .NET Core / .NET Standard TFMs):
	/// classic (non-SDK, .NET Framework) projects never publish — they carry no
	/// Sdk attribute/element and no TargetFramework — and an explicit net4* TFM
	/// on an SDK project is excluded by the same TFM check. SDK-style web
	/// (ASP.NET Core) projects are publishable and must NOT be filtered out by
	/// IsDotNetProject, which classifies the web flavor separately.</summary>
	public static bool IsPublishableProject (string projPath)
	{
		if (!IsProjectFile (projPath) || !File.Exists (projPath))
			return false;
		if (GetProjectKind (projPath) is null)
			return false;
		try {
			var root = XDocument.Load (projPath).Root;
			if (root is null)
				return false;
			var sdk = (string?)root.Attribute ("Sdk") ?? "";
			var sdkEls = root.Descendants ().Where (e => e.Name.LocalName == "Sdk")
				.Select (e => (string?)e ?? "").Where (s => s.Length > 0);
			if (sdk.Length == 0 && !sdkEls.Any ())
				return false; // classic (non-SDK) .vbproj/.csproj — .NET Framework
			var tfms = root.Descendants ()
				.Where (e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
				.Select (e => (string?)e ?? "")
				.SelectMany (s => (s ?? "").Split (';'))
				.Select (s => s.Trim ())
				.Where (s => s.Length > 0);
			if (!tfms.Any ())
				return true; // no explicit TFM — the SDK default (current) is a Core TFM
			return tfms.Any (t =>
				!t.StartsWith ("net4", StringComparison.OrdinalIgnoreCase) &&
				(t.StartsWith ("net", StringComparison.OrdinalIgnoreCase)
					|| t.StartsWith ("netstandard", StringComparison.OrdinalIgnoreCase)
					|| t.StartsWith ("netcoreapp", StringComparison.OrdinalIgnoreCase)));
		} catch {
			return false;
		}
	}

	/// <summary>The publish profile names (file names without extension) of
	/// Properties/PublishProfiles/*.pubxml — the file-level mirror of
	/// DotNetProject.GetPublishProfiles (legacy
	/// PublishToFolderProfilesCommandHandler lists the FileSystem ones).</summary>
	public static IReadOnlyList<string> GetPublishProfiles (string projPath)
	{
		var dir = Path.Combine (Path.GetDirectoryName (projPath) ?? string.Empty, "Properties", "PublishProfiles");
		if (!Directory.Exists (dir))
			return Array.Empty<string>();
		try {
			return Directory.GetFiles (dir, "*.pubxml", SearchOption.TopDirectoryOnly)
				.Select (f => Path.GetFileNameWithoutExtension (f))
				.OrderBy (n => n, StringComparer.OrdinalIgnoreCase)
				.ToList ();
		} catch {
			return Array.Empty<string>();
		}
	}

	// Shared format-preserving save (the SaveProjectReferences policy): keep the file's
	// detected BOM / XML declaration / newline format. Returns false without touching the
	// file when it cannot be saved.
	static bool SavePreservingFormat (XDocument doc, string path)
	{
		byte[] orig;
		try {
			orig = File.ReadAllBytes (path);
		} catch {
			return false;
		}
		bool bom = orig.Length >= 3 && orig [0] == 0xEF && orig [1] == 0xBB && orig [2] == 0xBF;
		var head = System.Text.Encoding.UTF8.GetString (orig, bom ? 3 : 0, Math.Min (orig.Length - (bom ? 3 : 0), 200));
		bool decl = head.StartsWith ("<?xml", StringComparison.Ordinal);
		string newlines = Array.IndexOf (orig, (byte)'\r') >= 0 ? "\r\n" : "\n";
		try {
			using (var stream = new FileStream (path, FileMode.Create, FileAccess.Write))
			using (var writer = System.Xml.XmlWriter.Create (stream, new System.Xml.XmlWriterSettings {
				Encoding = new System.Text.UTF8Encoding (bom),
				OmitXmlDeclaration = !decl,
				Indent = false,
				NewLineChars = newlines
			}))
				doc.Save (writer);
		} catch {
			return false;
		}
		return true;
	}

	static XDocument? LoadDoc (string path)
	{
		if (!File.Exists (path))
			return null;
		try {
			var doc = XDocument.Load (path);
			return doc.Root is null ? null : doc;
		} catch {
			return null;
		}
	}

	// The unconditional (top-level) PropertyGroup(s) of a classic .vbproj: the legacy
	// ItemProperty-on-VBProject fields land here, not in a per-configuration group.
	static XElement? FindGlobalGroup (XElement root, XNamespace ns)
		=> root.Elements (ns + "PropertyGroup").FirstOrDefault (pg =>
			pg.Attribute ("Condition") is null);

	// ---------- VB project options (top-level .vbproj) ----------

	/// <summary>The VB project-level options of a .vbproj, with the legacy defaults for
	/// absent elements (OptionExplicit On, OptionCompare Binary, others Off/empty,
	/// CompileTarget Library). Returns null when the file cannot be read.</summary>
	public static VBProjectFields? ReadVbProjectFields (string vbprojPath)
	{
		var doc = LoadDoc (vbprojPath);
		if (doc is null)
			return null;
		var ns = doc.Root!.Name.Namespace;
		var pg = FindGlobalGroup (doc.Root!, ns) ?? doc.Root!;
		string S (string name) => pg.Elements ().FirstOrDefault (e => e.Name.LocalName == name)?.Value ?? "";
		// Absent elements read as their legacy ItemProperty default (OptionInfer
		// Off, OptionExplicit On, OptionCompare Binary, OptionStrict Off), so the
		// panel shows the same value the legacy model would.
		string OnOff (string name, string def)
		{
			var v = S (name).Trim ();
			return v.Length == 0 ? def : (v.Equals ("On", StringComparison.OrdinalIgnoreCase) ? "On" : "Off");
		}
		string compare = S ("OptionCompare").Trim ();
		if (compare.Length == 0)
			compare = "Binary";
		string outputType = S ("OutputType");
		// Legacy VBProject: CompileTarget read from OutputType (Exe/WinExe/Library);
		// Module has no OutputType and reads as the enum's Module only if present.
		string compileTarget = outputType;
		return new VBProjectFields (
			OnOff ("OptionInfer", "Off"), OnOff ("OptionExplicit", "On"),
			compare, OnOff ("OptionStrict", "Off"),
			S ("MyType"), S ("StartupObject"), S ("ApplicationIcon"),
			S ("CodePage"), string.IsNullOrEmpty (compileTarget) ? "Library" : compileTarget);
	}

	/// <summary>One configuration of a .vbproj's VB project-level options. Values are
	/// the raw legacy token strings ("On"/"Off", "Binary"/"Text", OutputType) so the
	/// panel's combos map directly; absent elements resolve to the legacy
	/// ItemProperty defaults (OptionExplicit On, OptionCompare Binary, the rest
	/// Off/empty).</summary>
	public sealed record VBProjectFields (
		string OptionInfer, string OptionExplicit, string OptionCompare, string OptionStrict,
		string MyType, string StartupObject, string ApplicationIcon, string CodePage, string CompileTarget);

	/// <summary>Writes the VB project-level options into the .vbproj's top-level
	/// PropertyGroup (creating one if absent), mirroring the legacy ItemProperty write:
	/// the Option* and Compare fields are written when different from their default
	/// (OptionExplicit On, OptionCompare Binary, others Off/empty) and removed when back
	/// to it; CompileTarget writes OutputType (Library is the default and is removed).
	/// Returns false without touching the file when it cannot be saved.</summary>
	public static bool SaveVbProjectFields (string vbprojPath, VBProjectFields f)
	{
		var doc = LoadDoc (vbprojPath);
		if (doc is null)
			return false;
		var root = doc.Root!;
		var ns = root.Name.Namespace;
		var pg = FindGlobalGroup (root, ns);
		if (pg is null) {
			pg = new XElement (ns + "PropertyGroup");
			root.Add (pg);
		}
		XElement? Find (string name) => pg.Elements ().FirstOrDefault (e => e.Name.LocalName == name);
		void Suppressed (string name, string value, string def)
		{
			if (value == def) {
				var ex = Find (name);
				if (ex is not null)
					ex.Remove ();
				return;
			}
			var el = Find (name);
			if (el is null)
				pg.Add (new XElement (ns + name, value));
			else
				el.SetValue (value);
		}
		// The legacy ItemProperty defaults: OptionInfer "Off", OptionExplicit "On",
		// OptionCompare "Binary", OptionStrict "Off".
		Suppressed ("OptionInfer", f.OptionInfer, "Off");
		Suppressed ("OptionExplicit", f.OptionExplicit, "On");
		Suppressed ("OptionCompare", f.OptionCompare, "Binary");
		Suppressed ("OptionStrict", f.OptionStrict, "Off");
		Suppressed ("MyType", f.MyType, "");
		Suppressed ("StartupObject", f.StartupObject, "");
		Suppressed ("ApplicationIcon", f.ApplicationIcon, "");
		Suppressed ("CodePage", f.CodePage, "");
		Suppressed ("OutputType", f.CompileTarget, "Library");
		return SavePreservingFormat (doc, vbprojPath);
	}

	// ---------- VB Imports (top-level ItemGroup) ----------

	/// <summary>The VB Imports of a .vbproj: the <c>&lt;Import Include="..."/&gt;</c> items
	/// in document order (the legacy OnGetDefaultImports adds
	/// "$(MSBuildBinPath)\Microsoft.VisualBasic.targets"). Returns an empty list when the
	/// file cannot be read (the legacy reads an empty import set on a missing file).</summary>
	public static IReadOnlyList<string> ReadVbImports (string vbprojPath)
	{
		var doc = LoadDoc (vbprojPath);
		if (doc is null)
			return Array.Empty<string> ();
		var ns = doc.Root!.Name.Namespace;
		var list = new List<string> ();
		foreach (var ig in doc.Root!.Elements (ns + "ItemGroup"))
			foreach (var el in ig.Elements (ns + "Import"))
				if (el.Attribute ("Include") is { Value: { Length: > 0 } v })
					list.Add (v);
		return list;
	}

	/// <summary>Replaces the .vbproj's <c>&lt;Import Include="..."/&gt;</c> items with the
	/// given set, in order: every existing Import item is removed (its ItemGroup dropped
	/// only when it becomes empty) and, when the set is non-empty, one ItemGroup with the
	/// new imports is appended to the root (the legacy rewrite semantics). Returns false
	/// without touching the file when it cannot be saved.</summary>
	public static bool SaveVbImports (string vbprojPath, IReadOnlyList<string> imports)
	{
		var doc = LoadDoc (vbprojPath);
		if (doc is null)
			return false;
		var root = doc.Root!;
		var ns = root.Name.Namespace;
		// Remove the Import items that carry an Include attribute (the namespace
		// imports the panel edits), dropping a now-empty ItemGroup. Imports that
		// reference a targets file (<Import Project=.../>) are left untouched, so the
		// writer stays coherent with the reader (which only sees Include items) in
		// every layout.
		foreach (var ig in root.Elements (ns + "ItemGroup").ToList ())
			foreach (var el in ig.Elements (ns + "Import")
				.Where (e => e.Attribute ("Include") is not null).ToList ()) {
				el.Remove ();
			}
		foreach (var ig in root.Elements (ns + "ItemGroup").Where (g => g.HasElements == false).ToList ())
			ig.Remove ();
		if (imports.Count > 0) {
			var ig = new XElement (ns + "ItemGroup");
			foreach (var inc in imports)
				ig.Add (new XElement (ns + "Import", new XAttribute ("Include", inc)));
			root.Add (ig);
		}
		return SavePreservingFormat (doc, vbprojPath);
	}

	// ---------- VB compiler options (per-configuration PropertyGroup) ----------

	/// <summary>The per-configuration PropertyGroup condition the legacy writer emits
	/// (Project.BuildConfigCondition): the COMBINED form
	/// <c> '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' </c>, with an empty
	/// platform normalized to AnyCPU — exactly what real .vbproj/.csproj files carry.
	/// (P0 of this port used the simple <c> '$(Configuration)' == 'X' </c> form; P1
	/// aligned it to the combined one so both UIs produce value-compatible files.)</summary>
	static string VbConfigCondition (string configuration, string? platform)
	{
		var plat = string.IsNullOrEmpty (platform) ? "AnyCPU" : (platform == "Any CPU" ? "AnyCPU" : platform);
		return " '$(Configuration)|$(Platform)' == '" + configuration + "|" + plat + "' ";
	}

	/// <summary>Locates the per-configuration PropertyGroup by the combined legacy
	/// condition; as a fallback for files written in the simple
	/// <c> '$(Configuration)' == 'X' </c> form it matches that one (P0 wrote it).</summary>
	static XElement? FindVbConfigGroup (XElement root, XNamespace ns, string configuration, string? platform)
	{
		var combined = VbConfigCondition (configuration, platform);
		var pg = root.Elements (ns + "PropertyGroup")
			.FirstOrDefault (g => (string?)g.Attribute ("Condition") == combined);
		if (pg is not null)
			return pg;
		var simple = " '$(Configuration)' == '" + configuration + "' ";
		return root.Elements (ns + "PropertyGroup")
			.FirstOrDefault (g => (string?)g.Attribute ("Condition") == simple);
	}

	/// <summary>The VB compiler options of ONE configuration of a .vbproj, with the
	/// legacy VBCompilerParameters defaults for absent elements (Optimize false,
	/// DebugType full, WarningLevel 1, RemoveIntegerChecks false, WarningsDisabled
	/// false, empty strings). The per-configuration group is located by the combined
	/// legacy condition (see <see cref="VbConfigCondition"/>), falling back to the
	/// simple form for files written by other tools. Returns null when the file cannot
	/// be read; a group-less file reads as all defaults (like the legacy configuration
	/// with no per-configuration PropertyGroup).</summary>
	public static VBCompilerFields? ReadVbCompilerFields (string vbprojPath, string configuration, string? platform = null)
	{
		var doc = LoadDoc (vbprojPath);
		if (doc is null)
			return null;
		var ns = doc.Root!.Name.Namespace;
		var pg = FindVbConfigGroup (doc.Root!, ns, configuration, platform);
		string S (string name) => pg?.Elements ().FirstOrDefault (e => e.Name.LocalName == name)?.Value ?? "";
		bool B (string name) => S (name).Trim ().Equals ("true", StringComparison.OrdinalIgnoreCase);
		int warnLevel = int.TryParse (S ("WarningLevel").Trim (), out var wl) ? wl : 1;
		string debugType = S ("DebugType");
		if (debugType.Length == 0)
			debugType = "full";
		return new VBCompilerFields (
			B ("Optimize"), B ("DefineDebug"), B ("DefineTrace"),
			debugType, S ("DocumentationFile"), S ("NoWarn"), S ("WarningsAsErrors"),
			B ("TreatWarningsAsErrors"), warnLevel, S ("DefineConstants"),
			B ("RemoveIntegerChecks"), S ("RootNamespace"), S ("AdditionalParameters"));
	}

	/// <summary>One configuration of a .vbproj's VB compiler options. DebugType is the
	/// normalized token ("none"/"pdbonly"/"full"); WarningsDisabled is the legacy
	/// WarningLevel==0 view.</summary>
	public sealed record VBCompilerFields (
		bool Optimize, bool DefineDebug, bool DefineTrace, string DebugType,
		string DocumentationFile, string NoWarn, string WarningsAsErrors,
		bool TreatWarningsAsErrors, int WarningLevel, string DefineConstants,
		bool RemoveIntegerChecks, string RootNamespace, string AdditionalParameters);

	/// <summary>Writes the VB compiler options into the configuration's per-configuration
	/// PropertyGroup (see <see cref="FindVbConfigGroup"/>: the combined legacy condition
	/// <c> '$(Configuration)|$(Platform)' == 'X|AnyCPU' </c>, a simple-form group from an
	/// older writer is adopted and rewritten in the combined form, and a missing group is
	/// created before </Project> in the combined form) with the legacy
	/// VBCompilerParameters write semantics: each ItemProperty field is written when
	/// different from its default and removed when back to it (Optimize/RemoveIntegerChecks
	/// default false, DebugType "full", WarningLevel 1, the strings empty). Bool elements
	/// use lowercase true/false. Returns false without touching the file when it cannot be
	/// saved.</summary>
	public static bool SaveVbCompilerFields (string vbprojPath, string configuration, VBCompilerFields f, string? platform = null)
	{
		var doc = LoadDoc (vbprojPath);
		if (doc is null)
			return false;
		var root = doc.Root!;
		var ns = root.Name.Namespace;
		// The canonical combined condition: this is what the legacy writer
		// (Project.BuildConfigCondition) and every real .vbproj/.csproj carry.
		var cond = VbConfigCondition (configuration, platform);
		var pg = FindVbConfigGroup (root, ns, configuration, platform);
		if (pg is null) {
			pg = new XElement (ns + "PropertyGroup", new XAttribute ("Condition", cond));
			root.Add (pg);
		} else if ((string?)pg.Attribute ("Condition") != cond) {
			// Adopt a group written in the simple form (P0, or a hand edit): rewrite its
			// Condition so the file ends up in the canonical shape, one group per config.
			pg.SetAttributeValue ("Condition", cond);
		}
		XElement? Find (string name) => pg.Elements ().FirstOrDefault (e => e.Name.LocalName == name);
		void Set (string name, string value)
		{
			var el = Find (name);
			if (el is null)
				pg.Add (new XElement (ns + name, value));
			else
				el.SetValue (value);
		}
		void Suppressed (string name, string value, string def)
		{
			if (value == def) {
				var el = Find (name);
				if (el is not null)
					el.Remove ();
				return;
			}
			Set (name, value);
		}
		Suppressed ("Optimize", f.Optimize ? "true" : "false", "false");
		Suppressed ("DefineDebug", f.DefineDebug ? "true" : "false", "false");
		Suppressed ("DefineTrace", f.DefineTrace ? "true" : "false", "false");
		Suppressed ("DebugType", f.DebugType, "full");
		Suppressed ("DocumentationFile", f.DocumentationFile, "");
		Suppressed ("NoWarn", f.NoWarn, "");
		Suppressed ("WarningsAsErrors", f.WarningsAsErrors, "");
		Suppressed ("TreatWarningsAsErrors", f.TreatWarningsAsErrors ? "true" : "false", "false");
		// WarningLevel: the legacy VBCompilerParameters has no default suppression for it
		// (DefaultValue=1 but it is written whenever the group exists), so write it always.
		Set ("WarningLevel", f.WarningLevel.ToString (CultureInfo.InvariantCulture));
		Suppressed ("DefineConstants", f.DefineConstants, "");
		Suppressed ("RemoveIntegerChecks", f.RemoveIntegerChecks ? "true" : "false", "false");
		Suppressed ("RootNamespace", f.RootNamespace, "");
		Suppressed ("AdditionalParameters", f.AdditionalParameters, "");
		return SavePreservingFormat (doc, vbprojPath);
	}

	// ---------- ASP.NET XSP options (MonoDevelop/Properties/XspParameters in .csproj) ----------

	/// <summary>The ASP.NET XSP options of a .csproj, read from the
	/// <c>&lt;MonoDevelop&gt;&lt;Properties&gt;&lt;XspParameters&gt;</c> section (the legacy
	/// ItemProperty XspParameters IsExternal=true). Element names are the XspParameters
	/// ItemProperty names. Returns null when the section is absent (the legacy reads a
	/// fresh XspParameters) or the file cannot be read.</summary>
	public static XspFields? ReadXspFields (string csprojPath)
	{
		var doc = LoadDoc (csprojPath);
		if (doc is null)
			return null;
		var md = doc.Root!.Elements ().FirstOrDefault (e => e.Name.LocalName == "MonoDevelop");
		var props = md?.Elements ().FirstOrDefault (e => e.Name.LocalName == "Properties");
		var sec = props?.Elements ().FirstOrDefault (e => e.Name.LocalName == "XspParameters");
		if (sec is null)
			return null;
		string S (string name) => sec.Elements ().FirstOrDefault (e => e.Name.LocalName == name)?.Value ?? "";
		return new XspFields (
			int.TryParse (S ("Port"), out var p) ? p : 8080,
			S ("Address"), S ("SslMode"), S ("SslProtocol"), S ("KeyType"),
			S ("CertFile"), S ("KeyFile"), S ("PasswordOptions"), S ("Password"),
			S ("Verbose").Trim () != "false");
	}

	/// <summary>The ASP.NET XSP options of a .csproj. SslMode/KeyType/SslProtocol/
	/// PasswordOptions are the legacy enum token strings; Verbose defaults to true and
	/// the address/port to the XspParameters defaults when the section is absent.</summary>
	public sealed record XspFields (
		int Port, string Address, string SslMode, string SslProtocol, string KeyType,
		string CertificateFile, string KeyFile, string PasswordOptions, string Password, bool Verbose);

	/// <summary>Writes the ASP.NET XSP options into the .csproj's
	/// <c>&lt;MonoDevelop&gt;&lt;Properties&gt;&lt;XspParameters&gt;</c> section (created when
	/// absent), element by element with the XspParameters ItemProperty names. Returns
	/// false without touching the file when it cannot be saved.</summary>
	public static bool SaveXspFields (string csprojPath, XspFields f)
	{
		var doc = LoadDoc (csprojPath);
		if (doc is null)
			return false;
		var root = doc.Root!;
		var ns = root.Name.Namespace;
		var md = root.Elements ().FirstOrDefault (e => e.Name.LocalName == "MonoDevelop");
		if (md is null) {
			md = new XElement (ns + "MonoDevelop");
			root.Add (md);
		}
		var props = md.Elements ().FirstOrDefault (e => e.Name.LocalName == "Properties");
		if (props is null) {
			props = new XElement (ns + "Properties");
			md.Add (props);
		}
		var sec = props.Elements ().FirstOrDefault (e => e.Name.LocalName == "XspParameters");
		if (sec is null) {
			sec = new XElement (ns + "XspParameters");
			props.Add (sec);
		}
		void Set (string name, string value)
		{
			var el = sec.Elements ().FirstOrDefault (e => e.Name.LocalName == name);
			if (el is null)
				sec.Add (new XElement (ns + name, value));
			else
				el.SetValue (value);
		}
		Set ("Port", f.Port.ToString (CultureInfo.InvariantCulture));
		Set ("Address", f.Address);
		Set ("SslMode", f.SslMode);
		Set ("SslProtocol", f.SslProtocol);
		Set ("KeyType", f.KeyType);
		Set ("CertFile", f.CertificateFile);
		Set ("KeyFile", f.KeyFile);
		Set ("PasswordOptions", f.PasswordOptions);
		Set ("Password", f.Password);
		Set ("Verbose", f.Verbose ? "true" : "false");
		return SavePreservingFormat (doc, csprojPath);
	}

	// ---------- ASP.NET Core run configuration (launchSettings.json) ----------
	// The legacy AspNetCoreRunConfiguration edits a launchSettings.json profile
	// (LaunchProfileProvider / LaunchProfileData): the profile's launchBrowser,
	// launchUrl and applicationUrl (the latter stored flat under the profile, the
	// legacy OtherSettings serialization). The file lives at
	// <projectDir>/Properties/launchSettings.json. Unlike the .csproj/.sln/.vbproj
	// writers above, this one re-serializes the JSON (the legacy provider does not
	// preserve byte-for-byte format either); indentation matches the VS-generated
	// file (2 spaces).

	/// <summary>The path of an ASP.NET Core project's launchSettings.json
	/// (<c>Properties/launchSettings.json</c> next to the project file).</summary>
	public static string GetAspNetCoreLaunchSettingsPath (string csprojPath)
		=> Path.Combine (Path.GetDirectoryName (csprojPath)!, "Properties", "launchSettings.json");

	/// <summary>The profile names of an ASP.NET Core project's launchSettings.json, in
	/// document order. Empty when the file is absent or has no profiles (the legacy
	/// provider treats a missing file as no profiles).</summary>
	public static IReadOnlyList<string> GetAspNetCoreRunProfiles (string csprojPath)
	{
		var path = GetAspNetCoreLaunchSettingsPath (csprojPath);
		if (!File.Exists (path))
			return Array.Empty<string> ();
		try {
			var root = JsonNode.Parse (File.ReadAllText (path));
			var profiles = root?["profiles"] as JsonObject;
			if (profiles is null)
				return Array.Empty<string> ();
			return profiles.Select (kv => kv.Key).ToList ();
		} catch {
			return Array.Empty<string> ();
		}
	}

	/// <summary>One ASP.NET Core run profile's editor-relevant fields.
	/// ApplicationUrl is the profile's flat "applicationUrl" (the legacy
	/// LaunchProfileData.OtherSettings["applicationUrl"]); LaunchBrowser defaults to
	/// the legacy true; LaunchUrl to empty. Returns null when the profile does not
	/// exist.</summary>
	public sealed record AspNetCoreRunProfileFields (
		string Name, bool LaunchBrowser, string LaunchUrl, string ApplicationUrl);

	/// <summary>Reads one ASP.NET Core run profile's fields from launchSettings.json.
	/// Returns null when the file, the profiles object, or the named profile is absent.</summary>
	public static AspNetCoreRunProfileFields? ReadAspNetCoreRunProfile (string csprojPath, string profileName)
	{
		var path = GetAspNetCoreLaunchSettingsPath (csprojPath);
		if (!File.Exists (path))
			return null;
		JsonNode? root;
		try {
			root = JsonNode.Parse (File.ReadAllText (path));
		} catch {
			return null;
		}
		var profile = root?["profiles"] is JsonObject profiles && profiles[profileName] is JsonObject po ? po : null;
		if (profile is null)
			return null;
		return new AspNetCoreRunProfileFields (
			profileName,
			profile["launchBrowser"]?.GetValue<bool> () ?? true,
			profile["launchUrl"]?.GetValue<string> () ?? "",
			profile["applicationUrl"]?.GetValue<string> () ?? "");
	}

	/// <summary>Writes one ASP.NET Core run profile's fields into launchSettings.json,
	/// creating the file / profiles object / profile object when absent (the legacy
	/// provider creates them on first save). launchBrowser is always written; launchUrl
	/// and applicationUrl are written when non-empty and removed when empty (the legacy
	/// LaunchProfileData serialization omits null/empty values). Returns false without
	/// touching the file when the profile cannot be located or the file cannot be saved.</summary>
	public static bool SaveAspNetCoreRunProfile (string csprojPath, string profileName, AspNetCoreRunProfileFields f)
	{
		var path = GetAspNetCoreLaunchSettingsPath (csprojPath);
		JsonNode? root;
		if (File.Exists (path)) {
			try {
				root = JsonNode.Parse (File.ReadAllText (path));
			} catch {
				return false;
			}
			if (root is null)
				return false;
		} else {
			root = new JsonObject ();
		}
		if (root["profiles"] is not JsonObject profiles) {
			profiles = new JsonObject ();
			root["profiles"] = profiles;
		}
		if (profiles[profileName] is not JsonObject profile) {
			profile = new JsonObject { ["commandName"] = "Project" };
			profiles[profileName] = profile;
		}
		profile["launchBrowser"] = f.LaunchBrowser;
		if (f.LaunchUrl.Length > 0)
			profile["launchUrl"] = f.LaunchUrl;
		else
			profile.Remove ("launchUrl");
		if (f.ApplicationUrl.Length > 0)
			profile["applicationUrl"] = f.ApplicationUrl;
		else
			profile.Remove ("applicationUrl");
		try {
			Directory.CreateDirectory (Path.GetDirectoryName (path)!);
			File.WriteAllText (path, root.ToJsonString (new System.Text.Json.JsonSerializerOptions {
				WriteIndented = true
			}));
		} catch {
			return false;
		}
		return true;
	}
}
