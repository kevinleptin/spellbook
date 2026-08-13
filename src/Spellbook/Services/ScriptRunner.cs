using System.Diagnostics;

namespace Spellbook.Services;

/// <summary>在新控制台窗口运行 PowerShell 脚本,异步返回退出码。</summary>
public static class ScriptRunner
{
    /// <summary>拼接命令行:-ExecutionPolicy Bypass -File "路径" 参数(参数原样拼接)。</summary>
    public static string BuildArguments(string scriptPath, string args)
    {
        var baseArgs = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
        return string.IsNullOrWhiteSpace(args) ? baseArgs : $"{baseArgs} {args}";
    }

    /// <summary>在资源管理器中打开文件夹。</summary>
    public static void OpenFolder(string folderPath)
        => Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });

    /// <summary>UseShellExecute=true 使脚本在新控制台窗口运行,不阻塞 UI。</summary>
    public static async Task<int> RunAsync(string scriptPath, string args)
    {
        var psi = new ProcessStartInfo("powershell.exe", BuildArguments(scriptPath, args))
        {
            UseShellExecute = true,
        };
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动 powershell.exe");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
