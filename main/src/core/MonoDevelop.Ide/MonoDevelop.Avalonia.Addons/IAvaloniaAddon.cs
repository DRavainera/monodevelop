using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using MonoDevelop.AvaloniaAddons.Extensions;
using MonoDevelop.AvaloniaAddons.Manifests;
using MonoDevelop.AvaloniaAddons.Packages;

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

	/// <summary>Convenience base: <see cref="IAvaloniaAddon"/> receives the context.</summary>
	public abstract class AvaloniaAddon : IAvaloniaAddon
	{
		protected IAddonContext Context { get; private set; }

		public string Id { get; protected set; }

		public virtual void Initialize (IAddonContext context) => Context = context;

		public virtual void Load () { }

		public virtual void Unload () { }
	}

	/// <summary>Provides the context to an add-in during its lifecycle.</summary>
	public interface IAddonContext
	{
		/// <summary>Root folder where add-ins were discovered.</summary>
		string RootDirectory { get; }

		/// <summary>Extension-node registry (e.g. "/MonoDevelop/Ide/GlobalOptionsDialog").</summary>
		IAddonExtensionRegistry Extensions { get; }

		/// <summary>Shared composition host for MEF-like exports/imports.</summary>
		ICompositionHost Composition { get; }

		/// <summary>Service to register and manage commands.</summary>
		ICommandService CommandService { get; }

		/// <summary>Service to register and manage pads.</summary>
		IPadRegistry PadRegistry { get; }

		void Log (string message);
	}

	/// <summary>
	/// Platform hooks contributed by the host's platform add-in (MacPlatform /
	/// WindowsPlatform / LinuxPlatform). The shell resolves the first loaded
	/// implementor from the add-in host and uses it for the desktop operations it
	/// cannot do portably (open a URL, open a folder, default monospace face, MIME
	/// lookup for a file). It lives in this assembly — shared between the shell
	/// (default context) and the add-in load contexts — so the hook needs no
	/// reflection and no MonoDevelop.Ide type ever crosses the contexts.
	/// </summary>
	public interface IPlatformHooks
	{
		string Name { get; }

		string DefaultMonospaceFont { get; }

		void ShowUrl (string url);

		void OpenFolder (string path);

		string GetMimeTypeForUri (string uri);

		// Mime/icon lookups (B.4): the defaults resolve through the shared mime
		// catalog, so platform add-ins compiled before these members existed (stale
		// Mac/Windows builds) answer without a class-side implementation. A platform
		// add-in may still override any of them for OS-specific resolution.
		string GetMimeTypeDescription (string mimeType) => MimeCatalog.GetMimeTypeDescription (mimeType);

		string GetIconIdForFile (string fileName) => MimeCatalog.GetIconIdForFile (fileName);

		string GetIconIdForType (string mimeType) => MimeCatalog.GetIconIdForType (mimeType);
	}

	public interface ICommandService
	{
		void RegisterCommand (Command cmd);
		void RegisterCommandBar (ICommandBar commandBar);
	}

	public interface IPadRegistry
	{
		void RegisterPad (PadDefinition def);
	}

	public interface ICommandBar { }

	public record Command (string Id, string Label, string? Icon = null);

	public record PadDefinition (string Id, string Title, string Icon = "md-prefs-generic");
}
