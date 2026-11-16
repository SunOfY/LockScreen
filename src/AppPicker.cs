using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

partial class Shield {
    static string AppName(string path) {
        try {
            var name=FileVersionInfo.GetVersionInfo(path).FileDescription;
            if(!string.IsNullOrWhiteSpace(name))return name.Trim();
        }catch(IOException){}catch(UnauthorizedAccessException){}
        return Path.GetFileNameWithoutExtension(path);
    }

    void LockSelectedApps() {
        var paths=new HashSet<string>(prefs.SelectedApps??new string[0],StringComparer.OrdinalIgnoreCase);
        if(paths.Count==0)return;
        var targets=WindowGuard.Candidates().Where(a=>paths.Contains(a.Executable)).ToList();
        if(targets.Count>0)guard.Start(targets);
        if(guard.Active)guardTimer.Start();
        UpdateAppButton();
    }

    void PickApps() {
        var saved=new HashSet<string>(prefs.SelectedApps??new string[0],StringComparer.OrdinalIgnoreCase);
        var paths=new HashSet<string>(WindowGuard.Candidates().Select(a=>a.Executable),StringComparer.OrdinalIgnoreCase);
        paths.UnionWith(saved);
        using(var dialog=new Form{Text=I18n.T("Chọn ứng dụng đang mở"),StartPosition=FormStartPosition.CenterParent,
            FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font,BackColor=Style.Canvas})
        using(var hint=new ToolTip()){
            var heading=new Label{Font=new Font(Font.FontFamily,14,FontStyle.Bold),ForeColor=Style.Ink,Dock=DockStyle.Top,
                Height=47,Padding=new Padding(12,10,0,0)};
            var grid=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,AutoScroll=true,Padding=new Padding(12,4,0,0)};
            int columns=Math.Min(4,Math.Max(3,paths.Count));
            int rows=Math.Max(1,(paths.Count+columns-1)/columns);
            int visibleRows=Math.Max(1,Math.Min(rows,(Screen.FromControl(this).WorkingArea.Height-175)/90));
            dialog.ClientSize=new Size(16+columns*116+(rows>visibleRows?SystemInformation.VerticalScrollBarWidth:0),47+4+visibleRows*90+52);
            Action count=delegate{heading.Text=I18n.T("Ứng dụng")+"   "+grid.Controls.OfType<AppCard>().Count(c=>c.Checked)+" / "+paths.Count;};
            foreach(var item in paths.Select(path=>new{Path=path,Name=AppName(path)}).OrderBy(x=>x.Name)){
                var card=new AppCard(item.Path,item.Name,saved.Contains(item.Path));
                hint.SetToolTip(card,item.Name+Environment.NewLine+item.Path);
                card.CheckedChanged+=delegate{count();card.Invalidate();};
                grid.Controls.Add(card);
            }
            if(paths.Count==0)grid.Controls.Add(new Label{Text=I18n.T("Không có ứng dụng phù hợp."),AutoSize=true});
            count();
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,FlowDirection=FlowDirection.RightToLeft,Height=52,Padding=new Padding(4,3,10,4)};
            var save=new Button{Text=I18n.T("Lưu lựa chọn"),AutoSize=true,Height=36};
            var now=new Button{Text=I18n.T("Lưu và khóa"),AutoSize=true,Height=36};
            Style.Button(save,false);Style.Button(now,true);
            footer.Controls.Add(now);footer.Controls.Add(save);
            dialog.Controls.Add(grid);dialog.Controls.Add(heading);dialog.Controls.Add(footer);
            Action<bool> commit=delegate(bool lockNow){
                try{
                    var updated=prefs.Copy();
                    updated.SelectedApps=grid.Controls.OfType<AppCard>().Where(row=>row.Checked).Select(row=>(string)row.Tag).ToArray();
                    updated.Save();prefs=updated;
                    if(lockNow)LockSelectedApps();
                    dialog.DialogResult=DialogResult.OK;
                }catch(Exception ex){MessageBox.Show(dialog,ex.Message);}
            };
            save.Click+=delegate{commit(false);};now.Click+=delegate{commit(true);};
            dialog.ShowDialog(this);
        }
    }
}

