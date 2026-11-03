// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Diagnostics;

public sealed class DisplaySession : IDisposable {
 readonly Func<DisplayProfile> profile;readonly Func<FlexPanel.HardwareBundle> sensors;readonly Func<IDisplayOutput> factory;
 readonly Func<FlexPanel.Snapshot,DisplayProfile,Bitmap> render;readonly bool previewOnly;readonly CancellationTokenSource stop=new();
 readonly object imageLock=new();Bitmap preview;Task worker,sampler;volatile FlexPanel.Snapshot sample=new();long frames;string status="Startet";double actualFps;
 public string Status=>Volatile.Read(ref status);public long Frames=>Interlocked.Read(ref frames);public double ActualFps=>Volatile.Read(ref actualFps);
 public DisplaySession(Func<DisplayProfile> settings,Func<FlexPanel.HardwareBundle> source,Func<IDisplayOutput> output,Func<FlexPanel.Snapshot,DisplayProfile,Bitmap> renderer,bool previewMode=false){profile=settings;sensors=source;factory=output;render=renderer;previewOnly=previewMode;}
 public void Start(){sampler=Task.Run(SampleLoop);worker=Task.Run(Run);}
 async Task SampleLoop(){
  var average=new FlexPanel.BarAverager();var clock=Stopwatch.StartNew();
  try{while(!stop.IsCancellationRequested){double started=clock.Elapsed.TotalMilliseconds;var p=profile();
   if(p.Enabled){try{var raw=sensors().Select(p.Source,p.Gpu);p.MiraBox.SensorSource=p.Source;p.MiraBox.GpuId=p.Gpu;p.MiraBox.BarWindowMs=p.BarWindowMs;sample=average.Apply(FlexPanel.SensorSelection.Apply(raw,p.MiraBox),p.MiraBox);}catch(Exception e){average.Reset();sample=new(){SensorStatus=e.Message};}}
   await FlexPanel.PanelRate.Delay(clock,started,p.SensorMs,stop.Token);
  }}catch(OperationCanceledException)when(stop.IsCancellationRequested){}
 }
 public DateTime LastSampleAt=>sample.At;
 public Bitmap Preview(){lock(imageLock)return preview==null?null:(Bitmap)preview.Clone();}
 async Task Run(){
  IDisplayOutput output=null;
  var clock=Stopwatch.StartNew();double report=0;int count=0;
  try{while(!stop.IsCancellationRequested){double started=clock.Elapsed.TotalMilliseconds;var p=profile();
   if(!p.Enabled){output?.Dispose();output=null;status="Deaktiviert";Volatile.Write(ref actualFps,0);await Task.Delay(100,stop.Token);continue;}
   try{
    var rawFrame=sensors();var visible=sample with{Fps=rawFrame.Fps,FrameMs=rawFrame.FrameMs,FrameApp=rawFrame.FrameApp,FrameStatus=rawFrame.FrameStatus};
    if((DateTime.UtcNow-visible.At).TotalSeconds>3)visible=new(){SourceName=p.Source,FrameApp=rawFrame.FrameApp,Fps=rawFrame.Fps,FrameMs=rawFrame.FrameMs};
    using var image=render(visible,p);lock(imageLock){preview?.Dispose();preview=(Bitmap)image.Clone();}
    if(!previewOnly){output??=factory();output.Send(image,p);Interlocked.Increment(ref frames);count++;status="Verbunden";}else status="Vorschau · USB aus";
    double now=clock.Elapsed.TotalMilliseconds;if(now-report>=2000){Volatile.Write(ref actualFps,count*1000/(now-report));count=0;report=now;}
    await FlexPanel.PanelRate.Delay(clock,started,1000d/p.Fps,stop.Token);
   }catch(OperationCanceledException)when(stop.IsCancellationRequested){break;}
   catch(Exception e){status="Warte: "+e.Message;output?.Dispose();output=null;Volatile.Write(ref actualFps,0);await Task.Delay(1000,stop.Token);}
  }}catch(OperationCanceledException)when(stop.IsCancellationRequested){}finally{output?.Dispose();}
 }
 public void Cancel()=>stop.Cancel();
 public bool Wait(int timeout)=>worker==null||Task.WhenAll(worker,sampler).Wait(timeout);
 public void Dispose(){stop.Cancel();if(!Wait(10000))throw new TimeoutException("Displayzugriff wird noch beendet");lock(imageLock){preview?.Dispose();preview=null;}stop.Dispose();}
}
