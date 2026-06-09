using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HandleRdp.Data;
using HandleRdp.Models;

namespace HandleRdp.Forms
{
    /// <summary>RDP_SERVER_INFO 分頁：新增、修改、刪除、批次匯入、查詢、篩選。</summary>
    public sealed class ServerInfoTab : TabPage
    {
        private readonly RdpServerInfoRepository _repo = new RdpServerInfoRepository();
        private readonly FilterableGrid _grid = new FilterableGrid();

        public ServerInfoTab()
        {
            Text = "RDP_SERVER_INFO";
            Controls.Add(_grid);

            _grid.AddToolbarButton("查詢", (sender, e) => Reload());
            _grid.AddToolbarButton("新增", (sender, e) => AddOne());
            _grid.AddToolbarButton("修改選取", (sender, e) => EditSelected());
            _grid.AddToolbarButton("批次匯入(CSV)", (sender, e) => ImportCsv());
            _grid.AddToolbarButton("刪除選取", (sender, e) => DeleteSelected());

            // 啟動時不自動查詢，待使用者按「查詢」才向資料庫查詢。
        }

        private void Reload()
        {
            Ui.Guard(() => _grid.Bind(_repo.Query()));
        }

        private static List<Field> BuildFields(RdpServerInfo src, bool hostnameReadOnly)
        {
            return new List<Field>
            {
                new Field("Hostname", "Hostname", src?.Hostname ?? "", readOnly: hostnameReadOnly, required: true),
                new Field("Department", "Department", src?.Department ?? "", required: true),
                new Field("Category", "Category", src?.Category ?? "", required: true),
                new Field("Connectstring", "Connectstring", src?.Connectstring ?? ""),
                new Field("Sponsor", "Sponsor", src?.Sponsor ?? ""),
                new Field("Claim_Time", "Claim_Time", src?.Claim_Time?.ToString() ?? ""),
            };
        }

        private static RdpServerInfo ReadFields(FieldDialog dlg)
        {
            return new RdpServerInfo
            {
                Hostname = dlg.Get("Hostname"),
                Department = dlg.Get("Department"),
                Category = dlg.Get("Category"),
                Connectstring = dlg.Get("Connectstring"),
                Sponsor = Ui.NullIfEmpty(dlg.Get("Sponsor")),
                Claim_Time = Ui.ParseDate(dlg.Get("Claim_Time")),
            };
        }

        private void AddOne()
        {
            Ui.Guard(() =>
            {
                using (var dlg = new FieldDialog("新增 RDP_SERVER_INFO", BuildFields(null, hostnameReadOnly: false)))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    _repo.Add(ReadFields(dlg));
                }
                Reload();
                Ui.Info("已新增。");
            });
        }

        private void EditSelected()
        {
            Ui.Guard(() =>
            {
                var rows = _grid.SelectedRows();
                if (rows.Count != 1) { Ui.Info("請只選取一列進行修改。"); return; }

                var row = rows[0];
                var current = new RdpServerInfo
                {
                    Hostname = row["Hostname"].ToString(),
                    Department = row["Department"].ToString(),
                    Category = row["Category"].ToString(),
                    Connectstring = row["Connectstring"].ToString(),
                    Sponsor = row["Sponsor"] as string,
                    Claim_Time = row["Claim_Time"] as DateTime?,
                };

                // Hostname 為鍵值，修改時鎖定不可改。
                using (var dlg = new FieldDialog("修改 RDP_SERVER_INFO", BuildFields(current, hostnameReadOnly: true)))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    _repo.Update(ReadFields(dlg));
                }
                Reload();
                Ui.Info("已修改。");
            });
        }

        private void ImportCsv()
        {
            Ui.Guard(() =>
            {
                var path = Ui.PickCsv();
                if (path == null) return;

                var items = Csv.Read(path).Select(r => new RdpServerInfo
                {
                    Department = r.Field("Department"),
                    Category = r.Field("Category"),
                    Hostname = r.Field("Hostname"),
                    Connectstring = r.Field("Connectstring"),
                    Sponsor = Ui.NullIfEmpty(r.Field("Sponsor")),
                    Claim_Time = Ui.ParseDate(r.Field("Claim_Time")),
                }).ToList();

                if (items.Count == 0) { Ui.Info("CSV 沒有可匯入的資料列。"); return; }
                if (!Ui.Confirm("即將批次新增 " + items.Count + " 筆，是否繼續？")) return;

                _repo.AddMany(items);
                Reload();
                Ui.Info("已批次新增 " + items.Count + " 筆。");
            });
        }

        private void DeleteSelected()
        {
            Ui.Guard(() =>
            {
                var rows = _grid.SelectedRows();
                if (rows.Count == 0) { Ui.Info("請先選取要刪除的列。"); return; }

                var hostnames = rows.Select(r => r["Hostname"].ToString()).ToList();
                if (!Ui.Confirm("確定刪除 " + hostnames.Count + " 筆 RDP_SERVER_INFO？")) return;

                _repo.Delete(hostnames);
                Reload();
                Ui.Info("已刪除 " + hostnames.Count + " 筆。");
            });
        }
    }
}
