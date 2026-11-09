// ProjectCommands.cs
//
// Author:
//   Mike Krüger (mkrueger@novell.com)
//   Lluis Sanchez Gual (lluis@novell.com)
//   Michael Hutchinson (mhutchinson@novell.com)
//   Ankit Jain (jankit@novell.com)
//   Marek Sieradzki  <marek.sieradzki@gmail.com>
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

#if !AVALONIA_SHELL
using System;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Execution;
using CustomCommand = MonoDevelop.Projects.CustomCommand;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.FeatureConfiguration;
using MonoDevelop.Ide.Projects.FileNesting;
#else
using MonoDevelop.Components.Commands;
using MonoDevelop.Core; // the shell-side GettextCatalog shim (CoreGettextShim)
#endif

namespace MonoDevelop.Ide.Commands
{
	public enum ProjectCommands
	{
		AddNewProject,
		AddNewWorkspace,
		AddNewSolution,
		AddSolutionFolder,
		AddProject,
		AddItem,
		RemoveFromProject,
		Options,
		SolutionOptions,
		ProjectOptions,
		
		AddReference,
		AddNewFiles,
		AddFiles,
		NewFolder,
		AddFilesFromFolder,
		AddExistingFolder,
		IncludeToProject,
		BuildSolution,
		Build,
		RebuildSolution,
		Rebuild,
		SetAsStartupProject,
		Run,
		RunWithList,
		RunEntry,
		RunEntryWithList,
		Clean,
		CleanSolution,
		LocalCopyReference,
		Stop,
		ConfigurationSelector,
		CustomCommandList,
		Reload,
		ExportSolution,
		SpecificAssemblyVersion,
		SelectActiveConfiguration,
		SelectActiveRuntime,
		EditSolutionItem,
		Unload,
		SetStartupProjects,
		AddEmptyClass,
		ToggleFileNesting,
		ExcludeFromProject
	}

#if !AVALONIA_SHELL
	// Oleada 3 (cleanup, QA F-5e): renamed SolutionOptionsHandler →
	// SolutionOptionsMenuHandler to stop the class name reading as the
	// ProjectCommands.SolutionOptions command itself (and to keep it apart
	// from SolutionItemOptionsHandler, which gates the pad "Properties").
	internal class SolutionOptionsMenuHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}
	
	internal class ProjectOptionsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as Project;
			info.Enabled = project != null;
			info.Text = project != null ? GettextCatalog.GetString ("{0} _Options", project.Name) : GettextCatalog.GetString ("Project _Options");
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedObject);
		}
	}
	
	internal class SolutionItemOptionsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedBuildTarget != null;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedObject);
		}
	}

	internal class SetStartupProjectsHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolution?.GetAllProjects ()?.Skip (1)?.Any () ?? false;
		}

		protected override void Run ()
		{
			var sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol != null) {
				MultiItemSolutionRunConfiguration config = null;
				if (!sol.MultiStartupRunConfigurations.Any ()) {
					Xwt.Toolkit.NativeEngine.Invoke (() => {
						using (var dlg = new NewSolutionRunConfigurationDialog ()) {
							if (dlg.Run (IdeServices.DesktopService.GetFocusedTopLevelWindow ()).Id == "create") {
								config = new MultiItemSolutionRunConfiguration (dlg.RunConfigurationName, dlg.RunConfigurationName);
								sol.MultiStartupRunConfigurations.Add (config);
								sol.StartupConfiguration = config;
							}
						}
					});
				} else {
					config = sol.MultiStartupRunConfigurations.FirstOrDefault ();
				}

				// Show run configurations dialog
				if (config != null) {
					IdeApp.ProjectOperations.ShowRunConfiguration (sol, config);
				}
			}
		}
	}

	internal class EditReferencesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (!(IdeApp.ProjectOperations.CurrentSelectedProject is DotNetProject)) {
				info.Enabled = false;
				info.Bypass = true;
			}
			else
				info.Bypass = false;
		}

		protected override async void Run ()
		{
			//Edit references
			DotNetProject p = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (IdeApp.ProjectOperations.AddReferenceToProject (p))
				await IdeApp.ProjectOperations.SaveAsync (p);

		}
	}

	internal class BuildSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ((IdeApp.ProjectOperations.CurrentSelectedSolution != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
		}

		protected override void Run ()
		{
			//Build solution
			IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}

	internal class BuildHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
				info.Enabled = (buildTarget != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) && buildTarget.CanBuild (IdeApp.Workspace.ActiveConfiguration);
				if (buildTarget != null) {
					info.Text = GettextCatalog.GetString ("B_uild {0}", buildTarget.Name.Replace ("_","__"));
					if (buildTarget is SolutionFolder)
						info.Description = GettextCatalog.GetString ("Build solution {0}", buildTarget.Name);
					else if (buildTarget is Project)
						info.Description = GettextCatalog.GetString ("Build project {0}", buildTarget.Name);
					else
						info.Description = GettextCatalog.GetString ("Build {0}", buildTarget.Name);
				}
			}
			else
				info.Enabled = false;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
	}

	internal class RebuildSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ((IdeApp.ProjectOperations.CurrentSelectedSolution != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted));
		}

		protected override void Run ()
		{
			//Build solution
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}

	internal class RebuildHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
				info.Enabled = (buildTarget != null) && (IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) && buildTarget.CanBuild (IdeApp.Workspace.ActiveConfiguration);
				if (buildTarget != null) {
					info.Text = GettextCatalog.GetString ("R_ebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name.Replace ("_","__"));
					info.Description = GettextCatalog.GetString ("Rebuild {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
				}
			}
			else
				info.Enabled = false;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
	}

	internal class RunHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if ((IdeApp.Workspace.IsOpen) && (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted))
				info.Text = GettextCatalog.GetString ("Restart Without Debugging");
			else
				info.Text = GettextCatalog.GetString ("Start Without Debugging");

			info.Enabled = CanRun (Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		static IBuildTarget GetRunTarget ()
		{
			return IdeApp.ProjectOperations.CurrentSelectedSolution ?? IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
		}
		
		public static bool CanRun (IExecutionHandler executionHandler)
		{
			if (IdeApp.Workspace.IsOpen) {
				var target = GetRunTarget ();
				return target != null && IdeApp.ProjectOperations.CanExecute (target, executionHandler);
			} else
				return false;
		}

        public static async void RunMethod (IExecutionHandler executionHandler)
        {
            if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("An application is already running. Do you want to stop it?"), AlertButton.Stop))
					return;
                StopHandler.StopBuildOperations ();
                await IdeApp.ProjectOperations.CurrentRunOperation.Task;
            }

			if (IdeApp.Workspace.IsOpen) {
				var target = GetRunTarget ();
				IdeApp.ProjectOperations.Execute (target, executionHandler, true);
			}
        }

		protected override void Run ()
		{
            RunMethod (Runtime.ProcessService.DefaultExecutionHandler);
		}

	}

	internal class RunWithHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol != null && sol.StartupItem != null)
				ExecutionModeCommandService.GenerateExecutionModeCommands (sol.StartupItem, info);
		}

		protected override void Run (object dataItem)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol != null && sol.StartupItem != null)
				ExecutionModeCommandService.ExecuteCommand (sol.StartupItem, dataItem);
		}
	}

	internal class RunEntryHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			IBuildTarget buildTarget = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			info.Enabled = ((buildTarget != null) && (!(buildTarget is Workspace)) && IdeApp.ProjectOperations.CanExecute (buildTarget));

			if (buildTarget is Solution)
				info.Text = GettextCatalog.GetString ("Run Solution");
			else if (buildTarget is Project)
				info.Text = GettextCatalog.GetString ("Run Project");
		}

		protected override void Run ()
		{
			var target = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			IdeApp.ProjectOperations.Execute (target);
		}
	}

	internal class RunEntryWithHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			SolutionItem item = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as SolutionItem;
			if (item != null) {
				ExecutionModeCommandService.GenerateExecutionModeCommands (item, info);
			}
		}

		protected override void Run (object dataItem)
		{
			SolutionItem item = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as SolutionItem;
			if (item != null)
				ExecutionModeCommandService.ExecuteCommand (item, dataItem);
		}
	}

	internal class CleanHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedBuildTarget == null)
				info.Enabled = false;
			else {
				info.Enabled = IdeApp.ProjectOperations.CurrentSelectedBuildTarget.CanBuild (IdeApp.Workspace.ActiveConfiguration);
				info.Text = GettextCatalog.GetString ("C_lean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name.Replace ("_","__"));
				info.Description = GettextCatalog.GetString ("Clean {0}", IdeApp.ProjectOperations.CurrentSelectedBuildTarget.Name);
			}
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentSelectedBuildTarget);
		}
	}

	internal class CleanSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}

		protected override void Run ()
		{
			IdeApp.ProjectOperations.Clean (IdeApp.ProjectOperations.CurrentSelectedSolution);
		}
	}

	public class StopHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if ((IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) && (IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted))
				info.Enabled = false;
		}

		protected override void Run ()
		{
			StopBuildOperations ();
		}
		
		public static void StopBuildOperations ()
		{
			if (!IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentBuildOperation.Cancel ();
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
				IdeApp.ProjectOperations.CurrentRunOperation.Cancel ();
		}
	}

	internal class CustomCommandListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			IConfigurationTarget ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as IConfigurationTarget;
			if (ce != null) {
				ItemConfiguration conf = ce.DefaultConfiguration;
				if (conf != null) {
					foreach (CustomCommand cmd in conf.CustomCommands)
						if (cmd.Type == CustomCommandType.Custom)
							info.Add (cmd.Name, cmd);
				}
			}
		}

		protected override void Run (object dataItem)
		{
			var ce = IdeApp.ProjectOperations.CurrentSelectedBuildTarget as WorkspaceObject;
			CustomCommand cmd = (CustomCommand) dataItem;
			ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor (cmd.Name);
			
			Thread t = new Thread (
				async delegate () {
					using (monitor) {
						try {
							await cmd.Execute (monitor, ce, IdeApp.Workspace.ActiveConfiguration);
						} catch (Exception ex) {
							monitor.ReportError (GettextCatalog.GetString ("Command execution failed"), ex);
						}
					}
				}
			);
			t.IsBackground = true;
			t.Start ();
		}
	}

	internal class ExportSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			// FIXME: Once we fix Workspaces to offer Visual Studio formats (instead of the deprecated MonoDevelop 1.0 format), we can allow exporting of Workspaces as well.
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is Solution) && !(IdeApp.ProjectOperations.CurrentSelectedItem is SolutionItem))
				info.Enabled = false;
		}

		protected override void Run ()
		{
			Solution workspace;
			
			if (!(IdeApp.ProjectOperations.CurrentSelectedItem is WorkspaceItem))
				workspace = ((SolutionItem) IdeApp.ProjectOperations.CurrentSelectedItem).ParentSolution;
			else
				workspace = (Solution) IdeApp.ProjectOperations.CurrentSelectedItem;
			
			IdeApp.ProjectOperations.Export (workspace, null);
		}
	}
	
	internal class SelectActiveConfigurationHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
					var i = info.Add (conf, conf);
					if (conf == IdeApp.Workspace.ActiveConfigurationId)
						i.Checked  = true;
				}
			}
		}

		protected override void Run (object dataItem)
		{
			IdeApp.Workspace.ActiveConfigurationId = (string) dataItem;
		}
	}
	
	internal class SelectActiveRuntimeHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var enabled = FeatureSwitchService.IsFeatureEnabled (FeatureSwitches.RuntimeSelectorFeatureSwitchName);

			if (enabled.GetValueOrDefault () && IdeApp.Workspace.IsOpen && Runtime.SystemAssemblyService.GetTargetRuntimes ().Count () > 1) {
				foreach (var tr in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
					var item = info.Add (tr.DisplayName, tr);
					if (tr == IdeApp.Workspace.ActiveRuntime)
						item.Checked = true;
				}
			}
		}

		protected override void Run (object dataItem)
		{
			IdeApp.Workspace.ActiveRuntime = (MonoDevelop.Core.Assemblies.TargetRuntime) dataItem;
		}
	}
	
	class ApplyPolicyHandler: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null || IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}
		
		protected override void Run ()
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			using (var dlg = new ApplyPolicyDialog ((IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolutionItem ?? (IPolicyProvider)solution)) {
				if (MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok) {
					if (project != null)
						IdeApp.ProjectOperations.SaveAsync (project);
					else
						IdeApp.ProjectOperations.SaveAsync (solution);
				}
			}
		}
	}
	
	class ExportPolicyHandler: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null || IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}
		
		protected override void Run ()
		{
			using (ExportProjectPolicyDialog dlg = new ExportProjectPolicyDialog ((IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolutionItem ?? (IPolicyProvider)IdeApp.ProjectOperations.CurrentSelectedSolution))
				MessageService.ShowCustomDialog (dlg);
		}
	}

	internal class RunCodeAnalysisSolutionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = (IdeApp.ProjectOperations.CurrentSelectedSolution != null) &&
				(IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) &&
				IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ().Any (p => p.SupportsTarget ("RunCodeAnalysis"));
		}

		protected override void Run ()
		{
			var context = new ProjectOperationContext ();
			context.GlobalProperties.SetValue ("RunCodeAnalysisOnce", "true");
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedSolution, context);
		}
	}

	internal class RunCodeAnalysisProjectHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.IsOpen) {
				var project = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (project != null) {
					info.Enabled = project.SupportsTarget ("RunCodeAnalysis");
					info.Text = GettextCatalog.GetString ("Run Code Analysis on {0}", project.Name.Replace ("_","__"));
					return;
				}
			}
			info.Text = GettextCatalog.GetString ("Run Code Analysis on Project");
			info.Enabled = false;
		}

		protected override void Run ()
		{
			var context = new ProjectOperationContext ();
			context.GlobalProperties.SetValue ("RunCodeAnalysisOnce", "true");
			IdeApp.ProjectOperations.Rebuild (IdeApp.ProjectOperations.CurrentSelectedProject, context);
		}
	}

	internal class ToggleFileNestingHandler : CommandHandler
	{
		const string PropertyName = "MonoDevelop.Ide.FileNesting.Enabled";
		protected override void Update (CommandInfo info)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			info.Visible = solution != null && solution.GetAllProjects ().Any (FileNestingService.AppliesToProject);
			if (info.Visible) {
				info.Checked = IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.GetValue<bool> (PropertyName, true);
			}
		}

		protected override void Run ()
		{
			bool enabled = IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.GetValue<bool> (PropertyName, true);
			IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.SetValue (PropertyName, !enabled);
		}
	}
