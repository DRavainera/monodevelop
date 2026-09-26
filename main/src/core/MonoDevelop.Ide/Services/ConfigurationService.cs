using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Reads and persists configurations exactly where the legacy ProjectService
/// stores them: SolutionConfigurationPlatforms/ProjectConfigurationPlatforms in
/// the .sln global section, and PropertyGroup Condition entries in each .csproj
/// ('Name|Platform'). "Any CPU" maps to the MSBuild platform "AnyCPU" in project
/// files and "Any CPU" in the solution file (the legacy ItemConfiguration mapping).
/// </summary>
public static class ConfigurationService
{
	public const string AnyCpuSolution = "Any CPU";
	public const string AnyCpuProject = "AnyCPU";

	/// <summary>Solution configuration ids, e.g. "Debug|Any CPU" keys minus the
	/// platform → "Debug", "Debug|x86" (legacy ItemConfigurationCollection keys).</summary>
	public static IReadOnlyList<string> GetSolutionConfigurations (string slnPath)
	{
		var result = new List<string> ();
		if (!File.Exists (slnPath))
			return result;
		bool inSection = false;
		foreach (var raw in File.ReadAllLines (slnPath)) {
			var line = raw.Trim ();
			if (line.StartsWith ("GlobalSection(SolutionConfigurationPlatforms", StringComparison.Ordinal)) {
				inSection = true;
				continue;
			}
			if (inSection && line.StartsWith ("EndGlobalSection", StringComparison.Ordinal))
				break;
			if (inSection) {
				var eq = line.IndexOf (" = ", StringComparison.Ordinal);
				if (eq > 0) {
					var id = line.Substring (0, eq).Trim ();
					var bar = id.IndexOf ('|');
					var name = bar < 0 ? id : id.Substring (0, bar);
					if (!result.Contains (name, StringComparer.OrdinalIgnoreCase))
						result.Add (name);
				}
			}
		}
		return result;
	}

	/// <summary>Creates the configuration in the .sln (solution config + project
	/// config mappings for every project GUID) and in each project's .csproj
	/// (PropertyGroup Condition), like ProjectOperations.AddConfiguration.
	/// Returns false when the config already exists.</summary>
	public static bool AddSolutionConfiguration (string slnPath, string name, string platformDisplayName, bool createChildren)
	{
		if (!File.Exists (slnPath))
			throw new FileNotFoundException (slnPath);
		var platform = platformDisplayName == AnyCpuSolution ? AnyCpuSolution : platformDisplayName;
		var slnId = $"{name}|{platform}";
		var projPlatform = platform == AnyCpuSolution ? AnyCpuProject : platform;
		var projId = $"{name}|{projPlatform}";

		var lines = File.ReadAllLines (slnPath).ToList ();
		var existing = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		int globalSection = -1, endGlobal = -1;
		bool inCfgSection = false, inProjCfgSection = false;
		int projCfgInsert = -1;
		var projectGuids = new List<string> ();

		for (int i = 0; i < lines.Count; i++) {
			var t = lines [i].Trim ();
			if (t.StartsWith ("GlobalSection(SolutionConfigurationPlatforms", StringComparison.Ordinal)) {
				inCfgSection = true;
				globalSection = i;
				continue;
			}
			if (t.StartsWith ("GlobalSection(ProjectConfigurationPlatforms", StringComparison.Ordinal)) {
				inProjCfgSection = true;
				projCfgInsert = i + 1;
				continue;
			}
			if (t.StartsWith ("EndGlobalSection", StringComparison.Ordinal)) {
				inCfgSection = inCfgSection && false;
				if (inProjCfgSection) {
					inProjCfgSection = false;
					projCfgInsert = i;
				}
				continue;
			}
			if (inCfgSection && t.Contains (" = ")) {
				existing.Add (t.Substring (0, t.IndexOf (" = ", StringComparison.Ordinal)).Trim ());
				if (globalSection >= 0 && lines [i].EndsWith ("EndGlobalSection", StringComparison.Ordinal))
					globalSection = i;
			}
			if (inProjCfgSection && t.Contains (" = ")) {
				existing.Add (t.Substring (0, t.IndexOf (" = ", StringComparison.Ordinal)).Trim ());
				projCfgInsert = i + 1;
			}
			if (t.StartsWith ("Project(", StringComparison.Ordinal)) {
				// Project("{TYPE-GUID}") = "Name", "path", "{PROJECT-GUID}"
				var open = t.LastIndexOf ("{", StringComparison.Ordinal);
				var close = t.LastIndexOf ("}", StringComparison.Ordinal);
				if (open > 0 && close > open)
					projectGuids.Add (t.Substring (open, close - open + 1));
			}
			if (t.StartsWith ("EndGlobal", StringComparison.Ordinal))
				endGlobal = i;
		}

		if (existing.Contains (slnId) || existing.Contains (projId))
			return false;

		var outLines = new List<string> (lines);
		// Solution config entry right before EndGlobalSection of the configs.
		int cfgEnd = FindSectionEnd (outLines, "GlobalSection(SolutionConfigurationPlatforms");
		if (cfgEnd > 0)
			outLines.Insert (cfgEnd, $"\t\t{slnId} = {slnId}");

		// Per-project mapping entries (legacy copies the Debug template: build+deploy flags).
		int prjEnd = FindSectionEnd (outLines, "GlobalSection(ProjectConfigurationPlatforms");
		if (prjEnd > 0) {
			var block = new List<string> ();
			foreach (var g in projectGuids) {
				block.Add ($"\t\t{{{g.Trim ('{', '}')}}}.{projId}.ActiveCfg = {projId}");
				block.Add ($"\t\t{{{g.Trim ('{', '}')}}}.{projId}.Build.0 = {projId}");
			}
			outLines.InsertRange (prjEnd, block);
		}
		File.WriteAllLines (slnPath, outLines, Encoding.UTF8);

		if (createChildren)
			foreach (var csproj in Directory.GetFiles (Path.GetDirectoryName (slnPath)!, "*.csproj", SearchOption.AllDirectories)
				.Where (p => !p.Contains ("/obj/") && !p.Contains ("/bin/")))
				AddProjectConfiguration (csproj, name, projPlatform);
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

	static int FindSectionEnd (List<string> lines, string sectionStart)
	{
		bool inside = false;
		for (int i = 0; i < lines.Count; i++) {
			var t = lines [i].Trim ();
			if (!inside && t.StartsWith (sectionStart, StringComparison.Ordinal))
				inside = true;
			else if (inside && t.StartsWith ("EndGlobalSection", StringComparison.Ordinal))
				return i;
		}
		return -1;
	}

	/// <summary>Adds an existing .csproj into an existing .sln (legacy
	/// ProjectOperations.AddSolutionItem): Project entry with a fresh GUID plus the
	/// Debug/Release ActiveCfg/Build.0 mappings per configuration.</summary>
	public static void AppendProjectToSolution (string csprojPath, string slnPath)
	{
		var guid = Guid.NewGuid ().ToString ("B").ToUpperInvariant ();
		var projName = Path.GetFileNameWithoutExtension (csprojPath);
		var projRel = Path.GetRelativePath (Path.GetDirectoryName (slnPath)!, csprojPath).Replace ('\\', '/');
		var lines = File.ReadAllLines (slnPath).ToList ();
		int firstEndProject = lines.FindIndex (l => l.Trim () == "EndProject");
		if (firstEndProject < 0)
			throw new InvalidOperationException ("Malformed solution (no EndProject)");
		lines.Insert (firstEndProject + 1, $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projName}\", \"{projRel}\", \"{guid}\"");
		lines.Insert (firstEndProject + 2, "EndProject");

		var g = guid.Trim ('{', '}');
		var block = new List<string> ();
		var solConfigs = GetSolutionConfigurations (slnPath);
		foreach (var cfg in solConfigs.Count > 0 ? solConfigs : new List<string> { "Debug", "Release" }) {
			block.Add ($"\t\t{g}.{cfg}|AnyCPU.ActiveCfg = {cfg}|AnyCPU");
			block.Add ($"\t\t{g}.{cfg}|AnyCPU.Build.0 = {cfg}|AnyCPU");
		}
		int prjCfg = lines.FindIndex (l => l.Trim ().StartsWith ("GlobalSection(ProjectConfigurationPlatforms", StringComparison.Ordinal));
		if (prjCfg >= 0) {
			int endSection = prjCfg + 1;
			while (endSection < lines.Count && !lines [endSection].Trim ().StartsWith ("EndGlobalSection", StringComparison.Ordinal))
				endSection++;
			lines.InsertRange (endSection, block);
		} else {
			// Solutions without per-project mappings (hand-written slns): add the section.
			int global = lines.FindIndex (l => l.Trim () == "Global");
			if (global < 0)
				throw new InvalidOperationException ("Malformed solution (no Global)");
			lines.InsertRange (global + 1, new List<string> {
				"\tGlobalSection(ProjectConfigurationPlatforms) = postSolution",
			});
			lines.InsertRange (global + 2, block);
			lines.Insert (global + 2 + block.Count, "\tEndGlobalSection");
		}
		File.WriteAllLines (slnPath, lines, System.Text.Encoding.UTF8);
	}

