using System.Data;
using HandleRdp.Models;
using Microsoft.Data.SqlClient;

namespace HandleRdp.Data;

/// <summary>
/// RDP_SERVER_USER 的存取層：新增、刪除、查詢。
///
/// 重要規則（需求 1）：
///   - 每次「新增」與「刪除」都必須在「同一個交易」內同步寫入 RDP_USER_LOG，
///     避免主檔與日誌不一致。
///   - 刪除一定要有原因；原因會寫進 RDP_USER_LOG.Action（該表沒有獨立的原因欄位），
///     格式為 "DELETE: {reason}"。新增則寫 Action = "ADD"。
///   - 日誌的 Sponsor 由 RDP_SERVER_INFO 依 Hostname 查得（查不到則為 NULL）。
/// </summary>
public sealed class RdpServerUserRepository
{
    private const string SelectColumns =
        "Hostname, User_ID, Employee_ID, Login_Time, Logout_Time, Create_User, Create_Time, Claim_Time";

    /// <summary>查詢全部資料；篩選由 UI 端針對結果套用。</summary>
    public DataTable Query()
    {
        using var conn = Db.Open();
        using var da = new SqlDataAdapter($"SELECT {SelectColumns} FROM RDP_SERVER_USER", conn);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public void Add(RdpServerUser user) => AddMany(new[] { user });

    /// <summary>批次新增（需求 2）；全部成功才提交，任何一筆失敗就整批回滾。</summary>
    public void AddMany(IReadOnlyCollection<RdpServerUser> users)
    {
        if (users.Count == 0) return;

        using var conn = Db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var u in users)
            {
                InsertUser(conn, tx, u);
                var sponsor = LookupSponsor(conn, tx, u.Hostname);
                InsertLog(conn, tx, u.Hostname, u.User_ID, u.Employee_ID, sponsor, "ADD");
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 刪除一筆或多筆（需求 1、2）。reason 為必填，會寫入每一筆對應的日誌。
    /// </summary>
    public void Delete(IReadOnlyCollection<UserKey> keys, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("刪除原因為必填。", nameof(reason));
        if (keys.Count == 0) return;

        var action = "DELETE: " + reason.Trim();

        using var conn = Db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var k in keys)
            {
                // 先取出 Employee_ID 與 Sponsor，刪除後才有辦法完整寫日誌。
                var employeeId = LookupEmployeeId(conn, tx, k);
                var sponsor = LookupSponsor(conn, tx, k.Hostname);

                var affected = DeleteUser(conn, tx, k);
                if (affected == 0)
                    throw new InvalidOperationException(
                        $"找不到要刪除的紀錄：Hostname={k.Hostname}, User_ID={k.User_ID}。");

                InsertLog(conn, tx, k.Hostname, k.User_ID, employeeId, sponsor, action);
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ---- 內部輔助（全部使用同一交易） ----

    private static void InsertUser(SqlConnection conn, SqlTransaction tx, RdpServerUser u)
    {
        const string sql = """
            INSERT INTO RDP_SERVER_USER
                (Hostname, User_ID, Employee_ID, Login_Time, Logout_Time, Create_User, Create_Time, Claim_Time)
            VALUES
                (@Hostname, @User_ID, @Employee_ID, @Login_Time, @Logout_Time, @Create_User, @Create_Time, @Claim_Time)
            """;
        using var cmd = new SqlCommand(sql, conn, tx);
        Db.AddParam(cmd, "@Hostname", u.Hostname);
        Db.AddParam(cmd, "@User_ID", u.User_ID);
        Db.AddParam(cmd, "@Employee_ID", u.Employee_ID);
        Db.AddParam(cmd, "@Login_Time", u.Login_Time);
        Db.AddParam(cmd, "@Logout_Time", u.Logout_Time);
        Db.AddParam(cmd, "@Create_User", u.Create_User);
        Db.AddParam(cmd, "@Create_Time", u.Create_Time ?? DateTime.Now);
        Db.AddParam(cmd, "@Claim_Time", u.Claim_Time);
        cmd.ExecuteNonQuery();
    }

    private static int DeleteUser(SqlConnection conn, SqlTransaction tx, UserKey k)
    {
        const string sql = "DELETE FROM RDP_SERVER_USER WHERE Hostname = @Hostname AND User_ID = @User_ID";
        using var cmd = new SqlCommand(sql, conn, tx);
        Db.AddParam(cmd, "@Hostname", k.Hostname);
        Db.AddParam(cmd, "@User_ID", k.User_ID);
        return cmd.ExecuteNonQuery();
    }

    private static string LookupEmployeeId(SqlConnection conn, SqlTransaction tx, UserKey k)
    {
        const string sql =
            "SELECT TOP 1 Employee_ID FROM RDP_SERVER_USER WHERE Hostname = @Hostname AND User_ID = @User_ID";
        using var cmd = new SqlCommand(sql, conn, tx);
        Db.AddParam(cmd, "@Hostname", k.Hostname);
        Db.AddParam(cmd, "@User_ID", k.User_ID);
        return cmd.ExecuteScalar() as string ?? "";
    }

    private static string? LookupSponsor(SqlConnection conn, SqlTransaction tx, string hostname)
    {
        const string sql = "SELECT TOP 1 Sponsor FROM RDP_SERVER_INFO WHERE Hostname = @Hostname";
        using var cmd = new SqlCommand(sql, conn, tx);
        Db.AddParam(cmd, "@Hostname", hostname);
        return cmd.ExecuteScalar() as string;
    }

    private static void InsertLog(
        SqlConnection conn, SqlTransaction tx,
        string hostname, string userId, string employeeId, string? sponsor, string action)
    {
        const string sql = """
            INSERT INTO RDP_USER_LOG
                (Hostname, User_ID, Employee_ID, Sponsor, Action, Claim_Time)
            VALUES
                (@Hostname, @User_ID, @Employee_ID, @Sponsor, @Action, @Claim_Time)
            """;
        using var cmd = new SqlCommand(sql, conn, tx);
        Db.AddParam(cmd, "@Hostname", hostname);
        Db.AddParam(cmd, "@User_ID", userId);
        Db.AddParam(cmd, "@Employee_ID", employeeId);
        Db.AddParam(cmd, "@Sponsor", sponsor);
        Db.AddParam(cmd, "@Action", action);
        Db.AddParam(cmd, "@Claim_Time", DateTime.Now);
        cmd.ExecuteNonQuery();
    }
}
