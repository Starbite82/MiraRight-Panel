using System.Diagnostics;
namespace FlexPanel;

// Averages actual sensor samples only. Drawing the same sample again never adds weight.
public sealed class BarAverager {
 readonly Dictionary<string,Queue<(double at,double value)>> history=new();
 string signature="";int window=-1;DateTime lastSample=DateTime.MinValue;
 public Snapshot Apply(Snapshot snapshot,PanelConfig config)=>Apply(snapshot,config,Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency);
 public Snapshot Apply(Snapshot snapshot,PanelConfig config,double now){
  string identity=snapshot.SourceName+"|"+snapshot.GpuName+"|"+config.GpuId+"|"+string.Join('|',config.SensorSelections.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value));
  if(config.BarWindowMs<=0){Reset();return snapshot with{BarValues=new Dictionary<string,double>()};}
  if(window!=config.BarWindowMs||signature!=identity){Reset();window=config.BarWindowMs;signature=identity;}
  bool isNew=snapshot.At!=lastSample;var result=new Dictionary<string,double>();var active=new HashSet<string>();
  void Add(string key,double? raw){
   if(raw is not double value||!double.IsFinite(value)||value<0)return;
   active.Add(key);if(!history.TryGetValue(key,out var samples))history[key]=samples=new();
   if(isNew)samples.Enqueue((now,value));
   while(samples.Count>0&&samples.Peek().at<=now-window)samples.Dequeue();
   if(samples.Count>0)result[key]=samples.Average(p=>p.value);
  }
  Add("cpu",snapshot.CpuLoad);Add("gpu",snapshot.GpuLoad);Add("ram",snapshot.RamUsed);Add("vram",snapshot.VramUsed);
  for(int i=0;i<snapshot.CpuThreads.Length;i++)Add("thread:"+i,snapshot.CpuThreads[i]);
  foreach(string key in history.Keys.Where(k=>!active.Contains(k)).ToArray())history.Remove(key);
  lastSample=snapshot.At;return snapshot with{BarValues=result};
 }
 public void Reset(){history.Clear();signature="";window=-1;lastSample=DateTime.MinValue;}
}
