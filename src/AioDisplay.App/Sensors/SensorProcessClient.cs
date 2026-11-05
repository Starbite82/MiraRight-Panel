// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System.Diagnostics;
using System.Text.Json;

/// <summary>Owns one child process. Rendering only reads the latest complete frame.</summary>
public sealed class SensorProcessClient : IDisposable
{
    private readonly object sync = new();
    private readonly string executable;
    private readonly Action<string> log;
    private Process child;
    private SensorSnapshot latest;
    private DateTime attempted = DateTime.MinValue;
    private DateTime healthy = DateTime.MinValue;
    private string signature = "";
    private long sequence;
    private bool disposed;
    private readonly bool elevated;
    private bool elevationAttempted;
    private ElevatedSensorTransport transport;
    private bool scheduledOnly;
    private DateTime nextDebug, nextRetry;
    private int invalidFrames, failures;

    public SensorProcessClient(string path, Action<string> logger, bool elevated = false)
    {
        executable = path;
        log = logger;
        this.elevated = elevated;
    }

    public int? ProcessId
    {
        get
        {
            lock (sync)
            {
                try { return child != null && !child.HasExited ? child.Id : null; }
                catch (InvalidOperationException) { return null; }
            }
        }
    }

    public SensorSnapshot Latest => Volatile.Read(ref latest);
    public int? ExitCode { get { lock(sync) { try { return child != null && child.HasExited ? child.ExitCode : null; } catch(InvalidOperationException) { return null; } catch(System.ComponentModel.Win32Exception) { return null; } } } }

