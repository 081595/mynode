# **教師聘書管理系統技術規格書 (Teacher Appointment System Specification)**

本系統旨在處理教師聘書的發送、維護，並透過 2FA (雙重驗證) 與 JWT 授權機制，確保系統操作的安全性與資料的完整性。

## **1\. 技術棧與系統架構 (Technology Stack)**

### **1.1 核心開發框架**

* **開發環境**: **GitHub Codespaces**。  
* **後端**: **ASP.NET Core Web API** 整合 **Razor Pages** (用於行政管理介面)。  
* **資料庫**: **SQLite** (檔案型資料庫，便於初期部署與維護)。  
* **前端展示**: **Razor Pages (Server-side Rendering)** 與 **Bootstrap 5** 搭配 **AJAX/SignalR** 處理異步互動。 
* **工具庫**:  
  * **QRCoder**: 用於生成二維碼。  
  * **BCrypt.Net**: 用於敏感資訊（如 TwoFactorSecret）的雜湊處理。  
  * **System.IdentityModel.Tokens.Jwt**: 用於 JWT 核發與驗證。

### **1.2 身份驗證與授權 (Auth Mechanism)**

系統採用 **JWT (Access/Refresh Token)** 技術，結合 **HttpOnly Cookie** 安全傳輸：

* **Access Token**:  
  * 儲存於 HttpOnly Cookie，防止 JavaScript 存取 (防禦 XSS)。  
  * 有效期限：短時效 (建議 15-30 分鐘)。  
* **Refresh Token**:  
  * 同時儲存於 HttpOnly Cookie 與資料庫 (TeachAppoEmplBase.RefreshToken)。  
  * 用途：當 Access Token 到期時，自動於後端進行無感刷新。

## **2\. 功能模組設計**

### **2.1 2FA 雙重驗證流程 (無密碼登入)**

本系統採用「身分基礎識別 \+ 多因子動態驗證」機制，捨棄傳統密碼，改以 **身分證字號與生日** 作為第一層過濾。

#### **階段一：基礎身分識別 (Identification)**

1. **輸入資訊：** 使用者輸入 IdNo (身分證字號) 與 Birthday (格式：YYYYMMDD)。  
2. **身分核對：** 系統檢索 TeachAppoEmplBase 資料。  
3. **初始化挑戰：**  
   * 核對成功後，系統生成 6 位隨機驗證碼存入 TwoFactorSecret。  
   * 設置 TwoFactorExpired (效期 **5 分鐘**)。

#### **階段二：多因子挑戰 (MFA Challenge)**

使用者可選擇以下任一方式完成驗證，並具備彈性的重新生成機制：

##### **A. Email 驗證路徑**

* **預設發送：** 系統初步將驗證碼發送至該教師於系統留存之電子信箱。  
* **自主變更信箱 (重新生成)：** 若使用者無法存取原留存信箱（如已停用或空間滿了），可選擇「**變更信箱並重發**」。  
  * **操作：** 使用者可輸入一個新的電子信箱。  
  * **安全防護：** 為防止惡意嘗試，系統會針對「IdNo \+ 生日」的重發頻率進行限制（例如：每3分鐘限制重發 1 次）。  
* **測試模式：** **驗證碼** 將同步顯示於網頁 UI，便於開發與環境測試。

##### **B. QRCode 行動掃描路徑**

* **生成機制：** 系統生成帶有 UniqueSessionID 的專屬驗證連結並轉為 QRCode。  
* **同步技術 (SignalR)：** 透過 **SignalR** 實時監聽驗證狀態。教師以手機掃描並點擊「確認登入」後，手機端將**立即關閉**，桌機端將**立即自動跳轉**，無需手動重新整理。  
* **重新生成：** 若 QRCode 超時失效，使用者可點擊「**刷新 QRCode**」以取得新的驗證連結。

#### **階段三：授權核發 (Authorization)**

一旦通過 Email 代碼回填或行動端點擊確認，系統即核發 JWT 權杖：

