using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal sealed class RaisedButton : Button {
    internal bool Globe;
    internal string Symbol,Caption;
    bool down, hover;
    public RaisedButton(){DoubleBuffered=true;}
    protected override void OnMouseEnter(System.EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(System.EventArgs e){down=hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){down=e.Button==MouseButtons.Left;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
    static GraphicsPath Frame(RectangleF r){
        var p=new GraphicsPath();const int arc=14;
        p.AddArc(r.Left,r.Top,arc,arc,180,90);p.AddArc(r.Right-arc,r.Top,arc,arc,270,90);
        p.AddArc(r.Right-arc,r.Bottom-arc,arc,arc,0,90);p.AddArc(r.Left,r.Bottom-arc,arc,arc,90,90);p.CloseFigure();return p;
    }
    protected override void OnSizeChanged(System.EventArgs e){
        base.OnSizeChanged(e);
        if(Globe && Width>15 && Height>15){using(var shape=Frame(new RectangleF(1,1,Width-2,Height-2))){var old=Region;Region=new Region(shape);if(old!=null)old.Dispose();}}
    }
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        g.Clear(Parent==null?Style.Canvas:Parent.BackColor);
        bool primary=BackColor==Style.Accent;
        var face=new RectangleF(1,down?3:1,Width-3,Height-6);
        var shadow=face;shadow.Y=4;
        using(var p=Frame(shadow))using(var b=new SolidBrush(primary?Color.FromArgb(57,38,108):Color.FromArgb(181,175,196)))g.FillPath(b,p);
        using(var p=Frame(face))
        using(var b=new LinearGradientBrush(face,primary?(hover?Color.FromArgb(145,115,219):Color.FromArgb(123,94,196)):Color.White,
                primary?Style.Accent:(hover?Color.FromArgb(235,229,249):Color.FromArgb(239,237,244)),90f))
        using(var edge=new Pen(primary?Color.FromArgb(91,66,152):Color.FromArgb(205,199,217))){g.FillPath(b,p);g.DrawPath(edge,p);}
        if(Globe){
            float x=Width/2f-9,y=face.Top+face.Height/2-9;
            using(var pen=new Pen(Style.Accent,1.4f)){
                g.DrawEllipse(pen,x,y,18,18);g.DrawEllipse(pen,x+5,y,8,18);
                g.DrawLine(pen,x+1,y+6,x+17,y+6);g.DrawLine(pen,x+1,y+12,x+17,y+12);
            }
        }else if(Symbol!=null){
            var area=Rectangle.Round(face);
            using(var iconFont=new Font("Segoe MDL2 Assets",14))
                TextRenderer.DrawText(g,Symbol,iconFont,new Rectangle(area.X,area.Y+3,area.Width,26),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g,Caption,Font,new Rectangle(area.X,area.Y+29,area.Width,20),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
        }else TextRenderer.DrawText(g,Text,Font,Rectangle.Round(face),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
        if(Focused && ShowFocusCues){var r=Rectangle.Round(face);r.Inflate(-5,-4);ControlPaint.DrawFocusRectangle(g,r,ForeColor,BackColor);}
    }
}


