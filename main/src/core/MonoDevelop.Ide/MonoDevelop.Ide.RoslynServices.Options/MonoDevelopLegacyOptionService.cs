// MonoDevelopLegacyOptionService.cs
//
// Author:
//       MonoDevelop contributor
//
// Copyright (c) 2026 MonoDevelop
//
// Supplies the Roslyn legacy workspace option service used while constructing a
// Workspace. This modernized boot path composes the workspace through
// Microsoft.CodeAnalysis.Workspaces (net6/net10) whose internal
// LegacyGlobalOptionService/WorkspaceService export is not discoverable with the
// VisualStudio.Composition discovery used here, so we provide an equivalent
// export ourselves.
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	sealed class MonoDevelopLegacyOptionSet : OptionSet
	{
		public static readonly MonoDevelopLegacyOptionSet Instance = new MonoDevelopLegacyOptionSet ();

		public override object GetOption (OptionKey optionKey)
		{
			var option = optionKey.Option;
			if (option != null)
				return option.DefaultValue;
			return null;
		}

		public override OptionSet WithChangedOption (OptionKey optionAndLanguage, object value)
		{
			return this;
		}

		public override object GetInternalOptionValue (OptionKey optionKey)
		{
			return GetOption (optionKey);
		}
	}

	[ExportWorkspaceServiceFactory (typeof (ILegacyWorkspaceOptionService), ServiceLayer.Host), Shared]
	sealed class MonoDevelopLegacyOptionServiceFactory : IWorkspaceServiceFactory, ILegacyWorkspaceOptionService
	{
		public OptionSet LegacyGlobalOptions => MonoDevelopLegacyOptionSet.Instance;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return this;
		}
	}
}