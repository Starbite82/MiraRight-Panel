// SPDX-License-Identifier: GPL-3.0-only
static class GpuDetailsChecks {
 static void Check(bool value,string what){if(!value)throw new Exception(what);}
 public static void Run(){
  FlexPanel.Reading R(string gpu,string name,string unit,double? value)=>new(gpu,gpu,"GpuNvidia",gpu+"/"+name,name,unit,value);
  var sample=new FlexPanel.Snapshot{GpuName="gpu-a",Sensors=[R("gpu-a","GPU Core","Clock",2600),R("gpu-a","GPU Memory Junction","Temperature",62),R("gpu-a","D3D 3D","Load",85),R("gpu-a","D3D Video Decode","Load",0),R("gpu-a","D3D Video Encode","Load",20),R("gpu-a","D3D Video Encode","Load",40),R("gpu-b","GPU Core","Clock",9999)]};
  var details=FlexPanel.GpuDetails.Apply(sample,"gpu-a");
  Check(details.GpuClockMhz==2600&&details.GpuMemoryTemp==62&&details.Gpu3dLoad==85&&details.GpuDecodeLoad==0&&details.GpuEncodeLoad==40,"selected GPU details and multiple engines use maximum");
  var onlyAggregate=FlexPanel.GpuDetails.Apply(new(){GpuName="gpu-a",Sensors=[R("gpu-a","GPU Video Engine","Load",77)]},"gpu-a");
  Check(onlyAggregate.GpuDecodeLoad==null&&onlyAggregate.GpuEncodeLoad==null&&onlyAggregate.GpuMemoryTemp==null,"aggregate video engine not invented as decode/encode");
  var other=FlexPanel.GpuDetails.Apply(sample,"gpu-b");Check(other.GpuClockMhz==9999&&other.GpuMemoryTemp==null,"GPU switching does not borrow other device readings");
  var msi=new FlexPanel.Afterburner.Data(DateTime.UtcNow,[new("GPU clock","MHz",1100,0,0x20,0),new("GPU clock","MHz",2400,1,0x20,1),new("GPU memory temperature","°C",62,1,0,2)],[new(0,"GPU A",8388608),new(1,"GPU B",16777216)]);
  var mapped=FlexPanel.Afterburner.Build(msi,new(){GpuName="GPU B",VramTotal=16});
  Check(mapped.GpuClockMhz==2400&&mapped.GpuMemoryTemp==62&&mapped.GpuDecodeLoad==null,"Afterburner details follow the selected GPU index without inventing missing engines");
  var time=DateTime.UtcNow;var config=new FlexPanel.PanelConfig{BarWindowMs=1000};var bars=new FlexPanel.BarAverager();var numbers=new FlexPanel.ReadoutAverager();
  var low=details with{At=time,GpuClockMhz=1000,GpuMemoryTemp=40,Gpu3dLoad=0,GpuDecodeLoad=0,GpuEncodeLoad=0};
  numbers.Apply(bars.Apply(low,config,0),config,1000,0);
  var high=details with{At=time.AddMilliseconds(100),GpuClockMhz=3000,GpuMemoryTemp=60,Gpu3dLoad=100,GpuDecodeLoad=100,GpuEncodeLoad=100};
  var smooth=numbers.Apply(bars.Apply(high,config,100),config,1000,100);
  Check(smooth.GpuClockMhz==2000&&smooth.GpuMemoryTemp==50&&smooth.Gpu3dLoad==50&&smooth.GpuDecodeLoad==50&&smooth.GpuEncodeLoad==50&&smooth.BarValues["gpu.encode"]==50,"new numbers and bars participate in smoothing");
  var missing=numbers.Apply(high with{At=time.AddMilliseconds(200),GpuClockMhz=null,GpuMemoryTemp=null,GpuDecodeLoad=null},config,1000,200);
  Check(missing.GpuClockMhz==null&&missing.GpuMemoryTemp==null&&missing.GpuDecodeLoad==null,"missing GPU readings clear immediately");
  var fixture=details with{At=time,CpuLoad=48,CpuTemp=64,CpuWatts=88.4,CpuThreads=Enumerable.Range(0,32).Select(i=>(double?)(8+i*2)).ToArray(),GpuLoad=87,GpuTemp=72,GpuWatts=225.7,RamUsed=24,RamTotal=64,RamFree=40,RamTemp=46,RamWatts=null,VramUsed=13,VramTotal=16,VramFree=3,Fps=132,FrameMs=7.58,FrameApp="Beispiel-Spiel.exe",Fans=[new("case","Gehäuselüfter",900,"LHM",null,"","","",""),new("aio","AIO-Lüfter",1100,"LHM",64,"","","",""),new("pump","AIO-Pumpe",2800,"LHM",null,"","","","")]};
  foreach(string design in new[]{"Classic","Thermalright"}){using var image=FlexPanel.Renderer.Draw(fixture,new(){Design=design,ThreadNumbers=true});image.Save("gpu-details-"+design+".png");}
  Console.WriteLine("PASS: GPU isolation, clock/memory temperature, distinct video engines, missing values and smoothed details");
 }
}
