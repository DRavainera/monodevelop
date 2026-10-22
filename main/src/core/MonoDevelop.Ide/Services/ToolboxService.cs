using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Services;

/// <summary>One item of the Toolbox pad. Port of the item model of the
/// legacy <c>MonoDevelop.DesignerSupport</c> toolbox (
/// <c>TemplateToolboxNode</c>: name = shortcut, description, code) restricted
/// to the content the shell has: code templates.</summary>
public sealed class ToolboxItem
{
	public ToolboxItem (string name, string shortcut, string category, string? summary, string[] code)
	{
		Name = name;
		Shortcut = shortcut;
		Category = category;
		Summary = summary;
		Code = code;
	}

	/// <summary>Legacy <c>TemplateToolboxNode.Name</c>: the template shortcut.</summary>
	public string Name { get; }

	public string Shortcut { get; }

	public string Category { get; }

	/// <summary>Legacy <c>TemplateToolboxNode.Description</c> (tooltip in the
	/// legacy toolbox widget).</summary>
	public string? Summary { get; }

	/// <summary>Template code as lines (the legacy <c>CodeTemplate.Code</c>
	/// split on newlines); expanded at the caret on activation.</summary>
	public string[] Code { get; }
}

/// <summary>Aggregates the toolbox items available in the shell: the
/// editor's built-in code templates (the same set the Snippet command
/// expands — <c>SkTextEditor</c> is the single source of truth) and the
/// user templates the legacy GUI stores in the same place and format
/// (<c>UserDataRoot/Snippets/*.xml</c>, <c>CodeTemplates</c> v3.0), with the
/// legacy merge rule (a user template replaces the built-in one with the
/// same shortcut). The legacy "Text Snippets" category is the label of its
/// <c>CodeTemplateToolboxProvider</c>.</summary>
public static class ToolboxService
{
	public const string SnippetsCategory = "Text Snippets";

	/// <summary>The directory the legacy GUI stores user code templates
	/// (<c>CodeTemplateService.TemplatePath</c>).</summary>
	public static string UserTemplatesDir () =>
		UserProfile.Current.UserDataRoot.Combine ("Snippets").ToString ();

	/// <summary>All toolbox items: built-in editor templates + user
	/// templates (the user directory by default; <paramref name="snippetsDir"/>
	/// overrides it — used by the QA flow with an isolated fixture). User
	/// templates win over the built-in ones with the same shortcut (legacy
	/// <c>CodeTemplateService.LoadTemplates</c> merge order).</summary>
	public static List<ToolboxItem> GetItems (string? snippetsDir = null)
	{
		var user = LoadUserTemplates (snippetsDir ?? UserTemplatesDir ());
		var userShortcuts = user.Select (t => t.Shortcut).ToHashSet (StringComparer.Ordinal);
		return Controls.SkTextEditor.EditorTemplates
			.Select (t => new ToolboxItem (t.Name, t.Name, SnippetsCategory, null, t.Code))
			.Where (b => !userShortcuts.Contains (b.Shortcut))
			.Concat (user)
			.ToList ();
	}

	/// <summary>Items grouped by category (sorted), items sorted by name —
	/// the ordering of the legacy toolbox category tree.</summary>
	public static Dictionary<string, List<ToolboxItem>> GetCategories (IEnumerable<ToolboxItem> items)
	{
		var cats = new Dictionary<string, List<ToolboxItem>> ();
		foreach (var item in items) {
			if (!cats.TryGetValue (item.Category, out var list))
				cats [item.Category] = list = new List<ToolboxItem> ();
			list.Add (item);
		}
		return cats
			.ToDictionary (kv => kv.Key, kv => kv.Value.OrderBy (t => t.Name, StringComparer.Ordinal).ToList (),
				StringComparer.Ordinal)
			.OrderBy (kv => kv.Key, StringComparer.Ordinal)
			.ToDictionary (kv => kv.Key, kv => kv.Value);
	}

	/// <summary>The legacy <c>filterTextChanged</c>: an item matches when its
	/// name (shortcut) contains the term, case-insensitive; an empty term
	/// matches everything.</summary>
	public static IEnumerable<ToolboxItem> Filter (IEnumerable<ToolboxItem> items, string term)
	{
		if (string.IsNullOrWhiteSpace (term))
			return items;
		return items.Where (t => t.Name.IndexOf (term, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	/// <summary>Parses the user template files of a directory (legacy format:
	/// <c>&lt;CodeTemplates version="3.0"&gt;&lt;CodeTemplate&gt;
	/// &lt;Header&gt;…&lt;/Header&gt;&lt;Variables/&gt;&lt;Code&gt;
	/// <![CDATA[…]]></Code&gt;&lt;/CodeTemplate&gt;…</c>). Unreadable or
	/// malformed files are skipped with a log line, like the legacy loader.</summary>
	public static List<ToolboxItem> LoadUserTemplates (string dir)
	{
		var items = new List<ToolboxItem> ();
		if (string.IsNullOrEmpty (dir) || !Directory.Exists (dir))
			return items;
		foreach (var file in Directory.GetFiles (dir, "*.xml").OrderBy (f => f, StringComparer.Ordinal)) {
			try {
				var doc = new XmlDocument ();
				doc.Load (file);
				var root = doc.DocumentElement;
				if (root is null || root.LocalName != "CodeTemplates")
					continue;
				foreach (XmlNode templateNode in root.ChildNodes) {
					if (templateNode is not XmlElement template || template.LocalName != "CodeTemplate")
						continue;
					var item = ReadTemplate (template);
					if (item is not null)
						items.Add (item);
				}
			} catch (Exception ex) {
				Console.WriteLine ("[toolbox] " + file + ": " + ex.Message);
			}
		}
		return items;
	}

	static ToolboxItem? ReadTemplate (XmlElement template)
	{
		var header = template["Header"];
		if (header is null)
			return null;
		// The legacy provider only lists Standard-context templates.
		if (header["Context"] is not null)
			return null;
		string? El (string name) => header[name]?.InnerText;
		var shortcut = El ("Shortcut");
		if (string.IsNullOrWhiteSpace (shortcut))
			return null;
		var group = El ("_Group");
		var code = template["Code"]?.InnerText ?? "";
		return new ToolboxItem (
			shortcut,
			shortcut,
			string.IsNullOrWhiteSpace (group) ? SnippetsCategory : group.Trim (),
			string.IsNullOrWhiteSpace (El ("_Description")) ? null : El ("_Description")!.Trim (),
			code.Replace ("\r\n", "\n").Replace ('\r', '\n').Split ('\n'));
	}
}
