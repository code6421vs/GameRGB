# Repository Guidelines

## Project Structure & Module Organization

`GameRgb/` contains the .NET 10 WPF application. UI layout is defined in `MainWindow.xaml`, with event handling and application coordination in `MainWindow.xaml.cs`. Keep audio processing in `AudioCaptureService.cs`, frame generation in `LightingEngine.cs`, and hardware communication in `OpenRgbService.cs`. Supporting services manage the bundled OpenRGB host, RGB software conflicts, prerequisites, diagnostics, and persisted settings. Runtime assets are under `GameRgb/OpenRGB/` and `GameRgb/DriverInstallers/`; licensing notes belong in `THIRD_PARTY.md`. The solution entry point is `GameRgb.slnx`.

There is currently no test project. Add future automated tests under `GameRgb.Tests/`, mirroring production class names.

## Build, Test, and Development Commands

- `dotnet restore GameRgb.slnx` restores NuGet packages.
- `dotnet build GameRgb/GameRgb.csproj` builds the Windows desktop application and validates XAML.
- `dotnet run --project GameRgb/GameRgb.csproj` launches the app locally.
- `dotnet test GameRgb.slnx` runs all test projects once tests are added.

Run and hardware-test changes on Windows. RGB service control and PawnIO installation may require elevation.

## Coding Style & Naming Conventions

Use modern C# with nullable reference types enabled. Follow existing file-scoped namespaces and keep responsibilities separated by service. Use `PascalCase` for types, methods, and public properties; use `camelCase` for private fields, parameters, and locals. Suffix asynchronous methods with `Async` and pass cancellation tokens through long-running work. Keep XAML control names descriptive, such as `GradientColorPanel` or `GainSlider`. Avoid unrelated formatting changes in the compact existing source files.

## Testing Guidelines

For logic tests, prefer xUnit and name files `<ClassName>Tests.cs`; use method names that describe behavior, such as `Meter_RightToLeft_ReversesLedOrder`. Hardware-facing changes require a manual smoke test: start the app, confirm device discovery, run the test effect, stop and restore, then verify vendor RGB software regains control. Check `%LOCALAPPDATA%\GameRGB\GameRGB.log` for exceptions.

## Commit & Pull Request Guidelines

The repository has no commit history yet. Use short imperative subjects, for example `Add persisted lighting direction`. Keep commits focused. Pull requests should explain user-visible behavior, list build and manual test results, identify affected RGB devices, and include screenshots for layout changes. Link relevant issues when available.

## Configuration & Bundled Binaries

Do not commit personal `settings.json` or diagnostic logs. When replacing bundled OpenRGB or PawnIO files, verify redistribution terms, update `THIRD_PARTY.md`, and confirm the project still copies all runtime assets to the output directory.
