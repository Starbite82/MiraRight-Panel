// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

public static class AboutContent
{
    public const string Development = "KI-unterstützt entwickelt mit OpenAI Codex.";
    public const string Technology = "Technische Basis: .NET 10 und Windows Forms.";
    public const string MissingImprint = "Anbieterangaben wurden noch nicht hinterlegt. Dieser Bereich ist noch kein vollständiges Impressum.";
    public static string Version => typeof(AboutContent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "unbekannt";
    public static string Info => $"AIO Display {Version}\n\n{Development}\n{Technology}\n\nEigenständige Windows-Anwendung mit separatem C#-Sensorprozess und direkter WinUSB-Displayausgabe.\n\nProjekt: Starbite82/AIO-Display\nhttps://github.com/Starbite82/AIO-Display\n\nDie Entwicklung erfolgt iterativ mit versionierten Quellen, automatisierten Tests und dokumentierten Hardwareprüfungen. Unterstützte Geräte und Einrichtung stehen in der Anleitung.\n\nBedienung: LIESMICH.txt im Programmordner.";
    public static string ReadImprint(string directory) => ReadOptional(directory, "IMPRINT.txt", MissingImprint);
    public static string ReadLicenses(string directory) => ReadOptional(directory, "THIRD-PARTY-COMPONENTS.txt", "Drittanbieterhinweise sind in diesem Entwicklungsordner noch nicht vorhanden. Sie werden beim Paketieren erzeugt.") + "\n\n" + ReadOptional(directory, "LICENSE-PROFILE.txt", "App: GPL-3.0-only. Separater Sensorprozess und Datenvertrag: MIT. Drittanbieter behalten ihre eigenen Lizenzen; vollständige Texte und Quellen liegen dem Paket bei.");
    private static string ReadOptional(string directory, string name, string fallback)
    {
        try
        {
            string path = Path.Combine(directory, name);
            if (!File.Exists(path)) return fallback;
            if (new FileInfo(path).Length > 256 * 1024) return "Die Informationsdatei ist zu groß: " + name;
            string text = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }
        catch (IOException) { return "Die Informationsdatei konnte nicht gelesen werden: " + name; }
        catch (UnauthorizedAccessException) { return "Kein Lesezugriff auf die Informationsdatei: " + name; }
    }
}

public sealed class AboutWindow : Form
{
    private readonly RichTextBox body;
    public AboutWindow()
    {
        Text = "Info & Impressum";
        Icon = AppBrand.Icon;
        ClientSize = new Size(660, 440);
        BackColor = Color.FromArgb(9, 15, 25);
        ForeColor = UiTheme.Foreground;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Controls.Add(new Label { Text = "AIO Display " + AboutContent.Version, Bounds = new Rectangle(20, 15, 620, 30), ForeColor = UiTheme.Foreground, Font = new Font(Font, FontStyle.Bold) });
        body = new RichTextBox { Bounds = new Rectangle(20, 100, 620, 275), ReadOnly = true, BackColor = UiTheme.Surface, ForeColor = UiTheme.Foreground, BorderStyle = BorderStyle.FixedSingle, DetectUrls = false, TabStop = true, Font = Font, Text = AboutContent.Info };
        Controls.Add(body);
        AddPage("Info", 20, () => AboutContent.Info);
        AddPage("Impressum", 230, () => AboutContent.ReadImprint(Log.Root));
        AddPage("Lizenzen", 440, () => AboutContent.ReadLicenses(Log.Root));
        var close = new Button { Text = "Schließen", Bounds = new Rectangle(510, 397, 130, 30), DialogResult = DialogResult.Cancel };
        Controls.Add(close);
        CancelButton = close;
        using var menu = new ContextMenuStrip();
        UiTheme.Apply(this, menu);
    }
    private void AddPage(string title, int x, Func<string> content)
    {
        var button = new Button { Text = title, Bounds = new Rectangle(x, 55, 200, 30) };
        button.Click += (sender, e) => { body.Text = content(); body.SelectionStart = 0; body.ScrollToCaret(); };
        Controls.Add(button);
    }
}
