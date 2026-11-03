using System.Drawing.Drawing2D;
using System.Globalization;
namespace FlexPanel;

public static partial class Renderer {
 static readonly Color Back=Color.FromArgb(9,14,21),Surface=Color.FromArgb(17,26,37),Muted=Color.FromArgb(143,162,181),White=Color.FromArgb(233,242,248);
 static string N(double? n,string unit="",string format="0")=>n is double d&&double.IsFinite(d)?d.ToString(format,CultureInfo.GetCultureInfo("de-DE"))+unit:"—";
 public static Bitmap Draw(Snapshot s,PanelConfig c) {
  if(c.Design=="Thermalright")return DrawThermalright(s,c);
  var b=new Bitmap(1920,462);using var g=Graphics.FromImage(b);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.Clear(Back);
  Color accent=ColorTranslator.FromHtml(c.Accent);Text(g,"FLEX / PANEL",28,15,21,accent,true);Text(g,s.SourceName.ToUpperInvariant(),246,18,16,Muted);Text(g,DateTime.Now.ToString("HH:mm:ss"),1752,15,21,White);
  int count=c.Cards.Length;float width=(1920-40f-(count-1)*12)/count;
  for(int i=0;i<count;i++) {
   float x=20+i*(width+12);using var brush=new SolidBrush(Surface);g.FillRectangle(brush,x,57,width,379);using var pen=new Pen(accent,3);g.DrawLine(pen,x+18,78,x+54,78);
   string card=c.Cards[i];Text(g,card=="FANS"?"LÜFTER":card,x+18,92,22,White,true);
   float yy=137;void Big(string text){Text(g,text,x+18,yy,52,accent,true,width-32);}
   void Row(string name,string value,int row){Text(g,name,x+18,242+row*51,17,Muted);Text(g,value,x+18,263+row*51,23,White,true,width-32);}
   void Bar(double? used,double? total){float y=211;using var bb=new SolidBrush(Color.FromArgb(40,54,69));g.FillRectangle(bb,x+18,y,width-36,9);if(used.HasValue&&total>0){double percent=Math.Clamp(used.Value/total.Value*100,0,100);using var ab=new SolidBrush(card is "RAM" or "VRAM"?MemoryColor(percent):LoadColor(percent));g.FillRectangle(ab,x+18,y,(width-36)*(float)(percent/100),9);}}
   switch(card){
    case "CPU":
     Percentage(g,s.CpuLoad,x+18,128,50,accent);
     using(var track=new SolidBrush(Color.FromArgb(40,54,69)))g.FillRectangle(track,x+18,197,width-36,6);
     if(BarValue(s,"cpu",s.CpuLoad) is double load){using var fill=new SolidBrush(LoadColor(load));g.FillRectangle(fill,x+18,197,(width-36)*(float)Math.Clamp(load/100,0,1),6);}
     var threads=s.CpuThreads.Length>0?ThreadBars(s):new double?[Environment.ProcessorCount];
     Text(g,$"THREADS 1–{threads.Length}",x+18,212,13,Muted);
     ThreadGrid(g,threads,x+18,236,width-36,106,accent);
     Text(g,"TEMPERATUR",x+18,361,15,Muted);Text(g,N(s.CpuTemp," °C"),x+18,383,23,White,true,width/2-20);
     Text(g,"LEISTUNG",x+width/2+4,361,15,Muted);Text(g,N(s.CpuWatts," W","0.0"),x+width/2+4,383,23,White,true,width/2-20);
     break;
    case "GPU":Percentage(g,s.GpuLoad,x+18,137,52,accent);Bar(BarValue(s,"gpu",s.GpuLoad),100);Row("TEMPERATUR",N(s.GpuTemp," °C"),0);Row("LEISTUNGSAUFNAHME",N(s.GpuWatts," W","0.0"),2);break;
    case "RAM":Big(N(s.RamTotal>0?s.RamUsed/s.RamTotal*100:null," %"));Bar(BarValue(s,"ram",s.RamUsed),s.RamTotal);Row("BENUTZT / GESAMT",N(s.RamUsed,"","0.0")+" / "+N(s.RamTotal," GiB","0.0"),0);Row("TEMPERATUR",N(s.RamTemp," °C"),1);Row("LEISTUNGSAUFNAHME",N(s.RamWatts," W","0.0"),2);break;
    case "VRAM":Big(N(s.VramTotal>0?s.VramUsed/s.VramTotal*100:null," %"));Bar(BarValue(s,"vram",s.VramUsed),s.VramTotal);Row("BENUTZT / GESAMT",N(s.VramUsed,"","0.0")+" / "+N(s.VramTotal," GiB","0.0"),0);Row("FREI",N(s.VramFree," GiB","0.0"),2);break;
    case "FRAMES":Big(N(s.Fps));Text(g,"FPS  /  GERENDERT",x+18,211,16,Muted);Row("FRAME TIME",N(s.FrameMs," ms","0.00"),0);Row("AKTIVE ANWENDUNG",s.FrameApp.Length>0?s.FrameApp:"—",2);break;
    case "FANS":DrawFans(g,s.Fans,x,width,accent);break;
   }
  }
  Text(g,"Balken = belegt / ausgelastet     ·     — = kein aktueller Messwert",24,438,13,Muted);
  return b;
 }
 static string ShortFanName(string name)=>name.Contains("NVIDIA",StringComparison.OrdinalIgnoreCase)?name.Contains("1&2")?"GPU 1 & 2":"GPU 3":name=="Gehäuse Fans unten"?"Gehäuse unten":name=="AIO Fans"?"AIO-Lüfter":name=="AIO Pumpe"?"AIO-Pumpe":name;
 static void DrawFans(Graphics g,FanReading[] fans,float x,float width,Color accent){
  const int perPage=5;int pages=Math.Max(1,(fans.Length+perPage-1)/perPage),page=(int)(DateTime.UtcNow.TimeOfDay.TotalSeconds/8)%pages;
  Text(g,"RPM",x+18,127,10,Muted);if(pages>1)Text(g,$"{page+1}/{pages}",x+width-56,100,10,Muted);
  if(fans.Length==0){Text(g,"Keine RPM-Sensoren",x+18,169,14,Muted,false,width-36);return;}
  int row=0;foreach(var fan in fans.Skip(page*perPage).Take(perPage)){
   float y=149+row*55;Text(g,ShortFanName(fan.Name),x+18,y,13,White,true,width-36);
   Text(g,N(fan.Rpm),x+18,y+21,22,accent,true,132);
   if(fan.Temperature.HasValue)Text(g,N(fan.Temperature," °C"),x+width-102,y+25,16,Muted,false,84);
   row++;
  }
 }
 public static int FilledPixels(double? load,int height)=>load is double value&&double.IsFinite(value)?(int)Math.Floor(Math.Clamp(value,0,100)*height/100+1e-9):0;
 static void ThreadGrid(Graphics g,double?[] loads,float x,float y,float width,int height,Color accent){
  const int gap=6;int columns=Math.Min(8,loads.Length),rows=(loads.Length+columns-1)/columns;
  int cellWidth=(int)Math.Floor((width-(columns-1)*gap)/columns);
  int innerHeight=Math.Min(20,(height-(rows-1)*gap)/rows-2),cellHeight=innerHeight+2;
  var smoothing=g.SmoothingMode;g.SmoothingMode=SmoothingMode.None;
  using var border=new Pen(Color.FromArgb(65,85,103));using var background=new SolidBrush(Color.FromArgb(28,41,55));using var missing=new Pen(Muted);
  for(int i=0;i<loads.Length;i++){
   int left=(int)x+(i%columns)*(cellWidth+gap),top=(int)y+(i/columns)*(cellHeight+gap);
   g.FillRectangle(background,left+1,top+1,cellWidth-2,innerHeight);g.DrawRectangle(border,left,top,cellWidth-1,cellHeight-1);
   if(loads[i] is double value&&double.IsFinite(value)){int pixels=FilledPixels(value,innerHeight);if(pixels>0){using var fill=new SolidBrush(LoadColor(value));g.FillRectangle(fill,left+1,top+1+innerHeight-pixels,cellWidth-2,pixels);}}
   else g.DrawLine(missing,left+cellWidth/2-3,top+cellHeight/2,left+cellWidth/2+3,top+cellHeight/2);
  }
  g.SmoothingMode=smoothing;
 }
 static void Text(Graphics g,string text,float x,float y,float size,Color color,bool bold=false,float maxWidth=10000){size+=4;using var brush=new SolidBrush(color);using var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var fmt=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};g.DrawString(text,font,brush,new RectangleF(x,y,maxWidth,size*1.6f),fmt);}
}
