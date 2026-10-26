//
// UnixCompat.cs
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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Mono.Unix.Native
{
	// Reimplementation of the Mono.Posix surface the IDE actually uses (Mono.Unix.Native.*),
	// backed by direct P/Invoke into libc so the net10.0 tree has no dependency on the legacy
	// Mono.Posix assembly. Error convention matches Mono.Unix.Native.Syscall: 0 on success,
	// otherwise the positive errno value.
	[Flags]
	public enum FilePermissions
	{
		None = 0,
		S_ISUID = 0x800,
		S_ISGID = 0x400,
		S_ISVTX = 0x80,
		S_IFMT = 0xF000,
		S_IFIFO = 0x1000,
		S_IFCHR = 0x2000,
		S_IFDIR = 0x4000,
		S_IFBLK = 0x6000,
		S_IFREG = 0x8000,
		S_IFLNK = 0xA000,
		S_IFSOCK = 0xC000,
		S_IRUSR = 0x100,
		S_IWUSR = 0x80,
		S_IXUSR = 0x40,
		S_IRWXU = 0x1C0,
		S_IRGRP = 0x20,
		S_IWGRP = 0x10,
		S_IXGRP = 0x8,
		S_IRWXG = 0x38,
		S_IROTH = 0x4,
		S_IWOTH = 0x2,
		S_IXOTH = 0x1,
		S_IRWXO = 0x7,
		DEFFILEMODE = S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH,
		DEFFDIRMODE = S_IRWXU | S_IRWXG | S_IRWXO,
		ALLPERMS = 0x1FFF
	}

	// Standard glibc/Linux errno values (subset covers everything the call sites can hit).
	public enum Errno
	{
		EPERM = 1, ENOENT = 2, ESRCH = 3, EINTR = 4, EIO = 5, ENXIO = 6, E2BIG = 7,
		ENOEXEC = 8, EBADF = 9, ECHILD = 10, EAGAIN = 11, EWOULDBLOCK = 11, ENOMEM = 12,
		EACCES = 13, EFAULT = 14, ENOTBLK = 15, EBUSY = 16, EEXIST = 17, EXDEV = 18,
		ENODEV = 19, ENOTDIR = 20, EISDIR = 21, EINVAL = 22, ENFILE = 23, EMFILE = 24,
		ENOTTY = 25, ETXTBSY = 26, EFBIG = 27, ENOSPC = 28, ESPIPE = 29, EROFS = 30,
		EMLINK = 31, EPIPE = 32, EDOM = 33, ERANGE = 34, EDEADLK = 35, ENAMETOOLONG = 36,
		ENOLCK = 37, ENOSYS = 38, ENOTEMPTY = 39, ELOOP = 40, ENOMSG = 42, EIDRM = 43,
		ENOLINK = 67, EPROTO = 71, EMULTIHOP = 72, EBADMSG = 74, EOVERFLOW = 75,
		ENOTUNIQ = 87, EBADFD = 77, EREMCHG = 88, ELIBBAD = 81, EILSEQ = 84
	}

	public enum Signum
	{
		SIGHUP = 1, SIGINT = 2, SIGQUIT = 3, SIGILL = 4, SIGTRAP = 5, SIGABRT = 6, SIGBUS = 7,
		SIGFPE = 8, SIGKILL = 9, SIGUSR1 = 10, SIGSEGV = 11, SIGUSR2 = 12, SIGPIPE = 13,
		SIGALRM = 14, SIGTERM = 15, SIGSTKFLT = 16, SIGCHLD = 17, SIGCONT = 18, SIGSTOP = 19,
		SIGTSTP = 20, SIGTTIN = 21, SIGTTOU = 22, SIGURG = 23, SIGXCPU = 24, SIGXFSZ = 25,
		SIGVTALRM = 26, SIGPROF = 27, SIGWINCH = 28, SIGIO = 29, SIGPWR = 30, SIGSYS = 31
	}

	// glibc struct stat for x86-64 Linux (the only platform this tree builds for on Linux).
	// Only st_mode is consumed; the full layout is kept so the P/Invoke offset is correct.
	[StructLayout (LayoutKind.Sequential)]
	public struct Stat
	{
		public ulong st_dev;
		public ulong st_ino;
		public uint st_mode;
		public uint st_nlink;
		public int st_uid;
		public int st_gid;
		public int __pad0;
		public ulong st_rdev;
		public long st_size;
		public int st_blksize;
		public int __pad1;
		public long st_blocks;
		public long st_atime_sec;
		public long st_atime_nsec;
		public long st_mtime_sec;
		public long st_mtime_nsec;
		public long st_ctime_sec;
		public long st_ctime_nsec;
		public int __unused4;
		public int __unused5;
	}

	public static class Syscall
	{
		const string Libc = "libc";

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_chmod (string pathname, uint mode);

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_kill (int pid, int sig);

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_waitpid (int pid, out int status, int options);

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_setuid (uint uid);

		[DllImport (Libc)]
		static extern int libc_getpid ();

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_stat (string pathname, out Stat buf);

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_lstat (string pathname, out Stat buf);

		[DllImport (Libc, SetLastError = true)]
		static extern int libc_readlink (string path, byte [] buf, int bufsiz);

		// 0 on success, otherwise the positive errno value (Mono.Unix.Native.Syscall convention).
		static int ErrorToErrno (int result)
		{
			if (result == 0)
				return 0;
			return Marshal.GetLastWin32Error ();
		}

		public static int chmod (string pathname, FilePermissions permissions)
			=> ErrorToErrno (libc_chmod (pathname, (uint)permissions));

		public static int kill (int pid, Signum sig)
			=> ErrorToErrno (libc_kill (pid, (int)sig));

		// Raw waitpid semantics: -1 on error, otherwise the pid of the waited child.
		public static int wait (out int status)
			=> libc_waitpid (-1, out status, 0);

		public static int waitpid (int pid, out int status, int options)
			=> libc_waitpid (pid, out status, options);

		public static int setuid (uint uid)
			=> ErrorToErrno (libc_setuid (uid));

		public static int getpid ()
			=> libc_getpid ();

		public static int stat (string pathname, out Stat buf)
			=> ErrorToErrno (libc_stat (pathname, out buf));

		public static int lstat (string pathname, out Stat buf)
			=> ErrorToErrno (libc_lstat (pathname, out buf));

		// Resolves a symbolic link target via readlink. Returns null when the path is not a
		// symbolic link or the read fails.
		public static string ReadLink (string path)
		{
			for (int size = 256; size <= (1 << 20); size *= 2) {
				var buf = new byte[size];
				int n = libc_readlink (path, buf, buf.Length);
				if (n < 0)
					return null;
				if (n < buf.Length)
					return Encoding.UTF8.GetString (buf, 0, n);
			}
			return null;
		}

		public static bool WIFEXITED (int status) => (status & 0x7f) == 0;

		public static int WEXITSTATUS (int status) => (status >> 8) & 0xff;

		public static bool WIFSIGNALED (int status)
		{
			int s = status & 0x7f;
			return s != 0 && s != 0x7f;
		}

		public static Signum WTERMSIG (int status) => (Signum)(status & 0x7f);
	}
}

