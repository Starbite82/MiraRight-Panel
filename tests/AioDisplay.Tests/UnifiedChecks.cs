// SPDX-License-Identifier: GPL-3.0-only
using System.Drawing;
using System.Text.Json;

static class UnifiedChecks {
 public static void ScheduledRecovery(string host){
  Check(ScheduledSensorTask.Installed,"task required; test must not request UAC");
  using var parent=System.Diagnostics.Process.GetCurrentProcess();
  var options=new SensorOptions{Unified=true,IntervalMs=100,ParentId=parent.Id,ParentStartTicks=parent.StartTime.ToUniversalTime().Ticks};
  using var client=new SensorProcessClient(host,Console.WriteLine,true);
  void Wait(Func<bool> ready){var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<35){System.Windows.Forms.Application.DoEvents();client.EnsureRunning(options);if(ready())return;Thread.Sleep(50);}throw new Exception("Scheduled recovery timeout");}
  Wait(()=>client.Read().Fresh);int first=client.ProcessId.Value;using var old=System.Diagnostics.Process.GetProcessById(first);
  var field=typeof(SensorProcessClient).GetField("transport",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  ((IDisposable)field.GetValue(client)).Dispose();
  Wait(()=>client.ProcessId is int pid&&pid!=first&&client.Read().Fresh);
  Check(old.WaitForExit(10000),"disconnected worker exited");int second=client.ProcessId.Value;
  client.Stop();using var last=System.Diagnostics.Process.GetProcessById(second);Check(last.WaitForExit(15000),"replacement worker stopped");
  Console.WriteLine("PASS: scheduled sensor recovers from disconnected pipe without UAC and both workers exit");
 }
 public static void Layout(){
  SensorHub.HostPath=Path.Combine(Log.Root,"MissingSensorHost.exe");
  new DisplaySettings().Save();
  using var form=new UnifiedWindow(true,false);form.Show();System.Windows.Forms.Application.DoEvents();
  var tabs=(System.Windows.Forms.TabControl)form.Controls.OfType<System.Windows.Forms.TabControl>().Single();
  foreach(var size in new[]{new Size(1180,800),new Size(1000,700)}){
   form.ClientSize=size;
   for(int index=0;index<2;index++){
    tabs.SelectedIndex=index;System.Windows.Forms.Application.DoEvents();
    var split=tabs.TabPages[index].Controls.OfType<System.Windows.Forms.SplitContainer>().Single();
    var flow=split.Panel2.Controls.OfType<System.Windows.Forms.FlowLayoutPanel>().Single();
    flow.ScrollControlIntoView(flow.Controls[flow.Controls.Count-1]);System.Windows.Forms.Application.DoEvents();
    var last=flow.Controls.OfType<System.Windows.Forms.ComboBox>().Last();
    Check(last.Top>=0&&last.Bottom<=flow.ClientSize.Height-32,"last sensor fully visible with breathing room");
    var numbers=flow.Controls.OfType<System.Windows.Forms.CheckBox>().Single(c=>c.Text=="Threadnummern anzeigen");
    numbers.Checked=!numbers.Checked;
    Check((index==0?DisplaySettings.Load().Thermalright:DisplaySettings.Load().MiraBox).ThreadNumbers==numbers.Checked,"thread checkbox persisted");
    using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(Log.Root,$"layout-{index}-{size.Width}.png"));
   }
  }
  form.PostExit();System.Windows.Forms.Application.DoEvents();Console.WriteLine("PASS: both tabs at normal/minimum size, last sensor fully visible, independent thread options");
 }
 public static void Live(string host,int seconds,bool scheduled=false){
  if(scheduled){Check(ScheduledSensorTask.Installed,"preauthorized sensor task installed");using var identity=System.Security.Principal.WindowsIdentity.GetCurrent();Check(!new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator),"UI stays unelevated");}
  string old=SensorHub.HostPath;SensorHub.HostPath=host;
  try{
   var hub=new UnifiedSensorHub(new DisplaySettings{AdminSensors=scheduled},scheduled);
   try{
    var clock=System.Diagnostics.Stopwatch.StartNew();FlexPanel.HardwareBundle bundle=new();
    while(clock.Elapsed.TotalSeconds<20){System.Windows.Forms.Application.DoEvents();bundle=hub.Read();if(bundle.Libre.Count>0)break;Thread.Sleep(50);}
    Check(bundle.Libre.Count>0&&hub.ProcessId.HasValue,"live shared hardware initialized");int pid=hub.ProcessId.Value;using var child=System.Diagnostics.Process.GetProcessById(pid);
    int observations=0,stale=0;clock.Restart();while(clock.Elapsed.TotalSeconds<seconds){bundle=hub.Read();if(bundle.Libre.Count==0)stale++;Check(hub.ProcessId==pid,"single stable shared process");observations++;Thread.Sleep(80);}
    Check(stale==0,"no stale shared snapshots");hub.Dispose();Check(child.WaitForExit(10000)&&(hub.LastExitCode==0||scheduled&&hub.LastExitCode==null),"sensor and PresentMon shutdown (elevated exit code may be unavailable to the UI)");
    File.WriteAllText("unified-live-result.json",JsonSerializer.Serialize(new{Seconds=clock.Elapsed.TotalSeconds,Observations=observations,Stale=stale,Gpus=bundle.Libre.Count,OneProcess=true,GracefulExitCode=hub.LastExitCode},new JsonSerializerOptions{WriteIndented=true}));
    File.WriteAllText("unified-live-hardware.json",JsonSerializer.Serialize(bundle,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("PASS: live common hardware, one stable process, source/GPU bundle and graceful shutdown");
   }finally{hub.Dispose();}
  }finally{SensorHub.HostPath=old;}
 }
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
   settings.MiraBox.ThreadNumbers=false;settings.MiraBox.MiraBox.Design="Thermalright";settings.Save();var restored=DisplaySettings.Load();
   Check(restored.Thermalright.ThreadNumbers&&!restored.MiraBox.ThreadNumbers&&!restored.MiraBox.MiraBox.ThreadNumbers,"independent thread numbering survives roundtrip");
   restored.MiraBox.ThreadNumbers=true;restored.Save();Check(DisplaySettings.Load().MiraBox.MiraBox.ThreadNumbers,"MiraBox renderer receives numbering option");
   foreach(string design in new[]{"Classic","Thermalright"}){
    var fixture=new FlexPanel.Snapshot{CpuThreads=Enumerable.Range(0,32).Select(i=>(double?)(i*3)).ToArray()};
    using var plain=FlexPanel.Renderer.Draw(fixture,new(){Cards=["CPU"],Design=design});
    using var numbered=FlexPanel.Renderer.Draw(fixture,new(){Cards=["CPU"],Design=design,ThreadNumbers=true});
    int changes=0;for(int y=236;y<376;y++)for(int x=36;x<1884;x++)if(plain.GetPixel(x,y)!=numbered.GetPixel(x,y))changes++;
    Check(changes>100,"visible optional thread numbers in "+design);
   }
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
