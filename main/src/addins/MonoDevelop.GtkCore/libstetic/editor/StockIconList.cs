
using System;
using System.Reflection;
using System.Collections;
using Gtk;

namespace Stetic.Editor
{
	class StockIconList : IconList 
	{
		public StockIconList ()
		{
			foreach (string name in StockCompat.ListIds ())
				AddIcon (name, WidgetUtils.LoadIcon (name, Gtk.IconSize.Menu), name);
		}
	}
}
