using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class Style
{
    internal static readonly Color Canvas = Color.FromArgb(246,245,251);
    internal static readonly Color Ink = Color.FromArgb(37,33,57);
    internal static readonly Color Accent = Color.FromArgb(79,58,145);

    internal static void Button(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(218,214,230);
        button.BackColor = primary ? Accent : Color.White;
        button.ForeColor = primary ? Color.White : Ink;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(9,2,9,2);
    }
}

internal sealed class BrandHeader : Panel
{
    public BrandHeader()
    {
        DoubleBuffered = true;
        AccessibleName = "LockScreen";
        Margin = new Padding(0,0,0,7);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var sky = new LinearGradientBrush(ClientRectangle, Color.FromArgb(28,23,53), Color.FromArgb(95,72,133), 12f))
            g.FillRectangle(sky, ClientRectangle);
        int x = Width - 85, y = -15;
        for (int i = 4; i >= 0; i--)
        {
            int r = 44 + i * 19;
            using (var pen = new Pen(Color.FromArgb(50,214,192,239)))
                g.DrawEllipse(pen,x-r,y+67-r,2*r,2*r);
        }
        using (var moon = new LinearGradientBrush(new Rectangle(x-43,y+24,86,86), Color.FromArgb(247,224,196), Color.FromArgb(177,140,194), 75f))
            g.FillEllipse(moon,x-43,y+24,86,86);
        using (var name = new Font("Georgia",25,FontStyle.Regular))
        using (var sub = new Font("Segoe UI",10))
        {
            g.DrawString("LockScreen",name,Brushes.White,13,14);

        }
    }
}



