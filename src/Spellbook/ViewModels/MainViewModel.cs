using System.Collections.ObjectModel;
using System.IO;
using Spellbook.Models;
using Spellbook.Services;

namespace Spellbook.ViewModels;

/// <summary>
/// 主窗口视图模型:持有全部条目,负责分组、排序、搜索过滤与持久化。
/// 分组顺序:“未分组”恒最上(仅非空时显示),其余按首次出现顺序。
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ItemStore _store;
    private List<SpellItem> _items;
    // 保持 Model → VM 的稳定映射,重建分组时不丢失 IsVisible/PathMissing 状态
    private readonly Dictionary<SpellItem, ItemViewModel> _vmCache = new();
    // 分组 VM 同样按 Key 复用,避免每次变更后 UI 整组重建、引用陈旧
    private readonly Dictionary<string, GroupViewModel> _groupCache = new();

    private string _searchText = "";
    private string _statusText = "就绪";
    private bool _isStatusError;

    public MainViewModel(ItemStore store)
    {
        _store = store;
        _items = store.Load();
        StoreLoadFailed = store.LoadFailed;
        RebuildGroups();
    }

    public ObservableCollection<GroupViewModel> Groups { get; } = new();

    /// <summary>items.json 损坏时为 true,视图层据此提示用户。</summary>
    public bool StoreLoadFailed { get; }

    /// <summary>已有的非空分组名(按显示顺序),供“移动”菜单与编辑对话框使用。</summary>
    public IEnumerable<string> ExistingGroupNames =>
        Groups.Where(g => g.Key.Length > 0).Select(g => g.Key);

    /// <summary>搜索文本:实时按完整名称过滤,不区分大小写。</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value)) ApplyFilter();
        }
    }

    /// <summary>状态栏文本(最近一次运行)。</summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>退出码非 0 或运行出错时整行红色。</summary>
    public bool IsStatusError
    {
        get => _isStatusError;
        set => SetProperty(ref _isStatusError, value);
    }

    /// <summary>新增条目:排在对应分组末尾并保存。</summary>
    public void AddItem(SpellItem item)
    {
        item.SortOrder = NextSortOrder(item.GroupName);
        _items.Add(item);
        SaveAndRebuild();
    }

    /// <summary>编辑确认后调用(Model 字段已被对话框回填)。若分组变化则排到新组末尾。</summary>
    public void ApplyEdit(ItemViewModel item)
    {
        var currentGroup = Groups.FirstOrDefault(g => g.Items.Contains(item));
        if (currentGroup is not null && currentGroup.Key != item.Model.GroupName)
        {
            item.Model.SortOrder = NextSortOrder(item.Model.GroupName);
        }
        SaveAndRebuild();
        item.RaiseAllChanged();
    }

    public void DeleteItem(ItemViewModel item)
    {
        _items.Remove(item.Model);
        _vmCache.Remove(item.Model);
        SaveAndRebuild();
    }

    /// <summary>移动到指定分组末尾(同组移动即移到组尾)。</summary>
    public void MoveItemToGroup(ItemViewModel item, string groupKey)
    {
        item.Model.GroupName = groupKey;
        item.Model.SortOrder = NextSortOrder(groupKey, exclude: item.Model);
        SaveAndRebuild();
    }

    /// <summary>同分组内拖拽:把 source 插到 target 之前,组内序号重排为 0..n-1。</summary>
    public void ReorderBefore(ItemViewModel source, ItemViewModel target)
    {
        if (source == target || source.Model.GroupName != target.Model.GroupName) return;

        var group = _items
            .Where(i => i.GroupName == source.Model.GroupName)
            .OrderBy(i => i.SortOrder)
            .ToList();
        group.Remove(source.Model);
        group.Insert(group.IndexOf(target.Model), source.Model);
        for (var i = 0; i < group.Count; i++) group[i].SortOrder = i;

        SaveAndRebuild();
    }

    /// <summary>
    /// 运行脚本:路径不存在返回 false(并标记磁贴警告);
    /// 否则启动新控制台运行,退出码异步回填状态栏。
    /// </summary>
    public async Task<bool> RunItemAsync(ItemViewModel item)
    {
        item.RefreshPathMissing();
        if (item.PathMissing) return false;

        // 非脚本条目(文件夹/程序/文档/网址):交给系统启动,不等待退出
        if (item.LaunchKind != LaunchKind.Script)
        {
            try
            {
                ScriptRunner.Launch(item.ScriptPath,
                    item.LaunchKind == LaunchKind.Folder ? "" : item.Arguments);
                IsStatusError = false;
                StatusText = item.LaunchKind switch
                {
                    LaunchKind.Folder => $"{item.Name} 已打开文件夹",
                    LaunchKind.Url => $"{item.Name} 已在浏览器打开",
                    _ => $"{item.Name} 已启动",
                };
            }
            catch (Exception ex)
            {
                StatusText = $"{item.Name} 启动失败: {ex.Message}";
                IsStatusError = true;
            }
            return true;
        }

        try
        {
            IsStatusError = false;
            StatusText = $"{item.Name} 运行中…";
            var exitCode = await ScriptRunner.RunAsync(item.ScriptPath, item.Arguments);
            StatusText = $"{item.Name} 退出码 {exitCode}";
            IsStatusError = exitCode != 0;
        }
        catch (Exception ex)
        {
            // 启动失败(如系统缺 powershell.exe)也不崩溃,红色提示
            StatusText = $"{item.Name} 启动失败: {ex.Message}";
            IsStatusError = true;
        }
        return true;
    }

    /// <summary>分组排序:把 source 组整块插到 target 组之前("未分组"不可拖动也不可为目标)。</summary>
    public void ReorderGroupBefore(GroupViewModel source, GroupViewModel target)
    {
        if (source == target || source.Key.Length == 0 || target.Key.Length == 0) return;

        var keys = NamedGroupKeys();
        if (!keys.Remove(source.Key)) return;
        var index = keys.IndexOf(target.Key);
        if (index < 0) return;
        keys.Insert(index, source.Key);
        ApplyGroupOrder(keys);
    }

    /// <summary>分组能否上移/下移(delta = -1/1);"未分组"恒定置顶不可移动。</summary>
    public bool CanMoveGroup(GroupViewModel group, int delta)
    {
        if (group.Key.Length == 0) return false;
        var keys = NamedGroupKeys();
        var index = keys.IndexOf(group.Key);
        var to = index + delta;
        return index >= 0 && to >= 0 && to < keys.Count;
    }

    /// <summary>分组上移/下移一位,越界为空操作。</summary>
    public void MoveGroup(GroupViewModel group, int delta)
    {
        if (!CanMoveGroup(group, delta)) return;
        var keys = NamedGroupKeys();
        var index = keys.IndexOf(group.Key);
        keys.RemoveAt(index);
        keys.Insert(index + delta, group.Key);
        ApplyGroupOrder(keys);
    }

    private List<string> NamedGroupKeys() =>
        Groups.Select(g => g.Key).Where(k => k.Length > 0).ToList();

    /// <summary>按新的命名分组顺序重排主列表组块并保存("未分组"块恒在最前)。</summary>
    private void ApplyGroupOrder(List<string> namedKeys)
    {
        _items = _items.Where(i => i.GroupName.Length == 0)
            .Concat(namedKeys.SelectMany(k => _items.Where(i => i.GroupName == k)))
            .ToList();
        SaveAndRebuild();
    }

    /// <summary>下一个组尾序号(可排除某条,用于移动自身)。</summary>
    private int NextSortOrder(string groupKey, SpellItem? exclude = null)
    {
        var group = _items.Where(i => i.GroupName == groupKey && i != exclude).ToList();
        return group.Count == 0 ? 0 : group.Max(i => i.SortOrder) + 1;
    }

    private void SaveAndRebuild()
    {
        RebuildGroups();
        _store.Save(_items); // 重建时已归一化主列表(组块 + 组内序),保存该顺序
    }

    /// <summary>按规则重建分组集合,并把主列表归一化为“组块顺序 + 组内 SortOrder”。</summary>
    private void RebuildGroups()
    {
        // 分组键顺序:未分组最前,其余按首次出现
        var keys = new List<string>();
        if (_items.Any(i => i.GroupName.Length == 0)) keys.Add("");
        foreach (var item in _items)
        {
            if (item.GroupName.Length > 0 && !keys.Contains(item.GroupName)) keys.Add(item.GroupName);
        }

        Groups.Clear();
        var normalized = new List<SpellItem>();
        foreach (var key in keys)
        {
            if (!_groupCache.TryGetValue(key, out var group))
            {
                group = new GroupViewModel(key);
                _groupCache[key] = group;
            }
            group.Items.Clear();
            foreach (var model in _items.Where(i => i.GroupName == key).OrderBy(i => i.SortOrder))
            {
                normalized.Add(model);
                if (!_vmCache.TryGetValue(model, out var vm))
                {
                    vm = new ItemViewModel(model);
                    _vmCache[model] = vm;
                }
                group.Items.Add(vm);
            }
            Groups.Add(group);
        }
        _items = normalized;

        ApplyFilter();
    }

    /// <summary>按完整名称过滤(不区分大小写);组内全部隐藏时整组隐藏。</summary>
    private void ApplyFilter()
    {
        var text = _searchText.Trim();
        foreach (var group in Groups)
        {
            var anyVisible = false;
            foreach (var item in group.Items)
            {
                item.IsVisible = text.Length == 0 ||
                                 item.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
                anyVisible |= item.IsVisible;
            }
            group.IsVisible = anyVisible;
        }
    }
}
