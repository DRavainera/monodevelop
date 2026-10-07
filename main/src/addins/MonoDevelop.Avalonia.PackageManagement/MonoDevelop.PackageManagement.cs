using System;

namespace MonoDevelop.Avalonia.PackageManagement
{
	public class PackageManagementAddin : AvaloniaAddon
	{
		public override string Id => "MonoDevelop.PackageManagement";

		public override void Initialize(IAddonContext context)
		{
			// NuGet package management initialization logic would go here
		}
	}
}
