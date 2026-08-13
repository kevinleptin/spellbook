namespace Spellbook.Models;

/// <summary>一个脚本快捷方式条目。</summary>
public class SpellItem
{
    public string Name { get; set; } = "";

    /// <summary>ps1 文件完整路径。</summary>
    public string ScriptPath { get; set; } = "";

    /// <summary>命令行参数,原样拼接,可为空。</summary>
    public string Arguments { get; set; } = "";

    /// <summary>备注,多行文本,可为空。</summary>
    public string Notes { get; set; } = "";

    /// <summary>分组名,空字符串表示“未分组”。</summary>
    public string GroupName { get; set; } = "";

    /// <summary>组内排序序号。</summary>
    public int SortOrder { get; set; }
}
