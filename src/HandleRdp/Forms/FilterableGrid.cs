using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace HandleRdp.Forms
{
    /// <summary>
    /// 可重用的「資料表格 + 工具列」控制項（需求 3：類似 Excel 的欄位篩選）。
    /// 三個分頁共用同一份篩選/顯示邏輯。
    ///
    /// 篩選作法（需求：DataGridView 有資料後才篩選）：
    ///   - 資料載入 DataTable 後，點任一欄位的標題即彈出類 Excel 的下拉視窗，
    ///     可對該欄升/降冪排序、並用核取清單勾選要顯示的值。
    ///   - 各欄條件以 DataView.RowFilter（CONVERT 成字串後比對）即時套用在「已查詢的結果」上，
    ///     多欄條件以 AND 合併。
    /// </summary>
    public sealed class FilterableGrid : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly FlowLayoutPanel _toolbar = new FlowLayoutPanel();
        private readonly Label _status = new Label();

        // 每個有套用篩選的欄位 -> 允許顯示的字串值集合（不在字典中表示該欄不篩選）。
        private readonly Dictionary<string, HashSet<string>> _filters =
            new Dictionary<string, HashSet<string>>();
        private string _sortColumn;
        private bool _sortAsc;

        private DataTable _data;

        /// <summary>勾選欄的欄名（綁定到 DataTable 的一個 bool 欄位）。</summary>
        public const string SelectColumn = "選取";

        /// <summary>
        /// 是否在表格左側加上勾選欄，供批次操作逐筆勾選。需在第一次 Bind 之前設定。
        /// </summary>
        public bool Selectable { get; set; }

        public DataGridView Grid { get { return _grid; } }

        public FilterableGrid()
        {
            Dock = DockStyle.Fill;

            // 上方工具列（各分頁自行加入按鈕；內建一個清除篩選鈕）。
            _toolbar.Dock = DockStyle.Top;
            _toolbar.Height = 40;
            _toolbar.Padding = new Padding(4);
            _toolbar.WrapContents = false;
            _toolbar.AutoScroll = true;
            AddToolbarButton("清除篩選", (sender, e) => ClearFilters());

            // 表格
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = true;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            // 點欄位標題 -> 開啟類 Excel 的篩選/排序下拉。
            _grid.ColumnHeaderMouseClick += OnHeaderClick;
            // 讓勾選欄按一下就立即生效（否則要切到別的儲存格才會提交）。
            _grid.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            _status.Dock = DockStyle.Bottom;
            _status.Height = 22;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Padding = new Padding(6, 0, 0, 0);

            Controls.Add(_grid);
            Controls.Add(_toolbar);
            Controls.Add(_status);
        }

        /// <summary>分頁用來加入自己的動作按鈕。</summary>
        public void AddToolbarButton(string text, EventHandler onClick)
        {
            var btn = new Button { Text = text, AutoSize = true, Margin = new Padding(2) };
            btn.Click += onClick;
            _toolbar.Controls.Add(btn);
        }

        /// <summary>綁定查詢結果。每次重新查詢都會清掉舊的篩選與排序（回到全部顯示）。</summary>
        public void Bind(DataTable data)
        {
            // 勾選模式：在最前面加一個 bool 欄，DataGridView 會自動產生成核取方塊欄。
            if (Selectable && !data.Columns.Contains(SelectColumn))
            {
                var col = new DataColumn(SelectColumn, typeof(bool)) { DefaultValue = false };
                data.Columns.Add(col);
                col.SetOrdinal(0);
            }

            _data = data;
            _filters.Clear();
            _sortColumn = null;

            _grid.ReadOnly = !Selectable;
            _grid.DataSource = data;

            foreach (DataGridViewColumn c in _grid.Columns)
            {
                // 排序改由標題下拉處理，停用內建點標題排序。
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (Selectable)
                    c.ReadOnly = c.DataPropertyName != SelectColumn;
            }

            if (Selectable)
            {
                var sc = _grid.Columns[SelectColumn];
                if (sc != null) { sc.Width = 50; sc.Frozen = true; }
            }

            ApplyFilter();
        }

        /// <summary>取得目前已勾選的列（不受篩選影響，直接讀 DataTable）。</summary>
        public List<DataRow> CheckedRows()
        {
            var rows = new List<DataRow>();
            if (_data == null || !_data.Columns.Contains(SelectColumn)) return rows;
            foreach (DataRow r in _data.Rows)
                if (r[SelectColumn] is bool b && b)
                    rows.Add(r);
            return rows;
        }

        /// <summary>取得目前選取列（依目前篩選/排序對應回 DataRow）。</summary>
        public List<DataRow> SelectedRows()
        {
            var rows = new List<DataRow>();
            foreach (DataGridViewRow gridRow in _grid.SelectedRows)
            {
                var drv = gridRow.DataBoundItem as DataRowView;
                if (drv != null)
                    rows.Add(drv.Row);
            }
            return rows;
        }

        /// <summary>清除所有欄位的篩選與排序。</summary>
        public void ClearFilters()
        {
            _filters.Clear();
            _sortColumn = null;
            ApplyFilter();
        }

        private void OnHeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_data == null || e.ColumnIndex < 0) return;

            var name = _grid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(name) || name == SelectColumn) return;

            _filters.TryGetValue(name, out var current);
            using (var popup = new ColumnFilterPopup(DistinctValues(name), current))
            {
                var headerRect = _grid.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                popup.Location = _grid.PointToScreen(new Point(headerRect.Left, headerRect.Bottom));

                if (popup.ShowDialog(this) != DialogResult.OK) return;

                if (popup.Selected == null) _filters.Remove(name);
                else _filters[name] = popup.Selected;

                if (popup.Sort == ColumnFilterPopup.SortChoice.Asc) { _sortColumn = name; _sortAsc = true; }
                else if (popup.Sort == ColumnFilterPopup.SortChoice.Desc) { _sortColumn = name; _sortAsc = false; }

                ApplyFilter();
            }
        }

        private List<string> DistinctValues(string col)
        {
            var set = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DataRow r in _data.Rows)
            {
                var v = r[col];
                set.Add(v == null || v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.CurrentCulture));
            }
            return set.ToList();
        }

        private void ApplyFilter()
        {
            if (_data == null) return;

            var clauses = new List<string>();
            foreach (var kv in _filters)
            {
                if (kv.Value.Count == 0) { clauses.Add("1 = 0"); continue; } // 全部取消 -> 不顯示任何列
                var quoted = string.Join(", ", kv.Value.Select(v => "'" + v.Replace("'", "''") + "'"));
                clauses.Add("ISNULL(CONVERT([" + kv.Key + "], 'System.String'), '') IN (" + quoted + ")");
            }

            _data.DefaultView.RowFilter = string.Join(" AND ", clauses);
            _data.DefaultView.Sort = _sortColumn == null
                ? ""
                : "[" + _sortColumn + "] " + (_sortAsc ? "ASC" : "DESC");

            UpdateHeaderIndicators();
            _status.Text = "顯示 " + _data.DefaultView.Count + " / 共 " + _data.Rows.Count + " 筆";
        }

        /// <summary>在欄位標題標示是否套用篩選（▽）與排序方向（↑/↓）。</summary>
        private void UpdateHeaderIndicators()
        {
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                var name = c.DataPropertyName;
                if (string.IsNullOrEmpty(name)) continue;
                if (name == SelectColumn) { c.HeaderText = SelectColumn; continue; }

                var mark = "";
                if (_filters.ContainsKey(name)) mark += " ▽";
                if (_sortColumn == name) mark += _sortAsc ? " ↑" : " ↓";
                c.HeaderText = name + mark;
            }
        }
    }
}
