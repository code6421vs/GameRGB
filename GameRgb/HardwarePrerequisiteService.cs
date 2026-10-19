using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace GameRgb;

public sealed class HardwarePrerequisiteService
{
    const string PawnIoServiceKey = @"SYSTEM\CurrentControlSet\Services\PawnIO";
    const string PawnIoInstallerSha256 = "1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032";

    public async Task<string> EnsurePawnIoAsync(Window owner)
    {
        if (IsPawnIoInstalled()) return Loc.T("PawnIO 硬體驅動已安裝。", "The PawnIO hardware driver is installed.");

        var answer = System.Windows.MessageBox.Show(owner,
            Loc.T("OpenRGB 的 SMBus／I2C 裝置需要官方 PawnIO 核心驅動。\n\n目前沒有偵測到 PawnIO。是否現在安裝官方已簽章版本？\n若略過，USB／HID 裝置仍可使用，但部分主板、記憶體或顯卡可能不會出現。",
                "OpenRGB requires the official PawnIO kernel driver for SMBus/I2C devices.\n\nPawnIO was not detected. Install the officially signed version now?\nIf skipped, USB/HID devices will still work, but some motherboards, memory, or graphics cards may not appear."),
            Loc.T("GameRGB · 安裝硬體驅動", "GameRGB · Install hardware driver"), System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (answer != System.Windows.MessageBoxResult.Yes) return Loc.T("已略過 PawnIO 安裝；部分 SMBus／I2C 裝置可能無法使用。", "PawnIO installation was skipped; some SMBus/I2C devices may be unavailable.");

        var installer = Path.Combine(AppContext.BaseDirectory, "DriverInstallers", "PawnIO_setup.exe");
        if (!File.Exists(installer)) throw new FileNotFoundException(Loc.T("找不到 PawnIO 驅動安裝程式。請先執行 setup.ps1 後重新建置。", "PawnIO installer not found. Run setup.ps1 and rebuild first."), installer);

        await using (var stream = File.OpenRead(installer))
        {
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
            if (!hash.Equals(PawnIoInstallerSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(Loc.T("PawnIO 安裝程式完整性驗證失敗，已取消安裝。", "PawnIO installer integrity verification failed; installation was canceled."));
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = installer,
            Arguments = "-install -silent",
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(installer)!
        }) ?? throw new InvalidOperationException(Loc.T("無法啟動 PawnIO 驅動安裝程式。", "Could not start the PawnIO driver installer."));

        await process.WaitForExitAsync();
        if (process.ExitCode is not 0 and not 3010)
            throw new InvalidOperationException(Loc.T($"PawnIO 驅動安裝失敗，結束碼：{process.ExitCode}。", $"PawnIO driver installation failed with exit code {process.ExitCode}."));
        if (!IsPawnIoInstalled()) throw new InvalidOperationException(Loc.T("PawnIO 安裝程式已結束，但仍未偵測到驅動服務。", "The PawnIO installer completed, but the driver service was not detected."));

        return process.ExitCode == 3010 ? Loc.T("PawnIO 已安裝，Windows 需要重新啟動後才能完整使用。", "PawnIO was installed. Restart Windows for full functionality.") : Loc.T("PawnIO 硬體驅動安裝完成。", "PawnIO hardware driver installation completed.");
    }

    static bool IsPawnIoInstalled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(PawnIoServiceKey, writable: false);
        return key is not null;
    }
}