* **Access Token:** 15 分鐘（短期存取）。  
* **Refresh Token:** 7 天（長期續航）。
* **更新回覆狀態:** 將 teach_appo_resp 資料表中的 resp_status 變更為 1 (已回覆)。
* **清除 2FA 祕鑰:** 立即將資料庫中該員的 TwoFactorSecret 欄位清空（或設為 Null）。
* **銷毀 QRCode 連結:** 使該次驗證專屬的 UniqueSessionID 立即失效。
* **連線通知:** 透過 SignalR 發送指令關閉行動端視窗，並指揮桌機端瀏覽器導向首頁。

#### **💡 安全建議（針對非留存信箱功能）：**

既然允許輸入「非留存信箱」，建議在後端實作時增加以下邏輯，以防系統被當作發信跳板或被暴力破解：

1. **操作日誌：** 紀錄使用者所有操作(含登入等)，以便日後稽核。  
2. **紀錄變更 Log：** 將使用者輸入的「新信箱」與 IdNo 綁定紀錄，以便日後稽核。  
3. **成功驗證後同步：** 若使用者用「新信箱」驗證成功，系統可提示是否要「同步更新」個人資料中的聯絡信箱。  
4. **速率限制 (Rate Limiting)：** 嚴格限制同一個 **IdNo \+ 生日** 在短時間內請求驗證碼的次數。

### **2.2 系統首頁 (Dashboard)**

* **公開資訊**: 顯示系統名稱 **教師聘書管理系統 ** 及 **伺服器當前時間**。  
* **登入狀態顯示**: 已登入者顯示 id\_no + ch\_name； 未登入者顯示空白。  
* **導覽列選單**:  
  * 未登入：僅顯示 **登入** 按鈕。  
  * 已登入：依角色 **(User/Admin)** 權限, 顯示對應 **功能按鈕**（下載聘書、資料維護等）, **登出**。  
  * 「登入」與「登出」按鈕採互斥顯示。

### **2.3 教師聘書資料下載**

* **對象**: teach\_appo\_resp 資料表中的 pdf\_content (PDF 二進位資料)。  
* **權限控制**:  
  * User: 僅能檢索並下載屬於自己 empl\_no 的聘書。  
  * Admin: 可查詢並下載所有教師的聘書。
  * **自動化計算**: 教師端(User)下載時，系統自動累加 **下載次數累計**； 管理員端查閱則不累加。

### **2.4 教師個人基本資料維護**

* **權限控制**: Admin(管理員專屬)
* **功能**: 對 teach\_appo\_empl\_base 進行完整 CRUD。  
* **重點項目**: 包含 Role (角色權限)、Email 等關鍵欄位的編輯。

### **2.5 聘書發送與回覆維護**

* **權限控制**: Admin(管理員專屬)
* **功能**: 對 teach\_appo\_resp 進行維護。  
* **重點項目**: 上傳聘書 PDF 檔案、編輯備註、檢視教師回覆狀態。

### **2.6 登出功能**

* **動作**: 清除瀏覽器 Access Token 與 Refresh Token 的 Cookie，並同步使資料庫中的 Refresh Token 失效。

## **3\. 自動刷新機制 (Token Refresh Logic)**

當前端請求 API 收到 401 Unauthorized 時：

1. 攔截器自動讀取 Cookie 中的 RefreshToken。  
2. 呼叫刷新接口，核對資料庫中的 Token 是否匹配且在有效期內。  
3. 驗證成功後，重新核發新 Token 並寫入 Cookie。  
4. 若 Refresh Token 也失效，則引導至登入頁面重新進行 2FA。

## **4\. 合理性檢驗與優化建議**

* **安全防護**: 針對「第一階段驗證」應加入速率限制 (Rate Limiting)，防止暴力猜測身份證字號與生日。  
* **QRCode 同步**: 建議使用 SignalR Hub 實作。當手機端更新資料庫狀態後，Server 直接 Push 訊息給桌機，體驗最優。  
* **時效性**: TwoFactorExpired 與 AccessToken 建議設短（如 5 分鐘與 15 分鐘），RefreshToken 則可設長（如 7 天）。

**文件結束**