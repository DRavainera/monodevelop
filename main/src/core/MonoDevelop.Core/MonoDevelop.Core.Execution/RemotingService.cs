// 
// RemotingServices.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// The execution host no longer uses the .NET remoting channels, ObjRef marshaling or the
	/// binary formatter sink that this class once set up. Cross-process communication is served by
	/// the loopback message transport (RemoteProcessConnection/RemoteProcessServer/BinaryMessage).
	/// This class keeps two inert entry points for the still-deferred automated testing (AutoTest,
	/// reworked over the message bus in the "Interfaz" phase) and a no-op Dispose for ProcessService.
	/// </summary>
	public static class RemotingService
	{
		/// <summary>
		/// Historically registered the remoting IPC/TCP channels. The message transport creates its
		/// own loopback listener per connection, so there is no shared remoting channel to register
		/// anymore. Kept as a no-op for the deferred AutoTest call sites.
		/// </summary>
		public static void RegisterRemotingChannel ()
		{
			// No-op. The message transport (RemoteProcessConnection/RemoteProcessServer) sets up
			// its own loopback TCP listener; no remoting channel is registered.
		}

		/// <summary>
		/// Historically returned a textual URL for an object published over a remoting channel.
		/// Remoting channels were removed; the AutoTest handshake will be reworked over the message
		/// bus in the "Interfaz" phase. Anything still calling this has not been migrated yet.
		/// </summary>
		public static string GetMarshaledUrl (string objectUri)
		{
			throw new NotSupportedException ("Remoting channels were removed. Migrate this call site to the message-based bus (currently deferred to the 'Interfaz' phase): " + objectUri);
		}

		/// <summary>
		/// Historically cleaned up the remoting IPC socket file. No remoting channel file is created
		/// anymore, so this is a no-op kept for ProcessService.Dispose.
		/// </summary>
		internal static void Dispose ()
		{
		}
	}
}