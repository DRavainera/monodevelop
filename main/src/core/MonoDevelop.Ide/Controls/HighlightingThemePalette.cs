//
// HighlightingThemePalette.cs
//
// Built-in color themes for the dynamic syntax highlighting (VSCode-grade
// Dark+ / Light+ palettes).
//
// The legacy GTK world read editor color schemes from .vssettings/.tmTheme
// files. The shell ships two TextMate-style palettes as plain data and turns
// them into the same EditorTheme structure the legacy scope resolver works
// with, so scope-to-color resolution is the shared legacy implementation
// (EditorTheme + StackMatchExpression), not a re-implementation.
//
using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Controls
{
	/// <summary>
	/// The two built-in color themes (one per shell theme variant). Each entry
	/// is (name, scopes, settings); the first entry carries the base
	/// foreground/background the resolver reads from settings[0].
	/// </summary>
	static class HighlightingThemePalette
	{
		/// <summary>Dark theme name (shell default for the dark variant).</summary>
		public const string DarkName = "Dark+";
		/// <summary>Light theme name (shell default for the light variant).</summary>
		public const string LightName = "Light+";

		/// <summary>
		/// Builds an EditorTheme from palette entries using the legacy
		/// ThemeSetting model (scopes are parsed into StackMatchExpression by
		/// the constructor, so matching is identical to the GTK world).
		/// </summary>
		public static EditorTheme Build (string name, List<(string Scope, Dictionary<string, string> Settings)> entries)
		{
			var settings = new List<ThemeSetting> ();
			foreach (var (scope, kv) in entries) {
				var scopes = scope == "" ? new List<string> () : new List<string> { scope };
				settings.Add (new ThemeSetting (name, scopes, kv));
			}
			return new EditorTheme (name, settings);
		}

		static Dictionary<string, string> V (params (string, string)[] kv)
		{
			var d = new Dictionary<string, string> ();
			foreach (var (k, v) in kv)
				d [k] = v;
			return d;
		}

		static Dictionary<string, string> FG (string color) => V (("foreground", color));
		static Dictionary<string, string> FG (string color, string fontStyle) => V (("foreground", color), ("fontStyle", fontStyle));
		static Dictionary<string, string> BG (string color) => V (("background", color));

		/// <summary>VSCode "Dark+" palette (keywords #C586C0, strings #CE9178, comments #6A9955).</summary>
		public static EditorTheme BuildDark ()
		{
			return Build (DarkName, new List<(string, Dictionary<string, string>)> {
				("", V (("foreground", "#D4D4D4"), ("background", "#1E1E1E"))),
				("comment", FG ("#6A9955")),
				("comment.line.documentation", FG ("#6A9955")),
				("string", FG ("#CE9178")),
				("constant.numeric", FG ("#B5CEA8")),
				("constant.language", FG ("#569CD6")),
				("keyword", FG ("#C586C0")),
				("keyword.control", FG ("#C586C0")),
				("keyword.other", FG ("#C586C0")),
				("storage", FG ("#569CD6")),
				("storage.type", FG ("#569CD6")),
				("storage.modifier", FG ("#569CD6")),
				("entity.name.type", FG ("#4EC9B0")),
				("entity.name.namespace", FG ("#4EC9B0")),
				("entity.name.class", FG ("#4EC9B0")),
				("entity.name.function", FG ("#DCDCAA")),
				("entity.name.variable", FG ("#9CDCFE")),
				("entity.other.attribute-name", FG ("#9CDCFE")),
				("support.type", FG ("#4EC9B0")),
				("support.class", FG ("#4EC9B0")),
				("support.constant", FG ("#4FC1FF")),
				("support.function", FG ("#DCDCAA")),
				("variable", FG ("#9CDCFE")),
				("variable.parameter", FG ("#9CDCFE")),
				("support.variable", FG ("#9CDCFE")),
				("constant.character", FG ("#CE9178")),
				("constant.other", FG ("#569CD6")),
				("preprocessor", FG ("#C586C0")),
				// C# full grammar (source.cs) emits entity.name.tag.* for generic
				// type arguments (List<int> -> <int>) and for XML-doc tags; the
				// base "source.cs" scope has no color, so without this the
				// identifiers inside the angle brackets fell through to the base
				// foreground (uncolored). VSCode renders both in the type teal.
				("entity.name.tag", FG ("#4EC9B0")),
				("entity.name.field", FG ("#9CDCFE")),
				("invalid", FG ("#F44747")),
				("markup.heading", FG ("#569CD6", "bold")),
				("markup.italic", FG ("#D4D4D4", "italic")),
				("markup.bold", FG ("#D4D4D4", "bold")),
				("markup.inline.raw", FG ("#CE9178")),
				("markup.underline.link", FG ("#3794FF", "underline")),
				("markup.underline", FG ("#3794FF", "underline")),
				("markup.emoji", FG ("#E16D66")),
				("markup.strong", FG ("#D4D4D4", "bold")),
				("markup.italic.markdown", FG ("#D4D4D4", "italic")),
				("markup.deleted", FG ("#E06C75")),
				("markup.inserted", FG ("#4EC9B0")),
				("markup.strikethrough", FG ("#8A8A8A")),
				("meta.attribute", FG ("#4EC9B0")),
				("meta.preprocessor", FG ("#C586C0")),
			});
		}

		/// <summary>VSCode "Light+" palette (keywords #AF00DB, strings #A31515, comments #008000).</summary>
		public static EditorTheme BuildLight ()
		{
			return Build (LightName, new List<(string, Dictionary<string, string>)> {
				("", V (("foreground", "#000000"), ("background", "#FFFFFF"))),
				("comment", FG ("#008000")),
				("comment.line.documentation", FG ("#008000")),
				("string", FG ("#A31515")),
				("constant.numeric", FG ("#098658")),
				("constant.language", FG ("#0000FF")),
				("keyword", FG ("#AF00DB")),
				("keyword.control", FG ("#AF00DB")),
				("keyword.other", FG ("#AF00DB")),
				("storage", FG ("#0000FF")),
				("storage.type", FG ("#0000FF")),
				("storage.modifier", FG ("#0000FF")),
				("entity.name.type", FG ("#267F99")),
				("entity.name.namespace", FG ("#267F99")),
				("entity.name.class", FG ("#267F99")),
				("entity.name.function", FG ("#795E26")),
				("entity.name.variable", FG ("#001080")),
				("entity.other.attribute-name", FG ("#800000")),
				("support.type", FG ("#267F99")),
				("support.class", FG ("#267F99")),
				("support.constant", FG ("#0000FF")),
				("support.function", FG ("#795E26")),
				("variable", FG ("#001080")),
				("variable.parameter", FG ("#001080")),
				("support.variable", FG ("#001080")),
				("constant.character", FG ("#A31515")),
				("constant.other", FG ("#0000FF")),
				("preprocessor", FG ("#AF00DB")),
				// Same coverage as Dark+: the C# full grammar (source.cs) emits
				// entity.name.tag.* for generic type arguments and XML-doc tags;
				// without this they fall through to the base foreground.
				("entity.name.tag", FG ("#267F99")),
				("entity.name.field", FG ("#001080")),
				("invalid", FG ("#CD3131")),
				("markup.heading", FG ("#0000FF", "bold")),
				("markup.italic", FG ("#000000", "italic")),
				("markup.bold", FG ("#000000", "bold")),
				("markup.inline.raw", FG ("#A31515")),
				("markup.underline.link", FG ("#0000FF", "underline")),
				("markup.underline", FG ("#0000FF", "underline")),
				("markup.emoji", FG ("#E16D66")),
				("markup.strong", FG ("#000000", "bold")),
				("markup.italic.markdown", FG ("#000000", "italic")),
				("markup.deleted", FG ("#CD3131")),
				("markup.inserted", FG ("#267F99")),
				("markup.strikethrough", FG ("#808080")),
				("meta.attribute", FG ("#267F99")),
				("meta.preprocessor", FG ("#AF00DB")),
			});
		}
	}
}
