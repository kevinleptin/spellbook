using UnityEditor;
using UnityEngine;

namespace Spellbook.EditorTools
{
    /// <summary>
    /// 一键构建 Windows 版:菜单 Spellbook/构建 Windows 版,
    /// 或命令行 -batchmode -executeMethod Spellbook.EditorTools.BuildScript.Build。
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Spellbook/构建 Windows 版")]
        public static void Build()
        {
            ProjectSetup.EnsureAll();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Windows/Spellbook Arcane.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            Debug.Log($"构建结果: {summary.result}, 大小 {summary.totalSize / (1024 * 1024)} MB, " +
                      $"输出 {summary.outputPath}");
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                // 批处理模式下让退出码非 0
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
