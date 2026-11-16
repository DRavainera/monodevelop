using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoDevelop.AvaloniaShell.Views;

// Operating-system details for the About dialog's "Operating System" section.
//
// The legacy SystemInformation.GetOperatingSystemDescription() printed a single
// "uname -mrsv" line on Linux and macOS and Environment.OSVersion on Windows.
// The operator asked for a richer report on every OS: distribution (name +
// version), desktop environment (name + version), graphical server (Wayland /
// X11), architecture (X64, Arm64, ...) and the packaging kind the application
// was installed with (deb, rpm, flatpak, snap, AppImage, ...).
//
// This is a shell-only module: it lives next to the other shell Views and never
// touches MonoDevelop.Core, so the legacy GTK build is unaffected (§18.4). It
// deliberately does not run the Mono.Addins engine; everything here is plain
// file / environment / registry access with bounded timeouts on the external
// commands, so the dialog can never hang on a missing or broken tool.
public static class OperatingSystemInfo
{
	// Full text of the section, as it is shown in the dialog and copied to the
	// clipboard. Every field is a "\t<label>: <value>" line (legacy used a
	// leading tab for the OS description body too).
	public static string GetDescription ()
	{
		var sb = new StringBuilder ();
		if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
			AppendLinux (sb);
		else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
			AppendMac (sb);
		else if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
			AppendWindows (sb);
		else
			sb.Append ("\tSystem: ").AppendLine (RuntimeInformation.OSDescription);

		sb.Append ("\tArchitecture: ").AppendLine (GetArchitecture ());
		sb.Append ("\tInstallation type: ").AppendLine (GetPackagingKind ());
		return sb.ToString ().TrimEnd ();
	}

	// Processor architecture, human-readable ("X64", "Arm64", "X86", ...).
	static string GetArchitecture ()
	{
		var arch = RuntimeInformation.OSArchitecture;
		switch (arch) {
			case Architecture.X64: return "X64 (64-bit)";
			case Architecture.X86: return "x86 (32-bit)";
			case Architecture.Arm: return "Arm (32-bit)";
			case Architecture.Arm64: return "Arm64 (64-bit)";
			default: return arch.ToString ();
		}
	}

	// ---------- Linux ----------

	static void AppendLinux (StringBuilder sb)
	{
		var osrelease = ParseOsRelease ();
		if (osrelease.TryGetValue ("PRETTY_NAME", out var prettyName) && prettyName.Length > 0)
			sb.Append ("\tDistribution: ").AppendLine (prettyName);
		else {
			sb.Append ("\tDistribution: ");
			var prefix = "\tDistribution: ".Length;
			if (osrelease.TryGetValue ("NAME", out var name) && name.Length > 0)
				sb.Append (name);
			if (osrelease.TryGetValue ("VERSION_ID", out var versionId) && versionId.Length > 0) {
				if (sb.Length > prefix)
					sb.Append (' ');
				sb.Append (versionId);
			}
			sb.AppendLine ();
		}

		AppendDesktopEnvironment (sb);
		AppendGraphicalServer (sb);
		AppendKernel (sb);
	}

	// Distribution data from /etc/os-release (or /usr/lib/os-release on the
	// minimal systems that ship only that one). Values are unquoted because the
	// file format allows quoting them.
	static Dictionary<string, string> ParseOsRelease ()
	{
		var dict = new Dictionary<string, string> ();
		foreach (var file in new[] { "/etc/os-release", "/usr/lib/os-release" }) {
			string? text;
			try {
				if (!File.Exists (file))
					continue;
				text = File.ReadAllText (file);
			} catch {
				continue;
			}
			foreach (var rawLine in text.Split ('\n')) {
				var line = rawLine.Trim ();
				if (line.Length == 0 || line.StartsWith ('#') || line.StartsWith ('!'))
					continue;
				var eq = line.IndexOf ('=');
				if (eq <= 0)
					continue;
				var key = line.Substring (0, eq).Trim ();
				var value = line.Substring (eq + 1).Trim ();
				if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
					value = value.Substring (1, value.Length - 2);
				if (!string.IsNullOrEmpty (key))
					dict[key] = value;
			}
			if (dict.Count > 0)
				break;
		}
		return dict;
	}

	// Desktop environment name + version. The name comes from the
	// XDG_CURRENT_DESKTOP environment variable (set by the display manager);
	// the version is probed per known desktop, each probe bounded so the dialog
	// never hangs on a broken tool.
	static void AppendDesktopEnvironment (StringBuilder sb)
	{
		var desktop = Environment.GetEnvironmentVariable ("XDG_CURRENT_DESKTOP");
		if (string.IsNullOrEmpty (desktop)) {
			sb.Append ("\tDesktop: ").AppendLine ("unknown");
			return;
		}
		var first = desktop.Split (',')[0].Trim ();
		var label = first == "ubuntu:GNOME" ? "GNOME (ubuntu flavor)" : first;
		var version = GetDesktopVersion (first);
		sb.Append ("\tDesktop: ").Append (label);
		if (!string.IsNullOrEmpty (version))
			sb.Append (" ").Append (version);
		sb.AppendLine ();
	}

