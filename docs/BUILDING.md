# 建置指南

**繁體中文** | [English](BUILDING.en.md)

## 必要工具

- Windows 10 2004+ 或 Windows 11 x64
- .NET 10 SDK（確認：`dotnet --version`）
- Windows PowerShell 5.1 或 PowerShell 7
- Git（只有 clone 與提交時需要）
- Visual Studio 為選用；命令列 SDK 已可編譯 WPF

## 建議流程

```powershell
.\setup.ps1 -SkipBuild
dotnet restore .\GameRgb.slnx
dotnet build .\GameRgb\GameRgb.csproj -c Debug
dotnet run --project .\GameRgb\GameRgb.csproj
```

`-SkipBuild` 只準備不在 NuGet 的 OpenRGB 與 PawnIO。一般發佈使用：

```powershell
dotnet publish .\GameRgb\GameRgb.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\GameRGB
```

## 依賴更新

不要提交 `GameRgb/OpenRGB/`、`GameRgb/DriverInstallers/PawnIO_setup.exe`、`.deps-cache/` 或 `artifacts/`。升級下載依賴時：

1. 從官方發布頁選擇穩定版本。
2. 更新 `setup.ps1` 的版本、網址與 SHA-256。
3. 更新 `THIRD_PARTY.md` 的來源、版本、授權與雜湊。
4. 執行 `setup.ps1 -Force`，確認簽章、建置及硬體煙霧測試皆成功。

## 驗證

```powershell
dotnet build .\GameRgb\GameRgb.csproj -c Release
dotnet test .\GameRgb.slnx
```

目前尚無測試專案，因此 `dotnet test` 主要驗證 solution 可載入。硬體變更需手動測試：連線、開始燈效、停止與還原、正常關閉，再確認廠商 RGB 軟體能重新控制裝置。
