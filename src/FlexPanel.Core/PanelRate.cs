using System.Diagnostics;
namespace FlexPanel;

// Use elapsed time, including the work itself, rather than adding a full delay after each frame.
public static class PanelRate {
 public static double Remaining(double startedMs,double nowMs,double periodMs)=>Math.Max(0,periodMs-(nowMs-startedMs));
 public static Task Delay(Stopwatch clock,double startedMs,double periodMs,CancellationToken token){
  double remaining=Remaining(startedMs,clock.Elapsed.TotalMilliseconds,periodMs);
  return Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1,remaining)),token);
 }
}
