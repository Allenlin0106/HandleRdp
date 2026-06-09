using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HandleRdp.Data;
using HandleRdp.Models;

namespace HandleRdp.Forms
{
    /// <summary>RDP_SERVER_USER 分頁：新增、刪除（強制原因）、批次匯入、查詢、篩選。</summary>
    public sealed class ServerUserTab : TabPage
    {
        private readonly RdpServerUserRepository _repo = new RdpServerUserRepository();
        private readonly FilterableGrid _grid = new FilterableGrid();

        public ServerUserTab()
        {
            Text = "RDP_SERVER_USER";
            _grid.Selectable = true; // 啟用左側勾選欄，供批次刪除逐筆勾選
            Controls.Add(_grid);

            _grid.AddToolbarButton("重新查詢", (sender, e) => Reload());
            _grid.AddToolbarButton("新增", (sender, e) => AddOne());
            _grid.AddToolbarButton("批次匯入(CSV)", (sender, e) => ImportCsv());
            _grid.AddToolbarButton("刪除勾選(逐筆輸入原因)", (sender, e) => DeleteChecked());

            Reload();
        }

        private void Reload()
        {
            Ui.Guard(() => _grid.Bind(_repo.Query()));
        }

        private void AddOne()
        {
            Ui.Guard(() =>
            {
                var fields = new List<Field>
                {
                    new Field("Hostname", "Hostname", required: true),
                    new Field("User_ID", "User_ID", required: true),
                    new Field("Employee_ID", "Employee_ID", required: true),
                    new Field("Login_Time", "Login_Time"),
                    new Field("Logout_Time", "Logout_Time"),
                    new Field("Create_User", "Create_User", System.Environment.UserName),
                    new Field("Claim_Time", "Claim_Time"),
                };
                using (var dlg = new FieldDialog("新增 RDP_SERVER_USER", fields))
                {
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
                }
                Reload();
                Ui.Info("已新增，並已寫入 RDP_USER_LOG (Action=ADD)。");
            });
        }

        private void ImportCsv()
        {
            Ui.Guard(() =>
            {
                var path = Ui.PickCsv();
                if (path == null) return;

                var users = Csv.Read(path).Select(r => new RdpServerUser
                {
                    Hostname = r.Field("Hostname"),
                    User_ID = r.Field("User_ID"),
                    Employee_ID = r.Field("Employee_ID"),
                    Login_Time = Ui.ParseDate(r.Field("Login_Time")),
                    Logout_Time = Ui.ParseDate(r.Field("Logout_Time")),
                    Create_User = Ui.NullIfEmpty(r.Field("Create_User")),
                    Claim_Time = Ui.ParseDate(r.Field("Claim_Time")),
                }).ToList();

                if (users.Count == 0) { Ui.Info("CSV 沒有可匯入的資料列。"); return; }
                if (!Ui.Confirm("即將批次新增 " + users.Count + " 筆，並寫入對應日誌，是否繼續？")) return;

                _repo.AddMany(users);
                Reload();
                Ui.Info("已批次新增 " + users.Count + " 筆。");
            });
        }

        private void DeleteChecked()
        {
            Ui.Guard(() =>
            {
                var rows = _grid.CheckedRows();
                if (rows.Count == 0) { Ui.Info("請先勾選要刪除的列。"); return; }

                var keys = rows
                    .Select(r => new UserKey(r["Hostname"].ToString(), r["User_ID"].ToString()))
                    .ToList();

                // 逐筆強制輸入原因（需求 1）：每筆各一個原因輸入欄。
                var labels = keys.Select(k => "Hostname=" + k.Hostname + ", User_ID=" + k.User_ID).ToList();
                using (var dlg = new BatchReasonDialog("刪除 " + keys.Count + " 筆 — 請逐筆輸入原因", labels))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    // 依勾選順序，把每筆鍵值與其原因配對後逐筆處理。
                    var reasons = dlg.Reasons;
                    var items = new List<KeyValuePair<UserKey, string>>();
                    for (var i = 0; i < keys.Count; i++)
                        items.Add(new KeyValuePair<UserKey, string>(keys[i], reasons[i]));

                    _repo.Delete(items);
                }
                Reload();
                Ui.Info("已刪除 " + keys.Count + " 筆，並已逐筆寫入 RDP_USER_LOG (Action=DELETE: 各自原因)。");
            });
        }
    }
}
