# WebView2 Desktop Pet HUD Lab

Windows 桌面寵物與 HUD 實驗專案，使用 .NET WinForms 承載兩個 WebView2：

- 控制面板 WebView2：選擇寵物外觀。
- 透明覆蓋 WebView2：在桌面上顯示並移動 2D 或 3D 寵物。

本專案目前不依賴 MCP，可獨立執行。

## 功能

- 無邊框、置頂的寵物選擇面板
- 透明、置頂且滑鼠穿透的桌面寵物層
- 2D HTML／CSS 電子寵物
- Three.js／WebGL 程式化 3D 電子寵物
- 即時切換 2D 與 3D 外觀
- 寵物帶有慣性地跟隨系統滑鼠
- 靠近螢幕邊緣時自動調整位置
- 2D 漂浮、眨眼與搖尾動畫
- 3D 光照、漂浮、轉頭、搖尾與能量環動畫
- 支援多螢幕虛擬桌面座標
- Three.js 本地打包，執行時不依賴 CDN
- WebView2 資料存放於 `%LOCALAPPDATA%\A15-HudWebViewDemo\WebView2`

## 執行需求

- Windows 10 或 Windows 11
- .NET 10 SDK
- Microsoft Edge WebView2 Runtime

## 執行

雙擊 `啟動HUD.bat`，或執行：

```powershell
dotnet run
```

第一次執行會還原 Microsoft WebView2 NuGet 套件。

## 專案結構

- `HudForm.cs`：控制面板、透明寵物視窗與 WebView2 訊息橋接
- `wwwroot/index.html`：2D／3D 寵物選擇面板
- `wwwroot/pet-overlay.html`：透明寵物頁面
- `wwwroot/pet-overlay-source.js`：寵物與 Three.js 場景原始碼
- `wwwroot/pet-overlay.bundle.js`：瀏覽器執行用單檔 bundle
- `wwwroot/vendor/`：本地 Three.js 來源
- `啟動HUD.bat`：快速啟動

## 技術說明

本機頁面透過 WebView2 虛擬主機 `https://appassets.local/` 載入。Three.js 與寵物程式已預先打包成傳統 IIFE JavaScript，避免本機 ES Module 與 CORS 限制。

目前 3D 渲染約限制為 30 FPS，並限制裝置像素比例以降低全螢幕透明 WebGL 的 GPU 負擔。
