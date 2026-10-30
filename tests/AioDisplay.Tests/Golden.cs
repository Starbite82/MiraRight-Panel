// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
public class Golden
{
    [STAThread]
    public static void Main(string[] args)
    {
        Directory.CreateDirectory(args[0]); AppSettings.Current.HideStoppedFans = true;
        foreach (int threads in new[] { 8, 32, 64 }) foreach (bool numbers in new[] { false, true })
        {
            var s = new SensorSnapshot { Fresh = true, ReportedThreadCount = threads, VramTotalMb = 16384, SampleTicks = TimeSpan.TicksPerSecond * 10, SampleSequence = 1 };
            s.Entries.Add(new Reading { Id = 128, Name = "CPU temperature", Value = 61 }); s.Entries.Add(new Reading { Id = 144, Name = "CPU usage", Value = 9 }); s.Entries.Add(new Reading { Id = 0, Name = "GPU temperature", Value = 45 }); s.Entries.Add(new Reading { Id = 48, Name = "GPU usage", Value = 100 }); s.Entries.Add(new Reading { Id = 145, Name = "RAM usage", Value = 23456 }); s.Entries.Add(new Reading { Id = 49, Name = "GPU1 memory usage", Value = 11000 });
            for (int i = 1; i <= threads; i++) s.Entries.Add(new Reading { Id = 144, Name = "CPU" + i + " usage", Value = i * 11 % 101 });
            var fans = new List<FanReading>(); foreach (var name in new[] { "CPU", "Pumpe", "Front", "Unten", "GPU" }) fans.Add(new FanReading { Id = name, Name = name, Rpm = 1234 });
            for (int view = 0; view <= 6; view++) using (var image = view == 6 ? PanelRenderer.RenderDashboard(s, 48, numbers, fans) : PanelRenderer.RenderView(s, 48, view, numbers)) image.Save(Path.Combine(args[0], threads + "-" + numbers + "-" + view + ".png"));
        }
    }
}
