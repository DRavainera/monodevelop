using System;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ComboBoxEntry : ComboBox {

		public static new Gtk.ComboBox CreateInstance ()
		{
			Gtk.ComboBox c = Gtk.ComboBox.NewWithEntry ();
			// Make sure all children are created, so the mouse events can be
			// bound and the widget can be selected.
			c.EnsureStyle ();
			try {
				FixSensitivity (c);
			} catch {
			}
			return c;
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			if (IsTextCombo) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (new CodeTypeReference ("Gtk.ComboBox", CodeTypeReferenceOptions.GlobalReference)),
					"NewWithEntry"
				);
			} else
				return base.GenerateObjectCreation (ctx);
		}
	}
}
