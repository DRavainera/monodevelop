using System;
using System.Collections;
using System.Xml;

namespace Stetic.Wrapper {

	public class OptionMenu : Container {

		public override void Wrap (object obj, bool initialized)
		{
			Gtk.MenuButton omenu = (Gtk.MenuButton)obj;
			if (omenu.Popup == null) {
				Gtk.Menu menu = new Gtk.Menu ();
				menu.Show ();
				omenu.Popup = menu;
			}

			base.Wrap (obj, initialized);
		}

		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			int history = (int)GladeUtils.ExtractProperty (elem, "history", -1);
			base.ReadProperties (reader, elem);

			if (history != -1)
				Active = history;
			else
				Active = 0;
		}

		// Some versions of glade call the menu an internal child, some don't

		protected override ObjectWrapper ReadInternalChild (ObjectReader reader, XmlElement child_elem)
		{
			if (child_elem.GetAttribute ("internal-child") == "menu")
				return ReadChild (reader, child_elem);
			else
				return base.ReadInternalChild (reader, child_elem);
		}

		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (optionmenu.Popup);
			reader.ReadObject (wrapper, child_elem["widget"]);
			return wrapper;
		}

		public override IEnumerable GladeChildren {
			get {
				return new Gtk.Widget[] { optionmenu.Popup };
			}
		}

		Gtk.MenuButton optionmenu {
			get {
				return (Gtk.MenuButton)Wrapped;
			}
		}

		int active;

		public int Active {
			get {
				return active;
			}
			set {
				active = value;
				EmitNotify ("Active");
			}
		}
	}
}
