using System;

namespace Stetic
{
	// Equivalente de la clase Gtk.Stock del binding 2.12 (eliminado en 3.24):
	// constantes de stock-id y lookup mínimo para el código del diseñador.
	static class StockCompat
	{
		public const string Add = "gtk-add";
		public const string Apply = "gtk-apply";
		public const string Bold = "gtk-bold";
		public const string Cancel = "gtk-cancel";
		public const string Clear = "gtk-clear";
		public const string Close = "gtk-close";
		public const string Copy = "gtk-copy";
		public const string Cut = "gtk-cut";
		public const string Delete = "gtk-delete";
		public const string DialogError = "gtk-dialog-error";
		public const string DialogInfo = "gtk-dialog-info";
		public const string DialogQuestion = "gtk-dialog-question";
		public const string DialogWarning = "gtk-dialog-warning";
		public const string Execute = "gtk-execute";
		public const string Find = "gtk-find";
		public const string Help = "gtk-help";
		public const string MissingImage = "gtk-missing-image";
		public const string New = "gtk-new";
		public const string No = "gtk-no";
		public const string Open = "gtk-open";
		public const string Ok = "gtk-ok";
		public const string Paste = "gtk-paste";
		public const string Remove = "gtk-remove";
		public const string Save = "gtk-save";
		public const string SortAscending = "gtk-sort-ascending";
		public const string Yes = "gtk-yes";
		
		public static string[] ListIds ()
		{
			return new[] {
				Add, Apply, Bold, Cancel, Clear, Close, Copy, Cut, Delete,
				DialogError, DialogInfo, DialogQuestion, DialogWarning,
				Execute, Find, Help, MissingImage, New, No, Open, Ok,
				Paste, Remove, Save, SortAscending, Yes
			};
		}
		
		public static Gtk.StockItem Lookup (string id)
		{
			return new Gtk.StockItem {
				StockId = id,
				Label = id
			};
		}
	}
}
