using System.Text.Json;
namespace FlexPanel;

public sealed class HardwareBundle {
 public int Protocol {get;set;}=1;
 public Dictionary<string,Snapshot> Libre {get;set;}=new();
 public Dictionary<string,Snapshot> Afterburner {get;set;}=new();
 public string FrameApp {get;set;}="";public string FrameStatus {get;set;}="";
 public double? Fps {get;set;}public double? FrameMs {get;set;}
 public Snapshot Select(string source,string gpu){
  var table=source=="MSIAfterburner"?Afterburner:Libre;
  var selected=table.TryGetValue(gpu,out var exact)?exact:table.Values.OrderByDescending(s=>s.VramTotal??0).FirstOrDefault()??new Snapshot{SourceName=source};
  return selected with{Fps=Fps,FrameMs=FrameMs,FrameApp=FrameApp,FrameStatus=FrameStatus};
 }
 public static HardwareBundle Parse(string json){
  var bundle=JsonSerializer.Deserialize<HardwareBundle>(json)??throw new InvalidDataException("Keine gemeinsamen Sensordaten");
  if(bundle.Protocol!=1||bundle.Libre==null||bundle.Afterburner==null||bundle.Libre.Count>32||bundle.Afterburner.Count>32)throw new InvalidDataException("Ungültiges Sensorformat");
  return bundle;
 }
}
