// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using AioDisplay.SensorHost;

internal static class SensorWatchdogChecks
{
    public static void Run()
    {
        foreach (var scenario in new[] { "blocked-sample", "blocked-shutdown" })
        {
            using var child = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "AioDisplay.Tests.exe"),
                ArgumentList = { "--watchdog-child", Directory.GetCurrentDirectory(), scenario },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            try
            {
                var ready = child.StandardOutput.ReadLineAsync();
                if (!ready.Wait(TimeSpan.FromSeconds(5)) || ready.Result != "ready")
                    throw new Exception("Sensor watchdog child failed to start: " + scenario);
                if (!child.WaitForExit(12000) || child.ExitCode != -1)
                    throw new Exception("Sensor watchdog failed to kill the worker: " + scenario);
            }
            finally
            {
                if (!child.HasExited) child.Kill();
            }
        }
        Console.WriteLine("PASS: worker watchdog terminates blocked hardware reads and shutdown");
    }

    public static void Block(string scenario)
    {
        using var stop = new CancellationTokenSource();
        using var watchdog = new SensorWatchdog(stop.Token);
        watchdog.SnapshotWritten();
        if (scenario == "blocked-shutdown") stop.Cancel();
        Console.WriteLine("ready");
        Thread.Sleep(Timeout.Infinite);
    }
}
