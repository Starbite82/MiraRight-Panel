namespace FlexPanel;

public static partial class Renderer {
 public static double? BarValue(Snapshot snapshot,string key,double? raw)=>raw is double value&&double.IsFinite(value)?snapshot.BarValues.TryGetValue(key,out double mean)?mean:raw:null;
 public static Color MemoryColor(double percent)=>percent>=90?Color.FromArgb(255,58,63):percent>=85?Color.FromArgb(255,153,51):percent>=80?Color.FromArgb(255,205,65):Color.FromArgb(61,216,116);
 public static Color LoadColor(double percent){
  double fraction=Math.Clamp(percent,0,100)/100d;var low=Color.FromArgb(60,204,235);var high=Color.FromArgb(255,58,63);
  return Color.FromArgb((int)Math.Round(low.R+(high.R-low.R)*fraction),(int)Math.Round(low.G+(high.G-low.G)*fraction),(int)Math.Round(low.B+(high.B-low.B)*fraction));
 }
 static double?[] ThreadBars(Snapshot s)=>s.CpuThreads.Select((value,index)=>BarValue(s,"thread:"+index,value)).ToArray();
}
