# Third-Party Notices

GameRGB 本身採 GPL-3.0-or-later。下列元件由各自作者提供，保留原著作權與授權條款；GameRGB 的授權不會取代第三方授權。Repository 不提交 OpenRGB 或 PawnIO 二進位檔，`setup.ps1` 僅從所列官方來源下載未修改的發布檔。

## OpenRGB 1.1

- Source and releases: https://openrgb.org/releases.html
- Source repository: https://gitlab.com/CalcProgrammer1/OpenRGB
- License: GNU General Public License version 2（請以發布檔內 `LICENSE` 為準）
- Purpose: 啟動 SDK Server，偵測並控制 RGB 硬體。
- Download: `OpenRGB_1.1_Windows_64_8e16de2.zip`
- SHA-256: `63291726C6FC84EB479EF2EE752E97E1187F6CE9C49FD27EEE40DED588A8172A`

GameRGB 的發佈目錄會在 `ThirdPartyLicenses/GPL-2.0.txt` 提供 GPLv2 全文。若重新散布組合後的 GameRGB 發布目錄，必須保留該文件，並遵守 OpenRGB 的 GPL 義務及提供對應原始碼的要求。

## PawnIO Setup 2.2.0

- Driver source: https://github.com/namazso/PawnIO
- Official installer releases: https://github.com/namazso/PawnIO.Setup/releases
- License: GNU General Public License version 2（請以來源 repository 的 `COPYING` 為準）
- Purpose: OpenRGB 存取部分 SMBus／I2C 主板、記憶體與顯示卡所需的簽章核心驅動。
- Download: `PawnIO_setup.exe`
- SHA-256: `1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032`
- Expected Authenticode subject: `CN=namazso.eu`

腳本只下載安裝程式；實際安裝一定由使用者在首次啟動時確認並接受 UAC。GameRGB 不使用 PawnIO 的 unrestricted/developer 模式。

## NuGet packages

| Package | Version | License | Project |
| --- | ---: | --- | --- |
| NAudio | 3.2.0 | MIT | https://github.com/naudio/NAudio |
| OpenRGB.NET | 3.1.1 | MIT | https://github.com/diogotr7/OpenRGB.NET |

NuGet 可能解析額外的相依套件；實際版本與授權請以 `obj/project.assets.json`、套件內容及 NuGet.org metadata 為準。

完整 MIT 授權與著作權聲明會隨成品放在 `ThirdPartyLicenses/NAudio-MIT.txt` 與 `ThirdPartyLicenses/OpenRGB.NET-MIT.txt`。
