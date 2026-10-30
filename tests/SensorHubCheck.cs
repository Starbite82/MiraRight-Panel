using System;using System.IO;
static class SensorHubCheck {
 static void Assert(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Put(string path,string header,string[] rows){File.WriteAllLines(path,new[]{header}.ConcatRows(rows));}
 static string[] ConcatRows(this string[] first,string[] rows){var all=new string[first.Length+rows.Length];Array.Copy(first,all,first.Length);Array.Copy(rows,0,all,first.Length,rows.Length);return all;}
 static void Main(){
  string data=Path.Combine(Log.Root,"lhm-sensors.tsv"),cpu=Path.Combine(Log.Root,"fan-cpu.tsv");
  string old=File.Exists(data)?File.ReadAllText(data):null,oldCpu=File.Exists(cpu)?File.ReadAllText(cpu):null;
  try{
   var now=DateTime.UtcNow.Ticks.ToString();File.WriteAllText(cpu,now+"\t61.5");
   Put(data,now+"\t7\tAMD Radeon RX 9070 XT\t16384",new[]{"144\t4294967295\tCPU usage\t%\t25","144\t0\tCPU1 usage\t%\t50","145\t0\tRAM usage\tMB\t8192","0\t0\tGPU temperature\tC\t35","49\t0\tGPU1 memory usage\tMB\t4096","128\t4294967295\tCPU temperature\tC\t0"});
   var s=SensorHub.ReadLibre();Assert(s.Fresh&&s.SampleSequence==7,"Fresh direct sample");Assert(s.VramTotalMb==16384,"VRAM capacity");Assert(s.Find(0x91,"RAM usage").Value==8192,"RAM MB unit");Assert(s.Find(0x80,"CPU temperature").Value==61.5f&&s.CpuTemperatureSource=="Fan Control","Valid CPU fallback / reject zero");Assert(s.Source=="LibreHardwareMonitor"&&s.GpuName=="AMD Radeon RX 9070 XT","Source identity");
   File.WriteAllText(cpu,DateTime.UtcNow.AddSeconds(-10).Ticks+"\t61.5");s=SensorHub.ReadLibre();Assert(s.Find(0x80,"CPU temperature")==null,"Stale CPU temperature hidden");
   Put(data,now+"\t8\tGPU\t16384",new[]{"128\t4294967295\tCPU temperature\tC\t62"});s=SensorHub.ReadLibre();Assert(s.CpuTemperatureSource=="LibreHardwareMonitor"&&s.Find(128,"CPU temperature").Value==62,"Prefer direct valid temperature");
   Put(data,DateTime.UtcNow.AddSeconds(-4).Ticks+"\t9\tGPU\t16384",new[]{"144\t0\tCPU1 usage\t%\t99"});s=SensorHub.ReadLibre();Assert(!s.Fresh&&s.Entries.Count==0,"Reject stale direct file");
   Put(data,now+"\t10\tGPU\tNaN",new[]{"144\t0\tCPU1 usage\t%\tNaN"});s=SensorHub.ReadLibre();Assert(!s.Fresh&&!s.VramTotalMb.HasValue,"Reject invalid sensor numbers");
   Console.WriteLine("PASS: direct LHM units, source identity, CPU temperature fallback, freshness and invalid readings");
  }finally{if(old==null)File.Delete(data);else File.WriteAllText(data,old);if(oldCpu==null)File.Delete(cpu);else File.WriteAllText(cpu,oldCpu);}
 }
}
