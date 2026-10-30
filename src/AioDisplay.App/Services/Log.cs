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
    public static string Root = AppDomain.CurrentDomain.BaseDirectory, LastSensorError = "";
    public static void Write(string s) { try { var path = Path.Combine(Root, "panel.log"); if (File.Exists(path) && new FileInfo(path).Length > 1048576) File.WriteAllText(path, ""); File.AppendAllText(path, DateTime.Now.ToString("s") + " " + s + Environment.NewLine); } catch (IOException) { } }
}
