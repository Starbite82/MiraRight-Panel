using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public static class Log {
 public static string Root=AppDomain.CurrentDomain.BaseDirectory,LastSensorError="";
 public static void Write(string s){File.AppendAllText(Path.Combine(Root,"panel.log"),DateTime.Now.ToString("s")+" "+s+Environment.NewLine);}
}
public static class Program {
 [STAThread] public static void Main(string[] args){
  try{
   if(args.Contains("--render")){using(var image=PanelRenderer.Render(AfterburnerSnapshot.Read(),PanelRenderer.TotalMemoryGiB()))image.Save(Path.Combine(Log.Root,"live-preview.png"),ImageFormat.Png);return;}
   if(args.Contains("--diagnose")){
    var s=AfterburnerSnapshot.Read();File.WriteAllLines(Path.Combine(Log.Root,"sensors.txt"),new[]{s.Status+" "+Log.LastSensorError}.Concat(s.Entries.Select(r=>string.Format("{0:X4} GPU={1} {2} = {3} {4}",r.Id,r.Gpu,r.Name,r.Value,r.Unit))));
    using(var usb=new UsbDisplay()){usb.Open();var response=usb.Handshake();Log.Write("PROBE PM="+usb.Pm+" SUB="+usb.Sub+" "+BitConverter.ToString(response));}return;
   }
   bool first;using(var mutex=new Mutex(true,"Local\\BLUBStandaloneDisplay",out first)){
    if(!first)return;
    if(Process.GetProcessesByName("TRCC").Length>0||Process.GetProcessesByName("USBLCDNEW").Length>0)throw new Exception("Bitte zuerst TRCC und seine LCD-Helfer beenden.");
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new PanelWindow(args.Contains("--preview")));
   }
  }catch(Exception e){Log.Write("ERROR "+e);File.WriteAllText(Path.Combine(Log.Root,"error.txt"),e.ToString());}
 }
}
public sealed class PanelWindow:Form {
 PictureBox picture=new PictureBox();Label status=new Label();NotifyIcon tray;System.Threading.Timer timer;UsbDisplay lcd;int busy;bool closing,preview;long frames;string lastError="";DateTime lastLog=DateTime.MinValue;
 public PanelWindow(bool previewOnly){
  preview=previewOnly;Text="BLUB Display — MSI Afterburner";ClientSize=new Size(360,410);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;BackColor=Color.FromArgb(9,15,25);
  picture.Bounds=new Rectangle(20,15,320,320);picture.SizeMode=PictureBoxSizeMode.Normal;Controls.Add(picture);
  status.Bounds=new Rectangle(20,345,320,55);status.ForeColor=Color.LightSteelBlue;Controls.Add(status);
  var menu=new ContextMenuStrip();menu.Items.Add("Vorschau",null,(a,b)=>{Show();WindowState=FormWindowState.Normal;Activate();});menu.Items.Add("Beenden",null,(a,b)=>Close());
  tray=new NotifyIcon{Icon=SystemIcons.Application,Text="BLUB Display / Afterburner",Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=(a,b)=>{Show();WindowState=FormWindowState.Normal;};
  Resize+=(a,b)=>{if(WindowState==FormWindowState.Minimized)Hide();};
  Shown+=(a,b)=>timer=new System.Threading.Timer(Tick,null,0,500);
  FormClosing+=(a,b)=>{closing=true;if(timer!=null)timer.Dispose();};
  FormClosed+=(a,b)=>{tray.Dispose();if(picture.Image!=null)picture.Image.Dispose();if(Interlocked.CompareExchange(ref busy,0,0)==0&&lcd!=null){lcd.Dispose();lcd=null;}};
 }
 string Value(AfterburnerSnapshot s,uint id,string name,string suffix,double divisor=1){var r=s.Find(id,name);return s.Fresh&&r!=null&&r.Valid?(r.Value/divisor).ToString(divisor==1?"0":"0.0")+suffix:"--";}
 public static byte[] Encode565(Bitmap bmp){
  var pixels=new byte[320*320*2];
  // TRCC had 180 degrees selected; rotate the wire image while keeping preview upright.
  for(int y=0;y<320;y++)for(int x=0;x<320;x++){var c=bmp.GetPixel(319-x,319-y);ushort rgb=(ushort)(((c.R>>3)<<11)|((c.G>>2)<<5)|(c.B>>3));int o=(y*320+x)*2;pixels[o]=(byte)(rgb>>8);pixels[o+1]=(byte)rgb;}return pixels;
 }
 void Tick(object ignored){
  if(closing||Interlocked.Exchange(ref busy,1)!=0)return;
  Bitmap bmp=null;
  try{
   var sensors=AfterburnerSnapshot.Read();bmp=PanelRenderer.Render(sensors,PanelRenderer.TotalMemoryGiB());
   if(!preview){if(lcd==null){lcd=new UsbDisplay();lcd.Open();lcd.Handshake();Log.Write("CONNECTED PM="+lcd.Pm+" SUB="+lcd.Sub);}lcd.Send(Encode565(bmp));frames++;}
   string info=(preview?"VORSCHAU": "USB aktiv · "+frames+" Frames")+"\n"+sensors.Status;
   if(lastError!=""){Log.Write("RECOVERED");lastError="";}
   if((DateTime.Now-lastLog).TotalSeconds>=5){lastLog=DateTime.Now;File.WriteAllText(Path.Combine(Log.Root,"status.txt"),DateTime.Now.ToString("s")+" "+info.Replace("\n"," | ")+" | CPU="+Value(sensors,0x80,"CPU temperature","°C")+" GPU="+Value(sensors,0,"GPU temperature","°C"));bmp.Save(Path.Combine(Log.Root,"live-preview.png"),ImageFormat.Png);}
   var display=bmp;bmp=null;
   if(!closing)BeginInvoke((Action)(()=>{if(closing){display.Dispose();return;}var old=picture.Image;picture.Image=display;if(old!=null)old.Dispose();status.Text=info;}));else display.Dispose();
  }catch(Exception e){if(lcd!=null){lcd.Dispose();lcd=null;}if(e.Message!=lastError){lastError=e.Message;Log.Write("USB ERROR "+e.Message);}if(!closing)try{BeginInvoke((Action)(()=>status.Text="USB getrennt / erneuter Versuch\n"+lastError));}catch(InvalidOperationException){}
  }finally{if(bmp!=null)bmp.Dispose();if(closing&&lcd!=null){lcd.Dispose();lcd=null;}Interlocked.Exchange(ref busy,0);}
 }
}
