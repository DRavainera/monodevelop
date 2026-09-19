//
// IntrinsicFunctions.Extensions.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
namespace Microsoft.Build.Evaluation
{
	internal static partial class IntrinsicFunctions
	{
		// Similar to https://github.com/microsoft/msbuild/pull/4731
		// This avoids creating a string copy for the purpose of evaluation in metadata items.
		internal static string Copy (string value) => value;

		// Target framework intrinsics (ported from MSBuild's NuGetFrameworkWrapper-backed
		// implementation; see TargetFrameworkIntrinsics). The modern .NET SDK needs these
		// during evaluation to derive TargetFrameworkIdentifier/TargetFrameworkVersion.
		internal static string GetTargetFrameworkIdentifier (string tfm) => TargetFrameworkIntrinsics.GetTargetFrameworkIdentifier (tfm);

		internal static string GetTargetFrameworkVersion (string tfm, int versionPartCount = 2) => TargetFrameworkIntrinsics.GetTargetFrameworkVersion (tfm, versionPartCount);

		internal static string GetTargetPlatformIdentifier (string tfm) => TargetFrameworkIntrinsics.GetTargetPlatformIdentifier (tfm);

		internal static string GetTargetPlatformVersion (string tfm, int versionPartCount = 2) => TargetFrameworkIntrinsics.GetTargetPlatformVersion (tfm, versionPartCount);
	}
}
