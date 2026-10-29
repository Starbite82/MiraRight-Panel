using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
public static class UiTheme {
 public static readonly Color Surface=Color.FromArgb(28,43,61),Foreground=Color.FromArgb(240,245,252),Inactive=Color.FromArgb(174,190,211),Selected=Color.FromArgb(51,82,119);
 public static void Apply(Form form,ContextMenuStrip menu){form.ForeColor=Foreground;ApplyControls(form);menu.BackColor=Surface;menu.ForeColor=Foreground;menu.Renderer=new ReadableMenuRenderer();}
 static void ApplyControls(Control parent){foreach(Control c in parent.Controls){
  var combo=c as ComboBox;if(combo!=null){combo.BackColor=Surface;combo.ForeColor=Foreground;combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=23;combo.DrawItem+=(s,e)=>{
   bool selected=(e.State&DrawItemState.Selected)!=0,disabled=(e.State&DrawItemState.Disabled)!=0;Color back=selected?Selected:Surface;
   using(var brush=new SolidBrush(back))e.Graphics.FillRectangle(brush,e.Bounds);
   string text=e.Index>=0?combo.GetItemText(combo.Items[e.Index]):combo.Text;var rect=new Rectangle(e.Bounds.X+5,e.Bounds.Y,e.Bounds.Width-8,e.Bounds.Height);
   TextRenderer.DrawText(e.Graphics,text,e.Font,rect,disabled?Inactive:Foreground,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
   if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();
  };}
  var button=c as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.BackColor=Surface;button.ForeColor=Foreground;button.FlatAppearance.BorderColor=Color.FromArgb(105,127,154);button.FlatAppearance.MouseOverBackColor=Selected;button.FlatAppearance.MouseDownBackColor=Color.FromArgb(62,101,143);}
  if(c.HasChildren)ApplyControls(c);
 }}
}
public sealed class ReadableCheckBox:CheckBox {
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.Clear(BackColor);int top=(Height-15)/2;
  var state=Enabled?(Checked?CheckBoxState.CheckedNormal:CheckBoxState.UncheckedNormal):(Checked?CheckBoxState.CheckedDisabled:CheckBoxState.UncheckedDisabled);
  CheckBoxRenderer.DrawCheckBox(e.Graphics,new Point(0,top),state);
  var textArea=new Rectangle(23,0,Width-23,Height);TextRenderer.DrawText(e.Graphics,Text,Font,textArea,Enabled?UiTheme.Foreground:UiTheme.Inactive,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
  if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,textArea,UiTheme.Foreground,BackColor);
 }
}
public sealed class ReadableMenuRenderer:ToolStripProfessionalRenderer {
 protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(UiTheme.Surface);}
 protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){using(var brush=new SolidBrush(UiTheme.Surface))e.Graphics.FillRectangle(brush,e.AffectedBounds);}
 protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){using(var brush=new SolidBrush(e.Item.Selected?UiTheme.Selected:UiTheme.Surface))e.Graphics.FillRectangle(brush,new Rectangle(Point.Empty,e.Item.Size));}
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?UiTheme.Foreground:UiTheme.Inactive;base.OnRenderItemText(e);}
 protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var pen=new Pen(Color.FromArgb(105,127,154)))e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
}

public static class AppBrand {
 static System.Drawing.Icon icon;
 public static System.Drawing.Icon Icon {get{
  if(icon==null){using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AioDisplay.Icon"))icon=stream==null?System.Drawing.SystemIcons.Application:(System.Drawing.Icon)new System.Drawing.Icon(stream).Clone();}
  return icon;
 }}
}
