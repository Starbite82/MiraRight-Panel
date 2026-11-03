using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace FlexPanel;

public sealed class Sensors : IDisposable {
 readonly Computer computer=new(){IsCpuEnabled=true,IsGpuEnabled=true,IsMemoryEnabled=true,IsStorageEnabled=true,IsMotherboardEnabled=true};
 string error="";
 public static bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
 public Sensors(){try{computer.Open();}catch(Exception e){error=e.Message;}}
 static IEnumerable<IHardware> Flatten(IHardware h){yield return h;foreach(var child in h.SubHardware)foreach(var sub in Flatten(child))yield return sub;}
 // GenericCpu assigns per-logical-thread load sensors index 2 + thread number.
 // Sort the numeric sensor index, not the display name (which would place #10 before #2).
 public static double?[] ThreadLoads(IEnumerable<Reading> readings)=>readings
  .Where(s=>s.Unit=="Load"&&s.Name.StartsWith("CPU Core #",StringComparison.OrdinalIgnoreCase))
  .Select(s=>new{Reading=s,Index=int.TryParse(s.Id.Split('/').Last(),out int index)?index:-1})
  .Where(s=>s.Index>=2).OrderBy(s=>s.Index).Select(s=>s.Reading.Value is double value&&double.IsFinite(value)?(double?)Math.Clamp(value,0,100):null).ToArray();
 public Snapshot Read(PanelConfig c,bool update=true) {
  var hw=computer.Hardware.SelectMany(Flatten).ToArray();var updateErrors=new List<string>();
  foreach(var h in update?hw:[])try{h.Update();}catch(Exception e){updateErrors.Add(h.Name+": "+e.Message);}
  var rows=hw.SelectMany(h=>h.Sensors.Select(s=>new Reading(h.Identifier.ToString(),h.Name,h.HardwareType.ToString(),s.Identifier.ToString(),s.Name,s.SensorType.ToString(),s.Value is float f&&float.IsFinite(f)?f:null))).ToArray();
  var cpu=hw.FirstOrDefault(h=>h.HardwareType==HardwareType.Cpu);
  var gpus=hw.Where(h=>h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel).ToArray();
  var gpu=gpus.FirstOrDefault(h=>h.Identifier.ToString()==c.GpuId)??gpus.OrderByDescending(h=>h.Sensors.Where(s=>s.Name=="GPU Memory Total"&&s.Value.HasValue).Select(s=>(double)s.Value!.Value).DefaultIfEmpty(0).Max()).FirstOrDefault();
  var disks=hw.Where(h=>h.HardwareType==HardwareType.Storage).ToArray();
  // First-run match from the detected system configuration; user can choose any drive by ID.
  var disk=disks.FirstOrDefault(h=>h.Identifier.ToString()==c.DiskId)??disks.FirstOrDefault();
  Reading[] For(IHardware? h)=>rows.Where(s=>s.HardwareId==h?.Identifier.ToString()).ToArray();
  double? Pick(Reading[] rs,string type,params string[] names){foreach(var name in names){var s=rs.FirstOrDefault(s=>s.Unit==type&&s.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&s.Value.HasValue);if(s!=null)return s.Value;}return null;}
  var cr=For(cpu);var gr=For(gpu);var dr=For(disk);
  var mem=new MemoryStatus();mem.Length=(uint)Marshal.SizeOf<MemoryStatus>();bool hasMem=GlobalMemoryStatusEx(ref mem);
  var ramSensors=rows.Where(s=>s.Type=="Memory" || (s.Name.Contains("DIMM",StringComparison.OrdinalIgnoreCase)&&!s.Name.Contains("Voltage"))).ToArray();
  var ramTemperatures=ramSensors.Where(s=>s.Unit=="Temperature"&&s.Value.HasValue&&!s.Name.Contains("Limit")&&!s.Name.Contains("Resolution")).Select(s=>s.Value!.Value).ToArray();
  // Memory controller / SoC power is not DIMM power. Only accept explicit module measurements.
  var ramPower=ramSensors.Where(s=>s.Unit=="Power"&&s.Value.HasValue).ToArray();
  double? vrUsed=Pick(gr,"SmallData","GPU Memory Used")/1024 ?? Pick(gr,"Data","GPU Memory Used");
  double? vrTotal=Pick(gr,"SmallData","GPU Memory Total")/1024 ?? Pick(gr,"Data","GPU Memory Total");
  double? vrFree=vrTotal-vrUsed;
  var status=error.Length>0?error:!IsAdmin?"Standardmodus · CPU-/RAM-Sensoren können Administratorrechte benötigen":updateErrors.Count>0?string.Join("; ",updateErrors):"Sensoren lokal · LibreHardwareMonitor";
  double? cpuTemp=Pick(cr,"Temperature","CPU Package","Core (Tctl/Tdie)","CPU (Tctl/Tdie)","Core (Tdie)","Core Average");
  double? cpuPower=Pick(cr,"Power","CPU Package","Package");
  return new(){CpuName=cpu?.Name??"CPU",GpuName=gpu?.Name??"GPU",DiskName=disk?.Name??"Systemplatte",
   CpuLoad=Pick(cr,"Load","CPU Total"),CpuTemp=cpuTemp>0?cpuTemp:null,CpuWatts=cpuPower>0?cpuPower:null,CpuThreads=ThreadLoads(cr),
   GpuLoad=Pick(gr,"Load","GPU Core","D3D 3D"),GpuTemp=Pick(gr,"Temperature","GPU Core"),GpuWatts=Pick(gr,"Power","GPU Package","GPU Power","GPU Total","GPU Core"),
   RamUsed=hasMem?(mem.TotalPhysical-mem.AvailablePhysical)/1073741824d:null,RamTotal=hasMem?mem.TotalPhysical/1073741824d:null,RamFree=hasMem?mem.AvailablePhysical/1073741824d:null,
   RamTemp=ramTemperatures.Length>0?ramTemperatures.Max():null,RamWatts=ramPower.Length>0?ramPower.Sum(x=>x.Value!.Value):null,
   VramUsed=vrUsed,VramTotal=vrTotal,VramFree=vrFree,SsdTemp=Pick(dr,"Temperature","Temperature","Composite Temperature")??dr.FirstOrDefault(s=>s.Unit=="Temperature"&&s.Value.HasValue)?.Value,
   DiskLoad=Pick(dr,"Load","Total Activity"),SensorStatus=status,Sensors=rows,
   FanChannels=rows.Where(r=>r.Unit=="Fan").Select(r=>new FanChannel(r.Id,r.Name,r.HardwareId,r.Hardware,int.TryParse(r.Id.Split('/').Last(),out var index)?index:0,r.Value,"LibreHardwareMonitor")).ToArray()};
 }
 [StructLayout(LayoutKind.Sequential)]struct MemoryStatus {public uint Length,Load;public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtended;}
 [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
 public void Dispose()=>computer.Close();
}