#else
	// Oleada 2: AVALONIA_SHELL ports of the legacy handlers above. Each one
	// delegates to the shell's menu dispatcher (ShellCommandHandler) — the same
	// OnMenuCommand endpoint the catalog router uses — so behavior is
	// unchanged. Oleada 3 (slice 4) ports the solution-level enabled gates
	// (Build/Rebuild/Clean Solution, Run, Run Entry, Solution Options) onto
	// ShellCommandState.HasWorkspace. Oleada 3 (slice 5) adds the in-progress
	// gates: the shell keeps one shared build/run process, so
	// ShellCommandState.IsOperationRunning stands in for the legacy
	// CurrentBuildOperation/CurrentRunOperation in-progress checks — it gates
	// Stop (enabled while an operation runs) and the busy guards of
	// Build/Rebuild Solution, Run and Run Entry. Oleada 3 (slice 6) ports the
	// project-level selected-build-target gates of the main menu (Build/Rebuild/
	// Clean, Project Options, Set Startup Projects) onto
	// ShellCommandState.ActiveProjectCount — for those main-menu items the
	// "the loaded solution has projects" presence check stands in for
	// CurrentSelectedBuildTarget != null (coarser bridge, documented per
	// handler). Oleada 3 (slice 8) ports the context-only pad-selection gates:
	// SolutionItemOptions (Options) and EditReferences (Add Reference) read the
	// solution-pad selected node through ShellCommandState
	// (SelectedNodeIsBuildTarget / SelectedNodeIsProject) — the exact legacy
	// bridge for CurrentSelectedBuildTarget / CurrentSelectedProject is
	// DotNetProject, unlike the slice 6 coarsening. Oleada 3 (slice 9) ports
	// the dynamic labels: Build/Rebuild/Clean ("B_uild {0}" et al. with
	// ShellCommandState.SelectedBuildTargetName), Run ("Restart/Start Without
	// Debugging" on IsOperationRunning), Run Entry ("Run Solution"/"Run
	// Project" on the pad selection) and Project Options ("{0} _Options"). The
	// legacy underscore escapes (Name.Replace ("_","__")) are not portable —
	// Avalonia renders headers literally, so the names go in as-is.
	// ApplyPolicy/ExportPolicy forward their literal catalog id: the
	// ProjectCommands enum has no members for them. Oleada 3 (cleanup, QA
	// F-5e): SolutionOptionsMenuHandler (was SolutionOptionsHandler) — the
	// class name must not read as the SolutionOptions command it backs, and
	// the rename also clears the clash with SolutionItemOptionsHandler below.
	internal class SolutionOptionsMenuHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.SolutionOptions);
		}

		// Oleada 3 (slice 4): enabled when a solution is loaded (legacy:
		// CurrentSelectedSolution != null — exact bridge).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace;
		}
	}

	internal class ProjectOptionsHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.ProjectOptions);
		}

		// Oleada 3 (slice 6 + 9): enabled when the loaded solution has projects
		// (legacy: CurrentSelectedBuildTarget is a Project — the selected target
		// is bridged by the solution's project set). Slice 9 adds the legacy
		// dynamic label: "{0} _Options" with the selected project name when the
		// pad selection is a project row, "Project _Options" otherwise (the
		// catalog label — the mirror restores it unchanged).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && ShellCommandState.ActiveProjectCount > 0;
			var name = ShellCommandState.SelectedNodeIsProject ? ShellCommandState.SelectedBuildTargetName : null;
			info.Text = name is not null
				? GettextCatalog.GetString ("{0} _Options", name)
				: GettextCatalog.GetString ("Project _Options");
		}
	}

	internal class SolutionItemOptionsHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.Options);
		}

		// Oleada 3 (slice 8): enabled when the solution pad has a build target
		// selected (legacy: CurrentSelectedBuildTarget != null — the exact
		// bridge: the shell's pad selection classifies Solution/Project rows,
		// like the legacy IBuildTarget item type).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.SelectedNodeIsBuildTarget;
		}
	}

	internal class SetStartupProjectsHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.SetStartupProjects);
		}

		// Oleada 3 (slice 6): enabled when the loaded solution has projects
		// (legacy: a solution with >= 2 projects — the multi-startup nuance is
		// dropped because the shell's flow marks a single startup project).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && ShellCommandState.ActiveProjectCount > 0;
		}
	}

	internal class EditReferencesHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.AddReference);
		}

		// Oleada 3 (slice 8): enabled only when the pad selection is a project
		// row (legacy: CurrentSelectedProject is DotNetProject — every shell
		// project row is a .csproj). Bypass mirrors the legacy keybinding
		// behavior while the gate is off.
		protected override void Update (CommandInfo info)
		{
			if (!ShellCommandState.SelectedNodeIsProject) {
				info.Enabled = false;
				info.Bypass = true;
			} else
				info.Bypass = false;
		}
	}

	internal class BuildSolutionHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.BuildSolution);
		}

		// Oleada 3 (slice 4 + 5): enabled when a solution is loaded and no
		// build/run operation is in progress (legacy: CurrentSelectedSolution
		// != null && CurrentBuildOperation.IsCompleted — the shell's shared
		// process also covers the run operation, so IsOperationRunning is a
		// slightly coarser bridge).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && !ShellCommandState.IsOperationRunning;
		}
	}

	internal class BuildHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.Build);
		}

		// Oleada 3 (slice 6 + 9): enabled when the loaded solution has projects
		// and no build/run operation is in progress (legacy:
		// CurrentSelectedBuildTarget != null && CurrentBuildOperation.IsCompleted
		// && CanBuild — the selected target is bridged by the solution's project
		// set and CanBuild is not modeled, both documented coarsenings). Slice 9
		// adds the legacy dynamic "B_uild {0}" label; the legacy underscore
		// escape (Name.Replace ("_","__")) is not portable — Avalonia renders
		// the header literally, so the name goes in as-is.
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && ShellCommandState.ActiveProjectCount > 0 && !ShellCommandState.IsOperationRunning;
			var name = ShellCommandState.SelectedBuildTargetName;
			if (name is not null)
				info.Text = GettextCatalog.GetString ("B_uild {0}", name);
		}
	}

	internal class RebuildSolutionHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.RebuildSolution);
		}

		// Oleada 3 (slice 4 + 5): enabled when a solution is loaded and no
		// build/run operation is in progress (legacy: CurrentSelectedSolution
		// != null && CurrentBuildOperation.IsCompleted — the shell's shared
		// process also covers the run operation, so IsOperationRunning is a
		// slightly coarser bridge).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && !ShellCommandState.IsOperationRunning;
		}
	}

	internal class RebuildHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.Rebuild);
		}

		// Oleada 3 (slice 6 + 9): enabled when the loaded solution has projects
		// and no build/run operation is in progress (legacy:
		// CurrentSelectedBuildTarget != null && CurrentBuildOperation.IsCompleted
		// && CanBuild — the selected target is bridged by the solution's project
		// set and CanBuild is not modeled, both documented coarsenings). Slice 9
		// adds the legacy dynamic "R_ebuild {0}" label (the underscore escape is
		// not portable — Avalonia renders the header literally).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && ShellCommandState.ActiveProjectCount > 0 && !ShellCommandState.IsOperationRunning;
			var name = ShellCommandState.SelectedBuildTargetName;
			if (name is not null)
				info.Text = GettextCatalog.GetString ("R_ebuild {0}", name);
		}
	}

	internal class RunHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.Run);
		}

		// Oleada 3 (slice 4 + 5 + 9): enabled when a solution is loaded and no
		// build/run operation is in progress (legacy: Workspace.IsOpen && CanRun
		// — the legacy "an application is already running" confirmation dialog
		// is replaced by the busy guard). Slice 9 adds the legacy dynamic
		// label: "Restart Without Debugging" while a run operation is in
		// progress (shown disabled by the busy guard), "Start Without
		// Debugging" otherwise (the catalog/model label — the mirror restores
		// it unchanged).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && !ShellCommandState.IsOperationRunning;
			info.Text = ShellCommandState.IsOperationRunning
				? GettextCatalog.GetString ("Restart Without Debugging")
				: GettextCatalog.GetString ("Start Without Debugging");
		}
	}

	internal class RunWithHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ProjectCommands.RunWithList, dataItem);
		}
	}

	internal class RunEntryHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.RunEntry);
		}

		// Oleada 3 (slice 4 + 5 + 9): enabled when a solution is loaded and no
		// build/run operation is in progress (legacy: build target != null &&
		// CanExecute — the CanExecute in-progress part is bridged by
		// IsOperationRunning). Slice 9 adds the legacy dynamic label:
		// "Run Solution" for a selected solution row, "Run Project" for a
		// selected project row, unchanged (catalog "_Run Item") without a pad
		// selection (legacy: CurrentSelectedBuildTarget is Solution/Project
		// respectively; no selection left the label untouched).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && !ShellCommandState.IsOperationRunning;
			if (ShellCommandState.SelectedNodeIsSolution)
				info.Text = GettextCatalog.GetString ("Run Solution");
			else if (ShellCommandState.SelectedNodeIsProject)
				info.Text = GettextCatalog.GetString ("Run Project");
		}
	}

	internal class RunEntryWithHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ProjectCommands.RunEntryWithList, dataItem);
		}
	}

	internal class CleanHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.Clean);
		}

		// Oleada 3 (slice 6 + 9): enabled when the loaded solution has projects
		// and no build/run operation is in progress (legacy:
		// CurrentSelectedBuildTarget != null && .CanBuild — the selected target
		// is bridged by the solution's project set and CanBuild is not modeled,
		// both documented coarsenings; the busy guard !IsOperationRunning is an
		// added hardening: the legacy Clean does not check
		// CurrentBuildOperation.IsCompleted, unlike Build/Rebuild). Slice 9
		// adds the legacy dynamic "C_lean {0}" label (the underscore escape is
		// not portable — Avalonia renders the header literally).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace && ShellCommandState.ActiveProjectCount > 0 && !ShellCommandState.IsOperationRunning;
			var name = ShellCommandState.SelectedBuildTargetName;
			if (name is not null)
				info.Text = GettextCatalog.GetString ("C_lean {0}", name);
		}
	}

	internal class CleanSolutionHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.CleanSolution);
		}

		// Oleada 3 (slice 4): enabled when a solution is loaded (legacy:
		// CurrentSelectedSolution != null — exact bridge).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.HasWorkspace;
		}
	}

	public class StopHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.Stop);
		}

		// Oleada 3 (slice 5): enabled while a build or run operation is in
		// progress (legacy: !CurrentBuildOperation.IsCompleted ||
		// !CurrentRunOperation.IsCompleted — the shell maps both onto the
		// single shared process, so IsOperationRunning is the exact bridge).
		protected override void Update (CommandInfo info)
		{
			info.Enabled = ShellCommandState.IsOperationRunning;
		}
	}

	internal class CustomCommandListHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ProjectCommands.CustomCommandList, dataItem);
		}

		// Ronda 6 (BUG 3): port del legacy Update(CommandArrayInfo) — una entrada
		// por CustomCommand de Type == Custom del proyecto seleccionado (el
		// legacy lee IConfigurationTarget.DefaultConfiguration.CustomCommands del
		// CurrentSelectedBuildTarget). La shell no tiene el modelo GTK, así que
		// los lee de su propio modelo de proyecto a través del puente
		// ShellCommandState (ResolveSelectedProjectPath/ResolveActiveProject ->
		// ConfigurationService). El dataItem es la llave "customcmd:<Name>"
		// (string, porque el puente hace dataItem as string); el MainWindow la
		// resuelve de vuelta al CustomCommandEntry por nombre para ejecutarla.
		protected override void Update (CommandArrayInfo info)
		{
			foreach (var cmd in ShellCommandState.SelectedProjectCustomCommands)
				info.Add (cmd.Name, "customcmd:" + cmd.Name);
		}
	}

	internal class ExportSolutionHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.ExportSolution);
		}
	}

	internal class SelectActiveConfigurationHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ProjectCommands.SelectActiveConfiguration, dataItem);
		}
	}

	internal class SelectActiveRuntimeHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			ShellCommandHandler.Run (ProjectCommands.SelectActiveRuntime, dataItem);
		}
	}

	class ApplyPolicyHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ProjectCommands.ApplyPolicy");
		}
	}

	class ExportPolicyHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ProjectCommands.ExportPolicy");
		}
	}

	internal class RunCodeAnalysisSolutionHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ProjectCommands.RunCodeAnalysisSolution");
		}
	}

	internal class RunCodeAnalysisProjectHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run ("MonoDevelop.Ide.Commands.ProjectCommands.RunCodeAnalysisProject");
		}
	}

	internal class ToggleFileNestingHandler : CommandHandler
	{
		protected override void Run ()
		{
			ShellCommandHandler.Run (ProjectCommands.ToggleFileNesting);
		}
	}
#endif
}
