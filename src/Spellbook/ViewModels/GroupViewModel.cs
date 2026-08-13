using System.Collections.ObjectModel;

namespace Spellbook.ViewModels;

/// <summary>一个分组(含其磁贴集合)。Key 为空字符串表示“未分组”。</summary>
public class GroupViewModel : ViewModelBase
{
    private bool _isVisible = true;

    public GroupViewModel(string key) => Key = key;

    public string Key { get; }

    public string DisplayName => Key.Length == 0 ? "未分组" : Key;

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    /// <summary>搜索过滤后组内无匹配项时整组隐藏。</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }
}
