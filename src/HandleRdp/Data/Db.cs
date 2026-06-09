using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace HandleRdp.Data;

/// <summary>
/// 連線設定與輔助方法。連線字串放在 appsettings.json 的 ConnectionStrings:RdpDb。
/// </summary>
public static class Db
{
    private static readonly string ConnectionString = LoadConnectionString();

    private static string LoadConnectionString()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "找不到 appsettings.json，請確認檔案已隨組建輸出。", path);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var cs = doc.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("RdpDb")
            .GetString();

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("appsettings.json 缺少連線字串 ConnectionStrings:RdpDb。");

        return cs;
    }

    public static SqlConnection Open()
    {
        var conn = new SqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    /// <summary>新增一個具名參數，null 會轉成 DBNull。</summary>
    public static void AddParam(SqlCommand cmd, string name, object? value)
        => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
}
