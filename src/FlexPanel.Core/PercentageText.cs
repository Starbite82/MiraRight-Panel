using System.Globalization;
namespace FlexPanel;

public static partial class Renderer {
 static void Percentage(Graphics graphics,double? value,float x,float y,float size,Color color){
  float pixels=size+4,cell=pixels*.68f;
  string digits=value is double number&&double.IsFinite(number)?Math.Clamp(number,0,100).ToString("0",CultureInfo.InvariantCulture).PadLeft(3):"  —";
  using var font=new Font("Segoe UI",pixels,FontStyle.Bold,GraphicsUnit.Pixel);
  using var brush=new SolidBrush(color);
  using var format=new StringFormat(StringFormat.GenericTypographic){Alignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};
  for(int i=0;i<3;i++)graphics.DrawString(digits[i].ToString(),font,brush,new RectangleF(x+i*cell,y,cell,pixels*1.6f),format);
  graphics.DrawString("%",font,brush,x+3*cell+4,y,StringFormat.GenericTypographic);
 }
}
