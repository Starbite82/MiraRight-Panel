// SPDX-License-Identifier: GPL-3.0-only
using System.Drawing;
using System.Text.Json;

static class UnifiedChecks {
 static void Check(bool value,string name){if(!value)throw new Exception("Unified: "+name);}
 sealed class Output : IDisplayOutput {
  public int Count;public bool Fail;public ManualResetEventSlim Gate;
  public void Send(Bitmap image,DisplayProfile p){Gate?.Wait();if(Fail)throw new IOException("Simuliert");Interlocked.Increment(ref Count);}
  public void Dispose(){}
 }
 public static void Run(){
  string oldRoot=Log.Root;string root=Path.Combine(oldRoot,"dual-settings");Directory.CreateDirectory(root);
  try{
   Log.Root=root;var settings=new DisplaySettings{Thermalright=new(){Fps=2,Rotation=90,Source="MSIAfterburner",Gpu="a"},MiraBox=new(){Kind="MiraBox",Fps=30,Rotation=180,Gpu="b"}};
   settings.MiraBox.MiraBox.Design="Thermalright";settings.Save();var restored=DisplaySettings.Load();
   Check(restored.Thermalright.Fps==2&&restored.Thermalright.Rotation==90&&restored.Thermalright.Gpu=="a"&&restored.MiraBox.Fps==30&&restored.MiraBox.Rotation==180&&restored.MiraBox.MiraBox.Design=="Thermalright","independent settings roundtrip");
   var copy=restored.MiraBox.Copy();copy.MiraBox.SensorSelections["test"]="sensor";Check(restored.MiraBox.MiraBox.SensorSelections.Count==0,"settings copies isolated");
   var bundle=new FlexPanel.HardwareBundle{Libre=new(){{"a",new(){GpuLoad=10,VramTotal=8}},{"b",new(){GpuLoad=20,VramTotal=16}}},Afterburner=new(){{"a",new(){GpuLoad=30,VramTotal=8}}},Fps=75};
   var parsed=FlexPanel.HardwareBundle.Parse(JsonSerializer.Serialize(bundle));Check(parsed.Select("LibreHardwareMonitor","b").GpuLoad==20&&parsed.Select("MSIAfterburner","a").GpuLoad==30&&parsed.Select("LibreHardwareMonitor","").GpuLoad==20,"source and GPU selection");
   Check(parsed.Select("MSIAfterburner","a").Fps==75,"shared FPS");
   var sample=new FlexPanel.Snapshot{CpuThreads=[10,20],RamUsed=8,RamTotal=32,VramTotal=16,Sensors=[new("cpu","CPU","Cpu","t","Temp","Temperature",60)]};
   var aio=UnifiedSensorHub.ToAio(sample);Check(aio.Find(145,"RAM usage").Value==8192&&PanelRenderer.Thread(aio,2).Value==20&&aio.VramTotalMb==16384,"units and threads preserved");
   using var image=UnifiedEngine.RenderThermalright(sample,new());Check(image.Size==new Size(320,320),"native Thermalright size");
  }finally{Log.Root=oldRoot;}
  var latest=new FlexPanel.HardwareBundle{Libre=new(){{"",new(){At=DateTime.UtcNow,CpuLoad=20}}}};
  var slow=new Output();var fast=new Output();var slowSettings=new DisplayProfile{Fps=2,SensorMs=100};
  using(var first=new DisplaySession(()=>slowSettings.Copy(),()=>latest,()=>slow,(s,p)=>new Bitmap(8,8)))
  using(var second=new DisplaySession(()=>new(){Fps=30},()=>latest,()=>fast,(s,p)=>new Bitmap(8,8))){
   first.Start();second.Start();Thread.Sleep(800);Check(slow.Count>=1&&slow.Count<=3&&fast.Count>=10,"independent simultaneous output rates");
   slowSettings.Enabled=false;int before=slow.Count,other=fast.Count;Thread.Sleep(600);Check(slow.Count<=before+1&&fast.Count>other,"disable one display independently");
  }
  var broken=new Output{Fail=true};var healthy=new Output();
  using(var first=new DisplaySession(()=>new(){Fps=10},()=>latest,()=>broken,(s,p)=>new Bitmap(8,8)))
  using(var second=new DisplaySession(()=>new(){Fps=30},()=>latest,()=>healthy,(s,p)=>new Bitmap(8,8))){first.Start();second.Start();Thread.Sleep(500);Check(healthy.Count>=5&&first.Status.Contains("Simuliert"),"failed device isolated");}
  using var gate=new ManualResetEventSlim(false);var blocked=new Output{Gate=gate};var independent=new Output();
  using(var first=new DisplaySession(()=>new(){Fps=10,SensorMs=100},()=>latest,()=>blocked,(s,p)=>new Bitmap(8,8)))
  using(var second=new DisplaySession(()=>new(){Fps=30},()=>latest,()=>independent,(s,p)=>new Bitmap(8,8))){
   try{first.Start();second.Start();Thread.Sleep(500);Check(independent.Count>=5,"blocked writer isolated");var updated=DateTime.UtcNow;latest=new(){Libre=new(){{"",new(){At=updated,CpuLoad=75}}}};Thread.Sleep(200);Check(first.LastSampleAt==updated,"sensor sampling independent of blocked USB");}finally{gate.Set();}
  }
  var output=new Output();using(var session=new DisplaySession(()=>new(){Fps=30},()=>latest,()=>output,(s,p)=>new Bitmap(8,8),true)){session.Start();Thread.Sleep(150);Check(output.Count==0&&session.Frames==0,"preview has no USB access");}
  Console.WriteLine("PASS: dual profiles, shared FPS, units, independent rates, blocked USB, failure isolation and preview");
 }
}
