// DebugSessionService.cs — DAP client over stdio for netcoredbg, built from
// the git submodule main/external/netcoredbg (cmake build). One session at a
// time, like the legacy DebuggingService. Events (stopped/exited/output) are
// surfaced as C# events; the MainWindow marshals them onto the UI thread.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonNode = System.Text.Json.Nodes.JsonNode;
using JsonObject = System.Text.Json.Nodes.JsonObject;
using JsonArray = System.Text.Json.Nodes.JsonArray;
using JsonValue = System.Text.Json.Nodes.JsonValue;

namespace MonoDevelop.AvaloniaShell.Services;

/// <summary>One variable shown in the Locals/Watch pads (DAP Variable).</summary>
public record DebugVariable (string Name, string Value, bool HasChildren, int VariablesReference);

/// <summary>One stack frame of the stopped thread (DAP StackFrame).</summary>
public record DebugFrame (int Id, string Method, string File, int Line);

/// <summary>One thread of the debuggee (DAP Thread).</summary>
public record DebugThread (int Id, string Name, bool Stopped);

/// <summary>Result of a DAP evaluate (Watch pad / hover eval).</summary>
public record DebugEvaluation (string Value, bool HasChildren, int VariablesReference, string? Error);

/// <summary>A parsed DAP message (response body or event body).</summary>
public sealed class DapBody
{
	public JsonObject Raw { get; init; } = new ();
	public JsonNode? this [string name] => Raw [name];
}

/// <summary>Where the debugger stopped (DAP stopped event).</summary>
public record DebugStopInfo (string Reason, int ThreadId, DebugFrame [] Frames);

public sealed class DebugSessionService : IDisposable
{
	Process? proc;
	readonly System.Collections.ObjectModel.ObservableCollection<string> outputLog = new ();
	readonly BlockingCollection<DapBody> replies = new ();
	Task? readerTask;
	int nextSeq = 1;

	DebugStopInfo? lastStopInfo;
	// Last stop received (buffered), so late subscribers never miss it — the
	// event races with the launch path; polling LastStop is deterministic.
	public DebugStopInfo? LastStop => lastStopInfo;

	/// <summary>Clears the buffered stop (QA: waiting for the NEXT stop, e.g. after a step).</summary>
	public void ResetLastStop () => lastStopInfo = null;

	public event EventHandler<DebugStopInfo>? Stopped;
	public event EventHandler? Terminated;
	public event EventHandler<string>? DebuggerOutput;
	public event EventHandler<int>? BreakpointHit;

