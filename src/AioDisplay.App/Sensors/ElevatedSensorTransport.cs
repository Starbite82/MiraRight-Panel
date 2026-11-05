// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;

// A short-lived, read-only hardware worker; an optional preauthorized task can launch it.
internal sealed class ElevatedSensorTransport : IDisposable
{
    readonly CancellationTokenSource stop = new();
    NamedPipeServerStream pipe;
    public volatile bool Starting = true;
    public volatile bool Scheduled;
    public void Start(string executable, SensorOptions options, string configuration, Action<Process> started, Action<Process, string> receive, Action<string> log, bool scheduledOnly=false)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                stop.Token.ThrowIfCancellationRequested();
                string name = "AioDisplay-" + Guid.NewGuid().ToString("N");
                using var identity = WindowsIdentity.GetCurrent();
                var security = new PipeSecurity();
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new PipeAccessRule(identity.User, PipeAccessRights.FullControl, AccessControlType.Allow));
                pipe = NamedPipeServerStreamAcl.Create(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance, 65536, 65536, security);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                timeout.CancelAfter(TimeSpan.FromMinutes(2));
                var info = new ProcessStartInfo { FileName = executable, WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
                foreach (string argument in new[] { "--pipe", name, "--parent", options.ParentId.ToString(), "--parent-start", options.ParentStartTicks.ToString() }) info.ArgumentList.Add(argument);
                stop.Token.ThrowIfCancellationRequested();
                Process process = null;
                bool scheduled = ScheduledSensorTask.Installed;
                if(scheduledOnly&&!scheduled)throw new IOException("Sensoraufgabe nicht verfügbar; automatischer Neustart löst keine UAC-Abfrage aus.");
                Scheduled=scheduled;
                Log.Debug("sensor.launch",new { Mode=scheduled?"scheduled":"uac", options.ParentId });
                if (scheduled) { ScheduledSensorTask.Run(name, options); log("Sensorstart über Windows-Aufgabe (ohne UAC)."); }
                else { process = Process.Start(info) ?? throw new IOException("Sensorprozess wurde nicht gestartet."); started(process); }
                await pipe.WaitForConnectionAsync(timeout.Token);
                if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out uint pid)) throw new IOException("Sensorprozess am Datenkanal unbekannt.");
                if (scheduled) { process = ScheduledSensorTask.ConnectedProcess(pid); started(process); }
                else if (pid != process.Id) throw new IOException("Unerwarteter Sensorprozess am Datenkanal.");
                Log.Debug("sensor.pipe.connected",new { Pid=pid });
                using var writer = new StreamWriter(pipe, new System.Text.UTF8Encoding(false), 4096, true) { AutoFlush = true };
                using var reader = new StreamReader(pipe, System.Text.Encoding.UTF8, false, 4096, true);
                await writer.WriteLineAsync(configuration);
                Starting = false;
                while (!stop.IsCancellationRequested)
                {
                    string line = await reader.ReadLineAsync(stop.Token);
                    if (line == null) { Log.Debug("sensor.pipe.eof",new { Pid=process.Id, Requested=stop.IsCancellationRequested }); break; }
                    if (line.StartsWith("#LOG ")) { log("Sensor host: " + line[5..]);Log.Debug("sensor.stderr",new { Message=line[5..Math.Min(line.Length,4005)] }); }
                    else receive(process, line);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { if (!stop.IsCancellationRequested) { log("Admin-Sensorprozess: " + e.Message); Log.Debug("sensor.pipe.error",new { Type=e.GetType().FullName, e.Message, e.HResult, e.StackTrace }); } }
            finally { Starting = false; pipe?.Dispose(); }
        });
    }
    public void Dispose() { stop.Cancel(); pipe?.Dispose(); }
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetNamedPipeClientProcessId(IntPtr pipe, out uint pid);
}
