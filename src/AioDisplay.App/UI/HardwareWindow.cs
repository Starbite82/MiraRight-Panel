// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
public sealed class HardwareWindow : Form
{
    readonly List<string> ids = new List<string>(); readonly ComboBox gpu = new ComboBox();
    TextBox fan;
    readonly TextBox detected = new TextBox { Multiline = true, ReadOnly = true, BackColor = UiTheme.Surface, ForeColor = UiTheme.Foreground, ScrollBars = ScrollBars.Vertical, Bounds = new Rectangle(18, 292, 604, 135) };
    readonly Button detect = new Button { Text = "Automatisch suchen", Bounds = new Rectangle(18, 250, 190, 30) };
    public HardwareWindow()
    {
        Text = "Hardware & Pfade"; Icon = AppBrand.Icon; ClientSize = new Size(640, 490); BackColor = Color.FromArgb(9, 15, 25); ForeColor = UiTheme.Foreground; Font = new Font("Segoe UI", 10); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new Label { Text = "Leere Pfade = automatisch erkennen. Portable Ordner sind ebenfalls möglich.", Bounds = new Rectangle(18, 15, 604, 40) });
        fan = PathRow("Fan Control (optional, Ordner)", 60, AppSettings.Current.FanControlPath);
        Controls.Add(new Label { Text = "Grafikkarte (LibreHardwareMonitor)", Bounds = new Rectangle(18, 135, 440, 23) });
        gpu.Bounds = new Rectangle(18, 160, 604, 28); gpu.DropDownStyle = ComboBoxStyle.DropDownList; Controls.Add(gpu);
        ids.Add(""); gpu.Items.Add("Automatisch – größter Grafikspeicher");
        try { foreach (var card in SensorHub.Gpus) { if (!ids.Contains(card.Id)) { ids.Add(card.Id); gpu.Items.Add(card.Name + " (" + card.Id + ")"); } } } catch (IOException) { }
        string selected = AppSettings.Current.GpuIdentifier; if (!ids.Contains(selected)) { ids.Add(selected); gpu.Items.Add(selected + " (zurzeit nicht erkannt)"); }
        gpu.SelectedIndex = ids.IndexOf(selected);
        Controls.Add(new Label { Text = "Übernehmen startet die Sensor-Erkennung neu. Fehlende Werte erscheinen als --.", Bounds = new Rectangle(18, 203, 604, 44), ForeColor = UiTheme.Inactive });
        Controls.Add(detected); Controls.Add(detect); detect.Click += async (a, b) => await Detect(); Shown += async (a, b) => await Detect();
        var save = new Button { Text = "Übernehmen", Bounds = new Rectangle(347, 445, 135, 30) }; save.Click += (a, b) => Save(); Controls.Add(save);
        var cancel = new Button { Text = "Abbrechen", Bounds = new Rectangle(492, 445, 130, 30), DialogResult = DialogResult.Cancel }; Controls.Add(cancel); CancelButton = cancel;
        using (var menu = new ContextMenuStrip()) UiTheme.Apply(this, menu);
    }
    async System.Threading.Tasks.Task Detect()
    {
        detect.Enabled = false; detected.Text = "Programme werden gesucht…";
        try
        {
            var result = await System.Threading.Tasks.Task.Run(() => new[] { SoftwareDiscovery.Find("FanControl.exe", "FanControl.IPC.dll"), SoftwareDiscovery.Find("MSIAfterburner.exe"), SoftwareDiscovery.Find("LibreHardwareMonitor.exe") });
            if (IsDisposed) return;
            string bundled = Path.Combine(AppContext.BaseDirectory, "sensors", "LibreHardwareMonitorLib.dll");
            detected.Text = "Libre-Sensorbibliothek (integriert): " + (File.Exists(bundled) ? bundled : "im Entwicklungsbuild beim Sensorprozess") + Environment.NewLine
                + "FanControl: " + (result[0] ?? "nicht gefunden – optional / portablen Ordner wählen") + Environment.NewLine
                + "MSI Afterburner: " + (result[1] ?? "nicht gefunden – optional") + Environment.NewLine
                + "Libre-Oberfläche (nicht benötigt): " + (result[2] ?? "nicht installiert / nicht gefunden") + Environment.NewLine
                + "Externe Sensorprogramme müssen laufen. Afterburner wird über Shared Memory gelesen; dafür ist kein Pfad nötig.";
        }
        catch (Exception e) { if (!IsDisposed) detected.Text = "Suche nicht vollständig: " + e.Message; }
        finally { if (!IsDisposed) detect.Enabled = true; }
    }
    TextBox PathRow(string title, int y, string value)
    {
        Controls.Add(new Label { Text = title, Bounds = new Rectangle(18, y, 580, 23) });
        var box = new TextBox { Text = value, Bounds = new Rectangle(18, y + 25, 500, 28), BackColor = UiTheme.Surface, ForeColor = UiTheme.Foreground }; Controls.Add(box);
        var browse = new Button { Text = "Suchen…", Bounds = new Rectangle(530, y + 23, 92, 30) }; browse.Click += (a, b) => { using (var dialog = new FolderBrowserDialog { Description = title }) if (dialog.ShowDialog(this) == DialogResult.OK) box.Text = dialog.SelectedPath; }; Controls.Add(browse); return box;
    }
    void Save()
    {
        string f = fan.Text.Trim();
        if (f != "" && !File.Exists(Path.Combine(f, "FanControl.IPC.dll"))) { Error("Im Fan-Control-Ordner fehlt FanControl.IPC.dll."); return; }
        var s = AppSettings.Current; string oldF = s.FanControlPath, oldG = s.GpuIdentifier;
        s.FanControlPath = f; s.GpuIdentifier = ids[gpu.SelectedIndex];
        try { AppSettings.Save(); } catch (Exception e) { s.FanControlPath = oldF; s.GpuIdentifier = oldG; Error(e.Message); return; }
        DialogResult = DialogResult.OK; Close();
    }
    void Error(string message) { MessageBox.Show(this, message, "Hardware & Pfade", MessageBoxButtons.OK, MessageBoxIcon.Information); }
}
