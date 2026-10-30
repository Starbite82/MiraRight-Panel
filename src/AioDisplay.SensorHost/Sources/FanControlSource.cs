namespace AioDisplay.SensorHost;

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
public sealed class FanSample
{
    public long Ticks { get; init; }
    public List<FanReading> Fans { get; init; } = new(); public float? CpuTemperature { get; init; }
    public string Error { get; init; } = "";
}
// No Fan Control implementation is redistributed. Only the installed read-only IPC client is loaded.
public sealed class FanControlSource
{
    readonly string configuredRoot; Assembly assembly; object sensorClient, controlClient; string root; JsonDocument config; DateTime configAt = DateTime.MinValue;
    public FanControlSource(string folder) { configuredRoot = folder; }
    public FanSample Poll()
    {
        try
        {
            if (assembly == null) { root = FindRoot(configuredRoot); var context = new FanControlLoadContext(root); assembly = context.LoadFromAssemblyPath(Path.Combine(root, "FanControl.IPC.dll")); }
            var factory = assembly.GetType("FanControl.IPC.IPCFactory", true);
            sensorClient ??= factory.GetMethod("GetSensorClient").Invoke(null, null);
            if (DateTime.UtcNow - configAt > TimeSpan.FromSeconds(30)) { LoadConfiguration(factory); configAt = DateTime.UtcNow; }
            var reply = Call(sensorClient, "GetAllSensors", "GetAllSensorsRequest"); var sensors = ((IEnumerable)Get(reply, "Sensors")).Cast<object>().ToArray();
            var rows = new List<FanReading>(); var cpu = new List<(string Name, float Value)>();
            foreach (var sensor in sensors)
            {
                string id = Text(sensor, "Identifier"), name = Text(sensor, "Name"); bool has = (bool)Get(sensor, "HasValue"); float value = Convert.ToSingle(Get(sensor, "Value"));
                if (id.StartsWith("/amdcpu/") || id.StartsWith("/intelcpu/")) { if (id.Contains("/temperature/") && has && float.IsFinite(value) && value > 0 && value < 150) cpu.Add((name, value)); }
                if (Convert.ToInt32(Get(sensor, "Type")) != 2) continue;
                var label = ConfiguredName(id, name); if (label == null) continue;
                rows.Add(new FanReading { Id = id, Name = label, Rpm = has && float.IsFinite(value) && value >= 0 && value < 100000 ? value : null });
            }
            var preferred = cpu.Where(t => t.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)).ToArray();
            float? temperature = preferred.Length > 0 ? preferred.Max(t => t.Value) : cpu.Count > 0 ? cpu.Max(t => t.Value) : null;
            return new FanSample { Ticks = DateTime.UtcNow.Ticks, Fans = rows, CpuTemperature = temperature };
        }
        catch (Exception e) { sensorClient = null; controlClient = null; return new FanSample { Error = (e is TargetInvocationException ? e.InnerException?.Message : e.Message) ?? "Fan Control unavailable" }; }
    }
    object Call(object client, string method, string request)
    {
        var type = assembly.GetTypes().First(t => t.Name == request); var m = client.GetType().GetMethods().First(m => m.Name == method && m.GetParameters().Length == 4);
        return m.Invoke(client, new[] { Activator.CreateInstance(type), null, DateTime.UtcNow.AddMilliseconds(800), (object)CancellationToken.None });
    }
    void LoadConfiguration(Type factory)
    {
        string path = null;
        try { controlClient ??= factory.GetMethod("GetFanControlClient").Invoke(null, null); var reply = Call(controlClient, "ListAvailableConfigs", "ListAvailableConfigsRequest"); string current = Text(reply, "CurrentConfig"); if (!string.IsNullOrEmpty(current)) path = Path.IsPathRooted(current) ? current : Path.Combine(Text(reply, "ConfigFolder"), current); } catch { }
        if (path == null || !File.Exists(path)) { try { using var cache = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Cache.json"))); string folder = Prop(cache.RootElement, "CustomConfigFolder"), file = Prop(cache.RootElement, "CurrentConfigFileName"); if (!string.IsNullOrEmpty(file)) path = Path.Combine(string.IsNullOrEmpty(folder) ? Path.Combine(root, "Configurations") : folder, file); } catch { } }
        try { var next = path != null && File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)) : null; config?.Dispose(); config = next; } catch { }
    }
    string ConfiguredName(string id, string fallback)
    {
        if (config == null || !config.RootElement.TryGetProperty("FanControl", out var fc)) return fallback;
        string name = fallback;
        if (fc.TryGetProperty("FanSensors", out var sources)) foreach (var source in sources.EnumerateArray()) if (Prop(source, "Identifier") == id) { if (source.TryGetProperty("IsHidden", out var hidden) && hidden.ValueKind == JsonValueKind.True) return null; var nick = Prop(source, "NickName"); if (!string.IsNullOrEmpty(nick)) name = nick; break; }
        if (fc.TryGetProperty("Controls", out var controls)) foreach (var control in controls.EnumerateArray()) if (control.TryGetProperty("PairedFanSensor", out var paired) && paired.ValueKind == JsonValueKind.Object && Prop(paired, "Identifier") == id) { var nick = Prop(control, "NickName"); if (!string.IsNullOrEmpty(nick)) name = nick; break; }
        return name;
    }
    static string Prop(JsonElement obj, string key) => obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    static object Get(object obj, string key) => obj.GetType().GetProperty(key)?.GetValue(obj);
    static string Text(object obj, string key) => Get(obj, key)?.ToString() ?? "";
    public static string FindRoot(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured)) { if (File.Exists(Path.Combine(configured, "FanControl.IPC.dll"))) return Path.GetFullPath(configured); throw new FileNotFoundException("FanControl.IPC.dll fehlt im gewählten Ordner."); }
        var candidates = new List<string>(); foreach (var process in Process.GetProcessesByName("FanControl")) { using (process) { try { var file = process.MainModule?.FileName; if (file != null) candidates.Add(Path.GetDirectoryName(file)); } catch { } } }
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "FanControl")); foreach (var basePath in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) }) if (!string.IsNullOrEmpty(basePath)) candidates.Add(Path.Combine(basePath, "FanControl"));
        return candidates.FirstOrDefault(p => File.Exists(Path.Combine(p, "FanControl.IPC.dll"))) ?? throw new FileNotFoundException("Fan Control nicht gefunden (optional).");
    }
    sealed class FanControlLoadContext(string folder) : AssemblyLoadContext("FanControlIPC", false)
    {
        protected override Assembly Load(AssemblyName name) { if (name.Name.StartsWith("System.") || name.Name == "System" || name.Name == "netstandard" || name.Name == "mscorlib") return null; var file = Path.Combine(folder, name.Name + ".dll"); return File.Exists(file) ? LoadFromAssemblyPath(file) : null; }
        protected override IntPtr LoadUnmanagedDll(string name) { var file = Path.Combine(folder, name.EndsWith(".dll") ? name : name + ".dll"); return File.Exists(file) ? LoadUnmanagedDllFromPath(file) : IntPtr.Zero; }
    }
}
