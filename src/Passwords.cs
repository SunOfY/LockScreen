using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;

partial class Shield
{
    readonly CheckBox reveal=new CheckBox();
    readonly Button changePassword=new RaisedButton();

    void ConfigurePasswords()
    {
        Style.Button(changePassword,false);
        changePassword.Height=38;
        changePassword.Click+=delegate{if(!locked && prefs.Hash!=null)EditPassword();};
        reveal.Height=25;
        reveal.CheckedChanged+=delegate{pass.UseSystemPasswordChar=repeat.UseSystemPasswordChar=!reveal.Checked;};
    }

    bool MatchesPassword(string value)
    {
        var candidate=Derive(value);
        int difference=0;
        for(int i=0;i<candidate.Length;i++)difference|=candidate[i]^hash[i];
        Array.Clear(candidate,0,candidate.Length);
        return difference==0;
    }

    void StorePassword(string value)
    {
        var next=prefs.Copy();
        next.Salt=new byte[16];
        using(var random=RandomNumberGenerator.Create())random.GetBytes(next.Salt);
        next.Hash=HashPassword(value,next.Salt);
        next.Save();
        prefs=next;salt=next.Salt;hash=next.Hash;
    }

    void EditPassword()
    {
        using(var dialog=new Form{Text=I18n.T("Đổi mật khẩu"),Font=Font,BackColor=Style.Canvas,
            ForeColor=Style.Ink,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,
            AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MaximizeBox=false,MinimizeBox=false})
        {
            var rows=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,
                WrapContents=false,Padding=new Padding(16)};
            dialog.Controls.Add(rows);
            var fields=new TextBox[3];
            string[] captions={"Mật khẩu hiện tại","Mật khẩu mới","Xác nhận mật khẩu mới"};
            for(int i=0;i<fields.Length;i++){
                rows.Controls.Add(new Label{Text=I18n.T(captions[i]),AutoSize=true});
                fields[i]=new TextBox{AccessibleName=I18n.T(captions[i]),Width=320,UseSystemPasswordChar=true};
                rows.Controls.Add(fields[i]);
            }
            var show=new CheckBox{Text=I18n.T("Hiện mật khẩu"),AutoSize=true};
            rows.Controls.Add(show);
            show.CheckedChanged+=delegate{foreach(var box in fields)box.UseSystemPasswordChar=!show.Checked;};
            var error=new Label{AutoSize=true,MaximumSize=new Size(320,0),ForeColor=Color.Maroon};
            rows.Controls.Add(error);
            var save=new RaisedButton{Text=I18n.T("Đổi mật khẩu"),Size=new Size(320,40)};
            Style.Button(save,true);rows.Controls.Add(save);dialog.AcceptButton=save;
            bool recovered=false;
            var forgot=new LinkLabel{Text=I18n.T("Quên mật khẩu?"),AutoSize=true,TabStop=true};
            rows.Controls.Add(forgot);
            forgot.LinkClicked+=async delegate{
                forgot.Enabled=save.Enabled=false;
                try{
                    bool verified=await VerifyHello(dialog.Handle,I18n.T("Đặt lại mật khẩu LockScreen"));
                    if(dialog.IsDisposed)return;
                    if(!verified){error.Text=I18n.T("Chưa xác thực. Hãy thử lại hoặc dùng mật khẩu.");return;}
                    recovered=true;
                    fields[0].Clear();fields[0].Enabled=false;forgot.Visible=false;
                    error.Text=I18n.T("Đã xác minh. Hãy nhập mật khẩu mới.");fields[1].Focus();
                }catch(Exception){if(!dialog.IsDisposed)error.Text=I18n.T("Windows Hello chưa khả dụng. Hãy dùng mật khẩu.");}
                finally{if(!dialog.IsDisposed){forgot.Enabled=save.Enabled=true;}}
            };
            save.Click+=delegate{
                if(!recovered && !MatchesPassword(fields[0].Text)){error.Text=I18n.T("Mật khẩu chưa đúng.");fields[0].SelectAll();fields[0].Focus();return;}
                if(fields[1].Text.Length<8 || fields[1].Text!=fields[2].Text){error.Text=I18n.T("Hai mật khẩu cần giống nhau và dài ít nhất 8 ký tự.");return;}
                try{StorePassword(fields[1].Text);}catch(Exception ex){error.Text=ex.Message;return;}
                foreach(var box in fields)box.Clear();
                dialog.DialogResult=DialogResult.OK;
            };
            if(dialog.ShowDialog(this)==DialogResult.OK)status.Text=I18n.T("Đã ghi nhớ mật khẩu mới.");
        }
    }
}

