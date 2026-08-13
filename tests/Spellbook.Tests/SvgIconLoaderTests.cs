using System.IO;
using System.Text;
using System.Windows.Media;
using Spellbook.Services;

namespace Spellbook.Tests;

public class SvgIconLoaderTests
{
    private static Stream Utf8(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

    [Fact]
    public void Parse_AllHundredIcons_Succeeds()
    {
        var dir = IconAssetsTests.IconsDir();
        foreach (var icon in IconLibrary.All)
        {
            using var stream = File.OpenRead(Path.Combine(dir, icon.Key + ".svg"));
            var image = SvgIconLoader.Parse(stream);
            Assert.NotNull(image.Drawing);
            // 每个图标至少一条几何
            var group = Assert.IsType<DrawingGroup>(image.Drawing);
            Assert.True(group.Children.Count > 1, $"{icon.Key} 无绘制内容");
        }
    }

    [Fact]
    public void Parse_FillStrokeAndOpacity_Mapped()
    {
        var svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32">
            <path d="M0,0 H10 V10 H0 Z" fill="#FF0000" opacity="0.5"/>
            <path d="M2,2 L8,8" fill="none" stroke="#00FF00" stroke-width="2" stroke-linecap="round"/>
            </svg>
            """;

        var image = SvgIconLoader.Parse(Utf8(svg));

        // Children[0] 是透明画布矩形(保证所有图标同一 32×32 坐标系),路径从 [1] 开始
        var group = (DrawingGroup)image.Drawing;
        Assert.Equal(3, group.Children.Count);

        var filled = (GeometryDrawing)((DrawingGroup)group.Children[1]).Children[0];
        Assert.Equal(Color.FromRgb(0xFF, 0, 0), ((SolidColorBrush)filled.Brush!).Color);
        Assert.Null(filled.Pen);
        Assert.Equal(0.5, ((DrawingGroup)group.Children[1]).Opacity, 3);

        var stroked = (GeometryDrawing)((DrawingGroup)group.Children[2]).Children[0];
        Assert.Null(stroked.Brush);
        Assert.Equal(2, stroked.Pen!.Thickness);
        Assert.Equal(PenLineCap.Round, stroked.Pen.StartLineCap);
        Assert.Equal(Color.FromRgb(0, 0xFF, 0), ((SolidColorBrush)stroked.Pen.Brush).Color);
    }

    [Fact]
    public void Parse_InvalidXml_Throws()
        => Assert.ThrowsAny<Exception>(() => SvgIconLoader.Parse(Utf8("{not svg")));

    [Fact]
    public void Parse_InvalidPathData_Throws()
        => Assert.ThrowsAny<Exception>(() => SvgIconLoader.Parse(Utf8(
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path d="Q q q" fill="#000"/></svg>""")));

    [Fact]
    public void Parse_Result_IsFrozen()
    {
        var svg = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path d="M0,0 H4" fill="#000"/></svg>""";
        Assert.True(SvgIconLoader.Parse(Utf8(svg)).IsFrozen);
    }
}
