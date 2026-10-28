//
// GladeCompat.cs
//
// Compatibilidad de los diálogos Glade 2.0 (gui.glade / stetic.glade) con el
// binding GtkSharp 3.24: el ensamblado glade-sharp 2.12 no existe en el stack
// net10.0, así que este módulo reproduce la superficie mínima que usa el código:
//   - Glade.WidgetAttribute ([Glade.Widget] / [Glade.Widget("Nombre")])
//   - Glade.XML (ctor archivo o recurso, GetWidgetById, Autoconnect)
//   - Glade.XML.Autoconnect: liga campos [Glade.Widget] y conecta <signal>.
//
// El cargador soporta el subconjunto de Glade 2.0 presente en los .glade del
// proyecto (ver los comentarios por sección). No es un reemplazo general de
// glade-sharp.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Glade
{
	[AttributeUsage (AttributeTargets.Field, AllowMultiple = false)]
	public class WidgetAttribute : Attribute
	{
		public string Name { get; }

		public WidgetAttribute ()
		{
		}

		public WidgetAttribute (string name)
		{
			Name = name;
		}
	}

	public class XML
	{
		sealed class SignalBinding
		{
			public Gtk.Widget Widget;
			public string SignalName;
			public string HandlerName;
		}

		readonly XmlDocument doc = new XmlDocument ();
		readonly Dictionary<string, Gtk.Widget> widgetsById = new Dictionary<string, Gtk.Widget> (StringComparer.OrdinalIgnoreCase);
		readonly List<SignalBinding> signals = new List<SignalBinding> ();
		readonly List<string> warnings = new List<string> ();
		Gtk.Widget rootWidget;

		public XML (string filename, string root_name) : this (filename, root_name, null, null)
		{
		}

		public XML (string filename, string root_name, string lang) : this (filename, root_name, lang, null)
		{
		}

		public XML (string domain, string filename, string root_name, string lang)
		{
			if (File.Exists (filename)) {
				doc.Load (filename);
			} else {
				// El .glade va embebido en el ensamblado del usuario (p.ej.
				// "stetic.glade" en libstetic): probar el ensamblador y luego este.
				try {
					LoadResource (Assembly.GetCallingAssembly (), filename);
				} catch (Exception) {
					LoadResource (GetType ().Assembly, filename);
				}
			}
			Initialize (root_name);
		}

		void LoadResource (Assembly asm, string name)
		{
			using (var s = asm.GetManifestResourceStream (name)) {
				if (s == null)
					throw new FileNotFoundException (
						$"Recurso Glade '{name}' no encontrado en el ensamblado {asm.FullName}");
				doc.Load (s);
			}
		}

		void Initialize (string root_name)
		{
			XmlElement rootEl = null;
			XmlNodeList toplevels = doc.GetElementsByTagName ("widget");
			foreach (XmlElement el in toplevels) {
				if (el.ParentNode is XmlElement pe && pe.Name == "glade-interface" && el.GetAttribute ("id") == root_name) {
					rootEl = el;
					break;
				}
			}
			if (rootEl == null) {
				foreach (XmlElement el in toplevels) {
					if (el.GetAttribute ("id") == root_name) {
						rootEl = el;
						break;
					}
				}
			}
			if (rootEl == null)
				throw new ArgumentException ($"Widget raíz '{root_name}' no encontrado en el documento Glade");
			rootWidget = Builder.Build (this, rootEl, null, null);
		}

		public Gtk.Widget GetWidgetById (string id)
		{
			return widgetsById.TryGetValue (id, out var w) ? w : null;
		}

		public void Autoconnect (object target)
		{
			if (target == null)
				return;

			foreach (FieldInfo f in AllFields (target.GetType ())) {
				var attr = (WidgetAttribute) f.GetCustomAttribute (typeof (WidgetAttribute));
				if (attr == null)
					continue;
				string name = string.IsNullOrEmpty (attr.Name) ? f.Name : attr.Name;
				if (widgetsById.TryGetValue (name, out var w))
					f.SetValue (target, w);
				else
					AddWarning ($"Autoconnect: no hay widget '{name}' para el campo {f.DeclaringType.Name}.{f.Name}");
			}

			foreach (var sb in signals) {
				string eventName = ToPascal (sb.SignalName);
				EventInfo ev = FindEvent (sb.Widget.GetType (), eventName);
				if (ev == null) {
					AddWarning ($"Autoconnect: evento '{eventName}' no existe en {sb.Widget.GetType ().Name} (señal '{sb.SignalName}')");
					continue;
				}
				MethodInfo handler = FindHandler (target.GetType (), sb.HandlerName, ev.EventHandlerType);
				if (handler == null) {
					AddWarning ($"Autoconnect: manejador '{sb.HandlerName}' no encontrado o firma incompatible en {target.GetType ().Name}");
					continue;
				}
				try {
					ev.AddEventHandler (sb.Widget, Delegate.CreateDelegate (ev.EventHandlerType, target, handler));
				} catch (Exception ex) {
					AddWarning ($"Autoconnect: no se pudo conectar {sb.HandlerName}: {ex.Message}");
				}
			}

			foreach (string w in warnings)
				Console.Error.WriteLine ($"[Glade] {w}");
			warnings.Clear ();
		}

		internal void AddWarning (string msg) => warnings.Add (msg);

		internal void RegisterWidget (string id, Gtk.Widget w)
		{
			if (!string.IsNullOrEmpty (id) && w != null)
				widgetsById[id] = w;
		}

		internal void AddSignal (Gtk.Widget w, string signalName, string handlerName)
		{
			if (!string.IsNullOrEmpty (signalName) && !string.IsNullOrEmpty (handlerName))
				signals.Add (new SignalBinding { Widget = w, SignalName = signalName, HandlerName = handlerName });
		}

		static IEnumerable<FieldInfo> AllFields (Type t)
		{
			var list = new List<FieldInfo> ();
			for (Type c = t; c != null && c != typeof (object); c = c.BaseType)
				list.AddRange (c.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
			return list;
		}

		static EventInfo FindEvent (Type t, string name)
		{
			for (Type c = t; c != null && c != typeof (object); c = c.BaseType) {
				var ev = c.GetEvent (name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				if (ev != null)
					return ev;
			}
			return null;
		}

		static MethodInfo FindHandler (Type t, string name, Type delegateType)
		{
			MethodInfo invoke = delegateType.GetMethod ("Invoke");
			if (invoke == null)
				return null;
			var dparams = invoke.GetParameters ();
			if (dparams.Length < 2)
				return null;
			Type argsType = dparams[1].ParameterType;
			for (Type c = t; c != null && c != typeof (object); c = c.BaseType) {
				foreach (MethodInfo m in c.GetMethods (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
					if (m.Name != name || m.GetParameters ().Length != 2)
						continue;
					var p = m.GetParameters ();
					if (p[0].ParameterType != typeof (object))
						continue;
					// Contravariancia: el tipo de parámetro del método puede ser una
					// base (p.ej. EventArgs) del tipo de args del evento.
					if (p[1].ParameterType.IsAssignableFrom (argsType))
						return m;
				}
			}
			return null;
		}

		static string ToPascal (string s)
		{
			return string.Concat (s.Split ('_')
				.Where (x => x.Length > 0)
				.Select (x => char.ToUpperInvariant (x[0]) + x.Substring (1)));
		}
	}

	static class Builder
	{
		static readonly Dictionary<string, Func<object>> Factories = new Dictionary<string, Func<object>> {
			{ "GtkDialog", () => new Gtk.Dialog () },
			{ "GtkWindow", () => new Gtk.Window (Gtk.WindowType.Toplevel) },
			{ "GtkVBox", () => new Gtk.VBox () },
			{ "GtkHBox", () => new Gtk.HBox () },
			{ "GtkBox", () => new Gtk.HBox () },
			{ "GtkButton", () => new Gtk.Button () },
			{ "GtkCheckButton", () => new Gtk.CheckButton () },
			{ "GtkRadioButton", () => new Gtk.RadioButton ((string) null) },
			{ "GtkToggleButton", () => new Gtk.ToggleButton () },
			{ "GtkEntry", () => new Gtk.Entry () },
			{ "GtkLabel", () => new Gtk.Label (null) },
			{ "GtkNotebook", () => new Gtk.Notebook () },
			{ "GtkScrolledWindow", () => new Gtk.ScrolledWindow (null, null) },
			{ "GtkViewport", () => new Gtk.Viewport (null, null) },
			{ "GtkTreeView", () => new Gtk.TreeView (null) },
			{ "GtkImage", () => new Gtk.Image () },
			{ "GtkComboBox", () => new Gtk.ComboBox () },
			{ "GtkComboBoxEntry", () => Gtk.ComboBox.NewWithEntry () },
			{ "GtkFileChooserWidget", () => new Gtk.FileChooserWidget (Gtk.FileChooserAction.Open) },
			{ "GtkFileChooserDialog", () => new Gtk.FileChooserDialog ("", null, Gtk.FileChooserAction.Open, null) },
			{ "GtkFrame", () => new Gtk.Frame () },
			{ "GtkHButtonBox", () => new Gtk.HButtonBox () },
			{ "GtkVButtonBox", () => new Gtk.VButtonBox () },
			{ "GtkButtonBox", () => new Gtk.HButtonBox () },
			{ "GtkHSeparator", () => new Gtk.HSeparator () },
			{ "GtkVSeparator", () => new Gtk.VSeparator () },
			{ "GtkTextView", () => new Gtk.TextView () },
			{ "GtkTable", () => new Gtk.Table (0, 0, false) },
			{ "GtkEventBox", () => new Gtk.EventBox () },
			{ "GtkFixed", () => new Gtk.Fixed () },
			{ "GtkExpander", () => new Gtk.Expander ("") },
			{ "GtkAlignment", () => new Gtk.Alignment (0, 0, 1, 1) },
		};

		// Etiquetas estándar GTK3 para los botones que en Glade 2.0 usaban stock
		// (use_stock + label="gtk-*"): sin stock, se muestran con su texto.
		static readonly Dictionary<string, string> StockLabels = new Dictionary<string, string> {
			{ "gtk-ok", "_OK" },
			{ "gtk-apply", "_Apply" },
			{ "gtk-cancel", "_Cancel" },
			{ "gtk-close", "_Close" },
			{ "gtk-help", "_Help" },
			{ "gtk-yes", "_Yes" },
			{ "gtk-no", "_No" },
		};

		public static Gtk.Widget Build (XML ctx, XmlElement el, Gtk.Container parent, Gtk.Dialog actionAreaOwner)
		{
			string klass = el.GetAttribute ("class");
			string id = el.GetAttribute ("id");
			if (!Factories.TryGetValue (klass, out var factory))
				throw new NotSupportedException ($"Clase Glade '{klass}' no soportada por el cargador de compatibilidad");

			Gtk.Widget w = (Gtk.Widget) factory ();
			ctx.RegisterWidget (id, w);

			var props = GetProps (el);
			ApplyStockFix (w, props);
			foreach (var kv in props)
				SetProperty (ctx, w, kv.Key, kv.Value);

			foreach (XmlElement sig in ChildElements (el, "signal"))
				ctx.AddSignal (w, sig.GetAttribute ("name"), sig.GetAttribute ("handler"));

			if (parent != null)
				AttachChild (ctx, w, el, parent, actionAreaOwner);

			foreach (XmlElement child in ChildElements (el, "child"))
				BuildChild (ctx, child, (Gtk.Container) w, actionAreaOwner);

			return w;
		}

		static void BuildChild (XML ctx, XmlElement childEl, Gtk.Container parent, Gtk.Dialog actionAreaOwner)
		{
			if (childEl.SelectSingleNode ("placeholder") != null)
				return;

			XmlElement widgetEl = (XmlElement) childEl.SelectSingleNode ("widget");
			if (widgetEl == null)
				return;

			string internalChild = childEl.GetAttribute ("internal-child");

			// GtkDialog: el vbox y el action_area internos ya existen; no se
			// instancian los contenedores del XML, se reutilizan los del diálogo.
			if (parent is Gtk.Dialog dlg && !string.IsNullOrEmpty (internalChild)) {
				Gtk.Container target = internalChild == "action_area" ? dlg.ActionArea : dlg.ContentArea;
				ctx.RegisterWidget (widgetEl.GetAttribute ("id"), target);
				var props = GetProps (widgetEl);
				foreach (var kv in props)
					SetProperty (ctx, target, kv.Key, kv.Value);
				Gtk.Dialog owner = internalChild == "action_area" ? dlg : actionAreaOwner;
				foreach (XmlElement c in ChildElements (widgetEl, "child"))
					BuildChild (ctx, c, target, owner);
				return;
			}

			Build (ctx, widgetEl, parent, actionAreaOwner);
		}

		static void AttachChild (XML ctx, Gtk.Widget child, XmlElement widgetEl, Gtk.Container parent, Gtk.Dialog actionAreaOwner)
		{
			XmlElement packingEl = (XmlElement) widgetEl.ParentNode;
			XmlElement packing = packingEl != null ? (XmlElement) packingEl.SelectSingleNode ("packing") : null;
			var pack = packing != null ? GetProps (packing) : new Dictionary<string, string> ();

			if (parent is Gtk.ButtonBox && actionAreaOwner != null) {
				if (int.TryParse (PackGet (pack, "response_id"), out int responseId))
					actionAreaOwner.AddActionWidget (child, responseId);
				else
					parent.Add (child);
				return;
			}

			if (parent is Gtk.Box box) {
				bool end = PackGet (pack, "pack_type") == "GTK_PACK_END";
				bool expand = Bool (PackGet (pack, "expand"), false);
				bool fill = Bool (PackGet (pack, "fill"), true);
				int padding = Int (PackGet (pack, "padding"), 0);
				if (end)
					box.PackEnd (child, expand, fill, (uint) padding);
				else
					box.PackStart (child, expand, fill, (uint) padding);
				return;
			}

			if (parent is Gtk.Notebook nb) {
				// En Glade 2.0 la pestaña es <widget-child>, hermano de <child>.
				XmlElement notebookEl = (XmlElement) widgetEl.ParentNode;
				notebookEl = notebookEl != null ? (XmlElement) notebookEl.ParentNode : null;
				XmlElement tabWidget = notebookEl != null ? (XmlElement) notebookEl.SelectSingleNode ("widget-child") : null;
				Gtk.Widget tab = tabWidget != null ? Build (ctx, tabWidget, null, null) : null;
				nb.AppendPage (child, tab);
				return;
			}

			if (parent is Gtk.Table table) {
				uint left = (uint) Int (PackGet (pack, "left_attach"), 0);
				uint right = (uint) Int (PackGet (pack, "right_attach"), 1);
				uint top = (uint) Int (PackGet (pack, "top_attach"), 0);
				uint bottom = (uint) Int (PackGet (pack, "bottom_attach"), 1);
				uint xpad = (uint) Int (PackGet (pack, "xpad"), 0);
				uint ypad = (uint) Int (PackGet (pack, "ypad"), 0);
				table.Attach (child, left, right, top, bottom,
					ParseAttachOptions (PackGet (pack, "x_options")), ParseAttachOptions (PackGet (pack, "y_options")),
					xpad, ypad);
				return;
			}

			if (parent is Gtk.ScrolledWindow sw) {
				if (child is Gtk.IScrollable)
					sw.Add (child);
				else
					sw.AddWithViewport (child);
				return;
			}

			parent.Add (child);
		}

		static Gtk.AttachOptions ParseAttachOptions (string s)
		{
			if (string.IsNullOrEmpty (s))
				return Gtk.AttachOptions.Fill;
			Gtk.AttachOptions o = 0;
			foreach (string part in s.Split (new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				switch (part.ToLowerInvariant ()) {
					case "expand": o |= Gtk.AttachOptions.Expand; break;
					case "fill": o |= Gtk.AttachOptions.Fill; break;
					case "fixed": break;
					case "none": break;
				}
			}
			return o;
		}

		static void ApplyStockFix (Gtk.Widget w, Dictionary<string, string> props)
		{
			if (!(w is Gtk.Button) && !(w is Gtk.MenuItem))
				return;
			if (Bool (PackGet (props, "use_stock"), false) && props.ContainsKey ("label") &&
				StockLabels.TryGetValue (props["label"], out var label)) {
				props.Remove ("use_stock");
				props["label"] = label;
			}
			if (props.ContainsKey ("stock") && StockLabels.TryGetValue (props["stock"], out var label2)) {
				props.Remove ("stock");
				if (!props.ContainsKey ("label"))
					props["label"] = label2;
			}
		}

		static void SetProperty (XML ctx, Gtk.Widget w, string name, string raw)
		{
			if (string.IsNullOrEmpty (raw) || name == "response_id" || name == "items" || name == "translatable")
				return;

			PropertyInfo prop = FindProperty (w.GetType (), name);
			if (prop == null || prop.SetMethod == null) {
				if (name == "label")
					prop = FindProperty (w.GetType (), "Text");
				if (prop == null || prop.SetMethod == null) {
					ctx.AddWarning ($"Propiedad Glade '{name}' no existe (o es de solo lectura) en {w.GetType ().Name}; valor ignorado");
					return;
				}
			}

			try {
				prop.SetValue (w, ConvertValue (ctx, w, name, prop.PropertyType, raw));
			} catch (Exception ex) {
				ctx.AddWarning ($"No se pudo establecer {w.GetType ().Name}.{name} = '{raw}': {ex.Message}");
			}
		}

		static PropertyInfo FindProperty (Type t, string name)
		{
			for (Type c = t; c != null && c != typeof (object); c = c.BaseType) {
				var p = c.GetProperty (name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				if (p != null)
					return p;
			}
			return null;
		}

		static object ConvertValue (XML ctx, Gtk.Widget w, string propName, Type t, string raw)
		{
			if (t == typeof (bool))
				return raw == "True" || raw == "true" || raw == "1";
			if (t == typeof (int))
				return int.Parse (raw, CultureInfo.InvariantCulture);
			if (t == typeof (uint))
				return uint.Parse (raw, CultureInfo.InvariantCulture);
			if (t == typeof (float))
				return float.Parse (raw, CultureInfo.InvariantCulture);
			if (t == typeof (double))
				return double.Parse (raw, CultureInfo.InvariantCulture);
			if (t == typeof (char))
				return raw.Length > 0 ? raw[0] : '\0';
			if (t == typeof (string))
				return raw;
			if (t.IsEnum)
				return ConvertEnum (t, raw);
			if (t == typeof (Gtk.RadioButton[])) {
				// En Glade 2.0 "group" referencia el id de otro RadioButton.
				if (w is Gtk.RadioButton radio && ctx.GetWidgetById (raw) is Gtk.RadioButton group)
					return new Gtk.RadioButton[] { group };
			}
			throw new InvalidOperationException ($"Tipo no soportado para propiedad Glade: {t.FullName}");
		}

		static object ConvertEnum (Type t, string raw)
		{
			if (int.TryParse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
				return Enum.ToObject (t, num);
			string[] segs = raw.Split ('_');
			for (int i = 0; i < segs.Length; i++) {
				string candidate = string.Concat (
					Enumerable.Range (i, segs.Length - i).Select (j => PascalSeg (segs[j])));
				foreach (string n in t.GetEnumNames ()) {
					if (string.Equals (n, candidate, StringComparison.OrdinalIgnoreCase))
						return Enum.Parse (t, n);
				}
			}
			throw new InvalidOperationException ($"Valor de enumeración '{raw}' no mapeable a {t.Name}");
		}

		static string PascalSeg (string s) => char.ToUpperInvariant (s[0]) + s.Substring (1).ToLowerInvariant ();

		static Dictionary<string, string> GetProps (XmlElement el)
		{
			var result = new Dictionary<string, string> (StringComparer.Ordinal);
			foreach (XmlElement p in ChildElements (el, "property"))
				result[p.GetAttribute ("name")] = (p.InnerText ?? string.Empty).Trim ();
			return result;
		}

		static IEnumerable<XmlElement> ChildElements (XmlElement el, string name)
		{
			foreach (XmlNode n in el.ChildNodes)
				if (n is XmlElement e && e.Name == name)
					yield return e;
		}

		static string PackGet (Dictionary<string, string> pack, string key)
		{
			return pack != null && pack.TryGetValue (key, out var v) ? v : null;
		}

		static bool Bool (string s, bool fallback)
		{
			if (s == null)
				return fallback;
			return s == "True" || s == "true" || s == "1";
		}

		static int Int (string s, int fallback)
		{
			return int.TryParse (s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
		}
	}
}
