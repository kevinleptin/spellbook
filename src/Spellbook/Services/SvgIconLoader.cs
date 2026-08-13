using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace Spellbook.Services;

/// <summary>
/// 受限 SVG 子集解析器:viewBox + path(d/fill/stroke/stroke-width/
/// stroke-linecap/stroke-linejoin/opacity),转为冻结的 DrawingImage。
/// </summary>
public static class SvgIconLoader
{
    private static readonly Lazy<IReadOnlyDictionary<string, DrawingImage>> Cache = new(LoadAllCore);

    /// <summary>全部内置图标(嵌入资源),解析失败的图标被跳过。</summary>
    public static IReadOnlyDictionary<string, DrawingImage> LoadAll() => Cache.Value;

    /// <summary>按 Key 取图标;空/未知 Key 回退默认 book。</summary>
    public static DrawingImage Get(string iconKey)
    {
        var all = Cache.Value;
        if (!string.IsNullOrWhiteSpace(iconKey) && all.TryGetValue(iconKey, out var image)) return image;
        return all.TryGetValue("book", out var book) ? book : Empty;
    }

    private static readonly DrawingImage Empty = CreateEmpty();

    private static DrawingImage CreateEmpty()
    {
        var image = new DrawingImage(new DrawingGroup());
        image.Freeze();
        return image;
    }

    private static IReadOnlyDictionary<string, DrawingImage> LoadAllCore()
    {
        var result = new Dictionary<string, DrawingImage>();
        foreach (var icon in IconLibrary.All)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Icons/{icon.Key}.svg");
                using var stream = Application.GetResourceStream(uri)!.Stream;
                result[icon.Key] = Parse(stream);
            }
            catch
            {
                // 单个图标损坏不影响应用启动,该 Key 缺失时 Get 回退 book
            }
        }
        return result;
    }

    /// <summary>解析一个受限 SVG 流。格式非法时抛异常,由调用方容错。</summary>
    public static DrawingImage Parse(Stream svg)
    {
        var doc = XDocument.Load(svg);
        var root = doc.Root ?? throw new InvalidDataException("空 SVG 文档");

        var viewBox = ParseViewBox(root.Attribute("viewBox")?.Value);

        var group = new DrawingGroup();
        // 透明画布矩形:让所有图标共享同一坐标系与边界,渲染时统一缩放
        group.Children.Add(new GeometryDrawing(
            Brushes.Transparent, null, new RectangleGeometry(viewBox)));

        foreach (var path in root.Descendants().Where(e => e.Name.LocalName == "path"))
        {
            group.Children.Add(ParsePath(path));
        }

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }

    private static Rect ParseViewBox(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new Rect(0, 0, 32, 32);
        var parts = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        return new Rect(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture),
            double.Parse(parts[2], CultureInfo.InvariantCulture),
            double.Parse(parts[3], CultureInfo.InvariantCulture));
    }

    private static DrawingGroup ParsePath(XElement path)
    {
        var data = path.Attribute("d")?.Value
                   ?? throw new InvalidDataException("path 缺少 d 属性");
        var geometry = Geometry.Parse(data);

        var fill = ParseBrush(path.Attribute("fill")?.Value);
        var pen = ParsePen(path);
        if (fill is null && pen is null)
            throw new InvalidDataException("path 既无 fill 也无 stroke");

        // 每条 path 包一层 DrawingGroup 以承载 opacity
        var wrapper = new DrawingGroup();
        wrapper.Children.Add(new GeometryDrawing(fill, pen, geometry));
        var opacity = path.Attribute("opacity")?.Value;
        if (opacity is not null)
            wrapper.Opacity = double.Parse(opacity, CultureInfo.InvariantCulture);
        return wrapper;
    }

    private static SolidColorBrush? ParseBrush(string? value)
    {
        if (value is null || value == "none") return null;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    }

    private static Pen? ParsePen(XElement path)
    {
        var stroke = path.Attribute("stroke")?.Value;
        if (stroke is null || stroke == "none") return null;

        var pen = new Pen(ParseBrush(stroke), ParseDouble(path, "stroke-width", 1));

        var cap = path.Attribute("stroke-linecap")?.Value switch
        {
            "round" => PenLineCap.Round,
            "square" => PenLineCap.Square,
            _ => PenLineCap.Flat,
        };
        pen.StartLineCap = cap;
        pen.EndLineCap = cap;
        pen.DashCap = cap;
        pen.LineJoin = path.Attribute("stroke-linejoin")?.Value switch
        {
            "round" => PenLineJoin.Round,
            "bevel" => PenLineJoin.Bevel,
            _ => PenLineJoin.Miter,
        };
        pen.Freeze();
        return pen;
    }

    private static double ParseDouble(XElement el, string attr, double fallback)
    {
        var value = el.Attribute(attr)?.Value;
        return value is null ? fallback : double.Parse(value, CultureInfo.InvariantCulture);
    }
}
