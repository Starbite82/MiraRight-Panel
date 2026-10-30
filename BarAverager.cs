using System;
using System.Collections.Generic;
using System.Diagnostics;
public sealed class BarAverager {
 sealed class Sample {public double Time,Value;}
 readonly Dictionary<string,Queue<Sample>> history=new Dictionary<string,Queue<Sample>>();
 string source;int window=-1;long lastTicks=long.MinValue,lastSequence=long.MinValue;
 public void Apply(AfterburnerSnapshot snapshot,int milliseconds){Apply(snapshot,milliseconds,Stopwatch.GetTimestamp()*1000.0/Stopwatch.Frequency);}
 public void Apply(AfterburnerSnapshot snapshot,int milliseconds,double now){
  snapshot.BarValues.Clear();
  if(!snapshot.Fresh||milliseconds<=0){Reset();return;}
  if(window!=milliseconds||source!=snapshot.Source){Reset();window=milliseconds;source=snapshot.Source;}
  bool newSample=snapshot.SampleTicks!=lastTicks||snapshot.SampleSequence!=lastSequence;
  var active=new HashSet<string>();
  for(int i=1;i<=PanelRenderer.ThreadCount(snapshot);i++)Add(snapshot,"thread:"+i,PanelRenderer.Thread(snapshot,i),newSample,now,active);
  Add(snapshot,"ram",snapshot.Find(0x91,"RAM usage"),newSample,now,active);
  Add(snapshot,"vram",snapshot.Entries.Find(r=>r.Id==0x31&&r.Gpu==0),newSample,now,active);
  foreach(var key in new List<string>(history.Keys))if(!active.Contains(key))history.Remove(key);
  lastTicks=snapshot.SampleTicks;lastSequence=snapshot.SampleSequence;
 }
 void Add(AfterburnerSnapshot snapshot,string key,Reading reading,bool newSample,double now,HashSet<string> active){
  if(reading==null||!reading.Valid||reading.Value<0)return;active.Add(key);
  Queue<Sample> samples;if(!history.TryGetValue(key,out samples)){samples=new Queue<Sample>();history[key]=samples;}
  if(newSample||samples.Count==0)samples.Enqueue(new Sample{Time=now,Value=reading.Value});
  while(samples.Count>1&&samples.Peek().Time<=now-window)samples.Dequeue();
  double sum=0;foreach(var sample in samples)sum+=sample.Value;snapshot.BarValues[key]=sum/samples.Count;
 }
 void Reset(){history.Clear();source=null;window=-1;lastTicks=long.MinValue;lastSequence=long.MinValue;}
}
