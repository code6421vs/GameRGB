# Security Policy

**繁體中文** | [English](SECURITY.en.md)

## Reporting a vulnerability

請不要在公開 issue 貼出可利用細節、憑證或完整診斷記錄。請透過 GitHub repository 的 **Security → Report a vulnerability** 私下回報；若尚未啟用 private vulnerability reporting，請聯絡 repository 擁有者並只提供最少必要資訊。

## Binary dependencies

GameRGB 會下載並執行 OpenRGB，以及在使用者同意後安裝 PawnIO 核心驅動。`setup.ps1` 固定官方 HTTPS 網址與 SHA-256，並檢查 PawnIO Authenticode 簽章。若任何驗證失敗，請停止操作，不要關閉檢查或從第三方鏡像取得檔案。

更新依賴時，維護者必須重新核對官方發布頁、授權、雜湊與簽章，並同步修改 `THIRD_PARTY.md`。
