using System.Drawing;
using System.Windows.Forms;

partial class Shield {
    readonly RaisedButton lockTool=new RaisedButton();
    void ConfigureToolbar(){
        Style.Button(lockTool,true);
        lockTool.Click+=delegate{Act();};
        tools.WrapContents=true;tools.Size=new Size(372,116);tools.Margin=new Padding(0,1,0,5);
        var buttons=new Button[]{lockTool,settings,album,desktopOptions,changePassword,appButton};
        string[] symbols={"\uE72E","\uE715","\uE838","\uE765","\uE8D7","\uE71D"};
        tools.Controls.Clear();
        for(int i=0;i<buttons.Length;i++){
            var tile=(RaisedButton)buttons[i];
            tile.Symbol=symbols[i];tile.Font=new Font("Segoe UI",9,FontStyle.Bold);
            tile.Size=new Size(120,55);tile.Margin=new Padding(0,0,i%3==2?0:6,i<3?6:0);
            tile.TextChanged+=delegate{languageHint.SetToolTip(tile,tile.Text);tile.AccessibleName=tile.Text;};
            tools.Controls.Add(tile);
        }
        RefreshTiles();
    }
    void RefreshTiles(){
        lockTool.Text=I18n.T("Khóa ngay");
        var tiles=new Button[]{lockTool,settings,album,desktopOptions,changePassword,appButton};
        string[] keys={"Khóa","Email","Thư mục","Phím tắt","Mật khẩu","Ứng dụng"};
        for(int i=0;i<tiles.Length;i++){
            var tile=(RaisedButton)tiles[i];
            tile.Caption=I18n.T(keys[i]);tile.AccessibleName=tile.Text;
            languageHint.SetToolTip(tile,tile.Text);tile.Invalidate();
        }
    }
}

