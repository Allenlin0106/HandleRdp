using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HandleRdp.Models;

namespace HandleRdp.Data
{
    /// <summary>
    /// RDP_SERVER_INFO 的存取層：新增、修改、刪除、查詢。
    /// 以 Hostname 作為唯一鍵（修改/刪除均以 Hostname 鎖定）。此表不需寫日誌。
    /// </summary>
    public sealed class RdpServerInfoRepository
    {
        private const string SelectColumns =
            "Department, Category, Hostname, Connectstring, Sponsor, Create_Time, Claim_Time";

        public DataTable Query()
        {
            using (var conn = Db.Open())
            using (var da = new SqlDataAdapter("SELECT " + SelectColumns + " FROM RDP_SERVER_INFO", conn))
            {
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void Add(RdpServerInfo info)
        {
            AddMany(new[] { info });
        }

        /// <summary>批次新增（需求 2）；整批為單一交易。</summary>
        public void AddMany(IReadOnlyCollection<RdpServerInfo> items)
        {
            if (items.Count == 0) return;

            using (var conn = Db.Open())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    foreach (var info in items)
                        Insert(conn, tx, info);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>依 Hostname 修改其餘欄位。</summary>
        public void Update(RdpServerInfo info)
        {
            const string sql = @"
                UPDATE RDP_SERVER_INFO
                   SET Department = @Department,
                       Category = @Category,
                       Connectstring = @Connectstring,
                       Sponsor = @Sponsor,
                       Create_Time = @Create_Time,
                       Claim_Time = @Claim_Time
                 WHERE Hostname = @Hostname";
            using (var conn = Db.Open())
            using (var cmd = new SqlCommand(sql, conn))
            {
                BindNonKey(cmd, info);
                Db.AddParam(cmd, "@Hostname", info.Hostname);
                var affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new InvalidOperationException("找不到要修改的紀錄：Hostname=" + info.Hostname + "。");
            }
        }

        /// <summary>依 Hostname 刪除一筆或多筆（需求 2 批次）。</summary>
        public void Delete(IReadOnlyCollection<string> hostnames)
        {
            if (hostnames.Count == 0) return;

            using (var conn = Db.Open())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    foreach (var host in hostnames)
                    {
                        using (var cmd = new SqlCommand(
                            "DELETE FROM RDP_SERVER_INFO WHERE Hostname = @Hostname", conn, tx))
                        {
                            Db.AddParam(cmd, "@Hostname", host);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static void Insert(SqlConnection conn, SqlTransaction tx, RdpServerInfo info)
        {
            const string sql = @"
                INSERT INTO RDP_SERVER_INFO
                    (Department, Category, Hostname, Connectstring, Sponsor, Create_Time, Claim_Time)
                VALUES
                    (@Department, @Category, @Hostname, @Connectstring, @Sponsor, @Create_Time, @Claim_Time)";
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                Db.AddParam(cmd, "@Hostname", info.Hostname);
                BindNonKey(cmd, info);
                cmd.ExecuteNonQuery();
            }
        }

        private static void BindNonKey(SqlCommand cmd, RdpServerInfo info)
        {
            Db.AddParam(cmd, "@Department", info.Department);
            Db.AddParam(cmd, "@Category", info.Category);
            Db.AddParam(cmd, "@Connectstring", info.Connectstring);
            Db.AddParam(cmd, "@Sponsor", info.Sponsor);
            Db.AddParam(cmd, "@Create_Time", info.Create_Time ?? DateTime.Now);
            Db.AddParam(cmd, "@Claim_Time", info.Claim_Time);
        }
    }
}
