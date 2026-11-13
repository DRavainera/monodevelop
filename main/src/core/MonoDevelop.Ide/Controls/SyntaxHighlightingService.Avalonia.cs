//
// SyntaxHighlightingService.Avalonia.cs
//
// Port of the legacy MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlightingService
// to the Avalonia shell (the legacy one is entangled with Mono.Addins, NRefactory
// and the MonoDevelop.Components theme model, none of which exist here).
//
// Lives in the MonoDevelop.Ide.Editor.Highlighting namespace ON PURPOSE: the
// legacy SyntaxHighlightingDefinition resolves its scope-include bundles
// against an unqualified SyntaxHighlightingService.AllBundles, so the ported
// registry must sit in that exact namespace to be picked up.
//
// Contract the editor needs:
//   - AllBundles: the built-in language bundle, populated from the embedded
//     syntax definitions (the full VSCode C# grammar + the legacy
//     .sublime-syntax rules for F#/Markdown/YAML + the VSCode TextMate
//     grammars for C/C++/XML/JSON) plus the two built-in color themes
//     (Dark+/Light+).
//   - GetSyntaxHighlightingDefinition(path): extension-based language
//     resolution (same "longest extension wins" rule as the legacy).
//   - GetEditorTheme(name): theme lookup for the color palettes.
//   - AddHighlighting/AddEditorTheme: extension points for add-ins (VSIX).
//
// Definitions are parsed with the SAME legacy parsers (Sublime3Format for
// .sublime-syntax YAML, TextMateFormat for the .tmLanguage grammars), so rule
// authoring is identical to the GTK world.
//
using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide.Controls;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	/// <summary>
	/// Shell-side registry of syntax definitions and color themes (port of the
	/// legacy SyntaxHighlightingService for the Avalonia build, without the
	/// Mono.Addins/NRefactory/Components coupling).
	/// </summary>
	public static class SyntaxHighlightingService
	{
		static readonly List<LanguageBundle> languageBundles = new ();
		static readonly LanguageBundle builtInBundle = new ("default", null);
		static bool initialized;
		static readonly object initLock = new ();

		/// <summary>All registered bundles (built-in + user), as the legacy
		/// SyntaxHighlightingDefinition include-by-scope resolution expects.
		/// internal (not public) because LanguageBundle is an internal type.</summary>
		internal static IReadOnlyList<LanguageBundle> AllBundles {
			get {
				EnsureInitialized ();
				return languageBundles;
			}
		}

		/// <summary>Registered color theme names.</summary>
		public static string[] Styles {
			get {
				EnsureInitialized ();
				var names = new List<string> ();
				foreach (var bundle in languageBundles) {
					foreach (var t in bundle.EditorThemes) {
						if (!names.Contains (t.Name))
							names.Add (t.Name);
					}
				}
				return names.ToArray ();
			}
		}

		/// <summary>
		/// Resolves the color theme by name (Dark+ / Light+ built-ins, or any
		/// registered theme). Unknown names fall back to the dark palette so a
		/// missing theme never leaves the editor uncolored.
		/// </summary>
		public static EditorTheme GetEditorTheme (string name)
		{
			EnsureInitialized ();
			foreach (var bundle in languageBundles) {
				foreach (var t in bundle.EditorThemes) {
					if (t.Name == name) {
						var theme = t.GetEditorTheme ();
						if (theme != null)
							return theme;
					}
				}
			}
			// Fallback: the built-in dark palette.
			return HighlightingThemePalette.BuildDark ();
		}

		/// <summary>Dark palette name (the shell default for dark theme variants).</summary>
		public const string DarkName = "Dark+";
		/// <summary>Light palette name.</summary>
		public const string LightName = "Light+";

		/// <summary>
		/// Resolves the syntax definition for a file path (legacy rule: the
		/// longest matching registered extension wins; exact file names like
		/// "Makefile" win outright). Null when no registered language matches.
		/// </summary>
		public static SyntaxHighlightingDefinition GetSyntaxHighlightingDefinition (FilePath filePath)
		{
			EnsureInitialized ();
			// implicit operator string (FilePath.fileName)
			string fileNameStr = filePath;
			if (string.IsNullOrEmpty (fileNameStr))
				return null;
			var name = filePath.FileName;

			SyntaxHighlightingDefinition bestMatch = null;
			string bestType = null;
			int bestPosition = 0;

			foreach (var bundle in languageBundles) {
				foreach (var h in bundle.Highlightings) {
					for (int i = 0; i < h.FileTypes.Count; i++) {
						var fileType = h.FileTypes [i];
						if (string.IsNullOrEmpty (fileType))
							continue;
						if (!fileNameStr.EndsWith (fileType, StringComparison.OrdinalIgnoreCase))
							continue;
						// Full filename match (e.g. "Makefile", "ChangeLog"): done.
						if (name.Length == fileType.Length)
							return h.GetSyntaxHighlightingDefinition ();
						// "y" must not match "ab.xy": require the dot before.
						if (!fileType.StartsWith (".")) {
							int dot = fileNameStr.Length - fileType.Length - 1;
							if (dot < 0 || fileNameStr [dot] != '.')
								continue;
						}
						if (bestType == null || bestType.Length < fileType.Length ||
							(bestType.Length == fileType.Length && bestPosition > i)) {
							bestType = fileType;
							bestPosition = i;
							bestMatch = h.GetSyntaxHighlightingDefinition ();
						}
					}
				}
			}
			return bestMatch;
		}

		/// <summary>
		/// Registers a syntax definition provider (add-in extension point so
		/// VSIX add-ins can contribute languages to the whole shell).
		/// internal (not public) because ISyntaxHighlightingDefinitionProvider
		/// is an internal type.
		/// </summary>
		internal static void AddHighlighting (ISyntaxHighlightingDefinitionProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException (nameof (provider));
			EnsureInitialized ();
			builtInBundle.Add (provider);
		}

		/// <summary>
		/// Registers a color theme provider (add-in extension point).
		/// internal (not public) because IEditorThemeProvider is an internal type.
		/// </summary>
		internal static void AddEditorTheme (IEditorThemeProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException (nameof (provider));
			EnsureInitialized ();
			builtInBundle.Add (provider);
		}

		static void EnsureInitialized ()
		{
			if (initialized)
				return;
			lock (initLock) {
				if (initialized)
					return;
				LoadBuiltIns ();
				initialized = true;
			}
		}

		static void LoadBuiltIns ()
		{
			// --- Color themes: the two built-in VSCode-grade palettes. ---
			builtInBundle.Add (new EmbeddedThemeProvider (DarkName, HighlightingThemePalette.BuildDark));
			builtInBundle.Add (new EmbeddedThemeProvider (LightName, HighlightingThemePalette.BuildLight));

			// --- Syntax definitions: the legacy rules that ship in the repo.
			//     The loader is picked by the embedded content type: .sublime-syntax
			//     (YAML) -> Sublime3Format, .tmLanguage.json (JSON) ->
			//     TextMateFormat.ReadHighlightingFromJson, .tmLanguage (PLIST/XML)
			//     -> TextMateFormat.ReadHighlighting. ---
			Register ("csharp.tmLanguage.plist", "C#", new [] { ".cs", ".csx", ".cake" });
			Register ("fsharp.sublime-syntax", "F#", new [] { ".fs", ".fsi", ".fsx", ".fsscript" });
			Register ("markdown.sublime-syntax", "Markdown", new [] { ".md", ".markdown", ".mdown" });
			Register ("yaml.sublime-syntax", "YAML", new [] { ".yaml", ".yml" });
			Register ("c.json", "C", new [] { ".c", ".h.in" });
			Register ("cpp.json", "C++", new [] { ".cc", ".cpp", ".cp", ".cxx", ".cu", ".cuh", ".h", ".hh", ".hpp", ".hxx", ".inl", ".ino", ".ipp", ".tcc", ".tpp" });
			Register ("xml.json", "XML", new [] { ".xml", ".xsl", ".xslt", ".csproj", ".fsproj", ".vbproj", ".vcxproj", ".sln", ".props", ".targets", ".config", ".xaml", ".nuspec", ".resx", ".manifest" });
			Register ("json.plist", "JSON", new [] { ".json", ".sublime-settings", ".sublime-menu" });

			languageBundles.Add (builtInBundle);
		}

		/// <summary>
		/// Registers one embedded rule file as a definition provider. The
		/// loader is picked by the file extension (the legacy parsers).
		/// </summary>
		static void Register (string resource, string name, string [] fileTypes)
		{
			builtInBundle.Add (new EmbeddedDefinitionProvider (name, resource, fileTypes));
		}

		/// <summary>Manifest-name prefix of the embedded rule files
		/// (matches the EmbeddedResource LogicalName in the shell csproj).</summary>
		const string ResourcePrefix = "MonoDevelop.AvaloniaShell.Highlighting.Resources.";

		/// <summary>
		/// Definition provider backed by an embedded resource (loaded through
		/// the legacy ResourceStreamProvider), parsed with the legacy
		/// Sublime3 or TextMate parsers.
		/// </summary>
		sealed class EmbeddedDefinitionProvider : AbstractSyntaxHighlightingDefinitionProvider
		{
			readonly string resource;

			public EmbeddedDefinitionProvider (string name, string resource, string [] fileTypes)
				: base (name, scopeFromFile (resource), fileTypes, () => new ResourceStreamProvider (typeof (SyntaxHighlightingService).Assembly, ResourcePrefix + resource))
			{
				this.resource = resource;
			}

			static string scopeFromFile (string resource)
			{
				// The scopeName the legacy service registers each language
				// under (it is also what scope: includes match against).
				switch (System.IO.Path.GetFileName (resource)) {
				case "csharp.tmLanguage.plist": return "source.cs";
				case "fsharp.sublime-syntax": return "source.fs";
				case "markdown.sublime-syntax": return "text.html.markdown";
				case "yaml.sublime-syntax": return "source.yaml";
				case "c.json": return "source.c";
				case "cpp.json": return "source.cpp";
				case "xml.json": return "text.xml";
				case "json.plist": return "source.json";
				default: return "source." + System.IO.Path.GetFileNameWithoutExtension (resource);
				}
			}

			protected override SyntaxHighlightingDefinition LoadSyntaxHighlightingDefinition (Stream stream)
			{
				if (resource.EndsWith (".sublime-syntax", StringComparison.Ordinal)) {
					// Sublime3 YAML format.
					using var reader = new StreamReader (stream);
					return Sublime3Format.ReadHighlighting (reader);
				}
				if (resource.EndsWith (".plist", StringComparison.Ordinal)) {
					// TextMate PLIST (XML) — JSON.tmLanguage renamed: it is a
					// property list despite the .tmLanguage heritage.
					return TextMateFormat.ReadHighlighting (stream);
				}
				// TextMate JSON grammar (.tmLanguage.json).
				return TextMateFormat.ReadHighlightingFromJson (stream);
			}
		}

		/// <summary>
		/// Theme provider over a built-in EditorTheme (the two shell palettes).
		/// </summary>
		sealed class EmbeddedThemeProvider : IEditorThemeProvider
		{
			readonly Func<EditorTheme> build;
			EditorTheme theme;

			public EmbeddedThemeProvider (string name, Func<EditorTheme> build)
			{
				Name = name;
				this.build = build;
			}

			public string Name { get; }

			public EditorTheme GetEditorTheme ()
			{
				theme ??= build ();
				return theme;
			}
		}
	}
}
