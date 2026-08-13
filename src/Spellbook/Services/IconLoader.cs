using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Spellbook.Services;

/// <summary>
/// 内置 PNG 图标加载器:嵌入资源 → 冻结的 ImageSource 缓存。
/// 空/未知 Key 回退默认 book。
/// </summary>
public static class IconLoader
{
    private static readonly Lazy<IReadOnlyDictionary<string, ImageSource>> Cache = new(LoadAllCore);

    public static IReadOnlyDictionary<string, ImageSource> LoadAll() => Cache.Value;

    public static ImageSource Get(string iconKey)
    {
        var all = Cache.Value;
        if (!string.IsNullOrWhiteSpace(iconKey) && all.TryGetValue(iconKey, out var image)) return image;
        return all.TryGetValue("book", out var book) ? book : Empty;
    }

    private static readonly ImageSource Empty = CreateEmpty();

    private static ImageSource CreateEmpty()
    {
        var image = new DrawingImage(new DrawingGroup());
        image.Freeze();
        return image;
    }

    /// <summary>从流解码一张图标(测试也用它验证资产可解码)。</summary>
    public static ImageSource Decode(Stream stream)
    {
        var frame = BitmapFrame.Create(stream,
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        frame.Freeze();
        return frame;
    }

    private static IReadOnlyDictionary<string, ImageSource> LoadAllCore()
    {
        var result = new Dictionary<string, ImageSource>();
        foreach (var icon in IconLibrary.All)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Icons/{icon.Key}.png");
                using var stream = System.Windows.Application.GetResourceStream(uri)!.Stream;
                result[icon.Key] = Decode(stream);
            }
            catch
            {
                // 单个图标缺失/损坏不影响启动,该 Key 由 Get 回退 book
            }
        }
        return result;
    }
}
