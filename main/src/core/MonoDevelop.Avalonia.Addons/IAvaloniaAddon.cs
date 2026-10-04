using System;

namespace MonoDevelop.AvaloniaAddons
{
	/// <summary>
	/// Lifecycle of a shell add-in, modelled on the Visual Studio package model:
	/// discovered from a manifest, composed (MEF-like exports) then activated.
	/// </summary>
	public interface IAvaloniaAddon
	{
		string Id { get; }

		/// <summary>Called once the manifest is validated and the composition is built.</summary>
		void Initialize (IAddonContext context);

		/// <summary>Called after every add-in was initialized.</summary>
		void Load ();

		/// <summary>Called on shutdown.</summary>
		void Unload ();
	}

	/// <summary>Convenience base: <see cref="Initialize"/> receives the context.</summary>
	public abstract class AvaloniaAddon : IAvaloniaAddon
	{
		protected IAddonContext Context { get; private set; }

		public abstract string Id { get; }

		public virtual void Initialize (IAddonContext context) => Context = context;

		public virtual void Load () { }

		public virtual void Unload () { }
	}

	public interface IAddonContext
	{
		/// <summary>Root folder where add-ins were discovered.</summary>
		string RootDirectory { get; }

		/// <summary>Extension-node registry (e.g. "/MonoDevelop/Ide/GlobalOptionsDialog").</summary>
		IAddonExtensionRegistry Extensions { get; }

		/// <summary>Shared composition host for MEF-like exports/imports.</summary>
		ICompositionHost Composition { get; }

		void Log (string message);
	}
}
