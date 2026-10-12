using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoDevelop.AvaloniaAddons.Extensions;

namespace MonoDevelop.AvaloniaAddons
{
	/// <summary>
	/// Minimal MEF-like composition (Visual Studio model): parts decorated with
	/// [Export] are discovered per add-in assembly and injected through [Import] /
	/// [ImportMany]. Deliberately small: no change tracking, one-shot resolution.
	/// </summary>
	public interface ICompositionHost
	{
		IEnumerable<object> GetExports (Type contractType, string contractName = null);
		IReadOnlyList<object> Parts { get; }
		void Compose (object target);
	}

	public sealed class CompositionHost : ICompositionHost
	{
		readonly List<object> parts = new ();

		public IReadOnlyList<object> Parts => parts;

		public void Add (object part)
		{
			if (part is not null)
				parts.Add (part);
		}

		public void AddRange (IEnumerable<object> p)
		{
			foreach (var x in p)
				Add (x);
		}

		public IEnumerable<object> GetExports (Type contractType, string contractName = null)
		{
			foreach (var p in parts) {
				var t = p.GetType ();
				var exp = t.GetCustomAttribute<ExportAttribute> ();
				if (exp is null)
					continue;
				if (contractType is not null) {
					if (!contractType.IsInstanceOfType (p))
						continue;
				} else if (!string.IsNullOrEmpty (contractName)) {
					if (exp.ContractType?.FullName == contractName || exp.ContractName == contractName)
						continue;
				} else {
					continue;
				}
				yield return p;
			}
		}

		static bool Matches (ExportAttribute exp, Type contractType, string contractName)
		{
			if (contractType is not null)
				return exp.ContractType is null || exp.ContractType == contractType || exp.ContractType.IsAssignableFrom (contractType);
			if (!string.IsNullOrEmpty (contractName))
				return exp.ContractType?.FullName == contractName || exp.ContractName == contractName;
			return false;
		}

		/// <summary>Injects exports into [Import] properties/fields of <paramref name="target"/>.</summary>
		public void Compose (object target)
		{
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			foreach (var p in target.GetType ().GetProperties (flags)) {
				if (!p.CanWrite)
					continue;
				var imp = p.GetCustomAttribute<ImportAttribute> ();
				var many = p.GetCustomAttribute<ImportManyAttribute> ();
				if (imp is not null) {
					var match = parts.FirstOrDefault (x => x.GetType ().GetCustomAttribute<ExportAttribute> () is { } e && Matches (e, imp.ContractType, imp.ContractName));
					if (match is not null)
						p.SetValue (target, match);
				} else if (many is not null) {
					var listType = p.PropertyType.IsArray ? p.PropertyType.GetElementType () : typeof (object);
					var items = parts.Where (x => x.GetType ().GetCustomAttribute<ExportAttribute> () is { } e && Matches (e, many.ContractType, many.ContractName)).ToList ();
					if (p.PropertyType.IsArray) {
						var arr = Array.CreateInstance (listType, items.Count);
						for (int i = 0; i < items.Count; i++)
							arr.SetValue (items [i], i);
						p.SetValue (target, arr);
					} else if (typeof (IEnumerable).IsAssignableFrom (p.PropertyType)) {
						p.SetValue (target, items);
					}
				}
			}
		}
	}
}
