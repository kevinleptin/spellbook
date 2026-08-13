using System;
using System.Collections.Generic;
using System.Linq;

namespace Spellbook.Core
{
    /// <summary>
    /// 领域模型:持有全部条目,负责分组/排序/增删改与持久化。
    /// 逻辑与 WPF 版 MainViewModel 对齐:"未分组"恒最前(仅非空时出现),
    /// 其余分组按首次出现顺序;任何变更后立即归一化并保存。
    /// </summary>
    public class SpellbookModel
    {
        private readonly ItemStore _store;
        private List<SpellItem> _items;

        public SpellbookModel(ItemStore store)
        {
            _store = store;
            _items = store.Load();
            LoadFailed = store.LoadFailed;
            Normalize();
        }

        public bool LoadFailed { get; }

        /// <summary>分组键顺序:未分组("")最前,其余按首次出现。</summary>
        public List<string> GroupKeys()
        {
            var keys = new List<string>();
            if (_items.Any(i => i.GroupName.Length == 0)) keys.Add("");
            foreach (var item in _items.Where(i => i.GroupName.Length > 0))
            {
                if (!keys.Contains(item.GroupName)) keys.Add(item.GroupName);
            }
            return keys;
        }

        /// <summary>某分组内条目,按 SortOrder 升序。</summary>
        public List<SpellItem> ItemsIn(string groupKey) =>
            _items.Where(i => i.GroupName == groupKey).OrderBy(i => i.SortOrder).ToList();

        public IReadOnlyList<SpellItem> AllItems => _items;

        /// <summary>新增:排到对应分组末尾并保存。</summary>
        public void Add(SpellItem item)
        {
            item.SortOrder = NextSortOrder(item.GroupName);
            _items.Add(item);
            SaveNormalized();
        }

        /// <summary>编辑确认后调用(字段已回填)。分组变化时排到新组末尾。</summary>
        public void ApplyEdit(SpellItem item, string previousGroup)
        {
            if (previousGroup != item.GroupName)
            {
                item.SortOrder = NextSortOrder(item.GroupName, exclude: item);
            }
            SaveNormalized();
        }

        public void Delete(SpellItem item)
        {
            _items.Remove(item);
            SaveNormalized();
        }

        /// <summary>移动到指定分组末尾(同组移动即移到组尾)。</summary>
        public void MoveToGroup(SpellItem item, string groupKey)
        {
            item.GroupName = groupKey;
            item.SortOrder = NextSortOrder(groupKey, exclude: item);
            SaveNormalized();
        }

        /// <summary>同分组内拖拽:把 source 插到 target 之前,组内序号重排为 0..n-1。</summary>
        public void ReorderBefore(SpellItem source, SpellItem target)
        {
            if (source == target || source.GroupName != target.GroupName) return;

            var group = ItemsIn(source.GroupName);
            group.Remove(source);
            group.Insert(group.IndexOf(target), source);
            for (var i = 0; i < group.Count; i++) group[i].SortOrder = i;

            SaveNormalized();
        }

        /// <summary>搜索匹配:名称包含关键字,不区分大小写;空关键字匹配全部。</summary>
        public static bool Matches(SpellItem item, string search) =>
            string.IsNullOrWhiteSpace(search)
            || item.Name.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;

        private int NextSortOrder(string groupKey, SpellItem exclude = null)
        {
            var group = _items.Where(i => i.GroupName == groupKey && i != exclude).ToList();
            return group.Count == 0 ? 0 : group.Max(i => i.SortOrder) + 1;
        }

        /// <summary>主列表归一化为"组块顺序 + 组内 SortOrder",与 WPF 版保存顺序一致。</summary>
        private void Normalize()
        {
            _items = GroupKeys().SelectMany(ItemsIn).ToList();
        }

        private void SaveNormalized()
        {
            Normalize();
            _store.Save(_items);
        }
    }
}
