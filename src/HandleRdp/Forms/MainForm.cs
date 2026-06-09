using System.Windows.Forms;

namespace HandleRdp.Forms
{
    /// <summary>主視窗：三個資料表各一個分頁。</summary>
    public sealed class MainForm : Form
    {
        public MainForm()
        {
            Text = "HandleRdp — RDP 伺服器與使用者管理";
            Width = 1100;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(new ServerUserTab());
            tabs.TabPages.Add(new ServerInfoTab());
            tabs.TabPages.Add(new UserLogTab());

            Controls.Add(tabs);
        }
    }
}
