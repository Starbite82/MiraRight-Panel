using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
namespace FlexPanel;

public sealed class Frames : IDisposable {
 readonly object gate=new();Process? process;string target="";string session="";string[]? header;
 readonly Dictionary<string,Queue<(DateTime at,double ms)>> chains=new();readonly Dictionary<string,string> applications=new();readonly List<string> trace=new();string status="FPS-Erfassung noch nicht gestartet";
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint processId);
 public static (string name,uint pid) Foreground(){GetWindowThreadProcessId(GetForegroundWindow(),out uint pid);try{using var p=Process.GetProcessById((int)pid);return(p.ProcessName+".exe",pid);}catch{return("Desktop",pid);}}
 public void Start(string exe){Dispose();target=exe;header=null;lock(gate)chains.Clear();
  string path=Path.Combine(AppContext.BaseDirectory,"tools","PresentMon.exe");
  if(!File.Exists(path)){status="PresentMon.exe fehlt";return;}
  bool automatic=exe=="AUTO";
  if(string.IsNullOrWhiteSpace(exe)||exe.IndexOfAny(['"','\r','\n'])>=0){status="Ungültiger Prozessname";return;}
  try {
   var p=new Process{StartInfo=new(path){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};
   session="FlexPanel-"+Environment.ProcessId+"-"+Guid.NewGuid().ToString("N")[..8];
   if(!automatic){p.StartInfo.ArgumentList.Add("--process_name");p.StartInfo.ArgumentList.Add(exe);}
   foreach(string arg in new[]{"--output_stdout","--v1_metrics","--no_console_stats","--session_name",session})p.StartInfo.ArgumentList.Add(arg);
   p.OutputDataReceived+=(_,e)=>{if(e.Data!=null)Accept(e.Data,DateTime.UtcNow);};
   p.ErrorDataReceived+=(_,e)=>{if(!string.IsNullOrWhiteSpace(e.Data))lock(gate){status=e.Data;if(trace.Count<40)trace.Add("STDERR: "+e.Data);}};
   p.Start();process=p;p.BeginOutputReadLine();p.BeginErrorReadLine();status="Warte auf Frames · "+exe;
  }catch(Exception e){status=e.Message;}
 }
 public static string[] Csv(string line){var list=new List<string>();var s=new StringBuilder();bool quoted=false;for(int i=0;i<line.Length;i++){char c=line[i];if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){s.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){list.Add(s.ToString());s.Clear();}else s.Append(c);}list.Add(s.ToString());return list.ToArray();}
 public void Accept(string line,DateTime now){var fields=Csv(line.TrimStart('\uFEFF'));lock(gate){if(trace.Count<40)trace.Add(line);if(fields.Any(f=>f.Equals("MsBetweenPresents",StringComparison.OrdinalIgnoreCase))&&fields.Contains("Application")){header=fields;return;}if(header==null||fields.Length!=header.Length)return;
   string Get(string key){int i=Array.FindIndex(header,h=>h.Equals(key,StringComparison.OrdinalIgnoreCase));return i>=0?fields[i]:"";}
   if(target!="AUTO"&&!string.IsNullOrEmpty(target)&&!Get("Application").Equals(target,StringComparison.OrdinalIgnoreCase))return;
   if(!double.TryParse(Get("MsBetweenPresents"),NumberStyles.Float,CultureInfo.InvariantCulture,out double ms)||ms<=0||ms>10000)return;
   string key=Get("ProcessID")+":"+Get("SwapChainAddress");applications[key]=Get("Application");if(!chains.TryGetValue(key,out var q))chains[key]=q=new();q.Enqueue((now,ms));while(q.Count>2000||q.Count>0&&(now-q.Peek().at).TotalSeconds>1)q.Dequeue();status="PresentMon · automatische Anwendungserkennung";
  }}
 public (double? fps,double? ms,string status,string app) Read(DateTime now){var fg=Foreground();lock(gate){foreach(var key in chains.Keys.ToArray()){var q=chains[key];while(q.Count>0&&(now-q.Peek().at).TotalSeconds>2)q.Dequeue();if(q.Count==0){chains.Remove(key);applications.Remove(key);}}
   var candidates=chains.Where(kv=>kv.Value.Count>0);if(target=="AUTO"){
    var exact=candidates.Where(kv=>kv.Key.StartsWith(fg.pid+":"));candidates=exact.Any()?exact:candidates.Where(kv=>applications[kv.Key].Equals(fg.name,StringComparison.OrdinalIgnoreCase));
   }
   string app=target=="AUTO"?fg.name:target;var best=candidates.OrderByDescending(kv=>kv.Value.Count).Select(kv=>kv.Value).FirstOrDefault();if(best==null)return(null,null,status+" · keine aktuellen Frames für "+app,app);double mean=best.Average(x=>x.ms);return(1000/mean,mean,"PresentMon · "+app,app);}}
 public void SaveTrace(string path){lock(gate)File.WriteAllLines(path,trace);}
 public void Dispose(){if(process!=null){try{
   if(session.Length>0){var info=new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"tools","PresentMon.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"--session_name",session,"--terminate_existing_session"})info.ArgumentList.Add(arg);using var stopper=Process.Start(info);if(stopper!=null&&!stopper.WaitForExit(2500))stopper.Kill();}
   if(!process.WaitForExit(2000))process.Kill(true);
  }catch{}process.Dispose();process=null;}session="";lock(gate)chains.Clear();}
}
