using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
public static class SettingsCheck
{
    [STAThread]
    public static void Main()
    {
        string file = AppSettings.FilePath, old = File.Exists(file) ? File.ReadAllText(file) : null;
        try
        {
            File.WriteAllLines(file, new[] { "BarWindowMs=1000", "SensorProvider=1", "View=6", "RefreshMs=250", "ThreadNumbers=1", "HideStoppedFans=0", "CloseToTray=1" }); AppSettings.Load();
            if (AppSettings.Current.BarWindowMs != 1000 || AppSettings.Current.SensorProvider != 1 || AppSettings.Current.View != 6 || AppSettings.Current.RefreshMs != 250 || !AppSettings.Current.ThreadNumbers || AppSettings.Current.HideStoppedFans || !AppSettings.Current.CloseToTray) throw new Exception("Settings parse");
            AppSettings.Current.FanControlPath = @"D:\Portable\Fan Control"; AppSettings.Current.GpuIdentifier = "/gpu-nvidia/1"; AppSettings.Save(); AppSettings.Load(); if (AppSettings.Current.FanControlPath != @"D:\Portable\Fan Control" || AppSettings.Current.GpuIdentifier != "/gpu-nvidia/1") throw new Exception("Hardware path roundtrip"); if (AppSettings.Current.HideStoppedFans) throw new Exception("Settings roundtrip");
            foreach (int hzPeriod in new[] { 16, 33, 100, 125, 250, 500, 1000 }) { File.WriteAllLines(file, new[] { "RefreshMs=" + hzPeriod }); AppSettings.Load(); if (AppSettings.Current.RefreshMs != hzPeriod) throw new Exception("Refresh interval " + hzPeriod); }
            File.WriteAllLines(file, new[] { "View=999", "RefreshMs=5" }); AppSettings.Load(); if (AppSettings.Current.View != 6 || AppSettings.Current.RefreshMs != 500) throw new Exception("Settings bounds");
            if (!AutoStart.Command.EndsWith(" --minimized") || AutoStart.Command[0] != '\"') throw new Exception("Autostart command");
            AppSettings.Current.ThreadNumbers = true; Application.EnableVisualStyles();
            using (var form = new PanelWindow(true, false))
            {
                using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height)); image.Save("work/settings-preview.png"); }
                var e = new FormClosingEventArgs(CloseReason.UserClosing, false); typeof(PanelWindow).GetMethod("OnFormClosing", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { e }); if (!e.Cancel) throw new Exception("Close to tray");
            }
            using (var form = new PanelWindow(true, true)) { form.Show(); if (form.Visible || form.ShowInTaskbar) throw new Exception("Minimized startup is visible"); }
            Console.WriteLine("PASS: settings roundtrip, bounds, autostart quoting, close to tray, hidden startup");
        }
        finally { if (old != null) File.WriteAllText(file, old); else File.Delete(file); }
    }
}
