using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HandleRdp.Forms;

/// <summary>
/// 批次刪除時，逐筆輸入刪除原因的對話框。
/// 每一個勾選的項目各一列（說明 + 原因輸入），全部填寫後才允許確定，再依序處理。
/// </summary>
public sealed class BatchReasonDialog : Form
{
    private readonly List<string> _labels;
    private readonly List<TextBox> _boxes = new();

    public BatchReasonDialog(string title, IReadOnlyList<string> itemLabels)
    {
        _labels = itemLabels.ToList();
        Text = title;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(620, 420);
        MinimumSize = new Size(480, 240);

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = $"共 {_labels.Count} 筆，請逐筆輸入刪除原因（皆為必填）：",
            Padding = new Padding(8, 6, 0, 0),
        };

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            Padding = new Padding(6),
        };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

        foreach (var label in _labels)
        {
            var name = new Label
            {
                Text = label,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var box = new TextBox { Dock = DockStyle.Fill };
            _boxes.Add(box);
            list.Controls.Add(name);
            list.Controls.Add(box);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(8),
        };
        var ok = new Button { Text = "確定刪除", AutoSize = true };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += OnOk;
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        Controls.Add(list);
        Controls.Add(header);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        for (var i = 0; i < _boxes.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(_boxes[i].Text))
            {
                MessageBox.Show($"「{_labels[i]}」的刪除原因為必填。", "缺少原因",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _boxes[i].Focus();
                return;
            }
        }
        DialogResult = DialogResult.OK;
    }

    /// <summary>依項目順序回傳每筆的原因。</summary>
    public List<string> Reasons => _boxes.Select(b => b.Text.Trim()).ToList();
}
