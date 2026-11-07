using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CRoute.App.Converters;

/// <summary>
/// <c>true</c> → Visible, <c>false</c> → Collapsed. Set <see cref="Invert"/>
/// untuk kebalikannya (mis. panel "kosong" yang tampil saat data tidak ada).
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool visible = value is true;
        if (Invert)
            visible = !visible;

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
