// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.Contracts;

using System.Text.Json.Serialization;
public sealed class Reading
{
    public string Name { get; set; } = ""; public string Unit { get; set; } = ""; public float Value { get; set; }
    public uint Id { get; set; }
    public uint Gpu { get; set; }
    [JsonIgnore] public bool Valid => float.IsFinite(Value) && Math.Abs(Value) < 1e20;
}
public sealed class FanReading { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public double? Rpm { get; set; } }
public sealed class GpuDevice { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public double? TotalMb { get; set; } }
public sealed class SensorSnapshot
{
    public const int CurrentProtocol = 1;
    public int Protocol { get; set; } = CurrentProtocol;
    public List<Reading> Entries { get; set; } = new(); public List<FanReading> Fans { get; set; } = new(); public List<GpuDevice> Gpus { get; set; } = new();
    public string Status { get; set; } = "SENSOREN NICHT BEREIT"; public bool Fresh { get; set; }
    public double? VramTotalMb { get; set; }
    public string Source { get; set; } = "LibreHardwareMonitor"; public string CpuTemperatureSource { get; set; } = "--"; public string GpuName { get; set; } = ""; public string Error { get; set; } = "";
    public long SampleTicks { get; set; }
    public long SampleSequence { get; set; }
    public long FanSampleTicks { get; set; }
    public string FanSource { get; set; } = "";
    public int ReportedThreadCount { get; set; }
    [JsonIgnore] public Dictionary<string, double> BarValues { get; } = new();
    public Reading Find(uint id, string exact) { var r = Entries.Find(x => x.Id == id && x.Name.Equals(exact, StringComparison.OrdinalIgnoreCase)); return r ?? Entries.Find(x => x.Id == id); }
}
public sealed class SensorOptions
{
    public int Protocol { get; set; } = SensorSnapshot.CurrentProtocol; public int Provider { get; set; }
    public int IntervalMs { get; set; } = 100;
    public string FanControlPath { get; set; } = ""; public string GpuIdentifier { get; set; } = ""; public int ParentId { get; set; }
    public long ParentStartTicks { get; set; }
    public bool UseFanControl { get; set; } = true;
    public void Validate() { if (Protocol != SensorSnapshot.CurrentProtocol) throw new InvalidDataException("Unsupported sensor protocol"); if (Provider != 0 && Provider != 1) throw new InvalidDataException("Unknown provider"); if (!new[] { 100, 125, 250, 500, 1000 }.Contains(IntervalMs)) throw new InvalidDataException("Unsupported sensor interval"); }
}
