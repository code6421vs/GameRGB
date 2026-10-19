# 安裝與首次啟動

## 從原始碼建立可執行版本

先安裝 .NET 10 SDK、PowerShell 與 Microsoft Visual C++ 2015–2022 Redistributable (x64)。下載並解壓縮 repository 後雙擊 `setup.cmd`，或在 repository 根目錄執行：

```powershell
.\setup.ps1
```

腳本完成後，以系統管理員身分啟動：

```powershell
.\artifacts\GameRGB\GameRgb.exe
```

請保留整個 `artifacts/GameRGB/` 目錄，不要只複製 EXE；程式還需要旁邊的 DLL、`OpenRGB/` 與 `DriverInstallers/`。

## PawnIO

PawnIO 是第三方核心驅動，只在部分 SMBus／I2C 裝置需要。GameRGB 首次啟動時會偵測驅動；選擇安裝後，程式先驗證固定 SHA-256，再呼叫官方簽章安裝程式。安裝可能要求重新啟動 Windows。只控制 USB／HID 裝置時通常可略過。

## 更新與移除

更新原始碼後重新執行 `setup.ps1`。只有腳本內版本或雜湊更新時才需要重新下載依賴。移除 GameRGB 時刪除發佈目錄即可；使用者設定與記錄位於 `%LOCALAPPDATA%\GameRGB`。PawnIO 是系統驅動，不會隨刪除 GameRGB 自動移除。

## 常見問題

- **SHA-256 不符**：不要略過檢查。刪除 `.deps-cache/` 後重試，並確認代理伺服器或防毒軟體未替換下載內容。
- **OpenRGB 找不到裝置**：以系統管理員身分執行、安裝 PawnIO、關閉其他 RGB 軟體，再重新掃描。
- **OpenRGB.exe 無法啟動**：安裝 VC++ x64 runtime，並查看 `%LOCALAPPDATA%\GameRGB\GameRGB.log`。
