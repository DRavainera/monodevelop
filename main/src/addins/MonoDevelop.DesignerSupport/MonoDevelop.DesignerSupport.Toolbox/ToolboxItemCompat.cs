namespace System.Drawing.Design
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reflection;

	// Compat shim for System.Drawing.Design.ToolboxItem, which is not part of
	// modern .NET. MonoDevelop never instantiates designer toolbox items itself;
	// it only carries around type metadata for WinForms designers loaded from
	// external assemblies (the actual runtime instance is always resolved via
	// reflection/Activator against those assemblies).
	[Serializable]
	public class ToolboxItem
	{
		Type toolType;

		public ToolboxItem ()
		{
		}

		public ToolboxItem (Type toolType)
		{
			this.toolType = toolType;
		}

		Type ToolType {
			get { return toolType; }
		}

		public virtual string TypeName {
			get { return toolType != null ? toolType.AssemblyQualifiedName : null; }
		}

		public virtual System.Reflection.AssemblyName AssemblyName {
			get { return toolType != null ? toolType.Assembly.GetName () : null; }
		}

		public virtual string DisplayName {
			get { return toolType != null ? toolType.Name : null; }
			set { }
		}

		public virtual System.Drawing.Image Bitmap {
			get { return null; }
		}

		public virtual IEnumerable<ToolboxItemFilterAttribute> Filter {
			get { return new ToolboxItemFilterAttribute[0]; }
		}
	}
}