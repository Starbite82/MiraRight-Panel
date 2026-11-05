// SPDX-License-Identifier: GPL-3.0-only
static class PercentageChecks {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){
  var average=new FlexPanel.PercentageAverager();var config=new FlexPanel.PanelConfig();var time=DateTime.UtcNow;
  var first=new FlexPanel.Snapshot{At=time,CpuLoad=0,GpuLoad=20,BarValues=new Dictionary<string,double>{{"cpu",80}},RamUsed=12,CpuTemp=55,Fps=144};
  average.Apply(first,config,1000,0);
  var next=first with{At=time.AddMilliseconds(100),CpuLoad=100,GpuLoad=80};var shown=average.Apply(next,config,1000,100);
  Check(shown.CpuLoad==50&&shown.GpuLoad==50,"CPU/GPU percentages averaged");
  Check(shown.BarValues["cpu"]==80&&shown.RamUsed==12&&shown.CpuTemp==55&&shown.Fps==144&&next.CpuLoad==100,"bars, unrelated readings and original snapshot preserved");
  Check(average.Apply(next,config,1000,200).CpuLoad==50,"duplicate sample not weighted twice");
  var missing=average.Apply(next with{At=time.AddMilliseconds(300),GpuLoad=null},config,1000,300);Check(missing.GpuLoad==null,"missing readings not concealed");
  Check(average.Apply(next with{At=time.AddSeconds(2),CpuLoad=80},config,1000,2000).CpuLoad==80,"expired samples discarded");
  Check(average.Apply(next with{SourceName="Other",CpuLoad=30},config,1000,2100).CpuLoad==30,"source switch resets history");
  Check(average.Apply(next,config,0,2200).CpuLoad==100,"off preserves raw percentage");
  var profile=new DisplayProfile{PercentWindowMs=2000};profile.Validate();Check(profile.Copy().PercentWindowMs==2000,"per-display setting copied");
  Console.WriteLine("PASS: percentage smoothing, unique samples, missing values, expiry, source changes and independent bars");
 }
}
