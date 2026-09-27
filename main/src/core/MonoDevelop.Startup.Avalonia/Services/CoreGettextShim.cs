//
// CoreGettextShim.cs — compatibility adapter for REAL MonoDevelop dialogs
// compiled into the shell (integration without duplicates).
//
// The genuine MonoDevelop.Ide Xwt dialogs call MonoDevelop.Core's
// GettextCatalog; referencing the whole MonoDevelop.Core assembly would
// drag the legacy addin-engine runtime (Runtime.Preferences static init)
// into the Avalonia shell, so — like the Xwt net10 SystemXamlShim — the
// type is provided as a thin adapter that delegates to the shell's own
// GettextService catalog. Single translation source, no duplication of
// the .mo machinery.
//

using System;

namespace MonoDevelop.Core
{
	/// <summary>Shell-side GettextCatalog surface for compile-in dialogs.</summary>
	public static class GettextCatalog
	{
		public static string GetString (string phrase)
			=> MonoDevelop.Ide.Services.GettextService.T (phrase);

		public static string GetString (string phrase, object arg0)
			=> string.Format (GetString (phrase), arg0);

		public static string GetString (string phrase, object arg0, object arg1)
			=> string.Format (GetString (phrase), arg0, arg1);

		public static string GetString (string phrase, object arg0, object arg1, object arg2)
			=> string.Format (GetString (phrase), arg0, arg1, arg2);

		public static string GetString (string phrase, params object[] args)
			=> string.Format (GetString (phrase), args);
	}
}
