// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.SensorHost;

using System;
using System.Runtime.InteropServices;
public static class GpuMemory
{
    [DllImport("dxgi.dll")] static extern int CreateDXGIFactory1(ref Guid iid, out IntPtr factory);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int EnumAdapters(IntPtr factory, uint index, out IntPtr adapter);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetDescription(IntPtr adapter, out Description desc);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Description
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Name;
        public uint VendorId, DeviceId, SubsystemId, Revision;
        public UIntPtr DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
        public uint LuidLow; public int LuidHigh; public uint Flags;
    }
    static string cachedName; static double? cachedValue; static DateTime checkedAt;
    static Delegate Method(IntPtr instance, int index, Type type) { return Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), index * IntPtr.Size), type); }
    public static double? TotalMb(string gpuName)
    {
        if (string.IsNullOrWhiteSpace(gpuName)) return null;
        if (gpuName == cachedName && (cachedValue.HasValue || (DateTime.UtcNow - checkedAt).TotalSeconds < 30)) return cachedValue;
        cachedName = gpuName; checkedAt = DateTime.UtcNow; cachedValue = null; IntPtr factory = IntPtr.Zero;
        try
        {
            var iid = new Guid("770aae78-f26f-4dba-a829-253c83d1b387"); if (CreateDXGIFactory1(ref iid, out factory) < 0) return null;
            var enumerate = (EnumAdapters)Method(factory, 12, typeof(EnumAdapters));
            for (uint i = 0; i < 32; i++)
            {
                IntPtr adapter; if (enumerate(factory, i, out adapter) < 0) break;
                try { Description desc; var get = (GetDescription)Method(adapter, 10, typeof(GetDescription)); if (get(adapter, out desc) >= 0 && string.Equals(desc.Name.Trim(), gpuName.Trim(), StringComparison.OrdinalIgnoreCase) && desc.DedicatedVideoMemory.ToUInt64() > 0) { cachedValue = desc.DedicatedVideoMemory.ToUInt64() / 1048576.0; break; } }
                finally { Marshal.Release(adapter); }
            }
        }
        catch (COMException) { }
        catch (DllNotFoundException) { }
        finally { if (factory != IntPtr.Zero) Marshal.Release(factory); }
        return cachedValue;
    }
}
