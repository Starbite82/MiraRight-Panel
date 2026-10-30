using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class PanelRenderer {
 [StructLayout(LayoutKind.Sequential)] class MemoryStatus {
  public uint Length=(uint)Marshal.SizeOf(typeof(MemoryStatus)),Load;
  public ulong TotalPhysical,AvailablePhysical,TotalPage,AvailablePage,TotalVirtual,AvailableVirtual,Extended;
 }
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx([In,Out] MemoryStatus status);
 static readonly Color White=Color.FromArgb(237,246,255),Cyan=Color.FromArgb(78,216,233),Purple=Color.FromArgb(177,155,255),Muted=Color.FromArgb(133,154,181),Tile=Color.FromArgb(17,29,45),Track=Color.FromArgb(51,71,95);
 public static double TotalMemoryGiB(){var memory=new MemoryStatus();return GlobalMemoryStatusEx(memory)?memory.TotalPhysical/1073741824.0:double.NaN;}
 static void Rect(Graphics g,Color color,int x,int y,int width,int height){using(var b=new SolidBrush(color))g.FillRectangle(b,x,y,width,height);}
 static void Text(Graphics g,string text,float x,float y,float size,Color color,int maxWidth=320){
  for(;;){using(var f=new Font("Segoe UI",size,FontStyle.Bold,GraphicsUnit.Point)){
   if(size>6&&g.MeasureString(text,f).Width>maxWidth){size-=0.5f;continue;}
   using(var brush=new SolidBrush(color))g.DrawString(text,f,brush,x,y);return;
  }}
 }
 static string Value(AfterburnerSnapshot s,uint id,string name,string suffix){var r=s.Find(id,name);return s.Fresh&&r!=null&&r.Valid?r.Value.ToString("0")+suffix:"--";}
 static void LoadText(Graphics g,AfterburnerSnapshot s,uint id,string name,float x,float y,float size,Color color){
  string digits=Value(s,id,name,"").PadLeft(3);if(digits.Length>3)digits=digits.Substring(digits.Length-3);
  using(var font=new Font("Consolas",size,FontStyle.Bold,GraphicsUnit.Point))using(var brush=new SolidBrush(color)){
   float cell=size*0.82f;for(int i=0;i<3;i++){var box=new Rectangle((int)(x+i*cell),(int)y,(int)Math.Ceiling(cell),(int)Math.Ceiling(size*1.8f));TextRenderer.DrawText(g,digits[i].ToString(),font,box,color,TextFormatFlags.NoPadding|TextFormatFlags.HorizontalCenter|TextFormatFlags.Top);}
   g.DrawString("%",font,brush,x+3*cell+1,y);
  }
 }
 static double BarValue(AfterburnerSnapshot s,string key,double raw){double value;return s.BarValues.TryGetValue(key,out value)?value:raw;}
 public static int ThreadCount(AfterburnerSnapshot s){
  int count=s.ReportedThreadCount;
  foreach(var r in s.Entries){int n;if(r.Id==0x90&&r.Name.StartsWith("CPU",StringComparison.OrdinalIgnoreCase)&&r.Name.EndsWith(" usage",StringComparison.OrdinalIgnoreCase)&&int.TryParse(r.Name.Substring(3,r.Name.Length-9),out n))count=Math.Max(count,n);}
  return Math.Max(1,Math.Min(4096,count>0?count:Environment.ProcessorCount));
 }
 public static int PageStart(int count,int size,long ticks){return count<=size?0:(int)((Math.Max(0,ticks)/TimeSpan.TicksPerSecond/5)%((count+size-1)/size))*size;}
 public static Reading Thread(AfterburnerSnapshot s,int index){return s.Entries.Find(r=>r.Id==0x90&&r.Name.Equals("CPU"+index+" usage",StringComparison.OrdinalIgnoreCase));}
 public static string MemoryText(AfterburnerSnapshot s,double total){var used=s.Find(0x91,"RAM usage");return (s.Fresh&&used!=null&&used.Valid?(used.Value/1024.0).ToString("0.0"):"--")+" / "+(!double.IsNaN(total)?total.ToString("0.0"):"--")+" GB";}
 public static string VramText(AfterburnerSnapshot s){var used=s.Entries.Find(r=>r.Id==0x31&&r.Gpu==0);return (s.Fresh&&used!=null&&used.Valid?(used.Value/1024.0).ToString("0.0"):"--")+" / "+(s.VramTotalMb.HasValue?(s.VramTotalMb.Value/1024.0).ToString("0.0"):"--")+" GB";}
 public static Bitmap Render(AfterburnerSnapshot s,double total){
  var bmp=new Bitmap(320,320);using(var g=Graphics.FromImage(bmp)){
   g.Clear(Color.FromArgb(9,15,25));g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   Rect(g,Tile,0,0,158,103);Rect(g,Tile,162,0,158,103);
   Rect(g,Cyan,0,0,158,3);Rect(g,Purple,162,0,158,3);
   Text(g,"CPU",5,6,11,Cyan);Text(g,"GPU",167,6,11,Purple);
   Text(g,Value(s,0x80,"CPU temperature","°C"),3,23,32,White,153);
   Text(g,Value(s,0,"GPU temperature","°C"),165,23,32,White,153);
   LoadText(g,s,0x90,"CPU usage",6,76,15,Cyan);
   LoadText(g,s,0x30,"GPU usage",168,76,15,Purple);
   Rect(g,Tile,0,107,320,45);Text(g,"RAM",5,117,12,Muted);
   Text(g,MemoryText(s,total),67,114,20,White,250);
   int totalThreads=ThreadCount(s),start=PageStart(totalThreads,32,s.SampleTicks),visible=Math.Min(32,totalThreads-start);
   int cols=Math.Min(8,visible),rows=(visible+cols-1)/cols;
   for(int j=0;j<visible;j++){
    int i=start+j+1,x=(j%cols)*(320/cols),y=160+(j/cols)*(160/rows);
    var r=Thread(s,i);bool valid=s.Fresh&&r!=null&&r.Valid;double load=valid?Math.Max(0,Math.Min(100,r.Value)):0;
    double barLoad=valid?BarValue(s,"thread:"+i,load):0;
    var color=barLoad>=90?Color.FromArgb(255,173,91):Cyan;
    int tileW=320/cols-2,tileH=160/rows-2;Rect(g,Tile,x,y,tileW,tileH);Text(g,i.ToString(),x+2,y+1,7,Muted);
    Text(g,valid?load.ToString("0")+"%":"--",x+2,y+12,9,valid?White:Muted,35);
    Rect(g,Track,x+2,y+tileH-8,tileW-4,6);
    if(barLoad>0)Rect(g,color,x+2,y+tileH-8,Math.Max(1,(int)Math.Round((tileW-4)*barLoad/100)),6);
   }
   if(!s.Fresh){Rect(g,Color.FromArgb(9,15,25),0,76,320,27);Text(g,"SENSOREN: KEINE LIVEWERTE",5,80,9,Color.Orange);}
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
   if(!s.Fresh){Rect(g,Color.FromArgb(9,15,25),0,140,320,25);Text(g,"SENSOREN: KEINE LIVEWERTE",5,143,9,Color.Orange);}
  }return bmp;
 }
 static void DrawBars(Graphics g,AfterburnerSnapshot s,Rectangle area,int style,bool numbers){
  int total=ThreadCount(s),start=PageStart(total,32,s.SampleTicks),count=Math.Min(32,total-start);
  if(total>32){Text(g,(start+1)+"–"+(start+count)+" / "+total,area.X,area.Y,7,Muted,area.Width);area.Y+=15;area.Height-=15;}
  int columns=style==1?Math.Min(count,count<=16?4:8):style==3?count:Math.Min(count,16),rows=(count+columns-1)/columns;
  float cellW=area.Width/(float)columns,cellH=area.Height/(float)rows;
  for(int i=0;i<count;i++){
   int x=area.X+(int)Math.Round(i%columns*cellW),top=area.Y+(int)Math.Round(i/columns*cellH);
   int nextX=area.X+(int)Math.Round((i%columns+1)*cellW);
   int width=Math.Max(1,nextX-x-(style==1?3:2));
   int numberSpace=numbers?(cellW>=17?13:9):0;
   int height=Math.Max(1,(int)cellH-5-numberSpace),bottom=top+height;
   var r=Thread(s,start+i+1);bool valid=s.Fresh&&r!=null&&r.Valid;
   double load=valid?Math.Max(0,Math.Min(100,BarValue(s,"thread:"+(start+i+1),r.Value))):0;
   Rect(g,Track,x,top,width,height);
   int filled=load<=0?0:Math.Max(1,(int)Math.Round(height*load/100));
   if(style==2){
    int segment=Math.Max(2,height/12),gap=1;
    for(int offset=0;offset<height;offset+=segment+gap){int h=Math.Min(segment,height-offset);if(offset<filled)Rect(g,LoadColor((offset+h)*100.0/height,style),x,bottom-offset-h,width,h);}
   }else if(filled>0)Rect(g,LoadColor(load,style),x,bottom-filled,width,filled);
   if(!valid)Rect(g,Muted,x,bottom-1,width,1);
   if(numbers){
    float font=cellW>=17?6:cellW>=9?4.5f:3.0f;
    using(var f=new Font("Segoe UI",font,FontStyle.Regular,GraphicsUnit.Point))using(var brush=new SolidBrush(Muted))g.DrawString((start+i+1).ToString(),f,brush,x-1,bottom+1);
   }
  }
 }

 static void DrawMemoryBar(Graphics g,AfterburnerSnapshot s,string key,int x,double? usedMb,double? totalMb){
  const int y=94,height=52,width=18;Rect(g,Track,x,y,width,height);
  if(!usedMb.HasValue||!totalMb.HasValue||totalMb.Value<=0||double.IsNaN(totalMb.Value))return;
  double percent=Math.Max(0,Math.Min(100,100*BarValue(s,key,usedMb.Value)/totalMb.Value));int fill=(int)Math.Round(height*percent/100);
  Color color=percent>=90?Color.FromArgb(255,58,63):percent>=85?Color.FromArgb(255,153,51):percent>=80?Color.FromArgb(255,205,65):Color.FromArgb(61,216,116);
  if(fill>0)Rect(g,color,x,y+height-fill,width,fill);
 }
 public static Bitmap RenderDashboard(AfterburnerSnapshot s,double total,bool numbers,System.Collections.Generic.List<FanReading> fans){
  var bmp=Render(s,total);using(var g=Graphics.FromImage(bmp)){
   g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   Rect(g,Color.FromArgb(9,15,25),0,0,320,320);
   Rect(g,Tile,0,0,158,153);Rect(g,Tile,162,0,158,153);
   Rect(g,Cyan,0,0,158,3);Rect(g,Purple,162,0,158,3);
   Text(g,"CPU",5,5,10,Cyan);Text(g,"GPU",167,5,10,Purple);
   Text(g,Value(s,0x80,"CPU temperature","°C"),3,22,24,White,153);
   Text(g,Value(s,0,"GPU temperature","°C"),165,22,24,White,153);
   LoadText(g,s,0x90,"CPU usage",6,56,13,Cyan);
   LoadText(g,s,0x30,"GPU usage",168,56,13,Purple);
   Rect(g,Track,5,85,148,1);Rect(g,Track,167,85,148,1);
   Text(g,"RAM",5,92,10,Cyan);Text(g,"VRAM",167,92,10,Purple);
   Text(g,MemoryText(s,total),3,121,11,White,125);
   Text(g,VramText(s),165,121,11,White,125);
   var ramUsed=s.Find(0x91,"RAM usage");var vramUsed=s.Entries.Find(reading=>reading.Id==0x31&&reading.Gpu==0);
   DrawMemoryBar(g,s,"ram",134,s.Fresh&&ramUsed!=null&&ramUsed.Valid?(double?)ramUsed.Value:null,total*1024);
   DrawMemoryBar(g,s,"vram",296,s.Fresh&&vramUsed!=null&&vramUsed.Valid?(double?)vramUsed.Value:null,s.VramTotalMb);
   Rect(g,Tile,0,160,158,160);Rect(g,Tile,162,160,158,160);
   DrawBars(g,s,new Rectangle(3,163,152,154),1,numbers);
   Text(g,"RPM",166,161,8,Muted);
   if(AppSettings.Current.HideStoppedFans)fans=fans.FindAll(fan=>fan.Rpm.HasValue&&fan.Rpm.Value>0);
   if(fans.Count==0)Text(g,"Keine RPM",166,187,8,Muted,150);
   int fanStart=PageStart(fans.Count,8,s.SampleTicks),count=Math.Min(fans.Count-fanStart,8);
   if(fans.Count>8)Text(g,(fanStart+1)+"–"+(fanStart+count)+" / "+fans.Count,216,161,7,Muted,100);
   for(int i=0;i<count;i++){
    var fan=fans[fanStart+i];float rowHeight=140f/Math.Max(1,count);
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
