//
// From NuGet src/Core
//
// Copyright (c) 2010-2014 Outercurve Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net;
using System.Globalization;
using System.Text;

namespace MonoDevelop.Core.Web
{
	static class STSAuthHelper
	{
		/// <summary>
		/// Response header that specifies the WSTrust13 Windows Transport endpoint.
		/// </summary>
		/// <remarks>
		/// TODO: Is there a way to discover this \ negotiate this endpoint?
		/// </remarks>
		private const string STSEndPointHeader = "X-MonoDevelop-STS-EndPoint";

		/// <summary>
		/// Response header that specifies the realm to authenticate for. In most cases this would be the gallery we are going up against.
		/// </summary>
		private const string STSRealmHeader = "X-MonoDevelop-STS-Realm";

		/// <summary>
		/// Request header that contains the SAML token.
		/// </summary>
		private const string STSTokenHeader = "X-MonoDevelop-STS-Token";

		/// <summary>
		/// Adds the SAML token as a header to the request if it is already cached for this host.
		/// </summary>
		public static void PrepareSTSRequest(WebRequest request)
		{
			string cacheKey = GetCacheKey(request.RequestUri);
			string token;
			if (MemoryCache.Instance.TryGetValue(cacheKey, out token))
			{
				request.Headers[STSTokenHeader] = EncodeHeader(token);
			}
		}

		/// <summary>
		/// Attempts to retrieve a SAML token if the response indicates that server requires STS-based auth.
		/// </summary>
		/// <param name="requestUri">The feed URI we were connecting to.</param>
		/// <param name="response">The 401 response we receieved from the server.</param>
		/// <returns>True if we were able to successfully retrieve a SAML token from the STS specified in the response headers.</returns>
		public static bool TryRetrieveSTSToken(Uri requestUri, IHttpWebResponse response)
		{
			if (response.StatusCode != HttpStatusCode.Unauthorized)
			{
				// We only care to do STS auth if the server returned a 401
				return false;
			}

			string endPoint = GetSTSEndPoint(response);
			string realm = response.Headers[STSRealmHeader];
			if (String.IsNullOrEmpty(endPoint) || String.IsNullOrEmpty(realm))
			{
				// The server does not conform to our STS-auth requirements.
				return false;
			}

			string cacheKey = GetCacheKey(requestUri);

			// TODO: We need to figure out a way to cache the token for the duration of the token's validity (which is available as part of it's result).
			MemoryCache.Instance.GetOrAdd(cacheKey,
				() => GetSTSToken(requestUri, endPoint, realm),
				TimeSpan.FromMinutes(30),
				absoluteExpiration: true);
			return true;
		}

		private static string GetSTSToken(Uri requestUri, string endPoint, string appliesTo)
		{
			var typeProvider = WIFTypeProvider.GetWIFTypes();
			if (typeProvider == null)
			{
				throw new InvalidOperationException (
					String.Format (
						CultureInfo.CurrentCulture,
						"Connection to feed '{0}' requires the Windows Identity Foundation runtime to be installed.",
						requestUri
				)
				);
			}

			// WSTrust/WCF (System.ServiceModel) is not supported on .NET 8. If a WIF type provider is
			// ever discovered at runtime, obtain the STS token through reflection instead of the
			// former compile-time System.ServiceModel binding/factory/channel types.
			throw new NotSupportedException (
				String.Format (
					CultureInfo.CurrentCulture,
					"STS (WSTrust/WCF) authentication to feed '{0}' is not supported on .NET.",
					requestUri
			)
			);
		}

		private static string GetSTSEndPoint(IHttpWebResponse response)
		{
			return response.Headers[STSEndPointHeader].SafeTrim();
		}

		private static string GetCacheKey(Uri requestUri)
		{
			return STSTokenHeader + "|" + requestUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);
		}

		private static string EncodeHeader(string token)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
		}
	}
}
