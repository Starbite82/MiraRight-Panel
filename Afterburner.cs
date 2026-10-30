using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;

public sealed class Reading {
 public string Name,Unit;public float Value;public uint Id,Gpu;
 public bool Valid {get{return !float.IsNaN(Value)&&!float.IsInfinity(Value)&&Math.Abs(Value)<1e20;}}
}
public sealed class AfterburnerSnapshot {
 public Dictionary<string,double> BarValues=new Dictionary<string,double>();
 public List<Reading> Entries=new List<Reading>();public string Status;public bool Fresh;public double? VramTotalMb;public string Source="MSI Afterburner",CpuTemperatureSource="MSI Afterburner",GpuName="";public long SampleTicks,SampleSequence;
 public Reading Find(uint id,string exact){
  var r=Entries.Find(x=>x.Id==id&&x.Name.Equals(exact,StringComparison.OrdinalIgnoreCase));
  return r??Entries.Find(x=>x.Id==id);
 }
 public static AfterburnerSnapshot Read(){
  var result=new AfterburnerSnapshot();
  try{
   using(var map=MemoryMappedFile.OpenExisting("MAHMSharedMemory",MemoryMappedFileRights.Read))
   using(var view=map.CreateViewAccessor(0,0,MemoryMappedFileAccess.Read)){
    if(view.ReadUInt32(0)!=0x4D41484D)throw new Exception("Afterburner startet oder wurde beendet.");
    uint version=view.ReadUInt32(4),header=view.ReadUInt32(8),count=view.ReadUInt32(12),size=view.ReadUInt32(16);
    if(version<0x20000||header<32||size<1324||count>4096||(long)header+(long)count*size>view.Capacity)throw new Exception("Unbekanntes Afterburner-Datenformat.");
    uint time=view.ReadUInt32(20);result.SampleTicks=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(time).Ticks;
    double age=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds-time;
    result.Fresh=age>=-10&&age<15;result.Status=result.Fresh?"MSI AFTERBURNER":"SENSORDATEN VERALTET";
    for(int i=0;i<count;i++){
     long offset=header+(long)i*size;byte[] b=new byte[520];view.ReadArray(offset,b,0,b.Length);
     result.Entries.Add(new Reading{Name=String(b,0),Unit=String(b,260),Value=view.ReadSingle(offset+1300),Gpu=view.ReadUInt32(offset+1316),Id=view.ReadUInt32(offset+1320)});
    }
    result.VramTotalMb=ReadVramTotal(view);
    if(view.ReadUInt32(0)!=0x4D41484D)throw new Exception("Afterburner wurde beendet.");
   }
  }catch(Exception e){result.Fresh=false;result.Entries.Clear();result.Status="AFTERBURNER NICHT BEREIT";Log.LastSensorError=e.Message;}
  return result;
 }
 public static double? ReadVramTotal(MemoryMappedViewAccessor view){
  if(view.Capacity<32)return null;
  uint header=view.ReadUInt32(8),count=view.ReadUInt32(12),stride=view.ReadUInt32(16),gpuCount=view.ReadUInt32(24),gpuStride=view.ReadUInt32(28);
  long start=(long)header+(long)count*stride;
  if(gpuCount==0||gpuStride<1304||start<32||start+gpuStride>view.Capacity)return null;
  uint kib=view.ReadUInt32(start+1300);if(kib>0)return kib/1024.0;
  var nameBytes=new byte[260];view.ReadArray(start+520,nameBytes,0,260);string name=Encoding.Default.GetString(nameBytes).Trim((char)0);return GpuMemory.TotalMb(name);
 }

 static string String(byte[] b,int offset){int n=0;while(n<260&&b[offset+n]!=0)n++;return Encoding.Default.GetString(b,offset,n);}
}
