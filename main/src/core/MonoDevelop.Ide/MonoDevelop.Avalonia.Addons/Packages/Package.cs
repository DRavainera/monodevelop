using System;

namespace MonoDevelop.AvaloniaAddons.Packages
{
	/// <summary>Visual Studio style package: a unit of functionality with load/unload.</summary>
	public interface IPackage
	{
		void Initialize (IAddonContext context);
		void Load ();
		void Unload ();
	}

	public abstract class Package : IPackage
	{
		protected IAddonContext Context { get; private set; }
		public virtual void Initialize (IAddonContext context) => Context = context;
		public virtual void Load () { }
		public virtual void Unload () { }
	}

	/// <summary>Load the package automatically at startup (VS: ProvideAutoLoad).</summary>
	[AttributeUsage (AttributeTargets.Class)]
	public sealed class ProvideAutoLoadAttribute : Attribute
	{
		public ProvideAutoLoadAttribute (string trigger) => Trigger = trigger;
		public string Trigger { get; }
	}

	/// <summary>
	/// Contribute a Preferences options page (VS: ProvideOptionPage). The category id is
	/// the parent node id in the Preferences tree.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class)]
	public sealed class ProvideOptionPageAttribute : Attribute
	{
		public ProvideOptionPageAttribute (string categoryId, string id, string label, string pageTypeName)
		{
			CategoryId = categoryId; Id = id; Label = label; PageTypeName = pageTypeName;
		}
		public string CategoryId { get; }
		public string Id { get; }
		public string Label { get; }
		public string PageTypeName { get; }
	}
}