	static string? GetDesktopVersion (string desktop)
	{
		// (executable, arguments) probes, first one that prints wins.
		(string File, string Args)[] probes;
		switch (desktop) {
			case "GNOME":
				probes = new (string, string)[] { ("gnome-session", "--version") };
				break;
			case "KDE":
				probes = new (string, string)[] { ("plasma-discover", "--version"), ("kded5", "--version"), ("kded4", "--version") };
				break;
			case "XFCE":
				probes = new (string, string)[] { ("xfce4-session", "--version") };
				break;
			case "Cinnamon":
				probes = new (string, string)[] { ("cinnamon", "--version") };
				break;
			case "MATE":
				probes = new (string, string)[] { ("mate-session", "--version") };
				break;
			case "LXDE":
				probes = new (string, string)[] { ("lxsession", "--version") };
				break;
			case "LXQt":
				probes = new (string, string)[] { ("lxqt-session", "--version") };
				break;
			case "Budgie":
				probes = new (string, string)[] { ("budgie-settings", "--version") };
				break;
			case "Enlightenment":
				probes = new (string, string)[] { ("enlightenment", "--version") };
				break;
			default:
				return null;
		}
		foreach (var (file, args) in probes) {
			var outLine = RunCommand (file, args);
			if (outLine is not null && outLine.Length > 0)
				return FirstVersionToken (outLine);
		}
		return null;
	}

