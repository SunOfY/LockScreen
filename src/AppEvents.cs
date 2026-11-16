using System;
using System.Runtime.InteropServices;

partial class Shield {
    delegate void WindowEvent(IntPtr hook,uint kind,IntPtr window,int obj,int child,uint thread,uint time);
    [DllImport("user32.dll")]static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,WindowEvent callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")]static extern bool UnhookWinEvent(IntPtr hook);
    WindowEvent windowEvent;
    bool promptPosted;
    void InstallAppEvents(){
        guard.Reopened+=delegate{
            if(promptPosted || askingRestore || locked || !Enabled || !IsHandleCreated)return;
            promptPosted=true;
            BeginInvoke((Action)delegate{
                promptPosted=false;
                if(guard.Active && !askingRestore && !locked && Enabled){BringBack();AuthorizeRestore();}
            });
        };
        windowEvent=delegate(IntPtr hook,uint kind,IntPtr h,int obj,int child,uint thread,uint time){
            if(h==IntPtr.Zero || obj!=0 || child!=0 || !guard.Active || IsDisposed)return;
            try{if(kind==0x8003)guard.ObservedHidden(h);else guard.ObserveShow(h);}
            catch(Exception ex){status.Text=ex.Message;}
        };
        // Window show/hide notifications in this desktop; the callback runs on our UI thread.
        var subscription=SetWinEventHook(0x8002,0x8003,IntPtr.Zero,windowEvent,0,0,2);
        FormClosed+=delegate{if(subscription!=IntPtr.Zero)UnhookWinEvent(subscription);};
    }
}
