using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Avalonia model of the DocFood options (Preferences > Other > DocFood). Holds the
/// editable parts of the legacy DocConfig: the WordLists (ofThe / prefixThe / noThe /
/// acronyms), the WordExpansions (from -> to) and the Macros (tag -> value). The
/// generation Rules are edited as raw XML (the legacy Rules panel is an editable XML
/// area) and are carried as a string so the panel can show/edit them without the
/// add-in's Node tree (which the shell cannot reference: it is internal to the DocFood
/// assembly, which the shell does not load).
///
/// Persistence is a SettingsStore string key (the same mechanism the F# panel uses,
/// and the same "store a whole XML document as a string" precedent as UserPreferences
/// FontProperties). The XML is written in the exact schema the legacy DocConfig.Read
/// understands, so the config is cross-boundary compatible: the GTK-side generator
/// (DocConfig) and this panel share one config, exchanged through the string.
/// </summary>
public sealed class DocFoodConfig
{
	public static readonly string ConfigKey = "DocFood.Config";
	public static readonly string EnabledKey = "DocFood.Enabled";

	public Dictionary<string, List<string>> WordLists { get; } = new ();
	public Dictionary<string, string> WordExpansions { get; } = new ();
	public Dictionary<string, string> Macros { get; } = new ();
	public string RulesXml { get; set; } = "";

	/// <summary>The legacy default WordLists/WordExpansions/Macros, embedded as
	/// DocFood.config.xml (copied from the add-in — single source of truth).</summary>
	public static DocFoodConfig Defaults ()
	{
		var xml = typeof (DocFoodConfig).Assembly.GetManifestResourceStream ("MonoDevelop.AvaloniaShell.DocFood.config.xml");
		if (xml is null)
			throw new InvalidOperationException ("Embedded DocFood.config.xml not found");
		using (xml) {
			var doc = XDocument.Load (xml);
			return FromXml (doc);
		}
	}

	/// <summary>Loads the user config, falling back to the legacy defaults.</summary>
	public static DocFoodConfig Load ()
	{
		var raw = SettingsStore.GetString (ConfigKey);
		if (string.IsNullOrWhiteSpace (raw))
			return Defaults ();
		try {
			return FromXml (XDocument.Parse (raw));
		} catch {
			return Defaults ();
		}
	}

	public static bool LoadEnabled ()
		=> SettingsStore.GetBool (EnabledKey, true);

	public static void Save (DocFoodConfig cfg, bool enabled)
	{
		SettingsStore.SetString (ConfigKey, ToXml (cfg));
		SettingsStore.SetBool (EnabledKey, enabled);
	}

	// ---------- XML (de)serialization (legacy DocFood.config.xml schema) ----------

	public static DocFoodConfig FromXml (XDocument doc)
	{
		var cfg = new DocFoodConfig ();
		var root = doc.Root;
		if (root is null)
			return cfg;

		foreach (var wl in root.Element ("WordLists")?.Elements ("List") ?? Enumerable.Empty<XElement> ()) {
			var name = (string)wl.Attribute ("name");
			if (name is null)
				continue;
			var words = wl.Elements ("Word").Select (w => (string)w).Where (s => !string.IsNullOrEmpty (s)).ToList ();
			cfg.WordLists[name] = words;
		}
		foreach (var we in root.Element ("WordExpansion")?.Elements ("Word") ?? Enumerable.Empty<XElement> ()) {
			var from = (string)we.Attribute ("from");
			if (from is null)
				continue;
			cfg.WordExpansions[from] = (string)we.Attribute ("to") ?? "";
		}
		foreach (var m in root.Element ("Macros")?.Elements ("Macro") ?? Enumerable.Empty<XElement> ()) {
			var tag = (string)m.Attribute ("tag");
			if (tag is null)
				continue;
			cfg.Macros[tag] = (string)m.Attribute ("value") ?? "";
		}
		// Rules: normally an element; when the stored text was invalid XML it is
		// preserved as an XML comment (see RulesElement) so nothing is lost — read
		// the comment back (the first comment child, which only RulesElement adds)
		// so the panel can show (and warn about) it. Note: root.Elements() does not
		// return XComment nodes — .Nodes() is required.
		var rules = root.Element ("Rules");
		if (rules is not null) {
			cfg.RulesXml = RulesToString (rules);
		} else {
			// F3: the rules text was not well-formed XML and RulesElement stored it
			// as a comment so nothing is lost (root.Elements() does not return
			// XComment nodes — .Nodes() is required).
			var comment = root.Nodes ().OfType<XComment> ().FirstOrDefault ();
			if (comment is not null) {
				cfg.RulesXml = comment.Value.Trim ();
			} else {
				// Parseable text with a non-"Rules" root is also user rules text:
				// keep it verbatim (the writer of this doc is ToXml, so any child
				// other than the three known sections can only be rules).
				cfg.RulesXml = root.Elements ()
					.FirstOrDefault (el => el.Name.LocalName is not ("WordLists" or "WordExpansion" or "Macros"))
					?.ToString (SaveOptions.None) ?? "";
			}
		}
		return cfg;
	}

	public static string ToXml (DocFoodConfig cfg)
	{
		var root = new XElement ("DocFood");

		var wordLists = new XElement ("WordLists");
		foreach (var (name, words) in cfg.WordLists)
			wordLists.Add (new XElement ("List",
				new XAttribute ("name", name),
				words.Select (w => new XElement ("Word", w))));
		root.Add (wordLists);

		root.Add (new XElement ("WordExpansion",
			cfg.WordExpansions.Select (kv => new XElement ("Word",
				new XAttribute ("from", kv.Key),
				new XAttribute ("to", kv.Value)))));

		root.Add (new XElement ("Macros",
			cfg.Macros.Select (kv => new XElement ("Macro",
				new XAttribute ("tag", kv.Key),
				new XAttribute ("value", kv.Value)))));

		if (!string.IsNullOrWhiteSpace (cfg.RulesXml))
			root.Add (RulesElement (cfg.RulesXml));

		// IMPORTANT: write FORMATTED (indented), not compact. The legacy reader
		// (XmlReadHelper.ReadList, used by DocConfig.Read) is whitespace-sensitive:
		// its ReadElementString() + leading reader.Read() loop silently drops every
		// other sibling element when there is no whitespace text node between
		// elements. The default DocFood.config.xml is indented, so the store must
		// stay in that shape to remain readable by DocConfig.Read.
		return root.ToString (SaveOptions.None);
	}

	// Embed the Rules: as the element when the text is well-formed XML (so
	// DocConfig.Read picks the tree up), or as an XML comment when it is not
	// (so the user's raw text is preserved in the store instead of being
	// silently dropped — the document must always be parseable).
	static object RulesElement (string rulesXml)
	{
		try {
			return XElement.Parse (rulesXml);
		} catch {
			return new XComment (rulesXml);
		}
	}

	/// <summary>True when the rules text is well-formed XML with a single root
	/// element (what DocConfig.Read expects); an empty/whitespace text is treated
	/// as valid (no rules). Used by the panel to warn instead of silently storing
	/// invalid rules.</summary>
	public static bool RulesAreValid (string? rulesXml)
	{
		if (string.IsNullOrWhiteSpace (rulesXml))
			return true;
		try {
			return XElement.Parse (rulesXml) is not null;
		} catch {
			return false;
		}
	}

	/// <summary>The Rules element as raw XML (indented, for the editable Rules panel).</summary>
	static string RulesToString (XElement rules)
		=> rules.ToString (SaveOptions.None);
}
