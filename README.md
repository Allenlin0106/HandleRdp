# HandleRdp

針對三個 RDP 相關資料表進行查詢與維護的 **Windows 桌面程式 (C# / .NET Framework 4.8 WinForms + SQL Server)**。

| 資料表 | 功能 | 欄位 |
| --- | --- | --- |
| `RDP_SERVER_USER` | 新增、刪除、查詢 | Hostname, User_ID, Employee_ID, Login_Time, Logout_Time, Create_User, Create_Time, Claim_Time |
| `RDP_USER_LOG` | 查詢（唯讀） | Hostname, User_ID, Employee_ID, Sponsor, Action, Claim_Time |
| `RDP_SERVER_INFO` | 新增、修改、刪除、查詢 | Department, Category, Hostname, Connectstring, Sponsor, Create_Time, Claim_Time |

## 已實作的需求

1. **新增/刪除 `RDP_SERVER_USER` 會同步寫入 `RDP_USER_LOG`。**
   主檔與日誌包在**同一個資料庫交易**內，任何一步失敗就整批回滾，確保不會只寫一半。
   - 新增：`Action = "ADD"`
   - 刪除：**強制輸入原因**（UI 與資料層各擋一次），寫成 `Action = "DELETE: {原因}"`
2. **批次處理**：CSV 匯入可一次新增多筆。批次刪除 `RDP_SERVER_USER` 時，於表格左側的
   **勾選欄**逐筆勾選，按「刪除勾選」後彈出視窗，**逐筆輸入各別原因**（皆為必填），再依序處理；
   每筆寫入自己的 `RDP_USER_LOG`。整批為單一交易，任一筆失敗即全部回滾。
3. **篩選條件**：查詢結果可在任一欄位上以「包含 / 等於 / 開頭為」即時篩選（用戶端 `DataView.RowFilter`，不需重新查資料庫）。

## 需要你確認的設計決定（與既有 schema 的取捨）

- **刪除原因沒有對應欄位。** `RDP_USER_LOG` 沒有「原因」欄位，因此原因被併入 `Action`
  欄（`DELETE: {原因}`）。若你之後想加獨立欄位，請告知，我再調整 schema 與寫入邏輯。
- **日誌的 `Sponsor`** 是在同一交易內依 `Hostname` 從 `RDP_SERVER_INFO.Sponsor` 查得，查不到則為 `NULL`。
- **鍵值假設**：`RDP_SERVER_USER` 以 `(Hostname, User_ID)` 唯一識別；`RDP_SERVER_INFO` 以 `Hostname` 為鍵
  （修改/刪除均以此鎖定，Hostname 在修改時不可變更）。若實際主鍵不同請告知。
- `Create_Time` 在新增時若未指定則自動帶入現在時間；`Claim_Time` 維持使用者輸入（可空）。

## 設定

連線字串在 `src/HandleRdp/App.config` 的 `connectionStrings/RdpDb`，請改成你的 SQL Server：

```xml
<connectionStrings>
  <add name="RdpDb"
       connectionString="Server=你的主機;Database=RDP;User Id=帳號;Password=密碼;TrustServerCertificate=True;"
       providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

> 假設三個資料表**已存在**於資料庫中，本專案不含建表腳本。

## 建置與執行（需在 Windows 上）

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/HandleRdp
```

> 這是 `net48`（.NET Framework 4.8）SDK 樣式 WinForms 專案，**必須在 Windows 上**以 Visual Studio 2022
> 或已安裝 .NET Framework 4.8 開發包的 `dotnet`/`msbuild` 建置。
> 本程式碼是在 Linux 容器中撰寫，**尚未經過編譯或實機執行驗證**，請在 Windows 上首次建置時留意編譯訊息。

## 批次匯入 CSV 格式

第一列為標題，欄名需對應資料表欄位（大小寫不拘）。

`RDP_SERVER_USER`：
```csv
Hostname,User_ID,Employee_ID,Login_Time,Logout_Time,Create_User,Claim_Time
RDPHOST01,alice,E12345,2026-06-09 09:00,,admin,2026-06-09 09:00
```

`RDP_SERVER_INFO`：
```csv
Department,Category,Hostname,Connectstring,Sponsor,Claim_Time
IT,Prod,RDPHOST01,rdp://host01,bob,2026-06-09
```

## 專案結構

```
HandleRdp.sln
src/HandleRdp/
  App.config                  連線字串
  Program.cs                  進入點
  Models/Models.cs            POCO 與 UserKey
  Data/
    Db.cs                     連線與設定載入
    RdpServerUserRepository.cs 新增/刪除（含交易內寫日誌）/查詢
    RdpUserLogRepository.cs   查詢
    RdpServerInfoRepository.cs 新增/修改/刪除/查詢
  Forms/
    MainForm.cs              三個分頁的主視窗
    ServerUserTab.cs / ServerInfoTab.cs / UserLogTab.cs
    FilterableGrid.cs        可重用的表格 + 篩選列 + 工具列（可選的勾選欄）
    FieldDialog.cs           通用輸入對話框（含必填驗證）
    BatchReasonDialog.cs     批次刪除時逐筆輸入原因的對話框
    Csv.cs                   批次匯入用的極簡 CSV 讀取器
    Ui.cs                    錯誤呈現/訊息框/日期解析等共用工具
```
