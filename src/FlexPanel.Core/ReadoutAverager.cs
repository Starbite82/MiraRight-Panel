// SPDX-License-Identifier: MIT
namespace FlexPanel;

// Display-only means. Sensor snapshots and the independent bar history stay unchanged.
public sealed class ReadoutAverager {
 readonly Dictionary<string,Queue<(double at,double value)>> history=new();
 string identity="";int window=-1;long previous=long.MinValue;
 static double Now=>System.Diagnostics.Stopwatch.GetTimestamp()*1000d/System.Diagnostics.Stopwatch.Frequency;
 void Prepare(string key,int milliseconds){if(identity!=key||window!=milliseconds){Reset();identity=key;window=milliseconds;}}
 double? Mean(string key,double? raw,long sample,double now){
  if(raw is not double value||!double.IsFinite(value)){history.Remove(key);return null;}
  if(window<=0)return value;
  if(!history.TryGetValue(key,out var values))history[key]=values=new();
  if(sample!=previous)values.Enqueue((now,value));
  while(values.Count>0&&values.Peek().at<=now-window)values.Dequeue();
  return values.Count>0?values.Average(v=>v.value):value;
 }
 public Snapshot Apply(Snapshot s,PanelConfig config,int milliseconds)=>Apply(s,config,milliseconds,Now);
 public Snapshot Apply(Snapshot s,PanelConfig config,int milliseconds,double now){
  Prepare(s.SourceName+"|"+s.GpuName+"|"+config.GpuId+"|"+s.RamTotal+"|"+s.VramTotal+"|"+string.Join('|',config.SensorSelections.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value)),milliseconds);
  var active=new HashSet<string>();
  double? M(string key,double? value){active.Add(key);return Mean(key,value,s.At.Ticks,now);}
  double? ram=M("ram",s.RamUsed),vram=M("vram",s.VramUsed);
  var bars=new Dictionary<string,double>(s.BarValues);
  void RawBar(string key,double? value){if(!bars.ContainsKey(key)&&value is double v&&double.IsFinite(v))bars[key]=v;}
  RawBar("cpu",s.CpuLoad);RawBar("gpu",s.GpuLoad);RawBar("ram",s.RamUsed);RawBar("vram",s.VramUsed);
  for(int i=0;i<s.CpuThreads.Length;i++)RawBar("thread:"+i,s.CpuThreads[i]);
  var result=s with{
   BarValues=bars,
   CpuLoad=M("cpu.load",s.CpuLoad),CpuTemp=M("cpu.temp",s.CpuTemp),CpuWatts=M("cpu.watts",s.CpuWatts),
   GpuLoad=M("gpu.load",s.GpuLoad),GpuTemp=M("gpu.temp",s.GpuTemp),GpuWatts=M("gpu.watts",s.GpuWatts),
   RamUsed=ram,RamFree=s.RamFree.HasValue&&ram.HasValue&&s.RamTotal.HasValue?Math.Max(0,s.RamTotal.Value-ram.Value):M("ram.free",s.RamFree),RamTemp=M("ram.temp",s.RamTemp),RamWatts=M("ram.watts",s.RamWatts),
   VramUsed=vram,VramFree=s.VramFree.HasValue&&vram.HasValue&&s.VramTotal.HasValue?Math.Max(0,s.VramTotal.Value-vram.Value):M("vram.free",s.VramFree),
   SsdTemp=M("ssd.temp",s.SsdTemp),DiskLoad=M("disk.load",s.DiskLoad),
   CpuThreads=s.CpuThreads.Select((v,i)=>M("thread:"+i,v)).ToArray(),
   Fans=s.Fans.Select(f=>f with{Rpm=M("fan.rpm:"+f.Id+"|"+f.RpmSource,f.Rpm),Temperature=M("fan.temp:"+f.Id+"|"+f.TemperatureSensor+"|"+f.TemperatureSource,f.Temperature)}).ToArray()
  };
  foreach(string key in history.Keys.Where(k=>!active.Contains(k)).ToArray())history.Remove(key);
  previous=s.At.Ticks;return result;
 }
 public Snapshot ApplyFrames(Snapshot s,long sequence,int milliseconds)=>ApplyFrames(s,sequence,milliseconds,Now);
 public Snapshot ApplyFrames(Snapshot s,long sequence,int milliseconds,double now){
  Prepare(s.FrameApp,milliseconds);
  double? fps=Mean("fps",s.Fps,sequence,now),ms=Mean("frame.ms",s.FrameMs,sequence,now);
  previous=sequence;
  return s with{Fps=s.Fps.HasValue&&s.FrameMs>0&&ms>0?1000/ms:fps,FrameMs=ms};
 }
 public void Reset(){history.Clear();identity="";window=-1;previous=long.MinValue;}
}
