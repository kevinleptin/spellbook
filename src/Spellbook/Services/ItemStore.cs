using System.IO;
using System.Text.Json;
using Spellbook.Models;

namespace Spellbook.Services;

/// <summary>
/// 条目持久化:%APPDATA%\Spellbook\items.json。
/// 任何增删改后调用方应立即 Save。
/// </summary>
public class ItemStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // 中文等非 ASCII 字符不转义,保持 json 可读
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _filePath;

    /// <summary>JSON 损坏导致加载失败时为 true(此时返回空表,且不覆盖原文件)。</summary>
    public bool LoadFailed { get; private set; }

    public ItemStore(string? filePath = null) => _filePath = filePath ?? DefaultPath;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Spellbook", "items.json");

    public List<SpellItem> Load()
    {
        LoadFailed = false;
        if (!File.Exists(_filePath))
        {
            // 文件不存在时自动创建(空列表)
            Save(new List<SpellItem>());
            return new List<SpellItem>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<SpellItem>>(json) ?? new List<SpellItem>();
        }
        catch (JsonException)
        {
            // JSON 损坏:不崩溃、不覆盖原文件,以空列表启动
            LoadFailed = true;
            return new List<SpellItem>();
        }
    }

    public void Save(List<SpellItem> items)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(items, JsonOptions));
    }
}
