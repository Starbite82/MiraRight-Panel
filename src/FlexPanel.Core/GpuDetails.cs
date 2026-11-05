// SPDX-License-Identifier: MIT
namespace FlexPanel;

public static class GpuDetails {
 public static Snapshot Apply(Snapshot s,string gpuId){
  var rows=s.Sensors.Where(r=>r.HardwareId==gpuId).ToArray();
  if(rows.Length==0)rows=s.Sensors.Where(r=>r.Type.StartsWith("Gpu",StringComparison.OrdinalIgnoreCase)&&r.Hardware==s.GpuName).ToArray();
  double? Read(string[] units,params string[] names){
   foreach(string name in names){var found=rows.Where(r=>units.Contains(r.Unit,StringComparer.OrdinalIgnoreCase)&&r.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&r.Value is double v&&double.IsFinite(v)&&v>=0).Select(r=>r.Value).ToArray();if(found.Length>0)return found.Max();}
   return null;
  }
  double? Load(params string[] names){double? value=Read(["Load","%"],names);return value.HasValue?Math.Clamp(value.Value,0,100):null;}
  return s with{
   GpuClockMhz=Read(["Clock","MHz"],"GPU Core","GPU clock","GPU core clock"),
   GpuMemoryTemp=Read(["Temperature","°C","C"],"GPU Memory Junction","GPU Memory Temperature","GPU memory temperature"),
   Gpu3dLoad=Load("D3D 3D","GPU usage","GPU Core"),
   GpuDecodeLoad=Load("D3D Video Decode","GPU Video Decode","Video decode usage"),
   GpuEncodeLoad=Load("D3D Video Encode","GPU Video Encode","Video encode usage")
  };
 }
}
