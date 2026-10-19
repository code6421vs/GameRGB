# Contributing to GameRGB

**繁體中文** | [English](CONTRIBUTING.en.md)

感謝協助改善 GameRGB。提交前請先閱讀 `AGENTS.md` 的 repository 慣例，以及 `docs/BUILDING.md` 的環境準備方式。

1. 從短而明確的 branch 開始，避免在同一個變更混入無關格式調整。
2. 執行 `setup.ps1 -SkipBuild` 準備本機依賴，但不要提交下載的二進位檔。
3. 至少執行 `dotnet build GameRgb/GameRgb.csproj -c Release`。
4. 涉及燈效或硬體時，測試開始、停止、狀態還原及廠商軟體重新接管。
5. Pull request 說明使用者可見行為、測試結果、受影響裝置；介面變更請附截圖。

提交訊息使用簡短祈使句，例如 `Add persisted lighting direction`。提交貢獻即表示你有權提供該內容，並同意依本專案的 GPL-3.0-or-later 授權發布。