	static string FindNetcoredbg ()
	{
		// netcoredbg is a git submodule (main/external/netcoredbg) built from
		// source with cmake; the binary lands in its build/src folder next to
		// the managed part it needs (ManagedPart.dll, Microsoft.CodeAnalysis.*).
		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 6 && dir is not null; i++) {
			var candidate = Path.Combine (dir, "external", "netcoredbg", "build", "src", "netcoredbg");
			if (File.Exists (candidate))
				return candidate;
			dir = Path.GetDirectoryName (dir);
		}
		return "netcoredbg";
	}

	public bool IsActive => proc is { HasExited: false };
	public System.Collections.ObjectModel.ObservableCollection<string> OutputLog => outputLog;

	public async Task<bool> StartAsync (string programPath, string workingDirectory, IEnumerable<(string File, int Line)> breakpoints)
		=> await StartAsync (programPath, workingDirectory,
			breakpoints.Select (b => (b.File, b.Line, (string?)null, (int?)null, (string?)null)));

	public Task<bool> AttachAsync (int pid, IEnumerable<(string File, int Line)> breakpoints)
		=> AttachAsync (pid, breakpoints.Select (b => (b.File, b.Line, (string?)null, (int?)null, (string?)null)));

	public async Task<bool> StartAsync (string programPath, string workingDirectory, IEnumerable<(string File, int Line, string? Condition, int? HitCount, string? LogMessage)> breakpoints)
		=> await StartCoreAsync (launchArgs => {
			launchArgs ["program"] = programPath;
			launchArgs ["cwd"] = workingDirectory;
		}, breakpoints, attach: false);

	/// <summary>Attach to a running process (legacy AttachToProcessHandler):
	/// the adapter debugs the given PID instead of launching a program.</summary>
	public Task<bool> AttachAsync (int pid, IEnumerable<(string File, int Line, string? Condition, int? HitCount, string? LogMessage)> breakpoints)
		=> StartCoreAsync (launchArgs => {
			launchArgs ["mode"] = "attach";
			launchArgs ["processId"] = pid;
		}, breakpoints, attach: true);

	// Shared launch/attach path: start the adapter, initialize, launch|attach,
	// push breakpoints (with condition/hitCondition/logMessage when present),
	// configurationDone.
	async Task<bool> StartCoreAsync (Action<Dictionary<string, object>> configureLaunch, IEnumerable<(string File, int Line, string? Condition, int? HitCount, string? LogMessage)> breakpoints, bool attach)
	{
		attachMode = attach;
		if (IsActive)
			Terminate ();
		var dbg = FindNetcoredbg ();
		var psi = new ProcessStartInfo (dbg, "--interpreter=vscode") {
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		proc = Process.Start (psi);
		if (proc is null)
			return false;
		readerTask = Task.Run (ReadLoop);

		var init = await RequestAsync ("initialize", new Dictionary<string, object> { ["adapterID"] = "coreclr", ["threads"] = true });
		if (init is null || !IsOk (init))
			return false;
		var launchArgs = new Dictionary<string, object> {
			["type"] = "coreclr",
			["name"] = "MonoDevelop",
			["stopAtEntry"] = false,
		};
		configureLaunch (launchArgs);
		// Attach uses the DAP "attach" command (netcoredbg's launch handler only
		// launches: it requires "program" and ignores mode=attach).
		var startResponse = await RequestAsync (attach ? "attach" : "launch", launchArgs);
		if (startResponse is null || !IsOk (startResponse))
			return false;
		var byFile = breakpoints.GroupBy (b => b.File);
		foreach (var g in byFile) {
			await RequestAsync ("setBreakpoints", new Dictionary<string, object> {
				["source"] = new Dictionary<string, object> { ["name"] = Path.GetFileName (g.Key), ["path"] = g.Key },
				["breakpoints"] = g.Select (b => {
					var bp = new Dictionary<string, object> { ["line"] = b.Line };
					if (!string.IsNullOrWhiteSpace (b.Condition))
						bp ["condition"] = b.Condition!;
					if (b.HitCount is int hit && hit > 0)
						bp ["hitCondition"] = hit.ToString ();
					if (!string.IsNullOrWhiteSpace (b.LogMessage))
						bp ["logMessage"] = b.LogMessage!;
					return (object)bp;
				}).ToArray (),
				["lines"] = g.Select (b => b.Line).ToArray (),
			});
		}
		await RequestAsync ("configurationDone", new Dictionary<string, object> ());
		return true;
	}

	public async Task ContinueAsync ()
		=> await RequestAsync ("continue", new Dictionary<string, object> { ["threadId"] = lastStoppedThreadId });

	int lastStoppedThreadId;

	/// <summary>Legacy DebugCommands.Pause: break all threads of the debuggee.
	/// netcoredbg accepts a real thread id (the process PID works at attach time)
	/// and emits stopped(allThreadsStopped) — LastStop picks it up.</summary>
	public async Task PauseAsync (int? threadId = null)
	{
		var tid = threadId ?? (lastStoppedThreadId > 0 ? lastStoppedThreadId : 1);
		await RequestAsync ("pause", new Dictionary<string, object> { ["threadId"] = tid });
	}

	// DAP stepping (legacy StepOver/StepInto/StepOut commands): next/stepIn/
	// stepOut operate on the stopped thread; the stopped event follows.
	public Task StepOverAsync ()
		=> RequestAsync ("next", new Dictionary<string, object> { ["threadId"] = lastStoppedThreadId });

	public Task StepIntoAsync ()
		=> RequestAsync ("stepIn", new Dictionary<string, object> { ["threadId"] = lastStoppedThreadId });

	public Task StepOutAsync ()
		=> RequestAsync ("stepOut", new Dictionary<string, object> { ["threadId"] = lastStoppedThreadId });

	/// <summary>DAP threads — the Threads pad rows.</summary>
	public async Task<DebugThread []> GetThreadsAsync ()
	{
		var res = await RequestAsync ("threads", new Dictionary<string, object> ());
		var arr = res? ["body"]? ["threads"] as JsonArray;
		if (arr is null)
			return Array.Empty<DebugThread> ();
		return arr.Select (t => new DebugThread (
			t? ["id"]?.GetValue<int> () ?? 0,
			t? ["name"]?.GetValue<string> () ?? "?",
			(t? ["id"]?.GetValue<int> () ?? 0) == lastStoppedThreadId)).ToArray ();
	}

	/// <summary>DAP stackTrace for any thread (Call Stack pad rows).</summary>
	public async Task<DebugFrame []> GetStackTraceAsync (int threadId)
	{
		var frames = await RequestAsync ("stackTrace", new Dictionary<string, object> { ["threadId"] = threadId, ["levels"] = 20, ["startFrame"] = 0 });
		var arr = frames? ["body"]? ["stackFrames"] as JsonArray;
		if (arr is null)
			return Array.Empty<DebugFrame> ();
		return arr.OfType<JsonObject> ().Select (f => new DebugFrame (
			f ["id"]?.GetValue<int> () ?? 0,
			f ["name"]?.GetValue<string> () ?? "?",
			f ["source"]? ["path"]?.GetValue<string> () ?? "",
			f ["line"]?.GetValue<int> () ?? 0)).ToArray ();
	}

	/// <summary>DAP evaluate — Watch pad rows and expression evaluation.
	/// With a frameId the expression evaluates in that scope.</summary>
	public async Task<DebugEvaluation> EvaluateAsync (string expression, int? frameId = null)
	{
		var args = new Dictionary<string, object> { ["expression"] = expression, ["context"] = "watch" };
		if (frameId is int fid)
			args ["frameId"] = fid;
		var res = await RequestAsync ("evaluate", args);
		if (res is null || !IsOk (res))
			return new DebugEvaluation ("", false, 0, res? ["message"]?.GetValue<string> () ?? "evaluate failed");
		var body = res ["body"] as JsonObject;
		return new DebugEvaluation (
			body? ["result"]?.GetValue<string> () ?? "",
			(body? ["variablesReference"]?.GetValue<int> () ?? 0) > 0,
			body? ["variablesReference"]?.GetValue<int> () ?? 0,
			null);
	}

	/// <summary>Frame id of the current stop (Watch evaluates in this scope).</summary>
	public int? CurrentFrameId
		=> IsActive && lastFrames.Length > 0 ? lastFrames [0]! ["id"]?.GetValue<int> () : null;

	public async Task<DebugVariable []> GetLocalsAsync ()
	{
		if (!IsActive || lastFrames.Length == 0)
			return Array.Empty<DebugVariable> ();
		var scopes = await RequestAsync ("scopes", new Dictionary<string, object> { ["frameId"] = lastFrames [0]! ["id"]?.GetValue<int> () ?? 0 });
		var scopeList = scopes? ["body"]? ["scopes"] as JsonArray;
		if (scopeList is null || scopeList.Count == 0)
			return Array.Empty<DebugVariable> ();
		var localsRef = scopeList [0]? ["variablesReference"]?.GetValue<int> () ?? 0;
		return await GetVariablesAsync (localsRef);
	}

	public async Task<DebugVariable []> GetVariablesAsync (int variablesReference)
	{
		if (variablesReference <= 0)
			return Array.Empty<DebugVariable> ();
		var res = await RequestAsync ("variables", new Dictionary<string, object> { ["variablesReference"] = variablesReference });
		var arr = res? ["body"]? ["variables"] as JsonArray;
		if (arr is null)
			return Array.Empty<DebugVariable> ();
		return arr.Select (v => new DebugVariable (
			v? ["name"]?.GetValue<string> () ?? "?",
			v? ["value"]?.GetValue<string> () ?? "",
			(v? ["variablesReference"]?.GetValue<int> () ?? 0) > 0,
			v? ["variablesReference"]?.GetValue<int> () ?? 0)).ToArray ();
	}

	JsonObject? [] lastFrames = Array.Empty<JsonObject> ();

	public DebugFrame [] CurrentFrames
		=> lastFrames.Select (f => new DebugFrame (
			f? ["id"]?.GetValue<int> () ?? 0,
			f? ["name"]?.GetValue<string> () ?? "?",
			f? ["source"]? ["path"]?.GetValue<string> () ?? "",
			f? ["line"]?.GetValue<int> () ?? 0)).ToArray ();

	void ReadLoop ()
	{
		var stdout = proc!.StandardOutput;
		try {
			while (!proc.HasExited) {
				var header = ReadHeader (stdout);
				if (header is null)
					break;
				var len = header.Value;
				var buf = new char [len];
				int read = 0;
				while (read < len) {
					int n = stdout.Read (buf, read, len - read);
					if (n <= 0)
						break;
					read += n;
				}
				var json = new string (buf, 0, read);
				var msg = JsonNode.Parse (json) as JsonObject;
				var type = msg? ["type"]?.GetValue<string> ();
				if (type == "response") {
					replies.Add (new DapBody { Raw = msg! });
				} else if (type == "event") {
					HandleEvent (msg!);
				}
			}
		} catch (Exception) {
			// process died or stream ended — session is over
		}
	}

	// Reads "Content-Length: N\r\n\r\n" and returns N, or null on EOF.
	static int? ReadHeader (StreamReader r)
	{
		int len = -1;
		while (true) {
			var line = r.ReadLine ();
			if (line is null)
				return null;
			if (line.Length == 0)
				break; // end of headers
			if (line.StartsWith ("Content-Length:", StringComparison.OrdinalIgnoreCase))
				len = int.Parse (line ["Content-Length:".Length..].Trim ());
			// loop until blank line: consume remaining headers
		}
		return len >= 0 ? len : null;
	}

	string? ReadHeaderLine (StreamReader r)
	{
		var sb = new StringBuilder ();
		while (true) {
			int c = r.Read ();
			if (c < 0)
				return sb.Length > 0 ? sb.ToString () : null;
			sb.Append ((char)c);
			if (c == '\n')
				return sb.ToString ().TrimEnd ();
		}
	}

	void HandleEvent (JsonObject msg)
	{
		var ev = msg ["event"]?.GetValue<string> ();
		var body = msg ["body"] as JsonObject;
		switch (ev) {
		case "stopped":
			lastStoppedThreadId = body? ["threadId"]?.GetValue<int> () ?? 1;
			var reason = body? ["reason"]?.GetValue<string> () ?? "breakpoint";
			// Pull the stack on a worker thread, then notify.
			_ = Task.Run (async () => {
				var frames = await RequestAsync ("stackTrace", new Dictionary<string, object> { ["threadId"] = lastStoppedThreadId, ["levels"] = 10, ["startFrame"] = 0 });
				var arr = frames? ["body"]? ["stackFrames"] as JsonArray;
				lastFrames = arr?.OfType<JsonObject> ().ToArray () ?? Array.Empty<JsonObject> ();
				var parsed = CurrentFrames;
				lastStopInfo = new DebugStopInfo (reason, lastStoppedThreadId, parsed);
				Stopped?.Invoke (this, lastStopInfo);
				var hit = parsed.FirstOrDefault ();
				if (reason == "breakpoint" && hit is not null)
					BreakpointHit?.Invoke (this, hit.Line);
			});
			break;
		case "terminated":
		case "exited":
			Terminated?.Invoke (this, EventArgs.Empty);
			break;
		case "output":
			var text = body? ["output"]?.GetValue<string> () ?? "";
			DebuggerOutput?.Invoke (this, text);
			break;
		}
	}

	async Task<JsonObject?> RequestAsync (string command, Dictionary<string, object> args)
	{
		if (proc is null || proc.HasExited)
			return null;
		int seq = nextSeq++;
		var msg = new JsonObject {
			["seq"] = seq,
			["type"] = "request",
			["command"] = command,
			["arguments"] = SerializeArgs (args),
		};
		var payload = msg.ToJsonString ();
		var bytes = Encoding.UTF8.GetBytes (payload);
		try {
			await proc.StandardInput.WriteAsync ($"Content-Length: {bytes.Length}\r\n\r\n{payload}");
			await proc.StandardInput.FlushAsync ();
		} catch (Exception) {
			return null;
		}
		// Wait for the matching reply.
		var timeout = TimeSpan.FromSeconds (30);
		var sw = Stopwatch.StartNew ();
		while (sw.Elapsed < timeout) {
			DapBody m;
			try {
				m = replies.Take (new CancellationTokenSource (TimeSpan.FromMilliseconds (200)).Token);
			} catch (OperationCanceledException) {
				break;
			}
			if (m.Raw ["request_seq"]?.GetValue<int> () == seq)
				return m.Raw;
		}
		return null;
	}

	static JsonNode SerializeArgs (Dictionary<string, object> args)
	{
		var obj = new JsonObject ();
		foreach (var (k, v) in args) {
			obj [k] = v switch {
				int i => JsonValue.Create (i),
				bool b => JsonValue.Create (b),
				string s => JsonValue.Create (s),
				IEnumerable<object> list => new JsonArray (list.Select (SerializeValue).ToArray ()),
				Dictionary<string, object> d => SerializeArgs (d),
				_ => JsonValue.Create (v?.ToString () ?? ""),
			};
		}
		return obj;
	}

	static JsonNode SerializeValue (object v) => v switch {
		int i => JsonValue.Create (i),
		bool b => JsonValue.Create (b),
		string s => JsonValue.Create (s),
		Dictionary<string, object> d => SerializeArgs (d),
		_ => JsonValue.Create (v?.ToString () ?? ""),
	};

	static bool IsOk (JsonObject? response)
		=> response? ["success"]?.GetValue<bool> () ?? false;

	public void Terminate ()
	{
		try {
			if (IsActive) {
				// Detach when the session was an attach (legacy DetachFromProcess):
				// the debuggee keeps running instead of being killed.
				_ = RequestAsync (attachMode ? "disconnect" : "terminate", new Dictionary<string, object> { ["terminateDebuggee"] = !attachMode });
				Thread.Sleep (300);
			}
		} catch { }
		try { proc?.Kill (true); } catch { }
		proc = null;
	}

	/// <summary>True when the session came from Attach to Process (Terminate then
	/// detaches instead of killing the debuggee).</summary>
	public bool IsAttach => attachMode;
	bool attachMode;

	public void Dispose () => Terminate ();
}
