// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

// The task has a fixed executable in an administrator-protected directory. Only
// the random pipe name and the UI identity are supplied at run time, never commands.
public static class ScheduledSensorTask
{
    public static string Name => "MiraRight Panel Sensors-" + WindowsIdentity.GetCurrent().User.Value;
    public static string HostPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MiraRight Panel", "Sensors", "MiraRightPanel.SensorHost.exe");
    public const string Arguments = "--pipe $(Arg0) --parent $(Arg1) --parent-start $(Arg2)";
    static dynamic Service() { dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")); service.Connect(); return service; }
    public static bool Installed
    {
        get { try { dynamic task = Service().GetFolder("\\").GetTask(Name); return task.Enabled && Valid(task.Definition); } catch { return false; } }
    }
    static bool Valid(dynamic definition) => definition.Actions.Count == 1 &&
        string.Equals((string)definition.Actions.Item(1).Path, HostPath, StringComparison.OrdinalIgnoreCase) &&
        (string)definition.Actions.Item(1).Arguments == Arguments &&
        PrincipalSid((string)definition.Principal.UserId) == WindowsIdentity.GetCurrent().User.Value &&
        (int)definition.Principal.RunLevel == 1 && (int)definition.Principal.LogonType == 3;
    static string PrincipalSid(string user) => user.StartsWith("S-1-", StringComparison.Ordinal) ? new SecurityIdentifier(user).Value : ((SecurityIdentifier)new NTAccount(user).Translate(typeof(SecurityIdentifier))).Value;
    public static void Run(string pipe, SensorOptions options)
    {
        if (!pipe.StartsWith("AioDisplay-", StringComparison.Ordinal) || !Guid.TryParseExact(pipe[11..], "N", out _) || options.ParentId != Environment.ProcessId || options.ParentStartTicks <= 0)
            throw new InvalidDataException("Ungültige Sensor-Sitzung.");
        dynamic task = Service().GetFolder("\\").GetTask(Name);
        if (!task.Enabled || !Valid(task.Definition)) throw new InvalidDataException("Sensoraufgabe bitte neu einrichten.");
        task.Run(new string[] { pipe, options.ParentId.ToString(System.Globalization.CultureInfo.InvariantCulture), options.ParentStartTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) });
    }
    internal static Process ConnectedProcess(uint pid)
    {
        const uint QueryLimitedInformation = 0x1000;
        IntPtr handle = OpenProcess(QueryLimitedInformation, false, pid);
        if (handle == IntPtr.Zero) throw new IOException("Sensorprozess kann nicht geprüft werden.");
        try {
            var path = new StringBuilder(32768); int length = path.Capacity;
            if (!QueryFullProcessImageName(handle, 0, path, ref length) || !string.Equals(path.ToString(), HostPath, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Unerwarteter Sensorprozess am Datenkanal.");
        } finally { CloseHandle(handle); }
        return Process.GetProcessById(checked((int)pid));
    }
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder path, ref int size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
}
