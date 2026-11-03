// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System;
using System.Drawing;
public static class Check
{
    public static void Main()
    {
        foreach (int angle in new[] { 0, 90, 180, 270 })
        {
            using var image = new Bitmap(320, 320);
            image.SetPixel(0, 0, Color.Red);
            image.SetPixel(319, 0, Color.Lime);
            image.SetPixel(319, 319, Color.Blue);
            image.SetPixel(0, 319, Color.White);
            var encodedDisplay = PanelWindow.EncodeDisplay(image, angle);
            if (image.GetPixel(0, 0).ToArgb() != Color.Red.ToArgb() || image.GetPixel(319, 0).ToArgb() != Color.Lime.ToArgb())
                throw new Exception("Display encoding rotated the preview");
            PanelRenderer.ApplyRotation(image, angle);
            Color[] expectedCorners = angle switch
            {
                90 => new[] { Color.White, Color.Red, Color.Lime, Color.Blue },
                180 => new[] { Color.Blue, Color.White, Color.Red, Color.Lime },
                270 => new[] { Color.Lime, Color.Blue, Color.White, Color.Red },
                _ => new[] { Color.Red, Color.Lime, Color.Blue, Color.White }
            };
            Point[] corners = { new(0, 0), new(319, 0), new(319, 319), new(0, 319) };
            for (int i = 0; i < corners.Length; i++)
                if (image.GetPixel(corners[i].X, corners[i].Y).ToArgb() != expectedCorners[i].ToArgb())
                    throw new Exception("Clockwise display rotation " + angle);
            if (PanelWindow.Encode565(image).Length != 204800) throw new Exception("Rotated frame size");
            if (!encodedDisplay.SequenceEqual(PanelWindow.Encode565(image))) throw new Exception("Display-only rotation " + angle);
        }
        using (var b = new Bitmap(320, 320))
        {
            b.SetPixel(319, 319, Color.Red); b.SetPixel(0, 0, Color.Cyan); var p = PanelWindow.Encode565(b);
            if (p.Length != 204800 || p[0] != 248 || p[1] != 0 || p[204798] != 7 || p[204799] != 255) throw new Exception("RGB565 / Rotation");
        }
        var s = new SensorSnapshot(); s.Entries.Add(new Reading { Id = 128, Name = "CPU1 temperature", Value = 10 });
        var total = new Reading { Id = 128, Name = "CPU temperature", Value = 67 }; s.Entries.Add(total);
        if (s.Find(128, "CPU temperature").Value != 67) throw new Exception("CPU aggregate selection");
        total.Value = float.MaxValue; if (total.Valid) throw new Exception("FLT_MAX sentinel");
        total.Value = float.NaN; if (total.Valid) throw new Exception("NaN");
        s.Fresh = true;
        for (int i = 32; i >= 1; i--) s.Entries.Add(new Reading { Id = 0x90, Name = "CPU" + i + " usage", Value = i });
        for (int i = 1; i <= 32; i++) if (PanelRenderer.Thread(s, i).Value != i) throw new Exception("Thread mapping " + i);
        s.Entries.Add(new Reading { Id = 0x91, Name = "RAM usage", Value = 12288 });
        string expected = (12.0).ToString("0.0") + " / " + (48.0).ToString("0.0") + " GB";
        if (PanelRenderer.MemoryText(s, 48) != expected) throw new Exception("RAM used/total");
        using (var image = PanelRenderer.Render(s, 48)) if (image.Width != 320 || image.Height != 320) throw new Exception("Native resolution");
        s.Fresh = false; if (!PanelRenderer.MemoryText(s, 48).StartsWith("-- /")) throw new Exception("Stale memory reading");
        for (int mode = 0; mode <= 5; mode++) foreach (bool numbers in new[] { false, true }) using (var image = PanelRenderer.RenderView(s, 48, mode, numbers)) { if (image.Width != 320 || image.Height != 320) throw new Exception("View geometry " + mode); }
        var cold = PanelRenderer.LoadColor(0, 0); var hot = PanelRenderer.LoadColor(100, 0); if (hot.R <= cold.R || hot.B >= cold.B) throw new Exception("Load colour direction");
        s.Fresh = true; foreach (var entry in s.Entries) if (entry.Id == 0x90) entry.Value = (int.Parse(entry.Name.Substring(3, entry.Name.IndexOf(" usage") - 3)) - 1) * 100f / 31;
        using (var comparison = PanelRenderer.RenderView(s, 48, 0, false)) comparison.Save("work/comparison-test.png");
        s.Entries.Add(new Reading { Id = 0x11, Gpu = 0, Name = "GPU1 fan tachometer", Value = 700 });
        s.FanSampleTicks = DateTime.UtcNow.Ticks; s.Fans.Add(new FanReading { Id = "cpu", Name = "CPU Fan", Rpm = 555 }); s.Fans.Add(new FanReading { Id = "ADLX/gpu/fan", Name = "GPU" });
        var fans = FanData.Read(s); if (fans.Count != 2 || fans[0].Rpm != 555 || fans[1].Rpm.HasValue) throw new Exception("Fan mapping / no cross-GPU fallback");
        using (var dashboard = PanelRenderer.RenderDashboard(s, 48, false, fans)) if (dashboard.Width != 320) throw new Exception("Dashboard size");
        s.FanSampleTicks = DateTime.UtcNow.AddSeconds(-10).Ticks; if (FanData.Read(s).Exists(f => f.Id == "cpu")) throw new Exception("Stale fan data");
        using (var map = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(null, 4096)) using (var view = map.CreateViewAccessor())
        {
            view.Write(8, (uint)32); view.Write(12, (uint)1); view.Write(16, (uint)1324); view.Write(24, (uint)1); view.Write(28, (uint)1304); view.Write(2656, (uint)16777216);
            if (AfterburnerSource.ReadVramTotal(view) != 16384) throw new Exception("GPU descriptor VRAM capacity");
            view.Write(2656, (uint)0); if (AfterburnerSource.ReadVramTotal(view).HasValue) throw new Exception("Unknown VRAM capacity");
        }
        s.VramTotalMb = 16384; s.Entries.Add(new Reading { Id = 0x31, Gpu = 1, Name = "GPU2 memory usage", Value = 12345 }); s.Entries.Add(new Reading { Id = 0x31, Gpu = 0, Name = "GPU1 memory usage", Value = 8192 });
        string expectedVram = (8.0).ToString("0.0") + " / " + (16.0).ToString("0.0") + " GB"; if (PanelRenderer.VramText(s) != expectedVram) throw new Exception("VRAM GPU mapping/units");
        s.Fresh = false; if (!PanelRenderer.VramText(s).StartsWith("-- /")) throw new Exception("Stale VRAM");
        var stable = new SensorSnapshot { Fresh = true, VramTotalMb = 16384 }; var cpuLoad = new Reading { Id = 0x90, Gpu = uint.MaxValue, Name = "CPU usage", Value = 9 }; var gpuLoad = new Reading { Id = 0x30, Gpu = 0, Name = "GPU usage", Value = 9 }; stable.Entries.Add(cpuLoad); stable.Entries.Add(gpuLoad);
        using (var nine = PanelRenderer.RenderDashboard(stable, 48, false, new System.Collections.Generic.List<FanReading>()))
        {
            foreach (int value in new[] { 10, 100 })
            {
                cpuLoad.Value = value; gpuLoad.Value = value; using (var changed = PanelRenderer.RenderDashboard(stable, 48, false, new System.Collections.Generic.List<FanReading>()))
                {
                    foreach (int startX in new[] { 40, 202 }) for (int x = startX; x < startX + 15; x++) for (int y = 56; y < 79; y++) if (nine.GetPixel(x, y) != changed.GetPixel(x, y)) throw new Exception("Percentage glyph moved");
                }
            }
        }
        Console.WriteLine("PASS: RGB565, rotation, CPU aggregate, invalid values, 32 thread mappings, RAM used/total, native resolution, stale values");
    }
}
