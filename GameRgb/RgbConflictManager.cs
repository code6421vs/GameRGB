using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace GameRgb;

/// <summary>
/// Temporarily releases ASUS RGB hardware from Aura while GameRGB is active.
/// Only vendor lighting services are paused while GameRGB owns the hardware.
/// Desktop applications, Armoury Crate and the ASUS fan service are untouched.
/// </summary>
public sealed class RgbConflictManager
{
    [DllImport("ntdll.dll")]
    static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll")]
    static extern int NtResumeProcess(IntPtr processHandle);

    static readonly (string ServiceName, string ProcessName)[] ManagedServices =
    [
        ("LightingService", "LightingService"),
        ("AorusLcdService", "AorusLcdService")
    ];

    readonly SemaphoreSlim gate = new(1, 1);
    readonly List<string> stoppedServices = [];
    readonly List<Process> suspendedProcesses = [];
    readonly List<string> suspendErrors = [];
    bool suspended;

    public bool IsSuspended => suspended;

    public async Task<IReadOnlyList<string>> SuspendAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (suspended) return suspendErrors.ToArray();

            // Mark the transition immediately so a window-close request that
            // arrives during suspension waits for, and then restores, this work.
            suspended = true;
            var errors = new List<string>();
            suspendErrors.Clear();
            stoppedServices.Clear();
            suspendedProcesses.Clear();

            // LightingService is Aura's RGB writer. Do not stop the wider
            // Armoury Crate stack; doing so can disturb device enumeration.
            foreach (var (serviceName, processName) in ManagedServices)
            {
                if (!IsProcessRunning(processName)) continue;
                stoppedServices.Add(serviceName);
                var result = RunServiceCommand("stop", serviceName);
                if (result.ExitCode == 0 && WaitForProcess(processName, running: false, TimeSpan.FromSeconds(10)))
                {
                    DiagnosticsLog.Write($"已暫停 RGB 服務：{serviceName}");
                }
                else
                {
                    // A successful stop can finish after our timeout, so retain
                    // that original state. A rejected command that left the
                    // process running made no change and needs no later restore.
                    if (result.ExitCode != 0 && IsProcessRunning(processName))
                        stoppedServices.Remove(serviceName);
                    errors.Add($"無法停止 {serviceName}：{result.Message}");
                }
            }

            // GCC hosts its Dynamic Lighting writer in this headless child.
            // Suspending it preserves the existing GCC session and avoids
            // launching a desktop window when GameRGB later releases control.
            foreach (var process in Process.GetProcessesByName("GBT_DL_LIB"))
            {
                try
                {
                    var status = NtSuspendProcess(process.Handle);
                    if (status != 0) throw new InvalidOperationException($"NtSuspendProcess 回傳 0x{status:X8}");
                    suspendedProcesses.Add(process);
                    DiagnosticsLog.Write($"已暫停 Gigabyte 背景燈效程序：GBT_DL_LIB ({process.Id})");
                }
                catch (Exception ex)
                {
                    process.Dispose();
                    errors.Add("無法暫停 Gigabyte 背景燈效程序：" + ex.Message);
                }
            }

            suspended = stoppedServices.Count > 0 || suspendedProcesses.Count > 0;
            suspendErrors.AddRange(errors);
            return errors;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<string>> RestoreAsync()
    {
        await gate.WaitAsync();
        try
        {
            var errors = new List<string>();

            for (var i = stoppedServices.Count - 1; i >= 0; i--)
            {
                var serviceName = stoppedServices[i];
                var processName = ManagedServices.First(item => item.ServiceName == serviceName).ProcessName;
                if (IsProcessRunning(processName))
                {
                    stoppedServices.RemoveAt(i);
                    continue;
                }
                var result = RunServiceCommand("start", serviceName);
                if (result.ExitCode == 0)
                {
                    DiagnosticsLog.Write($"已恢復 RGB 服務：{serviceName}");
                    stoppedServices.RemoveAt(i);
                }
                else errors.Add($"無法啟動 {serviceName}：{result.Message}");
            }

            for (var i = suspendedProcesses.Count - 1; i >= 0; i--)
            {
                var process = suspendedProcesses[i];
                try
                {
                    if (!process.HasExited)
                    {
                        var status = NtResumeProcess(process.Handle);
                        if (status != 0) throw new InvalidOperationException($"NtResumeProcess 回傳 0x{status:X8}");
                        DiagnosticsLog.Write($"已恢復 Gigabyte 背景燈效程序：GBT_DL_LIB ({process.Id})");
                    }
                    process.Dispose();
                    suspendedProcesses.RemoveAt(i);
                }
                catch (Exception ex)
                {
                    errors.Add("無法恢復 Gigabyte 背景燈效程序：" + ex.Message);
                }
            }

            suspended = stoppedServices.Count > 0 || suspendedProcesses.Count > 0;
            if (!suspended) suspendErrors.Clear();
            return errors;
        }
        finally
        {
            gate.Release();
        }
    }

    static bool IsProcessRunning(string name)
    {
        var processes = Process.GetProcessesByName(name);
        try { return processes.Length > 0; }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    static bool WaitForProcess(string name, bool running, TimeSpan timeout)
    {
        var until = Stopwatch.StartNew();
        while (until.Elapsed < timeout)
        {
            if (IsProcessRunning(name) == running) return true;
            Thread.Sleep(100);
        }
        return IsProcessRunning(name) == running;
    }

    static (int ExitCode, string Message) RunServiceCommand(string command, string serviceName)
    {
        var scPath = Path.Combine(Environment.SystemDirectory, "sc.exe");
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = scPath,
            ArgumentList = { command, serviceName },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("無法執行 Windows 服務控制程式。");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(10000))
        {
            process.Kill(entireProcessTree: true);
            return (-1, "服務操作逾時");
        }

        Task.WaitAll(outputTask, errorTask);
        var message = string.Join(" ", outputTask.Result, errorTask.Result)
            .Replace('\r', ' ').Replace('\n', ' ').Trim();
        return (process.ExitCode, string.IsNullOrWhiteSpace(message) ? $"sc.exe 結束碼 {process.ExitCode}" : message);
    }
}
