using System;
using System.Diagnostics;
using System.IO;

namespace Spellbook.Core
{
    /// <summary>条目路径的启动方式分类,与 WPF 版 ScriptRunner 保持一致。</summary>
    public enum LaunchKind
    {
        /// <summary>.ps1 → 新控制台运行 PowerShell,可取退出码。</summary>
        Script,

        /// <summary>已存在的目录 → 资源管理器打开。</summary>
        Folder,

        /// <summary>其他文件(exe、文档等)→ 带参数直接启动,不等待。</summary>
        Program,

        /// <summary>http(s) 网址 → 默认浏览器打开。</summary>
        Url,
    }

    /// <summary>进程启动逻辑,纯 C#,无 Unity 依赖(可被 EditMode 测试覆盖)。</summary>
    public static class Launcher
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

        /// <summary>网址条目不做存在性检查,其余检查文件/目录是否存在。</summary>
        public static bool TargetMissing(string path) =>
            GetLaunchKind(path) != LaunchKind.Url
            && !File.Exists(path) && !Directory.Exists(path);

        /// <summary>拼接 PowerShell 命令行,与 WPF 版 BuildArguments 一致。</summary>
        public static string BuildPsArguments(string scriptPath, string args)
        {
            var baseArgs = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
            return string.IsNullOrWhiteSpace(args) ? baseArgs : $"{baseArgs} {args}";
        }

        /// <summary>
        /// 运行 .ps1:新控制台窗口,返回 Process 供调用方轮询退出码;
        /// 启动失败抛异常由调用方提示。
        /// </summary>
        public static Process StartScript(string scriptPath, string args)
        {
            var psi = new ProcessStartInfo("powershell.exe", BuildPsArguments(scriptPath, args))
            {
                UseShellExecute = true,
            };
            return Process.Start(psi)
                   ?? throw new InvalidOperationException("无法启动 powershell.exe");
        }

        /// <summary>启动程序/文档/文件夹/网址:交给系统关联,不等待退出。</summary>
        public static void Launch(string path, string args)
            => Process.Start(new ProcessStartInfo(path, args) { UseShellExecute = true });
    }
}
