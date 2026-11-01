// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.SensorHost;

using System.IO.Pipes;
using System.Security.Principal;
using System.Runtime.InteropServices;

internal sealed class SensorPipeSession : IDisposable
{
    readonly NamedPipeClientStream pipe;
    readonly TextReader input;
    readonly TextWriter output;
    public int ParentId { get; }
    public long ParentStartTicks { get; }
    public SensorPipeSession(string[] args)
    {
        string Value(string key) { int index = Array.IndexOf(args, key); return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new InvalidDataException("Missing pipe argument"); }
        string name = Value("--pipe");
        if (!name.StartsWith("AioDisplay-") || !Guid.TryParseExact(name[11..], "N", out _)) throw new InvalidDataException("Invalid sensor pipe");
        ParentId = int.Parse(Value("--parent")); ParentStartTicks = long.Parse(Value("--parent-start"));
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) throw new UnauthorizedAccessException("Admin-Sensorprozess benötigt erhöhte Rechte.");
        // Identification prevents the unelevated server from impersonating this worker.
        pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
        pipe.Connect(15000);
        if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out uint pid) || pid != ParentId) { pipe.Dispose(); throw new IOException("Unexpected sensor pipe owner"); }
        using var parent = System.Diagnostics.Process.GetProcessById(ParentId);
        if (parent.StartTime.ToUniversalTime().Ticks != ParentStartTicks) { pipe.Dispose(); throw new IOException("Parent identity changed"); }
        input = new StreamReader(pipe, System.Text.Encoding.UTF8, false, 4096, true);
        output = TextWriter.Synchronized(new StreamWriter(pipe, new System.Text.UTF8Encoding(false), 4096, true) { AutoFlush = true });
        Console.SetIn(input); Console.SetOut(output); Console.SetError(new PipeLogWriter(output));
    }
    public void Dispose() { try { output?.Dispose(); } catch (IOException) { } finally { pipe.Dispose(); input?.Dispose(); } }
    sealed class PipeLogWriter(TextWriter output) : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void WriteLine(string value) => output.WriteLine("#LOG " + value?.Replace('\n', ' ').Replace('\r', ' '));
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetNamedPipeServerProcessId(IntPtr pipe, out uint pid);
}
