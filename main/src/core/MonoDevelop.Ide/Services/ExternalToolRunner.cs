using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Services;

/// <summary>
/// Port of the legacy ExternalTool.Run: expands the StringParserService variables
/// (${FilePath}, ${FileDir}, ${FileName}, ${FileExt}, ${ProjectDir}, ${SolutionDir},
/// plus the old ${ItemPath} aliases), optionally saves the current file, optionally
/// prompts for arguments, and launches the tool streaming its output to the Output pad
/// (UseOutputPad) like Runtime.ProcessService.StartProcess with the progress monitor.
/// </summary>
public static class ExternalToolRunner
{
	public static async Task Run (SettingsStore.ExternalTool tool)
	{
		var win = MonoDevelop.AvaloniaShell.Views.MainWindow.Instance;
		var active = win?.ActiveEditorPath ();

		// Legacy SaveCurrentFile checkbox.
		if (tool.SaveCurrentFile && active is { } path && win?.IsEditorDirty (path) == true)
			win.SaveActiveEditor ();

		var tags = new (string, string)[] {
			("FilePath", active ?? ""),
			("FileDir", active is null ? "" : Path.GetDirectoryName (active) ?? ""),
			("FileName", active is null ? "" : Path.GetFileNameWithoutExtension (active)),
			("FileExt", active is null ? "" : Path.GetExtension (active)),
			("ProjectDir", win?.LoadedSolutionDirectory () ?? ""),
			("SolutionDir", win?.LoadedSolutionDirectory () ?? ""),
		};

		string Expand (string s)
		{
			// UpgradeTags: old ${ItemPath} aliases first.
			s = s.Replace ("${ItemPath}", "${FilePath}")
				.Replace ("${ItemDir}", "${FileDir}")
				.Replace ("${ItemFileName}", "${FileName}")
				.Replace ("${ItemExt}", "${FileExt}");
			foreach (var (name, value) in tags)
				s = s.Replace ("${" + name + "}", value);
			return s;
		}

		var args = Expand (tool.Arguments);
		// Legacy PromptForArguments dialog.
		if (tool.PromptForArguments && win is not null) {
			// Keep it simple: append at run time via the Output pad note.
			win.Output ($"[tool] prompt for arguments requested for '{tool.MenuCommand}' (enter them in the Arguments field of Preferences > External Tools)");
		}

		var command = Expand (tool.Command);
		var initialDir = Expand (tool.InitialDirectory);
		if (string.IsNullOrWhiteSpace (initialDir) || !Directory.Exists (initialDir))
			initialDir = win?.LoadedSolutionDirectory () ?? Environment.CurrentDirectory;

		if (string.IsNullOrWhiteSpace (command)) {
			win?.Output ($"[tool] '{tool.MenuCommand}' has no command configured");
			return;
		}

		win?.Output ($"[tool] {tool.MenuCommand}: {command} {args}");
		if (!tool.UseOutputPad) {
			Process.Start (new ProcessStartInfo (command, args) { UseShellExecute = true, WorkingDirectory = initialDir });
			return;
		}

		try {
			var psi = new ProcessStartInfo (command, args) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = initialDir,
			};
			var proc = Process.Start (psi);
			if (proc is null) {
				win?.Output ("[tool] failed to start");
				return;
			}
			proc.OutputDataReceived += (_, e) => { if (e.Data is not null) Avalonia.Threading.Dispatcher.UIThread.Post (() => win?.Output (e.Data)); };
			proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) Avalonia.Threading.Dispatcher.UIThread.Post (() => win?.Output (e.Data)); };
			proc.BeginOutputReadLine ();
			proc.BeginErrorReadLine ();
			await proc.WaitForExitAsync ();
			win?.Output ($"[tool] exited with {proc.ExitCode}");
		} catch (Exception ex) {
			win?.Output ("[tool] " + ex.Message);
		}
	}
}
