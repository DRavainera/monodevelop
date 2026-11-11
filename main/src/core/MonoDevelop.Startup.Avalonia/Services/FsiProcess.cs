using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>
/// A <c>dotnet fsi</c> child process speaking the F# interactive prompt protocol:
/// stdout carries the banner, the results and the trailing <c>"&gt; "</c> prompt
/// (with no newline after it, both for the primary and the continuation prompt),
/// while the diagnostics (the echoed source line with the <c>----^^^</c> marker
/// and the <c>error FS####:</c> message) go to stderr. <c>#q;;</c> exits cleanly
/// with code 0.
///
/// This is the <c>dotnet fsi</c> equivalent of the legacy
/// <c>InteractiveSession</c> (MonoDevelop.FSharpBinding/Services/InteractiveSession.fs):
/// that protocol (<c>SERVER-PROMPT&gt;</c> markers, <c>input</c>-prefixed lines and
/// the <c>MonoDevelop.FSharpInteractive.Service.exe</c> sidecar) does not exist in
/// the SDK's plain FSI, so the prompt is detected from the trailing <c>"&gt; "</c>
/// tail instead. The FSI prompt never echoes the typed input, so every line of
/// stdout is either banner or result.
///
/// stdout is read byte-wise from the raw stream (a line-based reader would never
/// surface the prompt, which has no trailing newline) and split on newlines;
/// stderr keeps line-based draining. Output and error lines are merged, in
/// arrival order, into one queue the pad drains — diagnostics interleave with
/// the results the same way they did in the legacy service.
/// </summary>
public sealed class FsiProcess : IDisposable
{
	readonly Process process;
	readonly StringBuilder stdoutBuf = new ();
	readonly object bufGate = new (); // guards stdoutBuf (read loop vs Exited flush)
	readonly Queue<string> merged = new ();
	readonly Queue<bool> mergedIsPrompt = new ();
	readonly object gate = new ();
	readonly CancellationTokenSource cts = new ();
	bool exited;
	bool promptSeen;

	/// <summary>Merged output line, in arrival order (stdout results and stderr
	/// diagnostics interleaved); the prompt marker is delivered as a line whose
	/// <c>isPrompt</c> flag is set.</summary>
	public event Action<string, bool>? LineReceived;

	/// <summary>The prompt marker <c>"&gt; "</c> was flushed to the end of
	/// stdout: the FSI is ready for the next input (legacy PromptReady).</summary>
	public event Action? PromptReady;

	/// <summary>The first prompt has been received — the session is usable.</summary>
	public event Action? Ready;

	/// <summary>The process exited (code 0 for a clean <c>#q;;</c>) —
	/// legacy Exited.</summary>
	public event Action<int>? Exited;

	/// <summary>The process is running.</summary>
	public bool IsRunning { get; private set; }

	/// <summary>The first prompt has been received — the session accepts
	/// input (the legacy PromptReady latch).</summary>
	public bool IsReady => promptSeen;

	public FsiProcess (string workingDirectory)
	{
		var psi = new ProcessStartInfo ("dotnet", "fsi --nologo") {
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			// stdin must be redirected too: StandardInputEncoding (below) and
			// SendLine both require it — .NET throws "StandardInputEncoding is
			// only supported when standard input is redirected" otherwise.
			RedirectStandardInput = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = workingDirectory,
			// stdout is read raw (BaseStream) and decoded UTF-8 in ReadStdoutLoop;
			// stderr uses the line reader, so its encoding must be UTF-8 (the F#
			// diagnostics carry accented text). Input is UTF-8 F# source.
			StandardErrorEncoding = Encoding.UTF8,
			// Input encoding must NOT emit the UTF-8 BOM: .NET's Encoding.UTF8
			// is a UTF8Encoding that writes the BOM on the first StreamWriter
			// flush, so the FSI would receive "\uFEFF1+1;;" and reject it with
			// FS0010 "Unexpected character". No-BOM UTF-8 is what the FSI
			// expects for piped source.
			StandardInputEncoding = new System.Text.UTF8Encoding (false),
		};
		process = new Process { StartInfo = psi };
		process.EnableRaisingEvents = true;
		process.ErrorDataReceived += (_, e) => { if (e.Data is not null) OnStderr (e.Data); };
		process.Exited += (_, _) => OnExited ();
		process.Start ();
		process.BeginErrorReadLine ();
		// stdout: raw byte reader (the prompt has no newline, see the class
		// summary); stderr keeps the line-based reader (its lines always do).
		_ = Task.Run (ReadStdoutLoop);
		IsRunning = true;
	}

