# **資料表結構說明（Tables Schema Specification）**

版本：v1.1

最後更新：2026-04-03

更新內容：優化欄位命名直覺性、強化索引邏輯與安全性規範。

本文件定義系統中兩張核心資料表之結構，用於管理教師基本資訊、二階段驗證（2FA）及聘書派送與回覆流程。

## **1\. 資料實體關係 (ER) 概述**

### **1.1 關聯邏輯**

* **主關聯鍵**：兩表主要透過 yr（學年度）與 empl\_no（人員代號）進行業務關聯。  
* **唯一性約束**：在 teach\_appo\_empl\_base 中，(yr, id\_no, birthday) 的組合必須唯一，且應能唯一對應至 empl\_no。

### **1.2 業務流程說明**

1. 系統透過 empl\_base 進行身份驗證與 2FA 發送。  
2. 驗證通過後，根據 empl\_no 至 teach\_appo\_resp 查詢該教師於當學年度的所有聘書紀錄。

## **2\. 資料表規格詳解**

### **2.1 teach\_appo\_empl\_base — 教師個人基本資料**

本表存儲教師核心個資、登入憑證及安全驗證資訊。

| 欄位名稱 | 資料類型 | 允許空值 | 說明 |
| :---- | :---- | :---- | :---- |
| yr | int | No | **PK** 學年度 (例如：115) |
| id\_no | varchar(60) | No | **PK** 身份證字號 (建議加密或雜湊存儲) |
| birthday | datetime | No | **PK** 生日 (格式：YYYY-MM-DD) |
| empl\_no | char(6) | No | 人員代號 (員工編號) |
| ch\_name | nvarchar(60) | No | 中文姓名 |
| en\_name | nvarchar(60) | Yes | 英文姓名 |
| email | nvarchar(100) | Yes | 電子郵件地址 |
| is_temporary_email | int | Yes | 0-原留存信箱,1-非原留存信箱 |
| mobile | nvarchar(100) | Yes | 手機號碼 |
| two\_factor\_secret | varchar(10) | Yes | 二階段驗證碼 (OTP/Secret) |
| two\_factor\_expired | datetime | Yes | 2FA 驗證碼到期時間 |
| two\_factor\_is\_verified | int | Yes | 0-未驗證,1-已驗證 |
| refresh\_token | nvarchar(255) | Yes | JWT Refresh Token |
| refresh\_token\_expired | datetime | Yes | Token 到期時間 |
| role | nvarchar(20) | No | 權限角色 (user, admin) |
| seq\_no | int (Identity) | No | 系統自動編號 (非業務主鍵) |
| create\_date | datetime | No | 建立日期 (Default: getdate()) |
| update\_date | datetime | No | 最後更新日期 |
| row\_version | rowversion | No | 樂觀鎖定 (Optimistic Concurrency) |
| deleted\_at | datetime | Yes | 軟刪除標記 |

### **2.2 teach\_appo\_resp — 聘書派送與回覆紀錄表**

本表存儲聘書 PDF 二進位資料、派送狀態及教師線上簽署(回覆)紀錄。

| 欄位名稱 | 資料類型 | 允許空值 | 說明 |
| :---- | :---- | :---- | :---- |
| yr | int | No | **PK** 學年度 (例如：115) |
| empl\_no | char(6) | No | **PK** 人員代號 |
| appo\_doc\_yy | int | No | **PK** 聘書年度字首 |
| appo\_doc\_ch | char(2) | No | **PK** 聘書字別 (如：教字) |
| appo\_doc\_seq | char(4) | No | **PK** 聘書流水號 |
| file\_name | nvarchar(100) | Yes | 原始 PDF 檔名 |
| pdf\_content | varbinary(max) | Yes | 聘書 PDF 檔案二進位內容 |
| resp\_status | int | No | 回覆狀態 (0: 未回覆, 1: 已回覆) |
| download\_count | int | No | 下載次數累計 |
| remark | nvarchar(200) | Yes | 系統或人工備註 |
| seq\_no | int (Identity) | No | 系統流水號 |
| create\_date | datetime | No | 建立日期 |
| update\_date | datetime | No | 回覆/更新日期 |

### **2.3 LoginLogs - 登入稽核紀錄表**

| 欄位名稱 | 資料類型 | 允許空值 | 說明 | 
| :---- | :---- | :---- | :---- | 
| **LogId** | INTEGER | PK | 自動遞增主鍵。 |
| **IdNo** | TEXT(60) | 是 | 登入者身分證字號。 | 
| **VerifyMethod** | int | Yes | 0-無, 1-「Email 驗證」, 2-「QRCode 掃描」 |
| **TargetEmail** | TEXT(100) | Yes | 紀錄該次發送驗證碼的目標信箱 |
| **ClientIP** | TEXT(200) | 是 | 使用者連線 IP 地址。 | 
| **UserAgent** | TEXT(100) | 否 | 瀏覽器或裝置資訊。 | 
| **Status** | INTEGER | 是 | 登入結果狀態。 (**0**: 失敗 / **1**: 成功)|
| **FailureReason** | TEXT(100) | 否 | 紀錄失敗原因。 |
| **Timestamp** | DATETIME | 是 | 紀錄產生的時間。 ( 預設為 CURRENT\_TIMESTAMP ) |

## **3\. 索引優化建議 (Performance Tuning)**

為了確保查詢效率，建議建立以下非叢集索引（Non-Clustered Indexes）：

1. **教師資料關聯索引**：  
   * IX\_teach\_appo\_empl\_base\_Lookup: (yr, empl\_no)  
   * *用途：加快從人員代號反查基本資料的速度。*  
2. **聘書查詢索引**：  
   * IX\_teach\_appo\_resp\_UserView: (yr, empl\_no, resp\_status)  
   * *用途：前端儀表板顯示「待回覆聘書」時的過濾效率。*

## **4\. 維運與安全性注意事項**

1. **主鍵選用**：目前 empl\_base 使用 (yr, id\_no, birthday) 作為主鍵。在實務上，若教師跨學年度資料需比對，請確保 empl\_no 在不同學年度之間的一致性。  
2. **敏感資料保護**：  
   * id\_no 與 birthday 屬於高度敏感個資，存儲時建議進行加密或至少在應用層進行遮罩處理。  
   * pdf\_content 存儲於資料庫（Blob）雖便於備份，但若檔案量極大，未來可考慮遷移至雲端存儲（如 S3/Azure Blob），資料庫僅保留連結。  
3. **效能管理**：由於 teach\_appo\_resp 含有 varbinary(max) 欄位，執行 SELECT \* 會造成嚴重的 IO 負擔。**開發時務必指定欄位**，僅在下載動作時提取 pdf\_content。

**文件結束**