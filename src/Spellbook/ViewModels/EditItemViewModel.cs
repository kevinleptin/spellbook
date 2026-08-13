using System.IO;
using Spellbook.Models;

namespace Spellbook.ViewModels;

/// <summary>新建/编辑对话框共用的视图模型。</summary>
public class EditItemViewModel : ViewModelBase
{
    private string _name = "";
    private string _scriptPath = "";
    private string _arguments = "";
    private string _notes = "";
    private string _groupName = "";
    // 上一次自动填入的名称:名称仍等于它(或为空)时,换文件会继续自动跟随
    private string _lastAutoName = "";

    public EditItemViewModel(IEnumerable<string> existingGroups, SpellItem? editing = null)
    {
        ExistingGroups = existingGroups.ToList();
        if (editing is not null)
        {
            _name = editing.Name;
            _scriptPath = editing.ScriptPath;
            _arguments = editing.Arguments;
            _notes = editing.Notes;
            _groupName = editing.GroupName;
        }
    }

    /// <summary>已有分组名,供下拉选择(仍可输入新分组名)。</summary>
    public List<string> ExistingGroups { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value)) OnPropertyChanged(nameof(CanConfirm));
        }
    }

    public string ScriptPath
    {
        get => _scriptPath;
        set
        {
            if (SetProperty(ref _scriptPath, value)) OnPropertyChanged(nameof(CanConfirm));
        }
    }

    public string Arguments { get => _arguments; set => SetProperty(ref _arguments, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string GroupName { get => _groupName; set => SetProperty(ref _groupName, value); }

    public bool CanConfirm =>
        !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ScriptPath);

    /// <summary>选取脚本文件:名称为空或未被用户手改时,自动填入文件名(无扩展名)。</summary>
    public void SetScriptPath(string path)
    {
        ScriptPath = path;
        if (string.IsNullOrWhiteSpace(Name) || Name == _lastAutoName)
        {
            _lastAutoName = Path.GetFileNameWithoutExtension(path);
            Name = _lastAutoName;
        }
    }

    /// <summary>生成新条目模型(编辑场景由调用方把字段拷回原模型)。</summary>
    public SpellItem ToModel(int sortOrder) => new()
    {
        Name = Name.Trim(),
        ScriptPath = ScriptPath.Trim(),
        Arguments = Arguments.Trim(),
        Notes = Notes,
        GroupName = GroupName.Trim(),
        SortOrder = sortOrder,
    };
}