	// "gnome-session 46.0" / "plasma-discover 6.1.4" -> the version token.
	static string FirstVersionToken (string line)
	{
		var tokens = line.Split (new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
		for (var i = tokens.Length - 1; i >= 0; i--) {
			var t = tokens[i].TrimEnd ('.', ',');
			if (t.Length > 0 && char.IsDigit (t[0]) && (t.IndexOf ('.') >= 0 || char.IsDigit (t[^1])))
				return t;
		}
		return line.Trim ();
	}

	// Graphical server: Wayland / X11, from the session environment.
	static void AppendGraphicalServer (StringBuilder sb)
	{
		var sessionType = Environment.GetEnvironmentVariable ("XDG_SESSION_TYPE");
		var waylandDisplay = Environment.GetEnvironmentVariable ("WAYLAND_DISPLAY");
		var display = Environment.GetEnvironmentVariable ("DISPLAY");

		string value;
		if (waylandDisplay is { Length: > 0 })
			value = "Wayland";
		else if (!string.IsNullOrEmpty (display))
			value = "X11";
		else if (string.IsNullOrEmpty (sessionType))
			value = "unknown";
		else
			value = sessionType;
		if (sessionType is { Length: > 0 } && !sessionType.Equals (value, StringComparison.OrdinalIgnoreCase))
			value += " (" + sessionType + ")";
		sb.Append ("\tGraphical server: ").AppendLine (value);
	}

	// Kernel: the same information the legacy "uname -mrsv" line carried.
	static void AppendKernel (StringBuilder sb)
	{
		var uname = RunCommand ("uname", "-sr");
		if (uname is not null && uname.Length > 0)
			sb.Append ("\tKernel: ").AppendLine (uname);
	}

	// ---------- macOS ----------

	static void AppendMac (StringBuilder sb)
	{
		// Product name + version: "macOS 15.2".
		var sw = RunCommand ("sw_vers", "-productVersion");
		sb.Append ("\tSystem: ").AppendLine ("macOS" + (sw is { Length: > 0 } ? " " + sw : string.Empty));

		// Desktop: macOS ships the AppKit (Cocoa) UI stack; no external DE to probe.
		sb.Append ("\tDesktop: ").AppendLine ("macOS (Cocoa)");

		// Graphical server: WindowServer, Apple's X11-compatible server.
		sb.Append ("\tGraphical server: ").AppendLine ("WindowServer");

		AppendKernel (sb);
	}

	// ---------- Windows ----------

	static void AppendWindows (StringBuilder sb)
	{
		var build = Environment.OSVersion.Version;
		var releaseId = ReadWindowsReleaseId ();
		sb.Append ("\tDistribution: ").Append ("Windows ").Append (releaseId is { Length: > 0 } ? releaseId + " " : string.Empty)
			.Append (build.Major).Append ('.').Append (build.Minor)
			.Append (" (build ").Append (build.Build).Append (")");
		sb.AppendLine ();

		// Desktop: the classic Windows shell.
		sb.Append ("\tDesktop: ").AppendLine ("Windows Desktop");

		// Graphical server: the Windows Desktop Window Manager (composition).
		sb.Append ("\tGraphical server: ").AppendLine ("Windows DWM");
	}

	static string? ReadWindowsReleaseId ()
	{
#if WINDOWS
		try {
			using var key = Microsoft.Win32.Registry.LocalMachine
				.OpenSubKey ("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");
			var currentBuildLabel = key?.GetValue ("CurrentBuildLabel") as string;
			if (currentBuildLabel is { Length: > 0 })
				return currentBuildLabel;
			var releaseId = key?.GetValue ("ReleaseId") as string;
			if (releaseId is { Length: > 0 })
				return releaseId;
		} catch {
		}
#endif
		return null;
	}

	// ---------- Packaging kind ----------

	// How THIS application was installed, from the environment the sandboxed
	// runtimes set, falling back to the package-manager database that owns the
	// shell's own executable, then "direct" (a plain file install / build).
	public static string GetPackagingKind ()
	{
		if (!string.IsNullOrEmpty (Env ("FLATPAK_ID")) || !string.IsNullOrEmpty (Env ("FLATPAK_RUN")))
			return "flatpak";
		if (!string.IsNullOrEmpty (Env ("SNAP")) || !string.IsNullOrEmpty (Env ("SNAP_REVISION")))
			return "snap";
		if (!string.IsNullOrEmpty (Env ("APPIMAGE")))
			return "AppImage";
		if (!string.IsNullOrEmpty (Env ("NIX_STORE_PATH")))
			return "nix";
		if (!string.IsNullOrEmpty (Env ("PKG_EXECUTABLE")))
			return "homebrew";

		return PackageOwnedByPackageSystem () ?? "direct";
	}

	static string? Env (string name) => Environment.GetEnvironmentVariable (name);

	// Ask the native package managers who owns the shell's executable. When the
	// app is launched through the .NET host (dotnet app.dll) the process path is
	// the host itself, so the entry assembly (the actual app file) is checked as
	// well.
	static string? PackageOwnedByPackageSystem ()
	{
		if (!RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
			return null;
		var files = new List<string> ();
		var exe = Environment.ProcessPath;
		if (!string.IsNullOrEmpty (exe))
			files.Add (exe);
		try {
			var entry = System.Reflection.Assembly.GetEntryAssembly()?.Location;
			if (!string.IsNullOrEmpty (entry))
				files.Add (entry);
		} catch {
		}
		foreach (var file in files) {
			try {
				var dpkg = RunCommandWithArgs ("dpkg", new[] { "-S", file });
				if (dpkg is not null && dpkg.Contains (": ") && !dpkg.Contains ("No path"))
					return "deb";
				if (Directory.Exists ("/var/lib/rpm") || File.Exists ("/usr/bin/rpm")) {
					var rpm = RunCommandWithArgs ("rpm", new[] { "-qf", file });
					if (rpm is not null && rpm.Length > 0
						&& !rpm.StartsWith ("rpm:", StringComparison.Ordinal)
						&& !rpm.Contains ("not owned", StringComparison.OrdinalIgnoreCase)
						&& !rpm.Contains ("No such file", StringComparison.OrdinalIgnoreCase))
						return "rpm";
				}
			} catch {
			}
		}
		return null;
	}

	// ---------- Command helper ----------

	static readonly int CommandTimeoutMs = 2000;

	// Run a command (argv form, no shell) and return its first non-empty stdout
	// line, trimmed. Bounded so the About dialog never hangs on a missing or
	// broken tool; null when the command cannot run, times out or exits
	// non-zero.
	static string? RunCommand (string file, string arguments)
	{
		var args = arguments.Split (' ', StringSplitOptions.RemoveEmptyEntries);
		return RunCommandWithArgs (file, args);
	}

	static string? RunCommandWithArgs (string file, string[] arguments)
	{
		try {
			var psi = new ProcessStartInfo (file) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			foreach (var arg in arguments)
				psi.ArgumentList.Add (arg);
			using var process = Process.Start (psi);
			if (process is null)
				return null;
			var stdout = process.StandardOutput.ReadToEnd ();
			process.StandardError.ReadToEnd ();
			if (!process.WaitForExit (CommandTimeoutMs)) {
				try { process.Kill (entireProcessTree: true); } catch { }
				return null;
			}
			if (process.ExitCode != 0)
				return null;
			foreach (var line in stdout.Split ('\n')) {
				var trimmed = line.Trim ();
				if (trimmed.Length > 0)
					return trimmed;
			}
		} catch {
		}
		return null;
	}
}
