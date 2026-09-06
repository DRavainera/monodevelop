//
// mdhost.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Logging;
using MonoDevelop.Core.Execution;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Mono.Addins;

public class MonoDevelopProcessHost
{
	static string ParentRuntime;
	static string configFileEnvVarName = "MONODEVELOP_MDHOST_CONFIG";

	public static int Main (string[] args)
	{
		// args[0] is the loopback port and args[1] the debug flag, both provided by the
		// RemoteProcessServer.Connect contract. The startup configuration (id, parent pid,
		// runtime, assembly paths) is forwarded through a temp file whose path comes in an
		// environment variable set by ProcessHostController.
		var configPath = Environment.GetEnvironmentVariable (configFileEnvVarName);

		try {
			string id = "?";
			int pidToWatch = 0;
			if (configPath != null && File.Exists (configPath)) {
				using (var input = new StreamReader (configPath)) {
					id = input.ReadLine ();
					pidToWatch = int.Parse (input.ReadLine ());
					ParentRuntime = input.ReadLine ();
					int numAsm = int.Parse (input.ReadLine ());
					while (numAsm-- > 0) {
						Assembly.LoadFrom (input.ReadLine ());
					}
				}
				try {
					File.Delete (configPath);
				} catch {
				}
			}

			if (pidToWatch > 0)
				WatchParentProcess (pidToWatch);

			RemoteProcessServer server = new RemoteProcessServer ();
			LoggingService.AddLogger (new LocalLogger (server, id));
			server.AddListener (new ProcessHost (server));

			// Connect to the parent. On success advertise our ProcessHost over the message bus so
			// the parent marks the host as running.
			server.Connect (args, new ProcessListener ());
			server.AddListener (new ProcessHost (server));
			server.SendMessage (new BinaryMessage (ProcessHostController.RegisterHostMessage));

			// Keep the process alive servicing messages. The connection is torn down by the
			// parent (Dispose/Shutdown) or when the watched parent process dies.
			var done = new ManualResetEvent (false);
			done.WaitOne ();
		} catch (Exception ex) {
			Console.WriteLine (ex);
		}

		return 0;
	}

	static void WatchParentProcess (int pid)
	{
		Thread t = new Thread (delegate () {
			while (true) {
				try {
					// Throws exception if process is not running.
					// When watching a .NET process from Mono, GetProcessById may
					// return the process with HasExited=true
					var p = System.Diagnostics.Process.GetProcessById (pid);
					if (p.HasExited)
						break;
				} catch {
					break;
				}
				Thread.Sleep (1000);
			}
			Environment.Exit (1);
		});
		t.Name = "Parent process watcher";
		t.IsBackground = true;
		t.Start ();
	}
}

class ProcessListener: MessageListener
{
}

class LocalLogger: ILogger
{
	RemoteProcessServer server;
	string id;

	public LocalLogger (RemoteProcessServer server, string id)
	{
		this.server = server;
		this.id = id;
	}

	#region ILogger implementation
	public void Log (LogLevel level, string message)
	{
		try {
			var msg = new BinaryMessage (ProcessHostController.LogMessage)
				.AddArgument ("Level", level)
				.AddArgument ("Message", "[" + id + "] " + message);
			msg.OneWay = true;
			server.SendMessage (msg);
		} catch {
			// Ignore
		}
	}

	public EnabledLoggingLevel EnabledLevel {
		get {
			return EnabledLoggingLevel.All;
		}
	}

	public string Name {
		get {
			return "Local Logger";
		}
	}
	#endregion
}

public class ProcessHost: MessageListener, IDisposable
{
	RemoteProcessServer server;
	readonly object objectsLock = new object ();
	Dictionary<int, IDisposable> instances = new Dictionary<int, IDisposable> ();
	int nextId;

	public override string TargetId {
		get {
			return ProcessHostController.ProcessHostTargetId;
		}
	}

	public ProcessHost (RemoteProcessServer server)
	{
		this.server = server;
	}

	[MessageHandler (ProcessHostController.CreateInstanceMessage)]
	BinaryMessage CreateInstance (BinaryMessage msg)
	{
		string assemblyPath = msg.GetArgument<string> ("AssemblyPath");
		string typeName = msg.GetArgument<string> ("TypeName");

		Assembly asm = Assembly.LoadFrom (assemblyPath);
		Type t = asm.GetType (typeName);
		if (t == null)
			throw new InvalidOperationException ("Type not found: " + typeName);

		var instance = (IDisposable)Activator.CreateInstance (t);
		int id = Interlocked.Increment (ref nextId);
		lock (objectsLock) {
			instances [id] = instance;
		}
		return msg.CreateResponse ().AddArgument ("InstanceId", id);
	}

	[MessageHandler (ProcessHostController.LoadAddinsMessage)]
	void LoadAddins (BinaryMessage msg)
	{
		Runtime.Initialize (false);
		var addins = msg.GetArgument<string[]> ("Addins");
		if (addins == null)
			return;
		foreach (string ad in addins)
			AddinManager.LoadAddin (null, ad);
	}

	[MessageHandler (ProcessHostController.DisposeObjectMessage)]
	void DisposeObject (BinaryMessage msg)
	{
		int instanceId = msg.GetArgument<int> ("InstanceId");
		IDisposable inst;
		lock (objectsLock) {
			if (!instances.TryGetValue (instanceId, out inst))
				return;
			instances.Remove (instanceId);
		}
		try {
			inst.Dispose ();
		} catch {
			// Ignore
		}
	}

	public void Dispose ()
	{
	}
}