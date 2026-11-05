// SPDX-License-Identifier: GPL-3.0-only
using AioDisplay.App;
using System.Drawing;

internal static class ThermalrightMetricsChecks
{
    static bool Same(Bitmap a, Bitmap b, Rectangle region)
    {
        for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++)if(a.GetPixel(x,y)!=b.GetPixel(x,y))return false;
        return true;
    }
    public static void Run()
    {
        foreach(double? value in new double?[]{null,double.NaN,double.PositiveInfinity,-1,double.MaxValue})
            if(PanelRenderer.MetricText(value,"W",true)!="— W")throw new Exception("Invalid metric rendered as valid");
        if(PanelRenderer.MetricText(0,"W",true)!="0 W"||PanelRenderer.MetricText(144,"FPS",true)!="144 FPS"||PanelRenderer.MetricText(144,"FPS",false)!="— FPS")throw new Exception("Zero/FPS/stale metric text");
        var sample=new FlexPanel.Snapshot{CpuTemp=73,GpuTemp=81,CpuLoad=9,GpuLoad=100,CpuWatts=105,GpuWatts=275,Fps=144,RamUsed=24,RamTotal=64,VramUsed=12,VramTotal=16,CpuThreads=Enumerable.Range(0,32).Select(i=>(double?)(i*3)).ToArray(),Sensors=[new("cpu","CPU","Cpu","load","CPU Total","Load",9)]};
        var profile=new DisplayProfile();
        using var image=UnifiedEngine.RenderThermalright(sample,profile);
        using var changed=UnifiedEngine.RenderThermalright(sample with{CpuWatts=250,GpuWatts=499,Fps=999},profile);
        if(Same(image,changed,new Rectangle(96,35,60,22))||Same(image,changed,new Rectangle(258,35,60,22))||Same(image,changed,new Rectangle(232,58,86,23)))throw new Exception("Power/FPS not connected to Thermalright dashboard");
        if(!Same(image,changed,new Rectangle(0,85,320,235))||!Same(image,changed,new Rectangle(163,56,65,25)))throw new Exception("New readouts affected memory/threads/fans or GPU percent slots");
        using var missing=UnifiedEngine.RenderThermalright(sample with{CpuWatts=null,GpuWatts=null,Fps=null},profile);
        using var invalid=UnifiedEngine.RenderThermalright(sample with{CpuWatts=double.NaN,GpuWatts=-1,Fps=double.PositiveInfinity},profile);
        if(!Same(missing,invalid,new Rectangle(0,0,320,320)))throw new Exception("Missing and invalid values must both be unavailable");
        image.Save("work/thermalright-watts-fps.png");missing.Save("work/thermalright-watts-fps-missing.png");
        using var large=UnifiedEngine.RenderThermalright(sample with{CpuTemp=105,GpuTemp=100,CpuWatts=9999,GpuWatts=9999,Fps=9999},profile);large.Save("work/thermalright-watts-fps-large.png");
        Console.WriteLine("PASS: Thermalright watts/FPS, missing/invalid/zero/stale readouts and unchanged lower layout/percent slots");
    }
}
