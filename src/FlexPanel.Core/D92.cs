using HidSharp;
using System.Buffers.Binary;
using System.Text;
using System.Drawing.Imaging;
namespace FlexPanel;

// Wire-format reference: nomunomu0504/sub-screen-player, MIT/Apache-2.0.
// Only volatile live frames are sent. No firmware, stored logo or SCREEN commands.
public sealed class D92 : IDisposable {
 HidStream? stream;DateTime keepAlive;int brightness=-1;
 public string Status {get;private set;}="USB noch nicht verbunden";
 public static HidDevice[] Devices()=>DeviceList.Local.GetHidDevices().Where(d=>((d.VendorID==0x2100&&d.ProductID==0x0006)||(d.VendorID==0x5548&&d.ProductID==0x1011))&&d.GetMaxOutputReportLength()==1025).ToArray();
 public static byte[] Command(string word,byte[]? args=null) {
  if(word is not("DIS" or "CONNECT" or "LIG" or "CLE"))throw new ArgumentException("Unzulässiger D92-Befehl");
  byte[] b=new byte[1024];Encoding.ASCII.GetBytes("CRT\0\0"+word).CopyTo(b,0);args?.CopyTo(b,5+word.Length);return b;
 }
 public static byte[] Frame(byte[] jpeg) {
  if(jpeg.Length>512*1024 || jpeg.Length<2 || jpeg[0]!=255 || jpeg[1]!=216)throw new ArgumentException("Ungültiges JPEG");
  int total=32+jpeg.Length;byte[] b=new byte[((total+1023)/1024)*1024];
  Encoding.ASCII.GetBytes("CRT\0\0DRA").CopyTo(b,0);BinaryPrimitives.WriteInt32BigEndian(b.AsSpan(8,4),total);b[12]=0xb1;jpeg.CopyTo(b,32);return b;
 }
 void Write(byte[] bytes) {
  if(stream==null)throw new IOException("USB nicht verbunden");
  byte[] report=new byte[1025];
  for(int p=0;p<bytes.Length;p+=1024){Buffer.BlockCopy(bytes,p,report,1,1024);stream.Write(report);}
 }
 public void Show(Bitmap bitmap,PanelConfig config) {
  try {
   if(stream==null) {
    // Do not compete with the vendor application's USB writer.
    if(System.Diagnostics.Process.GetProcessesByName("MiraBox Craft").Length>0){Status="MiraBox Craft bitte vollständig beenden";return;}
    var matches=Devices().Where(d=>string.IsNullOrEmpty(config.DeviceSerial)||string.Equals(d.GetSerialNumber(),config.DeviceSerial,StringComparison.OrdinalIgnoreCase)).ToArray();
    if(matches.Length!=1){Status=matches.Length==0?"D92 nicht gefunden":"Mehrere D92: Seriennummer auswählen";return;}
    stream=matches[0].Open();stream.WriteTimeout=1500;Write(Command("DIS"));brightness=-1;keepAlive=DateTime.MinValue;
   }
   if((DateTime.UtcNow-keepAlive).TotalSeconds>=2){Write(Command("CONNECT"));keepAlive=DateTime.UtcNow;}
   if(brightness!=config.Brightness){Write(Command("LIG",[0,0,(byte)Math.Clamp(config.Brightness,1,100)]));brightness=config.Brightness;}
   using var portrait=(Bitmap)bitmap.Clone();if(config.Rotate180)portrait.RotateFlip(RotateFlipType.Rotate180FlipNone);portrait.RotateFlip(RotateFlipType.Rotate90FlipNone);
   using var ms=new MemoryStream();using var ep=new EncoderParameters(1);ep.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,85L);
   portrait.Save(ms,ImageCodecInfo.GetImageEncoders().First(x=>x.FormatID==ImageFormat.Jpeg.Guid),ep);
   Write(Frame(ms.ToArray()));Thread.Sleep(4);Status="D92 · direkt über USB · 1920 × 462";
  }catch(Exception ex){Status="USB: "+ex.Message;stream?.Dispose();stream=null;}
 }
 public void Dispose(){try{if(stream!=null)Write(Command("CLE",[0,0,(byte)'D',(byte)'C']));}catch{}stream?.Dispose();stream=null;}
}
