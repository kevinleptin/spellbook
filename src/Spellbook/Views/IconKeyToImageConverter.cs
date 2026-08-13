using System.Globalization;
using System.Windows.Data;
using Spellbook.Services;

namespace Spellbook.Views;

/// <summary>图标 Key → DrawingImage(供 XAML 绑定)。</summary>
public class IconKeyToImageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => IconLoader.Get(value as string ?? "");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
