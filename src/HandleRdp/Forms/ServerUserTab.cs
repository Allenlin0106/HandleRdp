using System.Data;
using HandleRdp.Data;
using HandleRdp.Models;

namespace HandleRdp.Forms;

/// <summary>RDP_SERVER_USER 分頁：新增、刪除（強制原因）、批次匯入、查詢、篩選。</summary>
public sealed class ServerUserTab : TabPage
{
    private readonly RdpServerUserRepository _repo = new();
    private readonly FilterableGrid _grid = new();

    public ServerUserTab()
    {
        Text = "RDP_SERVER_USER";
        Controls.Add(_grid);

        _grid.AddToolbarButton("重新查詢", (_, _) => Reload());
        _grid.AddToolbarButton("新增", (_, _) => AddOne());
        _grid.AddToolbarButton("批次匯入(CSV)", (_, _) => ImportCsv());
        _grid.AddToolbarButton("刪除選取(需原因)", (_, _) => DeleteSelected());

        Reload();
    }

    private void Reload() => Ui.Guard(() => _grid.Bind(_repo.Query()));

    private void AddOne() => Ui.Guard(() =>
    {
        var fields = new List<Field>
        {
            new("Hostname", "Hostname", required: true),
            new("User_ID", "User_ID", required: true),
            new("Employee_ID", "Employee_ID", required: true),
            new("Login_Time", "Login_Time"),
            new("Logout_Time", "Logout_Time"),
            new("Create_User", "Create_User", Environment.UserName),
            new("Claim_Time", "Claim_Time"),
        };
        using var dlg = new FieldDialog("新增 RDP_SERVER_USER", fields);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _repo.Add(new RdpServerUser
        {
            Hostname = dlg.Get("Hostname"),
            User_ID = dlg.Get("User_ID"),
            Employee_ID = dlg.Get("Employee_ID"),
            Login_Time = Ui.ParseDate(dlg.Get("Login_Time")),
            Logout_Time = Ui.ParseDate(dlg.Get("Logout_Time")),
            Create_User = Ui.NullIfEmpty(dlg.Get("Create_User")),
            Claim_Time = Ui.ParseDate(dlg.Get("Claim_Time")),
        });
        Reload();
        Ui.Info("已新增，並已寫入 RDP_USER_LOG (Action=ADD)。");
    });

    private void ImportCsv() => Ui.Guard(() =>
    {
        var path = Ui.PickCsv();
        if (path == null) return;

        var users = Csv.Read(path).Select(r => new RdpServerUser
        {
            Hostname = r.GetValueOrDefault("Hostname", ""),
            User_ID = r.GetValueOrDefault("User_ID", ""),
            Employee_ID = r.GetValueOrDefault("Employee_ID", ""),
            Login_Time = Ui.ParseDate(r.GetValueOrDefault("Login_Time", "")),
            Logout_Time = Ui.ParseDate(r.GetValueOrDefault("Logout_Time", "")),
            Create_User = Ui.NullIfEmpty(r.GetValueOrDefault("Create_User", "")),
            Claim_Time = Ui.ParseDate(r.GetValueOrDefault("Claim_Time", "")),
        }).ToList();

        if (users.Count == 0) { Ui.Info("CSV 沒有可匯入的資料列。"); return; }
        if (!Ui.Confirm($"即將批次新增 {users.Count} 筆，並寫入對應日誌，是否繼續？")) return;

        _repo.AddMany(users);
        Reload();
        Ui.Info($"已批次新增 {users.Count} 筆。");
    });

    private void DeleteSelected() => Ui.Guard(() =>
    {
        var rows = _grid.SelectedRows();
        if (rows.Count == 0) { Ui.Info("請先選取要刪除的列。"); return; }

        var keys = rows
            .Select(r => new UserKey(r["Hostname"].ToString() ?? "", r["User_ID"].ToString() ?? ""))
            .ToList();

        // 強制輸入原因（需求 1）
        var fields = new List<Field> { new("Reason", "刪除原因", required: true) };
        using var dlg = new FieldDialog($"刪除 {keys.Count} 筆 — 請輸入原因", fields, width: 480);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _repo.Delete(keys, dlg.Get("Reason"));
        Reload();
        Ui.Info($"已刪除 {keys.Count} 筆，並已寫入 RDP_USER_LOG (Action=DELETE: 原因)。");
    });
}
