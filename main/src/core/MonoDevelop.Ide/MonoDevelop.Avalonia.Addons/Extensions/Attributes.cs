using System;

namespace MonoDevelop.AvaloniaAddons.Extensions
{
	/// <summary>MEF-style export (as in the Visual Studio SDK).</summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class ExportAttribute : Attribute
	{
		public ExportAttribute () { }
		public ExportAttribute (string contractName) => ContractName = contractName;
		/// <summary>Optional contract type the export is bound to.</summary>
		public Type ContractType { get; set; }
		public string ContractName { get; }
	}

	/// <summary>MEF-style import of exactly one export.</summary>
	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ImportAttribute : Attribute
	{
		public ImportAttribute () { }
		public ImportAttribute (string contractName) => ContractName = contractName;
		public Type ContractType { get; set; }
		public string ContractName { get; }
	}

	/// <summary>MEF-style import of a collection.</summary>
	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ImportManyAttribute : Attribute
	{
		public ImportManyAttribute () { }
		public ImportManyAttribute (string contractName) => ContractName = contractName;
		public Type ContractType { get; set; }
		public string ContractName { get; }
	}
}
