namespace AioDisplay.App;

using System.Diagnostics;
public static class SensorHub
{
    static readonly object sync = new(); static SensorProcessClient client;
    public static string HostPath = Path.Combine(AppContext.BaseDirectory, "sensors", "AioDisplay.SensorHost.exe");
    static SensorProcessClient Client { get { lock (sync) return client ??= new SensorProcessClient(HostPath, Log.Write); } }
    public static void EnsureRunning() { var settings = AppSettings.Current; using var current = Process.GetCurrentProcess(); Client.EnsureRunning(new SensorOptions { Provider = settings.SensorProvider, IntervalMs = settings.SensorMs, FanControlPath = settings.FanControlPath, GpuIdentifier = settings.GpuIdentifier, ParentId = current.Id, ParentStartTicks = current.StartTime.ToUniversalTime().Ticks }); }
    public static SensorSnapshot Read() => Client.Read();
    public static List<GpuDevice> Gpus => Client.Latest?.Gpus ?? new();
    public static void Stop() => Client.Stop();
}
