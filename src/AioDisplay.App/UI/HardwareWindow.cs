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
    readonly ReadableCheckBox adminSensors = new ReadableCheckBox { Text = "Direkte Sensoren mit Adminrechten", Bounds = new Rectangle(248, 178, 374, 30) };
    readonly TextBox detected = new TextBox { Multiline = true, ReadOnly = true, BackColor = UiTheme.Surface, ForeColor = UiTheme.Foreground, ScrollBars = ScrollBars.Vertical, Bounds = new Rectangle(18, 220, 604, 180) };
    readonly Button detect = new Button { Text = "Erkennung aktualisieren", Bounds = new Rectangle(18, 178, 210, 30) };
    public HardwareWindow()
    {
        Text = "Hardware & Sensoren"; Icon = AppBrand.Icon; ClientSize = new Size(640, 510); BackColor = Color.FromArgb(9, 15, 25); ForeColor = UiTheme.Foreground; Font = new Font("Segoe UI", 10); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new Label { Text = "Sensoren und optionale Programme werden automatisch erkannt. Eine separate Libre-Installation ist nicht erforderlich.", Bounds = new Rectangle(18, 15, 604, 44) });
        fan = PathRow("FanControl-Ordner (nur für portable / abweichende Installationen)", 450, AppSettings.Current.FanControlPath);
        var manual = Controls.Cast<Control>().Where(c => c.Top >= 450).ToArray(); foreach (var c in manual) c.Visible = false;
        Controls.Add(new Label { Text = "Grafikkarte", Bounds = new Rectangle(18, 65, 440, 23) });
        gpu.Bounds = new Rectangle(18, 90, 604, 28); gpu.DropDownStyle = ComboBoxStyle.DropDownList; Controls.Add(gpu);
        ids.Add(""); gpu.Items.Add("Automatisch – größter Grafikspeicher");
        try { foreach (var card in SensorHub.Gpus) { if (!ids.Contains(card.Id)) { ids.Add(card.Id); gpu.Items.Add(card.Name + " (" + card.Id + ")"); } } } catch (IOException) { }
        string selected = AppSettings.Current.GpuIdentifier; if (!ids.Contains(selected)) { ids.Add(selected); gpu.Items.Add(selected + " (zurzeit nicht erkannt)"); }
        gpu.SelectedIndex = ids.IndexOf(selected);
        Controls.Add(new Label { Text = "Übernehmen startet die Sensor-Erkennung neu. Fehlende Werte erscheinen als --.", Bounds = new Rectangle(18, 133, 604, 38), ForeColor = UiTheme.Inactive });
        Controls.Add(detected); Controls.Add(detect); detect.Click += async (a, b) => await Detect(); Shown += async (a, b) => await Detect();
        adminSensors.Checked = AppSettings.Current.AdminSensors; Controls.Add(adminSensors);
        var save = new Button { Text = "Übernehmen", Bounds = new Rectangle(347, 465, 135, 30) }; save.Click += (a, b) => Save(); Controls.Add(save);
        var names = new Button { Text = "Lüfter benennen…", Bounds = new Rectangle(18, 465, 220, 30) }; names.Click += (a,b) => { using var dialog = new FanNamesWindow(); dialog.ShowDialog(this); }; Controls.Add(names);
        var cancel = new Button { Text = "Abbrechen", Bounds = new Rectangle(492, 465, 130, 30), DialogResult = DialogResult.Cancel }; Controls.Add(cancel); CancelButton = cancel;
        var advanced = new ReadableCheckBox { Text = "Erweitert: FanControl-Ordner selbst festlegen", Bounds = new Rectangle(18, 414, 604, 28) }; Controls.Add(advanced);
        advanced.CheckedChanged += (a, b) => { foreach (var c in manual) c.Visible = advanced.Checked; names.Top = save.Top = cancel.Top = advanced.Checked ? 535 : 465; ClientSize = new Size(640, advanced.Checked ? 580 : 510); };
        using (var menu = new ContextMenuStrip()) UiTheme.Apply(this, menu);
    }
    async System.Threading.Tasks.Task Detect()
    {
        detect.Enabled = false; detected.Text = "Programme werden gesucht…";
        try
        {
            var result = await System.Threading.Tasks.Task.Run(() => new[] { SoftwareDiscovery.Find("FanControl.exe", "FanControl.IPC.dll"), SoftwareDiscovery.Find("MSIAfterburner.exe") });
            if (IsDisposed) return;
            detected.Text = "CPU, GPU und Speicher: integrierte Libre-Sensorbibliothek" + Environment.NewLine
                + "Lüfter: FanControl mit Namen, alternativ direkte Libre-RPM, soweit verfügbar. Später gestartetes FanControl wird automatisch verbunden." + Environment.NewLine
                + "FanControl: " + (result[0] ?? "nicht gefunden – optional") + Environment.NewLine
                + "MSI Afterburner: " + (result[1] ?? "nicht gefunden – optional") + Environment.NewLine
                + "Admin-Modus: Windows-Bestätigung beim Start des Sensorprozesses. Liest Lüfter direkt ohne FanControl. Die Oberfläche läuft weiter ohne Adminrechte. Nach Abbruch: Übernehmen zum erneuten Versuch.";
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
        if (!adminSensors.Checked && f != "" && !File.Exists(Path.Combine(f, "FanControl.IPC.dll"))) { Error("Im Fan-Control-Ordner fehlt FanControl.IPC.dll."); return; }
        var s = AppSettings.Current; string oldF = s.FanControlPath, oldG = s.GpuIdentifier;
        if (adminSensors.Checked && !s.AdminSensors) { try { FanNames.Import(FanData.Read(SensorHub.Read(), false)); } catch(Exception e) { Error("Lüfternamen konnten nicht übernommen werden: " + e.Message); return; } }
        bool oldAdmin = s.AdminSensors; s.AdminSensors = adminSensors.Checked;
        s.FanControlPath = f; s.GpuIdentifier = ids[gpu.SelectedIndex];
        try { AppSettings.Save(); } catch (Exception e) { s.AdminSensors = oldAdmin; s.FanControlPath = oldF; s.GpuIdentifier = oldG; Error(e.Message); return; }
        DialogResult = DialogResult.OK; Close();
    }
    void Error(string message) { MessageBox.Show(this, message, "Hardware & Pfade", MessageBoxButtons.OK, MessageBoxIcon.Information); }
}
