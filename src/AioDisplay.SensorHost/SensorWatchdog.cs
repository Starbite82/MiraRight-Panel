// SPDX-License-Identifier: MIT
namespace AioDisplay.SensorHost;

using System.Diagnostics;

// Hardware APIs can block inside native code. Keep the deadline outside the
// sampling thread, even during shutdown when a hardware Dispose can block.
internal sealed class SensorWatchdog : IDisposable
{
    readonly object gate = new();
    readonly System.Threading.Timer timer;
    readonly CancellationToken stop;
    readonly CancellationTokenRegistration stopRegistration;
    bool disposed;
    long deadline = Stopwatch.GetTimestamp() + 25 * Stopwatch.Frequency;

    public SensorWatchdog(CancellationToken stop)
    {
        this.stop = stop;
        timer = new(Check, null, 250, 250);
        stopRegistration = stop.Register(() => SetDeadline(4));
    }

    public void SnapshotWritten()
    {
        lock (gate)
        {
            if (!stop.IsCancellationRequested) deadline = Stopwatch.GetTimestamp() + 8 * Stopwatch.Frequency;
        }
    }

    void SetDeadline(int seconds)
    {
        lock (gate) deadline = Stopwatch.GetTimestamp() + seconds * Stopwatch.Frequency;
    }

    void Check(object state)
    {
        lock (gate)
        {
            if (disposed || Stopwatch.GetTimestamp() < deadline) return;
            using var process = Process.GetCurrentProcess();
            process.Kill();
        }
    }

    public void Dispose()
    {
        lock (gate) disposed = true;
        stopRegistration.Dispose();
        timer.Dispose();
    }
}
