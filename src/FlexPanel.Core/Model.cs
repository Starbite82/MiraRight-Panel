using System.Text.Json;
namespace FlexPanel;

public sealed class PanelConfig {
 public string[] Cards {get;set;} = ["CPU","GPU","RAM","VRAM","FRAMES","FANS"];
 public string FanControlConfigPath {get;set;} = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"FanControl","Configurations","userConfig.json");
 public string Accent {get;set;} = "#42DED2";
 public string GpuId {get;set;} = "";
 public string DiskId {get;set;} = "";
 public string GameProcess {get;set;} = "AUTO";
 public string SensorSource {get;set;} = "LibreHardwareMonitor";
 public string DeviceSerial {get;set;} = "";
 public int Brightness {get;set;} = 65;
 public bool Rotate180 {get;set;}
 public bool ThreadNumbers {get;set;}
 public bool UsbEnabled {get;set;} = true;
 public int RefreshMs {get;set;} = 500;
 public string Design {get;set;} = "Classic";
 public int SensorMs {get;set;}
 public int DisplayFps {get;set;}
 public int BarWindowMs {get;set;}=500;
 public static readonly int[] BarWindows=[0,250,500,1000,2000];
 public Dictionary<string,string> SensorSelections {get;set;} = new();
 public static readonly int[] SensorIntervals=[1000,500,250,125,100];
 public static readonly int[] DisplayRates=[1,2,4,8,10,30,60];
 public PanelConfig Normalize(){
  if(!SensorIntervals.Contains(SensorMs))SensorMs=SensorIntervals.MinBy(x=>Math.Abs(x-RefreshMs));
  if(!DisplayRates.Contains(DisplayFps))DisplayFps=DisplayRates.MinBy(x=>Math.Abs(1000d/x-Math.Max(1,RefreshMs)));
  if(Design!="Thermalright")Design="Classic";
  string[] allowed=["CPU","GPU","RAM","VRAM","FRAMES","FANS"];
  Cards=(Cards??[]).Select(c=>c=="SSD"?"FANS":c).Where(allowed.Contains).Distinct().ToArray();if(Cards.Length==0)Cards=allowed;
  if(!BarWindows.Contains(BarWindowMs))BarWindowMs=500;
  SensorSelections??=new();return this;
 }
 public static string PathName => Path.Combine(AppContext.BaseDirectory,"panel.json");
 public static PanelConfig Load() {
  if(!File.Exists(PathName)) return new PanelConfig().Normalize();
  var c=JsonSerializer.Deserialize<PanelConfig>(File.ReadAllText(PathName)) ?? new();
  c.RefreshMs=Math.Clamp(c.RefreshMs,250,5000);c.Brightness=Math.Clamp(c.Brightness,1,100);
  if(c.SensorSource!="MSIAfterburner")c.SensorSource="LibreHardwareMonitor";
  bool migrated=c.Cards.Contains("SSD");
  string[] valid=["CPU","GPU","RAM","VRAM","FRAMES","FANS"];
  c.Cards=c.Cards.Select(card=>card=="SSD"?"FANS":card).Where(valid.Contains).Distinct().ToArray();
  if(c.Cards.Length==0)c.Cards=valid;
  _=ColorTranslator.FromHtml(c.Accent);c.Normalize();if(migrated)c.Save();return c;
 }
 public void Save()=>File.WriteAllText(PathName,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));
}
public record Reading(string HardwareId,string Hardware,string Type,string Id,string Name,string Unit,double? Value);
public record FanChannel(string Id,string Name,string HardwareId,string Hardware,int Index,double? Rpm,string Source);
public record FanReading(string Id,string Name,double? Rpm,string RpmSource,double? Temperature,string TemperatureSensor,string TemperatureSource,string Reference,string Curve);
public sealed record Snapshot {
 public DateTime At {get;init;} = DateTime.UtcNow;
 public double? CpuLoad {get;init;} public double? CpuTemp {get;init;} public double? CpuWatts {get;init;}
 public double?[] CpuThreads {get;init;}=[];
 public IReadOnlyDictionary<string,double> BarValues {get;init;}=new Dictionary<string,double>();
 public double? GpuLoad {get;init;} public double? GpuTemp {get;init;} public double? GpuWatts {get;init;}
 public double? RamUsed {get;init;} public double? RamTotal {get;init;} public double? RamFree {get;init;}
 public double? RamTemp {get;init;} public double? RamWatts {get;init;}
 public double? VramUsed {get;init;} public double? VramTotal {get;init;} public double? VramFree {get;init;}
 public double? SsdTemp {get;init;} public double? DiskLoad {get;init;}
 public double? Fps {get;init;} public double? FrameMs {get;init;}
 public string CpuName {get;init;}="";public string GpuName {get;init;}="";public string DiskName {get;init;}="";
 public string FrameStatus {get;init;}="";public string SensorStatus {get;init;}="";
 public string FrameApp {get;init;}="";
 public string SourceName {get;init;}="LibreHardwareMonitor";
 public Reading[] Sensors {get;init;}=[];
 public FanChannel[] FanChannels {get;init;}=[];
 public FanReading[] Fans {get;init;}=[];
 public string FanStatus {get;init;}="";
}
