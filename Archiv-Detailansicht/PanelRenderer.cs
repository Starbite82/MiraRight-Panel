using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

public static class PanelRenderer {
 [StructLayout(LayoutKind.Sequential)] class MemoryStatus {
  public uint Length=(uint)Marshal.SizeOf(typeof(MemoryStatus)),Load;
  public ulong TotalPhysical,AvailablePhysical,TotalPage,AvailablePage,TotalVirtual,AvailableVirtual,Extended;
 }
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx([In,Out] MemoryStatus status);
 static readonly Color White=Color.FromArgb(237,246,255),Cyan=Color.FromArgb(78,216,233),Purple=Color.FromArgb(177,155,255),Muted=Color.FromArgb(133,154,181),Tile=Color.FromArgb(17,29,45);
 public static double TotalMemoryGiB(){var memory=new MemoryStatus();return GlobalMemoryStatusEx(memory)?memory.TotalPhysical/1073741824.0:double.NaN;}
 static void Rect(Graphics g,Color color,int x,int y,int width,int height){using(var b=new SolidBrush(color))g.FillRectangle(b,x,y,width,height);}
 static void Text(Graphics g,string text,float x,float y,float size,Color color,int maxWidth=320){
  for(;;){using(var f=new Font("Segoe UI",size,FontStyle.Bold,GraphicsUnit.Point)){
   if(size>6&&g.MeasureString(text,f).Width>maxWidth){size-=0.5f;continue;}
   using(var brush=new SolidBrush(color))g.DrawString(text,f,brush,x,y);return;
  }}
 }
 static string Value(AfterburnerSnapshot s,uint id,string name,string suffix){var r=s.Find(id,name);return s.Fresh&&r!=null&&r.Valid?r.Value.ToString("0")+suffix:"--";}
 public static Reading Thread(AfterburnerSnapshot s,int index){return s.Entries.Find(r=>r.Id==0x90&&r.Name.Equals("CPU"+index+" usage",StringComparison.OrdinalIgnoreCase));}
 public static string MemoryText(AfterburnerSnapshot s,double total){var used=s.Find(0x91,"RAM usage");return (s.Fresh&&used!=null&&used.Valid?(used.Value/1024.0).ToString("0.0"):"--")+" / "+(!double.IsNaN(total)?total.ToString("0.0"):"--")+" GB";}
 public static Bitmap Render(AfterburnerSnapshot s,double total){
  var bmp=new Bitmap(320,320);using(var g=Graphics.FromImage(bmp)){
   g.Clear(Color.FromArgb(9,15,25));g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   Rect(g,Tile,0,0,158,103);Rect(g,Tile,162,0,158,103);
   Rect(g,Cyan,0,0,158,3);Rect(g,Purple,162,0,158,3);
   Text(g,"CPU",5,6,11,Cyan);Text(g,"GPU",167,6,11,Purple);
   Text(g,Value(s,0x80,"CPU temperature","°C"),3,23,32,White,153);
   Text(g,Value(s,0,"GPU temperature","°C"),165,23,32,White,153);
   Text(g,Value(s,0x90,"CPU usage","%"),6,76,15,Cyan);
   Text(g,Value(s,0x30,"GPU usage","%"),168,76,15,Purple);
   Rect(g,Tile,0,107,320,45);Text(g,"RAM",5,117,12,Muted);
   Text(g,MemoryText(s,total),67,114,20,White,250);
   for(int i=1;i<=32;i++){
    int x=((i-1)%8)*40,y=160+((i-1)/8)*40;
    var r=Thread(s,i);bool valid=s.Fresh&&r!=null&&r.Valid;double load=valid?Math.Max(0,Math.Min(100,r.Value)):0;
    var color=load>=90?Color.FromArgb(255,173,91):Cyan;
    Rect(g,Tile,x,y,38,38);Text(g,i.ToString(),x+2,y+1,7,Muted);
    Text(g,valid?load.ToString("0")+"%":"--",x+2,y+12,9,valid?White:Muted,35);
    Rect(g,Color.FromArgb(39,55,75),x+2,y+30,34,6);
    if(load>0)Rect(g,color,x+2,y+30,Math.Max(1,(int)Math.Round(34*load/100)),6);
   }
   if(!s.Fresh){Rect(g,Color.FromArgb(9,15,25),0,76,320,27);Text(g,"AFTERBURNER: KEINE LIVEWERTE",5,80,9,Color.Orange);}
  }return bmp;
 }
}
