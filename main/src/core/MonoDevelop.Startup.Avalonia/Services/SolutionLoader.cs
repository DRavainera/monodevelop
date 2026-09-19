using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MonoDevelop.AvaloniaShell.Services;

public record SolutionEntry (string Name, string ProjectPath, bool IsFolder, string? Parent);

/// <summary>
/// Loads solution data for the Solution pad using Microsoft.Build's SolutionFile
/// (loaded by reflection from the IDE runtime or the installed SDK), mirroring how
/// the IDE locates MSBuild without hard-wiring a package reference.
/// </summary>
public static class SolutionLoader
{
	static Assembly? msbuildAsm;

	public static (string Title, IReadOnlyList<SolutionEntry> Projects)? Load (string slnPath)
	{
		try {
			var asm = GetMicrosoftBuild ();
			if (asm is null)
				return null;

			var sfType = asm.GetType ("Microsoft.Build.Construction.SolutionFile", true)!;
			var parse = sfType.GetMethod ("Parse", BindingFlags.Public | BindingFlags.Static)
				?? throw new MissingMethodException ("SolutionFile.Parse not found");
			var sf = parse.Invoke (null, new object?[] { slnPath });

			var projectsProp = sfType.GetProperty ("ProjectsInOrder")!;
			var projects = (System.Collections.IEnumerable)projectsProp.GetValue (sf)!;

			var projectTypeType = asm.GetType ("Microsoft.Build.Construction.SolutionProjectType", true)!;
			var solutionFolder = Enum.Parse (projectTypeType, "SolutionFolder");
			var unknownType = Enum.Parse (projectTypeType, "Unknown");

			var guidToName = new Dictionary<string, string> ();
			var entries = new List<SolutionEntry> ();

			foreach (var p in projects) {
				var pType = p.GetType ();
				var name = (string?)pType.GetProperty ("ProjectName")?.GetValue (p) ?? "";
				var relPath = (string?)pType.GetProperty ("RelativePath")?.GetValue (p) ?? "";
				var absPath = (string?)pType.GetProperty ("AbsolutePath")?.GetValue (p) ?? "";
				var guid = (string?)pType.GetProperty ("ProjectGuid")?.GetValue (p);
				var parentGuid = (string?)pType.GetProperty ("ParentProjectGuid")?.GetValue (p);
				var type = pType.GetProperty ("ProjectType")?.GetValue (p);

				if (guid is not null)
					guidToName [guid] = name;

				bool isFolder = Equals (type, solutionFolder) || Equals (type, unknownType);
				if (!isFolder && string.IsNullOrEmpty (relPath))
					continue;

				string? parent = null;
				if (parentGuid is { Length: > 0 } && guidToName.TryGetValue (parentGuid, out var pn))
					parent = pn;

				entries.Add (new SolutionEntry (name, isFolder ? "" : absPath, isFolder, parent));
			}

			var title = Path.GetFileNameWithoutExtension (slnPath);
			return (title, entries);
		} catch (Exception ex) {
			Console.WriteLine ("[solution] load failed: " + ex.Message);
			return null;
		}
	}

	static Assembly GetMicrosoftBuild ()
	{
		if (msbuildAsm is not null)
			return msbuildAsm;

		// Prefer the IDE runtime dir (net10run, when the shell is staged next to it),
		// then the .NET SDK installation.
		string? shellDir = Path.GetDirectoryName (typeof (SolutionLoader).Assembly.Location);
		string? candidate = shellDir is null ? null : Path.Combine (shellDir, "Microsoft.Build.dll");
		if (candidate is null || !File.Exists (candidate)) {
			var dotnetRoot = Environment.GetEnvironmentVariable ("DOTNET_ROOT") ?? "/usr/share/dotnet";
			var home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty (dotnetRoot) || !Directory.Exists (dotnetRoot))
				dotnetRoot = Path.Combine (home, ".dotnet");
			var sdkDir = Directory.GetDirectories (Path.Combine (dotnetRoot, "sdk"))
				.OrderByDescending (d => Version.TryParse (Path.GetFileName (d), out var v) ? v : new Version (0, 0))
				.FirstOrDefault ();
			candidate = sdkDir is null ? null : Path.Combine (sdkDir, "Microsoft.Build.dll");
		}

		msbuildAsm = Assembly.LoadFrom (candidate!);
		return msbuildAsm;
	}
}
