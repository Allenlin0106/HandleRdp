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
3. **篩選條件（類 Excel）**：查詢出資料後，點任一欄位標題即彈出下拉視窗，可對該欄升/降冪排序、並以核取清單勾選要顯示的值；多欄條件以 AND 合併（用戶端 `DataView.RowFilter`，不需重新查資料庫）。工具列另有「清除篩選」可一鍵還原。

## 需要你確認的設計決定（與既有 schema 的取捨）

- **刪除原因沒有對應欄位。** `RDP_USER_LOG` 沒有「原因」欄位，因此原因被併入 `Action`
  欄（`DELETE: {原因}`）。若你之後想加獨立欄位，請告知，我再調整 schema 與寫入邏輯。
- **日誌的 `Sponsor`** 是在同一交易內依 `Hostname` 從 `RDP_SERVER_INFO.Sponsor` 查得，查不到則為 `NULL`。
- **鍵值假設**：`RDP_SERVER_USER` 以 `(Hostname, User_ID)` 唯一識別；`RDP_SERVER_INFO` 以 `Hostname` 為鍵
  （修改/刪除均以此鎖定，Hostname 在修改時不可變更）。若實際主鍵不同請告知。
- **時間欄位一律由資料庫在「寫入當下」帶入（`GETDATE()`），不由使用者輸入。**
  - `RDP_SERVER_USER` 新增：`Login_Time`、`Logout_Time`、`Create_Time`、`Claim_Time` 皆寫入當下時間。
  - `RDP_SERVER_INFO` 新增：`Create_Time`、`Claim_Time` 皆寫入當下時間。
  - `RDP_SERVER_INFO` 修改：保留原 `Create_Time`，`Claim_Time` 更新為寫入當下時間。
  - CSV 匯入時即使含時間欄位也會被忽略，一律以寫入當下時間為準。

## 設定

連線字串在 `src/HandleRdp/App.config` 的 `connectionStrings/RdpDb`，請改成你的 SQL Server：

```xml
<connectionStrings>
  <add name="RdpDb"
       connectionString="Server=你的主機;Database=RDP;User Id=帳號;Password=密碼;TrustServerCertificate=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

> 假設三個資料表**已存在**於資料庫中，本專案不含建表腳本。
> SQL 連線使用 .NET Framework **內建的 `System.Data.SqlClient`**，不需任何 NuGet 套件。

## 建置與執行（Visual Studio 2017）

直接以 **Visual Studio 2017** 開啟 `HandleRdp.sln`，按 F5 建置並執行；
或用命令列（VS2017 的開發者命令提示字元）：

```cmd
msbuild HandleRdp.sln /p:Configuration=Release
bin\Release\HandleRdp.exe
```

> 這是 **classic（非 SDK 樣式）`.csproj`**、目標 **.NET Framework 4.8**、語言 **C# 7.3** 的 WinForms 專案，
> 以相容 Visual Studio 2017。請用 VS2017 或 `msbuild` 建置（classic 專案不適用 `dotnet build`）。
> 本程式碼是在 Linux 容器中撰寫，**尚未經過編譯或實機執行驗證**，請在 Windows 上首次建置時留意編譯訊息。

## 批次匯入 CSV 格式

第一列為標題，欄名需對應資料表欄位（大小寫不拘）。**時間欄位不需提供（提供也會被忽略），由資料庫於寫入當下帶入。**

`RDP_SERVER_USER`：
```csv
Hostname,User_ID,Employee_ID,Create_User
RDPHOST01,alice,E12345,admin
```

`RDP_SERVER_INFO`：
```csv
Department,Category,Hostname,Connectstring,Sponsor
IT,Prod,RDPHOST01,rdp://host01,bob
```

## 專案結構

```
HandleRdp.sln                 (Visual Studio 2017)
src/HandleRdp/
  HandleRdp.csproj            classic 專案檔 (net48, C# 7.3)
  App.config                  連線字串
  Program.cs                  進入點
  Properties/AssemblyInfo.cs  組件資訊
  Models/Models.cs            POCO 與 UserKey
  Data/
    Db.cs                     連線與設定載入 (System.Data.SqlClient)
    RdpServerUserRepository.cs 新增/刪除（含交易內寫日誌）/查詢
    RdpUserLogRepository.cs   查詢
    RdpServerInfoRepository.cs 新增/修改/刪除/查詢
  Forms/
    MainForm.cs              三個分頁的主視窗
    ServerUserTab.cs / ServerInfoTab.cs / UserLogTab.cs
    FilterableGrid.cs        可重用的表格 + 工具列（類 Excel 欄位篩選、可選的勾選欄）
    ColumnFilterPopup.cs     欄位標題點擊後的排序/勾選篩選下拉視窗
    FieldDialog.cs           通用輸入對話框（含必填驗證）
    BatchReasonDialog.cs     批次刪除時逐筆輸入原因的對話框
    Csv.cs                   批次匯入用的極簡 CSV 讀取器
    Ui.cs                    錯誤呈現/訊息框/日期解析等共用工具
```
