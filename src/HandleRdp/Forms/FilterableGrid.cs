using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace HandleRdp.Forms;

/// <summary>
/// 可重用的「資料表格 + 篩選列 + 工具列」控制項（需求 3：對查詢結果提供篩選條件）。
/// 三個分頁共用同一份篩選/顯示邏輯，避免重複。
///
/// 篩選作法：資料載入 DataTable 後，使用 DataView.RowFilter 在「已查詢的結果」上即時篩選，
/// 透過 CONVERT(...,'System.String') 讓任意型別欄位都能用文字比對，不必管欄位型別。
/// </summary>
public sealed class FilterableGrid : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly FlowLayoutPanel _toolbar = new();
    private readonly ComboBox _column = new();
    private readonly ComboBox _op = new();
    private readonly TextBox _value = new();
    private readonly Label _status = new();

    private DataTable? _data;

    /// <summary>勾選欄的欄名（綁定到 DataTable 的一個 bool 欄位）。</summary>
    public const string SelectColumn = "選取";

    /// <summary>
    /// 是否在表格左側加上勾選欄，供批次操作逐筆勾選。需在第一次 Bind 之前設定。
    /// </summary>
    public bool Selectable { get; set; }

    public DataGridView Grid => _grid;

    public FilterableGrid()
    {
        Dock = DockStyle.Fill;

        // 上方工具列（各分頁自行加入按鈕）
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 40;
        _toolbar.Padding = new Padding(4);
        _toolbar.WrapContents = false;
        _toolbar.AutoScroll = true;

        // 篩選列
        var filterBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(4),
            WrapContents = false,
            AutoScroll = true,
        };
        _column.DropDownStyle = ComboBoxStyle.DropDownList;
        _column.Width = 160;
        _op.DropDownStyle = ComboBoxStyle.DropDownList;
        _op.Width = 90;
        _op.Items.AddRange(new object[] { "包含", "等於", "開頭為" });
        _op.SelectedIndex = 0;
        _value.Width = 200;

        var apply = new Button { Text = "套用篩選", AutoSize = true };
        apply.Click += (_, _) => ApplyFilter();
        var clear = new Button { Text = "清除", AutoSize = true };
        clear.Click += (_, _) => { _value.Text = ""; ApplyFilter(); };
        _value.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { ApplyFilter(); e.SuppressKeyPress = true; } };

        filterBar.Controls.Add(new Label { Text = "篩選欄位：", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        filterBar.Controls.Add(_column);
        filterBar.Controls.Add(_op);
        filterBar.Controls.Add(_value);
        filterBar.Controls.Add(apply);
        filterBar.Controls.Add(clear);

        // 表格
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        // 讓勾選欄按一下就立即生效（否則要切到別的儲存格才會提交）。
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        _status.Dock = DockStyle.Bottom;
        _status.Height = 22;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Padding = new Padding(6, 0, 0, 0);

        Controls.Add(_grid);
        Controls.Add(filterBar);
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

    /// <summary>綁定查詢結果，並重建篩選欄位清單與套用目前篩選。</summary>
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
        _grid.ReadOnly = !Selectable;
        _grid.DataSource = data;

        if (Selectable)
        {
            // 只有勾選欄可編輯，其餘欄位維持唯讀。
            foreach (DataGridViewColumn c in _grid.Columns)
                c.ReadOnly = c.DataPropertyName != SelectColumn;
            var sc = _grid.Columns[SelectColumn];
            if (sc != null) { sc.HeaderText = SelectColumn; sc.Width = 50; sc.Frozen = true; }
        }

        var selected = _column.SelectedItem as string;
        _column.Items.Clear();
        foreach (DataColumn col in data.Columns)
            if (col.ColumnName != SelectColumn)
                _column.Items.Add(col.ColumnName);
        if (selected != null && _column.Items.Contains(selected))
            _column.SelectedItem = selected;
        else if (_column.Items.Count > 0)
            _column.SelectedIndex = 0;

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

    private void ApplyFilter()
    {
        if (_data == null) return;

        var text = _value.Text.Trim();
        if (text.Length == 0 || _column.SelectedItem is not string col)
        {
            _data.DefaultView.RowFilter = "";
        }
        else
        {
            var v = text.Replace("'", "''");
            var expr = $"CONVERT([{col}], 'System.String')";
            _data.DefaultView.RowFilter = _op.SelectedIndex switch
            {
                1 => $"{expr} = '{v}'",
                2 => $"{expr} LIKE '{v}%'",
                _ => $"{expr} LIKE '%{v}%'",
            };
        }

        _status.Text = $"顯示 {_data.DefaultView.Count} / 共 {_data.Rows.Count} 筆";
    }

    /// <summary>取得目前選取列（依目前篩選/排序對應回 DataRow）。</summary>
    public List<DataRow> SelectedRows()
    {
        var rows = new List<DataRow>();
        foreach (DataGridViewRow gridRow in _grid.SelectedRows)
        {
            if (gridRow.DataBoundItem is DataRowView drv)
                rows.Add(drv.Row);
        }
        return rows;
    }
}
