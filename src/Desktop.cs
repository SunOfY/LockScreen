using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

partial class Shield
{
    readonly Button desktopOptions = new RaisedButton();
    readonly NotifyIcon tray = new NotifyIcon();
    int shortcutId = 412;
    bool exiting;
    const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    static string ShortcutName(uint mods, uint key)
    {
        return ((mods & 2)!=0 ? "Ctrl + " : "") + ((mods & 1)!=0 ? "Alt + " : "") +
               ((mods & 4)!=0 ? "Shift + " : "") + (Keys)key;
    }

    void BringBack()
    {
        if (locked || !Enabled) return;
        WindowState=FormWindowState.Normal;
        Show();
        Activate();
    }

    void ConfigureTray()
    {
        desktopOptions.Text=I18n.T("Phím tắt và khởi động");
        Style.Button(desktopOptions,false);
        desktopOptions.Height=38;
        desktopOptions.Click+=delegate{DesktopSettings();};
        var menu=new ContextMenuStrip();
        menu.Items.Add(I18n.T("Mở LockScreen"),null,delegate{BringBack();});
        menu.Items.Add(I18n.T("Bật chế độ riêng tư"),null,delegate{if(!locked && Enabled){BringBack();Act();}});
        menu.Items.Add(I18n.T("Thoát LockScreen"),null,delegate{if(!locked && Enabled){if(guard.Active && !AuthorizeRestore())return;exiting=true;Close();}});
        menu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=locked || !Enabled;};
        tray.Text="LockScreen";tray.Icon=Icon;tray.ContextMenuStrip=menu;tray.Visible=true;
        tray.DoubleClick+=delegate{BringBack();};
        Resize+=delegate{if(!locked && WindowState==FormWindowState.Minimized)Hide();};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){
            if(e.CloseReason!=CloseReason.UserClosing || exiting)return;
            e.Cancel=true;
            if(!locked){Hide();tray.ShowBalloonTip(2000,"LockScreen",I18n.T("LockScreen đang chạy ở khay hệ thống."),ToolTipIcon.Info);}
        };
        FormClosed+=delegate{tray.Visible=false;tray.Dispose();menu.Dispose();};
    }

    void DesktopSettings()
    {
        string command="\""+Application.ExecutablePath+"\" --tray";
        using(var dialog=new Form{Text=I18n.T("LockScreen · Phím tắt và khởi động"),ClientSize=new Size(440,360),
            FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,
            StartPosition=FormStartPosition.CenterParent,Font=Font,BackColor=Style.Canvas})
        using(var rows=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(20),WrapContents=false})
        {
            dialog.Controls.Add(rows);
            rows.Controls.Add(new Label{Text=I18n.T("Chọn tổ hợp để bật chế độ riêng tư"),AutoSize=true});
            var keys=new FlowLayoutPanel{Width=395,Height=40,WrapContents=false};
            var ctrl=new CheckBox{Text="Ctrl",AutoSize=true,Checked=(prefs.Modifiers&2)!=0};
            var alt=new CheckBox{Text="Alt",AutoSize=true,Checked=(prefs.Modifiers&1)!=0};
            var shift=new CheckBox{Text="Shift",AutoSize=true,Checked=(prefs.Modifiers&4)!=0};
            var key=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=78};
            for(int i=65;i<=90;i++)key.Items.Add((Keys)i);
            for(int i=48;i<=57;i++)key.Items.Add((Keys)i);
            for(int i=(int)Keys.F1;i<=(int)Keys.F11;i++)key.Items.Add((Keys)i);
            key.SelectedItem=(Keys)prefs.ShortcutKey;
            keys.Controls.AddRange(new Control[]{ctrl,alt,shift,key});rows.Controls.Add(keys);
            var startup=new CheckBox{Text=I18n.T("Chạy ngầm khi đăng nhập Windows"),AutoSize=true};
            string registered;
            try{using(var r=Registry.CurrentUser.OpenSubKey(StartupKey))
                registered=r==null?null:(r.GetValue("LockScreen") ?? r.GetValue("Vesper")) as string;}
            catch(Exception ex){MessageBox.Show(this,ex.Message);return;}
            startup.Checked=!string.IsNullOrWhiteSpace(registered);
            rows.Controls.Add(startup);
            if(startup.Checked && !string.Equals(registered,command,StringComparison.OrdinalIgnoreCase))
                rows.Controls.Add(new Label{Text=I18n.T("Đường dẫn khởi động khác bản này. Bấm Lưu để cập nhật."),AutoSize=true,MaximumSize=new Size(390,0)});
            var startupSettings=new LinkLabel{Text=I18n.T("Kiểm tra trong cài đặt khởi động Windows"),AutoSize=true};
            startupSettings.LinkClicked+=delegate{try{System.Diagnostics.Process.Start("ms-settings:startupapps");}catch(Exception ex){MessageBox.Show(dialog,ex.Message);}};
            rows.Controls.Add(startupSettings);
            rows.Controls.Add(new Label{Text=I18n.T("Đóng cửa sổ để ẩn vào khay hệ thống. Nhấp đúp biểu tượng LockScreen để mở lại."),Width=385,Height=56});
            var save=new Button{Text=I18n.T("Lưu tùy chọn"),Width=185,Height=40};
            Style.Button(save,true);rows.Controls.Add(save);
            save.Click+=delegate{
                uint mods=(uint)((ctrl.Checked?2:0)|(shift.Checked?4:0)|(alt.Checked?1:0));
                if(mods==0 || key.SelectedItem==null){MessageBox.Show(dialog,I18n.T("Chọn ít nhất một phím bổ trợ và một phím chính."));return;}
                uint vk=(uint)(Keys)key.SelectedItem;
                bool reserve=mods!=prefs.Modifiers || vk!=prefs.ShortcutKey;
                int next=shortcutId==412?413:412;
                if(reserve && !RegisterHotKey(Handle,next,0x4000|mods,vk)){MessageBox.Show(dialog,I18n.T("Windows không cấp được tổ hợp này. Hãy chọn tổ hợp khác."));return;}
                try{
                    var updated=prefs.Copy();updated.Modifiers=mods;updated.ShortcutKey=vk;
                    using(var r=Registry.CurrentUser.CreateSubKey(StartupKey)){
                        object previous=r.GetValue("LockScreen"), legacy=r.GetValue("Vesper");
                        try {
                            if(startup.Checked)r.SetValue("LockScreen",command);else r.DeleteValue("LockScreen",false);
                            r.DeleteValue("Vesper",false);
                            updated.Save();
                        } catch {
                            if(previous==null)r.DeleteValue("LockScreen",false);else r.SetValue("LockScreen",previous);
                            if(legacy!=null)r.SetValue("Vesper",legacy);
                            throw;
                        }
                    }
                    if(reserve){if(hotkey)UnregisterHotKey(Handle,shortcutId);shortcutId=next;hotkey=true;}
                    prefs=updated;Setup();dialog.Close();
                }catch(Exception ex){if(reserve)UnregisterHotKey(Handle,next);MessageBox.Show(dialog,ex.Message);}
            };
            dialog.ShowDialog(this);
        }
    }
}






