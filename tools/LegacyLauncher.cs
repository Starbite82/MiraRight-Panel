// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

// Compatibility entry point for the retired BLUB-Standalone installation.
// No sensor, USB or settings code from the former application is executed.
internal static class LegacyLauncher
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string retired = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BLUB-Display.exe");
            string current = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "AIO-Display", "BLUB-Display.exe"));
            if (!File.Exists(current)) throw new FileNotFoundException("Die aktuelle Anwendung wurde nicht gefunden.", current);
            // Run in the caller's actual login context, including when Windows invokes the old entry.
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                string entry = key == null ? null : key.GetValue("BLUB AIO Display") as string;
                if (entry != null && entry.IndexOf(retired, StringComparison.OrdinalIgnoreCase) >= 0)
                    key.SetValue("BLUB AIO Display", "\"" + current + "\" --minimized", RegistryValueKind.String);
            }
            // Only the application's public switches are forwarded; no arbitrary command line.
            string forwarded = string.Join(" ", args.Where(a => new[] { "--minimized", "--stop", "--enable-autostart", "--disable-autostart", "--preview", "--render" }.Contains(a)));
            Process.Start(new ProcessStartInfo(current, forwarded) { WorkingDirectory = Path.GetDirectoryName(current), UseShellExecute = true });
            return 0;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "AIO Display – Programmstart", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
