// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Text.Json;

public sealed class UnifiedWindow : Form {
 volatile DisplaySettings settings;UnifiedEngine engine;readonly bool previewOnly;readonly bool verify;
 readonly TabControl tabs=new(){Dock=DockStyle.Fill};readonly Label shared=new(){Dock=DockStyle.Top,Height=38,Padding=new Padding(10),ForeColor=Color.LightSteelBlue};
 readonly Dictionary<string,PictureBox> pictures=new();readonly Dictionary<string,Label> labels=new();
 readonly Dictionary<string,ComboBox> gpus=new();readonly Dictionary<string,Dictionary<string,ComboBox>> metrics=new();readonly Dictionary<string,string> signatures=new();
 readonly System.Windows.Forms.Timer refresh=new(){Interval=250};readonly NotifyIcon tray=new();bool closing,exitRequested,suppressShow;DateTime lastReport;
 public UnifiedWindow(bool preview,bool minimized,bool verification=false){
  previewOnly=preview;verify=verification;suppressShow=minimized;settings=DisplaySettings.Load();if(!preview)settings.Save();
  Text="MiraRight Panel — Hardware Monitor";Icon=AppBrand.Icon;ClientSize=new Size(1180,800);MinimumSize=new Size(1000,700);BackColor=UiTheme.Surface;ForeColor=UiTheme.Foreground;Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=!minimized;
  Controls.Add(tabs);var header=new Panel{Dock=DockStyle.Top,Height=38};Controls.Add(header);shared.Dock=DockStyle.Fill;header.Controls.Add(shared);var imprint=new LinkLabel{Text="Impressum",Dock=DockStyle.Right,Width=110,TextAlign=ContentAlignment.MiddleCenter,LinkColor=Color.FromArgb(78,216,233),ActiveLinkColor=Color.White};header.Controls.Add(imprint);imprint.LinkClicked+=(_,_)=>{using var dialog=new AboutWindow(true);dialog.ShowDialog(this);};AddDisplay("Thermalright");AddDisplay("MiraBox");AddGeneral();
  var menu=new ContextMenuStrip();menu.Items.Add("Einstellungen öffnen",null,(_,_)=>PostOpen());menu.Items.Add("Impressum",null,(_,_)=>{using var dialog=new AboutWindow(true);dialog.ShowDialog(this);});menu.Items.Add("Beenden",null,(_,_)=>PostExit());
  tray.Icon=Icon;tray.Text="MiraRight Panel";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=(_,_)=>PostOpen();UiTheme.Apply(this,menu);
  Resize+=(_,_)=>{if(WindowState==FormWindowState.Minimized){Hide();ShowInTaskbar=false;}};
  engine=new(()=>settings,previewOnly);refresh.Tick+=(_,_)=>UpdatePreview();
  Shown+=async(_,_)=>{if(verify){await Task.Delay(9000);if(!closing)CaptureVerification();}};
  var handle=Handle;BeginInvoke((Action)(()=>{engine.Start();refresh.Start();if(!minimized)PostOpen();}));
 }
 DisplayProfile Profile(string kind)=>kind=="Thermalright"?settings.Thermalright:settings.MiraBox;
 void Change(string kind,Action<DisplayProfile> change){
  try{var next=JsonSerializer.Deserialize<DisplaySettings>(JsonSerializer.Serialize(settings));var profile=kind=="Thermalright"?next.Thermalright:next.MiraBox;change(profile);next.Save();settings=next;}
  catch(Exception e){MessageBox.Show(this,e.Message,"Einstellungen nicht gespeichert");}
 }
 static ComboBox SelectBox(object[] values,int selected,int width=425){var box=new ComboBox{Width=width,DropDownStyle=ComboBoxStyle.DropDownList};box.Items.AddRange(values);box.SelectedIndex=Math.Clamp(selected,0,values.Length-1);return box;}
 void AddDisplay(string kind){
  var initial=Profile(kind);var page=new TabPage(kind=="Thermalright"?"Thermalright FROZEN WARFRAME 360":"MiraBox D92 9.2-inch Universal Screen for PC"){BackColor=BackColor};tabs.TabPages.Add(page);
  var split=new SplitContainer{Size=new Size(1140,650),Dock=DockStyle.Fill,SplitterDistance=600,Panel1MinSize=330,Panel2MinSize=430};page.Controls.Add(split);
  var picture=new PictureBox{Dock=DockStyle.Fill,BackColor=Color.FromArgb(9,15,25),SizeMode=PictureBoxSizeMode.Zoom};pictures[kind]=picture;split.Panel1.Controls.Add(picture);
  var status=new Label{Dock=DockStyle.Bottom,Height=98,Padding=new Padding(12),ForeColor=Color.LightSteelBlue};labels[kind]=status;split.Panel1.Controls.Add(status);
  var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(12)};split.Panel2.Controls.Add(flow);
  void Title(string text)=>flow.Controls.Add(new Label{Text=text,AutoSize=true,Margin=new Padding(3,9,3,2)});
  var enabled=new CheckBox{Text="Display aktiv",AutoSize=true,Checked=initial.Enabled};flow.Controls.Add(enabled);enabled.CheckedChanged+=(_,_)=>Change(kind,p=>p.Enabled=enabled.Checked);
  Title("Design / Ansicht");
  int[] views=[6,1,0,2,3,4,5];object[] designs=kind=="Thermalright"?["Dashboard mit CPU und Lüftern","CPU-Detailansicht","Vier CPU-Varianten","Cyan nach Rot","Kompakte Balken","Farbsegmente","Schmale Linien"]:["Klassisches Dashboard","Thermalright · Panorama"];
  var design=SelectBox(designs,kind=="Thermalright"?Array.IndexOf(views,initial.AioView):initial.MiraBox.Design=="Thermalright"?1:0);flow.Controls.Add(design);
  design.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>{if(kind=="Thermalright")p.AioView=views[design.SelectedIndex];else p.MiraBox.Design=design.SelectedIndex==1?"Thermalright":"Classic";});
  Title("Drehung · nur auf dem Display");int[] angles=kind=="Thermalright"?[0,90,180,270]:[0,180];var rotation=SelectBox(angles.Select(v=>(object)(v+"°")).ToArray(),Array.IndexOf(angles,initial.Rotation));flow.Controls.Add(rotation);rotation.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.Rotation=angles[rotation.SelectedIndex]);
  Title("Displayrate · Ziel");var fps=SelectBox(FlexPanel.PanelConfig.DisplayRates.Select(v=>(object)(v+" Bilder/s")).ToArray(),Array.IndexOf(FlexPanel.PanelConfig.DisplayRates,initial.Fps));flow.Controls.Add(fps);fps.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.Fps=FlexPanel.PanelConfig.DisplayRates[fps.SelectedIndex]);
  Title("Messwerte übernehmen · unabhängig von der Displayrate");var rate=SelectBox(FlexPanel.PanelConfig.SensorIntervals.Select(v=>(object)$"{1000d/v:0.#} Hz · {v} ms").ToArray(),Array.IndexOf(FlexPanel.PanelConfig.SensorIntervals,initial.SensorMs));flow.Controls.Add(rate);rate.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.SensorMs=FlexPanel.PanelConfig.SensorIntervals[rate.SelectedIndex]);
  Title("Balkenglättung");var smooth=SelectBox(["Aus","Leicht · 0,25 s","Mittel · 0,5 s","Ruhig · 1 s","Stark · 2 s"],Array.IndexOf(FlexPanel.PanelConfig.BarWindows,initial.BarWindowMs));flow.Controls.Add(smooth);smooth.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.BarWindowMs=FlexPanel.PanelConfig.BarWindows[smooth.SelectedIndex]);
  Title("Messwert-Zahlen glätten · inkl. FPS und Lüfter");var percentages=SelectBox(["Aus","Leicht · 0,25 s","Mittel · 0,5 s","Ruhig · 1 s","Stark · 2 s"],Array.IndexOf(FlexPanel.PanelConfig.BarWindows,initial.ValueWindowMs??initial.PercentWindowMs));flow.Controls.Add(percentages);percentages.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.ValueWindowMs=FlexPanel.PanelConfig.BarWindows[percentages.SelectedIndex]);
  Title("Hardware-Sensorquelle");var source=SelectBox(["LibreHardwareMonitor","MSI Afterburner"],initial.Source=="MSIAfterburner"?1:0);flow.Controls.Add(source);source.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.Source=source.SelectedIndex==1?"MSIAfterburner":"LibreHardwareMonitor");
  Title("Grafikkarte");var gpu=SelectBox([new Choice("","Automatisch · größter Grafikspeicher")],0);gpus[kind]=gpu;flow.Controls.Add(gpu);gpu.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.Gpu=((Choice)gpu.SelectedItem).Id);
  if(kind=="MiraBox"){
   Title("Helligkeit");var brightness=new NumericUpDown{Minimum=1,Maximum=100,Value=initial.MiraBox.Brightness,Width=120};flow.Controls.Add(brightness);brightness.ValueChanged+=(_,_)=>Change(kind,p=>p.MiraBox.Brightness=(int)brightness.Value);
   Title("Anzeigebereiche · Auswahl und Reihenfolge");var cards=new CheckedListBox{Width=425,Height=130,CheckOnClick=true};foreach(var card in initial.MiraBox.Cards)cards.Items.Add(card,true);foreach(var card in new[]{"CPU","GPU","RAM","VRAM","FRAMES","FANS"}.Except(initial.MiraBox.Cards))cards.Items.Add(card,false);flow.Controls.Add(cards);
   cards.ItemCheck+=(_,_)=>BeginInvoke((Action)(()=>{var selected=cards.CheckedItems.Cast<string>().ToArray();if(selected.Length>0)Change(kind,p=>p.MiraBox.Cards=selected);}));
   var order=new FlowLayoutPanel{Width=425,Height=34};flow.Controls.Add(order);
   foreach(int direction in new[]{-1,1}){var move=new Button{Text=direction<0?"↑":"↓",Width=70};order.Controls.Add(move);move.Click+=(_,_)=>{int from=cards.SelectedIndex,to=from+direction;if(from<0||to<0||to>=cards.Items.Count)return;var name=cards.Items[from];bool selected=cards.GetItemChecked(from);cards.Items.RemoveAt(from);cards.Items.Insert(to,name);cards.SetItemChecked(to,selected);cards.SelectedIndex=to;var active=cards.CheckedItems.Cast<string>().ToArray();if(active.Length>0)Change(kind,p=>p.MiraBox.Cards=active);};}
  }else{
   var stopped=new CheckBox{Text="Lüfter mit 0 RPM ausblenden",AutoSize=true,Checked=initial.HideStoppedFans};flow.Controls.Add(stopped);stopped.CheckedChanged+=(_,_)=>Change(kind,p=>p.HideStoppedFans=stopped.Checked);
  }
  var numbers=new CheckBox{Text="Threadnummern anzeigen",AutoSize=true,Checked=initial.ThreadNumbers};flow.Controls.Add(numbers);numbers.CheckedChanged+=(_,_)=>Change(kind,p=>p.ThreadNumbers=numbers.Checked);
  var controls=new Dictionary<string,ComboBox>();metrics[kind]=controls;
  foreach(var metric in FlexPanel.SensorSelection.Metrics){Title(metric.Name+" · Sensor");var box=SelectBox([new Choice("","Automatisch")],0);flow.Controls.Add(box);controls[metric.Key]=box;box.SelectionChangeCommitted+=(_,_)=>Change(kind,p=>p.MiraBox.SensorSelections[p.Source+"/"+metric.Key]=((Choice)box.SelectedItem).Id);}
  // An actual trailing control extends the scroll range reliably, including at higher DPI.
  flow.Controls.Add(new Panel{Width=1,Height=64,Margin=Padding.Empty});
 }
 void AddGeneral(){
  var page=new TabPage("Gemeinsame Sensoren & Start"){BackColor=BackColor};tabs.TabPages.Add(page);var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(20)};page.Controls.Add(flow);
  flow.Controls.Add(new Label{Text="Beide Displays teilen sich einen Sensorprozess und eine FPS-Erfassung. Hardware wird gemeinsam bis zu 10-mal pro Sekunde gelesen. Jede Anzeige übernimmt Werte und zeichnet in ihrem eigenen Takt.",Width=900,Height=65});
  var permissions=new Button{Text=settings.AdminSensors?"Sensorrechte deaktivieren …":"Sensorrechte aktivieren …",Width=270,Height=36,Enabled=!previewOnly};flow.Controls.Add(permissions);
  var permissionStatus=new Label{Text=settings.AdminSensors?"Erweiterte Sensorrechte sind aktiv.":"Standardrechte: Mainboard-Lüfter, Temperaturen und FPS können fehlen.",Width=900,Height=35};flow.Controls.Add(permissionStatus);
  permissions.Click+=(_,_)=>{bool elevated=!settings.AdminSensors;string text=elevated?(ScheduledSensorTask.Installed?"Der Sensorprozess startet über die eingerichtete Windows-Aufgabe ohne erneute UAC-Abfrage. Beide Anzeigen werden kurz neu verbunden. Jetzt starten?":"Der Sensorprozess benötigt eine Windows-Administratorbestätigung. Beide Anzeigen werden kurz neu verbunden. Jetzt starten?"):"Ohne Administratorrechte neu verbinden? Mainboard-Lüfter, Temperaturen und FPS können fehlen.";if(MessageBox.Show(this,text,"Sensorrechte",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;engine.Dispose();var next=JsonSerializer.Deserialize<DisplaySettings>(JsonSerializer.Serialize(settings));next.AdminSensors=elevated;next.Save();settings=next;permissions.Text=elevated?"Sensorrechte deaktivieren …":"Sensorrechte aktivieren …";permissionStatus.Text=elevated?"Erweiterte Sensorrechte sind aktiv.":"Standardrechte: Mainboard-Lüfter, Temperaturen und FPS können fehlen.";engine=new(()=>settings,previewOnly);engine.Start();};
  var auto=new CheckBox{Text="Gemeinsames Panel mit Windows starten (minimiert)",AutoSize=true,Enabled=!previewOnly,Checked=AutoStart.Enabled};flow.Controls.Add(auto);auto.CheckedChanged+=(_,_)=>AutoStart.Set(auto.Checked);
  bool sensorTaskInstalled=!previewOnly&&ScheduledSensorTask.Installed;
  flow.Controls.Add(new Label{Text=sensorTaskInstalled?"Sensorstart ohne UAC: eingerichtet. Der Sensorprozess startet automatisch mit dem Panel.":"Sensorstart ohne UAC: einmalige Einrichtung über Install-SensorAutostart.ps1 (als Administrator).",Width=900,Height=48});
  flow.Controls.Add(new Label{Text="Die bisherigen Einzelprogramme bleiben als Backup erhalten. Deren Autostarts müssen beim dauerhaften Wechsel deaktiviert werden, damit sie die USB-Geräte nicht gleichzeitig belegen.",Width=900,Height=65});
  var about=new Button{Text="Info & Impressum",Width=270,Height=36};flow.Controls.Add(about);about.Click+=(_,_)=>{using var dialog=new AboutWindow();dialog.ShowDialog(this);};
  var debug=new Button{Text="Debug-Log öffnen",Width=270,Height=36};flow.Controls.Add(debug);debug.Click+=(_,_)=>{try{Log.Debug("log.opened");System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe"){UseShellExecute=true,ArgumentList={Log.DebugPath}});}catch(Exception e){MessageBox.Show(this,e.Message,"Debug-Log");}};
  var hide=new Button{Text="In den Infobereich",Width=270,Height=36};flow.Controls.Add(hide);hide.Click+=(_,_)=>{Hide();ShowInTaskbar=false;};var exit=new Button{Text="Gemeinsames Panel beenden",Width=270,Height=36};flow.Controls.Add(exit);exit.Click+=(_,_)=>PostExit();
 }
 record Choice(string Id,string Name){public override string ToString()=>Name;}
 void UpdateChoices(string kind){
  var p=Profile(kind);var bundle=engine.Latest;var sample=bundle.Select(p.Source,p.Gpu);
  string signature=p.Source+"|"+p.Gpu+"|"+string.Join('|',bundle.Libre.Keys)+"|"+string.Join('|',sample.Sensors.Select(s=>s.Id))+"|"+string.Join('|',p.MiraBox.SensorSelections.Select(s=>s.Key+s.Value));
  if(signatures.TryGetValue(kind,out string previous)&&previous==signature)return;signatures[kind]=signature;
  var gpu=gpus[kind];gpu.Items.Clear();gpu.Items.Add(new Choice("","Automatisch · größter Grafikspeicher"));foreach(var card in bundle.Libre)gpu.Items.Add(new Choice(card.Key,card.Value.GpuName));int index=gpu.Items.Cast<Choice>().ToList().FindIndex(c=>c.Id==p.Gpu);if(index<0){gpu.Items.Add(new Choice(p.Gpu,"Zurzeit nicht verfügbar · "+p.Gpu));index=gpu.Items.Count-1;}gpu.SelectedIndex=index;
  foreach(var metric in FlexPanel.SensorSelection.Metrics){var box=metrics[kind][metric.Key];box.Items.Clear();box.Items.Add(new Choice("","Automatisch"));foreach(var sensor in sample.Sensors.Where(r=>FlexPanel.SensorSelection.Matches(r,metric.Unit)).DistinctBy(r=>r.Id))box.Items.Add(new Choice(FlexPanel.SensorSelection.Key(sensor),sensor.Hardware+" · "+sensor.Name));p.MiraBox.SensorSelections.TryGetValue(p.Source+"/"+metric.Key,out string id);int selected=box.Items.Cast<Choice>().ToList().FindIndex(c=>c.Id==(id??""));if(selected<0){box.Items.Add(new Choice(id,"Zurzeit nicht verfügbar · "+id));selected=box.Items.Count-1;}box.SelectedIndex=selected;}
 }
 void UpdatePreview(){
  if(closing)return;
  try{foreach(string kind in new[]{"Thermalright","MiraBox"}){var display=kind=="Thermalright"?engine.Thermalright:engine.MiraBox;var p=Profile(kind);if(Visible){var image=display.Preview();if(image!=null){var old=pictures[kind].Image;pictures[kind].Image=image;old?.Dispose();}UpdateChoices(kind);}labels[kind].Text=$"{display.Status}\n{display.ActualFps:0.0} USB-Bilder/s · Ziel {p.Fps} · Werte {1000d/p.SensorMs:0.#} Hz\n{p.Source}";}
   shared.Text=(previewOnly?"Vorschau · beide USB-Ausgaben aus":"Gemeinsames Panel · beide Displays unabhängig")+" · "+(previewOnly?"ohne Rechteanforderung":settings.AdminSensors?"Sensorrechte aktiviert":"Standard-Sensorrechte");
   if((DateTime.UtcNow-lastReport).TotalSeconds>=3){lastReport=DateTime.UtcNow;engine.SaveState();}
  }catch(Exception e){shared.Text=e.Message;}
 }
 void CaptureVerification(){var selected=tabs.SelectedTab;try{foreach(int index in new[]{0,1}){tabs.SelectedIndex=index;tabs.PerformLayout();using var image=new Bitmap(Width,Height);DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(Path.Combine(Log.Root,index==0?"Unified-Thermalright.png":"Unified-MiraBox.png"));}using var thermal=engine.Thermalright.Preview();thermal?.Save(Path.Combine(Log.Root,"thermalright-preview.png"));using var mira=engine.MiraBox.Preview();mira?.Save(Path.Combine(Log.Root,"mirabox-preview.png"));engine.SaveState();}finally{tabs.SelectedTab=selected;}}
 protected override void SetVisibleCore(bool value)=>base.SetVisibleCore(suppressShow?false:value);
 public void PostOpen(){if(closing)return;try{BeginInvoke((Action)(()=>{suppressShow=false;ShowInTaskbar=true;WindowState=FormWindowState.Normal;Show();Activate();}));}catch(InvalidOperationException){}}
 public void PostExit(){if(closing)return;try{BeginInvoke((Action)(()=>{exitRequested=true;Close();}));}catch(InvalidOperationException){}}
 protected override void OnFormClosing(FormClosingEventArgs e){
  if(e.CloseReason==CloseReason.UserClosing&&!exitRequested){e.Cancel=true;Hide();ShowInTaskbar=false;return;}
  closing=true;refresh.Stop();try{engine.Dispose();}catch(Exception ex){Log.Write(ex.ToString());if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;closing=false;refresh.Start();MessageBox.Show(this,"Der Gerätezugriff wird noch beendet. Bitte erneut Beenden wählen.");return;}}
  tray.Visible=false;tray.Dispose();foreach(var image in pictures.Values)image.Image?.Dispose();base.OnFormClosing(e);
 }
}
