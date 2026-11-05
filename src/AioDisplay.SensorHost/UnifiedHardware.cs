// SPDX-License-Identifier: MIT
namespace AioDisplay.SensorHost;

using System.Text.Json;

internal sealed class UnifiedHardware : IDisposable {
 readonly FlexPanel.Sensors sensors=new();readonly FlexPanel.Fans resolver=new();readonly FlexPanel.Frames frames=new();
 public UnifiedHardware(){frames.Start("AUTO");}
 public string Sample(string fanConfig){
  var config=new FlexPanel.PanelConfig().Normalize();if(!string.IsNullOrWhiteSpace(fanConfig))config.FanControlConfigPath=fanConfig;
  var first=sensors.Read(config);var ids=first.Sensors.Where(s=>s.Type.StartsWith("Gpu")).Select(s=>s.HardwareId).Distinct().Take(32).ToArray();if(ids.Length==0)ids=[""];
  var bundle=new FlexPanel.HardwareBundle();FlexPanel.Afterburner.Data data=null;string error="";
  try{data=FlexPanel.Afterburner.ReadMemory();}catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException){error=e.Message;}
  foreach(string id in ids){
   config.GpuId=id;var libre=id==""?first:sensors.Read(config,false);
   var ab=data==null?new FlexPanel.Snapshot{SourceName="MSI Afterburner",SensorStatus=error}:FlexPanel.Afterburner.Build(data,libre);
   config.SensorSource="LibreHardwareMonitor";var lf=resolver.Resolve(config,libre,ab);
   config.SensorSource="MSIAfterburner";var af=resolver.Resolve(config,libre,ab);
   bundle.Libre[id]=libre with{Fans=lf,FanStatus=resolver.Status};bundle.Afterburner[id]=ab with{Fans=af,FanStatus=resolver.Status};
  }
  var f=frames.Read(DateTime.UtcNow);bundle.Fps=f.fps;bundle.FrameMs=f.ms;bundle.FrameApp=f.app;bundle.FrameStatus=f.status;
  return JsonSerializer.Serialize(bundle);
 }
 public void Dispose(){frames.Dispose();sensors.Dispose();}
}
