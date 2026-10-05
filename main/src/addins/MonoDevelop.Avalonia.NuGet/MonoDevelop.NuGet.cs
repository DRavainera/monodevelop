using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.AvaloniaAddons;
using MonoDevelop.AvaloniaAddons.Packages;

namespace MonoDevelop.Avalonia.NuGet
{
	public class NuGetAddin : AvaloniaAddon
	{
		public override string Id => "MonoDevelop.NuGet";

		public override void Initialize(IAddonContext context)
		{
			// Logic to wire up NuGet settings would go here
		}
	}
}