// SPDX-License-Identifier: MIT
namespace FlexPanel;

// Only the two displayed percentages change; bar means and other readings remain intact.
public sealed class PercentageAverager {
 readonly BarAverager average=new();
 public Snapshot Apply(Snapshot snapshot,PanelConfig config,int windowMs)=>Apply(snapshot,config,windowMs,System.Diagnostics.Stopwatch.GetTimestamp()*1000d/System.Diagnostics.Stopwatch.Frequency);
 public Snapshot Apply(Snapshot snapshot,PanelConfig config,int windowMs,double now){
  var values=average.Apply(snapshot,new PanelConfig{BarWindowMs=windowMs,GpuId=config.GpuId,SensorSelections=config.SensorSelections},now).BarValues;
  return snapshot with{CpuLoad=values.TryGetValue("cpu",out double cpu)?cpu:snapshot.CpuLoad,GpuLoad=values.TryGetValue("gpu",out double gpu)?gpu:snapshot.GpuLoad};
 }
 public void Reset()=>average.Reset();
}
