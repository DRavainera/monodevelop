using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IOptionPersister))]
	[Shared]
	sealed class MonoDevelopOptionPersister : IOptionPersister
	{
		readonly Dictionary<string, object> values = new Dictionary<string, object> ();

		public bool TryFetch (OptionKey key, out object value)
		{
			return values.TryGetValue (PersistedKey (key), out value);
		}

		public bool TryPersist (OptionKey key, object value)
		{
			values [PersistedKey (key)] = value;
			return true;
		}

		static string PersistedKey (OptionKey key)
		{
			return key.Language + ":" + key.Option.Feature + ":" + key.Option.Name;
		}
	}
}