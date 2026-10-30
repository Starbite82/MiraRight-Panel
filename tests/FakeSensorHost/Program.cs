// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

using System.Text.Json;
var options = JsonSerializer.Deserialize<SensorOptions>(Console.ReadLine());
if (options.GpuIdentifier == "crash") return 2;
long seq = 0;
while (true)
{
    if (options.GpuIdentifier == "invalid") Console.WriteLine("{bad json}");
    else if (options.GpuIdentifier != "freeze" || seq < 5) { var s = new SensorSnapshot { Fresh = true, SampleTicks = DateTime.UtcNow.Ticks, SampleSequence = ++seq }; s.Entries.Add(new Reading { Id = 144, Name = "CPU usage", Value = 42 }); Console.WriteLine(SnapshotProtocol.Serialize(s)); }
    await Task.Delay(100);
}
