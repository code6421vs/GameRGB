using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace GameRgb;

public sealed class OpenRgbHostManager : IAsyncDisposable
{
    Process? ownedProcess;
    bool connectedToWindowsService;

    public async Task<string> EnsureServerAsync(string host, int port, CancellationToken token = default)
    {
        if (await CanConnectAsync(host, port, TimeSpan.FromMilliseconds(500), token))
        {
            connectedToWindowsService = IsOpenRgbWindowsServiceRunning();
            return Loc.T("已連接現有的 OpenRGB Server。", "Connected to an existing OpenRGB Server.");
        }

        if (!IsLocalHost(host))
            throw new InvalidOperationException(Loc.T($"遠端 OpenRGB Server {host}:{port} 無法連線，GameRGB 不會在本機代為啟動遠端服務。", $"Could not connect to remote OpenRGB Server {host}:{port}; GameRGB will not start a local replacement for a remote server."));

        var executable = FindOpenRgbExecutable();
        var directory = Path.GetDirectoryName(executable)!;
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = directory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = $"--localconfig --server --server-host 127.0.0.1 --server-port {port}"
        }) ?? throw new InvalidOperationException(Loc.T("無法啟動本機的 OpenRGB Server。", "Could not start the local OpenRGB Server."));
        ownedProcess = process;
        DiagnosticsLog.Write($"已啟動本機 OpenRGB Server：{executable} (PID {process.Id})");

        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(45))
        {
            token.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                var exitCode = process.ExitCode;
                process.Dispose();
                ownedProcess = null;
                throw new InvalidOperationException(Loc.T($"OpenRGB Server 提前結束，結束碼：{exitCode}。", $"OpenRGB Server exited early with code {exitCode}."));
            }
            if (await CanConnectAsync("127.0.0.1", port, TimeSpan.FromMilliseconds(300), token))
            {
                // The SDK port opens before OpenRGB finishes asynchronous HID
                // discovery. Querying immediately produces a partial list (for
                // example ASUS exposes only its two onboard LEDs and the G502
                // has not registered yet). Let detection and controller config
                // settle before MainWindow asks for controller data.
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                if (process.HasExited)
                    throw new InvalidOperationException(Loc.T($"OpenRGB Server 在硬體掃描期間結束，結束碼：{process.ExitCode}。", $"OpenRGB Server exited during hardware discovery with code {process.ExitCode}."));
                return Loc.T("OpenRGB Server 已啟動並完成硬體掃描。", "OpenRGB Server started and completed hardware discovery.");
            }
            await Task.Delay(250, token);
        }

        await StopAsync();
        throw new TimeoutException(Loc.T("等待 OpenRGB Server 啟動逾時。", "Timed out waiting for OpenRGB Server to start."));
    }

    public async Task StopAsync()
    {
        var process = Interlocked.Exchange(ref ownedProcess, null);
        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try { await process.WaitForExitAsync(timeout.Token); }
                    catch (OperationCanceledException) { DiagnosticsLog.Write("停止 OpenRGB Server 逾時"); }
                }
                DiagnosticsLog.Write("OpenRGB Server 已停止");
            }
            finally { process.Dispose(); }
        }

        if (!connectedToWindowsService) return;
        connectedToWindowsService = false;
        var result = await Task.Run(() => RunServiceCommand("stop", "OpenRGB"));
        DiagnosticsLog.Write(result.ExitCode == 0
            ? "OpenRGB Windows 服務已停止，已釋放 RGB 裝置"
            : Loc.T("無法停止 OpenRGB Windows 服務：", "Could not stop the OpenRGB Windows service: ") + result.Message);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(Loc.T("無法停止 OpenRGB Windows 服務：", "Could not stop the OpenRGB Windows service: ") + result.Message);
    }

    static string FindOpenRgbExecutable()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "OpenRGB", "OpenRGB.exe");
        if (File.Exists(bundled)) return bundled;

        var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OpenRGB", "OpenRGB.exe");
        if (File.Exists(installed)) return installed;
        throw new FileNotFoundException(Loc.T("找不到下載或已安裝的 OpenRGB.exe。請先執行 setup.ps1，或安裝 OpenRGB。", "Downloaded or installed OpenRGB.exe was not found. Run setup.ps1 or install OpenRGB."), bundled);
    }

    static bool IsLocalHost(string host) => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host == "127.0.0.1" || host == "::1";

    static bool IsOpenRgbWindowsServiceRunning()
    {
        var result = RunServiceCommand("queryex", "OpenRGB");
        if (result.ExitCode != 0) return false;
        var match = Regex.Match(result.Message, @"PID\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        return match.Success && match.Groups[1].Value != "0";
    }

    static (int ExitCode, string Message) RunServiceCommand(string command, string serviceName)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
            ArgumentList = { command, serviceName },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException(Loc.T("無法執行 Windows 服務控制程式。", "Could not run the Windows service control utility."));
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(10000))
        {
            process.Kill(entireProcessTree: true);
            return (-1, Loc.T("服務操作逾時", "Service operation timed out"));
        }
        Task.WaitAll(output, error);
        var message = string.Join(" ", output.Result, error.Result).Replace('\r', ' ').Replace('\n', ' ').Trim();
        return (process.ExitCode, string.IsNullOrWhiteSpace(message) ? Loc.T($"sc.exe 結束碼 {process.ExitCode}", $"sc.exe exit code {process.ExitCode}") : message);
    }

    static async Task<bool> CanConnectAsync(string host, int port, TimeSpan timeout, CancellationToken token)
    {
        using var client = new TcpClient();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
        linked.CancelAfter(timeout);
        try
        {
            await client.ConnectAsync(host, port, linked.Token);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException) { return false; }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
