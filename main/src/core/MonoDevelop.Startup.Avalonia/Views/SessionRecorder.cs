using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace MonoDevelop.AvaloniaShell.Views;

/// <summary>
/// Session recorder local al shell (Fila C). Registra los ids de comando que
/// pasan por MainWindow.OnMenuCommand (el único punto de dispatch: menús,
/// hotkeys y re-dispatch de replay) y los escribe en el mismo formato XML que
/// el legacy MonoDevelop.Components.AutoTest.SessionRecord
/// (xs-event-replay-log / CommandEvent), de modo que los registros son
/// intercambiables con los grabados por el subsistema AutoTest de GTK. Los
/// eventos KeyPress/StringEvent (específicos de Gdk) no se registran en el
/// shell y se omiten al cargar, para poder reproducir archivos legacy con los
/// eventos que el shell sabe dispatchear.
/// </summary>
public sealed class SessionRecorder
{
	public sealed class CommandEvent
	{
		public string CommandId { get; set; } = "";
		public int DataItemIndex { get; set; } = -1;

		public XElement ToXML ()
		{
			return new XElement ("event", new XAttribute ("type", "CommandEvent"),
				new XElement ("commandID", CommandId),
				new XElement ("dataItemIndex", DataItemIndex));
		}

		public static CommandEvent FromXML (XElement element)
		{
			var ev = new CommandEvent ();
			foreach (var e in element.Elements ()) {
				if (e.Name == "commandID")
					ev.CommandId = e.Value;
				else if (e.Name == "dataItemIndex")
					ev.DataItemIndex = Convert.ToInt32 (e.Value);
			}
			return ev;
		}
	}

	// El legacy SessionRecord.HandleCommandManagerCommandActivated excluye los
	// comandos que controlan la propia grabación/reproducción.
	static readonly string[] ExcludedIds = {
		"MonoDevelop.Ide.Commands.ToolCommands.ToggleSessionRecorder",
		"MonoDevelop.Ide.Commands.ToolCommands.ReplaySession",
	};

	readonly List<CommandEvent> events = new ();

	public bool IsRecording { get; private set; }

	public int EventCount => events.Count;

	/// <summary>Solo accesible con la grabación detenida (legacy: la sesión
	/// debe estar pausada antes de leer los eventos).</summary>
	public IReadOnlyList<CommandEvent> Events => events;

	public void Start ()
	{
		IsRecording = true;
	}

	public void Stop ()
	{
		IsRecording = false;
	}

	public void Record (object commandId)
	{
		if (!IsRecording)
			return;
		var id = commandId as string ?? commandId.ToString ();
		if (ExcludedIds.Contains (id))
			return;
		events.Add (new CommandEvent { CommandId = id, DataItemIndex = -1 });
	}

	// Legacy SessionRecord.WriteLogToFile: raíz xs-event-replay-log, XML indentado.
	public void WriteLogToFile (string filepath)
	{
		var doc = new XDocument (new XElement ("xs-event-replay-log",
			events.Select (ev => ev.ToXML ())));
		using (XmlWriter xw = XmlWriter.Create (filepath, new XmlWriterSettings { Indent = true })) {
			doc.Save (xw);
		}
	}

	// Legacy SessionRecord.LoadFromLogFile: los tipos de evento desconocidos
	// (KeyPress/String de grabaciones GTK) se omiten en vez de fallar.
	public bool LoadFromLogFile (string filepath)
	{
		try {
			XDocument doc = XDocument.Load (filepath);
			var root = doc.Element ("xs-event-replay-log");
			if (root is null)
				return false;
			foreach (XElement element in root.Elements ()) {
				if (element is null)
					continue;
				var evType = element.Attribute ("type")?.Value;
				if (evType != "CommandEvent")
					continue;
				events.Add (CommandEvent.FromXML (element));
			}
			return true;
		} catch (Exception) {
			return false;
		}
	}

	public void Clear ()
	{
		events.Clear ();
	}
}
