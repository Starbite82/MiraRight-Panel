// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 AIO Display contributors

namespace AioDisplay.App;

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
public static class Log
{
    static readonly object debugLock = new();
    public static string DebugPath => Path.Combine(Root, "debug.log");
    public static void Debug(string name, object data = null)
    {
        try { lock (debugLock) {
            if (File.Exists(DebugPath) && new FileInfo(DebugPath).Length >= 1048576) {
                for (int i=2;i>=0;i--) { string from=i==0?DebugPath:DebugPath+"."+i; if(File.Exists(from)) File.Move(from,DebugPath+"."+(i+1),true); }
            }
            string details=System.Text.Json.JsonSerializer.Serialize(data);
            if(details.Length>8192)details=System.Text.Json.JsonSerializer.Serialize(new { Truncated=true, Details=details[..8192] });
            string line=System.Text.Json.JsonSerializer.Serialize(new { At=DateTime.UtcNow, Process=Environment.ProcessId, Event=name, Data=System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(details) });
            File.AppendAllText(DebugPath,line+Environment.NewLine,new System.Text.UTF8Encoding(false));
        }} catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
    public static string Root = AppDomain.CurrentDomain.BaseDirectory, LastSensorError = "";
    public static void Write(string s) { try { var path = Path.Combine(Root, "panel.log"); if (File.Exists(path) && new FileInfo(path).Length > 1048576) File.WriteAllText(path, ""); File.AppendAllText(path, DateTime.Now.ToString("s") + " " + s + Environment.NewLine); } catch (IOException) { } }
}