	async Task ReadStdoutLoop ()
	{
		var stream = process.StandardOutput.BaseStream;
		var buffer = new byte[4096];
		while (true) {
			int read;
			try {
				read = await stream.ReadAsync (buffer.AsMemory (), cts.Token);
			} catch (OperationCanceledException) {
				return;
			} catch (IOException) {
				return; // the process is gone
			}
			if (read <= 0)
				return;
			HandleChunk (Encoding.UTF8.GetString (buffer, 0, read));
		}
	}

	void HandleChunk (string text)
	{
		// Collect the complete lines (and whether the prompt marker is pending)
		// under bufGate — the Exited flusher can touch stdoutBuf from another
		// thread — then Emit them outside (Emit takes the merged gate).
		var lines = new List<string> ();
		bool promptPending = false;
		lock (bufGate) {
			stdoutBuf.Append (text);
			string s = stdoutBuf.ToString ();
			int idx;
			while ((idx = s.LastIndexOf ('\n')) >= 0) {
				var chunk = s.Substring (0, idx);
				s = s.Substring (idx + 1);
				lines.AddRange (chunk.Split ('\n'));
			}
			// The trailing "> " prompt: detected on the buffered tail (which ends
			// right after the marker, since no newline follows it), then consumed
			// so the next output does not retrigger it.
			if (s.Length >= 2 && s [s.Length - 2] == '>' && s [s.Length - 1] == ' ') {
				promptPending = true;
				s = s.Substring (0, s.Length - 2);
			}
			stdoutBuf.Clear ();
			stdoutBuf.Append (s);
		}
		foreach (var line in lines)
			Emit (line, false);
		if (promptPending)
			Emit (string.Empty, true);
	}

	void OnStderr (string data)
	{
		Emit (data, false);
	}

	void Emit (string line, bool isPrompt)
	{
		lock (gate) {
			merged.Enqueue (line);
			mergedIsPrompt.Enqueue (isPrompt);
		}
		if (isPrompt && !promptSeen) {
			promptSeen = true;
			Ready?.Invoke ();
		}
		if (isPrompt)
			PromptReady?.Invoke ();
		DrainMerged ();
	}

	/// <summary>Drains the merged queue to LineReceived in arrival order.</summary>
	void DrainMerged ()
	{
		while (true) {
			string line;
			bool isPrompt;
			lock (gate) {
				if (merged.Count == 0)
					return;
				line = merged.Dequeue ()!;
				isPrompt = mergedIsPrompt.Dequeue ();
			}
			LineReceived?.Invoke (line, isPrompt);
		}
	}

	void OnExited ()
	{
		int code = 0;
		try {
			code = process.ExitCode;
		} catch (InvalidOperationException) {
			// The process was still dying when the event fired.
		}
		// Flush whatever stdout tail never got a newline (under bufGate: the
		// read loop may still be draining when the Exited event fires).
		var lines = new List<string> ();
		lock (bufGate) {
			if (stdoutBuf.Length > 0) {
				lines.AddRange (stdoutBuf.ToString ().Split ('\n'));
				stdoutBuf.Clear ();
			}
		}
		foreach (var line in lines)
			Emit (line, false);
		if (exited)
			return;
		exited = true;
		IsRunning = false;
		Exited?.Invoke (code);
	}

	/// <summary>Sends a line of F# source to the session (the caller has
	/// already applied the <c>;;</c> terminator semantics).</summary>
	public bool SendLine (string line)
	{
		try {
			if (!IsRunning || process.HasExited)
				return false;
			process.StandardInput.WriteLine (line);
			process.StandardInput.Flush ();
			return true;
		} catch (Exception ex) {
			Console.WriteLine ("[fsi] send failed: " + ex.Message);
			return false;
		}
	}

	/// <summary>Sends <c>#q;;</c> and waits briefly for the clean exit (plain
	/// FSI has no kill message — that was the legacy service protocol). Falls
	/// back to a hard kill when the FSI does not exit in time, so a blocked
	/// session neither leaves the pad dead nor orphans the process.</summary>
	public void Shutdown ()
	{
		if (exited)
			return;
		try {
			if (!process.HasExited) {
				process.StandardInput.WriteLine ("#q;;");
				process.StandardInput.Flush ();
				if (!process.WaitForExit (3000))
					process.Kill ();
			}
		} catch (Exception ex) {
			Console.WriteLine ("[fsi] shutdown: " + ex.Message);
		}
	}

	public void Dispose ()
	{
		try {
			cts.Cancel ();
		} catch { }
		Shutdown ();
		try {
			process.Dispose ();
		} catch { }
	}
}
