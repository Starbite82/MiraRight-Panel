// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using AioDisplay.Contracts;
internal static class DiscoveryChecks
{
    public static void Main()
    {
        string parent = Path.Combine(Directory.GetCurrentDirectory(), "discovery-fixture");
        string old = Path.Combine(parent, "old"), current = Path.Combine(parent, "current");
        Directory.CreateDirectory(old); Directory.CreateDirectory(current);
        File.WriteAllText(Path.Combine(old, "FanControl.exe"), "fixture");
        File.WriteAllText(Path.Combine(current, "FanControl.exe"), "fixture");
        File.WriteAllText(Path.Combine(current, "FanControl.IPC.dll"), "fixture");
        string found = SoftwareDiscovery.FindInFolders(new[] { null, "", old, current, current }, "FanControl.exe", "FanControl.IPC.dll");
        if (found != current) throw new Exception("Detection must skip incomplete FanControl installs");
        if (SoftwareDiscovery.FindInFolders(new[] { old }, "MSIAfterburner.exe") != null) throw new Exception("Missing executable accepted");
        Environment.SetEnvironmentVariable("AIO_DISCOVERY_TEST", current);
        try { if (SoftwareDiscovery.FindInFolders(new[] { "%AIO_DISCOVERY_TEST%" }, "FanControl.exe") != current) throw new Exception("Expandable path detection"); }
        finally { Environment.SetEnvironmentVariable("AIO_DISCOVERY_TEST", null); }
        Console.WriteLine("PASS: software discovery, incomplete installs, missing tools, expandable paths");
    }
}
