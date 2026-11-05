// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System;
using System.IO;
using Microsoft.Win32;
public sealed class AppSettings
{
    public static AppSettings Current = new AppSettings();
    public bool AdminSensors;
    public int Rotation;
    public string FanControlPath = "", GpuIdentifier = "";
    public int View = 6, RefreshMs = 500, SensorProvider = 0, SensorMs = 250, BarWindowMs = 500; public bool ThreadNumbers, HideStoppedFans = true, CloseToTray = true;
    public static string FilePath { get { return Path.Combine(Log.Root, "settings.ini"); } }
    public static void Load()
    {
        Current = new AppSettings();
        try
        {
            if (File.Exists(FilePath)) { foreach (var line in File.ReadAllLines(FilePath)) { var p = line.Split(new[] { '=' }, 2); if (p.Length != 2) continue; int n; switch (p[0]) { case "Rotation": if (int.TryParse(p[1], out n) && (n == 0 || n == 90 || n == 180 || n == 270)) Current.Rotation = n; break; case "AdminSensors": Current.AdminSensors = p[1] == "1"; break; case "FanControlPath": Current.FanControlPath = p[1]; break; case "GpuIdentifier": Current.GpuIdentifier = p[1]; break; case "BarWindowMs": if (int.TryParse(p[1], out n) && (n == 0 || n == 250 || n == 500 || n == 1000 || n == 2000)) Current.BarWindowMs = n; break; case "SensorMs": if (int.TryParse(p[1], out n) && (n == 100 || n == 125 || n == 250 || n == 500 || n == 1000)) Current.SensorMs = n; break; case "SensorProvider": if (int.TryParse(p[1], out n) && (n == 0 || n == 1)) Current.SensorProvider = n; break; case "View": if (int.TryParse(p[1], out n) && n >= 0 && n <= 6) Current.View = n; break; case "RefreshMs": if (int.TryParse(p[1], out n) && (n == 16 || n == 33 || n == 100 || n == 125 || n == 250 || n == 500 || n == 1000)) Current.RefreshMs = n; break; case "ThreadNumbers": Current.ThreadNumbers = p[1] == "1"; break; case "HideStoppedFans": Current.HideStoppedFans = p[1] != "0"; break; case "CloseToTray": Current.CloseToTray = p[1] != "0"; break; } } }
            else { var old = File.ReadAllLines(Path.Combine(Log.Root, "ansicht.txt")); int v; if (old.Length > 0 && int.TryParse(old[0], out v) && v >= 0 && v <= 6) Current.View = v; Current.ThreadNumbers = old.Length > 1 && old[1] == "1"; }
        }
        catch (IOException) { }
    }
    public static void Save() { var s = Current; File.WriteAllLines(FilePath, new[] { "Rotation=" + s.Rotation, "AdminSensors=" + (s.AdminSensors ? 1 : 0), "FanControlPath=" + s.FanControlPath, "GpuIdentifier=" + s.GpuIdentifier, "BarWindowMs=" + s.BarWindowMs, "SensorMs=" + s.SensorMs, "SensorProvider=" + s.SensorProvider, "View=" + s.View, "RefreshMs=" + s.RefreshMs, "ThreadNumbers=" + (s.ThreadNumbers ? 1 : 0), "HideStoppedFans=" + (s.HideStoppedFans ? 1 : 0), "CloseToTray=" + (s.CloseToTray ? 1 : 0) }); }
}
public static class AutoStart
{
    const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run", Name = "MiraRight Panel";
    public static string Command { get { return "\"" + Path.Combine(Log.Root, "MiraRightPanel.exe") + "\" --minimized"; } }
    public static bool Enabled { get { using (var k = Registry.CurrentUser.OpenSubKey(Key)) return k != null && string.Equals(k.GetValue(Name) as string, Command, StringComparison.OrdinalIgnoreCase); } }
    public static void Set(bool enabled) { using (var k = Registry.CurrentUser.CreateSubKey(Key)) { if (enabled) k.SetValue(Name, Command, RegistryValueKind.String); else k.DeleteValue(Name, false); } }
}
