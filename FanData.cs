using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
public sealed class FanReading {public string Id,Name;public double? Rpm;}
public static class FanData {
 static Process bridge;static DateTime lastAttempt=DateTime.MinValue;
 public static void EnsureRunning(){if(bridge!=null&&!bridge.HasExited)return;if((DateTime.UtcNow-lastAttempt).TotalSeconds<10)return;Start();}
 public static void Start(){
  if(bridge!=null&&!bridge.HasExited)return;if(bridge!=null){bridge.Dispose();bridge=null;}lastAttempt=DateTime.UtcNow;
  string runtime=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),@".cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe");
  string system=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"PowerShell\7\pwsh.exe");
  if(File.Exists(system))runtime=system;
  if(!File.Exists(runtime)){Log.Write("Fan Control: PowerShell 7 nicht gefunden.");return;}
  try{bridge=Process.Start(new ProcessStartInfo{FileName=runtime,Arguments="-NoLogo -NoProfile -File \""+Path.Combine(Log.Root,"FanControlBridge.ps1")+"\" -ParentId "+Process.GetCurrentProcess().Id,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});}catch(Exception e){Log.Write("Fan bridge: "+e.Message);}
 }
 public static void Stop(){try{if(bridge!=null&&!bridge.HasExited)bridge.Kill();}catch{}if(bridge!=null)bridge.Dispose();}
 public static List<FanReading> Read(AfterburnerSnapshot s){
  var rows=new List<FanReading>();
  try{var lines=File.ReadAllLines(Path.Combine(Log.Root,"fan-rpm.tsv"));long ticks;bool fresh=lines.Length>0&&long.TryParse(lines[0],out ticks)&&Math.Abs((DateTime.UtcNow-new DateTime(ticks,DateTimeKind.Utc)).TotalSeconds)<5;
   foreach(var line in lines.Skip(1)){var p=line.Split('\t');if(p.Length!=3)continue;double v;double? rpm=fresh&&double.TryParse(p[2],NumberStyles.Float,CultureInfo.InvariantCulture,out v)&&!double.IsNaN(v)&&!double.IsInfinity(v)&&v>=0&&v<100000?(double?)v:null;
    if(p[0].StartsWith("ADLX/")){var gpu=s.Entries.Find(r=>r.Id==0x11&&r.Gpu==0);if(!rpm.HasValue&&s.Fresh&&gpu!=null&&gpu.Valid)rpm=gpu.Value;}
    rows.Add(new FanReading{Id=p[0],Name=p[1],Rpm=rpm});
   }
  }catch(IOException){}catch(ArgumentOutOfRangeException){}
  return rows;
 }
 public static string ShortName(FanReading r){if(r.Id.StartsWith("ADLX/"))return "GPU";return r.Name.Replace("Pump Fan","Pumpe").Replace(" #1","").Replace("CPU Fan","CPU").Replace("Chipset Fan","Chipsatz").Replace("System Fan #","SYS ").Replace("EZ-Connect Fan","EZ-Connect").Replace("Fan unten","Unten");}
}
