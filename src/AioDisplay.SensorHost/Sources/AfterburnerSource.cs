// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.SensorHost;

using System;
using System.Text;
using System.IO.MemoryMappedFiles;
public static class AfterburnerSource
{
    private static readonly Encoding Ansi = WindowsEncoding();
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern uint GetACP();
    private static Encoding WindowsEncoding() { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding((int)GetACP()); }
    public static SensorSnapshot Read()
    {
        var result = new SensorSnapshot();
        try
        {
            using (var map = MemoryMappedFile.OpenExisting("MAHMSharedMemory", MemoryMappedFileRights.Read))
            using (var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
            {
                if (view.ReadUInt32(0) != 0x4D41484D) throw new Exception("Afterburner startet oder wurde beendet.");
                uint version = view.ReadUInt32(4), header = view.ReadUInt32(8), count = view.ReadUInt32(12), size = view.ReadUInt32(16);
                if (version < 0x20000 || header < 32 || size < 1324 || count > 4096 || (long)header + (long)count * size > view.Capacity) throw new Exception("Unbekanntes Afterburner-Datenformat.");
                uint time = view.ReadUInt32(20); result.SampleTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time).Ticks;
                double age = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - time;
                result.Fresh = age >= -10 && age < 15; result.Status = result.Fresh ? "MSI AFTERBURNER" : "SENSORDATEN VERALTET";
                for (int i = 0; i < count; i++)
                {
                    long offset = header + (long)i * size; byte[] b = new byte[520]; view.ReadArray(offset, b, 0, b.Length);
                    var reading = new Reading { Name = String(b, 0), Unit = String(b, 260), Value = view.ReadSingle(offset + 1300), Gpu = view.ReadUInt32(offset + 1316), Id = view.ReadUInt32(offset + 1320) };
                    if (reading.Valid) result.Entries.Add(reading);
                }
                result.VramTotalMb = ReadVramTotal(view);
                if (view.ReadUInt32(0) != 0x4D41484D) throw new Exception("Afterburner wurde beendet.");
            }
        }
        catch (Exception e) { result.Fresh = false; result.Entries.Clear(); result.Status = "AFTERBURNER NICHT BEREIT"; result.Error = e.Message; }
        return result;
    }
    public static double? ReadVramTotal(MemoryMappedViewAccessor view)
    {
        if (view.Capacity < 32) return null;
        uint header = view.ReadUInt32(8), count = view.ReadUInt32(12), stride = view.ReadUInt32(16), gpuCount = view.ReadUInt32(24), gpuStride = view.ReadUInt32(28);
        long start = (long)header + (long)count * stride;
        if (gpuCount == 0 || gpuStride < 1304 || start < 32 || start + gpuStride > view.Capacity) return null;
        uint kib = view.ReadUInt32(start + 1300); if (kib > 0) return kib / 1024.0;
        var nameBytes = new byte[260]; view.ReadArray(start + 520, nameBytes, 0, 260); string name = Ansi.GetString(nameBytes).Trim((char)0); return GpuMemory.TotalMb(name);
    }

    static string String(byte[] b, int offset) { int n = 0; while (n < 260 && b[offset + n] != 0) n++; return Ansi.GetString(b, offset, n); }
}
