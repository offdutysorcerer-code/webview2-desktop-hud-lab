# WebView2 Desktop HUD Lab

一個 Windows 桌面 HUD 實驗專案，使用 .NET WinForms 與 WebView2，讓 HTML、CSS 和 JavaScript UI 常駐桌面，並搭配透明、穿透的滑鼠視覺覆蓋層。

本專案目前不依賴 MCP，可獨立執行。

## 功能

- 無邊框、置頂的 WebView2 HUD 控制面板
- 即時顯示系統滑鼠座標
- 將滑鼠移動到虛擬桌面的左上、中央或右下
- 在目前位置送出滑鼠左鍵
- 透明且不攔截操作的游標 HUD
- `F8` 切換面板互動／滑鼠穿透
- 支援多螢幕虛擬桌面座標
- WebView2 使用者資料存放於 `%LOCALAPPDATA%\A15-HudWebViewDemo\WebView2`

## 執行需求

- Windows 10 或 Windows 11
- .NET 10 SDK
- Microsoft Edge WebView2 Runtime

## 執行

雙擊 `啟動HUD.bat`，或在專案資料夾執行：

```powershell
dotnet run
```

第一次執行會還原 Microsoft WebView2 NuGet 套件。

## 操作

- 面板位置按鈕：移動系統滑鼠
- 「在目前位置點一下」：送出一次真實的滑鼠左鍵
- 「切換滑鼠穿透」：HUD 保持顯示，操作會落到後方視窗
- `F8`：切換互動／穿透模式
- `Esc` 或面板右上角按鈕：關閉 HUD

## 專案結構

- `HudForm.cs`：HUD 面板、游標覆蓋層與 Windows API 整合
- `wwwroot/index.html`：WebView2 控制面板 UI
- `HudWebViewDemo.csproj`：.NET 專案設定
- `啟動HUD.bat`：快速啟動

> 安全設計：只有在使用者操作面板時才會移動或點擊滑鼠；不包含自動連點、鍵盤控制或 MCP 連線。
