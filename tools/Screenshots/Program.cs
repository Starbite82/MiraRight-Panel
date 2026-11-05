// SPDX-License-Identifier: GPL-3.0-only
// Generates documentation with the real UI/renderers and explicit sample values.
using System.Reflection;
using AioDisplay.App;
using FlexPanel;

static class DocumentationScreenshots
{
    static T Field<T>(object form, string name) => (T)form.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
    static void Capture(Form form, string path) { using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(path); }
    [STAThread] static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass the repository root.");
        string repository = Path.GetFullPath(args[0]), output = Path.Combine(repository, "docs", "images");
        Directory.CreateDirectory(output);
        Log.Root = Path.Combine(repository, "work", "screenshot-fixture"); Directory.CreateDirectory(Log.Root);
        // No real sensors, elevation, USB writes or user configuration are used.
        SensorHub.HostPath = Path.Combine(Log.Root, "NoSensorHost.exe");
        AppSettings.Current = new();
        var settings = new DisplaySettings { AdminSensors = false, FanConfigPath = "" };
        settings.Thermalright.ThreadNumbers = true;
        settings.MiraBox.MiraBox.Design = "Classic";
        settings.Save();
        File.Copy(Path.Combine(repository, "NUTZUNG-UND-HAFTUNG.txt"), Path.Combine(Log.Root, "NUTZUNG-UND-HAFTUNG.txt"), true);
        File.Copy(Path.Combine(repository, "DATENSCHUTZ.txt"), Path.Combine(Log.Root, "DATENSCHUTZ.txt"), true);
        var sample = new Snapshot {
            CpuLoad=48, CpuTemp=64, CpuWatts=88.4,
            CpuThreads=Enumerable.Range(0,32).Select(i=>(double?)(6+(i*17)%91)).ToArray(),
            GpuLoad=87, GpuTemp=72, GpuWatts=225.7, GpuName="Beispiel-GPU",
            GpuClockMhz=2600, GpuMemoryTemp=62, Gpu3dLoad=85, GpuDecodeLoad=0, GpuEncodeLoad=40,
            RamUsed=24, RamTotal=64, RamFree=40, RamTemp=42, RamWatts=null,
            VramUsed=13, VramTotal=16, VramFree=3,
            Fps=132, FrameMs=7.58, FrameApp="Beispiel-Spiel.exe", SourceName="LibreHardwareMonitor",
            Fans=[new("case","Gehäuselüfter",850,"LibreHardwareMonitor",42,"","","",""),
                  new("cpu","AIO-Lüfter",1200,"LibreHardwareMonitor",64,"","","",""),
                  new("pump","AIO-Pumpe",2400,"LibreHardwareMonitor",null,"","","",""),
                  new("gpu1","GPU 1 & 2",1450,"LibreHardwareMonitor",72,"","","",""),
                  new("gpu2","GPU 3",1480,"LibreHardwareMonitor",72,"","","","")]
        };
        using var thermal = UnifiedEngine.RenderThermalright(sample, settings.Thermalright);
        using var classic = Renderer.Draw(sample, settings.MiraBox.MiraBox);
        var panoramaConfig = new PanelConfig { Design="Thermalright", ThreadNumbers=true }.Normalize();
        using var panorama = Renderer.Draw(sample, panoramaConfig);
        thermal.Save(Path.Combine(output,"miraright-panel-thermalright-dashboard.png"));
        classic.Save(Path.Combine(output,"miraright-panel-mirabox-classic.png"));
        panorama.Save(Path.Combine(output,"miraright-panel-mirabox-panorama.png"));
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using var form = new UnifiedWindow(preview:true, minimized:false);
        form.Show(); Application.DoEvents();
        Field<System.Windows.Forms.Timer>(form,"refresh").Stop();
        Field<Label>(form,"shared").Text="Dokumentationsvorschau · Beispielwerte · keine USB-Ausgabe";
        var pictures=Field<Dictionary<string,PictureBox>>(form,"pictures");
        var labels=Field<Dictionary<string,Label>>(form,"labels");
        foreach(var pair in new[]{("Thermalright",thermal),("MiraBox",classic)}) {
            pictures[pair.Item1].Image?.Dispose(); pictures[pair.Item1].Image=(Bitmap)pair.Item2.Clone();
            labels[pair.Item1].Text="Beispielwerte · USB aus\nDisplayrate und Messwertübernahme unabhängig einstellbar\nLibreHardwareMonitor";
        }
        var tabs=Field<TabControl>(form,"tabs");
        tabs.SelectedIndex=0; form.PerformLayout(); Capture(form,Path.Combine(output,"miraright-panel-einstellungen-thermalright.png"));
        tabs.SelectedIndex=1; form.PerformLayout(); Capture(form,Path.Combine(output,"miraright-panel-einstellungen-mirabox.png"));
        using(var about=new AboutWindow()) {
            about.Show(); Application.DoEvents();
            about.Controls.OfType<Button>().Single(b=>b.Text=="Nutzung & Haftung").PerformClick();
            Capture(about,Path.Combine(output,"miraright-panel-nutzung-haftung.png"));
            about.Controls.OfType<Button>().Single(b=>b.Text=="Datenschutz").PerformClick();
            Capture(about,Path.Combine(output,"miraright-panel-datenschutz.png")); about.Close();
        }
        form.PostExit(); Application.DoEvents();
        Console.WriteLine("Seven documentation images generated with sample values; no hardware or USB access.");
    }
}
