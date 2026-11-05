// SPDX-License-Identifier: MIT
namespace FlexPanel;

public static partial class Renderer {
 static void DrawGpuDetails(Graphics g,Snapshot s,float x,float y,float width,Color accent){
  float column=(width-12)/2;
  Text(g,"GPU-TAKT",x,y,10,Muted,false,column);Text(g,"SPEICHER-TEMP.",x+column+12,y,10,Muted,false,column);
  Text(g,N(s.GpuClockMhz," MHz"),x,y+22,19,White,true,column);Text(g,N(s.GpuMemoryTemp," °C"),x+column+12,y+22,19,White,true,column);
  void Engine(string name,string key,double? raw,int row){
   float top=y+65+row*28;
   Text(g,name,x,top,10,Muted,false,width-62);Text(g,N(raw," %"),x+width-55,top,10,White,true,55);
   using var track=new SolidBrush(Color.FromArgb(51,71,95));g.FillRectangle(track,x,top+19,width,7);
   if(BarValue(s,key,raw) is double value){using var fill=new SolidBrush(LoadColor(value));g.FillRectangle(fill,x,top+19,width*(float)Math.Clamp(value/100,0,1),7);}
  }
  Engine("3D","gpu.3d",s.Gpu3dLoad,0);Engine("VIDEO DECODE","gpu.decode",s.GpuDecodeLoad,1);Engine("VIDEO ENCODE","gpu.encode",s.GpuEncodeLoad,2);
 }
}
