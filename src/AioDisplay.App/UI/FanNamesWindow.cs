// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;
using System.Drawing;
using System.Windows.Forms;

public sealed class FanNamesWindow : Form
{
    readonly DataGridView grid = new();
    public FanNamesWindow(IEnumerable<FanReading> fans = null)
    {
        Text = "Lüfter benennen"; Icon = AppBrand.Icon; ClientSize = new Size(680, 440); BackColor = Color.FromArgb(9,15,25); ForeColor = UiTheme.Foreground; Font = new Font("Segoe UI",10); StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new Label { Text = "Eigene Namen gelten unabhängig von FanControl. Leerer Name = Sensorname.\nAuch stehende Lüfter werden hier aufgelistet; es wird nichts geregelt.", Bounds = new Rectangle(18,15,644,48) });
        grid.Bounds = new Rectangle(18,75,644,300); grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.RowHeadersVisible = false; grid.BackgroundColor = UiTheme.Surface; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; grid.EnableHeadersVisualStyles = false;
        grid.DefaultCellStyle.BackColor = UiTheme.Surface; grid.DefaultCellStyle.ForeColor = UiTheme.Foreground; grid.DefaultCellStyle.SelectionBackColor = UiTheme.Selected; grid.DefaultCellStyle.SelectionForeColor = UiTheme.Foreground;
        grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Surface; grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Foreground;
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sensor", HeaderText = "Sensor", ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rpm", HeaderText = "RPM", ReadOnly = true, FillWeight = 35 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Eigener Name", MaxInputLength = 60 });
        foreach (var fan in fans ?? FanData.Read(SensorHub.Read(), false)) { int index = grid.Rows.Add(fan.Name, fan.Rpm?.ToString("0") ?? "--", FanNames.Get(fan.Id,"")); grid.Rows[index].Tag = fan.Id; }
        Controls.Add(grid);
        var save = new Button { Text = "Speichern", Bounds = new Rectangle(390,392,130,32) }; save.Click += (a,b) => { try { grid.EndEdit(); FanNames.Save(grid.Rows.Cast<DataGridViewRow>().Select(r => new KeyValuePair<string,string>((string)r.Tag, r.Cells["Name"].Value?.ToString() ?? ""))); DialogResult = DialogResult.OK; Close(); } catch(Exception e) { MessageBox.Show(this,e.Message,"Lüfternamen"); } }; Controls.Add(save);
        var cancel = new Button { Text = "Abbrechen", Bounds = new Rectangle(532,392,130,32), DialogResult = DialogResult.Cancel }; Controls.Add(cancel); CancelButton = cancel;
        using var menu = new ContextMenuStrip(); UiTheme.Apply(this,menu);
    }
}
