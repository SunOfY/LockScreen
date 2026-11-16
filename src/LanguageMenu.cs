using System;
using System.Drawing;
using System.Windows.Forms;

partial class Shield {
    readonly RaisedButton languageButton=new RaisedButton{Globe=true};
    readonly ToolTip languageHint=new ToolTip();
    void ConfigureLanguage(){
        Style.Button(languageButton,false);
        languageButton.Size=new Size(36,34);
        languageButton.Location=new Point(brand.Width-44,8);
        languageButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        brand.Controls.Add(languageButton);
        var menu=new ContextMenuStrip();
        string[] codes={"en","vi","zh-Hant"};
        string[] names={"English","Tiếng Việt","繁體中文"};
        for(int i=0;i<codes.Length;i++){
            string code=codes[i];var item=menu.Items.Add(names[i]) as ToolStripMenuItem;
            item.Tag=code;
            item.Click+=delegate{
                if(I18n.Language==code)return;
                var next=prefs.Copy();next.Language=code;
                try{next.Save();}catch(Exception ex){MessageBox.Show(this,ex.Message);return;}
                var password=pass.Text;var confirmation=repeat.Text;
                prefs=next;I18n.Language=code;RefreshLanguage();Setup();pass.Text=password;repeat.Text=confirmation;
            };
        }
        menu.Opening+=delegate{foreach(ToolStripMenuItem item in menu.Items)item.Checked=(string)item.Tag==I18n.Language;};
        languageButton.Click+=delegate{menu.Show(languageButton,new Point(0,languageButton.Height));};
        FormClosed+=delegate{menu.Dispose();languageHint.Dispose();};
        RefreshLanguage();
    }
    void RefreshLanguage(){
        languageButton.AccessibleName=I18n.T("Ngôn ngữ");languageHint.SetToolTip(languageButton,languageButton.AccessibleName);
        
        settings.Text=I18n.T("Ảnh và email");album.Text=I18n.T("Thư mục ảnh");
        hello.Text=I18n.T("Mở bằng Windows Hello");
        passLabel.Text=I18n.T("Mật khẩu");repeatLabel.Text=I18n.T("Nhập lại mật khẩu");
        desktopOptions.Text=I18n.T("Phím tắt và khởi động");
        var entries=tray.ContextMenuStrip.Items;
        entries[0].Text=I18n.T("Mở LockScreen");entries[1].Text=I18n.T("Bật chế độ riêng tư");entries[2].Text=I18n.T("Thoát LockScreen");
        brand.Invalidate();
    }
}



