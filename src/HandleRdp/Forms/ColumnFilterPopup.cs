using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HandleRdp.Forms
{
    /// <summary>
    /// 類似 Excel 欄位篩選的下拉視窗：可對該欄位升/降冪排序，並以勾選方式選擇要顯示的值。
    /// 以 ShowDialog 顯示，按「確定」「升冪排序」「降冪排序」會關閉並套用。
    /// </summary>
    public sealed class ColumnFilterPopup : Form
    {
        public enum SortChoice { None, Asc, Desc }

        private const string SelectAllText = "(全選)";
        private const string BlankText = "(空白)";

        private readonly CheckedListBox _list = new CheckedListBox();
        private readonly List<string> _allValues;
        private bool _suppress;

        /// <summary>使用者選擇的排序方式。</summary>
        public SortChoice Sort { get; private set; } = SortChoice.None;

        /// <summary>
        /// 勾選要顯示的字串值集合；若等於全部值則為 null（代表此欄不篩選）。
        /// 空白值以空字串表示。
        /// </summary>
        public HashSet<string> Selected { get; private set; }

        /// <param name="distinctValues">該欄位所有相異值（已轉成字串，空白以空字串表示）。</param>
        /// <param name="currentFilter">目前已套用的值集合；null 代表目前未篩選（全部顯示）。</param>
        public ColumnFilterPopup(IReadOnlyList<string> distinctValues, HashSet<string> currentFilter)
        {
            _allValues = distinctValues.ToList();

            Text = "篩選";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Width = 240;
            Height = 340;

            var sortAsc = new Button { Text = "升冪排序 ↑", Dock = DockStyle.Top, Height = 26 };
            var sortDesc = new Button { Text = "降冪排序 ↓", Dock = DockStyle.Top, Height = 26 };

            _list.Dock = DockStyle.Fill;
            _list.CheckOnClick = true;
            _list.IntegralHeight = false;
            _list.Items.Add(SelectAllText, true);
            foreach (var v in _allValues)
            {
                var isChecked = currentFilter == null || currentFilter.Contains(v);
                _list.Items.Add(new Item(v), isChecked);
            }
            SyncSelectAllState();
            _list.ItemCheck += OnItemCheck;

            var ok = new Button { Text = "確定", Dock = DockStyle.Bottom, Height = 28 };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom, Height = 28 };

            ok.Click += (s, e) => { CommitSelection(); DialogResult = DialogResult.OK; };
            sortAsc.Click += (s, e) => { Sort = SortChoice.Asc; CommitSelection(); DialogResult = DialogResult.OK; };
            sortDesc.Click += (s, e) => { Sort = SortChoice.Desc; CommitSelection(); DialogResult = DialogResult.OK; };

            Controls.Add(_list);
            Controls.Add(ok);
            Controls.Add(cancel);
            Controls.Add(sortDesc);
            Controls.Add(sortAsc);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void OnItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_suppress) return;

            if (e.Index == 0)
            {
                // 切換 (全選)：同步所有值項目。
                var check = e.NewValue == CheckState.Checked;
                _suppress = true;
                for (var i = 1; i < _list.Items.Count; i++)
                    _list.SetItemChecked(i, check);
                _suppress = false;
            }
            else
            {
                // 值項目變更後，再同步 (全選) 狀態（變更套用後才計算）。
                BeginInvoke((Action)SyncSelectAllState);
            }
        }

        private void SyncSelectAllState()
        {
            var all = true;
            for (var i = 1; i < _list.Items.Count; i++)
                if (!_list.GetItemChecked(i)) { all = false; break; }

            _suppress = true;
            _list.SetItemChecked(0, all);
            _suppress = false;
        }

        private void CommitSelection()
        {
            var sel = new HashSet<string>();
            for (var i = 1; i < _list.Items.Count; i++)
                if (_list.GetItemChecked(i))
                    sel.Add(((Item)_list.Items[i]).Value);

            // 全選 => 不篩選（null）；否則記錄被勾選的值。
            Selected = sel.Count == _allValues.Count ? null : sel;
        }

        /// <summary>清單項目：保留原始值，顯示時將空白轉為提示文字。</summary>
        private sealed class Item
        {
            public string Value { get; }
            public Item(string value) { Value = value; }
            public override string ToString() { return Value.Length == 0 ? BlankText : Value; }
        }
    }
}
