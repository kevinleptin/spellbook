using System.IO;
using System.Windows.Media;
using Spellbook.Models;
using Spellbook.Services;

namespace Spellbook.ViewModels;

/// <summary>磁贴对应的视图模型,包装一个 SpellItem。</summary>
public class ItemViewModel : ViewModelBase
{
    private bool _isVisible = true;
    private bool _pathMissing;

    public ItemViewModel(SpellItem model)
    {
        Model = model;
        RefreshPathMissing();
    }

    public SpellItem Model { get; }

    public string Name => Model.Name;
    public string ScriptPath => Model.ScriptPath;
    public string Arguments => Model.Arguments;
    public string Notes => Model.Notes;
    public string GroupName => Model.GroupName;

    /// <summary>磁贴图标(空/未知 Key 回退默认法术书)。</summary>
    public ImageSource IconImage => IconLoader.Get(Model.IconKey);

    /// <summary>磁贴标题:超过 4 个字符截断并加“…”。</summary>
    public string DisplayName =>
        Name.Length <= 4 ? Name : Name[..4] + "…";

    /// <summary>Tooltip 第二行:完整路径 + 参数。</summary>
    public string TooltipPathLine =>
        string.IsNullOrWhiteSpace(Arguments) ? ScriptPath : $"{ScriptPath} {Arguments}";

    /// <summary>备注为空时 Tooltip 不显示备注行。</summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    /// <summary>搜索过滤可见性。</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>脚本路径已失效时磁贴显示警告标记。</summary>
    public bool PathMissing
    {
        get => _pathMissing;
        set => SetProperty(ref _pathMissing, value);
    }

    /// <summary>条目指向文件夹时,点击 = 打开资源管理器。</summary>
    public bool IsFolder => Directory.Exists(ScriptPath);

    public void RefreshPathMissing() =>
        PathMissing = !File.Exists(ScriptPath) && !Directory.Exists(ScriptPath);

    /// <summary>编辑回填后刷新全部展示属性。</summary>
    public void RaiseAllChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(ScriptPath));
        OnPropertyChanged(nameof(Arguments));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(GroupName));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(TooltipPathLine));
        OnPropertyChanged(nameof(HasNotes));
        OnPropertyChanged(nameof(IconImage));
        RefreshPathMissing();
    }
}
