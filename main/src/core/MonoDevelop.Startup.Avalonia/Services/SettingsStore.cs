using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// Typed access to the legacy MonoDevelopProperties.xml property store plus the side
/// files the GTK IDE uses (KeyBindings/Custom.kb.xml, MonoDevelop-tools.xml) with the
/// exact same keys and XML formats, so settings written here are read by the GTK UI
/// and vice versa.
/// </summary>
public static class SettingsStore
{
	// ---------- Generic property access (wrappers over UserPreferences) ----------

	public static bool GetBool (string key, bool def)
		=> bool.TryParse (UserPreferences.Get (key), out var v) ? v : def;

	public static void SetBool (string key, bool v)
		=> UserPreferences.Set (key, v ? "True" : "False");

	public static string? GetString (string key) => UserPreferences.Get (key);

	public static void SetString (string key, string? v)
		=> UserPreferences.Set (key, string.IsNullOrEmpty (v) ? null : v);

	// ---------- Key bindings (KeyBindings/Custom.kb.xml — KeyBindingSet.Save format) ----------
	//
	// Legacy: KeyBindingService.SaveCurrentBindings writes
	//   <schemes version="1.0"><scheme name="current">
	//     <binding command="MonoDevelop.Ide.Commands.FileCommands.Save" shortcut="Control+S"/>
	//   </scheme></schemes>
	// to ~/.local/share/MonoDevelop/9.0/KeyBindings/Custom.kb.xml.

	static string KeyBindingsPath => Path.Combine (
		Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
		".local", "share", "MonoDevelop", "9.0", "KeyBindings", "Custom.kb.xml");

	public static Dictionary<string, string> LoadKeyBindings ()
	{
		var result = new Dictionary<string, string> (StringComparer.Ordinal);
		try {
			var path = KeyBindingsPath;
			if (!File.Exists (path))
				return result;
			var doc = XDocument.Load (path);
			foreach (var b in doc.Descendants ("binding")) {
				var cmd = (string?)b.Attribute ("command");
				var shortcut = (string?)b.Attribute ("shortcut");
				if (!string.IsNullOrEmpty (cmd))
					result [cmd!] = shortcut ?? "";
			}
		} catch (Exception ex) {
			Console.WriteLine ("[settings] keybindings load failed: " + ex.Message);
		}
		return result;
	}

	public static void SaveKeyBindings (IReadOnlyDictionary<string, string> bindings)
	{
		try {
			var path = KeyBindingsPath;
			Directory.CreateDirectory (Path.GetDirectoryName (path)!);
			var root = new XElement ("schemes", new XAttribute ("version", "1.0"),
				new XElement ("scheme", new XAttribute ("name", "current"),
					bindings.Where (kvp => !string.IsNullOrEmpty (kvp.Value))
						.Select (kvp => new XElement ("binding",
							new XAttribute ("command", kvp.Key),
							new XAttribute ("shortcut", kvp.Value)))));
			root.Save (path);
		} catch (Exception ex) {
			Console.WriteLine ("[settings] keybindings save failed: " + ex.Message);
		}
	}

	// ---------- External tools (MonoDevelop-tools.xml — ExternalToolService format) ----------

	public sealed class ExternalTool
	{
		public string MenuCommand = "";
		public string Command = "";
		public string Arguments = "";
		public string InitialDirectory = "";
		public string AccelKey = "";
		public bool PromptForArguments;
		public bool UseOutputPad = true;
		public bool SaveCurrentFile;
	}

	static string ToolsPath => Path.Combine (
		Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
		".config", "MonoDevelop", "9.0", "MonoDevelop-tools.xml");

	public static List<ExternalTool> LoadTools ()
	{
		var tools = new List<ExternalTool> ();
		try {
			var path = ToolsPath;
			if (File.Exists (path)) {
				var doc = XDocument.Load (path);
				foreach (var el in doc.Descendants ("ExternalTool")) {
					tools.Add (new ExternalTool {
						MenuCommand = (string?)el.Attribute ("menuCommand") ?? "",
						Command = (string?)el.Attribute ("command") ?? "",
						Arguments = (string?)el.Attribute ("arguments") ?? "",
						InitialDirectory = (string?)el.Attribute ("initialDirectory") ?? "",
						AccelKey = (string?)el.Attribute ("accelKey") ?? "",
						PromptForArguments = ((string?)el.Attribute ("promptForArguments")) == "True",
						UseOutputPad = ((string?)el.Attribute ("useOutputPad")) != "False",
						SaveCurrentFile = ((string?)el.Attribute ("saveCurrentFile")) == "True",
					});
				}
			}
		} catch (Exception ex) {
			Console.WriteLine ("[settings] tools load failed: " + ex.Message);
		}
		return tools;
	}

