using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Services;

/// <summary>One node of the Help pad documentation tree. Port of the data
/// source of the legacy <c>MonoDevelop.Ide.Gui.Pads.HelpTree</c>: the legacy
/// reads a Monodoc tree (<c>monodoc.xml</c> index + per-node files) through
/// <c>Monodoc.RootTree</c>, which is a compatibility stub in the net10 build
/// (see <c>MonoRoslynCompat/CompatStubs.cs</c>), so the index XML is parsed
/// directly here and the same documentation sets load without the Monodoc
/// assembly.</summary>
public sealed class HelpNode
{
	public HelpNode (string caption)
	{
		Caption = caption;
	}

	public string Caption { get; }

	/// <summary>Legacy <c>Node.PublicUrl</c>: the page of this node. Absolute
	/// (http/https/file) or relative to <see cref="SourceDir"/>.</summary>
	public string? Url { get; internal set; }

	public string? Summary { get; internal set; }

	/// <summary>Directory holding this node's index file; the base for
	/// relative <see cref="Url"/> and <see cref="ChildrenFile"/>.</summary>
	public string? SourceDir { get; internal set; }

	/// <summary>Relative path of the file holding this node's children. Non-
	/// null until the children are lazily parsed (<see cref="LoadChildren"/>),
	/// like the legacy pad populating rows on <c>RowExpanded</c>.</summary>
	public string? ChildrenFile { get; internal set; }

	public List<HelpNode> Children { get; } = new ();

	public bool IsChildrenLoaded { get; internal set; }

	public bool HasChildData => ChildrenFile is not null || Children.Count > 0;
}

/// <summary>Loads Monodoc documentation trees for the Help pad: the
/// <c>monodoc.xml</c> index of each source directory is merged under one
/// "Mono Documentation" root (legacy <c>HelpTree</c> behavior), children of
/// <c>&lt;node file="..."&gt;</c> entries are parsed lazily on expansion, and
/// node URLs are resolved against the index directory.</summary>
public static class HelpTreeService
{
	public const string RootCaption = "Mono Documentation";

	public sealed class HelpTreeResult
	{
		/// <summary>null when no source directory holds a <c>monodoc.xml</c>.</summary>
		public HelpNode? Root { get; set; }
		public List<string> Searched { get; } = new ();
		public List<string> Errors { get; } = new ();
	}

	/// <summary>Default source directories: the application directory (legacy
	/// <c>RootTree.LoadTree</c> reads <c>monodoc.xml</c> next to the
	/// executable) and the user drop-in directory (shell addition so a
	/// documentation set can be installed without touching the app).</summary>
	public static List<string> DefaultSourceDirs ()
	{
		var dirs = new List<string> { AppContext.BaseDirectory };
		try {
			dirs.Add (UserProfile.Current.UserDataRoot.Combine ("monodoc").ToString ());
		} catch {
			// Profile not initialized (unit tests): the app directory only.
		}
		return dirs;
	}

	public static HelpTreeResult LoadTree (IEnumerable<string> sourceDirs)
	{
		var result = new HelpTreeResult ();
		HelpNode? root = null;
		foreach (var dir in sourceDirs) {
			result.Searched.Add (dir);
			var index = Path.Combine (dir, "monodoc.xml");
			if (!File.Exists (index))
				continue;
			HelpNode set;
			try {
				set = ParseIndexFile (index);
			} catch (Exception ex) {
				result.Errors.Add (index + ": " + ex.Message);
				continue;
			}
			root ??= new HelpNode (RootCaption) { SourceDir = Path.GetDirectoryName (index) };
			foreach (var n in set.Children)
				root.Children.Add (n);
		}
		if (root is not null)
			root.IsChildrenLoaded = true;
		result.Root = root;
		return result;
	}

