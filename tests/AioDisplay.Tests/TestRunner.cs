// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System.Reflection;
using System.Text.Json;
using System.Drawing;
internal static class TestRunner
{
    [STAThread]
    static int Main(string[] args)
    {
        var output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(AppContext.BaseDirectory, "results"); Directory.CreateDirectory(output); Directory.SetCurrentDirectory(output); Directory.CreateDirectory("work"); Log.Root = output;
        try
        {
            if (args.FirstOrDefault() == "--elevated") { IntegrationChecks.Elevated(args[2]); return 0; }
            if (args.FirstOrDefault() == "--probe-direct") { using var direct = new AioDisplay.SensorHost.DirectFanSource(); using var identity = System.Security.Principal.WindowsIdentity.GetCurrent(); bool admin = new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); var samples = new List<AioDisplay.SensorHost.FanSample>(); for(int i=0;i<4;i++){samples.Add(direct.Poll());Thread.Sleep(1000);} File.WriteAllText(Path.Combine(output,"direct-admin-probe.json"),JsonSerializer.Serialize(new {Admin=admin,Samples=samples},new JsonSerializerOptions{WriteIndented=true})); return 0; }
            if (args.FirstOrDefault() == "--probe-fans") { using var direct = new AioDisplay.SensorHost.DirectFanSource(); var fc = new AioDisplay.SensorHost.FanControlSource(""); for(int i=0;i<3;i++) { Console.WriteLine("DIRECT " + JsonSerializer.Serialize(direct.Poll())); Console.WriteLine("FANCONTROL " + JsonSerializer.Serialize(fc.Poll())); Thread.Sleep(1000); } return 0; }
            if (args.FirstOrDefault() == "--discover") { foreach (string name in new[] { "FanControl.exe", "MSIAfterburner.exe", "LibreHardwareMonitor.exe" }) Console.WriteLine(name + ": " + (SoftwareDiscovery.Find(name, name == "FanControl.exe" ? "FanControl.IPC.dll" : null) ?? "not found")); return 0; }
            if (args.FirstOrDefault() == "--golden") { Golden.Main(new[] { output }); return 0; }
            if (args.FirstOrDefault() == "--live") { IntegrationChecks.Live(args[2], int.Parse(args[3])); return 0; }
            if (args.FirstOrDefault() == "--lifecycle") { IntegrationChecks.Lifecycle(args[2], args[3]); return 0; }
            if (args.FirstOrDefault() == "--parent-harness") { IntegrationChecks.ParentHarness(args[2]); return 0; }
            foreach (var type in new[] { typeof(Check), typeof(SettingsCheck), typeof(BarAveragerCheck), typeof(HardwareCheck), typeof(SnapshotFilesCheck), typeof(AboutChecks) }) type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, null);
            DiscoveryChecks.Main(); FanNamesChecks.Main(); ProtocolChecks(); UnifiedChecks.Run(); Console.WriteLine("ALL REGRESSION TESTS PASSED"); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e is TargetInvocationException ? e.InnerException : e); return 1; }
        finally { SensorHub.Stop(); }
    }
    static void Assert(bool ok, string what) { if (!ok) throw new Exception(what); }
    static void ProtocolChecks()
    {
        var s = new SensorSnapshot { Fresh = true, SampleTicks = DateTime.UtcNow.Ticks, SampleSequence = 1, ReportedThreadCount = 32 }; s.Entries.Add(new Reading { Id = 144, Name = "CPU usage", Value = 42 }); s.Fans.Add(new FanReading { Id = "fan", Name = "Rückseite", Rpm = 900 });
        Assert(SnapshotProtocol.TryRead(SnapshotProtocol.Serialize(s), out var parsed) && parsed.Fans[0].Name == "Rückseite", "IPC UTF8 roundtrip"); Assert(SnapshotProtocol.IsFresh(parsed, DateTime.UtcNow), "IPC fresh"); Assert(!SnapshotProtocol.IsFresh(parsed, DateTime.UtcNow.AddSeconds(3)), "IPC stale");
        foreach (var invalid in new[] { "{", "null", "", new string('x', SnapshotProtocol.MaxLineLength + 1), "{\"Protocol\":9}", "{\"Protocol\":1,\"SampleTicks\":9223372036854775807}" }) Assert(!SnapshotProtocol.TryRead(invalid, out _), "Malformed IPC accepted");
        s.Protocol = 99; Assert(!SnapshotProtocol.TryRead(SnapshotProtocol.Serialize(s), out _), "Unsupported protocol"); s.Protocol = 1; s.Entries[0].Value = float.MaxValue; Assert(!SnapshotProtocol.TryRead(SnapshotProtocol.Serialize(s), out _), "Invalid readings");
        Assert(MemoryUnits.Divisor(false, "Data") == 1048576 && MemoryUnits.Divisor(true, "Data") == 1.0 / 1024 && MemoryUnits.Divisor(true, "SmallData") == 1, "Memory units");
        Console.WriteLine("PASS: versioned IPC, UTF8, freshness, invalid frames, memory units");
    }
}
