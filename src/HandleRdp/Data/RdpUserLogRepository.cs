using System.Data;
using Microsoft.Data.SqlClient;

namespace HandleRdp.Data;

/// <summary>RDP_USER_LOG 的存取層：僅提供查詢（需求：此表只查不改）。</summary>
public sealed class RdpUserLogRepository
{
    public DataTable Query()
    {
        using var conn = Db.Open();
        using var da = new SqlDataAdapter(
            "SELECT Hostname, User_ID, Employee_ID, Sponsor, Action, Claim_Time FROM RDP_USER_LOG", conn);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }
}
