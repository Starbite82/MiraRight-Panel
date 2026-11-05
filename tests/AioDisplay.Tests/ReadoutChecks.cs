// SPDX-License-Identifier: GPL-3.0-only
static class ReadoutChecks {
 static void Check(bool valid,string what){if(!valid)throw new Exception(what);}
 public static void Run(){
  var smoother=new FlexPanel.ReadoutAverager();var config=new FlexPanel.PanelConfig();var time=DateTime.UtcNow;
  FlexPanel.FanReading Fan(string id,double? rpm)=>new(id,id,rpm,"LHM",50,"cpu.temp","LHM","","");
  var a=new FlexPanel.Snapshot{At=time,CpuLoad=0,GpuLoad=20,CpuTemp=40,GpuTemp=50,CpuWatts=10,GpuWatts=100,RamUsed=8,RamTotal=32,RamFree=24,VramUsed=4,VramTotal=16,VramFree=12,RamTemp=30,RamWatts=5,CpuThreads=[0,20],Fans=[Fan("a",900),Fan("b",1000)]};
  smoother.Apply(a,config,1000,0);
  var b=a with{At=time.AddMilliseconds(100),CpuLoad=100,GpuLoad=80,CpuTemp=60,GpuTemp=70,CpuWatts=30,GpuWatts=200,RamUsed=16,RamFree=16,VramUsed=8,VramFree=8,RamTemp=50,RamWatts=15,CpuThreads=[100,80],Fans=[Fan("b",3000),Fan("a",1100)]};
  var result=smoother.Apply(b,config,1000,100);
  Check(result.CpuLoad==50&&result.GpuLoad==50&&result.CpuWatts==20&&result.GpuWatts==150&&result.CpuTemp==50&&result.GpuTemp==60&&result.RamTemp==40&&result.RamWatts==10,"temperatures, loads and watts averaged");
  Check(result.RamUsed==12&&result.RamFree==20&&result.VramUsed==6&&result.VramFree==10,"memory means keep used/free/total consistent");
  Check(result.Fans[0].Rpm==2000&&result.Fans[1].Rpm==1000,"fans tracked by ID, not order");
  Check(result.BarValues["cpu"]==100&&result.CpuThreads[0]==50&&b.CpuLoad==100,"raw bar fallback and original snapshot preserved");
  Check(smoother.Apply(b,config,1000,200).GpuWatts==150,"duplicate sensor sample not weighted again");
  var missing=smoother.Apply(b with{At=time.AddMilliseconds(300),CpuTemp=null,Fans=[Fan("a",null)]},config,1000,300);Check(missing.CpuTemp==null&&missing.Fans[0].Rpm==null,"missing values shown immediately");
  Check(smoother.Apply(b with{SourceName="Other"},config,1000,400).GpuWatts==200,"new sensor source resets history");
  Check(smoother.Apply(b,config,0,500).CpuWatts==30,"off restores current values");
  var frames=new FlexPanel.ReadoutAverager();frames.ApplyFrames(new(){FrameApp="A",Fps=100,FrameMs=10},1,1000,0);
  var f=frames.ApplyFrames(new(){FrameApp="A",Fps=50,FrameMs=20},2,1000,100);Check(f.FrameMs==15&&Math.Abs(f.Fps.Value-1000d/15)<0.001,"FPS stays consistent with averaged frame time");
  Check(frames.ApplyFrames(new(){FrameApp="B",Fps=25,FrameMs=40},3,1000,200).Fps==25,"foreground app switch clears old FPS");
  Check(frames.ApplyFrames(new(){FrameApp="B"},4,1000,300).Fps==null,"no ghost frames when application stops rendering");
  var profile=new DisplayProfile{PercentWindowMs=2000};profile.Validate();Check(profile.ValueWindowMs==2000,"old percentage setting migrated");profile.ValueWindowMs=500;profile.Validate();Check(profile.Copy().ValueWindowMs==500,"new setting independent and copied");
  var demo=b with{CpuWatts=125.5,GpuWatts=125.5,CpuThreads=Enumerable.Range(0,32).Select(i=>(double?)(i*3)).ToArray(),GpuName="Beispiel-GPU",Fps=100,FrameMs=10};
  using var classic=FlexPanel.Renderer.Draw(demo,new(){ThreadNumbers=true});classic.Save("readout-classic.png");
  using var panorama=FlexPanel.Renderer.Draw(demo,new(){Design="Thermalright",ThreadNumbers=true});panorama.Save("readout-panorama.png");
  Console.WriteLine("PASS: all readout means, memory consistency, fan identity, raw bars, migration and foreground FPS reset");
 }
}
