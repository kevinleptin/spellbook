using System.IO;
using Spellbook.Services;

namespace Spellbook.Tests;

public class IconLoaderTests
{
    [Fact]
    public void Decode_AllHundredIcons_Succeeds()
    {
        var dir = IconAssetsTests.IconsDir();
        foreach (var icon in IconLibrary.All)
        {
            using var stream = File.OpenRead(Path.Combine(dir, icon.Key + ".png"));
            var image = IconLoader.Decode(stream);
            Assert.True(image.Width > 0 && image.Height > 0, $"{icon.Key} 解码尺寸异常");
        }
    }

    [Fact]
    public void Decode_InvalidData_Throws()
        => Assert.ThrowsAny<Exception>(() =>
            IconLoader.Decode(new MemoryStream(new byte[] { 1, 2, 3 })));
}
