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

    public SensorProcessClient(string path, Action<string> logger)
    {
        executable = path;
        log = logger;
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

    public void EnsureRunning(SensorOptions options)
    {
        options.Validate();
        string configuration = JsonSerializer.Serialize(options, SnapshotProtocol.Json);
        lock (sync)
        {
            if (disposed) return;
            DateTime now = DateTime.UtcNow;
            if (child != null && !child.HasExited)
            {
                bool unchanged = configuration == signature;
                bool starting = healthy == DateTime.MinValue && now - attempted < TimeSpan.FromSeconds(20);
                if (unchanged && (starting || now - healthy < TimeSpan.FromSeconds(5))) return;
                log("Sensor host restart: " + (unchanged ? "no valid snapshots" : "configuration changed"));
                StopLocked();
                attempted = DateTime.MinValue;
            }
            if (now - attempted < TimeSpan.FromSeconds(3)) return;
            attempted = now;
            StopLocked();
            try
            {
                if (!File.Exists(executable))
                    throw new FileNotFoundException("Sensorprozess fehlt. Programm bitte vollständig entpacken.", executable);
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
        if (!SnapshotProtocol.TryRead(line, out var snapshot)) return;
        lock (sync)
        {
            // Ignore callbacks queued by a previous child after a restart.
            if (disposed || child != process || snapshot.SampleSequence <= sequence) return;
            sequence = snapshot.SampleSequence;
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
        Entries = s.Entries, Fans = s.Fans, Gpus = s.Gpus,
        Status = s.Status, Fresh = s.Fresh, VramTotalMb = s.VramTotalMb,
        Source = s.Source, CpuTemperatureSource = s.CpuTemperatureSource,
        GpuName = s.GpuName, Error = s.Error,
        SampleTicks = s.SampleTicks, SampleSequence = s.SampleSequence,
        FanSampleTicks = s.FanSampleTicks, FanSource = s.FanSource, ReportedThreadCount = s.ReportedThreadCount
    };

    private void StopLocked()
    {
        var process = child;
        child = null;
        Volatile.Write(ref latest, null);
        if (process == null) return;
        // Do not wait under the lock: pending stdout callbacks can still need it.
        try { if (!process.HasExited) process.Kill(true); }
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
