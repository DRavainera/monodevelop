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

	// C# project type GUID (the FAE04EC0 the legacy CSharpBinding registers) and
	// the solution folder type GUID: folders reference no project file and the
	// legacy writer skips them for build configurations (SupportsBuild == false).
	// SlnFile stores TypeGuid verbatim as the quoted text of the Project line —
	// braces included, exactly as the .sln format writes it.
	const string CSharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
	const string SolutionFolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D4EEDE}";
	// The other GUID spelling real .sln files carry (the one the legacy
	// MSBuildProjectService.FolderTypeGuid uses): both mark a solution folder.
	const string SolutionFolderTypeGuidAlt = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

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

	/// <summary>Absolute paths of the .csproj projects referenced by the .sln
	/// (real model: SlnFile.Projects, paths resolved against the solution
	/// directory). A directory scan is only a fallback for slns that yield
	/// nothing — a referenced project may live outside the solution dir.</summary>
	public static List<string> GetSolutionProjects (string slnPath)
	{
		var slnDir = Path.GetDirectoryName (slnPath)!;
		var projs = new List<string> ();
		try {
			foreach (var p in ReadSln (slnPath).Projects) {
				if (p.FilePath == null || !p.FilePath.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase))
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

	/// <summary>Adds an existing .csproj into an existing .sln (legacy
	/// ProjectOperations.AddSolutionItem): Project entry with a fresh GUID plus the
	/// ActiveCfg/Build.0 mappings per existing configuration.</summary>
	public static void AppendProjectToSolution (string csprojPath, string slnPath)
	{
		var sln = ReadSln (slnPath);
		var guid = Guid.NewGuid ().ToString ("B").ToUpperInvariant (); // braced, as the .sln format stores it
		var projName = Path.GetFileNameWithoutExtension (csprojPath);
		var projRel = Path.GetRelativePath (Path.GetDirectoryName (slnPath)!, csprojPath).Replace ('\\', '/');
		sln.Projects.Add (new SlnProject {
			TypeGuid = CSharpProjectTypeGuid,
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
			TypeGuid = CSharpProjectTypeGuid,
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
		/// <summary>Conteo de items DECLARADOS en el fichero (no evaluados por
		/// MSBuild): Compile/None/Content/EmbeddedResource, solo los > 0.</summary>
		public IReadOnlyDictionary<string, int> ItemCounts { get; }

		internal ProjectFileModel (string path, string name, string? outputType,
			IReadOnlyList<string> targetFrameworks, string? outputPath, string? baseOutputPath,
			IReadOnlyList<string> configurations, IReadOnlyList<string> frameworkRefs,
			IReadOnlyList<string> packageRefs, IReadOnlyList<string> assemblyRefs,
			IReadOnlyList<string> projectRefs, IReadOnlyDictionary<string, int> itemCounts)
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
			ItemCounts = itemCounts;
		}
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
			itemCounts: itemCounts);
	}
}
