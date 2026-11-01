// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.SensorHost;

using LibreHardwareMonitor.Hardware;

// Separate slow worker: motherboard access never runs in the CPU/GPU sampling loop.
public sealed class DirectFanSource : IDisposable
{
    Computer computer;
    DateTime retryAt;
    string error = "";
    public FanSample Poll()
    {
        if (computer == null && DateTime.UtcNow < retryAt) return new FanSample { Error = error };
        try
        {
            if (computer == null)
            {
                computer = new Computer { IsMotherboardEnabled = true, IsControllerEnabled = true };
                computer.Open();
            }
            var rows = new List<FanReading>();
            foreach (var hardware in computer.Hardware) Read(hardware, rows);
            if (rows.Count == 0) { Dispose(); retryAt = DateTime.UtcNow.AddSeconds(30); }
            return new FanSample { Ticks = DateTime.UtcNow.Ticks, Fans = rows, Source = "Libre direkt" };
        }
        catch (Exception e)
        {
            error = e.Message; try { Dispose(); } catch { } retryAt = DateTime.UtcNow.AddSeconds(30);
            return new FanSample { Error = error };
        }
    }
    static void Read(IHardware hardware, List<FanReading> rows)
    {
        hardware.Update();
        foreach (var sensor in hardware.Sensors)
            if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue && float.IsFinite(sensor.Value.Value) && sensor.Value >= 0 && sensor.Value < 100000)
                rows.Add(new FanReading { Id = sensor.Identifier.ToString(), Name = sensor.Name, Rpm = sensor.Value.Value });
        foreach (var child in hardware.SubHardware) Read(child, rows);
    }
    public void Dispose() { try { computer?.Close(); } finally { computer = null; } }
}

public static class FanSelection
{
    public static bool HasReadings(FanSample sample) => sample.Ticks > 0 && Math.Abs(DateTime.UtcNow.Ticks - sample.Ticks) < 3 * TimeSpan.TicksPerSecond && sample.Fans.Any(f => f.Rpm.HasValue && double.IsFinite(f.Rpm.Value) && f.Rpm >= 0 && f.Rpm < 100000);
}
