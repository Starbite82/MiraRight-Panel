// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System.Drawing;
using System.Windows.Forms;
internal static class FanNamesChecks
{
    public static void Main()
    {
        const string id = "/lpc/test-sio/fan/10", direct = "/lpc/test-sio/0/fan/10";
        FanNames.Save(new[] { new KeyValuePair<string,string>(id,"Rückseite") });
        if (FanNames.Get(direct,"fallback") != "Rückseite") throw new Exception("Cross-provider channel name");
        FanNames.Import(new[] { new FanReading { Id = direct, Name = "System Fan #1" } });
        if (FanNames.Get(direct,"fallback") != "Rückseite") throw new Exception("Import replaced user name");
        var s = new SensorSnapshot { Fresh = true, FanSource = "Libre direkt", FanSampleTicks = DateTime.UtcNow.Ticks, Fans = new() { new FanReading { Id = direct, Name = "System Fan #1", Rpm = 900 }, new FanReading { Id = "stopped", Name = "Stehender Lüfter", Rpm = 0 } }, Entries = new() { new Reading { Id = 17, Gpu = 0, Value = 800 } } };
        var rows = FanData.Read(s);
        if (rows.Count != 3 || rows[0].Name != "Rückseite" || s.Fans[0].Name != "System Fan #1") throw new Exception("Independent names, direct GPU fan and immutable snapshot");
        using var form = new FanNamesWindow(FanData.Read(s,false)); form.Show(); Application.DoEvents();
        if (form.Controls.OfType<DataGridView>().Single().Rows.Count != 3) throw new Exception("Stopped fan not available for naming");
        using var bitmap = new Bitmap(form.Width,form.Height); form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height)); bitmap.Save("work/fan-names-preview.png"); form.Close();
        FanNames.Save(new[] { new KeyValuePair<string,string>(id,"") });
        if (FanNames.Get(direct,"fallback") != "fallback") throw new Exception("Default name reset");
        Console.WriteLine("PASS: independent UTF8 fan names, provider migration, immutable snapshots, GPU RPM and stopped fans");
    }
}
