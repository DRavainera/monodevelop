//
// RemoteProcessObject.cs
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

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// Base class for objects that may be hosted in an external process (mdhost) and reached
	/// through the message-based execution host RPC. It is a plain class: the remoting
	/// transparent proxy (MarshalByRefObject) was removed in favor of the loopback message
	/// transport used by ProcessHostController/mdhost. Subclasses keep compiling unchanged.
	/// </summary>
	public class RemoteProcessObject: IDisposable
	{
		/// <summary>
		/// Disposes the object, and kills the remote process if there are no more remote objects running on it
		/// </summary>
		public virtual void Dispose ()
		{
			// Disposal is now coordinated by the message-based host (the handler that receives
			// ObjectDispose calls DisposeObject on the remote instance). No remoting disconnect here.
		}
		
		/// <summary>
		/// Shutdowns the remote process that is running this instance.
		/// </summary>
		/// <remarks>
		/// This method can only be used if the remote process is not shared with other objects.
		/// In the message-based host it is handled as an ObjectShutdown message.
		/// </remarks>
		public void Shutdown ()
		{
			// Do nothing. This method is intercepted and executed by MonoDevelop.
		}
	}
}
