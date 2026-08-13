using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Spellbook.Core
{
    /// <summary>
    /// 条目持久化:与 WPF 版共用 %APPDATA%\Spellbook\items.json。
    /// Newtonsoft 缩进输出与 System.Text.Json 兼容(PascalCase、非 ASCII 不转义)。
    /// </summary>
    public class ItemStore
    {
        private readonly string _filePath;

        /// <summary>JSON 损坏导致加载失败时为 true(此时返回空表,且不覆盖原文件)。</summary>
        public bool LoadFailed { get; private set; }

        public ItemStore(string filePath = null) => _filePath = filePath ?? DefaultPath;

        public static string DefaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Spellbook", "items.json");

        public List<SpellItem> Load()
        {
            LoadFailed = false;
            if (!File.Exists(_filePath))
            {
                Save(new List<SpellItem>());
                return new List<SpellItem>();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<SpellItem>>(json) ?? new List<SpellItem>();
            }
            catch (JsonException)
            {
                LoadFailed = true;
                return new List<SpellItem>();
            }
        }

        public void Save(List<SpellItem> items)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(items, Formatting.Indented));
        }
    }
}
