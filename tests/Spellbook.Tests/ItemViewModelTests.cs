using System.IO;
using Spellbook.Models;
using Spellbook.ViewModels;

namespace Spellbook.Tests;

public class ItemViewModelTests
{
    private static ItemViewModel Vm(string name = "n", string path = @"C:\s.ps1", string args = "", string notes = "")
        => new(new SpellItem { Name = name, ScriptPath = path, Arguments = args, Notes = notes });

    [Theory]
    [InlineData("清理")]
    [InlineData("abcd")]
    [InlineData("四个字符")]
    public void DisplayName_FourCharsOrLess_Unchanged(string name)
        => Assert.Equal(name, Vm(name).DisplayName);

    [Theory]
    [InlineData("清理临时文件", "清理临时…")]
    [InlineData("cleanup", "clea…")]
    public void DisplayName_OverFourChars_Truncated(string name, string expected)
        => Assert.Equal(expected, Vm(name).DisplayName);

    [Fact]
    public void TooltipPathLine_WithArgs_Appended()
        => Assert.Equal(@"C:\s.ps1 -a 1", Vm(args: "-a 1").TooltipPathLine);

    [Fact]
    public void TooltipPathLine_EmptyArgs_PathOnly()
        => Assert.Equal(@"C:\s.ps1", Vm().TooltipPathLine);

    [Fact]
    public void HasNotes_FalseWhenEmpty()
    {
        Assert.False(Vm().HasNotes);
        Assert.True(Vm(notes: "备注").HasNotes);
    }

    [Fact]
    public void FolderPath_NotMissing_AndIsFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "SpellbookTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var vm = Vm(path: dir);
            Assert.False(vm.PathMissing); // 文件夹路径视为有效目标
            Assert.True(vm.IsFolder);
        }
        finally
        {
            Directory.Delete(dir);
        }
    }

    [Fact]
    public void MissingPath_IsNeitherFileNorFolder()
    {
        var vm = Vm(path: @"C:\不存在的路径\xyz");
        Assert.True(vm.PathMissing);
        Assert.False(vm.IsFolder);
    }
}
