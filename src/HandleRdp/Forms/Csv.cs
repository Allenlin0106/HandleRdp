using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HandleRdp.Forms
{
    /// <summary>
    /// 極簡 CSV 讀取器（支援雙引號包覆與引號內逗號）。
    /// 第一列視為標題，回傳每列的「欄名 -> 值」字典。批次匯入用。
    /// </summary>
    public static class Csv
    {
        /// <summary>
        /// 取得某欄的值，欄位不存在時回傳空字串。
        /// （.NET Framework 沒有 Dictionary.GetValueOrDefault，故自備此輔助方法。）
        /// </summary>
        public static string Field(this IReadOnlyDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var v) ? v : "";
        }

        public static List<Dictionary<string, string>> Read(string path)
        {
            var lines = File.ReadAllLines(path);
            var rows = new List<Dictionary<string, string>>();
            if (lines.Length == 0) return rows;

            var headers = ParseLine(lines[0]);
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var fields = ParseLine(lines[i]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var c = 0; c < headers.Count; c++)
                    row[headers[c]] = c < fields.Count ? fields[c] : "";
                rows.Add(row);
            }
            return rows;
        }

        private static List<string> ParseLine(string line)
        {
            var result = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(ch);
                }
                else
                {
                    if (ch == '"') inQuotes = true;
                    else if (ch == ',') { result.Add(field.ToString().Trim()); field.Clear(); }
                    else field.Append(ch);
                }
            }
            result.Add(field.ToString().Trim());
            return result;
        }
    }
}
