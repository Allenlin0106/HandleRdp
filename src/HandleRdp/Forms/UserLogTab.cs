using HandleRdp.Data;

namespace HandleRdp.Forms;

/// <summary>RDP_USER_LOG 分頁：僅查詢與篩選（此表唯讀）。</summary>
public sealed class UserLogTab : TabPage
{
    private readonly RdpUserLogRepository _repo = new();
    private readonly FilterableGrid _grid = new();

    public UserLogTab()
    {
        Text = "RDP_USER_LOG";
        Controls.Add(_grid);

        _grid.AddToolbarButton("重新查詢", (_, _) => Reload());
        Reload();
    }

    private void Reload() => Ui.Guard(() => _grid.Bind(_repo.Query()));
}
