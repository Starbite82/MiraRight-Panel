using System.Text.Json;
using System.Text.RegularExpressions;
namespace FlexPanel;

public sealed class Fans {
 public record Definition(string Id,string Name,string FanId,string ReferenceId,string ReferenceName,string Curve,double? FixedPercent);
 Definition[] definitions=[];HashSet<string> hidden=[];DateTime nextRead;string loadedPath="",status="";
 public static string NormalizeId(string id)=>Regex.Replace(id,@"^(/lpc/[^/]+)/\d+/(fan|control)/","$1/$2/",RegexOptions.IgnoreCase);
 public static string LinkedId(string id){
  var match=Regex.Match(id,@"^NVApiWrapper/(\d+)-[^/]+/fan/(\d+)$",RegexOptions.IgnoreCase);
  return match.Success?$"/gpu-nvidia/{match.Groups[1].Value}/fan/{int.Parse(match.Groups[2].Value)+1}":NormalizeId(id);
 }
 static string S(JsonElement e,string property)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(property,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString()??"":"";
 static JsonElement P(JsonElement e,string property)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(property,out var p)?p:default;
 static bool Flag(JsonElement e,string property)=>P(e,property).ValueKind==JsonValueKind.True;
 static JsonElement[] Array(JsonElement e)=>e.ValueKind==JsonValueKind.Array?e.EnumerateArray().ToArray():[];
 public static (Definition[] active,string[] hidden) Parse(string json){
  using var doc=JsonDocument.Parse(json);var root=P(doc.RootElement,"FanControl");if(root.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Keine FanControl-Konfiguration.");
  var curves=Array(P(root,"FanCurves"));var custom=Array(P(root,"CustomSensors"));var active=new List<Definition>();var hidden=new List<string>();
  foreach(var control in Array(P(root,"Controls"))){
   string fanId=S(P(control,"PairedFanSensor"),"Identifier"),id=S(control,"Identifier");
   if(fanId.Length==0)fanId=id.Replace("/control/","/fan/");
   if(Flag(control,"IsHidden")||!Flag(control,"Enable")){hidden.Add(LinkedId(fanId));continue;}
   string curveName=S(P(control,"SelectedFanCurve"),"Name");var curve=curves.FirstOrDefault(c=>S(c,"Name")==curveName);
   string referenceId=S(P(curve,"SelectedTempSource"),"Identifier");string referenceName=custom.Where(c=>S(c,"Identifier")==referenceId).Select(c=>S(c,"NickName")).FirstOrDefault()??referenceId;
   double? fixedPercent=P(curve,"Percent").TryGetDoubleSafe();if(Flag(control,"ManualControl"))fixedPercent=P(control,"ManualControlValue").TryGetDoubleSafe();
   active.Add(new(id,S(control,"NickName"),LinkedId(fanId),referenceId,referenceName,curveName,fixedPercent));
  }
  return(active.ToArray(),hidden.ToArray());
 }
 void Refresh(string path){
  if(path==loadedPath&&DateTime.UtcNow<nextRead)return;loadedPath=path;nextRead=DateTime.UtcNow.AddSeconds(5);
  try{var parsed=Parse(File.ReadAllText(path));definitions=parsed.active;hidden=new(parsed.hidden,StringComparer.OrdinalIgnoreCase);status="FanControl-Zuordnung: "+Path.GetFileName(path)+" · nur lesend";}
  catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException){status="FanControl-Zuordnung nicht lesbar: "+e.Message;}
 }
 public FanReading[] Resolve(PanelConfig c,Snapshot libre,Snapshot msi){Refresh(c.FanControlConfigPath);return Build(definitions,hidden,c.SensorSource,libre,msi);}
 public string Status=>status;
 static bool SameHardware(string a,string b)=>string.Concat(a.Where(char.IsLetterOrDigit)).Equals(string.Concat(b.Where(char.IsLetterOrDigit)),StringComparison.OrdinalIgnoreCase);
 public static FanReading[] Build(Definition[] definitions,ISet<string> hidden,string source,Snapshot libre,Snapshot msi){
  var result=new List<FanReading>();var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var d in definitions){var lhm=libre.FanChannels.FirstOrDefault(f=>NormalizeId(f.Id).Equals(d.FanId,StringComparison.OrdinalIgnoreCase));used.Add(d.FanId);
   var ab=lhm==null?null:msi.FanChannels.FirstOrDefault(f=>f.Index==lhm.Index&&f.Hardware.Length>0&&SameHardware(f.Hardware,lhm.Hardware));
   var chosen=source=="MSIAfterburner"?(ab?.Rpm.HasValue==true?ab:lhm):(lhm?.Rpm.HasValue==true?lhm:ab);
   var (temp,sensor,tempSource,reference)=Reference(d,libre,msi,lhm,source);
   result.Add(new(d.Id,d.Name.Length>0?d.Name:lhm?.Name??"Lüfter",chosen?.Rpm,chosen?.Source??"—",temp,sensor,tempSource,reference,d.Curve));
  }
  foreach(var fan in libre.FanChannels){string id=NormalizeId(fan.Id);if(used.Contains(id)||hidden.Contains(id))continue;
   if(definitions.Length>0&&fan.Rpm==0)continue;used.Add(id);
   var ab=msi.FanChannels.FirstOrDefault(f=>f.Hardware.Length>0&&SameHardware(f.Hardware,fan.Hardware)&&f.Index==fan.Index);
   var chosen=source=="MSIAfterburner"&&ab?.Rpm.HasValue==true?ab:fan;
   result.Add(new(id,fan.Name,chosen.Rpm,chosen.Source,null,"nicht zugeordnet","—","keine FanControl-Referenz",""));
  }
  foreach(var fan in msi.FanChannels.Where(f=>f.Rpm>0)){
   if(libre.FanChannels.Any(l=>SameHardware(l.Hardware,fan.Hardware)&&l.Index==fan.Index))continue;
   result.Add(new(fan.Id,fan.Name,fan.Rpm,fan.Source,null,"nicht zugeordnet","—","keine FanControl-Referenz",""));
  }
  return result.ToArray();
 }
 static (double? value,string name,string source,string reference) Reference(Definition d,Snapshot l,Snapshot a,FanChannel? fan,string selected){
  if(d.FixedPercent.HasValue)return(null,"keine Temperaturreferenz","—",$"Fest {d.FixedPercent:0} %");
  if(d.ReferenceId.StartsWith("File/",StringComparison.OrdinalIgnoreCase))return(null,d.ReferenceName,"extern","Externe Referenz: "+d.ReferenceName);
  var exact=l.Sensors.FirstOrDefault(r=>r.Id==d.ReferenceId&&r.Unit=="Temperature");
  if(exact!=null)return(exact.Value>0?exact.Value:null,exact.Name,"LibreHardwareMonitor",d.ReferenceId);
  if(d.ReferenceId.StartsWith("NVApiWrapper/",StringComparison.OrdinalIgnoreCase)&&fan!=null){
   // Only map the known GPU core sensor; do not substitute hotspot/memory sensors.
   if(d.ReferenceId.EndsWith("/sensor/0",StringComparison.OrdinalIgnoreCase)){
    if(selected=="MSIAfterburner"&&SameHardware(fan.Hardware,a.GpuName)&&a.GpuTemp.HasValue)return(a.GpuTemp,"GPU Core","MSI Afterburner",d.ReferenceId);
    var gpu=l.Sensors.FirstOrDefault(r=>r.HardwareId==fan.HardwareId&&r.Unit=="Temperature"&&r.Name=="GPU Core");
    if(gpu!=null)return(gpu.Value,gpu.Name,"LibreHardwareMonitor",d.ReferenceId);
   }
  }
  return(null,d.ReferenceName.Length>0?d.ReferenceName:"nicht zugeordnet","—",d.ReferenceId.Length>0?d.ReferenceId:"keine Temperaturreferenz");
 }
}
static class FanJsonExtensions { public static double? TryGetDoubleSafe(this JsonElement e)=>e.ValueKind==JsonValueKind.Number&&e.TryGetDouble(out var value)?value:null; }
