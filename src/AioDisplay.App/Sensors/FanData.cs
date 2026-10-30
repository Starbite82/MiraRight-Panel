namespace AioDisplay.App;

public static class FanData
{
    public static List<FanReading> Read(SensorSnapshot s)
    {
        var rows = s.FanSampleTicks > 0 && Math.Abs((DateTime.UtcNow.Ticks - s.FanSampleTicks) / (double)TimeSpan.TicksPerSecond) < 5 ? s.Fans.ToList() : new List<FanReading>();
        if (rows.Count == 0 && s.Fresh) { var gpu = s.Entries.Find(r => r.Id == 0x11 && r.Gpu == 0); if (gpu != null && gpu.Valid && gpu.Value > 0) rows.Add(new FanReading { Id = "selected-gpu", Name = "GPU", Rpm = gpu.Value }); }
        return rows;
    }
    public static string ShortName(FanReading r) { if (r.Id.StartsWith("ADLX/")) return "GPU"; return r.Name.Replace("Pump Fan", "Pumpe").Replace(" #1", "").Replace("CPU Fan", "CPU").Replace("Chipset Fan", "Chipsatz").Replace("System Fan #", "SYS ").Replace("EZ-Connect Fan", "EZ-Connect").Replace("Fan unten", "Unten"); }
}
