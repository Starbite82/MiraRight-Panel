// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Diagnostics;

public sealed class UnifiedSensorHub : IDisposable {
 readonly SensorProcessClient client;readonly SensorOptions options;readonly EventWaitHandle stopSignal;long parsedSequence=-1;FlexPanel.HardwareBundle bundle=new();bool disposed;
 public UnifiedSensorHub(DisplaySettings settings,bool allowElevation){
  client=new SensorProcessClient(SensorHub.HostPath,Log.Write,settings.AdminSensors&&allowElevation);
  string stopName="Local\\AioUnifiedSensorStop-"+Guid.NewGuid().ToString("N");stopSignal=new EventWaitHandle(false,EventResetMode.ManualReset,stopName);
  using var parent=Process.GetCurrentProcess();options=new(){Unified=true,IntervalMs=100,Provider=0,UseFanControl=false,FanConfigPath=settings.FanConfigPath,ParentId=parent.Id,ParentStartTicks=parent.StartTime.ToUniversalTime().Ticks,StopEvent=stopName};
 }
 public int? ProcessId=>client.ProcessId;
 public int? LastExitCode {get;private set;}
 public FlexPanel.HardwareBundle Read(){
  client.EnsureRunning(options);var snapshot=client.Read();
  if(!snapshot.Fresh||snapshot.UnifiedJson.Length==0)return new();
  if(snapshot.SampleSequence!=parsedSequence){bundle=FlexPanel.HardwareBundle.Parse(snapshot.UnifiedJson);parsedSequence=snapshot.SampleSequence;}
  return bundle;
 }
 public void Dispose(){
  if(disposed)return;disposed=true;
  stopSignal.Set();try{if(client.ProcessId is int id){using var child=Process.GetProcessById(id);child.WaitForExit(7000);}}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}
  LastExitCode=client.ExitCode;client.Dispose();stopSignal.Dispose();
 }
 public static SensorSnapshot ToAio(FlexPanel.Snapshot sample){
  var result=new SensorSnapshot{Fresh=(DateTime.UtcNow-sample.At).TotalSeconds<3&&sample.Sensors.Length>0,SampleTicks=sample.At.Ticks,SampleSequence=sample.At.Ticks,Source=sample.SourceName,GpuName=sample.GpuName,VramTotalMb=sample.VramTotal*1024,ReportedThreadCount=sample.CpuThreads.Length,FanSampleTicks=sample.At.Ticks,FanSource=sample.SourceName,Fans=sample.Fans.Select(f=>new FanReading{Id=f.Id,Name=f.Name,Rpm=f.Rpm}).ToList()};
  void Add(uint id,string name,string unit,double? value,uint gpu=uint.MaxValue){if(value is double number&&double.IsFinite(number))result.Entries.Add(new(){Id=id,Name=name,Unit=unit,Value=(float)number,Gpu=gpu});}
  Add(128,"CPU temperature","C",sample.CpuTemp);Add(144,"CPU usage","%",sample.CpuLoad);Add(0,"GPU temperature","C",sample.GpuTemp,0);Add(48,"GPU usage","%",sample.GpuLoad,0);Add(145,"RAM usage","MB",sample.RamUsed*1024);Add(49,"GPU1 memory usage","MB",sample.VramUsed*1024,0);
  for(int i=0;i<sample.CpuThreads.Length;i++)Add(144,"CPU"+(i+1)+" usage","%",sample.CpuThreads[i],(uint)i);
  return result;
 }
}
