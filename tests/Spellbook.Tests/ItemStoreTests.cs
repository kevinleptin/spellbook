using System.IO;
using Spellbook.Models;
using Spellbook.Services;

namespace Spellbook.Tests;

public class ItemStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _file;

    public ItemStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "SpellbookTests_" + Guid.NewGuid().ToString("N"));
        _file = Path.Combine(_dir, "items.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void SaveThenLoad_Roundtrips()
    {
        var store = new ItemStore(_file);
        var items = new List<SpellItem>
        {
            new()
            {
                Name = "清理临时文件",
                ScriptPath = @"C:\scripts\cleanup.ps1",
                Arguments = "-Days 7",
                Notes = "第一行\n第二行",
                GroupName = "运维",
                SortOrder = 3,
            },
        };

        store.Save(items);
        var loaded = new ItemStore(_file).Load();

        var item = Assert.Single(loaded);
        Assert.Equal("清理临时文件", item.Name);
        Assert.Equal(@"C:\scripts\cleanup.ps1", item.ScriptPath);
        Assert.Equal("-Days 7", item.Arguments);
        Assert.Equal("第一行\n第二行", item.Notes);
        Assert.Equal("运维", item.GroupName);
        Assert.Equal(3, item.SortOrder);
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyAndCreatesFile()
    {
        var store = new ItemStore(_file);

        var loaded = store.Load();

        Assert.Empty(loaded);
        Assert.False(store.LoadFailed);
        Assert.True(File.Exists(_file)); // 文件不存在时自动创建
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsEmptyAndSetsLoadFailed()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_file, "{not json");
        var store = new ItemStore(_file);

        var loaded = store.Load();

        Assert.Empty(loaded);
        Assert.True(store.LoadFailed);
        // 不覆盖原文件
        Assert.Equal("{not json", File.ReadAllText(_file));
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        var nested = Path.Combine(_dir, "a", "b", "items.json");
        var store = new ItemStore(nested);

        store.Save(new List<SpellItem>());

        Assert.True(File.Exists(nested));
    }
}
