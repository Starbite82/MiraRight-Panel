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
    public HardwareWindow()
    {
        Text = "Hardware & Pfade"; Icon = AppBrand.Icon; ClientSize = new Size(640, 310); BackColor = Color.FromArgb(9, 15, 25); ForeColor = UiTheme.Foreground; Font = new Font("Segoe UI", 10); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new Label { Text = "Leere Pfade = automatisch erkennen. Portable Ordner sind ebenfalls möglich.", Bounds = new Rectangle(18, 15, 604, 40) });
        fan = PathRow("Fan Control (optional, Ordner)", 60, AppSettings.Current.FanControlPath);
        Controls.Add(new Label { Text = "Grafikkarte (LibreHardwareMonitor)", Bounds = new Rectangle(18, 135, 440, 23) });
        gpu.Bounds = new Rectangle(18, 160, 604, 28); gpu.DropDownStyle = ComboBoxStyle.DropDownList; Controls.Add(gpu);
        ids.Add(""); gpu.Items.Add("Automatisch – größter Grafikspeicher");
        try { foreach (var card in SensorHub.Gpus) { if (!ids.Contains(card.Id)) { ids.Add(card.Id); gpu.Items.Add(card.Name + " (" + card.Id + ")"); } } } catch (IOException) { }
        string selected = AppSettings.Current.GpuIdentifier; if (!ids.Contains(selected)) { ids.Add(selected); gpu.Items.Add(selected + " (zurzeit nicht erkannt)"); }
        gpu.SelectedIndex = ids.IndexOf(selected);
        Controls.Add(new Label { Text = "Übernehmen startet die Sensor-Erkennung neu. Fehlende Werte erscheinen als --.", Bounds = new Rectangle(18, 203, 604, 44), ForeColor = UiTheme.Inactive });
        var save = new Button { Text = "Übernehmen", Bounds = new Rectangle(347, 265, 135, 30) }; save.Click += (a, b) => Save(); Controls.Add(save);
        var cancel = new Button { Text = "Abbrechen", Bounds = new Rectangle(492, 265, 130, 30), DialogResult = DialogResult.Cancel }; Controls.Add(cancel); CancelButton = cancel;
        using (var menu = new ContextMenuStrip()) UiTheme.Apply(this, menu);
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
