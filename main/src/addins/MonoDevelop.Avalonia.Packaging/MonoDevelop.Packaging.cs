using System;

namespace MonoDevelop.Avalonia.Packaging
{
	public class PackagingAddin : AvaloniaAddon
	{
		public override string Id => "MonoDevelop.Packaging";

		public override void Initialize(IAddonContext context)
		{
			// NuGet packaging initialization logic would go here
		}
	}
}
