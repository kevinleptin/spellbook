using System.IO;
using Spellbook.Services;

namespace Spellbook.Tests;

public class IconAssetsTests
{
    /// <summary>从测试输出目录向上找仓库根(含 .git 的目录)。</summary>
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            dir = dir.Parent!;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    public static string IconsDir() =>
        Path.Combine(RepoRoot(), "src", "Spellbook", "Assets", "Icons");

    [Fact]
    public void Library_Has100UniqueKeys()
    {
        Assert.Equal(100, IconLibrary.All.Count);
        Assert.Equal(100, IconLibrary.All.Select(i => i.Key).Distinct().Count());
        Assert.All(IconLibrary.All, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(i.Category));
        });
    }

    [Fact]
    public void EveryIconFileExistsOnDisk()
    {
        var dir = IconsDir();
        foreach (var icon in IconLibrary.All)
        {
            Assert.True(File.Exists(Path.Combine(dir, icon.Key + ".png")),
                $"缺少图标文件: {icon.Key}.png");
        }
        // 目录中也不应有清单之外的多余图标文件
        var files = Directory.GetFiles(dir, "*.png").Select(Path.GetFileNameWithoutExtension);
        Assert.Empty(files.Except(IconLibrary.All.Select(i => i.Key)));
    }

    [Fact]
    public void Book_IsFirstIcon()
        => Assert.Equal("book", IconLibrary.All[0].Key);
}