	/// <summary>Lazily parses a node's children file (called by the pad on
	/// row expansion, like the legacy <c>RowExpanded</c>). Returns true when
	/// the node ends up with data available (loaded children or a file that
	/// held none).</summary>
	public static bool LoadChildren (HelpNode node)
	{
		if (node.ChildrenFile is null || node.IsChildrenLoaded || node.SourceDir is null)
			return node.ChildrenFile is null;
		var file = Path.Combine (node.SourceDir, node.ChildrenFile);
		node.IsChildrenLoaded = true;
		if (!File.Exists (file))
			return false;
		try {
			var parsed = ParseIndexFile (file);
			// A children file is <root><nodes>...</nodes></root>: the node's
			// children are the contents of <nodes>; a file holding a single
			// bare node contributes that node itself.
			if (parsed.Children.Count > 0)
				node.Children.AddRange (parsed.Children);
			else
				node.Children.Add (parsed);
		} catch (Exception ex) {
			Console.WriteLine ("[help] " + file + ": " + ex.Message);
			return false;
		}
		node.ChildrenFile = null;
		return node.Children.Count > 0;
	}

	/// <summary>Resolves a node's URL: absolute URLs as-is, relative ones
	/// against the node's index directory (legacy Monodoc viewer semantics).</summary>
	public static string? ResolveUrl (HelpNode node)
	{
		if (node.Url is null)
			return null;
		if (Uri.TryCreate (node.Url, UriKind.Absolute, out _))
			return node.Url;
		return node.SourceDir is null ? node.Url : Path.GetFullPath (Path.Combine (node.SourceDir, node.Url));
	}

	/// <summary>Parses a <c>monodoc.xml</c> index (or a children file with the
	/// same shape): <c>&lt;root&gt;&lt;name&gt;…&lt;/name&gt;&lt;summary&gt;…
	/// &lt;/summary&gt;&lt;nodes&gt;&lt;node …/&gt;…&lt;/nodes&gt;&lt;/root&gt;.</summary>
	public static HelpNode ParseIndexFile (string path)
	{
		var doc = new XmlDocument ();
		doc.Load (path);
		var rootEl = doc.DocumentElement;
		if (rootEl is null)
			throw new InvalidOperationException ("empty document");
		var node = new HelpNode (ElementText (rootEl, "name") ?? Path.GetFileNameWithoutExtension (path)) {
			Summary = ElementText (rootEl, "summary"),
			SourceDir = Path.GetDirectoryName (Path.GetFullPath (path)),
		};
		foreach (var child in ChildNodeElements (rootEl))
			node.Children.Add (MakeNode (child, node.SourceDir));
		return node;
	}

	static HelpNode MakeNode (XmlElement element, string? sourceDir)
	{
		var url = ElementText (element, "url") ?? element.GetAttribute ("url");
		var file = element.GetAttribute ("file");
		var node = new HelpNode (ElementText (element, "name") ?? element.GetAttribute ("name") ?? "") {
			Url = string.IsNullOrEmpty (url) ? null : url,
			Summary = ElementText (element, "summary"),
			SourceDir = sourceDir,
			ChildrenFile = string.IsNullOrEmpty (file) ? null : file,
		};
		if (node.ChildrenFile is null) {
			foreach (var child in ChildNodeElements (element))
				node.Children.Add (MakeNode (child, sourceDir));
			node.IsChildrenLoaded = true;
		}
		return node;
	}

	/// <summary>The <c>&lt;node&gt;</c> children of an element: Monodoc nests
	/// them under a <c>&lt;nodes&gt;</c> wrapper; direct children are accepted
	/// too for tolerance.</summary>
	static IEnumerable<XmlElement> ChildNodeElements (XmlElement element)
	{
		var nodes = element["nodes"];
		var parent = nodes is not null ? (XmlElement)nodes : element;
		foreach (XmlNode child in parent.ChildNodes) {
			if (child is XmlElement el && el.LocalName == "node")
				yield return el;
		}
	}

	static string? ElementText (XmlElement element, string localName)
	{
		var el = element[localName];
		return el is null ? null : el.InnerText;
	}
}
