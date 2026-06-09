using System;

namespace HandleRdp.Models;

/// <summary>RDP_SERVER_USER 一筆紀錄。</summary>
public sealed class RdpServerUser
{
    public string Hostname { get; set; } = "";
    public string User_ID { get; set; } = "";
    public string Employee_ID { get; set; } = "";
    public DateTime? Login_Time { get; set; }
    public DateTime? Logout_Time { get; set; }
    public string? Create_User { get; set; }
    public DateTime? Create_Time { get; set; }
    public DateTime? Claim_Time { get; set; }
}

/// <summary>RDP_SERVER_INFO 一筆紀錄。</summary>
public sealed class RdpServerInfo
{
    public string Department { get; set; } = "";
    public string Category { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string Connectstring { get; set; } = "";
    public string? Sponsor { get; set; }
    public DateTime? Create_Time { get; set; }
    public DateTime? Claim_Time { get; set; }
}

/// <summary>
/// 刪除 RDP_SERVER_USER 時用來鎖定一筆紀錄的鍵值。
/// 假設 (Hostname, User_ID) 可唯一識別一筆使用者紀錄。
/// </summary>
public readonly struct UserKey
{
    public string Hostname { get; }
    public string User_ID { get; }

    public UserKey(string hostname, string userId)
    {
        Hostname = hostname;
        User_ID = userId;
    }
}
