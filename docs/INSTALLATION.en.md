# Installation and First Run

[繁體中文](INSTALLATION.md) | **English**

## Build from source

Install the .NET 10 SDK, PowerShell, and Microsoft Visual C++ 2015–2022 Redistributable (x64). Download and extract the repository, then double-click `setup.cmd`, or run:

```powershell
.\setup.ps1
```

When setup finishes, run the following executable as administrator:

```powershell
.\artifacts\GameRGB\GameRgb.exe
```

Keep the complete `artifacts/GameRGB/` directory. Do not copy only the EXE; GameRGB also needs the adjacent DLLs, `OpenRGB/`, and `DriverInstallers/` directories.

## PawnIO

PawnIO is a third-party kernel driver required only for some SMBus/I2C devices. GameRGB checks for it at first run. If installation is approved, GameRGB verifies the pinned SHA-256 before launching the officially signed installer. Windows may require a restart. PawnIO can usually be skipped when controlling only USB/HID devices.

## Updating and uninstalling

Run `setup.ps1` again after updating the source. Dependencies are downloaded again only when their pinned versions or hashes change. Delete the publish directory to remove GameRGB. User settings and logs are stored in `%LOCALAPPDATA%\GameRGB`. PawnIO is a system driver and is not removed automatically with GameRGB.

## Troubleshooting

- **SHA-256 mismatch:** Do not bypass verification. Delete `.deps-cache/`, retry, and check whether a proxy or antivirus product is replacing downloads.
- **OpenRGB finds no devices:** Run as administrator, install PawnIO when required, close other RGB applications, and rescan.
- **OpenRGB.exe does not start:** Install the VC++ x64 runtime and inspect `%LOCALAPPDATA%\GameRGB\GameRGB.log`.
