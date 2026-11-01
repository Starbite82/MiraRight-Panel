// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

public static class FanData
{
    public static List<FanReading> Read(SensorSnapshot s, bool applyNames = true)
    {
        var rows = s.FanSampleTicks > 0 && Math.Abs((DateTime.UtcNow.Ticks - s.FanSampleTicks) / (double)TimeSpan.TicksPerSecond) < 5 ? s.Fans.ToList() : new List<FanReading>();
        if ((rows.Count == 0 || s.FanSource == "Libre direkt") && s.Fresh) { var gpu = s.Entries.Find(r => r.Id == 0x11 && r.Gpu == 0); if (gpu != null && gpu.Valid && gpu.Value > 0) rows.Add(new FanReading { Id = "selected-gpu", Name = "GPU", Rpm = gpu.Value }); }
        return rows.Select(f => new FanReading { Id = f.Id, Name = applyNames ? FanNames.Get(f.Id, f.Name) : f.Name, Rpm = f.Rpm }).ToList();
    }
    public static string ShortName(FanReading r) { string custom = FanNames.Get(r.Id, ""); if (custom != "") return custom; if (r.Id.StartsWith("ADLX/")) return "GPU"; return r.Name.Replace("Pump Fan", "Pumpe").Replace(" #1", "").Replace("CPU Fan", "CPU").Replace("Chipset Fan", "Chipsatz").Replace("System Fan #", "SYS ").Replace("EZ-Connect Fan", "EZ-Connect").Replace("Fan unten", "Unten"); }
}
