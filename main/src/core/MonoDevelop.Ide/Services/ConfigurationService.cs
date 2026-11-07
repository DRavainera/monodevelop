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

	static SlnFile ReadSln (string slnPath)
	{
		if (!File.Exists (slnPath))
			throw new FileNotFoundException (slnPath);
		var sln = new SlnFile ();
		sln.Read (slnPath);
		return sln;
	}

	static bool IsSolutionFolder (SlnProject p)
		=> p.TypeGuid != null && p.TypeGuid.Equals (SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase);

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
}
