using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
	/// .csproj and .fsproj (the F# SDK-style project, Phase 3 of the F# port:
	/// the project model and build/run gates see F# projects the same way as C#).</summary>
	public static bool IsProjectFile (string path)
	{
		if (string.IsNullOrEmpty (path))
			return false;
		return path.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith (".fsproj", StringComparison.OrdinalIgnoreCase);
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
}
