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
public sealed class PanelWindow : Form
{
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window, int command);
    readonly BarAverager barAverager = new BarAverager();
    readonly PictureBox picture = new PictureBox(); readonly Label status = new Label(); NotifyIcon tray; System.Threading.Timer timer; Thread streamThread; readonly ManualResetEvent stopStream = new ManualResetEvent(false); volatile byte[] preparedPixels; volatile string streamError = ""; int busy;
    volatile bool closing; bool preview, exitRequested, suppressShow; long frames; string lastError = ""; DateTime lastLog = DateTime.MinValue;
    public PanelWindow(bool previewOnly, bool minimized)
    {
        preview = previewOnly; suppressShow = minimized; Icon = AppBrand.Icon; Text = "AIO Display — Einstellungen"; ClientSize = new Size(700, 562); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; BackColor = Color.FromArgb(9, 15, 25); Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterScreen; ShowInTaskbar = !minimized;
        Controls.Add(new Label { Text = "Live-Vorschau", Bounds = new Rectangle(20, 12, 320, 28), ForeColor = Color.White, Font = new Font("Segoe UI", 13, FontStyle.Bold) });
        picture.Bounds = new Rectangle(20, 49, 320, 320); Controls.Add(picture);
        status.Bounds = new Rectangle(20, 380, 320, 92); status.ForeColor = Color.LightSteelBlue; status.Font = new Font("Segoe UI", 9); Controls.Add(status);
        Controls.Add(new Label { Text = "Einstellungen", Bounds = new Rectangle(365, 14, 310, 30), ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold) });
        Controls.Add(new Label { Text = "Sensorquelle", Bounds = new Rectangle(365, 51, 96, 25), ForeColor = Color.LightSteelBlue });
        var source = new ComboBox { Bounds = new Rectangle(463, 48, 212, 28), DropDownStyle = ComboBoxStyle.DropDownList }; source.Items.AddRange(new object[] { "LibreHardwareMonitor", "MSI Afterburner" }); source.SelectedIndex = AppSettings.Current.SensorProvider; Controls.Add(source);
        Controls.Add(new Label { Text = "Displayansicht", Bounds = new Rectangle(365, 95, 300, 22), ForeColor = Color.LightSteelBlue });
        int[] views = { 6, 1, 0, 2, 3, 4, 5 }; var selector = new ComboBox { Bounds = new Rectangle(365, 120, 310, 28), DropDownStyle = ComboBoxStyle.DropDownList }; selector.Items.AddRange(new object[] { "Dashboard mit CPU und Lüftern", "CPU-Detailansicht", "Vier CPU-Varianten vergleichen", "A · Cyan nach Rot", "B · Kompakte Balken", "C · Farbsegmente", "D · Schmale Linien" }); selector.SelectedIndex = Array.IndexOf(views, AppSettings.Current.View); Controls.Add(selector);
        var numbers = Check("Threadnummern anzeigen", 162, AppSettings.Current.ThreadNumbers);
        var fans = Check("Lüfter mit 0 RPM ausblenden", 193, AppSettings.Current.HideStoppedFans);
        Controls.Add(new Label { Text = "Aktualisierung", Bounds = new Rectangle(365, 235, 140, 23), ForeColor = Color.LightSteelBlue });
        var rate = new ComboBox { Bounds = new Rectangle(512, 231, 163, 28), DropDownStyle = ComboBoxStyle.DropDownList }; rate.Items.AddRange(new object[] { "1 × pro Sekunde", "2 × pro Sekunde", "4 × pro Sekunde", "8 × pro Sekunde", "10 × pro Sekunde", "30 × pro Sekunde", "60 × pro Sekunde" }); int[] rates = { 1000, 500, 250, 125, 100, 33, 16 }; rate.SelectedIndex = Array.IndexOf(rates, AppSettings.Current.RefreshMs); Controls.Add(rate);
        Controls.Add(new Label { Text = "LHM-Messrate", Bounds = new Rectangle(365, 273, 140, 23), ForeColor = Color.LightSteelBlue });
        var sensorRate = new ComboBox { Bounds = new Rectangle(512, 269, 163, 28), DropDownStyle = ComboBoxStyle.DropDownList }; sensorRate.Items.AddRange(new object[] { "1 Hz · 1000 ms", "2 Hz · 500 ms", "4 Hz · 250 ms", "8 Hz · 125 ms", "10 Hz · 100 ms" }); sensorRate.SelectedIndex = Array.IndexOf(rates, AppSettings.Current.SensorMs); sensorRate.Enabled = AppSettings.Current.SensorProvider == 0; Controls.Add(sensorRate);
        Controls.Add(new Label { Text = "Balkenglättung", Bounds = new Rectangle(365, 311, 140, 23), ForeColor = Color.LightSteelBlue });
        var smoothing = new ComboBox { Bounds = new Rectangle(512, 307, 163, 28), DropDownStyle = ComboBoxStyle.DropDownList }; smoothing.Items.AddRange(new object[] { "Aus", "Leicht · 0,25 s", "Mittel · 0,5 s", "Ruhig · 1 s", "Stark · 2 s" }); int[] windows = { 0, 250, 500, 1000, 2000 }; smoothing.SelectedIndex = Array.IndexOf(windows, AppSettings.Current.BarWindowMs); Controls.Add(smoothing);
        var auto = Check("Mit Windows starten (minimiert)", 353, AutoStart.Enabled);
        auto.Enabled = !preview;
        var close = Check("Beim Schließen weiterlaufen", 384, AppSettings.Current.CloseToTray);
        var note = new Label { Text = "Einstellungen werden sofort gespeichert.\nLHM-Messrate separat einstellbar.\nHardware-Erkennung unter „Hardware & Pfade“.", Bounds = new Rectangle(365, 427, 310, 67), ForeColor = Color.LightSteelBlue, Font = new Font("Segoe UI", 9) }; Controls.Add(note);
        var about = new Button { Text = "Info & Impressum", Bounds = new Rectangle(20, 477, 320, 30) };
        about.Click += (sender, e) => { using var dialog = new AboutWindow(); dialog.ShowDialog(this); }; Controls.Add(about);
        var hardware = new Button { Text = "Hardware & Pfade…", Bounds = new Rectangle(20, 513, 320, 30) };
        hardware.Click += (a, b) => { using (var dialog = new HardwareWindow()) if (dialog.ShowDialog(this) == DialogResult.OK) { SensorHub.Stop(); } }; Controls.Add(hardware);
        var hide = new Button { Text = "In den Infobereich", Bounds = new Rectangle(365, 513, 166, 30) }; hide.Click += (a, b) => HideToTray(); Controls.Add(hide);
        var exit = new Button { Text = "Beenden", Bounds = new Rectangle(543, 513, 132, 30) }; exit.Click += (a, b) => ExitNow(); Controls.Add(exit);
        Action save = () => { try { AppSettings.Save(); } catch (Exception e) { note.Text = "Speichern nicht möglich: " + e.Message; } };
        smoothing.SelectedIndexChanged += (a, b) => { AppSettings.Current.BarWindowMs = windows[smoothing.SelectedIndex]; save(); };
        source.SelectedIndexChanged += (a, b) => { AppSettings.Current.SensorProvider = source.SelectedIndex; sensorRate.Enabled = source.SelectedIndex == 0; save(); };
        sensorRate.SelectedIndexChanged += (a, b) => { AppSettings.Current.SensorMs = rates[sensorRate.SelectedIndex]; save(); };
        selector.SelectedIndexChanged += (a, b) => { AppSettings.Current.View = views[selector.SelectedIndex]; numbers.Enabled = AppSettings.Current.View != 1; fans.Enabled = AppSettings.Current.View == 6; save(); }; numbers.Enabled = AppSettings.Current.View != 1; fans.Enabled = AppSettings.Current.View == 6;
        numbers.CheckedChanged += (a, b) => { AppSettings.Current.ThreadNumbers = numbers.Checked; save(); }; fans.CheckedChanged += (a, b) => { AppSettings.Current.HideStoppedFans = fans.Checked; save(); };
        rate.SelectedIndexChanged += (a, b) => { AppSettings.Current.RefreshMs = rates[rate.SelectedIndex]; if (timer != null) timer.Change(0, Math.Max(100, AppSettings.Current.RefreshMs)); save(); };
        auto.CheckedChanged += (a, b) => { try { AutoStart.Set(auto.Checked); } catch (Exception e) { note.Text = "Autostart nicht geändert: " + e.Message; } save(); };
        close.CheckedChanged += (a, b) => { AppSettings.Current.CloseToTray = close.Checked; save(); };
        var menu = new ContextMenuStrip(); menu.Items.Add("Einstellungen öffnen", null, (a, b) => OpenSettings()); menu.Items.Add("Info & Impressum", null, (a, b) => { using var dialog = new AboutWindow(); dialog.ShowDialog(this); }); menu.Items.Add("Beenden", null, (a, b) => ExitNow());
        tray = new NotifyIcon { Icon = AppBrand.Icon, Text = "AIO Display", Visible = true, ContextMenuStrip = menu }; tray.DoubleClick += (a, b) => OpenSettings();
        UiTheme.Apply(this, menu);
        Resize += (a, b) => { if (WindowState == FormWindowState.Minimized) HideToTray(); };
        var windowHandle = Handle;
        BeginInvoke((Action)(() => { SensorHub.EnsureRunning(); if (!preview) { streamThread = new Thread(StreamLoop) { IsBackground = true, Name = "AIO LCD stream" }; streamThread.Start(); } timer = new System.Threading.Timer(Tick, null, 0, Math.Max(100, AppSettings.Current.RefreshMs)); Log.Write(minimized ? "START minimized" : "START settings"); if (!minimized) OpenSettings(); }));
    }
    CheckBox Check(string text, int y, bool value) { var check = new ReadableCheckBox { Text = text, Bounds = new Rectangle(365, y, 320, 28), ForeColor = Color.White, Checked = value }; Controls.Add(check); return check; }
    protected override void SetVisibleCore(bool value) { base.SetVisibleCore(suppressShow ? false : value); }
    public void PostOpen() { if (!closing) try { BeginInvoke((Action)OpenSettings); } catch (InvalidOperationException) { } }
    public void PostExit() { if (!closing) try { BeginInvoke((Action)ExitNow); } catch (InvalidOperationException) { } }
    void OpenSettings() { if (closing) return; suppressShow = false; ShowInTaskbar = true; WindowState = FormWindowState.Normal; Show(); ShowWindow(Handle, 5); Activate(); }
    void HideToTray() { ShowInTaskbar = false; Hide(); }
    void ExitNow() { exitRequested = true; Close(); }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !exitRequested && AppSettings.Current.CloseToTray) { e.Cancel = true; HideToTray(); return; }
        closing = true; stopStream.Set(); if (timer != null) timer.Dispose(); base.OnFormClosing(e);
    }
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        bool stopped = streamThread == null || streamThread.Join(2500); if (stopped) stopStream.Dispose(); SensorHub.Stop(); tray.Dispose(); if (picture.Image != null) picture.Image.Dispose(); base.OnFormClosed(e);
    }
    string Value(SensorSnapshot s, uint id, string name, string suffix) { var r = s.Find(id, name); return s.Fresh && r != null && r.Valid ? r.Value.ToString("0") + suffix : "--"; }
    public static byte[] Encode565(Bitmap bmp)
    {
        var pixels = new byte[320 * 320 * 2]; var rectangle = new Rectangle(0, 0, 320, 320);
        var bitmapData = bmp.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[320 * 4]; for (int y = 0; y < 320; y++)
            {
                Marshal.Copy(IntPtr.Add(bitmapData.Scan0, (319 - y) * bitmapData.Stride), row, 0, row.Length);
                for (int x = 0; x < 320; x++) { int source = (319 - x) * 4; ushort rgb = (ushort)(((row[source + 2] >> 3) << 11) | ((row[source + 1] >> 2) << 5) | (row[source] >> 3)); int offset = (y * 320 + x) * 2; pixels[offset] = (byte)(rgb >> 8); pixels[offset + 1] = (byte)rgb; }
            }
        }
        finally { bmp.UnlockBits(bitmapData); }
        return pixels;
    }

    void StreamLoop()
    {
        UsbDisplay connection = null; var clock = Stopwatch.StartNew(); double due = 0; string loggedError = "";
        try
        {
            while (!closing && !stopStream.WaitOne(0))
            {
                var pixels = preparedPixels; if (pixels == null) { if (stopStream.WaitOne(20)) break; due = clock.Elapsed.TotalMilliseconds; continue; }
                try
                {
                    if (connection == null) { if (Process.GetProcessesByName("TRCC").Length > 0 || Process.GetProcessesByName("USBLCDNEW").Length > 0) throw new IOException("Bitte TRCC beenden."); connection = new UsbDisplay(); connection.Open(); connection.Handshake(); Log.Write("CONNECTED PM=" + connection.Pm + " SUB=" + connection.Sub); }
                    connection.Send(pixels); Interlocked.Increment(ref frames); streamError = ""; if (loggedError != "") { Log.Write("RECOVERED"); loggedError = ""; }
                }
                catch (Exception e) { streamError = e.Message; if (loggedError != e.Message) { Log.Write("USB ERROR " + e.Message); loggedError = e.Message; } if (connection != null) { connection.Dispose(); connection = null; } if (stopStream.WaitOne(1000)) break; due = clock.Elapsed.TotalMilliseconds; continue; }
                double period = AppSettings.Current.RefreshMs == 16 ? 1000.0 / 60 : AppSettings.Current.RefreshMs == 33 ? 1000.0 / 30 : AppSettings.Current.RefreshMs;
                due += period; double remaining = due - clock.Elapsed.TotalMilliseconds;
                if (remaining > 0) { if (stopStream.WaitOne(Math.Max(1, (int)remaining))) break; } else if (remaining < -period) due = clock.Elapsed.TotalMilliseconds;
            }
        }
        finally { if (connection != null) connection.Dispose(); }
    }

    void Tick(object ignored)
    {
        if (closing || Interlocked.CompareExchange(ref busy, 1, 0) != 0) return; Bitmap bmp = null;
        try
        {
            SensorHub.EnsureRunning(); var sensors = SensorHub.Read(); barAverager.Apply(sensors, AppSettings.Current.BarWindowMs); bmp = PanelRenderer.RenderView(sensors, PanelRenderer.TotalMemoryGiB(), AppSettings.Current.View, AppSettings.Current.ThreadNumbers);
            if (!preview) preparedPixels = Encode565(bmp);
            bool fanFresh = FanData.Read(sensors).Any(f => f.Rpm.HasValue); string info = (preview ? "Vorschau ohne Display" : streamError != "" ? "Warte: " + streamError : Interlocked.Read(ref frames) > 0 ? "Display verbunden" : "Display startet") + "\n" + sensors.Source + " " + (sensors.Fresh ? "aktiv" : "wartet") + "\nCPU-Temperatur: " + sensors.CpuTemperatureSource + "\nFan Control: " + (fanFresh ? "aktiv" : "wartet");
            if (lastError != "") { Log.Write("RECOVERED"); lastError = ""; }
            if ((DateTime.Now - lastLog).TotalSeconds >= 5) { lastLog = DateTime.Now; File.WriteAllText(Path.Combine(Log.Root, "status.txt"), DateTime.Now.ToString("s") + " USB " + frames + " Frames | " + info.Replace("\n", " | ") + " | CPU=" + Value(sensors, 0x80, "CPU temperature", "°C") + " GPU=" + Value(sensors, 0, "GPU temperature", "°C") + " | Sample=" + sensors.SampleSequence + "@" + sensors.SampleTicks); bmp.Save(Path.Combine(Log.Root, "live-preview.png"), ImageFormat.Png); }
            var display = bmp; bmp = null; if (!closing) BeginInvoke((Action)(() => { if (closing) { display.Dispose(); return; } var old = picture.Image; picture.Image = display; if (old != null) old.Dispose(); status.Text = info; })); else display.Dispose();
        }
        catch (Exception e)
        {
            if (e.Message != lastError) { lastError = e.Message; Log.Write("RENDER ERROR " + e.Message); }
            if (!closing) try { BeginInvoke((Action)(() => status.Text = "Warte auf das Display\n" + lastError)); } catch (InvalidOperationException) { }
        }
        finally { if (bmp != null) bmp.Dispose(); Interlocked.Exchange(ref busy, 0); }
    }
}
