using System;
using System.Drawing;
using System.Collections.Generic;
static class HardwareCheck
{
    static void Assert(bool ok, string text) { if (!ok) throw new Exception(text); }
    [STAThread]
    static void Main()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        foreach (int n in new[] { 1, 4, 8, 12, 16, 24, 32, 48, 64, 128 })
        {
            var s = new SensorSnapshot { Fresh = true, ReportedThreadCount = n, SampleTicks = DateTime.UtcNow.Ticks };
            for (int i = 1; i <= n; i++) s.Entries.Add(new Reading { Id = 0x90, Name = "CPU" + i + " usage", Value = i % 101 });
            Assert(PanelRenderer.ThreadCount(s) == n, "Thread count " + n);
            var avg = new BarAverager(); avg.Apply(s, 500, 0); Assert(s.BarValues.ContainsKey("thread:" + n), "Last thread averaged " + n);
            var visited = new HashSet<int>(); for (int t = 0; t < ((n + 31) / 32); t++)
            {
                int start = PanelRenderer.PageStart(n, 32, t * 5L * TimeSpan.TicksPerSecond); for (int j = start; j < Math.Min(n, start + 32); j++) visited.Add(j); s.SampleTicks = t * 5L * TimeSpan.TicksPerSecond;
                for (int view = 0; view <= 6; view++) using (var b = PanelRenderer.RenderView(s, 48, view, true)) Assert(b.Width == 320 && b.Height == 320, "Render " + view);
            }
            Assert(visited.Count == n, "All threads reachable " + n);
        }
        var fans = new List<FanReading>(); for (int i = 0; i < 25; i++) fans.Add(new FanReading { Id = "fan" + i, Name = "Fan " + i, Rpm = 1000 + i });
        var seen = new HashSet<int>(); for (int t = 0; t < 4; t++) { int start = PanelRenderer.PageStart(fans.Count, 8, t * 5L * TimeSpan.TicksPerSecond); for (int i = start; i < Math.Min(start + 8, fans.Count); i++) seen.Add(i); using (var b = PanelRenderer.RenderDashboard(new SensorSnapshot { SampleTicks = t * 5L * TimeSpan.TicksPerSecond }, 32, true, fans)) { } }
        Assert(seen.Count == 25, "All fans reachable");
        using (var form = new HardwareWindow()) { Assert(form.Controls.Count >= 9, "Hardware controls"); form.Show(); System.Windows.Forms.Application.DoEvents(); using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height)); image.Save("work/hardware-preview.png"); } form.Close(); }
        Console.WriteLine("PASS: 1–128 threads, every view, smoothing, pagination, 25 fans, hardware dialog");
    }
}
