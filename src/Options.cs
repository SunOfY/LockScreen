using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Threading.Tasks;

public class Preferences {
    public byte[] Salt, Hash;
    public string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ManChe","Anh");
    public bool Photo=true, Mail;
    public string Host="", From="", To="", User="", Secret="";
    public string GoogleId="", GoogleSecret="", Refresh="";
    public bool UseGoogle;
    public string Language="en";
    public string[] SelectedApps=new string[0];
    public int Port=587;
    public uint Modifiers=3, ShortcutKey=(uint)Keys.K;
    static string FileName {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ManChe","settings.dat");}}
    public static Preferences Load(){
        if(!File.Exists(FileName))return new Preferences();
        var bytes=ProtectedData.Unprotect(File.ReadAllBytes(FileName),null,DataProtectionScope.CurrentUser);
        using(var s=new MemoryStream(bytes))return (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(s);
    }
    public void Save(){
        Directory.CreateDirectory(Path.GetDirectoryName(FileName));string tmp=FileName+"."+Guid.NewGuid()+".tmp";
        try{using(var s=new MemoryStream()){
            new XmlSerializer(typeof(Preferences)).Serialize(s,this);
            File.WriteAllBytes(tmp,ProtectedData.Protect(s.ToArray(),null,DataProtectionScope.CurrentUser));
        }if(File.Exists(FileName))File.Replace(tmp,FileName,null);else File.Move(tmp,FileName);}
        finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
    public Preferences Copy(){return (Preferences)MemberwiseClone();}
    public async Task Send(string path){
        if(UseGoogle){await Gmail.Send(this,path);return;}
        using(var msg=new MailMessage(From,To))
        using(var client=new SmtpClient(Host,Port)){
            msg.Subject=I18n.T("LockScreen • Có lần nhập sai mật khẩu");
            msg.Body=path==null?I18n.T("Thư thử từ LockScreen."):I18n.T("Ảnh đính kèm được chụp trên ")+Environment.MachineName+I18n.T(" lúc ")+File.GetLastWriteTime(path).ToString("g")+".";
            if(path!=null)msg.Attachments.Add(new Attachment(path));else msg.Subject=I18n.T("LockScreen • Thư thử");client.EnableSsl=true;
            client.Credentials=new NetworkCredential(User,Secret);await client.SendMailAsync(msg);
        }
    }
}
class Options : Form {
    FlowLayoutPanel rows=new FlowLayoutPanel();
    TextBox folder,host,from,to,user,secret,port;
    CheckBox photo,mail;
    public Preferences Result;
    void Add(Control c){if(c is Button)Style.Button((Button)c,false);c.Margin=new Padding(0,2,0,5);rows.Controls.Add(c);}
    TextBox Field(string caption,string value){
        Add(new Label{Text=caption,AutoSize=true});var t=new TextBox{Text=value,Width=400};Add(t);return t;
    }
    public Options(Preferences p){
        Text=I18n.T("LockScreen · Cài đặt");BackColor=Style.Canvas;ForeColor=Style.Ink;Font=new Font("Segoe UI",10);ClientSize=new Size(515,610);MinimumSize=new Size(470,400);StartPosition=FormStartPosition.CenterParent;
        rows.Dock=DockStyle.Fill;rows.FlowDirection=FlowDirection.TopDown;rows.WrapContents=false;rows.AutoScroll=true;rows.Padding=new Padding(22,12,12,12);Controls.Add(rows);
        Add(new Label{Text=I18n.T("Ảnh và thông báo"),AutoSize=true,Font=new Font("Segoe UI",21,FontStyle.Bold)});photo=new CheckBox{Text=I18n.T("Chụp ảnh khi nhập sai mật khẩu"),Checked=p.Photo,AutoSize=true};Add(photo);
        folder=Field(I18n.T("Thư mục lưu ảnh"),p.Folder);folder.ReadOnly=true;
        var browse=new Button{Text=I18n.T("Chọn thư mục…"),AutoSize=true};Add(browse);
        browse.Click+=delegate{using(var d=new FolderBrowserDialog{SelectedPath=folder.Text})if(d.ShowDialog(this)==DialogResult.OK)folder.Text=d.SelectedPath;};
        mail=new CheckBox{Text=I18n.T("Gửi ảnh qua email sau khi lưu"),AutoSize=true,Checked=p.Mail};Add(mail);
        var draft=p.Copy();
        var provider=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=400};provider.Items.AddRange(new object[]{I18n.T("Gmail • Đăng nhập bằng Google"),"SMTP",I18n.T("Gmail • Mật khẩu ứng dụng")});provider.SelectedIndex=p.UseGoogle?0:(string.IsNullOrEmpty(p.Host)||p.Host=="smtp.gmail.com"?2:1);Add(provider);
        to=Field(I18n.T("Email nhận ảnh (có thể khác tài khoản gửi)"),p.To);
        int simpleStart=rows.Controls.Count;
        bool savedGmail=p.Host=="smtp.gmail.com";
        var senderBox=Field(I18n.T("Tài khoản Gmail gửi thư"),savedGmail?p.From:"");
        var keyBox=Field(I18n.T("Mật khẩu ứng dụng Google (16 ký tự)"),savedGmail?p.Secret:"");keyBox.UseSystemPasswordChar=true;
        const string googleUrl="https://myaccount.google.com/apppasswords";
        var open=new Button{Text=I18n.T("Tạo mật khẩu ứng dụng trên Google…"),AutoSize=true};Add(open);
        open.Click+=delegate{try{foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}){string browser=Path.Combine(root,@"CocCoc\Browser\Application\browser.exe");if(File.Exists(browser)){System.Diagnostics.Process.Start(browser,googleUrl);return;}}System.Diagnostics.Process.Start(googleUrl);}catch(Exception ex){MessageBox.Show(this,ex.Message);}};
        Action<Preferences> simple=delegate(Preferences v){v.UseGoogle=false;v.Host="smtp.gmail.com";v.Port=587;v.User=v.From=new MailAddress(senderBox.Text.Trim()).Address;v.Secret=keyBox.Text.Replace(" ","").Trim();if(v.Secret.Length!=16)throw new Exception(I18n.T("Nhập mật khẩu ứng dụng Google gồm 16 ký tự."));};
        int simpleEnd=rows.Controls.Count;
        var login=new Button{Text=string.IsNullOrEmpty(p.Refresh)?I18n.T("Đăng nhập bằng Google"):I18n.T("Đổi tài khoản Google"),AutoSize=true};Add(login);
        var test=new Button{Text=I18n.T("Gửi thư thử đến địa chỉ trên"),AutoSize=true};Add(test);
        var cancel=new System.Threading.CancellationTokenSource();FormClosed+=delegate{cancel.Cancel();};
        login.Click+=async delegate{try{
            var next=draft.Copy();
            if(string.IsNullOrEmpty(next.GoogleId)){MessageBox.Show(this,I18n.T("Lần đầu cần chọn tệp JSON OAuth Desktop app của dự án đã bật Gmail API."));using(var d=new OpenFileDialog{Filter="Google OAuth Desktop (*.json)|*.json"}){if(d.ShowDialog(this)!=DialogResult.OK)return;Gmail.Import(next,d.FileName);}}
            Enabled=false;await Gmail.Login(next,cancel.Token);if(IsDisposed)return;draft=next;login.Text=I18n.T("Đổi tài khoản Google");MessageBox.Show(this,I18n.T("Đã kết nối. Nhập email nhận rồi lưu cài đặt."));
        }catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message);}finally{if(!IsDisposed)Enabled=true;}};
        test.Click+=async delegate{try{var temp=draft.Copy();temp.To=new MailAddress(to.Text.Trim()).Address;temp.UseGoogle=provider.SelectedIndex==0;if(provider.SelectedIndex==2)simple(temp);if(temp.UseGoogle && string.IsNullOrEmpty(temp.Refresh))throw new Exception(I18n.T("Hãy đăng nhập Google trước."));test.Enabled=false;await temp.Send(null);if(!IsDisposed)MessageBox.Show(this,I18n.T("Gmail đã nhận thư để gửi."));}catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message);}finally{if(!IsDisposed)test.Enabled=true;}};
        int smtpStart=rows.Controls.Count;
        host=Field(I18n.T("Máy chủ SMTP hỗ trợ STARTTLS"),p.Host);port=Field(I18n.T("Cổng SMTP"),p.Port.ToString());from=Field(I18n.T("Địa chỉ gửi"),p.From);
        user=Field(I18n.T("Tên đăng nhập SMTP"),p.User);secret=Field(I18n.T("Mật khẩu ứng dụng / mật khẩu SMTP"),p.Secret);secret.UseSystemPasswordChar=true;
        Action toggle=delegate{bool enabled=photo.Checked && mail.Checked;provider.Visible=enabled;to.Visible=enabled;rows.Controls[rows.Controls.IndexOf(to)-1].Visible=enabled;bool google=enabled && provider.SelectedIndex==0;for(int i=smtpStart;i<rows.Controls.Count;i++)rows.Controls[i].Visible=enabled && provider.SelectedIndex==1;for(int i=simpleStart;i<simpleEnd;i++)rows.Controls[i].Visible=enabled && provider.SelectedIndex==2;login.Visible=google;test.Visible=enabled && provider.SelectedIndex!=1;mail.Enabled=photo.Checked;foreach(var t in new[]{host,port,from,to,user,secret})t.Enabled=photo.Checked && mail.Checked;};
        provider.SelectedIndexChanged+=delegate{toggle();};photo.CheckedChanged+=delegate{toggle();};mail.CheckedChanged+=delegate{toggle();};toggle();
        var save=new Button{Text=I18n.T("Lưu cài đặt"),Dock=DockStyle.Bottom,Height=46};Style.Button(save,true);Controls.Add(save);
        save.Click+=delegate{try{
            var updated=draft.Copy();updated.UseGoogle=provider.SelectedIndex==0;updated.Folder=folder.Text;updated.Photo=photo.Checked;updated.Mail=mail.Checked && photo.Checked;
            if(updated.UseGoogle && updated.Mail){if(string.IsNullOrEmpty(updated.Refresh))throw new Exception(I18n.T("Hãy đăng nhập bằng Google."));updated.To=new MailAddress(to.Text.Trim()).Address;}
            if(provider.SelectedIndex==2 && updated.Mail){simple(updated);updated.To=new MailAddress(to.Text.Trim()).Address;}if(provider.SelectedIndex==1 && updated.Mail){int n;if(!int.TryParse(port.Text,out n)||n<1||n>65535||string.IsNullOrWhiteSpace(host.Text)||string.IsNullOrWhiteSpace(user.Text)||secret.Text.Length==0)throw new Exception(I18n.T("Điền đủ thông tin SMTP và cổng hợp lệ."));
                updated.Port=n;updated.Host=host.Text.Trim();updated.To=new MailAddress(to.Text.Trim()).Address;updated.From=new MailAddress(from.Text.Trim()).Address;updated.User=user.Text.Trim();updated.Secret=secret.Text;}
            Directory.CreateDirectory(updated.Folder);updated.Save();Result=updated;DialogResult=DialogResult.OK;
        }catch(Exception ex){MessageBox.Show(this,ex.Message,Text);}};
    }
}








