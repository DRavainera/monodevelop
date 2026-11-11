//
// HighlightingCasShims.cs
//
// .NET Framework Code Access Security types removed from the .NET BCL, needed
// by the legacy RegexEngine (MonoDevelop.Ide.Editor.Highlighting/RegexEngine),
// a .NET Framework-era port of .NET's regex engine. The engine runs
// Reflection.Emit under [HostProtection]/[PermissionSet]/[SecurityCritical]
// demands and calls ReflectionPermission/CodeAccessPermission at runtime.
//
// In .NET (no CAS) the demands are no-ops and the attributes are dead
// metadata, so inert shims let the engine compile and run unchanged. The set
// is the exact minimal one verified by the end-to-end highlighting probe:
//
//   - SecurityPermissionAttribute, SecurityAction and PermissionState EXIST in
//     the .NET 10 BCL (System.Runtime) and are NOT redefined here (the
//     attribute requires the real SkipVerification member).
//   - System.Security.Permissions is NOT in the shell's compile closure, so
//     the namespace shims below cannot collide.
//
#if AVALONIA_SHELL

using System;

namespace System.Security
{
	[AttributeUsage (AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	public sealed class SecurityCriticalAttribute : Attribute { }
}

namespace System.Security.Permissions
{
	[AttributeUsage (AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	public sealed class PermissionSetAttribute : Attribute
	{
		public PermissionSetAttribute (SecurityAction action) { }
		public PermissionSetAttribute (SecurityAction action, bool unrestricted) { Unrestricted = unrestricted; }
		public bool Unrestricted { get; set; }
	}

	public class ReflectionPermission
	{
		public ReflectionPermission (PermissionState state) { }
		public void Assert () { }
	}

	public static class CodeAccessPermission
	{
		public static void RevertAssert () { }
		public static void Assert () { }
	}
}

namespace System
{
	[AttributeUsage (AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	public sealed class HostProtectionAttribute : Attribute
	{
		public HostProtectionAttribute () { }
		public bool MayLeakOnAbort { get; set; }
		public bool Synchronization { get; set; }
		public bool ExternalCodeMethInvocation { get; set; }
		public bool UnsafeExecution { get; set; }
		public bool Environment { get; set; }
		public bool Exit { get; set; }
		public bool ExitMethInvocation { get; set; }
		public bool ReflectionElevation { get; set; }
		public bool ReflectionElevationMethInvocation { get; set; }
		public bool Reflection { get; set; }
		public bool ReflectionUnrestrictedMethInvocation { get; set; }
	}
}

#endif
