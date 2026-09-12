namespace System.Runtime.Serialization
{
	using System;
	using System.Collections.Generic;

	public class ImportOptions
	{
		public List<Type> ReferencedCollectionTypes { get; } = new List<Type> ();
	}

	public class XsdDataContractImporter
	{
		public ImportOptions Options { get; set; }
	}
}