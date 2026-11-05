namespace FlexPanel;

public static class SensorSelection {
 public record Metric(string Key,string Name,string Unit);
 public static readonly Metric[] Metrics=[new("CpuLoad","CPU · Auslastung","Load"),new("CpuTemp","CPU · Temperatur","Temperature"),new("CpuWatts","CPU · Leistung","Power"),new("GpuLoad","GPU · Auslastung","Load"),new("GpuTemp","GPU · Temperatur","Temperature"),new("GpuWatts","GPU · Leistung","Power"),new("RamTemp","RAM · Temperatur","Temperature"),new("RamWatts","RAM · Leistung","Power")];
 public static bool Matches(Reading r,string unit)=>r.Unit==unit||unit=="Load"&&r.Unit=="%"||unit=="Temperature"&&r.Unit is "°C" or "C"||unit=="Power"&&r.Unit=="W";
 public static string Key(Reading r)=>r.Id.StartsWith("afterburner/")?r.Id+"|"+r.Name+"|"+r.Unit:r.Id;
 public static Snapshot Apply(Snapshot s,PanelConfig c){
  double? Get(string key,double? automatic){
   if(!c.SensorSelections.TryGetValue(c.SensorSource+"/"+key,out string? id)||id.Length==0)return automatic;
   string unit=Metrics.First(m=>m.Key==key).Unit;
   var row=s.Sensors.FirstOrDefault(r=>Key(r)==id&&Matches(r,unit));
   return row?.Value is double value&&double.IsFinite(value)?value:null;
  }
  return s with{CpuLoad=Get("CpuLoad",s.CpuLoad),CpuTemp=Get("CpuTemp",s.CpuTemp),CpuWatts=Get("CpuWatts",s.CpuWatts),GpuLoad=Get("GpuLoad",s.GpuLoad),GpuTemp=Get("GpuTemp",s.GpuTemp),GpuWatts=Get("GpuWatts",s.GpuWatts),RamTemp=Get("RamTemp",s.RamTemp),RamWatts=Get("RamWatts",s.RamWatts)};
 }
}
