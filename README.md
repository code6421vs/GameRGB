# GameRGB

**繁體中文** | [English](README.en.md)

GameRGB 是 Windows 上的音訊反應式 RGB 控制器。它使用 NAudio 分析系統或指定程序的輸出音訊，再透過 OpenRGB SDK Server 控制主機板、ARGB 風扇、顯示卡與滑鼠等裝置。

> 本 repository **不包含** OpenRGB、PawnIO 或其他非 NuGet 二進位檔。`setup.ps1` 會從官方發布來源下載固定版本、驗證 SHA-256，再建立可執行的發佈目錄。

## 功能

- WASAPI 程序或全系統音訊擷取，搭配低／中／高頻 FFT 分析。
- 三頻區段、彩色 VU、單色／漸層整體脈動、音量環及固定色彩模式。
- 方向、顏色、增幅、餘暉、FPS、音訊來源及視窗位置／大小自動保存。
- 介面可即時切換繁體中文與 English；語系會自動保存，首次啟動則跟隨 Windows 顯示語言。
- 自動啟動本機 OpenRGB SDK Server，並在正常離開時還原裝置與 RGB 軟體。
- 首次啟動檢查 PawnIO；只有使用者同意後才會以系統管理員權限安裝。

## 系統需求

- Windows 10 2004（build 19041）或 Windows 11，x64。
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。只有執行預先建置版本時可改裝 .NET 10 Desktop Runtime。
- Windows PowerShell 5.1 或 PowerShell 7。
- 網路連線，用來下載 NuGet、OpenRGB 及 PawnIO。
- 建議安裝 [Microsoft Visual C++ 2015–2022 Redistributable (x64)](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)。
- 執行燈效與安裝 PawnIO 時需要接受 UAC 提示。

## 一鍵準備與編譯

下載並解壓縮 repository 後，直接雙擊 `setup.cmd`；或在 repository 根目錄執行：

```powershell
.\setup.ps1
```

腳本會下載並驗證 OpenRGB 1.1 portable 與 PawnIO 2.2.0、還原 NuGet 套件，最後以 `Release / win-x64 / framework-dependent` 發佈至：

```text
artifacts/GameRGB/GameRgb.exe
```

常用選項：

```powershell
.\setup.ps1 -Configuration Debug
.\setup.ps1 -SkipBuild        # 只準備非 NuGet 依賴
.\setup.ps1 -Force            # 重新展開已下載的依賴
```

下載檔案會快取在 `.deps-cache/`；OpenRGB 與 PawnIO 會放到被 Git 忽略的專案目錄，建置時再複製至輸出。腳本不會自行安裝 PawnIO。

## 首次啟動

1. 以系統管理員身分執行 `GameRgb.exe`。
2. 若硬體需要 SMBus／I2C 存取，可同意安裝已驗證簽章的 PawnIO；USB／HID 裝置通常可略過。
3. 等待 OpenRGB 掃描，勾選裝置、音訊來源與燈效，再按「開始燈效」。
4. 若裝置仍被 G HUB、GCC 或 Armoury Crate 控制，完整關閉衝突的 RGB 程式後重新掃描。

詳細步驟請見 [安裝說明](docs/INSTALLATION.md) 與 [建置說明](docs/BUILDING.md)。

## 授權

GameRGB 採 [GNU GPL 3.0 或更新版本](LICENSE) 授權。下載的 OpenRGB、PawnIO 與 NuGet 套件仍分別受原作者授權約束；來源、版本、用途與雜湊列於 [THIRD_PARTY.md](THIRD_PARTY.md)。GPL 不表示第三方作者為 GameRGB 提供背書。

## 硬體與還原提醒

ARGB 與多 LED 裝置可呈現逐燈效果；單區裝置只能顯示一個顏色。程式正常停止或關閉時會嘗試還原開始前狀態；若被工作管理員強制結束、電腦斷電或 OpenRGB 提前終止，可能需要重新啟動廠商服務或手動套用 OpenRGB 設定檔。診斷記錄位於 `%LOCALAPPDATA%\GameRGB\GameRGB.log`。
