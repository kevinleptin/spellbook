namespace Spellbook.Core
{
    /// <summary>
    /// 一个启动器条目。字段名与 WPF 版(src/Spellbook/Models/SpellItem.cs)完全一致,
    /// 两版共读写 %APPDATA%\Spellbook\items.json,序列化为 PascalCase 属性名。
    /// </summary>
    public class SpellItem
    {
        public string Name { get; set; } = "";

        /// <summary>目标路径:ps1 / 文件夹 / 程序或文档 / http(s) 网址。</summary>
        public string ScriptPath { get; set; } = "";

        /// <summary>命令行参数,原样拼接,可为空。</summary>
        public string Arguments { get; set; } = "";

        /// <summary>备注,多行文本,可为空。</summary>
        public string Notes { get; set; } = "";

        /// <summary>分组名,空字符串表示"未分组"。</summary>
        public string GroupName { get; set; } = "";

        /// <summary>组内排序序号。</summary>
        public int SortOrder { get; set; }

        /// <summary>图标 Key(共用 src/ 的 163 个手绘图标),空表示默认 book。</summary>
        public string IconKey { get; set; } = "";
    }
}
