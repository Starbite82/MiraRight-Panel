using System;
static class BarAveragerCheck {
 static AfterburnerSnapshot Sample(long sequence,float value){var s=new AfterburnerSnapshot{Fresh=true,Source="LibreHardwareMonitor",SampleTicks=sequence,SampleSequence=sequence};s.Entries.Add(new Reading{Id=0x90,Gpu=0,Name="CPU1 usage",Value=value});s.Entries.Add(new Reading{Id=0x91,Name="RAM usage",Value=value*100});return s;}
 static void Equal(double actual,double expected,string what){if(Math.Abs(actual-expected)>0.001)throw new Exception(what+": "+actual);}
 static void Main(){
  var average=new BarAverager();var first=Sample(1,10);average.Apply(first,500,0);Equal(first.BarValues["thread:1"],10,"Initial value");
  var second=Sample(2,90);average.Apply(second,500,100);Equal(second.BarValues["thread:1"],50,"Mean");Equal(second.BarValues["ram"],5000,"RAM mean");Equal(second.Entries[0].Value,90,"Raw measurement preserved");
  var duplicate=Sample(2,90);average.Apply(duplicate,500,150);Equal(duplicate.BarValues["thread:1"],50,"Repeated rendered frame must not overweight sample");
  var later=Sample(3,90);average.Apply(later,500,600);Equal(later.BarValues["thread:1"],90,"Expired old sample");
  var changed=Sample(4,5);changed.Source="MSI Afterburner";average.Apply(changed,500,610);Equal(changed.BarValues["thread:1"],5,"Source reset");
  changed.Fresh=false;average.Apply(changed,500,620);if(changed.BarValues.Count!=0)throw new Exception("Stale data retained");
  var recovered=Sample(5,42);average.Apply(recovered,500,630);Equal(recovered.BarValues["thread:1"],42,"Recovery without stale history");
  average.Apply(recovered,0,640);if(recovered.BarValues.Count!=0)throw new Exception("Off mode not raw");
  var invalid=Sample(6,float.MaxValue);average.Apply(invalid,500,650);if(invalid.BarValues.Count!=0)throw new Exception("Invalid readings averaged");
  Console.WriteLine("PASS: bar means, unique samples, raw numbers, window expiry, source/stale reset and off mode");
 }
}
