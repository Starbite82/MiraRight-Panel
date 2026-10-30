using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
public static class SensorHub {
 static readonly object sync=new object();static Process bridge;static int interval;static long lastHealthyTicks;static DateTime lastAttempt=DateTime.MinValue;
 public static void EnsureRunning(){lock(sync){
  if(AppSettings.Current.SensorProvider==1){Stop();return;}
  if(bridge!=null&&!bridge.HasExited){
   bool sameInterval=interval==AppSettings.Current.SensorMs;
   double age=(DateTime.UtcNow.Ticks-Interlocked.Read(ref lastHealthyTicks))/10000000.0;
   if(sameInterval&&((DateTime.UtcNow-lastAttempt).TotalSeconds<8||age<5))return;
   Log.Write(sameInterval?"LHM WATCHDOG: no fresh snapshot for "+age.ToString("0.0",CultureInfo.InvariantCulture)+" s":"LHM interval changed");Stop();lastAttempt=DateTime.MinValue;
  }
  if((DateTime.UtcNow-lastAttempt).TotalSeconds<3)return;lastAttempt=DateTime.UtcNow;
  if(bridge!=null){try{Log.Write("LHM helper exited: "+bridge.ExitCode);}catch{}bridge.Dispose();bridge=null;}
  string runtime=RuntimeLocator.PowerShell();
  interval=AppSettings.Current.SensorMs;
  try{
   bridge=new Process{StartInfo=new ProcessStartInfo{FileName=runtime,Arguments="-NoLogo -NoProfile -File \""+Path.Combine(Log.Root,"LibreHardwareBridge.ps1")+"\" -ParentId "+Process.GetCurrentProcess().Id+" -IntervalMs "+interval,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true}};
   bridge.ErrorDataReceived+=(sender,e)=>{if(!string.IsNullOrWhiteSpace(e.Data))Log.Write("LHM helper error: "+e.Data);};
   bridge.Start();bridge.BeginErrorReadLine();Log.Write("LHM helper started: pid="+bridge.Id+" interval="+interval+"ms");
  }catch(Exception e){Log.Write("LHM bridge: "+e.Message);if(bridge!=null){bridge.Dispose();bridge=null;}}
 }}
 public static void Stop(){lock(sync){try{if(bridge!=null&&!bridge.HasExited)bridge.Kill();}catch{}if(bridge!=null){bridge.Dispose();bridge=null;}}}
 public static AfterburnerSnapshot Read(){return AppSettings.Current.SensorProvider==1?AfterburnerSnapshot.Read():ReadLibre();}
 public static AfterburnerSnapshot ReadLibre(){
  var s=new AfterburnerSnapshot{Source="LibreHardwareMonitor",CpuTemperatureSource="--",Status="LHM NICHT BEREIT"};
  try{
   var lines=SnapshotFiles.ReadLines(Path.Combine(Log.Root,"lhm-sensors.tsv"));if(lines.Length<2)return s;
   var header=lines[0].Split('\t');long ticks,sequence;double total;
   if(header.Length<4||!long.TryParse(header[0],out ticks)||!long.TryParse(header[1],out sequence)||Math.Abs((DateTime.UtcNow-new DateTime(ticks,DateTimeKind.Utc)).TotalSeconds)>2)return s;
   if(header.Length>4){int n;if(int.TryParse(header[4],out n)&&n>0&&n<=4096)s.ReportedThreadCount=n;}
   s.SampleTicks=ticks;s.SampleSequence=sequence;s.GpuName=header[2];if(double.TryParse(header[3],NumberStyles.Float,CultureInfo.InvariantCulture,out total)&&total>0&&!double.IsNaN(total)&&!double.IsInfinity(total))s.VramTotalMb=total;
   for(int i=1;i<lines.Length;i++){
    var p=lines[i].Split('\t');uint id,gpu;float value;if(p.Length!=5||!uint.TryParse(p[0],out id)||!uint.TryParse(p[1],out gpu)||!float.TryParse(p[4],NumberStyles.Float,CultureInfo.InvariantCulture,out value))continue;
    var r=new Reading{Id=id,Gpu=gpu,Name=p[2],Unit=p[3],Value=value};if(r.Valid&&(!(id==0||id==0x80)||value>0))s.Entries.Add(r);
   }
   s.Fresh=s.Entries.Count>0;if(s.Fresh)Interlocked.Exchange(ref lastHealthyTicks,DateTime.UtcNow.Ticks);s.Status=s.Fresh?"LHM DIREKT "+AppSettings.Current.SensorMs+" ms":"LHM NICHT BEREIT";
   var temperature=s.Entries.Find(r=>r.Id==0x80);
   if(temperature!=null)s.CpuTemperatureSource="LibreHardwareMonitor";
   else if(s.Fresh){var cpu=ReadFanCpu();if(cpu.HasValue){s.Entries.Add(new Reading{Id=0x80,Gpu=uint.MaxValue,Name="CPU temperature",Unit="C",Value=cpu.Value});s.CpuTemperatureSource="Fan Control";}}
  }catch(IOException){}catch(ArgumentOutOfRangeException){}
  return s;
 }
 static float? ReadFanCpu(){try{var p=SnapshotFiles.ReadText(Path.Combine(Log.Root,"fan-cpu.tsv")).Split('\t');long ticks;float value;if(p.Length==2&&long.TryParse(p[0],out ticks)&&Math.Abs((DateTime.UtcNow-new DateTime(ticks,DateTimeKind.Utc)).TotalSeconds)<3&&float.TryParse(p[1],NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&value>0&&value<150)return value;}catch(IOException){}catch(ArgumentOutOfRangeException){}return null;}
}
