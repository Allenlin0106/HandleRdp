using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HandleRdp.Forms
{
    /// <summary>單一欄位定義。</summary>
    public sealed class Field
    {
        public string Key { get; }
        public string Label { get; }
        public string Value { get; set; }
        public bool ReadOnly { get; }
        public bool Required { get; }

        public Field(string key, string label, string value = "", bool readOnly = false, bool required = false)
        {
            Key = key;
            Label = label;
            Value = value;
            ReadOnly = readOnly;
            Required = required;
        }
    }

    /// <summary>
    /// 以欄位清單動態產生的通用輸入對話框。新增使用者、編輯伺服器都共用，避免重複手刻表單。
    /// 必填欄位未填時不允許確定（Fail loud：在 UI 與資料層各擋一次）。
    /// </summary>
    public sealed class FieldDialog : Form
    {
        private readonly List<Field> _fields;
        private readonly Dictionary<string, TextBox> _boxes = new Dictionary<string, TextBox>();

        public FieldDialog(string title, List<Field> fields, int width = 440)
        {
            _fields = fields;
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var rowHeight = 30;
            var top = 12;
            foreach (var f in fields)
            {
                var label = new Label
                {
                    Text = f.Required ? f.Label + " *" : f.Label,
                    Left = 12,
                    Top = top + 3,
                    Width = 130,
                };
                var box = new TextBox
                {
                    Left = 150,
                    Top = top,
                    Width = width - 180,
                    Text = f.Value,
                    ReadOnly = f.ReadOnly,
                };
                if (f.ReadOnly) box.BackColor = SystemColors.Control;
                Controls.Add(label);
                Controls.Add(box);
                _boxes[f.Key] = box;
                top += rowHeight;
            }

            var ok = new Button { Text = "確定", DialogResult = DialogResult.OK, Left = width - 180, Top = top + 8, Width = 80 };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Left = width - 92, Top = top + 8, Width = 80 };
            ok.Click += OnOk;

            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
            ClientSize = new Size(width, top + 48);
        }

        private void OnOk(object sender, EventArgs e)
        {
            foreach (var f in _fields)
            {
                if (f.Required && string.IsNullOrWhiteSpace(_boxes[f.Key].Text))
                {
                    MessageBox.Show("「" + f.Label + "」為必填。", "缺少必填欄位",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    _boxes[f.Key].Focus();
                    return;
                }
            }
        }

        public string Get(string key)
        {
            return _boxes[key].Text.Trim();
        }
    }
}
