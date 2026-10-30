namespace AioDisplay.SensorHost;

using LibreHardwareMonitor.Hardware;
using System.Text.RegularExpressions;
public sealed class LibreHardwareSource : IDisposable
{
    readonly Computer computer = new() { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true };
    readonly bool legacyUnits = Enum.GetNames<SensorType>().Contains("SmallData");
    public LibreHardwareSource() { try { computer.Open(); } catch { computer.Close(); throw; } }
    public SensorSnapshot Sample(string preferredGpu)
    {
        foreach (var hardware in computer.Hardware) hardware.Update();
        var snapshot = new SensorSnapshot { Source = "LibreHardwareMonitor" };
        var loads = new List<double>(); var temps = new List<double>(); int thread = 0;
        foreach (var cpu in computer.Hardware.Where(h => h.HardwareType == HardwareType.Cpu))
        {
            var threads = cpu.Sensors.Where(s => s.SensorType == SensorType.Load && Regex.IsMatch(s.Name, @"^CPU (Core|Thread) #\d+")).OrderBy(s => s.Index).ToArray();
            foreach (var sensor in threads) { thread++; Add(snapshot, 144, (uint)(thread - 1), "CPU" + thread + " usage", "%", sensor); }
            var load = Sensor(cpu, "CPU Total", "Load"); if (Valid(load?.Value)) for (int i = 0; i < Math.Max(1, threads.Length); i++) loads.Add(load.Value.Value);
            ISensor temperature = null;
            foreach (var name in new[] { "Core (Tctl/Tdie)", "CPU Package", "CPU (Tctl/Tdie)", "Core (Tdie)" }) { var s = Sensor(cpu, name, "Temperature"); if (Temperature(s?.Value)) { temperature = s; break; } }
            temperature ??= cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature && Temperature(s.Value)).OrderByDescending(s => s.Value).FirstOrDefault();
            if (temperature != null) temps.Add(temperature.Value.Value);
        }
        snapshot.ReportedThreadCount = thread;
        if (loads.Count > 0) AddValue(snapshot, 144, uint.MaxValue, "CPU usage", "%", loads.Average());
        if (temps.Count > 0) { AddValue(snapshot, 128, uint.MaxValue, "CPU temperature", "C", temps.Max()); snapshot.CpuTemperatureSource = "LibreHardwareMonitor"; }
        var ram = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory && h.Sensors.Any(s => s.Identifier.ToString().StartsWith("/ram/")));
        if (ram != null) { var used = Memory(ram, "Memory Used"); Add(snapshot, 145, 0, "RAM usage", "MB", used, Divisor(used)); }
        var gpus = computer.Hardware.Where(h => h.HardwareType.ToString().StartsWith("Gpu")).ToArray();
        foreach (var card in gpus) snapshot.Gpus.Add(new GpuDevice { Id = card.Identifier.ToString(), Name = card.Name, TotalMb = MemoryMb(card, "GPU Memory Total") });
        var gpu = gpus.FirstOrDefault(g => g.Identifier.ToString() == preferredGpu) ?? gpus.OrderByDescending(g => MemoryMb(g, "GPU Memory Total") ?? 0).FirstOrDefault();
        if (gpu != null)
        {
            snapshot.GpuName = gpu.Name;
            Add(snapshot, 0, 0, "GPU temperature", "C", Sensor(gpu, "GPU Core", "Temperature"));
            Add(snapshot, 48, 0, "GPU usage", "%", Sensor(gpu, "GPU Core", "Load") ?? Sensor(gpu, "D3D 3D", "Load"));
            var used = Memory(gpu, "GPU Memory Used"); Add(snapshot, 49, 0, "GPU1 memory usage", "MB", used, Divisor(used));
            Add(snapshot, 17, 0, "GPU1 fan tachometer", "RPM", Sensor(gpu, "GPU Fan", "Fan"));
            snapshot.VramTotalMb = MemoryMb(gpu, "D3D Dedicated Memory Total") ?? MemoryMb(gpu, "GPU Memory Total");
        }
        snapshot.Fresh = snapshot.Entries.Count > 0; snapshot.Status = snapshot.Fresh ? "LHM DIREKT" : "LHM NICHT BEREIT"; return snapshot;
    }
    static ISensor Sensor(IHardware hardware, string name, string type) => hardware.Sensors.FirstOrDefault(s => s.Name == name && s.SensorType.ToString() == type);
    static ISensor Memory(IHardware hardware, string name) => hardware.Sensors.FirstOrDefault(s => s.Name == name && (s.SensorType.ToString() == "Data" || s.SensorType.ToString() == "SmallData"));
    double Divisor(ISensor sensor) => MemoryUnits.Divisor(legacyUnits, sensor?.SensorType.ToString());
    double? MemoryMb(IHardware hardware, string name) { var s = Memory(hardware, name); return s != null && Valid(s.Value) && s.Value > 0 ? s.Value.Value / Divisor(s) : null; }
    static bool Valid(float? value) => value.HasValue && float.IsFinite(value.Value) && Math.Abs(value.Value) < 1e20;
    static bool Temperature(float? value) => Valid(value) && value > 0 && value < 150;
    static void Add(SensorSnapshot s, uint id, uint gpu, string name, string unit, ISensor sensor, double divisor = 1) { if (sensor != null && Valid(sensor.Value)) AddValue(s, id, gpu, name, unit, sensor.Value.Value / divisor); }
    static void AddValue(SensorSnapshot s, uint id, uint gpu, string name, string unit, double value) { if (!double.IsFinite(value) || Math.Abs(value) >= 1e20 || (unit == "C" && (value <= 0 || value >= 150))) return; s.Entries.Add(new Reading { Id = id, Gpu = gpu, Name = name, Unit = unit, Value = (float)value }); }
    public void Dispose() => computer.Close();
}
public static class MemoryUnits { public static double Divisor(bool legacy, string type) => legacy ? (type == "SmallData" ? 1 : 1.0 / 1024) : 1048576; }
