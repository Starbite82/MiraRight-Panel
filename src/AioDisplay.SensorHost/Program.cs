// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.SensorHost;

using System.Diagnostics;
using System.Text.Json;
internal static class SensorHostProgram
{
    static FanSample fans = new();
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false); using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; stop.Cancel(); };
        LibreHardwareSource lhm = null;
        SensorPipeSession pipe = null;
        try
        {
            if (args.Contains("--pipe")) pipe = new SensorPipeSession(args);
            var input = await Console.In.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)); if (input == null || input.Length > 32768) throw new InvalidDataException("Missing sensor configuration");
            var options = JsonSerializer.Deserialize<SensorOptions>(input, SnapshotProtocol.Json) ?? throw new InvalidDataException("Invalid sensor configuration"); options.Validate();
            if (pipe != null) { if (options.ParentId != pipe.ParentId || options.ParentStartTicks != pipe.ParentStartTicks) throw new InvalidDataException("Pipe parent mismatch"); options.UseFanControl = false; options.FanControlPath = ""; }
            Process parent = null; if (options.ParentId > 0) { parent = Process.GetProcessById(options.ParentId); if (parent.StartTime.ToUniversalTime().Ticks != options.ParentStartTicks) throw new InvalidDataException("Parent identity changed"); }
            using (parent)
            {
                _ = Task.Run(async () => { while (!stop.IsCancellationRequested) { if (parent != null) { try { if (parent.HasExited) { stop.Cancel(); return; } } catch { stop.Cancel(); return; } } await Task.Delay(250, stop.Token); } }, stop.Token);
                var fanTask = Task.Run(async () =>
                {
                    var source = new FanControlSource(options.FanControlPath);
                    using var direct = new DirectFanSource();
                    string lastState = "";
                    while (!stop.IsCancellationRequested)
                    {
                        var sample = options.UseFanControl ? source.Poll() : new FanSample();
                        string fanControlError = sample.Error;
                        bool fromFanControl = FanSelection.HasReadings(sample);
                        if (!fromFanControl) sample = direct.Poll();
                        Volatile.Write(ref fans, sample);
                        string state = (fromFanControl ? "Fan Control" : "Libre direkt") + ": " + sample.Fans.Count(f => f.Rpm.HasValue) + " RPM-Sensoren" + (sample.Error == "" ? "" : " (" + sample.Error + ")") + (fanControlError == "" ? "" : " | Fan Control: " + fanControlError);
                        if (state != lastState) { lastState = state; Console.Error.WriteLine(state); }
                        await Task.Delay(1000, stop.Token);
                    }
                }, stop.Token);
                long sequence = 0; var clock = Stopwatch.StartNew(); double due = 0; DateTime nextOpen = DateTime.MinValue; string lastLhmError = "";
                while (!stop.IsCancellationRequested)
                {
                    SensorSnapshot sample;
                    if (options.Provider == 1) { sample = AfterburnerSource.Read(); sample.Source = "MSI Afterburner"; sample.CpuTemperatureSource = "MSI Afterburner"; }
                    else
                    {
                        try
                        {
                            if (lhm == null && DateTime.UtcNow >= nextOpen) lhm = new LibreHardwareSource();
                            sample = lhm?.Sample(options.GpuIdentifier) ?? new SensorSnapshot(); lastLhmError = "";
                        }
                        catch (Exception e) { if (e.Message != lastLhmError) { lastLhmError = e.Message; Console.Error.WriteLine("LHM: " + e.Message); } try { lhm?.Dispose(); } catch { } lhm = null; nextOpen = DateTime.UtcNow.AddSeconds(3); sample = new SensorSnapshot { Error = e.Message }; }
                    }
                    var fan = Volatile.Read(ref fans); if (fan.Ticks > 0 && (DateTime.UtcNow.Ticks - fan.Ticks) < 3 * TimeSpan.TicksPerSecond) { sample.Fans = fan.Fans; sample.FanSampleTicks = fan.Ticks; sample.FanSource = fan.Source; if (sample.Fresh && sample.Find(128, "CPU temperature") == null && fan.CpuTemperature.HasValue) { sample.Entries.Add(new Reading { Id = 128, Gpu = uint.MaxValue, Name = "CPU temperature", Unit = "C", Value = fan.CpuTemperature.Value }); sample.CpuTemperatureSource = "Fan Control"; } }
                    sample.SampleSequence = ++sequence;
                    // Afterburner timestamps must stay tied to its own samples, not the polling loop.
                    if (options.Provider == 0) sample.SampleTicks = DateTime.UtcNow.Ticks;
                    else if (sample.SampleTicks <= 0) sample.SampleTicks = DateTime.UtcNow.Ticks;
                    await Console.Out.WriteLineAsync(SnapshotProtocol.Serialize(sample)); await Console.Out.FlushAsync();
                    if (args.Contains("--once")) break;
                    due += options.IntervalMs; var delay = due - clock.Elapsed.TotalMilliseconds; if (delay > 0) await Task.Delay(TimeSpan.FromMilliseconds(delay), stop.Token); else if (delay < -options.IntervalMs) due = clock.Elapsed.TotalMilliseconds;
                }
            }
            return 0;
        }
        catch (OperationCanceledException) { return 0; }
        catch (IOException) when (pipe != null) { return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally { stop.Cancel(); lhm?.Dispose(); pipe?.Dispose(); }
    }
}
