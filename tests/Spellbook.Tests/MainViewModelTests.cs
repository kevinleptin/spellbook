using System.IO;
using Spellbook.Models;
using Spellbook.Services;
using Spellbook.ViewModels;

namespace Spellbook.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly string _dir;
    private readonly string _file;

    public MainViewModelTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "SpellbookTests_" + Guid.NewGuid().ToString("N"));
        _file = Path.Combine(_dir, "items.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private MainViewModel NewVm() => new(new ItemStore(_file));

    private static SpellItem Item(string name, string group = "", int sort = 0) => new()
    {
        Name = name,
        ScriptPath = @"C:\scripts\" + name + ".ps1",
        GroupName = group,
        SortOrder = sort,
    };

    [Fact]
    public void AddItem_AppendsToGroupEnd_AndPersists()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "运维"));
        vm.AddItem(Item("b", "运维"));

        var reloaded = NewVm();
        var group = Assert.Single(reloaded.Groups);
        Assert.Equal(new[] { "a", "b" }, group.Items.Select(i => i.Name));
        Assert.True(group.Items[0].Model.SortOrder < group.Items[1].Model.SortOrder);
    }

    [Fact]
    public void Groups_UngroupedFirst_ThenFirstAppearance()
    {
        var vm = NewVm();
        vm.AddItem(Item("z1", "Z组"));
        vm.AddItem(Item("a1", "A组"));
        vm.AddItem(Item("free"));   // 未分组

        Assert.Equal(new[] { "未分组", "Z组", "A组" }, vm.Groups.Select(g => g.DisplayName));
        Assert.Equal("", vm.Groups[0].Key);
    }

    [Fact]
    public void Ungrouped_HiddenWhenEmpty()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "运维"));

        Assert.DoesNotContain(vm.Groups, g => g.Key == "");
    }

    [Fact]
    public void DeleteItem_RemovesAndPersists()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "运维"));
        vm.AddItem(Item("b", "运维"));

        vm.DeleteItem(vm.Groups[0].Items[0]);

        Assert.Equal(new[] { "b" }, vm.Groups[0].Items.Select(i => i.Name));
        Assert.Equal(new[] { "b" }, NewVm().Groups[0].Items.Select(i => i.Name));
    }

    [Fact]
    public void DeleteLastItem_RemovesGroup()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "运维"));

        vm.DeleteItem(vm.Groups[0].Items[0]);

        Assert.Empty(vm.Groups);
    }

    [Fact]
    public void MoveItem_AppendsToTargetGroupEnd()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "甲"));
        vm.AddItem(Item("b", "乙"));
        vm.AddItem(Item("c", "乙"));

        var a = vm.Groups.First(g => g.Key == "甲").Items[0];
        vm.MoveItemToGroup(a, "乙");

        Assert.DoesNotContain(vm.Groups, g => g.Key == "甲"); // 空组消失
        var target = Assert.Single(vm.Groups);
        Assert.Equal(new[] { "b", "c", "a" }, target.Items.Select(i => i.Name));
    }

    [Fact]
    public void ReorderBefore_InsertsAndRenumbers()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "组"));
        vm.AddItem(Item("b", "组"));
        vm.AddItem(Item("c", "组"));

        var group = vm.Groups[0];
        var c = group.Items.First(i => i.Name == "c");
        var a = group.Items.First(i => i.Name == "a");
        vm.ReorderBefore(c, a); // c 插到 a 前

        Assert.Equal(new[] { "c", "a", "b" }, group.Items.Select(i => i.Name));
        Assert.Equal(new[] { 0, 1, 2 }, group.Items.Select(i => i.Model.SortOrder));
        // 持久化
        Assert.Equal(new[] { "c", "a", "b" }, NewVm().Groups[0].Items.Select(i => i.Name));
    }

    [Fact]
    public void ReorderToGroupEnd_MovesToEnd()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "组"));
        vm.AddItem(Item("b", "组"));

        vm.MoveItemToGroup(vm.Groups[0].Items[0], "组"); // 同组移动 = 移到末尾

        Assert.Equal(new[] { "b", "a" }, vm.Groups[0].Items.Select(i => i.Name));
    }

    [Fact]
    public void Filter_CaseInsensitive_MatchesFullName()
    {
        var vm = NewVm();
        vm.AddItem(Item("Cleanup", "组"));
        vm.AddItem(Item("deploy", "组"));

        vm.SearchText = "CLEA"; // 匹配完整名称(而非截断显示名),不区分大小写

        var items = vm.Groups[0].Items;
        Assert.True(items.First(i => i.Name == "Cleanup").IsVisible);
        Assert.False(items.First(i => i.Name == "deploy").IsVisible);
    }

    [Fact]
    public void Filter_HidesGroupWithNoMatch()
    {
        var vm = NewVm();
        vm.AddItem(Item("aaa", "甲"));
        vm.AddItem(Item("bbb", "乙"));

        vm.SearchText = "aaa";

        Assert.True(vm.Groups.First(g => g.Key == "甲").IsVisible);
        Assert.False(vm.Groups.First(g => g.Key == "乙").IsVisible);
    }

    [Fact]
    public void Filter_Empty_ShowsAll()
    {
        var vm = NewVm();
        vm.AddItem(Item("aaa", "甲"));
        vm.AddItem(Item("bbb", "乙"));

        vm.SearchText = "aaa";
        vm.SearchText = "";

        Assert.All(vm.Groups, g => Assert.True(g.IsVisible));
        Assert.All(vm.Groups.SelectMany(g => g.Items), i => Assert.True(i.IsVisible));
    }

    [Fact]
    public void ReorderGroupBefore_MovesBlock_AndPersists()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "甲"));
        vm.AddItem(Item("b", "乙"));
        vm.AddItem(Item("c", "丙"));

        var bing = vm.Groups.First(g => g.Key == "丙");
        var jia = vm.Groups.First(g => g.Key == "甲");
        vm.ReorderGroupBefore(bing, jia);

        Assert.Equal(new[] { "丙", "甲", "乙" }, vm.Groups.Select(g => g.Key));
        Assert.Equal(new[] { "丙", "甲", "乙" }, NewVm().Groups.Select(g => g.Key));
    }

    [Fact]
    public void ReorderGroupBefore_UngroupedTarget_IsNoOp()
    {
        var vm = NewVm();
        vm.AddItem(Item("free"));
        vm.AddItem(Item("a", "甲"));
        vm.AddItem(Item("b", "乙"));

        var yi = vm.Groups.First(g => g.Key == "乙");
        vm.ReorderGroupBefore(yi, vm.Groups.First(g => g.Key == ""));

        Assert.Equal(new[] { "", "甲", "乙" }, vm.Groups.Select(g => g.Key));
    }

    [Fact]
    public void MoveGroup_UpAndDown_SwapsNeighbors_UngroupedPinned()
    {
        var vm = NewVm();
        vm.AddItem(Item("free"));
        vm.AddItem(Item("a", "甲"));
        vm.AddItem(Item("b", "乙"));
        vm.AddItem(Item("c", "丙"));

        vm.MoveGroup(vm.Groups.First(g => g.Key == "丙"), -1);
        Assert.Equal(new[] { "", "甲", "丙", "乙" }, vm.Groups.Select(g => g.Key));

        vm.MoveGroup(vm.Groups.First(g => g.Key == "甲"), -1); // 已是命名组第一,不能越过未分组
        Assert.Equal(new[] { "", "甲", "丙", "乙" }, vm.Groups.Select(g => g.Key));

        vm.MoveGroup(vm.Groups.First(g => g.Key == "甲"), 1);
        Assert.Equal(new[] { "", "丙", "甲", "乙" }, vm.Groups.Select(g => g.Key));
        // 持久化
        Assert.Equal(new[] { "", "丙", "甲", "乙" }, NewVm().Groups.Select(g => g.Key));
    }

    [Fact]
    public void CanMoveGroup_Boundaries()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "甲"));
        vm.AddItem(Item("b", "乙"));

        Assert.False(vm.CanMoveGroup(vm.Groups.First(g => g.Key == "甲"), -1));
        Assert.True(vm.CanMoveGroup(vm.Groups.First(g => g.Key == "甲"), 1));
        Assert.False(vm.CanMoveGroup(vm.Groups.First(g => g.Key == "乙"), 1));
    }

    [Fact]
    public void ApplyEdit_GroupChanged_MovesToNewGroupEnd()
    {
        var vm = NewVm();
        vm.AddItem(Item("a", "甲"));
        vm.AddItem(Item("b", "乙"));

        var a = vm.Groups.First(g => g.Key == "甲").Items[0];
        a.Model.Name = "a2";
        a.Model.GroupName = "乙";
        vm.ApplyEdit(a);

        var target = Assert.Single(vm.Groups);
        Assert.Equal("乙", target.Key);
        Assert.Equal(new[] { "b", "a2" }, target.Items.Select(i => i.Name));
    }
}
