using Spellbook.Models;
using Spellbook.ViewModels;

namespace Spellbook.Tests;

public class EditItemViewModelTests
{
    [Fact]
    public void SetScriptPath_FillsNameFromFileName()
    {
        var vm = new EditItemViewModel(new[] { "运维" });

        vm.SetScriptPath(@"C:\scripts\cleanup.ps1");

        Assert.Equal("cleanup", vm.Name);
        Assert.Equal(@"C:\scripts\cleanup.ps1", vm.ScriptPath);
    }

    [Fact]
    public void SetScriptPath_KeepsUserEditedName()
    {
        var vm = new EditItemViewModel(Array.Empty<string>());
        vm.SetScriptPath(@"C:\scripts\cleanup.ps1");

        vm.Name = "我的清理"; // 用户手改
        vm.SetScriptPath(@"C:\scripts\other.ps1");

        Assert.Equal("我的清理", vm.Name);
    }

    [Fact]
    public void SetScriptPath_RefillsWhenNameStillAuto()
    {
        var vm = new EditItemViewModel(Array.Empty<string>());

        vm.SetScriptPath(@"C:\scripts\cleanup.ps1");
        vm.SetScriptPath(@"C:\scripts\other.ps1"); // 未手改名称,跟随新文件

        Assert.Equal("other", vm.Name);
    }

    [Fact]
    public void CanConfirm_RequiresNameAndPath()
    {
        var vm = new EditItemViewModel(Array.Empty<string>());
        Assert.False(vm.CanConfirm);

        vm.SetScriptPath(@"C:\scripts\a.ps1");
        Assert.True(vm.CanConfirm);

        vm.Name = "";
        Assert.False(vm.CanConfirm);
    }

    [Fact]
    public void NewItem_DefaultsToBookIcon()
        => Assert.Equal("book", new EditItemViewModel(Array.Empty<string>()).IconKey);

    [Fact]
    public void ToModel_CarriesIconKey()
    {
        var vm = new EditItemViewModel(Array.Empty<string>());
        vm.SetScriptPath(@"C:\s\a.ps1");
        vm.IconKey = "fireball";

        Assert.Equal("fireball", vm.ToModel(0).IconKey);
    }

    [Fact]
    public void Editing_PrefillsIconKey()
    {
        var item = new SpellItem { Name = "n", ScriptPath = @"C:\a.ps1", IconKey = "skull" };
        Assert.Equal("skull", new EditItemViewModel(Array.Empty<string>(), item).IconKey);
    }

    [Fact]
    public void Editing_EmptyIconKey_FallsBackToBook()
    {
        var item = new SpellItem { Name = "n", ScriptPath = @"C:\a.ps1" };
        Assert.Equal("book", new EditItemViewModel(Array.Empty<string>(), item).IconKey);
    }

    [Fact]
    public void Editing_PrefillsAllFields()
    {
        var item = new SpellItem
        {
            Name = "部署",
            ScriptPath = @"C:\s\deploy.ps1",
            Arguments = "-env prod",
            Notes = "小心",
            GroupName = "运维",
        };

        var vm = new EditItemViewModel(new[] { "运维" }, item);

        Assert.Equal("部署", vm.Name);
        Assert.Equal(@"C:\s\deploy.ps1", vm.ScriptPath);
        Assert.Equal("-env prod", vm.Arguments);
        Assert.Equal("小心", vm.Notes);
        Assert.Equal("运维", vm.GroupName);
    }

    [Fact]
    public void ApplyTo_CopiesAllEditableFields_IncludingIconKey()
    {
        var original = new SpellItem
        {
            Name = "旧名", ScriptPath = @"C:\old.ps1", Arguments = "-old",
            Notes = "旧备注", GroupName = "旧组", SortOrder = 5, IconKey = "book",
        };
        var vm = new EditItemViewModel(Array.Empty<string>(), original)
        {
            Name = "新名", Arguments = "-new", Notes = "新备注",
            GroupName = "新组", IconKey = "fireball",
        };
        vm.SetScriptPath(@"C:\new.ps1");

        vm.ApplyTo(original);

        Assert.Equal("新名", original.Name);
        Assert.Equal(@"C:\new.ps1", original.ScriptPath);
        Assert.Equal("-new", original.Arguments);
        Assert.Equal("新备注", original.Notes);
        Assert.Equal("新组", original.GroupName);
        Assert.Equal("fireball", original.IconKey); // 编辑改图标必须生效
        Assert.Equal(5, original.SortOrder);        // 排序号不被编辑覆盖
    }

    [Fact]
    public void ToModel_CopiesFieldsAndSortOrder()
    {
        var vm = new EditItemViewModel(Array.Empty<string>());
        vm.SetScriptPath(@"C:\s\a.ps1");
        vm.Arguments = "-x";
        vm.Notes = "n";
        vm.GroupName = " 运维 "; // 分组名去首尾空白

        var model = vm.ToModel(7);

        Assert.Equal("a", model.Name);
        Assert.Equal("运维", model.GroupName);
        Assert.Equal(7, model.SortOrder);
    }
}