	/// <summary>Imports a loose .csproj as a new .sln next to it (legacy
	/// ProjectOperations.ImportProject → creates the solution file with one
	/// project entry, its GUID, and Debug/Release config mappings). Returns the
	/// created .sln path.</summary>
	public static string AddProjectToSolution (string csprojPath, string slnPath)
	{
		var guid = Guid.NewGuid ().ToString ("B").ToUpperInvariant ();
		var projName = Path.GetFileNameWithoutExtension (csprojPath);
		var projRel = Path.GetRelativePath (Path.GetDirectoryName (slnPath)!, csprojPath).Replace ('\\', '/');
		var projGuid = Guid.NewGuid ().ToString ("B").ToUpperInvariant (); // C# project type
		_ = guid;
		var sln = new StringBuilder ();
		sln.AppendLine ("");
		sln.AppendLine ("Microsoft Visual Studio Solution File, Format Version 12.00");
		sln.AppendLine ($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projName}\", \"{projRel}\", \"{projGuid}\"");
		sln.AppendLine ("EndProject");
		sln.AppendLine ("Global");
		sln.AppendLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
		sln.AppendLine ("\t\tDebug|Any CPU = Debug|Any CPU");
		sln.AppendLine ("\t\tRelease|Any CPU = Release|Any CPU");
		sln.AppendLine ("\tEndGlobalSection");
		sln.AppendLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
		var g = projGuid.Trim ('{', '}');
		foreach (var cfg in new[] { "Debug", "Release" })
			foreach (var flag in new[] { "ActiveCfg", "Build.0" })
				sln.AppendLine ($"\t\t{g}.{cfg}|AnyCPU.{flag} = {cfg}|AnyCPU");
		sln.AppendLine ("\tEndGlobalSection");
		sln.AppendLine ("\tGlobalSection(SolutionProperties) = preSolution");
		sln.AppendLine ("\t\tHideSolutionNode = FALSE");
		sln.AppendLine ("\tEndGlobalSection");
		sln.AppendLine ("EndGlobal");
		File.WriteAllText (slnPath, sln.ToString ().Replace ("\r\n", "\n"), Encoding.UTF8);
		return slnPath;
	}
}
