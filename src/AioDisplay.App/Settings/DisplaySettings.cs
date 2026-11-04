// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Text.Json;

public sealed class DisplayProfile {
 public string Kind {get;set;}="Thermalright";public bool Enabled {get;set;}=true;
 public int Rotation {get;set;}public int Fps {get;set;}=10;public int SensorMs {get;set;}=100;public int BarWindowMs {get;set;}=500;
 public string Source {get;set;}="LibreHardwareMonitor";public string Gpu {get;set;}="";
 public int AioView {get;set;}=6;public bool ThreadNumbers {get;set;}=true;public bool HideStoppedFans {get;set;}=true;
 public FlexPanel.PanelConfig MiraBox {get;set;}=new FlexPanel.PanelConfig().Normalize();
 public void Validate(){
  if(Kind!="Thermalright"&&Kind!="MiraBox")throw new InvalidDataException("Unbekanntes Display");
  if(!(Kind=="MiraBox"?new[]{0,180}:new[]{0,90,180,270}).Contains(Rotation))Rotation=0;
  if(!FlexPanel.PanelConfig.DisplayRates.Contains(Fps))Fps=10;if(!FlexPanel.PanelConfig.SensorIntervals.Contains(SensorMs))SensorMs=100;
  if(!FlexPanel.PanelConfig.BarWindows.Contains(BarWindowMs))BarWindowMs=500;
  if(Source!="MSIAfterburner")Source="LibreHardwareMonitor";if(AioView<0||AioView>6)AioView=6;
  MiraBox??=new();MiraBox.Normalize();MiraBox.GpuId=Gpu;MiraBox.SensorSource=Source;MiraBox.BarWindowMs=BarWindowMs;MiraBox.Rotate180=Rotation==180;MiraBox.ThreadNumbers=ThreadNumbers;
 }
 public DisplayProfile Copy()=>JsonSerializer.Deserialize<DisplayProfile>(JsonSerializer.Serialize(this));
}
public sealed class DisplaySettings {
 public int Version {get;set;}=1;public bool AdminSensors {get;set;}
 public string FanConfigPath {get;set;}="";
 public DisplayProfile Thermalright {get;set;}=new();
 public DisplayProfile MiraBox {get;set;}=new(){Kind="MiraBox"};
 public static string FilePath=>Path.Combine(Log.Root,"displays.json");
 public static DisplaySettings Load(){
  DisplaySettings settings;
  if(File.Exists(FilePath))settings=JsonSerializer.Deserialize<DisplaySettings>(File.ReadAllText(FilePath))??throw new InvalidDataException("Displayeinstellungen fehlen");
  else{
   var legacy=AppSettings.Current;settings=new(){AdminSensors=legacy.AdminSensors};
   settings.Thermalright=new(){Rotation=legacy.Rotation,Fps=FlexPanel.PanelConfig.DisplayRates.MinBy(f=>Math.Abs(1000d/f-legacy.RefreshMs)),SensorMs=legacy.SensorMs,BarWindowMs=legacy.BarWindowMs,Source=legacy.SensorProvider==1?"MSIAfterburner":"LibreHardwareMonitor",Gpu=legacy.GpuIdentifier,AioView=legacy.View,ThreadNumbers=legacy.ThreadNumbers,HideStoppedFans=legacy.HideStoppedFans};
   string flex=Path.Combine(Log.Root,"legacy-flexpanel.json");
   if(File.Exists(flex)){var imported=JsonSerializer.Deserialize<FlexPanel.PanelConfig>(File.ReadAllText(flex))??new();imported.Normalize();settings.MiraBox=new(){Kind="MiraBox",Enabled=imported.UsbEnabled,MiraBox=imported,Rotation=imported.Rotate180?180:0,Fps=imported.DisplayFps,SensorMs=imported.SensorMs,BarWindowMs=imported.BarWindowMs,Source=imported.SensorSource,Gpu=imported.GpuId};settings.FanConfigPath=imported.FanControlConfigPath;}
  }
  if(settings.Version!=1||settings.Thermalright==null||settings.MiraBox==null)throw new InvalidDataException("Unbekannte Displayeinstellungen");
  settings.Thermalright.Kind="Thermalright";settings.MiraBox.Kind="MiraBox";settings.Thermalright.Validate();settings.MiraBox.Validate();return settings;
 }
 public void Save(){Thermalright.Validate();MiraBox.Validate();string temporary=FilePath+".tmp";File.WriteAllText(temporary,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temporary,FilePath,true);}
}
