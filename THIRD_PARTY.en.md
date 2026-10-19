# Third-Party Notices

[繁體中文](THIRD_PARTY.md) | **English**

GameRGB itself is licensed under GPL-3.0-or-later. The following components are supplied by their respective authors and retain their original copyrights and licenses. The GameRGB license does not replace third-party terms. This repository does not commit OpenRGB or PawnIO binaries; `setup.ps1` downloads unmodified release files only from the official sources listed below.

## OpenRGB 1.1

- Source and releases: https://openrgb.org/releases.html
- Source repository: https://gitlab.com/CalcProgrammer1/OpenRGB
- License: GNU General Public License version 2; refer to the upstream license for authoritative terms
- Purpose: RGB hardware discovery and control through the SDK Server
- Download: `OpenRGB_1.1_Windows_64_8e16de2.zip`
- SHA-256: `63291726C6FC84EB479EF2EE752E97E1187F6CE9C49FD27EEE40DED588A8172A`

Published GameRGB output includes the GPLv2 text at `ThirdPartyLicenses/GPL-2.0.txt`. Anyone redistributing the combined directory must retain this file, comply with OpenRGB's GPL obligations, and make corresponding source available as required.

## PawnIO Setup 2.2.0

- Driver source: https://github.com/namazso/PawnIO
- Official installer releases: https://github.com/namazso/PawnIO.Setup/releases
- License: GNU General Public License version 2; refer to the source repository's `COPYING` file for authoritative terms
- Purpose: Signed kernel driver used by OpenRGB to access certain motherboard, memory, and graphics-card SMBus/I2C devices
- Download: `PawnIO_setup.exe`
- SHA-256: `1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032`
- Expected Authenticode subject: `CN=namazso.eu`

The script downloads the installer but does not install it. Installation requires explicit first-run approval and UAC consent. GameRGB does not use PawnIO's unrestricted/developer mode.

## NuGet packages

| Package | Version | License | Project |
| --- | ---: | --- | --- |
| NAudio | 3.2.0 | MIT | https://github.com/naudio/NAudio |
| OpenRGB.NET | 3.1.1 | MIT | https://github.com/diogotr7/OpenRGB.NET |

NuGet may resolve additional transitive dependencies. Refer to `obj/project.assets.json`, package contents, and NuGet.org metadata for the exact resolved versions and terms.

Complete MIT terms and copyright notices are published as `ThirdPartyLicenses/NAudio-MIT.txt` and `ThirdPartyLicenses/OpenRGB.NET-MIT.txt`.
