# Contributing to GameRGB

[繁體中文](CONTRIBUTING.md) | **English**

Thank you for helping improve GameRGB. Read `AGENTS.md` for repository conventions and `docs/BUILDING.en.md` for environment setup before contributing.

1. Use a short, focused branch and avoid unrelated formatting changes.
2. Run `setup.ps1 -SkipBuild` to prepare local dependencies, but never commit downloaded binaries.
3. Run at least `dotnet build GameRgb/GameRgb.csproj -c Release`.
4. For lighting or hardware changes, test start, stop, state restoration, and vendor-software takeover.
5. Pull requests should describe user-visible behavior, test results, affected devices, and include screenshots for UI changes.

Use short imperative commit subjects, such as `Add persisted lighting direction`. By contributing, you confirm that you have the right to provide the work and agree to release it under GPL-3.0-or-later.
