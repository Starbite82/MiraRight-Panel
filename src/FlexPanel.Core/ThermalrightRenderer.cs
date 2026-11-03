using System.Drawing.Drawing2D;
namespace FlexPanel;

public static partial class Renderer {
 static Bitmap DrawThermalright(Snapshot s,PanelConfig c){
  var b=new Bitmap(1920,462);using var g=Graphics.FromImage(b);
  g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.Clear(Color.FromArgb(9,15,25));
  var cyan=Color.FromArgb(78,216,233);var purple=Color.FromArgb(177,155,255);
  Text(g,"MIRARIGHT PANEL",24,12,21,cyan,true);Text(g,s.SourceName,350,16,16,Muted);Text(g,DateTime.Now.ToString("HH:mm:ss"),1752,12,21,White);
  float width=(1880f-(c.Cards.Length-1)*12)/c.Cards.Length;
  for(int i=0;i<c.Cards.Length;i++){
   string card=c.Cards[i];float x=20+i*(width+12);Color accent=card is "GPU" or "VRAM"?purple:cyan;
   using(var tile=new SolidBrush(Color.FromArgb(17,29,45)))g.FillRectangle(tile,x,55,width,377);
   using(var stripe=new SolidBrush(accent))g.FillRectangle(stripe,x,55,width,4);
   Text(g,card=="FANS"?"LÜFTER":card,x+16,72,21,accent,true);
   void Value(string text,float y,int size=48)=>Text(g,text,x+16,y,size,White,true,width-32);
   void Label(string text,float y)=>Text(g,text,x+16,y,14,Muted,false,width-32);
   switch(card){
    case "CPU":
     Value(N(s.CpuTemp," °C"),112);Percentage(g,s.CpuLoad,x+16,186,30,cyan);
     Label("THREADS",244);ThermalThreads(g,ThreadBars(s),x+16,276,width-32,100,cyan);
     Label("LEISTUNG  "+N(s.CpuWatts," W","0.0"),394);break;
    case "GPU":
     Value(N(s.GpuTemp," °C"),112);Percentage(g,s.GpuLoad,x+16,186,30,purple);
     ThermalMemory(g,BarValue(s,"gpu",s.GpuLoad),100,x+16,251,width-32,12,false);
     Label(s.GpuName,291);Label("LEISTUNG",353);Value(N(s.GpuWatts," W","0.0"),378,25);break;
    case "RAM":case "VRAM":
     bool ram=card=="RAM";double? used=ram?s.RamUsed:s.VramUsed,total=ram?s.RamTotal:s.VramTotal;
     Value(N(total>0?used/total*100:null," %"),112);Label("BENUTZT / GESAMT",208);
     Value(N(used,"","0.0")+" / "+N(total,"","0.0"),242,26);Label("GiB",290);
     ThermalMemory(g,BarValue(s,ram?"ram":"vram",used),total,x+16,329,width-32,18,true);
     Label(ram?"TEMP  "+N(s.RamTemp," °C"):"FREI  "+N(s.VramFree," GiB","0.0"),369);
     if(ram)Label("LEISTUNG  "+N(s.RamWatts," W","0.0"),398);break;
    case "FRAMES":
     Value(N(s.Fps),112);Label("FPS · AKTIVE ANWENDUNG",192);Value(N(s.FrameMs," ms","0.00"),246,32);
     Label("FRAME TIME",300);Text(g,s.FrameApp.Length>0?s.FrameApp:"—",x+16,377,18,White,true,width-32);break;
    case "FANS":DrawFans(g,s.Fans,x,width,cyan);break;
   }
  }
  Text(g,"Benutzt / Gesamt · — = kein aktueller Messwert",24,437,13,Muted);return b;
 }
 static void ThermalMemory(Graphics g,double? used,double? total,float x,float y,float width,int height,bool memory){
  using var track=new SolidBrush(Color.FromArgb(51,71,95));g.FillRectangle(track,x,y,width,height);
  if(used is not double value||total is not double capacity||capacity<=0||!double.IsFinite(value)||!double.IsFinite(capacity))return;
  double load=Math.Clamp(value/capacity,0,1);Color color=memory?MemoryColor(load*100):LoadColor(load*100);
  using var fill=new SolidBrush(color);g.FillRectangle(fill,x,y,width*(float)load,height);
 }
 static void ThermalThreads(Graphics g,double?[] threads,float x,float y,float width,int height,Color accent){
  if(threads.Length==0){Text(g,"—",x,y,20,Muted);return;}
  const int pageSize=32;int pages=(threads.Length+pageSize-1)/pageSize,page=(int)(DateTime.UtcNow.TimeOfDay.TotalSeconds/5)%pages;
  var loads=threads.Skip(page*pageSize).Take(pageSize).ToArray();int columns=Math.Min(8,loads.Length),rows=(loads.Length+columns-1)/columns;float cellWidth=width/columns,cellHeight=(float)height/rows;
  for(int j=0;j<loads.Length;j++){
   float left=x+j%columns*cellWidth,top=y+j/columns*cellHeight;float w=cellWidth-4,h=cellHeight-4;
   using var track=new SolidBrush(Color.FromArgb(51,71,95));g.FillRectangle(track,left,top,w,h);
   if(loads[j] is double value&&double.IsFinite(value)){float fill=h*(float)Math.Clamp(value/100,0,1);using var brush=new SolidBrush(LoadColor(value));g.FillRectangle(brush,left,top+h-fill,w,fill);}
  }
 }
}
