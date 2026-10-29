using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
public static class Log {
 public static string Root=AppDomain.CurrentDomain.BaseDirectory,LastSensorError="";
 public static void Write(string s){try{var path=Path.Combine(Root,"panel.log");if(File.Exists(path)&&new FileInfo(path).Length>1048576)File.WriteAllText(path,"");File.AppendAllText(path,DateTime.Now.ToString("s")+" "+s+Environment.NewLine);}catch(IOException){}}
}
public static class Program {
 const string ShowSignal="Local\\BLUBDisplayShow",StopSignal="Local\\BLUBDisplayStop";
 static void Signal(string name){try{using(var e=EventWaitHandle.OpenExisting(name))e.Set();}catch(WaitHandleCannotBeOpenedException){}}
 [STAThread] public static void Main(string[] args){
  try{
   AppSettings.Load();
   if(args.Contains("--stop")){Signal(StopSignal);return;}
   if(args.Contains("--enable-autostart")){AutoStart.Set(true);return;}
   if(args.Contains("--disable-autostart")){AutoStart.Set(false);return;}
   if(args.Contains("--render")){using(var image=PanelRenderer.RenderView(AfterburnerSnapshot.Read(),PanelRenderer.TotalMemoryGiB(),AppSettings.Current.View,AppSettings.Current.ThreadNumbers))image.Save(Path.Combine(Log.Root,"live-preview.png"),ImageFormat.Png);return;}
   if(args.Contains("--diagnose")){
    var s=AfterburnerSnapshot.Read();File.WriteAllLines(Path.Combine(Log.Root,"sensors.txt"),new[]{s.Status+" "+Log.LastSensorError}.Concat(s.Entries.Select(r=>string.Format("{0:X4} GPU={1} {2} = {3} {4}",r.Id,r.Gpu,r.Name,r.Value,r.Unit))));
    using(var usb=new UsbDisplay()){usb.Open();var response=usb.Handshake();Log.Write("PROBE PM="+usb.Pm+" SUB="+usb.Sub+" "+BitConverter.ToString(response));}return;
   }
   bool first;using(var mutex=new Mutex(true,"Local\\BLUBStandaloneDisplay",out first)){
    if(!first){if(!args.Contains("--minimized"))Signal(ShowSignal);return;}
    AppSettings.Save();
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    using(var show=new EventWaitHandle(false,EventResetMode.AutoReset,ShowSignal))using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,StopSignal))
    using(var form=new PanelWindow(args.Contains("--preview"),args.Contains("--minimized"))){
     var openWait=ThreadPool.RegisterWaitForSingleObject(show,(o,t)=>form.PostOpen(),null,-1,false);
     var stopWait=ThreadPool.RegisterWaitForSingleObject(stop,(o,t)=>form.PostExit(),null,-1,false);
     try{Application.Run(form);}finally{openWait.Unregister(null);stopWait.Unregister(null);}
    }
   }
  }catch(Exception e){Log.Write("ERROR "+e);File.WriteAllText(Path.Combine(Log.Root,"error.txt"),e.ToString());}
 }
}
public sealed class PanelWindow:Form {
 [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window,int command);
 readonly PictureBox picture=new PictureBox();readonly Label status=new Label();NotifyIcon tray;System.Threading.Timer timer;UsbDisplay lcd;int busy;
 volatile bool closing;bool preview,exitRequested,suppressShow;long frames;string lastError="";DateTime lastLog=DateTime.MinValue;
 public PanelWindow(bool previewOnly,bool minimized){
  preview=previewOnly;suppressShow=minimized;Icon=AppBrand.Icon;Text="AIO Display — Einstellungen";ClientSize=new Size(700,435);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;BackColor=Color.FromArgb(9,15,25);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=!minimized;
  Controls.Add(new Label{Text="Live-Vorschau",Bounds=new Rectangle(20,12,320,28),ForeColor=Color.White,Font=new Font("Segoe UI",13,FontStyle.Bold)});
  picture.Bounds=new Rectangle(20,49,320,320);Controls.Add(picture);
  status.Bounds=new Rectangle(20,380,320,48);status.ForeColor=Color.LightSteelBlue;status.Font=new Font("Segoe UI",9);Controls.Add(status);
  Controls.Add(new Label{Text="Einstellungen",Bounds=new Rectangle(365,14,310,30),ForeColor=Color.White,Font=new Font("Segoe UI",14,FontStyle.Bold)});
  Controls.Add(new Label{Text="Displayansicht",Bounds=new Rectangle(365,55,300,22),ForeColor=Color.LightSteelBlue});
  int[] views={6,1,0,2,3,4,5};var selector=new ComboBox{Bounds=new Rectangle(365,80,310,28),DropDownStyle=ComboBoxStyle.DropDownList};selector.Items.AddRange(new object[]{"Dashboard mit CPU und Lüftern","CPU-Detailansicht","Vier CPU-Varianten vergleichen","A · Cyan nach Rot","B · Kompakte Balken","C · Farbsegmente","D · Schmale Linien"});selector.SelectedIndex=Array.IndexOf(views,AppSettings.Current.View);Controls.Add(selector);
  var numbers=Check("Threadnummern anzeigen",122,AppSettings.Current.ThreadNumbers);
  var fans=Check("Lüfter mit 0 RPM ausblenden",153,AppSettings.Current.HideStoppedFans);
  Controls.Add(new Label{Text="Aktualisierung",Bounds=new Rectangle(365,195,140,23),ForeColor=Color.LightSteelBlue});
  var rate=new ComboBox{Bounds=new Rectangle(512,191,163,28),DropDownStyle=ComboBoxStyle.DropDownList};rate.Items.AddRange(new object[]{"1 × pro Sekunde","2 × pro Sekunde","4 × pro Sekunde"});int[] rates={1000,500,250};rate.SelectedIndex=Array.IndexOf(rates,AppSettings.Current.RefreshMs);Controls.Add(rate);
  var auto=Check("Mit Windows starten (minimiert)",237,AutoStart.Enabled);
  var close=Check("Beim Schließen weiterlaufen",268,AppSettings.Current.CloseToTray);
  var note=new Label{Text="Einstellungen werden sofort gespeichert.\nAfterburner und Fan Control müssen laufen.\nDas Display läuft im Infobereich weiter.",Bounds=new Rectangle(365,311,310,67),ForeColor=Color.LightSteelBlue,Font=new Font("Segoe UI",9)};Controls.Add(note);
  var hide=new Button{Text="In den Infobereich",Bounds=new Rectangle(365,386,166,30)};hide.Click+=(a,b)=>HideToTray();Controls.Add(hide);
  var exit=new Button{Text="Beenden",Bounds=new Rectangle(543,386,132,30)};exit.Click+=(a,b)=>ExitNow();Controls.Add(exit);
  Action save=()=>{try{AppSettings.Save();}catch(Exception e){note.Text="Speichern nicht möglich: "+e.Message;}};
  selector.SelectedIndexChanged+=(a,b)=>{AppSettings.Current.View=views[selector.SelectedIndex];numbers.Enabled=AppSettings.Current.View!=1;fans.Enabled=AppSettings.Current.View==6;save();};numbers.Enabled=AppSettings.Current.View!=1;fans.Enabled=AppSettings.Current.View==6;
  numbers.CheckedChanged+=(a,b)=>{AppSettings.Current.ThreadNumbers=numbers.Checked;save();};fans.CheckedChanged+=(a,b)=>{AppSettings.Current.HideStoppedFans=fans.Checked;save();};
  rate.SelectedIndexChanged+=(a,b)=>{AppSettings.Current.RefreshMs=rates[rate.SelectedIndex];if(timer!=null)timer.Change(0,AppSettings.Current.RefreshMs);save();};
  auto.CheckedChanged+=(a,b)=>{try{AutoStart.Set(auto.Checked);}catch(Exception e){note.Text="Autostart nicht geändert: "+e.Message;}save();};
  close.CheckedChanged+=(a,b)=>{AppSettings.Current.CloseToTray=close.Checked;save();};
  var menu=new ContextMenuStrip();menu.Items.Add("Einstellungen öffnen",null,(a,b)=>OpenSettings());menu.Items.Add("Beenden",null,(a,b)=>ExitNow());
  tray=new NotifyIcon{Icon=AppBrand.Icon,Text="AIO Display",Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=(a,b)=>OpenSettings();
  UiTheme.Apply(this,menu);
  Resize+=(a,b)=>{if(WindowState==FormWindowState.Minimized)HideToTray();};
  var windowHandle=Handle;
  BeginInvoke((Action)(()=>{FanData.Start();timer=new System.Threading.Timer(Tick,null,0,AppSettings.Current.RefreshMs);Log.Write(minimized?"START minimized":"START settings");if(!minimized)OpenSettings();}));
 }
 CheckBox Check(string text,int y,bool value){var check=new ReadableCheckBox{Text=text,Bounds=new Rectangle(365,y,320,28),ForeColor=Color.White,Checked=value};Controls.Add(check);return check;}
 protected override void SetVisibleCore(bool value){base.SetVisibleCore(suppressShow?false:value);}
 public void PostOpen(){if(!closing)try{BeginInvoke((Action)OpenSettings);}catch(InvalidOperationException){}}
 public void PostExit(){if(!closing)try{BeginInvoke((Action)ExitNow);}catch(InvalidOperationException){}}
 void OpenSettings(){if(closing)return;suppressShow=false;ShowInTaskbar=true;WindowState=FormWindowState.Normal;Show();ShowWindow(Handle,5);Activate();}
 void HideToTray(){ShowInTaskbar=false;Hide();}
 void ExitNow(){exitRequested=true;Close();}
 protected override void OnFormClosing(FormClosingEventArgs e){
  if(e.CloseReason==CloseReason.UserClosing&&!exitRequested&&AppSettings.Current.CloseToTray){e.Cancel=true;HideToTray();return;}
  closing=true;if(timer!=null)timer.Dispose();base.OnFormClosing(e);
 }
 protected override void OnFormClosed(FormClosedEventArgs e){
  FanData.Stop();tray.Dispose();if(picture.Image!=null)picture.Image.Dispose();if(Interlocked.CompareExchange(ref busy,0,0)==0&&lcd!=null){lcd.Dispose();lcd=null;}base.OnFormClosed(e);
 }
 string Value(AfterburnerSnapshot s,uint id,string name,string suffix){var r=s.Find(id,name);return s.Fresh&&r!=null&&r.Valid?r.Value.ToString("0")+suffix:"--";}
 public static byte[] Encode565(Bitmap bmp){var pixels=new byte[320*320*2];for(int y=0;y<320;y++)for(int x=0;x<320;x++){var c=bmp.GetPixel(319-x,319-y);ushort rgb=(ushort)(((c.R>>3)<<11)|((c.G>>2)<<5)|(c.B>>3));int o=(y*320+x)*2;pixels[o]=(byte)(rgb>>8);pixels[o+1]=(byte)rgb;}return pixels;}
 void Tick(object ignored){
  if(closing||Interlocked.CompareExchange(ref busy,1,0)!=0)return;Bitmap bmp=null;
  try{
   FanData.EnsureRunning();var sensors=AfterburnerSnapshot.Read();bmp=PanelRenderer.RenderView(sensors,PanelRenderer.TotalMemoryGiB(),AppSettings.Current.View,AppSettings.Current.ThreadNumbers);
   if(!preview){if(lcd==null){if(Process.GetProcessesByName("TRCC").Length>0||Process.GetProcessesByName("USBLCDNEW").Length>0)throw new IOException("Bitte TRCC beenden.");lcd=new UsbDisplay();lcd.Open();lcd.Handshake();Log.Write("CONNECTED PM="+lcd.Pm+" SUB="+lcd.Sub);}lcd.Send(Encode565(bmp));frames++;}
   bool fanFresh=FanData.Read(sensors).Any(f=>f.Rpm.HasValue);string info=(preview?"Vorschau ohne Display":"Display verbunden")+" · Afterburner "+(sensors.Fresh?"aktiv":"wartet")+"\nFan Control "+(fanFresh?"aktiv":"wartet");
   if(lastError!=""){Log.Write("RECOVERED");lastError="";}
   if((DateTime.Now-lastLog).TotalSeconds>=5){lastLog=DateTime.Now;File.WriteAllText(Path.Combine(Log.Root,"status.txt"),DateTime.Now.ToString("s")+" USB "+frames+" Frames | "+info.Replace("\n"," | ")+" | CPU="+Value(sensors,0x80,"CPU temperature","°C")+" GPU="+Value(sensors,0,"GPU temperature","°C"));bmp.Save(Path.Combine(Log.Root,"live-preview.png"),ImageFormat.Png);}
   var display=bmp;bmp=null;if(!closing)BeginInvoke((Action)(()=>{if(closing){display.Dispose();return;}var old=picture.Image;picture.Image=display;if(old!=null)old.Dispose();status.Text=info;}));else display.Dispose();
  }catch(Exception e){if(lcd!=null){lcd.Dispose();lcd=null;}if(e.Message!=lastError){lastError=e.Message;Log.Write("USB ERROR "+e.Message);}if(!closing)try{BeginInvoke((Action)(()=>status.Text="Warte auf das Display\n"+lastError));}catch(InvalidOperationException){}
  }finally{if(bmp!=null)bmp.Dispose();if(closing&&lcd!=null){lcd.Dispose();lcd=null;}Interlocked.Exchange(ref busy,0);}
 }
}
