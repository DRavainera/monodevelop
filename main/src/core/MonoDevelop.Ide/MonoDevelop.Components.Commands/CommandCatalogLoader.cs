#if AVALONIA_SHELL
//
// CommandCatalogLoader.cs
//
// Non-GTK loader for the Avalonia shell (AVALONIA_SHELL): it reads the legacy
// command catalogs (the *.addin.xml files, i.e. every
// <Extension path="/MonoDevelop/Ide/Commands"> section of MonoDevelop.Ide and
// the add-ins, copied to the output as addinxml/) and builds the command
// registry from the NON-GTK data model (Command/ActionCommand), so the shell
// dispatches through the same CommandManager pipeline the GTK IDE uses.
//
// The codons (CommandCodon and friends) are deliberately NOT reused: they
// derive from Mono.Addins.ExtensionNode and only exist inside the Mono.Addins
// add-in engine, which would load the GTK MonoDevelop.Ide add-in. This loader
// mirrors CommandCodon.CreateInstance's mapping (CommandCodon.cs) onto plain
// ActionCommand instances.
//

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.Components.Commands
{
	public static class CommandCatalogLoader
	{
		/// <summary>First file that failed to load, if any (null when clean).</summary>
		public static string LastError { get; private set; }

		/// <summary>
		/// Loads every *.addin.xml under baseDir/addinxml and registers the
		/// commands found in their /MonoDevelop/Ide/Commands extensions into
		/// manager. defaultHandler types are NOT resolved (the GTK handler
		/// classes live in the GTK assembly); those commands dispatch through
		/// manager.CommandRouter. Returns the number of commands registered.
		/// </summary>
		public static int Load (CommandManager manager, string baseDir)
		{
			LastError = null;
			var catalogDir = Path.Combine (baseDir, "addinxml");
			if (!Directory.Exists (catalogDir)) {
				LastError = "Command catalog directory not found: " + catalogDir;
				return 0;
			}

			int count = 0;
			foreach (var file in Directory.EnumerateFiles (catalogDir, "*.addin.xml", SearchOption.AllDirectories)) {
				try {
					count += LoadFile (manager, file);
				} catch (Exception ex) {
					LastError = $"Command catalog '{file}' failed: {ex.Message}";
				}
			}
			return count;
		}

		static int LoadFile (CommandManager manager, string file)
		{
			var doc = XDocument.Load (file);
			int count = 0;
			foreach (var ext in doc.Descendants ("Extension")) {
				// The command extension point has sub-paths per area
				// (/MonoDevelop/Ide/Commands, /.../Commands/Project, /.../Commands/Hidden,
				// /.../Commands/TextEditor, ...) — all of them define commands.
				var path = (string) ext.Attribute ("path");
				if (path != "/MonoDevelop/Ide/Commands" && !path.StartsWith ("/MonoDevelop/Ide/Commands/", StringComparison.Ordinal))
					continue;
				count += Walk (manager, ext, null);
			}
			return count;
		}

		static int Walk (CommandManager manager, XElement el, string category)
		{
			int count = 0;
			foreach (var child in el.Elements ()) {
				switch (child.Name.LocalName) {
					case "Category":
						// CommandCategoryCodon.Name comes from the _name attribute.
						var catName = (string) child.Attribute ("_name");
						if (string.IsNullOrEmpty (catName))
							catName = (string) child.Attribute ("id");
						count += Walk (manager, child, catName);
						break;
					case "Command":
						count += ProcessCommand (manager, child, category);
						break;
					case "Condition":
						count += WalkCondition (manager, child, category);
						break;
				}
			}
			return count;
		}

		/// <summary>
		/// Mirrors the add-in engine's condition evaluation for catalog loading:
		/// a Platform condition selects the OS-specific branch; every other
		/// condition kind (ItemType, ProjectTypeId, FlavorType, ...) constrains
		/// menu placement, not command existence, so its commands register
		/// unconditionally.
		/// </summary>
		static int WalkCondition (CommandManager manager, XElement el, string category)
		{
			var id = (string) el.Attribute ("id");
			if (id == "Platform" && !PlatformMatches ((string) el.Attribute ("value")))
				return 0;
			return Walk (manager, el, category);
		}

		static bool PlatformMatches (string value)
		{
			if (string.IsNullOrEmpty (value))
				return true;
			bool negated = value.StartsWith ("!", StringComparison.Ordinal);
			if (negated)
				value = value.Substring (1);
			bool match = value switch {
				"linux" => OperatingSystem.IsLinux (),
				"windows" => OperatingSystem.IsWindows (),
				"mac" => OperatingSystem.IsMacOS (),
				_ => false
			};
			return negated ? !match : match;
		}

		// Maps one <Command .../> element to an ActionCommand, mirroring
		// CommandCodon.CreateInstance (type flags, shortcut selection, @-prefixed
		// ids). defaultHandler is intentionally left unresolved.
		static int ProcessCommand (CommandManager manager, XElement el, string category)
		{
			var id = (string) el.Attribute ("id");
			if (string.IsNullOrEmpty (id))
				return 0;
			if (id.StartsWith ("@"))
				id = id.Substring (1);

			var label = (string) el.Attribute ("_label");
			if (string.IsNullOrEmpty (label))
				label = id;

			// CommandCodon: Platform.IsMac ? macShortcut : shortcut, with
			// winShortcut overriding on Windows. Space-separated chords: the
			// first is the binding, the rest are alternates.
			var winShortcut = (string) el.Attribute ("winShortcut");
			var keyBinding = OperatingSystem.IsWindows () && !string.IsNullOrEmpty (winShortcut)
				? winShortcut
				: (string) el.Attribute ("shortcut");
			var chords = (keyBinding ?? "").Split (' ', StringSplitOptions.RemoveEmptyEntries);

			// type flags: normal|check|radio|custom|array
			var type = (string) el.Attribute ("type") ?? "normal";
			var actionType = ActionType.Normal;
			var isArray = false;
			var custom = false;
			foreach (var p in type.Split ('|')) {
				switch (p) {
					case "check":
						actionType = ActionType.Check;
						break;
					case "radio":
						actionType = ActionType.Radio;
						break;
					case "custom":
						custom = true;
						break;
					case "array":
						isArray = true;
						break;
				}
			}
			// Custom (widget-hosting) commands have no non-GTK equivalent:
			// register them as normal commands; the shell renders them with its
			// own menu entries.
			if (custom)
				actionType = ActionType.Normal;

			var cmd = new ActionCommand (id, label, (string) el.Attribute ("icon"), chords.Length > 0 ? chords[0] : null, actionType);
			if (chords.Length > 1)
				cmd.AlternateAccelKeys = chords.Skip (1).ToArray ();
			cmd.CommandArray = isArray;
			cmd.Description = (string) el.Attribute ("_description") ?? (string) el.Attribute ("description");
			cmd.Category = category ?? string.Empty;
			cmd.DisabledVisible = (bool?) el.Attribute ("disabledVisible") ?? true;
			var displayName = (string) el.Attribute ("_displayName");
			if (!string.IsNullOrWhiteSpace (displayName))
				cmd.DisplayName = displayName;
			manager.RegisterCommand (cmd);
			return 1;
		}
	}
}
#endif
