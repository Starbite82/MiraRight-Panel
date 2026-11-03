// SPDX-License-Identifier: GPL-3.0-only
namespace AioDisplay.App;

using System.Diagnostics;

public interface IDisplayOutput : IDisposable { void Send(Bitmap image,DisplayProfile profile); }
static class LegacyOwner {
 public static void Refuse(string mutex,string application){try{if(Mutex.TryOpenExisting(mutex,out var handle)){handle.Dispose();throw new IOException(application+" läuft noch · bitte im Tray beenden");}}catch(UnauthorizedAccessException){throw new IOException(application+" belegt das Display");}}
}
public sealed class ThermalrightOutput : IDisplayOutput {
 UsbDisplay device;
 public void Send(Bitmap image,DisplayProfile profile){
  if(device==null){
   LegacyOwner.Refuse("Local\\BLUBStandaloneDisplay","AIO-Einzelversion");
   if(Process.GetProcessesByName("TRCC").Length>0||Process.GetProcessesByName("USBLCDNEW").Length>0)throw new IOException("TRCC belegt möglicherweise das Display");
   try{device=new();device.Open();device.Handshake();}catch{Dispose();throw;}
  }
  try{device.Send(PanelWindow.EncodeDisplay(image,profile.Rotation));}catch{Dispose();throw;}
 }
 public void Dispose(){device?.Dispose();device=null;}
}
public sealed class MiraBoxOutput : IDisplayOutput {
 readonly FlexPanel.D92 device=new();
 public void Send(Bitmap image,DisplayProfile profile){LegacyOwner.Refuse("Local\\FlexPanel.D92","FlexPanel-Einzelversion");device.Show(image,profile.MiraBox);if(!device.Status.StartsWith("D92 ·"))throw new IOException(device.Status);}
 public void Dispose()=>device.Dispose();
}
