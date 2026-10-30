namespace AioDisplay.App;

using System.IO;
using System.Text;
using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
public static class SnapshotFiles
{
    public static string LastWriteError = "";
    static readonly int processId = Process.GetCurrentProcess().Id;
    public static bool Publish(string path, string[] lines)
    {
        string temp = path + "." + processId + ".tmp";
        try
        {
            File.WriteAllLines(temp, lines, new UTF8Encoding(false));
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try { if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path); LastWriteError = ""; return true; }
                catch (IOException) { if (attempt == 5) throw; Thread.Sleep(10); }
                catch (UnauthorizedAccessException) { if (attempt == 5) throw; Thread.Sleep(10); }
            }
        }
        catch (Exception e) { LastWriteError = e.Message; }
        finally { if (File.Exists(temp)) { try { File.Delete(temp); } catch { } } }
        return false;
    }
    public static string ReadText(string path)
    {
        for (int attempt = 0; ; attempt++)
        {
            try { using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (var reader = new StreamReader(file, Encoding.UTF8, true)) return reader.ReadToEnd(); }
            catch (IOException) { if (attempt >= 9) throw; Thread.Sleep(2); }
            catch (UnauthorizedAccessException) { if (attempt >= 9) throw; Thread.Sleep(2); }
        }
    }
    public static string[] ReadLines(string path) { var lines = new List<string>(); using (var reader = new StringReader(ReadText(path))) { string line; while ((line = reader.ReadLine()) != null) lines.Add(line); } return lines.ToArray(); }
}
