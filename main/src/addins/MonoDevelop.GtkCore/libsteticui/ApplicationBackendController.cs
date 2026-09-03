
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Stetic
{
	internal class ApplicationBackendController: MarshalByRefObject
	{
		bool stopping;
		ApplicationBackend backend;
		string channelId;
		IsolatedApplication app;
		
		ManualResetEvent runningEvent = new ManualResetEvent (false);
		
		public event EventHandler Stopped;
		
		public ApplicationBackendController (IsolatedApplication app, string channelId)
		{
			this.app = app;
			this.channelId = channelId;
		}
		
		public ApplicationBackend Backend {
			get { return backend; }
		}
		
		public Application Application {
			get { return app; }
		}
		
		public void StartBackend ()
		{
			runningEvent.Reset ();
			
			string asm = GetType().Assembly.Location;

			// Publish this controller over the registered remoting channel and hand the child a
			// textually-encoded URL to it, instead of serializing a binary ObjRef (a BinaryFormatter
			// deserialization surface). The child reconstructs the proxy with Activator.GetObject.
			ObjRef oref = RemotingServices.Marshal (this);
			string controllerUrl = GetMarshaledUrl (oref.URI);
		
			Process process = new Process ();
			process.StartInfo = new ProcessStartInfo ("sh", "-c \"mono --debug " + asm + "\"");
			process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.EnableRaisingEvents = true;
			process.Start ();
			process.StandardInput.WriteLine (channelId);
			process.StandardInput.WriteLine (controllerUrl);
			process.StandardInput.Flush ();
			process.Exited += OnExited;
			
			if (!runningEvent.WaitOne (10000, false))
				throw new ApplicationException ("Couldn't create a remote process.");
		}

		// Builds the connectable URL for the marshaled object on the channel registered for this
		// backend (see IsolatedApplication.RegisterRemotingChannel). TCP uses "tcp://host:port/uri",
		// Mono's Unix channel uses "unix://<socket-path>?<object-uri>".
		string GetMarshaledUrl (string objectUri)
		{
			string targetName = channelId == "tcp" ? "__internal_tcp" : "unix";
			bool isTcp = channelId == "tcp";
			foreach (IChannel ch in ChannelServices.RegisteredChannels) {
				if (ch.ChannelName != targetName)
					continue;
				ChannelDataStore store = ((IChannelReceiver)ch).ChannelData as ChannelDataStore;
				if (store == null || store.ChannelUris.Length == 0)
					continue;
				string baseUrl = store.ChannelUris[0];
				if (isTcp)
					return baseUrl + "/" + objectUri;
				// Mono's UnixChannel URL grammar is unix://<socket-path>?<object-uri>
				string path = baseUrl.Substring ("unix://".Length);
				return "unix://" + path + "?" + objectUri;
			}
			throw new InvalidOperationException ("No remoting channel is available for the backend.");
		}
		
		public void StopBackend (bool waitUntilDone)
		{
			stopping = true;
			runningEvent.Reset ();
			backend.Dispose ();
			if (waitUntilDone)
				runningEvent.WaitOne (9000, false);
		}
		
		void OnExited (object o, EventArgs args)
		{
			Gtk.Application.Invoke (OnExitedInGui);
		}
		
		void OnExitedInGui (object o, EventArgs args)
		{
			if (!stopping && Stopped != null)
				Stopped (this, EventArgs.Empty);
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}
		
		internal void Connect (ApplicationBackend backend)
		{
			this.backend = backend;
			runningEvent.Set ();
		}
		
		internal void Disconnect (ApplicationBackend backend)
		{
			this.backend = null;
			runningEvent.Set ();
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
