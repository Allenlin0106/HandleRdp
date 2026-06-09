using System;
using System.Globalization;
using System.Windows.Forms;

namespace HandleRdp.Forms;

/// <summary>UI 共用小工具：統一錯誤呈現、訊息框、日期解析與檔案挑選。</summary>
public static class Ui
{
    /// <summary>包住會碰資料庫的動作，任何例外都明確跳錯（Rule 12：失敗要大聲）。</summary>
    public static void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "操作失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static void Info(string message)
        => MessageBox.Show(message, "訊息", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool Confirm(string message)
        => MessageBox.Show(message, "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
           == DialogResult.Yes;

    public static DateTime? ParseDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
            return dt;
        throw new FormatException($"無法解析日期時間：「{text}」。");
    }

    public static string? NullIfEmpty(string text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    public static string? PickCsv()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "選擇要匯入的 CSV",
            Filter = "CSV 檔 (*.csv)|*.csv|所有檔案 (*.*)|*.*",
        };
        return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
    }
}
