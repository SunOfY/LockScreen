using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

public sealed class AppSession {
    public int Pid;
    public long Started;
    public string Name;
    public string Executable;
    public List<long> Windows=new List<long>();
    public override string ToString(){return Name+"  ("+Pid+")";}
}

internal sealed class WindowGuard {
    delegate bool Visitor(IntPtr hwnd, IntPtr param);
    [DllImport("user32.dll")] static extern bool EnumWindows(Visitor visit,IntPtr param);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out int pid);
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr hwnd,int mode);
    readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ManChe","guarded-windows.xml");
    List<AppSession> apps=new List<AppSession>();
    internal bool Active{get{return apps.Count!=0;}}
    static bool Alive(AppSession app){
        try{using(var p=Process.GetProcessById(app.Pid))return p.StartTime.ToUniversalTime().Ticks==app.Started;}
        catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}
    }
    internal WindowGuard(){
        if(File.Exists(path))using(var stream=File.OpenRead(path))
            apps=(List<AppSession>)new XmlSerializer(typeof(List<AppSession>)).Deserialize(stream);
        apps.RemoveAll(a=>string.IsNullOrEmpty(a.Executable) && !Alive(a));
    }
    internal static List<AppSession> Candidates(){
        var result=new List<AppSession>();
        using(var self=Process.GetCurrentProcess()){
            foreach(var proc in Process.GetProcesses())using(proc){
                try{
                    if(proc.Id==self.Id || proc.SessionId!=self.SessionId || proc.MainWindowHandle==IntPtr.Zero)continue;
                    string name=proc.ProcessName;
                    if(new[]{"explorer","Taskmgr","ApplicationFrameHost","ShellExperienceHost","SystemSettings","StartMenuExperienceHost","LockApp","SearchHost","TextInputHost","CredentialUIBroker"}.Contains(name,StringComparer.OrdinalIgnoreCase))continue;
                    string exe=proc.MainModule.FileName;
                    result.Add(new AppSession{Pid=proc.Id,Name=name,Executable=exe,Started=proc.StartTime.ToUniversalTime().Ticks});
                }catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}
            }
        }
        return result.OrderBy(a=>a.Name).ToList();
    }
    void Save(){
        Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid()+".tmp";
        try{using(var output=File.Create(temp))new XmlSerializer(typeof(List<AppSession>)).Serialize(output,apps);
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    internal void Start(List<AppSession> selected){
        var previous=apps;
        apps=new List<AppSession>(apps);
        foreach(var app in selected)if(!apps.Any(a=>a.Pid==app.Pid && a.Started==app.Started))apps.Add(app);
        try{Save();}catch{apps=previous;throw;}
        Sweep(true);
    }
    internal event Action Reopened;
    readonly HashSet<IntPtr> hiding=new HashSet<IntPtr>();
    internal void ObserveShow(IntPtr h){
        if(!Active || !IsWindowVisible(h) || GetAncestor(h,2)!=h)return;
        int pid;GetWindowThreadProcessId(h,out pid);
        var app=apps.FirstOrDefault(a=>a.Pid==pid && Alive(a));
        if(app==null){
            try{using(var proc=Process.GetProcessById(pid)){
                if(proc.SessionId!=currentSession)return;
                string exe=proc.MainModule.FileName;
                if(!apps.Any(a=>string.Equals(a.Executable,exe,StringComparison.OrdinalIgnoreCase)))return;
                app=new AppSession{Pid=pid,Name=proc.ProcessName,Executable=exe,Started=proc.StartTime.ToUniversalTime().Ticks};
                apps.Add(app);
            }}catch(System.ComponentModel.Win32Exception){return;}catch(InvalidOperationException){return;}catch(ArgumentException){return;}
        }
        if(hiding.Contains(h))return;
        if(!app.Windows.Contains(h.ToInt64()))app.Windows.Add(h.ToInt64());
        Save();if(!ShowWindowAsync(h,0))throw new InvalidOperationException(I18n.T("Không thể ẩn cửa sổ. Kiểm tra ứng dụng trước khi rời máy."));
        hiding.Add(h);if(Reopened!=null)Reopened();
    }
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
    static readonly int currentSession=Process.GetCurrentProcess().SessionId;
    internal void ObservedHidden(IntPtr h){hiding.Remove(h);}
    internal void Sweep(bool initial=false){
        if(!Active)return;
        hiding.RemoveWhere(h=>!IsWindowVisible(h));
        bool changed=apps.RemoveAll(a=>string.IsNullOrEmpty(a.Executable) && !Alive(a))>0;
        var targets=apps.Where(Alive).ToDictionary(a=>a.Pid);
        var hide=new List<IntPtr>();
        EnumWindows(delegate(IntPtr h,IntPtr unused){
            if(!IsWindowVisible(h))return true;
            int pid;GetWindowThreadProcessId(h,out pid);AppSession app;
            if(targets.TryGetValue(pid,out app)){
                if(!app.Windows.Contains(h.ToInt64())){app.Windows.Add(h.ToInt64());changed=true;}
                hide.Add(h);
            }return true;
        },IntPtr.Zero);
        if(changed || hide.Count>0)Save(); // Journal handles before hiding, for recovery after a restart.
        foreach(var h in hide){if(!initial && !hiding.Contains(h) && Reopened!=null)Reopened();hiding.Add(h);if(!ShowWindowAsync(h,0))throw new InvalidOperationException(I18n.T("Không thể ẩn cửa sổ. Kiểm tra ứng dụng trước khi rời máy."));
    }
    }
    internal void Restore(){
        foreach(var app in apps.Where(Alive)){
            foreach(long handle in app.Windows){
                IntPtr h=new IntPtr(handle);int owner;GetWindowThreadProcessId(h,out owner);
                if(owner==app.Pid && !IsWindowVisible(h) && !ShowWindowAsync(h,8))
                    throw new InvalidOperationException(I18n.T("Không thể mở LockScreen"));
            }
        }
        if(File.Exists(path))File.Delete(path);apps.Clear();hiding.Clear();
    }
}

partial class Shield {
    readonly WindowGuard guard=new WindowGuard();
    readonly Button appButton=new RaisedButton();
    readonly Timer guardTimer=new Timer{Interval=350};
    void UpdateAppButton(){appButton.Text=I18n.T(guard.Active?"Mở lại ứng dụng":"Khóa ứng dụng");}
    void ConfigureWindows(){
        InstallAppEvents();
        Style.Button(appButton,false);appButton.Height=38;
        guardTimer.Tick+=delegate{try{guard.Sweep();if(!guard.Active){guardTimer.Stop();UpdateAppButton();}}
            catch(Exception ex){guardTimer.Stop();status.Text=ex.Message;tray.ShowBalloonTip(4000,"LockScreen",ex.Message,ToolTipIcon.Warning);}};
        if(guard.Active)guardTimer.Start();
        FormClosed+=delegate{guardTimer.Dispose();};
        appButton.Click+=delegate{
            if(locked || prefs.Hash==null)return;
            if(guard.Active){AuthorizeRestore();return;}
            PickApps();
        };
    }
    bool askingRestore;
    bool AuthorizeRestore(){if(askingRestore || locked)return false;askingRestore=true;try{return RestoreDialog();}finally{askingRestore=false;}}
    bool RestoreDialog(){
        using(var dialog=new Form{Text=I18n.T("Mở lại ứng dụng"),ClientSize=new Size(320,182),Font=Font,TopMost=true,StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            var rows=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown};
            dialog.Controls.Add(rows);var label=new Label{Text=I18n.T("Mật khẩu"),AutoSize=true};rows.Controls.Add(label);
            var input=new TextBox{Width=280,UseSystemPasswordChar=true};rows.Controls.Add(input);
            var open=new Button{Text=I18n.T("Mở bằng mật khẩu"),Width=280,Height=34};
            var bio=new Button{Text=I18n.T("Mở bằng Windows Hello"),Width=280,Height=34};rows.Controls.Add(open);rows.Controls.Add(bio);
            dialog.AcceptButton=open;
            open.Click+=delegate{if(MatchesPassword(input.Text))dialog.DialogResult=DialogResult.OK;else{input.Clear();label.Text=I18n.T("Mật khẩu chưa đúng.");}};
            bio.Click+=async delegate{bio.Enabled=false;try{bool ok=await VerifyHello(dialog.Handle,I18n.T("Mở lại ứng dụng"));if(!dialog.IsDisposed && ok)dialog.DialogResult=DialogResult.OK;}
                catch{if(!dialog.IsDisposed)label.Text=I18n.T("Windows Hello chưa khả dụng. Hãy dùng mật khẩu.");}
                finally{if(!dialog.IsDisposed)bio.Enabled=true;}};
            if(dialog.ShowDialog(this)!=DialogResult.OK)return false;
            guardTimer.Stop();try{guard.Restore();UpdateAppButton();return true;}
            catch(Exception ex){MessageBox.Show(this,ex.Message);return false;}
        }
    }
}



