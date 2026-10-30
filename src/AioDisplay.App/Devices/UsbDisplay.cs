namespace AioDisplay.App;

using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

public sealed class UsbDisplay : IDisposable
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr sa, uint creation, uint flags, IntPtr template);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_Initialize(SafeFileHandle file, out IntPtr usb);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_Free(IntPtr usb);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_QueryInterfaceSettings(IntPtr usb, byte alt, out InterfaceDescriptor desc);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_QueryPipe(IntPtr usb, byte alt, byte index, out PipeInformation pipe);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_SetPipePolicy(IntPtr usb, byte pipe, uint policy, uint length, ref uint value);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_WritePipe(IntPtr usb, byte pipe, byte[] data, uint length, out uint transferred, IntPtr overlap);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_ReadPipe(IntPtr usb, byte pipe, byte[] data, uint length, out uint transferred, IntPtr overlap);
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct InterfaceDescriptor { public byte Length, Type, Number, Alt, Endpoints, Class, SubClass, Protocol, Index; }
    [StructLayout(LayoutKind.Sequential)] struct PipeInformation { public int Type; public byte Id; public ushort MaxPacketSize; public byte Interval; }
    SafeFileHandle file; IntPtr usb; byte output, input; public int Pm, Sub;
    static void Check(bool ok, string operation) { if (!ok) throw new Win32Exception(Marshal.GetLastWin32Error(), operation); }
    public void Open()
    {
        // Only the installed LCD interface; no driver replacement or firmware access.
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceClasses\{1d4b2365-4749-48ea-b38a-7c6fdddd7e26}"))
        {
            if (key == null) throw new IOException("LCD-Schnittstelle nicht registriert.");
            var candidates = key.GetSubKeyNames().Where(n => n.IndexOf("VID_87AD&PID_70DB", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (candidates.Length != 1) throw new IOException("Erwartet genau ein 87AD:70DB Display, gefunden: " + candidates.Length);
            string path = candidates[0].Replace("##?#", @"\\?\");
            file = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            if (file.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "LCD oeffnen");
        }
        Check(WinUsb_Initialize(file, out usb), "WinUSB initialisieren");
        InterfaceDescriptor desc; Check(WinUsb_QueryInterfaceSettings(usb, 0, out desc), "USB-Interface lesen");
        for (byte i = 0; i < desc.Endpoints; i++) { PipeInformation pipe; Check(WinUsb_QueryPipe(usb, 0, i, out pipe), "USB-Pipe lesen"); if (pipe.Type == 2) { if ((pipe.Id & 128) != 0) input = pipe.Id; else output = pipe.Id; } }
        if (output != 1 || input == 0) throw new IOException("Unerwartete LCD-Endpunkte.");
        uint timeout = 1500; Check(WinUsb_SetPipePolicy(usb, output, 3, 4, ref timeout), "Schreibtimeout"); Check(WinUsb_SetPipePolicy(usb, input, 3, 4, ref timeout), "Lesetimeout");
    }
    public byte[] Handshake() { var b = new byte[64]; b[0] = 0x12; b[1] = 0x34; b[2] = 0x56; b[3] = 0x78; b[56] = 1; Write(b); var response = new byte[1024]; uint n; Check(WinUsb_ReadPipe(usb, input, response, 1024, out n, IntPtr.Zero), "LCD-Handshake lesen"); if (n < 41 || response[24] == 0) throw new IOException("Ungueltige LCD-Antwort."); Pm = response[24]; Sub = response[36]; Array.Resize(ref response, (int)n); return response; }
    void Write(byte[] data) { uint n; Check(WinUsb_WritePipe(usb, output, data, (uint)data.Length, out n, IntPtr.Zero), "LCD-Frame senden"); if (n != data.Length) throw new IOException("Unvollstaendige USB-Uebertragung."); }
    public void Send(byte[] pixels)
    {
        // Confirmed handshake PM=32 SUB=1: 320x320, RGB565 big endian (FBL 100).
        if (Pm != 32 || Sub != 1) throw new IOException("Modell noch nicht freigegeben: PM=" + Pm + " SUB=" + Sub);
        if (pixels.Length != 320 * 320 * 2) throw new IOException("Ungueltige Framegroesse.");
        var frame = new byte[64 + pixels.Length]; frame[0] = 0x12; frame[1] = 0x34; frame[2] = 0x56; frame[3] = 0x78;
        Action<int, int> put = (o, v) => Buffer.BlockCopy(BitConverter.GetBytes(v), 0, frame, o, 4);
        put(4, 3); put(8, 320); put(12, 320); put(56, 2); put(60, pixels.Length); Buffer.BlockCopy(pixels, 0, frame, 64, pixels.Length);
        for (int o = 0; o < frame.Length; o += 16384) { var b = new byte[Math.Min(16384, frame.Length - o)]; Buffer.BlockCopy(frame, o, b, 0, b.Length); Write(b); }
        if (frame.Length % 512 == 0) Write(new byte[0]);
    }
    public void Dispose() { if (usb != IntPtr.Zero) { WinUsb_Free(usb); usb = IntPtr.Zero; } if (file != null) file.Dispose(); }
}
