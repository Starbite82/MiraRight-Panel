// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System.Diagnostics;
using System.Text.Json;
public static class IntegrationChecks
{
    static void Assert(bool ok, string what) { if (!ok) throw new Exception(what); }
    static SensorOptions Options(string gpu = "") { using var p = Process.GetCurrentProcess(); return new() { IntervalMs = 100, GpuIdentifier = gpu, ParentId = p.Id, ParentStartTicks = p.StartTime.ToUniversalTime().Ticks }; }
    static void Wait(Func<bool> predicate, int seconds, string what) { var watch = Stopwatch.StartNew(); while (watch.Elapsed.TotalSeconds < seconds) { if (predicate()) return; Thread.Sleep(50); } throw new Exception("Timeout: " + what); }
    public static void Live(string host, int seconds)
    {
        var lines = new List<string>(); using var client = new SensorProcessClient(host, l => { lock (lines) lines.Add(l); Console.WriteLine(l); }); var options = Options();
        client.EnsureRunning(options); Wait(() => client.Read().Fresh, 15, "initial sensors"); int pid = client.ProcessId.Value;
        var first = client.Read(); Assert(first.ReportedThreadCount == Environment.ProcessorCount, "Physical thread count"); Assert(first.Entries.Any(r => r.Name == "CPU usage") && first.Entries.Any(r => r.Id == 145), "CPU and RAM");
        var clock = Stopwatch.StartNew(); long start = first.SampleSequence, last = start; double maxGap = 0, lastChange = 0; int stale = 0;
        while (clock.Elapsed.TotalSeconds < seconds) { client.EnsureRunning(options); var s = client.Read(); if (!s.Fresh) stale++; if (s.SampleSequence != last && s.Fresh) { maxGap = Math.Max(maxGap, clock.Elapsed.TotalMilliseconds - lastChange); lastChange = clock.Elapsed.TotalMilliseconds; last = s.SampleSequence; } Thread.Sleep(40); }
        var final = client.Read(); double hz = (last - start) / clock.Elapsed.TotalSeconds; Assert(client.ProcessId == pid, "Unexpected sensor restart"); Assert(stale == 0 && hz > 8, "Live freshness/rate");
        var result = new { Seconds = clock.Elapsed.TotalSeconds, Hz = hz, MaxGapMs = maxGap, Stale = stale, Threads = final.ReportedThreadCount, Gpu = final.GpuName, Fans = final.Fans.Count, CpuTemperatureSource = final.CpuTemperatureSource }; File.WriteAllText("live-result.json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })); Console.WriteLine(JsonSerializer.Serialize(result));
        var cards = final.Gpus.ToArray(); foreach (var card in cards) { options.GpuIdentifier = card.Id; client.EnsureRunning(options); Wait(() => client.Read().Fresh && client.Read().GpuName == card.Name, 15, "GPU selection " + card.Name); }
        options.GpuIdentifier = "/missing"; client.EnsureRunning(options); Wait(() => client.Read().Fresh, 15, "GPU auto fallback");
        options.FanControlPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); client.EnsureRunning(options); Wait(() => client.Read().Fresh, 15, "No Fan Control"); Thread.Sleep(1500); Assert(client.Read().FanSource != "Fan Control", "Absent Fan Control must use direct fallback");
        options.Provider = 1; client.EnsureRunning(options); Wait(() => client.Latest?.Source == "MSI Afterburner", 10, "Afterburner process source switch");
        Console.WriteLine("PASS: real sensors, explicit GPUs, missing GPU, absent Fan Control, Afterburner switch");
    }
    public static void Lifecycle(string host, string fake)
    {
        using var client = new SensorProcessClient(fake, Console.WriteLine); var options = Options(); client.EnsureRunning(options); Wait(() => client.Read().Fresh, 8, "fake sample"); int original = client.ProcessId.Value;
        using (var p = Process.GetProcessById(original)) p.Kill(); Thread.Sleep(3200); client.EnsureRunning(options); Wait(() => client.Read().Fresh, 8, "restart after crash"); Assert(client.ProcessId != original, "Crash recovery");
        options.GpuIdentifier = "freeze"; client.EnsureRunning(options); Wait(() => client.Read().Fresh, 8, "before freeze"); int frozen = client.ProcessId.Value; Thread.Sleep(8500); Assert(!client.Read().Fresh, "Stale data hidden"); client.EnsureRunning(options); Wait(() => client.ProcessId != frozen && client.Read().Fresh, 8, "watchdog recovery");
        options.GpuIdentifier = "invalid"; client.EnsureRunning(options); Thread.Sleep(500); Assert(!client.Read().Fresh, "Malformed data hidden"); int invalid = client.ProcessId.Value; Thread.Sleep(20500); client.EnsureRunning(options); Assert(client.ProcessId != invalid, "Malformed stream watchdog");
        options.GpuIdentifier = ""; client.EnsureRunning(options); Wait(() => client.Read().Fresh, 8, "back to good"); int owned = client.ProcessId.Value; client.Stop(); Wait(() => Exited(owned), 5, "owned process shutdown");
        using (var missing = new SensorProcessClient(host + ".missing", Console.WriteLine)) { missing.EnsureRunning(options); Assert(!missing.Read().Fresh && !missing.ProcessId.HasValue, "Missing executable"); }
        using var parent = Process.Start(new ProcessStartInfo { FileName = Environment.ProcessPath, ArgumentList = { "--parent-harness", Directory.GetCurrentDirectory(), host }, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
        var pidLine = parent.StandardOutput.ReadLine(); Assert(int.TryParse(pidLine, out var orphan), "Orphan PID"); Assert(parent.WaitForExit(15000) && parent.ExitCode == 0, "Parent harness exit"); Wait(() => Exited(orphan), 10, "host exits after parent");
        Console.WriteLine("PASS: crash recovery, stale watchdog, malformed stream, owned shutdown, missing exe, parent death");
    }
    public static void ParentHarness(string host) { using var p = Process.Start(new ProcessStartInfo { FileName = host, UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true }); p.StandardInput.WriteLine(JsonSerializer.Serialize(Options())); p.StandardInput.Close(); Console.WriteLine(p.Id); Console.Out.Flush(); if (p.StandardOutput.ReadLine() == null) throw new Exception("No child frame"); }
    static bool Exited(int id) { try { using var p = Process.GetProcessById(id); return p.HasExited; } catch (ArgumentException) { return true; } }
}
