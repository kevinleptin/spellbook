using System.Diagnostics;
using System.IO;

namespace Spellbook.Services;

/// <summary>条目路径的启动方式分类。</summary>
public enum LaunchKind
{
    /// <summary>.ps1 → 新控制台运行 PowerShell,等待退出码。</summary>
    Script,

    /// <summary>已存在的目录 → 资源管理器打开。</summary>
    Folder,

    /// <summary>其他文件(exe、文档等)→ 带参数直接启动,不等待。</summary>
    Program,

    /// <summary>http(s) 网址 → 默认浏览器打开。</summary>
    Url,
}

/// <summary>在新控制台窗口运行 PowerShell 脚本,异步返回退出码。</summary>
public static class ScriptRunner
{
    /// <summary>按路径判断启动方式:网址 > 目录 > .ps1 脚本 > 其他程序/文档。</summary>
    public static LaunchKind GetLaunchKind(string path)
    {
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return LaunchKind.Url;
        }
        if (Directory.Exists(path)) return LaunchKind.Folder;
        return Path.GetExtension(path).Equals(".ps1", StringComparison.OrdinalIgnoreCase)
            ? LaunchKind.Script
            : LaunchKind.Program;
    }

    /// <summary>
    /// 启动程序/文档/网址:UseShellExecute=true 交给系统关联,不等待退出
    /// (Excel、浏览器等 GUI 程序可能长时间运行)。
    /// </summary>
    public static void Launch(string path, string args)
        => Process.Start(new ProcessStartInfo(path, args) { UseShellExecute = true });

    /// <summary>拼接命令行:-ExecutionPolicy Bypass -File "路径" 参数(参数原样拼接)。</summary>
    public static string BuildArguments(string scriptPath, string args)
    {
        var baseArgs = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
        return string.IsNullOrWhiteSpace(args) ? baseArgs : $"{baseArgs} {args}";
    }

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
