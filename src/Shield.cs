using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.Security.Credentials.UI;

using Microsoft.Win32;

[ComImport, Guid("39E050C3-4E74-441A-8DC0-B81104DF949C"), InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
interface IHello {
    [return: MarshalAs(UnmanagedType.Interface)]
    Windows.Foundation.IAsyncOperation<UserConsentVerificationResult> Verify(IntPtr window, [MarshalAs(UnmanagedType.HString)] string message, ref Guid iid);
}
partial class Shield : Form, IMessageFilter {
    Preferences prefs=Preferences.Load();
    Button settings=new RaisedButton(); Label passLabel=new Label(), repeatLabel=new Label();
    BrandHeader brand = new BrandHeader();
    FlowLayoutPanel tools = new FlowLayoutPanel();
    bool hotkey;
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);hotkey=RegisterHotKey(Handle,shortcutId,0x4000 | prefs.Modifiers,prefs.ShortcutKey);}
    protected override void OnHandleDestroyed(EventArgs e){if(hotkey)UnregisterHotKey(Handle,shortcutId);base.OnHandleDestroyed(e);}
    protected override void WndProc(ref Message m){if(m.Msg==0x312 && m.WParam.ToInt32()==shortcutId){if(!locked && Enabled){BringBack();Act();}return;}base.WndProc(ref m);}
    Timer idle = new Timer { Interval=15000 };
    public bool PreFilterMessage(ref Message m){
        if(!locked)return false;
        bool key=m.Msg==0x100 || m.Msg==0x104;
        bool click=m.Msg==0x201 || m.Msg==0x204 || m.Msg==0x207;
        if(!key && !click)return false;
        idle.Stop();idle.Start();
        if(panel.Visible)return false;
        panel.Visible=true;Activate();pass.Focus();return true;
    }
    [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    TextBox pass = new TextBox(), repeat = new TextBox();
    Button action = new RaisedButton(), hello = new RaisedButton(), album = new RaisedButton();
    CheckBox photo = new CheckBox();
    Label title = new Label(), status = new Label();
    FlowLayoutPanel panel = new FlowLayoutPanel();
    List<Form> covers = new List<Form>();
    bool locked, capturing, verifying;
    byte[] salt, hash;
    int epoch;
    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ManChe","Anh");
    [STAThread] static void Main() { SetProcessDPIAware(); Application.EnableVisualStyles(); bool first;using(var mutex=new System.Threading.Mutex(true,@"Local\ManChe_Privacy",out first)){if(!first){MessageBox.Show(I18n.T("LockScreen đang chạy."));return;}try{Application.Run(new Shield());}catch(Exception ex){MessageBox.Show(ex.Message,I18n.T("Không thể mở LockScreen"));}} }
    Shield() {
        I18n.Language=prefs.Language;folder=prefs.Folder;salt=prefs.Salt;hash=prefs.Hash;Text="LockScreen"; ClientSize=new Size(402,430); MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedSingle;
        StartPosition=FormStartPosition.CenterScreen; BackColor=Style.Canvas; ForeColor=Style.Ink;
        Font=new Font("Segoe UI",11); panel.AutoSize=true;panel.AutoSizeMode=AutoSizeMode.GrowAndShrink;panel.SizeChanged+=delegate{CenterPanel();}; panel.FlowDirection=FlowDirection.TopDown; panel.WrapContents=false;
        Controls.Add(panel); Resize+=delegate { CenterPanel(); }; CenterPanel();
        brand.Size=new Size(372,74);panel.Controls.Add(brand);title.Font=new Font(Font.FontFamily,19,FontStyle.Bold); title.AutoSize=true; panel.Controls.Add(title);
        pass.UseSystemPasswordChar=repeat.UseSystemPasswordChar=true;
        foreach(Control c in new Control[]{passLabel,pass,repeatLabel,repeat,reveal,action,hello,tools,status}) {c.Width=362; c.Margin=new Padding(4,5,4,5); panel.Controls.Add(c);}
        photo.Text=I18n.T("Chụp ảnh khi nhập sai mật khẩu"); photo.Height=32; photo.Checked=prefs.Photo; passLabel.Text=I18n.T("Mật khẩu"); repeatLabel.Text=I18n.T("Nhập lại mật khẩu"); passLabel.Height=repeatLabel.Height=23;settings.Text=I18n.T("Cài đặt ảnh và email");settings.Click+=delegate{using(var dialog=new Options(prefs)){if(dialog.ShowDialog(this)==DialogResult.OK){prefs=dialog.Result;folder=prefs.Folder;photo.Checked=prefs.Photo;Setup();}}};
        tools.Height=39;tools.WrapContents=false;tools.Margin=new Padding(4,1,4,6);
        foreach(var b in new[]{settings,album}){
            Style.Button(b,false);b.Size=new Size(177,39);b.Margin=new Padding(0,0,8,0);tools.Controls.Add(b);
        }
        Style.Button(action,true);Style.Button(hello,false);action.Height=46;hello.Height=40;
        settings.Text=I18n.T("Cài đặt ảnh và email");album.Text=I18n.T("Thư mục ảnh");hello.Text=I18n.T("Mở bằng Windows Hello");
        status.AutoSize=true;status.MinimumSize=new Size(362,18);status.MaximumSize=new Size(362,0);status.ForeColor=Color.FromArgb(90,83,106);status.Font=new Font("Segoe UI",10);
        album.Click+=delegate {try {Directory.CreateDirectory(folder); System.Diagnostics.Process.Start("explorer.exe","\""+folder+"\"");}catch(Exception e){status.Text=e.Message;}};
        action.Click+=delegate { Act(); }; hello.Click+=delegate { Verify(); }; AcceptButton=action;
        ConfigureWindows();ConfigurePasswords();ConfigureTray();ConfigureLanguage();ConfigureToolbar();status.SizeChanged+=delegate{if(Visible && !locked)FitWindow();};
        Application.AddMessageFilter(this);
        idle.Tick+=delegate { idle.Stop(); if(locked && !verifying){reveal.Checked=false;pass.Clear(); panel.Visible=false;} };
        FormClosed+=delegate {Application.RemoveMessageFilter(this);idle.Dispose();};
        SystemEvents.DisplaySettingsChanged+=Displays; FormClosed+=delegate {SystemEvents.DisplaySettingsChanged-=Displays;};
        Setup();
    }
    protected override void OnShown(EventArgs e){base.OnShown(e);Setup();if(Array.IndexOf(Environment.GetCommandLineArgs(),"--tray")>=0 && prefs.Hash!=null)BeginInvoke((Action)Hide);}
    void FitWindow(){if(!locked && WindowState==FormWindowState.Normal)ClientSize=new Size(panel.PreferredSize.Width+24,panel.PreferredSize.Height+24);}
    void CenterPanel(){panel.Location=new Point(Math.Max(0,(ClientSize.Width-panel.Width)/2),Math.Max(0,(ClientSize.Height-panel.Height)/2));}
    void Setup(){action.Visible=false;AcceptButton=lockTool;appButton.Visible=prefs.Hash!=null;UpdateAppButton();reveal.Checked=false;reveal.Visible=prefs.Hash==null;changePassword.Visible=prefs.Hash!=null;changePassword.Text=I18n.T("Đổi mật khẩu");reveal.Text=I18n.T("Hiện mật khẩu");languageButton.Visible=desktopOptions.Visible=brand.Visible=tools.Visible=true;ForeColor=Style.Ink;status.ForeColor=Color.FromArgb(90,83,106);settings.Visible=album.Visible=true; passLabel.Visible=pass.Visible=repeatLabel.Visible=repeat.Visible=prefs.Hash==null;title.Visible=prefs.Hash==null;title.Text=prefs.Hash==null?I18n.T("Thiết lập mật khẩu"):I18n.T("Quyền riêng tư"); action.Text=I18n.T("Khóa ngay"); status.Text=(prefs.Hash==null?I18n.T("Đặt mật khẩu từ 8 ký tự cho lần đầu. "):"")+(hotkey?I18n.T("Phím tắt: ")+ShortcutName(prefs.Modifiers,prefs.ShortcutKey):I18n.T("Phím tắt chưa khả dụng. Dùng nút Bật chế độ riêng tư.")); hello.Visible=false; pass.Clear(); repeat.Clear(); pass.Focus();RefreshTiles();panel.PerformLayout();FitWindow();}
    byte[] Derive(string input){return HashPassword(input,salt);}
    static byte[] HashPassword(string input,byte[] passwordSalt){using(var k=new Rfc2898DeriveBytes(input,passwordSalt,120000,HashAlgorithmName.SHA256))return k.GetBytes(32);}
    void Act(){
        if(!locked){
            if(prefs.Hash==null){if(pass.Text.Length<8 || pass.Text!=repeat.Text){status.Text=I18n.T("Hai mật khẩu cần giống nhau và dài ít nhất 8 ký tự.");return;}
            try{StorePassword(pass.Text);}catch(Exception ex){status.Text=ex.Message;return;}}
            try{LockSelectedApps();}catch(Exception ex){status.Text=ex.Message;MessageBox.Show(this,ex.Message);return;}
            appButton.Visible=false;reveal.Checked=false;reveal.Visible=true;changePassword.Visible=false;
            languageButton.Visible=desktopOptions.Visible=languageButton.Visible=brand.Visible=tools.Visible=false;ForeColor=Color.White;status.ForeColor=Color.Silver;passLabel.Visible=pass.Visible=true;repeatLabel.Visible=settings.Visible=false;
            locked=true;epoch++; pass.Clear();repeat.Clear();repeat.Visible=photo.Visible=album.Visible=false;hello.Visible=true;
            FormBorderStyle=FormBorderStyle.None;TopMost=true;MinimumSize=System.Drawing.Size.Empty;BackColor=Color.Black;panel.Visible=false;BuildCovers();
            action.Visible=true;AcceptButton=action;title.Visible=true;title.Text=I18n.T("Mở khóa LockScreen");action.Text=I18n.T("Mở bằng mật khẩu");status.Text=I18n.T("Nhập mật khẩu để tiếp tục.");
            if(SetThreadExecutionState(0x80000003)==0)status.Text+=I18n.T(" Không giữ được máy thức."); pass.Focus();return;
        }
        bool correct=MatchesPassword(pass.Text);pass.Clear();reveal.Checked=false;
        if(correct)Unlock();else{status.Text=I18n.T("Mật khẩu chưa đúng.");if(photo.Checked && !capturing)Snapshot();}
    }
    void Displays(object s,EventArgs e){if(IsHandleCreated && !IsDisposed)BeginInvoke((Action)delegate{if(locked)BuildCovers();});}
    void BuildCovers(){foreach(var f in covers)f.Dispose();covers.Clear();Bounds=Screen.PrimaryScreen.Bounds;
        foreach(var screen in Screen.AllScreens)if(!screen.Primary){var f=new Form{FormBorderStyle=FormBorderStyle.None,BackColor=BackColor,TopMost=true,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Bounds=screen.Bounds}; f.FormClosing+=delegate(object sender,FormClosingEventArgs args){if(locked && args.CloseReason==CloseReason.UserClosing)args.Cancel=true;}; f.Show();covers.Add(f);}Activate();}
    void Unlock(){if(!locked)return;locked=false;epoch++;SetThreadExecutionState(0x80000000);foreach(var f in covers)f.Dispose();covers.Clear();
        idle.Stop();panel.Visible=true;BackColor=Style.Canvas;TopMost=false;FormBorderStyle=FormBorderStyle.Sizable;ClientSize=new Size(402,430);MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedSingle;CenterToScreen();repeat.Visible=photo.Visible=album.Visible=true;Setup();if(mailFailure!=null){status.Text=mailFailure;mailFailure=null;}}
    static async System.Threading.Tasks.Task<bool> VerifyHello(IntPtr owner,string purpose){
        var factory=(IHello)WindowsRuntimeMarshal.GetActivationFactory(typeof(UserConsentVerifier));
        var iid=new Guid("fd596ffd-2318-558f-9dbe-d21df43764a5");
        return await factory.Verify(owner,purpose,ref iid)==UserConsentVerificationResult.Verified;
    }
    async void Verify(){if(verifying)return;verifying=true;int id=epoch;try{
        bool verified=await VerifyHello(Handle,I18n.T("Mở LockScreen"));
        if(locked && id==epoch){if(verified)Unlock();else status.Text=I18n.T("Chưa xác thực. Hãy thử lại hoặc dùng mật khẩu.");}
    }catch(Exception){if(id==epoch)status.Text=I18n.T("Windows Hello chưa khả dụng. Hãy dùng mật khẩu.");}finally{verifying=false;if(locked){idle.Stop();idle.Start();}}}
    string mailFailure;
    async void Deliver(Preferences options,string path){try{await options.Send(path);}catch(Exception ex){mailFailure=I18n.T("Ảnh đã lưu, gửi email thất bại. ")+ex.Message;if(!locked)status.Text=mailFailure;}}
    async void Snapshot(){var options=prefs;string path=Path.Combine(folder,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")+".jpg");capturing=true;int id=epoch;try{using(var camera=new MediaCapture()){
        await camera.InitializeAsync(new MediaCaptureInitializationSettings{StreamingCaptureMode=StreamingCaptureMode.Video});
        if(id!=epoch)return;
        using(var memory=new InMemoryRandomAccessStream()){await camera.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(),memory);
        if(id!=epoch)return;memory.Seek(0);Directory.CreateDirectory(folder);using(var input=memory.AsStreamForRead())using(var output=File.Create(path))await input.CopyToAsync(output);}
    }if(id==epoch)status.Text=I18n.T("Mật khẩu chưa đúng.");if(options.Mail)Deliver(options,path);}catch(Exception){if(id==epoch)status.Text=I18n.T("Mật khẩu chưa đúng. Chụp ảnh thất bại. Kiểm tra webcam và quyền camera.");}finally{capturing=false;}}
}





















