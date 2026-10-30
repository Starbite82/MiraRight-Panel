using System;
using System.Drawing;
public static class Check {
 public static void Main(){
  using(var b=new Bitmap(320,320)){
   b.SetPixel(319,319,Color.Red);b.SetPixel(0,0,Color.Cyan);var p=PanelWindow.Encode565(b);
   if(p.Length!=204800||p[0]!=248||p[1]!=0||p[204798]!=7||p[204799]!=255)throw new Exception("RGB565 / Rotation");
  }
  var s=new AfterburnerSnapshot();s.Entries.Add(new Reading{Id=128,Name="CPU1 temperature",Value=10});
  var total=new Reading{Id=128,Name="CPU temperature",Value=67};s.Entries.Add(total);
  if(s.Find(128,"CPU temperature").Value!=67)throw new Exception("CPU aggregate selection");
  total.Value=float.MaxValue;if(total.Valid)throw new Exception("FLT_MAX sentinel");
  total.Value=float.NaN;if(total.Valid)throw new Exception("NaN");
  s.Fresh=true;
  for(int i=32;i>=1;i--)s.Entries.Add(new Reading{Id=0x90,Name="CPU"+i+" usage",Value=i});
  for(int i=1;i<=32;i++)if(PanelRenderer.Thread(s,i).Value!=i)throw new Exception("Thread mapping "+i);
  s.Entries.Add(new Reading{Id=0x91,Name="RAM usage",Value=12288});
  string expected=(12.0).ToString("0.0")+" / "+(48.0).ToString("0.0")+" GB";
  if(PanelRenderer.MemoryText(s,48)!=expected)throw new Exception("RAM used/total");
  using(var image=PanelRenderer.Render(s,48))if(image.Width!=320||image.Height!=320)throw new Exception("Native resolution");
  s.Fresh=false;if(!PanelRenderer.MemoryText(s,48).StartsWith("-- /"))throw new Exception("Stale memory reading");
  for(int mode=0;mode<=5;mode++)foreach(bool numbers in new[]{false,true})using(var image=PanelRenderer.RenderView(s,48,mode,numbers)){if(image.Width!=320||image.Height!=320)throw new Exception("View geometry "+mode);}
  var cold=PanelRenderer.LoadColor(0,0);var hot=PanelRenderer.LoadColor(100,0);if(hot.R<=cold.R||hot.B>=cold.B)throw new Exception("Load colour direction");
  s.Fresh=true;foreach(var entry in s.Entries)if(entry.Id==0x90)entry.Value=(int.Parse(entry.Name.Substring(3,entry.Name.IndexOf(" usage")-3))-1)*100f/31;
  using(var comparison=PanelRenderer.RenderView(s,48,0,false))comparison.Save("work/comparison-test.png");
  var fanPath=System.IO.Path.Combine(Log.Root,"fan-rpm.tsv");var original=System.IO.File.Exists(fanPath)?System.IO.File.ReadAllText(fanPath):null;
  try{
   s.Entries.Add(new Reading{Id=0x11,Gpu=0,Name="GPU1 fan tachometer",Value=700});
   System.IO.File.WriteAllLines(fanPath,new[]{DateTime.UtcNow.Ticks.ToString(),"/lpc/nct6687dr/fan/0\tCPU Fan\t555","ADLX/gpu/fan\tGPU\t"});
   var fans=FanData.Read(s);if(fans.Count!=2||fans[0].Rpm!=555||fans[1].Rpm.HasValue)throw new Exception("Fan RPM mapping / no cross-GPU fallback");
   using(var dashboard=PanelRenderer.RenderDashboard(s,48,false,fans)){if(dashboard.Width!=320||dashboard.Height!=320)throw new Exception("Dashboard size");}
   System.IO.File.WriteAllLines(fanPath,new[]{DateTime.UtcNow.AddSeconds(-10).Ticks.ToString(),"/lpc/nct6687dr/fan/0\tCPU Fan\t555"});
   if(FanData.Read(s)[0].Rpm.HasValue)throw new Exception("Stale fan RPM");
  }finally{if(original!=null)System.IO.File.WriteAllText(fanPath,original);else System.IO.File.Delete(fanPath);}
  using(var map=System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(null,4096))using(var view=map.CreateViewAccessor()){
   view.Write(8,(uint)32);view.Write(12,(uint)1);view.Write(16,(uint)1324);view.Write(24,(uint)1);view.Write(28,(uint)1304);view.Write(2656,(uint)16777216);
   if(AfterburnerSnapshot.ReadVramTotal(view)!=16384)throw new Exception("GPU descriptor VRAM capacity");
   view.Write(2656,(uint)0);if(AfterburnerSnapshot.ReadVramTotal(view).HasValue)throw new Exception("Unknown VRAM capacity");
  }
  s.VramTotalMb=16384;s.Entries.Add(new Reading{Id=0x31,Gpu=1,Name="GPU2 memory usage",Value=12345});s.Entries.Add(new Reading{Id=0x31,Gpu=0,Name="GPU1 memory usage",Value=8192});
  string expectedVram=(8.0).ToString("0.0")+" / "+(16.0).ToString("0.0")+" GB";if(PanelRenderer.VramText(s)!=expectedVram)throw new Exception("VRAM GPU mapping/units");
  s.Fresh=false;if(!PanelRenderer.VramText(s).StartsWith("-- /"))throw new Exception("Stale VRAM");
  var stable=new AfterburnerSnapshot{Fresh=true,VramTotalMb=16384};var cpuLoad=new Reading{Id=0x90,Gpu=uint.MaxValue,Name="CPU usage",Value=9};var gpuLoad=new Reading{Id=0x30,Gpu=0,Name="GPU usage",Value=9};stable.Entries.Add(cpuLoad);stable.Entries.Add(gpuLoad);
  using(var nine=PanelRenderer.RenderDashboard(stable,48,false,new System.Collections.Generic.List<FanReading>())){
   foreach(int value in new[]{10,100}){cpuLoad.Value=value;gpuLoad.Value=value;using(var changed=PanelRenderer.RenderDashboard(stable,48,false,new System.Collections.Generic.List<FanReading>())){
    foreach(int startX in new[]{40,202})for(int x=startX;x<startX+15;x++)for(int y=56;y<79;y++)if(nine.GetPixel(x,y)!=changed.GetPixel(x,y))throw new Exception("Percentage glyph moved");
   }}
  }
  Console.WriteLine("PASS: RGB565, rotation, CPU aggregate, invalid values, 32 thread mappings, RAM used/total, native resolution, stale values");
 }
}
