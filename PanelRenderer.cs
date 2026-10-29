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
 public static Color LoadColor(double load,int style){
  load=Math.Max(0,Math.Min(100,load));
  Color low=style==0?Color.FromArgb(60,204,235):Color.FromArgb(61,216,116);
  Color middle=Color.FromArgb(255,205,65),high=Color.FromArgb(255,58,63);
  if(style==0)return Blend(low,high,load/100);
  return load<50?Blend(low,middle,load/50):Blend(middle,high,(load-50)/50);
 }
 static Color Blend(Color a,Color b,double t){return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
 public static Bitmap RenderView(AfterburnerSnapshot s,double total,int view,bool numbers){
  if(view==6)return RenderDashboard(s,total,numbers,FanData.Read(s));
  if(view==1)return Render(s,total);
  if(view<0||view>5)view=0;
  Bitmap bmp=view==0?new Bitmap(320,320):Render(s,total);
  using(var g=Graphics.FromImage(bmp)){
   g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   if(view==0){
    g.Clear(Color.FromArgb(9,15,25));
    for(int style=0;style<4;style++){
     int x=style%2*160,y=style/2*160;
     Rect(g,Tile,x,y,158,158);
     Text(g,((char)('A'+style)).ToString(),x+4,y+2,10,White);
     DrawBars(g,s,new Rectangle(x+4,y+25,150,128),style,numbers);
    }
   }else{

    Rect(g,Color.FromArgb(9,15,25),0,156,320,164);
    DrawBars(g,s,new Rectangle(3,160,314,157),view-2,numbers);
   }
   if(!s.Fresh){Rect(g,Color.FromArgb(9,15,25),0,140,320,25);Text(g,"AFTERBURNER: KEINE LIVEWERTE",5,143,9,Color.Orange);}
  }return bmp;
 }
 static void DrawBars(Graphics g,AfterburnerSnapshot s,Rectangle area,int style,bool numbers){
  int columns=style==1?8:style==3?32:16,rows=32/columns;
  float cellW=area.Width/(float)columns,cellH=area.Height/(float)rows;
  for(int i=0;i<32;i++){
   int x=area.X+(int)Math.Round(i%columns*cellW),top=area.Y+(int)Math.Round(i/columns*cellH);
   int nextX=area.X+(int)Math.Round((i%columns+1)*cellW);
   int width=Math.Max(1,nextX-x-(style==1?3:2));
   int numberSpace=numbers?(cellW>=17?13:9):0;
   int height=Math.Max(1,(int)cellH-5-numberSpace),bottom=top+height;
   var r=Thread(s,i+1);bool valid=s.Fresh&&r!=null&&r.Valid;
   double load=valid?Math.Max(0,Math.Min(100,r.Value)):0;
   if(style!=3)Rect(g,Color.FromArgb(28,43,61),x,top,width,height);
   int filled=load<=0?0:Math.Max(1,(int)Math.Round(height*load/100));
   if(style==2){
    int segment=Math.Max(2,height/12),gap=1;
    for(int offset=0;offset<height;offset+=segment+gap){int h=Math.Min(segment,height-offset);if(offset<filled)Rect(g,LoadColor((offset+h)*100.0/height,style),x,bottom-offset-h,width,h);}
   }else if(filled>0)Rect(g,LoadColor(load,style),x,bottom-filled,width,filled);
   if(!valid)Rect(g,Muted,x,bottom-1,width,1);
   if(numbers){
    float font=cellW>=17?6:cellW>=9?4.5f:3.0f;
    using(var f=new Font("Segoe UI",font,FontStyle.Regular,GraphicsUnit.Point))using(var brush=new SolidBrush(Muted))g.DrawString((i+1).ToString(),f,brush,x-1,bottom+1);
   }
  }
 }

 public static Bitmap RenderDashboard(AfterburnerSnapshot s,double total,bool numbers,System.Collections.Generic.List<FanReading> fans){
  var bmp=Render(s,total);using(var g=Graphics.FromImage(bmp)){
   g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   Rect(g,Color.FromArgb(9,15,25),0,156,320,164);
   Rect(g,Tile,0,160,158,160);Rect(g,Tile,162,160,158,160);
   DrawBars(g,s,new Rectangle(3,163,152,154),1,numbers);
   Text(g,"RPM",166,161,8,Muted);
   if(fans.Count==0)Text(g,"Fan Control: --",166,187,8,Muted,150);
   if(AppSettings.Current.HideStoppedFans)fans=fans.FindAll(fan=>fan.Rpm.HasValue&&fan.Rpm.Value>0);
   int count=Math.Min(fans.Count,11);
   for(int i=0;i<count;i++){
    var fan=fans[i];float rowHeight=140f/Math.Max(1,count);
    float labelSize=Math.Max(7,Math.Min(14,rowHeight*0.43f));
    float valueSize=Math.Max(8,Math.Min(16,rowHeight*0.51f));
    int y=(int)(178+i*rowHeight+(rowHeight-valueSize*1.5f)/2);
    Text(g,FanData.ShortName(fan),166,y,labelSize,Muted,87);
    string value=fan.Rpm.HasValue?fan.Rpm.Value.ToString("0"):"--";
    using(var font=new Font("Segoe UI",valueSize,FontStyle.Bold,GraphicsUnit.Point))using(var brush=new SolidBrush(fan.Rpm.HasValue?White:Muted)){
     float width=g.MeasureString(value,font).Width;g.DrawString(value,font,brush,318-width,y-1);
    }
   }
  }return bmp;
 }

}
