// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.Contracts;

using System.Text.Json;
public static class SnapshotProtocol
{
    public const int MaxLineLength = 1024 * 1024;
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = false };
    public static string Serialize(SensorSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Json);
    public static bool TryRead(string line, out SensorSnapshot snapshot)
    {
        snapshot = null; if (string.IsNullOrWhiteSpace(line) || line.Length > MaxLineLength) return false;
        try
        {
            var s = JsonSerializer.Deserialize<SensorSnapshot>(line, Json);
            if (s == null || s.Protocol != SensorSnapshot.CurrentProtocol || s.SampleTicks <= 0 || s.SampleTicks > DateTime.MaxValue.Ticks || s.SampleSequence <= 0 || s.Entries == null || s.Entries.Count > 8192 || s.Fans == null || s.Fans.Count > 1024 || s.Gpus == null || s.Gpus.Count > 64 || s.ReportedThreadCount < 0 || s.ReportedThreadCount > 4096) return false;
            if (s.Entries.Any(r => r == null || r.Name == null || r.Name.Length > 512 || r.Unit == null || !r.Valid)) return false;
            if (s.Fans.Any(f => f == null || f.Id == null || f.Name == null || f.Name.Length > 512 || f.Rpm.HasValue && (!double.IsFinite(f.Rpm.Value) || f.Rpm.Value < 0 || f.Rpm.Value > 100000))) return false;
            if (s.Gpus.Any(g => g == null || g.Id == null || g.Name == null)) return false;
            if (s.VramTotalMb.HasValue && (!double.IsFinite(s.VramTotalMb.Value) || s.VramTotalMb.Value <= 0)) return false;
            snapshot = s; return true;
        }
        catch (JsonException) { return false; }
    }
    public static bool IsFresh(SensorSnapshot snapshot, DateTime now) => snapshot != null && snapshot.Fresh && snapshot.SampleTicks > 0 && snapshot.SampleTicks <= DateTime.MaxValue.Ticks && Math.Abs((now.Ticks - snapshot.SampleTicks) / (double)TimeSpan.TicksPerSecond) < 2;
}
