//
// DefaultWebCertificateProvider.cs
//
// Author:
//       Alan McGovern <alan@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Net;

namespace MonoDevelop.Ide
{
	public class DefaultWebCertificateProvider : IWebCertificateProvider
	{
		Dictionary<string, bool> TrustedCertificates;
		
		public DefaultWebCertificateProvider ()
		{
			TrustedCertificates = new Dictionary<string, bool> ();
		}
		
		/// <param name="certificateFingerprint">Exact SHA-256 thumbprint of the certificate.</param>
		public bool GetIsCertificateTrusted (string uri, string certificateFingerprint)
		{
			bool value;
			
			if (!TrustedCertificates.TryGetValue (certificateFingerprint, out value)) {
				// Show an interactive confirmation when a Gtk main loop is present. In headless
				// runs the Application.Invoke delegate never executes, so bound the wait: if the
				// prompt cannot be shown within the timeout, deny instead of blocking forever.
				using (var handle = new System.Threading.ManualResetEvent (false)) {
					bool shown = false;
					Gtk.Application.Invoke ((o, args) => {
						shown = true;
						value = MessageService.AskQuestion (
							GettextCatalog.GetString ("Untrusted HTTP certificate detected"),
							GettextCatalog.GetString ("Do you want to temporarily trust this certificate in order to connect to the server at {0}?", uri),
							AlertButton.Yes, AlertButton.No) == AlertButton.Yes;
						TrustedCertificates [certificateFingerprint] = value;
						handle.Set ();
					});
					// Bounded wait: if no main loop runs, the delegate is never invoked and we deny.
					if (!handle.WaitOne (15000) && !shown) {
						value = false;
						TrustedCertificates [certificateFingerprint] = value;
					}
				}
			}

			return value;
		}
	}
}