    public void EnsureRunning(SensorOptions options)
    {
        options.Validate();
        string configuration = JsonSerializer.Serialize(options, SnapshotProtocol.Json);
        lock (sync)
        {
            if (disposed) return;
            if (elevated && elevationAttempted && signature != configuration) { StopLocked(); elevationAttempted = false; attempted = DateTime.MinValue; }
            if (transport?.Starting == true && signature == configuration) return;
            DateTime now = DateTime.UtcNow;
            if(now>=nextDebug){nextDebug=now.AddSeconds(30);Log.Debug("sensor.heartbeat",new { Pid=ProcessId, Elevated=elevated, Scheduled=transport?.Scheduled, Starting=transport?.Starting, Sequence=sequence, AgeMs=healthy==DateTime.MinValue?(double?)null:(now-healthy).TotalMilliseconds, InvalidFrames=invalidFrames, RetryBlocked=elevated&&elevationAttempted, NextRetry=nextRetry });}
            bool recoverable=elevated&&elevationAttempted&&transport?.Scheduled==true&&transport.Starting==false;
            if(recoverable&&child!=null&&child.HasExited){Log.Debug("sensor.exited",new { Pid=child.Id, ExitCode=ExitCode });StopLocked();scheduledOnly=true;elevationAttempted=false;nextRetry=now.AddSeconds(Math.Min(60,3*Math.Pow(2,Math.Min(failures++,4))));}
            if (child != null && !child.HasExited)
            {
                bool unchanged = configuration == signature;
                bool starting = healthy == DateTime.MinValue && now - attempted < TimeSpan.FromSeconds(20);
                if (unchanged && (starting || now - healthy < TimeSpan.FromSeconds(5))) return;
                log("Sensor host restart: " + (unchanged ? "no valid snapshots" : "configuration changed"));
                Log.Debug("sensor.watchdog",new { Pid=child.Id, Reason=unchanged?"no valid snapshots":"configuration changed", AgeMs=healthy==DateTime.MinValue?(double?)null:(now-healthy).TotalMilliseconds, Sequence=sequence });
                bool retryWithoutUac=transport?.Scheduled==true;
                StopLocked();
                if(retryWithoutUac){scheduledOnly=true;elevationAttempted=false;nextRetry=now.AddSeconds(Math.Min(60,3*Math.Pow(2,Math.Min(failures++,4))));}
                attempted = DateTime.MinValue;
            }
            if(now<nextRetry)return;
            if (now - attempted < TimeSpan.FromSeconds(3)) return;
            // Never repeat UAC prompts automatically after cancellation or worker failure.
            if (elevated && elevationAttempted) return;
            attempted = now;
            StopLocked();
            try
            {
                if (!File.Exists(executable))
                    throw new FileNotFoundException("Sensorprozess fehlt. Programm bitte vollständig entpacken.", executable);
                if (elevated)
                {
                    if (options.ParentId <= 0) throw new InvalidDataException("Admin-Sensorprozess benötigt einen übergeordneten Prozess.");
                    elevationAttempted = true; signature = configuration; sequence = 0; healthy = DateTime.MinValue;
                    var currentTransport = new ElevatedSensorTransport(); transport = currentTransport;
                    currentTransport.Start(executable, options, configuration, process => { lock (sync) { if (disposed || transport != currentTransport) { process.Dispose(); return; } child = process; attempted = DateTime.UtcNow; log("Admin sensor host started: pid=" + process.Id); } }, Receive, log, scheduledOnly);
                    return;
                }
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        WorkingDirectory = Path.GetDirectoryName(executable),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    }
                };
                process.OutputDataReceived += (sender, e) => Receive(process, e.Data);
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data)) log("Sensor host: " + e.Data);
                };
                child = process;
                signature = configuration;
                sequence = 0;
                healthy = DateTime.MinValue;
                Volatile.Write(ref latest, null);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.StandardInput.WriteLine(configuration);
                process.StandardInput.Close();
                log("Sensor host started: pid=" + process.Id);
            }
            catch (Exception e)
            {
                log("Sensor host start failed: " + e.Message);
                StopLocked();
            }
        }
    }

    private void Receive(Process process, string line)
    {
        if (!SnapshotProtocol.TryRead(line, out var snapshot)) { if(!string.IsNullOrEmpty(line))Interlocked.Increment(ref invalidFrames); return; }
        lock (sync)
        {
            // Ignore callbacks queued by a previous child after a restart.
            if (disposed || child != process || snapshot.SampleSequence <= sequence) return;
            if(sequence==0)Log.Debug("sensor.first-snapshot",new { Pid=process.Id, snapshot.SampleSequence, Bytes=line.Length, snapshot.Status });
            sequence = snapshot.SampleSequence; failures=0;
            Volatile.Write(ref latest, snapshot);
            // Unavailable sensors are a valid answer; only a silent/malformed producer needs restarting.
            healthy = DateTime.UtcNow;
        }
    }

    public SensorSnapshot Read()
    {
        var snapshot = Latest;
        if (SnapshotProtocol.IsFresh(snapshot, DateTime.UtcNow)) return Copy(snapshot);
        return new SensorSnapshot
        {
            Source = snapshot?.Source ?? "LibreHardwareMonitor",
            Status = "SENSOREN NICHT BEREIT",
            Gpus = snapshot?.Gpus ?? new(),
            Error = snapshot?.Error ?? ""
        };
    }

    private static SensorSnapshot Copy(SensorSnapshot s) => new()
    {
        UnifiedJson = s.UnifiedJson, Entries = s.Entries, Fans = s.Fans, Gpus = s.Gpus,
        Status = s.Status, Fresh = s.Fresh, VramTotalMb = s.VramTotalMb,
        Source = s.Source, CpuTemperatureSource = s.CpuTemperatureSource,
        GpuName = s.GpuName, Error = s.Error,
        SampleTicks = s.SampleTicks, SampleSequence = s.SampleSequence,
        FanSampleTicks = s.FanSampleTicks, FanSource = s.FanSource, ReportedThreadCount = s.ReportedThreadCount
    };

    private void StopLocked()
    {
        var process = child;
        transport?.Dispose(); transport = null;
        child = null;
        Volatile.Write(ref latest, null);
        if (process == null) return;
        // Do not wait under the lock: pending stdout callbacks can still need it.
        try { if (!elevated && !process.HasExited) process.Kill(true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception e) { log("Sensor host stop: " + e.Message); }
        finally { process.Dispose(); }
    }

    public void Stop()
    {
        lock (sync)
        {
            StopLocked();
            attempted = DateTime.MinValue;
            elevationAttempted = false;
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            StopLocked();
        }
    }
}