	public static void SaveTools (IReadOnlyList<ExternalTool> tools)
	{
		try {
			var path = ToolsPath;
			Directory.CreateDirectory (Path.GetDirectoryName (path)!);
			var root = new XElement ("Tools", new XAttribute ("version", "2.0"),
				tools.Select (t => new XElement ("ExternalTool",
					new XAttribute ("menuCommand", t.MenuCommand),
					new XAttribute ("command", t.Command),
					new XAttribute ("arguments", t.Arguments),
					new XAttribute ("initialDirectory", t.InitialDirectory),
					new XAttribute ("accelKey", t.AccelKey),
					new XAttribute ("promptForArguments", t.PromptForArguments ? "True" : "False"),
					new XAttribute ("useOutputPad", t.UseOutputPad ? "True" : "False"),
					new XAttribute ("saveCurrentFile", t.SaveCurrentFile ? "True" : "False"))));
			root.Save (path);
		} catch (Exception ex) {
			Console.WriteLine ("[settings] tools save failed: " + ex.Message);
		}
	}

	// ---------- Task priority colors (legacy rgb:rrrr/gggg/bbbb format) ----------

	public static string? GetTaskColor (string key)
	{
		var v = UserPreferences.Get (key);
		if (string.IsNullOrEmpty (v))
			return null;
		// legacy "rgb:ffff/0000/0000" → "#FF0000"
		try {
			var parts = v.Replace ("rgb:", "").Split ('/');
			if (parts.Length == 3)
				return "#" + string.Join ("", parts.Select (p => ClampHex (p)));
		} catch { }
		return null;
	}

	public static void SetTaskColor (string key, string hex)
	{
		// "#FF0000" → legacy "rgb:ffff/0000/0000"
		try {
			if (hex.Length == 7 && hex [0] == '#') {
				string Scale (string c) => new string (c [0], 2) + new string (c [1], 2);
				UserPreferences.Set (key,
					$"rgb:{Scale (hex.Substring (1, 2))}/{Scale (hex.Substring (3, 2))}/{Scale (hex.Substring (5, 2))}");
				return;
			}
			UserPreferences.Set (key, hex.Length > 0 ? hex : null);
		} catch { }
	}

	static string ClampHex (string p)
		=> p.Length >= 2 ? p.Substring (0, 2) : p.PadLeft (2, '0');

	// ---------- Fonts (legacy FontsPanel: nested FontProperties property) ----------
	//
	// Legacy stores a nested Properties node:
	//   <Property key="FontProperties">
	//     <Properties><Property key="Editor" value="Monospace 12"/>...</Properties>
	//   </Property>
	// Our flat store keeps the nested XML as the string value of FontProperties.
	static readonly string[] FontRoles = { "Editor", "Pad", "OutputPad" };

	public static string? GetFontSpec (string role)
	{
		try {
			var raw = UserPreferences.Get ("FontProperties");
			if (string.IsNullOrEmpty (raw))
				return null;
			var doc = XElement.Parse (raw);
			return (string?)doc.Elements ("Property")
				.FirstOrDefault (e => (string?)e.Attribute ("key") == role)?.Attribute ("value");
		} catch {
			return null;
		}
	}

	public static void SetFontSpec (string role, string spec)
	{
		try {
			var raw = UserPreferences.Get ("FontProperties");
			var doc = string.IsNullOrEmpty (raw) ? new XElement ("Properties") : XElement.Parse (raw);
			var el = doc.Elements ("Property").FirstOrDefault (e => (string?)e.Attribute ("key") == role);
			if (el is null)
				doc.Add (new XElement ("Property", new XAttribute ("key", role), new XAttribute ("value", spec)));
			else
				el.SetAttributeValue ("value", spec);
			UserPreferences.Set ("FontProperties", doc.ToString (SaveOptions.DisableFormatting));
		} catch (Exception ex) {
			Console.WriteLine ("[settings] font save failed: " + ex.Message);
		}
	}

	public static void ClearFonts () => UserPreferences.Set ("FontProperties", null);
}
