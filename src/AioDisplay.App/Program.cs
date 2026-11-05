// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
public static class Program
{
    static bool previewMode;
    static string ShowSignal => previewMode ? "Local\\AioUnifiedPreviewShow" : "Local\\AioUnifiedShow";
    static string StopSignal => previewMode ? "Local\\AioUnifiedPreviewStop" : "Local\\AioUnifiedStop";
    static void Signal(string name) { try { using (var e = EventWaitHandle.OpenExisting(name)) e.Set(); } catch (WaitHandleCannotBeOpenedException) { } }
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            previewMode = args.Contains("--preview"); AppSettings.Load();
            if (args.Contains("--stop")) { Signal(StopSignal); return; }
            if (!previewMode && args.Contains("--enable-autostart")) { AutoStart.Set(true); return; }
            if (!previewMode && args.Contains("--disable-autostart")) { AutoStart.Set(false); return; }
            if (args.Contains("--render"))
            {
                try
                {
                    SensorHub.EnsureRunning();
                    var timeout = Stopwatch.StartNew();
                    while (!SensorHub.Read().Fresh && timeout.Elapsed < TimeSpan.FromSeconds(20)) Thread.Sleep(50);
                    using var image = PanelRenderer.RenderView(SensorHub.Read(), PanelRenderer.TotalMemoryGiB(), AppSettings.Current.View, AppSettings.Current.ThreadNumbers);
                    image.Save(Path.Combine(Log.Root, "live-preview.png"), ImageFormat.Png);
                }
                finally { SensorHub.Stop(); }
                return;
            }
            if (args.Contains("--diagnose"))
            {
                var s = SensorHub.Read(); File.WriteAllLines(Path.Combine(Log.Root, "sensors.txt"), new[] { s.Status + " " + Log.LastSensorError }.Concat(s.Entries.Select(r => string.Format("{0:X4} GPU={1} {2} = {3} {4}", r.Id, r.Gpu, r.Name, r.Value, r.Unit))));
                using (var usb = new UsbDisplay()) { usb.Open(); var response = usb.Handshake(); Log.Write("PROBE PM=" + usb.Pm + " SUB=" + usb.Sub + " " + BitConverter.ToString(response)); }
                return;
            }
            bool first; using (var mutex = new Mutex(true, previewMode ? "Local\\AioUnifiedPreview" : "Local\\AioUnified", out first))
            {
                if (!first) { if (!args.Contains("--minimized")) Signal(ShowSignal); return; }
                AppSettings.Save();
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                using (var show = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSignal)) using (var stop = new EventWaitHandle(false, EventResetMode.AutoReset, StopSignal))
                using (var form = new UnifiedWindow(args.Contains("--preview"), args.Contains("--minimized"), args.Contains("--verify")))
                {
                    var openWait = ThreadPool.RegisterWaitForSingleObject(show, (o, t) => form.PostOpen(), null, -1, false);
                    var stopWait = ThreadPool.RegisterWaitForSingleObject(stop, (o, t) => form.PostExit(), null, -1, false);
                    try { Application.Run(form); } finally { openWait.Unregister(null); stopWait.Unregister(null); }
                }
            }
        }
        catch (Exception e) { Log.Write("ERROR " + e); File.WriteAllText(Path.Combine(Log.Root, "error.txt"), e.ToString()); }
    }
}
