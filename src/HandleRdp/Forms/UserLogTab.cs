using System.Windows.Forms;
using HandleRdp.Data;

namespace HandleRdp.Forms
{
    /// <summary>RDP_USER_LOG 分頁：僅查詢與篩選（此表唯讀）。</summary>
    public sealed class UserLogTab : TabPage
    {
        private readonly RdpUserLogRepository _repo = new RdpUserLogRepository();
        private readonly FilterableGrid _grid = new FilterableGrid();

        public UserLogTab()
        {
            Text = "RDP_USER_LOG";
            Controls.Add(_grid);

            _grid.AddToolbarButton("查詢", (sender, e) => Reload());

            // 啟動時不自動查詢，待使用者按「查詢」才向資料庫查詢。
        }

        private void Reload()
        {
            Ui.Guard(() => _grid.Bind(_repo.Query()));
        }
    }
}
