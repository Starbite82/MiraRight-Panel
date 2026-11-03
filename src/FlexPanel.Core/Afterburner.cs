using System.IO.MemoryMappedFiles;
using System.Text;
namespace FlexPanel;

// Read-only implementation of the locally installed MSI Afterburner SDK's
// MAHMSharedMemory.h, v2 layout. No control API, DLL injection or setting writes.
public static class Afterburner {
 public record Entry(string Name,string Unit,double? Value,uint Gpu,uint Id,int Index);
 public record Gpu(uint Index,string Name,uint MemoryKB);
 public record Data(DateTime Timestamp,Entry[] Entries,Gpu[] Gpus);
 static uint U(byte[] b,int p)=>BitConverter.ToUInt32(b,p);
 static string Text(byte[] b,int p)=>Encoding.Latin1.GetString(b,p,260).Split('\0')[0];
 public static Data Parse(byte[] b,DateTime now){
  if(b.Length<32||U(b,0)!=0x4D41484D||U(b,4)<0x20000)throw new InvalidDataException("Afterburner-Daten sind nicht initialisiert (MAHM v2 erforderlich).");
  uint header=U(b,8),count=U(b,12),size=U(b,16),gpus=U(b,24),gpuSize=U(b,28);
  if(header<32||count>4096||size<1324||gpus>32||(gpus>0&&gpuSize<1304))throw new InvalidDataException("Ungültige MAHM-Tabellengrößen.");
  ulong end=(ulong)header+(ulong)count*size+(ulong)gpus*gpuSize;
  if(end>(ulong)b.Length)throw new InvalidDataException("Unvollständige Afterburner-Daten.");
  DateTime stamp=DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt32(b,20)).UtcDateTime;
  if((now-stamp).TotalSeconds>10||(stamp-now).TotalSeconds>5)throw new InvalidDataException("Afterburner-Daten sind älter als zehn Sekunden.");
  var entries=new List<Entry>();for(int i=0;i<count;i++){
   int p=checked((int)(header+(ulong)i*size));float value=BitConverter.ToSingle(b,p+1300);
   entries.Add(new(Text(b,p),Text(b,p+260),float.IsFinite(value)&&value!=float.MaxValue?value:null,U(b,p+1316),U(b,p+1320),i));
  }
  var devices=new List<Gpu>();for(uint i=0;i<gpus;i++){int p=checked((int)(header+(ulong)count*size+(ulong)i*gpuSize));devices.Add(new(i,Text(b,p+520),U(b,p+1300)));}
  return new(stamp,entries.ToArray(),devices.ToArray());
 }
 public static Data ReadMemory(){
  using var map=MemoryMappedFile.OpenExisting("MAHMSharedMemory",MemoryMappedFileRights.Read);using var view=map.CreateViewAccessor(0,0,MemoryMappedFileAccess.Read);
  byte[] header=new byte[32];view.ReadArray(0,header,0,32);
  ulong length=(ulong)U(header,8)+(ulong)U(header,12)*U(header,16)+(ulong)U(header,24)*U(header,28);
  if(length<32||length>16*1024*1024||length>(ulong)view.Capacity)throw new InvalidDataException("Ungültige Shared-Memory-Länge.");
  byte[] bytes=new byte[(int)length];view.ReadArray(0,bytes,0,bytes.Length);
  if(view.ReadUInt32(0)!=U(bytes,0)||view.ReadUInt32(20)!=U(bytes,20)||view.ReadUInt32(12)!=U(bytes,12))throw new InvalidDataException("Afterburner aktualisiert gerade; erneuter Versuch folgt.");
  return Parse(bytes,DateTime.UtcNow);
 }
 public static Snapshot Read(Snapshot baseline){
  try{return Build(ReadMemory(),baseline);}catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException){return new Snapshot{SourceName="MSI Afterburner",SensorStatus="MSI Afterburner nicht verfügbar: "+e.Message};}
 }
 static string Normalize(string s)=>string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();
 public static Snapshot Build(Data data,Snapshot baseline){
  var gpu=data.Gpus.FirstOrDefault(g=>Normalize(g.Name)==Normalize(baseline.GpuName));
  var rows=data.Entries;
  double? Global(uint id)=>rows.FirstOrDefault(e=>e.Id==id&&e.Gpu==uint.MaxValue)?.Value;
  double? OnGpu(uint id,string unit)=>gpu==null?null:rows.FirstOrDefault(e=>e.Id==id&&e.Gpu==gpu.Index&&e.Unit.Equals(unit,StringComparison.OrdinalIgnoreCase))?.Value;
  var threadEntries=rows.Where(e=>e.Id==0x90&&e.Gpu!=uint.MaxValue).OrderBy(e=>e.Gpu).ToArray();
  double?[] threads=threadEntries.Length==0?[]:new double?[Math.Min(256,(int)threadEntries.Max(e=>e.Gpu)+1)];
  foreach(var e in threadEntries)if(e.Gpu<threads.Length)threads[e.Gpu]=e.Value.HasValue?Math.Clamp(e.Value.Value,0,100):null;
  double? ram=rows.FirstOrDefault(e=>e.Id==0x91&&e.Unit.Equals("MB",StringComparison.OrdinalIgnoreCase))?.Value/1024;
  double? vram=OnGpu(0x31,"MB")/1024;
  double? vramTotal=gpu?.MemoryKB>0?gpu.MemoryKB/1048576d:gpu!=null?baseline.VramTotal:null;
  var readings=rows.Select(e=>new Reading("afterburner/"+e.Gpu,"MSI Afterburner",e.Id is 0x80 or 0x90 or 0x100?"Cpu":e.Id==0x91?"Memory":"Afterburner","afterburner/"+e.Index,e.Name,e.Unit,e.Value)).ToArray();
  return new Snapshot{At=data.Timestamp,SourceName="MSI Afterburner",CpuName=baseline.CpuName,GpuName=gpu?.Name??"GPU nicht zugeordnet",DiskName=baseline.DiskName,
   CpuLoad=Global(0x90),CpuTemp=Global(0x80),CpuWatts=Global(0x100),CpuThreads=threads,
   GpuLoad=OnGpu(0x30,"%"),GpuTemp=OnGpu(0,"°C"),GpuWatts=OnGpu(0x61,"W"),RamUsed=ram,RamTotal=baseline.RamTotal,RamFree=Free(baseline.RamTotal,ram),
   VramUsed=vram,VramTotal=vramTotal,VramFree=Free(vramTotal,vram),Sensors=readings,
   FanChannels=rows.Where(e=>e.Unit.Equals("RPM",StringComparison.OrdinalIgnoreCase)).Select(e=>new FanChannel("afterburner/"+e.Index,e.Name,"afterburner/gpu/"+e.Gpu,e.Id is 0x11 or 0x13 or 0x15?data.Gpus.FirstOrDefault(g=>g.Index==e.Gpu)?.Name??"":"",e.Id is 0x11 or 0x13 or 0x15?(int)(e.Id-0x11)/2+1:0,e.Value,"MSI Afterburner")).ToArray(),
   SensorStatus="MSI Afterburner · "+rows.Length+" Sensoren · Kapazitäten: Windows/LHM"+(gpu==null?" · ausgewählte GPU nicht zugeordnet":"")};
 }
 static double? Free(double? total,double? used)=>total.HasValue&&used.HasValue&&used>=0&&used<=total?total-used:null;
}
