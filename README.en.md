# GameRGB

[繁體中文](README.md) | **English**

GameRGB is an audio-reactive RGB controller for Windows. It analyzes system-wide audio or audio from a selected process with NAudio, then controls motherboards, ARGB fans, graphics cards, mice, and other devices through the OpenRGB SDK Server.

> This repository does **not** contain OpenRGB, PawnIO, or other non-NuGet binaries. `setup.ps1` downloads pinned releases from official sources, verifies their SHA-256 hashes, and creates a runnable publish directory.

## Features

- Per-process or system-wide WASAPI capture with low-, mid-, and high-frequency FFT analysis.
- Three-band, multicolor VU, solid/gradient pulse, volume ring, and static color effects.
- Automatic persistence of direction, colors, gain, decay, FPS, audio source, and window placement.
- Live Traditional Chinese/English UI switching. The selection is saved; first launch follows the Windows display language.
- Automatic startup of a local OpenRGB SDK Server and best-effort restoration of RGB devices and vendor software on exit.
- First-run PawnIO detection; the signed driver is installed only after explicit user approval.

## Requirements

- Windows 10 version 2004 (build 19041) or Windows 11, x64.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The .NET 10 Desktop Runtime is sufficient only for a prebuilt release.
- Windows PowerShell 5.1 or PowerShell 7.
- Internet access for NuGet, OpenRGB, and PawnIO downloads.
- [Microsoft Visual C++ 2015–2022 Redistributable (x64)](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) is recommended.
- UAC approval is required for hardware control and PawnIO installation.

## One-click setup and build

Download and extract the repository, then double-click `setup.cmd`. Alternatively, run this from the repository root:

```powershell
.\setup.ps1
```

The script downloads and verifies OpenRGB 1.1 portable and PawnIO 2.2.0, restores NuGet packages, and creates a `Release / win-x64 / framework-dependent` publish at:

```text
artifacts/GameRGB/GameRgb.exe
```

Useful options:

```powershell
.\setup.ps1 -Configuration Debug
.\setup.ps1 -SkipBuild        # Prepare non-NuGet dependencies only
.\setup.ps1 -Force            # Re-extract downloaded dependencies
```

Downloads are cached in `.deps-cache/`. OpenRGB and PawnIO are placed in Git-ignored project directories and copied into the build output. The setup script does not install PawnIO.

## First run

1. Run `GameRgb.exe` as administrator.
2. Approve PawnIO installation if your SMBus/I2C hardware requires it. USB/HID devices usually do not.
3. Wait for OpenRGB discovery, select devices, an audio source, and an effect, then start the effect.
4. If G HUB, GCC, or Armoury Crate still controls a device, fully exit the conflicting RGB software and rescan.

See the [installation guide](docs/INSTALLATION.en.md) and [build guide](docs/BUILDING.en.md) for details.

## License

GameRGB is licensed under the [GNU GPL version 3 or later](LICENSE). Downloaded copies of OpenRGB, PawnIO, and NuGet packages remain governed by their respective licenses. Sources, versions, purposes, and hashes are documented in [THIRD_PARTY.en.md](THIRD_PARTY.en.md). The GPL does not imply endorsement by any third-party author.

## Hardware and restoration notes

ARGB and multi-LED devices can display per-LED effects; single-zone devices can display only one color at a time. GameRGB attempts to restore the original state after a normal stop or exit. Forced termination, power loss, or an early OpenRGB shutdown may require restarting vendor services or manually applying an OpenRGB profile. Diagnostics are written to `%LOCALAPPDATA%\GameRGB\GameRGB.log`.
