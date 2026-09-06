// 
// Mono.Unix.Catalog.cs
// 
// Author:
//   MonoDevelop contributors
//
// Copyright (C) 2026
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

namespace Mono.Unix
{
	// Internal replacement for Mono.Posix' Mono.Unix.Catalog. Preserves the exact type and method
	// surface so existing call sites (e.g. the generated Gui/* widgets that use
	// global::Mono.Unix.Catalog.GetString) keep compiling without the legacy Mono.Posix assembly.
	// The actual gettext .mo reading lives in MonoDevelop.Core.Catalog; this facade just forwards.
	public static class Catalog
	{
		public static void Init (string domain, string path) => MonoDevelop.Core.Catalog.Init (domain, path);

		public static string GetString (string phrase) => MonoDevelop.Core.Catalog.GetString (phrase);

		public static string GetPluralString (string singular, string plural, int number)
			=> MonoDevelop.Core.Catalog.GetPluralString (singular, plural, number);
	}
}