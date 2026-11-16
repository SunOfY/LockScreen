using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

internal sealed class AppCard : CheckBox {
    readonly Bitmap logo;
    internal AppCard(string path,string name,bool selected){
        Tag=path;Text=AccessibleName=name;Checked=selected;
        Size=new Size(108,82);Margin=new Padding(0,0,8,8);
        DoubleBuffered=true;Cursor=Cursors.Hand;Appearance=Appearance.Button;
        try{if(File.Exists(path))using(var icon=Icon.ExtractAssociatedIcon(path)){
            if(icon!=null)logo=icon.ToBitmap();
        }}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}
        if(logo==null)logo=SystemIcons.Application.ToBitmap();
    }
    protected override void Dispose(bool disposing){if(disposing && logo!=null)logo.Dispose();base.Dispose(disposing);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(Style.Canvas);g.SmoothingMode=SmoothingMode.AntiAlias;
        var r=new Rectangle(1,1,Width-3,Height-3);
        using(var p=new GraphicsPath()){
            const int a=16;
            p.AddArc(r.Left,r.Top,a,a,180,90);p.AddArc(r.Right-a,r.Top,a,a,270,90);
            p.AddArc(r.Right-a,r.Bottom-a,a,a,0,90);p.AddArc(r.Left,r.Bottom-a,a,a,90,90);p.CloseFigure();
            using(var fill=new SolidBrush(Checked?Color.FromArgb(237,231,251):Color.White))g.FillPath(fill,p);
            using(var edge=new Pen(Checked?Style.Accent:Color.FromArgb(213,209,224),Checked?2:1))g.DrawPath(edge,p);
        }
        g.DrawImage(logo,(Width-32)/2,13,32,32);
        if(Checked){int x=Width-21;using(var fill=new SolidBrush(Style.Accent))g.FillEllipse(fill,x,7,14,14);
            using(var pen=new Pen(Color.White,1.5f))g.DrawLines(pen,new[]{new Point(x+3,14),new Point(x+6,17),new Point(x+11,11)});}
        TextRenderer.DrawText(g,Text,Font,new Rectangle(4,48,Width-8,27),Style.Ink,
            TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
        if(Focused && ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10));
    }
}
