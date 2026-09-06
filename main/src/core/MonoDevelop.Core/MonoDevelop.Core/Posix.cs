// 
// Posix.cs
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

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoDevelop.Core
{
	// Internal minimal POSIX shim used to replace the legacy Mono.Posix / Mono.Unix.Native
	// dependency in the core. Exposes the same operation names with a P/Invoke backed
	// implementation so the call sites remain unchanged in shape.
	public static class Posix
	{
		const string Libc = "libc";

		// errno values (Linux/glibc ABI). Referenced by the call sites that map errors.
		public const int EACCES = 13;
		public const int EPERM = 1;
		public const int EINVAL = 22;
		public const int ENOTDIR = 20;
		public const int ENOENT = 2;
		public const int ENAMETOOLONG = 36;
		public const int EXDEV = 18;
		public const int EEXIST = 17;

		// open() flags
		public const int O_WRONLY = 1;
		public const int O_CREAT = 0x40;
		public const int O_TRUNC = 0x200;

		// open() mode bits
		public const int S_IFREG = 0x8000;
		public const int S_IRUSR = 0x100;
		public const int S_IWUSR = 0x80;
		public const int S_IRGRP = 0x20;
		public const int S_IWGRP = 0x10;

		// signals
		public const int SIGINT = 2;
		public const int SIGQUIT = 3;

		[DllImport (Libc, SetLastError = true)]
		static extern int open (string pathname, int flags, int mode);

		[DllImport (Libc, SetLastError = true)]
		static extern int close (int fd);

		[DllImport (Libc, SetLastError = true)]
		static extern int dup2 (int oldfd, int newfd);

		[DllImport (Libc, SetLastError = true)]
		static extern int unlink (string pathname);

		[DllImport (Libc, SetLastError = true)]
		static extern int symlink (string target, string linkpath);

		[DllImport (Libc, SetLastError = true)]
		static extern int kill (int pid, int sig);

		[DllImport (Libc, SetLastError = true)]
		static extern int rename (string oldpath, string newpath);

		[DllImport (Libc, SetLastError = true)]
		static extern int readlink (string path, byte [] buf, int bufsiz);

		/// <summary>Opens a file with the given flags and mode. Returns the file descriptor, or -1 on
		/// failure with errno available via <see cref="GetLastError"/>.</summary>
		public static int Open (string path, int flags, int mode) => open (path, flags, mode);

		public static int Close (int fd) => close (fd);

		public static int Dup2 (int oldfd, int newfd) => dup2 (oldfd, newfd);

		public static int Unlink (string path) => unlink (path);

		/// <summary>Creates a symbolic link from <paramref name="linkpath"/> to
		/// <paramref name="target"/>. Returns 0 on success, negative on failure.</summary>
		public static int Symlink (string target, string linkpath) => symlink (target, linkpath);

		public static int Kill (int pid, int sig) => kill (pid, sig);

		public static int Rename (string oldpath, string newpath) => rename (oldpath, newpath);

		/// <summary>Resolves the target of a symbolic link via readlink. Returns the raw target
		/// (which may be relative) or null when <paramref name="path"/> is not a symbolic link.</summary>
		public static string ReadLink (string path)
		{
			// Grow the buffer up to a practical maximum; readlink returns -1 (ELOOP) once the
			// buffer is large enough to hold the result but not all target is read yet.
			for (int size = 256; size <= (1 << 20); size *= 2) {
				var buf = new byte[size];
				int n = readlink (path, buf, buf.Length);
				if (n < 0)
					return null;
				if (n < buf.Length)
					return Encoding.UTF8.GetString (buf, 0, n);
			}
			return null;
		}

		/// <summary>Returns the errno value for the last failed libc call (requires SetLastError=true
		/// on the DllImports above).</summary>
		public static int GetLastError () => Marshal.GetLastWin32Error ();

		/// <summary>POSIX waitpid status macro WIFSIGNALED: true if the process was terminated by a
		/// signal.</summary>
		public static bool WIFSIGNALED (int status)
		{
			int s = status & 0x7f;
			return s != 0x7f && s != 0;
		}

		/// <summary>POSIX waitpid status macro WTERMSIG: the terminating signal number.</summary>
		public static int WTERMSIG (int status) => status & 0x7f;
	}
}