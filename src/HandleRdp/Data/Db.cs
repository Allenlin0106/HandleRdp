using System;
using System.Configuration;
using Microsoft.Data.SqlClient;

namespace HandleRdp.Data;

/// <summary>
/// 連線設定與輔助方法。連線字串放在 App.config 的 connectionStrings/RdpDb。
/// </summary>
public static class Db
{
    private static readonly string ConnectionString = LoadConnectionString();

    private static string LoadConnectionString()
    {
        var setting = ConfigurationManager.ConnectionStrings["RdpDb"];
        if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            throw new InvalidOperationException("App.config 缺少連線字串 connectionStrings/RdpDb。");
        return setting.ConnectionString;
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