namespace Mono.Unix
{
	// Reimplementation of the Mono.Posix Mono.Unix.UnixFileInfo surface the IDE uses
	// (ctor + Protection) via stat/fchmod P/Invoke.
	public class UnixFileInfo
	{
		readonly string fullpath;

		public UnixFileInfo (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException (nameof (filename));
			fullpath = Path.GetFullPath (filename);
		}

		public string FullName => fullpath;

		public string Name => Path.GetFileName (fullpath);

		public bool Exists
		{
			get
			{
				Native.Stat buf;
				return Native.Syscall.stat (fullpath, out buf) == 0;
			}
		}

		public Native.FilePermissions Protection
		{
			get
			{
				Native.Stat buf;
				int err = Native.Syscall.stat (fullpath, out buf);
				if (err != 0)
					throw new IOException (string.Format ("stat({0}) failed: errno {1}", fullpath, err));
				return (Native.FilePermissions)(buf.st_mode & 0x1FFF);
			}
			set
			{
				int err = Native.Syscall.chmod (fullpath, value);
				if (err != 0)
					throw new IOException (string.Format ("chmod({0}, {1:o}) failed: errno {2}", fullpath, (uint)value, err));
			}
		}
	}

	// Reimplementation of the Mono.Posix Mono.Unix.UnixSymbolicLinkInfo surface the IDE uses
	// (IsSymbolicLink, HasContents, ContentsPath) via lstat/readlink P/Invoke.
	public class UnixSymbolicLinkInfo : UnixFileInfo
	{
		public UnixSymbolicLinkInfo (string filename) : base (filename)
		{
		}

		public bool IsSymbolicLink
		{
			get
			{
				Native.Stat buf;
				if (Native.Syscall.lstat (FullName, out buf) != 0)
					return false;
				return (buf.st_mode & 0xF000) == (uint)Native.FilePermissions.S_IFLNK;
			}
		}

		public bool HasContents
		{
			get
			{
				return ContentsPath != null;
			}
		}

		public string ContentsPath
		{
			get
			{
				if (!IsSymbolicLink)
					return null;
				return Native.Syscall.ReadLink (FullName);
			}
		}
	}
}
