//
// ProcessHostController.cs
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
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Timer = System.Timers.Timer;
using MonoDevelop.Core;
using MonoDevelop.Core.Logging;
using Mono.Addins;
using Process = System.Diagnostics.Process;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// Parent side of the external process host. Spawns the mdhost child process and talks to it
	/// over the loopback message transport (RemoteProcessConnection), replacing the former .NET
	/// remoting channel/ObjRef handshake. Objects created in mdhost are addressed by integer ID and
	/// exposed through <see cref="RemoteProcessObjectHandle"/>.
	/// </summary>
	[System.ComponentModel.DesignerCategory ("Code")]
	internal class ProcessHostController: IProcessHostController
	{
		public const string ProcessHostTargetId = "ProcessHost";
		public const string LoadAddinsMessage = "LoadAddins";
		public const string CreateInstanceMessage = "CreateInstance";
		public const string DisposeObjectMessage = "DisposeObject";
		public const string RegisterHostMessage = "RegisterHost";
		public const string LogMessage = "Log";
		public const string ConfigFileEnvVar = "MONODEVELOP_MDHOST_CONFIG";

		int references;
		uint stopDelay;
		DateTime lastReleaseTime;
		bool starting;
		bool stopping;
		Timer timer;
		string id;
		IExecutionHandler executionHandlerFactory;
		int shutdownTimeout = 2000;

		RemoteProcessConnection connection;
		ManualResetEvent runningEvent = new ManualResetEvent (false);
		ManualResetEvent exitRequestEvent = new ManualResetEvent (false);
		ManualResetEvent exitedEvent = new ManualResetEvent (false);
		
		readonly object remoteObjectsLock = new object ();
		List<RemoteProcessObjectHandle> remoteObjects = new List<RemoteProcessObjectHandle> ();

		public ProcessHostController (string id, uint stopDelay, IExecutionHandler executionHandlerFactory)
		{
			if (string.IsNullOrEmpty (id))
				id = "?";
			this.id = id;
			this.stopDelay = stopDelay;
			this.executionHandlerFactory = executionHandlerFactory;
			timer = new Timer ();
			timer.AutoReset = false;
			timer.Elapsed += new System.Timers.ElapsedEventHandler (WaitTimeout);
		}

		void Start (IList<string> userAssemblyPaths = null, OperationConsole console = null)
		{
			lock (this) {
				if (starting)
					return;
				starting = true;
				exitRequestEvent.Reset ();

				string tmpFile = null;
				try {
					string location = Path.Combine (Path.GetDirectoryName (typeof(ProcessHostController).Assembly.Location), "mdhost.exe");

					// Pass the startup configuration to mdhost through a temp file (its path is
					// forwarded via an environment variable since the transport controls the child args).
					tmpFile = Path.GetTempFileName ();
					StreamWriter sw = new StreamWriter (tmpFile);
					sw.WriteLine (id);
					sw.WriteLine (Process.GetCurrentProcess ().Id);
					sw.WriteLine (Runtime.SystemAssemblyService.CurrentRuntime.RuntimeId);

					// Explicitly load Mono.Addins since the target runtime may not have it installed
					sw.WriteLine (1);
					sw.WriteLine (typeof(AddinManager).Assembly.Location);
					sw.Close ();

					var envVars = new Dictionary<string, string> {
						[ConfigFileEnvVar] = tmpFile
					};

					operationConsole = console ?? new ProcessHostConsole ();
					connection = new RemoteProcessConnection (location, AppDomain.CurrentDomain.BaseDirectory, envVars,
						executionHandlerFactory ?? Runtime.ProcessService.DefaultExecutionHandler, operationConsole);
					connection.StatusChanged += OnConnectionStatusChanged;
					connection.AddListener (this);

					Counters.ExternalHostProcesses.Inc (1);
					var c = connection.Connect ();
					c.ContinueWith (t => {
						if (t.IsFaulted || t.IsCanceled) {
							LoggingService.LogError ("Could not start mdhost process", t.Exception);
							ProcessExited ();
						}
					});
				} catch (Exception ex) {
					if (tmpFile != null) {
						try {
							File.Delete (tmpFile);
						} catch {
						}
					}
					LoggingService.LogError (ex.ToString ());
					throw;
				}
			}
		}

		OperationConsole operationConsole;

		void OnConnectionStatusChanged (object s, EventArgs args)
		{
			var st = connection.Status;
			if (st == ConnectionStatus.Disconnected || st == ConnectionStatus.ConnectionFailed)
				ProcessExited ();
		}

		void ProcessExited ()
		{
			lock (this) {

				Counters.ExternalHostProcesses.Dec (1);

				lock (remoteObjectsLock) {
					remoteObjects.Clear ();
				}
				
				exitedEvent.Set ();
				
				// If the remote process crashes, a thread may be left hung in WaitForExit. This will awaken it.
				exitRequestEvent.Set ();
				
				runningEvent.Reset ();
				references = 0;
			}
		}

		public object CreateInstance (Type type, string[] addins, IList<string> userAssemblyPaths = null, OperationConsole console = null)
		{
			return CreateInstance (type.Assembly.Location, type.FullName, addins, userAssemblyPaths, console);
		}

		public object CreateInstance (string assemblyPath, string typeName, string[] addins, IList<string> userAssemblyPaths = null, OperationConsole console = null)
		{
			lock (this) {
				references++;
				if (connection == null || connection.Status != ConnectionStatus.Connected)
					Start (userAssemblyPaths, console);
			}

			if (!runningEvent.WaitOne (15000, false)) {
				lock (this) references--;
				throw new ApplicationException ("Couldn't create a remote process.");
			}

			try {
				// Before creating the instance, load the add-ins on which it depends
				if (addins != null && addins.Length > 0)
					connection.SendMessage (new BinaryMessage (LoadAddinsMessage, ProcessHostTargetId).AddArgument ("Addins", addins)).Wait ();

				var req = new BinaryMessage (CreateInstanceMessage, ProcessHostTargetId)
					.AddArgument ("AssemblyPath", assemblyPath)
					.AddArgument ("TypeName", typeName);
				var resp = connection.SendMessage (req).Result;
				if (resp.Name == "Error")
					throw new RemoteProcessException (resp.GetArgument<string> ("Message"));
				int instanceId = resp.GetArgument<int> ("InstanceId");

				var handle = new RemoteProcessObjectHandle (this, instanceId);
				lock (remoteObjectsLock) {
					remoteObjects.Add (handle);
				}
				Counters.ExternalObjects.Inc (1);
				return handle;
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		[MessageHandler (RegisterHostMessage)]
		void HandleRegisterHost (BinaryMessage msg)
		{
			lock (this) {
				runningEvent.Set ();
				starting = false;
			}
		}

		[MessageHandler (LogMessage)]
		void HandleLog (BinaryMessage msg)
		{
			LoggingService.Log (msg.GetArgument<LogLevel> ("Level"), msg.GetArgument<string> ("Message"));
		}

		internal void PostObjectDispose (RemoteProcessObjectHandle obj)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					connection?.PostMessage (new BinaryMessage (DisposeObjectMessage, ProcessHostTargetId).AddArgument ("InstanceId", obj.InstanceId));
				} catch {
					// Ignore
				}
				ReleaseInstance (obj);
			});
		}

		internal void PostObjectShutdown ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					connection?.Disconnect ().Ignore ();
				} catch {
					// Ignore
				}
			});
		}

		internal void ReleaseInstance (RemoteProcessObjectHandle obj)
		{
			ReleaseInstance (obj, 2000);
		}
		
		internal void ReleaseInstance (RemoteProcessObjectHandle proc, int shutdownTimeout)
		{
			Counters.ExternalObjects.Dec (1);
			if (connection == null)
				return;
			
			lock (this) {
				lock (remoteObjectsLock) {
					for (int n=0; n<remoteObjects.Count; n++) {
						if (ReferenceEquals (remoteObjects [n], proc)) {
							remoteObjects.RemoveAt (n);
							break;
						}
					}
				}
				
				references--;
				if (references == 0) {
					lastReleaseTime = DateTime.Now;
					if (!stopping) {
						stopping = true;
						this.shutdownTimeout = shutdownTimeout;
						if (stopDelay == 0) {
							// Always stop asyncrhonously, so the remote object
							// has time to end the dispose call.
							timer.Interval = 1000;
							timer.Enabled = true;
						} else {
							timer.Interval = stopDelay;
							timer.Enabled = true;
						}
					}
				}
			}
		}
		
		void WaitTimeout (object sender, System.Timers.ElapsedEventArgs args)
		{
			try {
				RemoteProcessConnection oldConnection;
				
				lock (this) {
					if (references > 0) {
						stopping = false;
						return;
					}
	
					uint waited = (uint) (DateTime.Now - lastReleaseTime).TotalMilliseconds;
					if (waited < stopDelay) {
						timer.Interval = stopDelay - waited;
						timer.Enabled = true;
						return;
					}
				
					runningEvent.Reset ();
					exitedEvent.Reset ();
					exitRequestEvent.Set ();
					oldConnection = connection;
					connection = null;
					stopping = false;
				}
	
				if (!exitedEvent.WaitOne (shutdownTimeout, false)) {
					try {
						oldConnection?.Disconnect ().Ignore ();
					} catch {
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		public void RegisterHost (IProcessHost processHost)
		{
			// No longer used: mdhost advertises itself over the message bus (HandleRegisterHost).
		}
		
		public void WaitForExit ()
		{
			exitRequestEvent.WaitOne ();
		}
		
		public ILogger GetLogger ()
		{
			// Logging from the child is forwarded over the message bus (HandleLog), so there is no
			// marshaled remote logger anymore.
			return LoggingService.RemoteLogger;
		}
	}

	/// <summary>
	/// Client-side handle for an object created in the mdhost process. It exposes the base host
	/// contract (Dispose/Shutdown) as messages over the loopback bus; type-specific member
	/// invocation is deferred to the "Interfaz" phase.
	/// </summary>
	internal sealed class RemoteProcessObjectHandle : IDisposable
	{
		readonly ProcessHostController controller;
		public int InstanceId { get; }

		internal RemoteProcessObjectHandle (ProcessHostController controller, int instanceId)
		{
			this.controller = controller;
			InstanceId = instanceId;
		}

		public void Dispose ()
		{
			controller?.PostObjectDispose (this);
		}

		public void Shutdown ()
		{
			controller?.PostObjectShutdown ();
		}
	}

	class ProcessHostConsole: OperationConsole
	{
		public override TextReader In {
			get { return Console.In; }
		}
		
		public override TextWriter Out {
			get { return Console.Out; }
		}
		
		public override TextWriter Error {
			get { return Console.Error; }
		}
		
		public override TextWriter Log {
			get { return Out; }
		}
	}
}