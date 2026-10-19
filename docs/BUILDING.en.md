# Build Guide

[繁體中文](BUILDING.md) | **English**

## Prerequisites

- Windows 10 version 2004+ or Windows 11 x64
- .NET 10 SDK (verify with `dotnet --version`)
- Windows PowerShell 5.1 or PowerShell 7
- Git, when cloning or contributing
- Visual Studio is optional; the command-line SDK can build the WPF project

## Development workflow

```powershell
.\setup.ps1 -SkipBuild
dotnet restore .\GameRgb.slnx
dotnet build .\GameRgb\GameRgb.csproj -c Debug
dotnet run --project .\GameRgb\GameRgb.csproj
```

`-SkipBuild` prepares OpenRGB and PawnIO, which are not NuGet dependencies. To publish a normal release:

```powershell
dotnet publish .\GameRgb\GameRgb.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\GameRGB
```

## Updating dependencies

Do not commit `GameRgb/OpenRGB/`, `GameRgb/DriverInstallers/PawnIO_setup.exe`, `.deps-cache/`, or `artifacts/`. When upgrading a downloaded dependency:

1. Select a stable version from its official release page.
2. Update the version, URL, and SHA-256 in `setup.ps1`.
3. Update both third-party notice files with the source, version, license, and hash.
4. Run `setup.ps1 -Force`, then verify the signature, build, and hardware smoke test.

## Verification

```powershell
dotnet build .\GameRgb\GameRgb.csproj -c Release
dotnet test .\GameRgb.slnx
```

There is currently no test project, so `dotnet test` mainly verifies that the solution loads. Hardware changes require a manual test: connect, start an effect, stop and restore, exit normally, and confirm that vendor RGB software can regain control.
