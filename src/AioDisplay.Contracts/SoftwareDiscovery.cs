// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.Contracts;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

// Discovery only: never starts, installs or downloads external software.
public static class SoftwareDiscovery
{
    public static string Find(string executable, string requiredFile = null)
    {
        string stem = Path.GetFileNameWithoutExtension(executable);
        var folders = new List<string>();
        foreach (var process in Process.GetProcessesByName(stem))
            using (process) { try { folders.Add(Path.GetDirectoryName(ProcessPath(process.Id))); } catch { } }
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var app = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + executable);
                var appPath = app?.GetValue("") as string;
                if (!string.IsNullOrWhiteSpace(appPath)) folders.Add(Path.GetDirectoryName(appPath.Trim('"')));
                using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall != null) foreach (var name in uninstall.GetSubKeyNames())
                {
                    using var item = uninstall.OpenSubKey(name);
                    string display = item?.GetValue("DisplayName") as string ?? "";
                    if (!display.Replace(" ", "").Contains(stem, StringComparison.OrdinalIgnoreCase)) continue;
                    folders.Add(item?.GetValue("InstallLocation") as string);
                    string icon = item?.GetValue("DisplayIcon") as string;
                    if (!string.IsNullOrWhiteSpace(icon))
                    {
                        int comma = icon.LastIndexOf(',');
                        if (comma >= 0 && int.TryParse(icon[(comma + 1)..], out _)) icon = icon[..comma];
                        folders.Add(Path.GetDirectoryName(icon.Trim('"')));
                    }
                }
            }
            catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException or ArgumentException) { }
        }
        string[] names = stem == "MSIAfterburner" ? new[] { "MSI Afterburner", stem } : new[] { stem };
        foreach (var parent in new[] { AppContext.BaseDirectory, Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps") })
        {
            if (string.IsNullOrWhiteSpace(parent)) continue;
            foreach (var name in names) { folders.Add(Path.Combine(parent, name)); folders.Add(Path.Combine(parent, name, "current")); }
        }
        return FindInFolders(folders, executable, requiredFile);
    }

    public static string FindInFolders(IEnumerable<string> folders, string executable, string requiredFile = null)
    {
        foreach (string folder in folders.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                string expanded = Environment.ExpandEnvironmentVariables(folder.Trim('"'));
                if (File.Exists(Path.Combine(expanded, executable)) && (requiredFile == null || File.Exists(Path.Combine(expanded, requiredFile)))) return Path.GetFullPath(expanded);
            }
            catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException) { }
        }
        return null;
    }

    // Limited query access also works for many programs running elevated.
    static string ProcessPath(int id)
    {
        IntPtr handle = OpenProcess(0x1000, false, id);
        if (handle == IntPtr.Zero) return null;
        try { var buffer = new StringBuilder(32768); int length = buffer.Capacity; return QueryFullProcessImageName(handle, 0, buffer, ref length) ? buffer.ToString() : null; }
        finally { CloseHandle(handle); }
    }
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, int id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref int size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
}
