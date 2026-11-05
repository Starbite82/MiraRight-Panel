// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Text.Json;

public sealed class UnifiedEngine : IDisposable {
 readonly UnifiedSensorHub hub;readonly CancellationTokenSource stop=new();Task sensorTask;volatile FlexPanel.HardwareBundle latest=new();
 public DisplaySession Thermalright {get;}public DisplaySession MiraBox {get;}
 public FlexPanel.HardwareBundle Latest=>latest;public int? SensorProcessId=>hub.ProcessId;
 public UnifiedEngine(Func<DisplaySettings> settings,bool previewOnly){
  hub=new(settings(),!previewOnly);
  Log.Debug("panel.start",new { Version=System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute),false).Cast<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion, Preview=previewOnly, settings().AdminSensors });
  Thermalright=new(()=>settings().Thermalright.Copy(),()=>latest,()=>new ThermalrightOutput(),RenderThermalright,previewOnly);
  MiraBox=new(()=>settings().MiraBox.Copy(),()=>latest,()=>new MiraBoxOutput(),(s,p)=>FlexPanel.Renderer.Draw(s,p.MiraBox),previewOnly);
 }
 public static Bitmap RenderThermalright(FlexPanel.Snapshot sample,DisplayProfile profile){
  var adapted=UnifiedSensorHub.ToAio(sample);
  foreach(var bar in sample.BarValues)adapted.BarValues[bar.Key.StartsWith("thread:")?"thread:"+(int.Parse(bar.Key[7..])+1):bar.Key]=bar.Key is "ram" or "vram"?bar.Value*1024:bar.Value;
  return PanelRenderer.RenderView(adapted,sample.RamTotal??double.NaN,profile.AioView,profile.ThreadNumbers,profile.HideStoppedFans,new PanelRenderer.DashboardMetrics(sample.CpuWatts,sample.GpuWatts,sample.Fps));
 }
 public void Start(){
  sensorTask=Task.Run(async()=>{try{while(!stop.IsCancellationRequested){try{latest=hub.Read();}catch(Exception e){latest=new();Log.Write("Shared sensors: "+e.Message);}await Task.Delay(50,stop.Token);}}catch(OperationCanceledException)when(stop.IsCancellationRequested){}});
  Thermalright.Start();MiraBox.Start();
 }
 public void SaveState(){
  var state=new{At=DateTime.UtcNow,ProcessId=Environment.ProcessId,SensorProcessId,SharedSensorProcesses=SensorProcessId.HasValue?1:0,Thermalright=new{Thermalright.Status,Thermalright.Frames,Thermalright.ActualFps,Thermalright.FansWithRpm},MiraBox=new{MiraBox.Status,MiraBox.Frames,MiraBox.ActualFps,MiraBox.FansWithRpm}};
  string file=Path.Combine(Log.Root,"dual-state.json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true}));File.Move(file+".tmp",file,true);
 }
 public void Dispose(){stop.Cancel();Thermalright.Cancel();MiraBox.Cancel();if(sensorTask!=null&&!sensorTask.Wait(10000))throw new TimeoutException("Sensorzugriff wird noch beendet");Thermalright.Dispose();MiraBox.Dispose();hub.Dispose();stop.Dispose();}
}
