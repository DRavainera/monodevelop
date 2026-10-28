/*
 * ProjectOptionsPanelWidget.cs.
 *
 * Author:
 *   Rolf Bjarne Kvinge <RKvinge@novell.com>
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
 
using MonoDevelop.Core;

namespace MonoDevelop.VBNetBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ProjectOptionsPanelWidget : Gtk.Bin
	{
		VBProject project;

		public ProjectOptionsPanelWidget (MonoDevelop.Projects.Project project)
		{
			Gtk.ListStore store;
			Gtk.CellRendererText cr;

			this.Build();

			this.project = (VBProject) project;

			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Executable"));
			store.AppendValues (GettextCatalog.GetString ("Library"));
			store.AppendValues (GettextCatalog.GetString ("Executable with GUI"));
			store.AppendValues (GettextCatalog.GetString ("Module")); 
			compileTargetCombo.Model = store;
			compileTargetCombo.PackStart (cr, true);
			compileTargetCombo.AddAttribute (cr, "text", 0);
			compileTargetCombo.Active = (int) this.project.CompileTarget;
			compileTargetCombo.Changed += delegate(object sender, EventArgs e) {
				entryMainClass.Sensitive = compileTargetCombo.Active != (int) CompileTarget.Library	&& compileTargetCombo.Active != (int) CompileTarget.Module;
			};

			txtMyType.AppendText ("WindowsForms");
			txtMyType.AppendText ("Windows");
			txtMyType.AppendText ("Console");
			switch (this.project.MyType) {
			case "WindowsForms":
				txtMyType.Active = 0;
				break;
			case "Windows":
				txtMyType.Active = 1;
				break;
			case "Console":
				txtMyType.Active = 2;
				break;
			case null:
			case "":
				break;
			default:
				txtMyType.AppendText (this.project.MyType);
				txtMyType.Active = 3;
				break;
			}

			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Binary");
			store.AppendValues ("Text");
			cmbOptionCompare.Model = store;
			cmbOptionCompare.PackStart (cr, true);
			cmbOptionCompare.AddAttribute (cr, "text", 0);
			cmbOptionCompare.Active = this.project.BinaryOptionCompare ? 0 : 1;
				
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionExplicit.Model = store;
			cmbOptionExplicit.PackStart (cr, true);
			cmbOptionExplicit.AddAttribute (cr, "text", 0);
			cmbOptionExplicit.Active = this.project.OptionExplicit ? 0 : 1;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionInfer.Model = store;
			cmbOptionInfer.PackStart (cr, true);
			cmbOptionInfer.AddAttribute (cr, "text", 0);
			cmbOptionInfer.Active = this.project.OptionInfer ? 0 : 1;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionStrict.Model = store;
			cmbOptionStrict.PackStart (cr, true);
			cmbOptionStrict.AddAttribute (cr, "text", 0);
			cmbOptionStrict.Active = this.project.OptionStrict ? 0 : 1;

			// Codepage
			string foundEncoding = null;
			string currentCodepage = this.project.CodePage;
			foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
				if (e.CodePage == -1)
					continue;
				if (e.Id == currentCodepage)
					foundEncoding = e.Id;
				cmbCodePage.AppendText (e.Id);
			}
			if (foundEncoding != null)
				((Gtk.Entry)cmbCodePage.Child).Text = foundEncoding;
			else if (!string.IsNullOrEmpty (currentCodepage))
				((Gtk.Entry)cmbCodePage.Child).Text = currentCodepage;
			
			((Gtk.Entry)entryMainClass.Child).Text = this.project.StartupObject;
			iconEntry.Path = this.project.ApplicationIcon;
		}
		
		static string ComboText (global::Gtk.ComboBox combo)
		{
			global::Gtk.ITreeModel model = combo.Model;
			if (model == null || combo.Active < 0)
				return null;
			global::Gtk.TreeIter iter;
			if (!model.GetIter (out iter, new global::Gtk.TreePath (combo.Active)))
				return null;
			return model.GetValue (iter, 0) as string;
		}
		
		public void StorePanelContents ()
		{
			project.BinaryOptionCompare = ComboText (cmbOptionCompare) == "Binary";
			project.OptionExplicit = ComboText (cmbOptionExplicit) == "On";
			project.OptionInfer = ComboText (cmbOptionInfer) == "On";
			project.OptionStrict = ComboText (cmbOptionStrict) == "On";
			project.MyType = txtMyType.ActiveText;
			project.StartupObject = entryMainClass.ActiveText;
			project.CodePage = ((Gtk.Entry)cmbCodePage.Child).Text;
			project.ApplicationIcon = iconEntry.Path;
			project.CompileTarget = (CompileTarget) compileTargetCombo.Active;
		}
	}
}
