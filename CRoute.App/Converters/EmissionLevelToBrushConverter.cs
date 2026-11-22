using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CRoute.App.ViewModels;

namespace CRoute.App.Converters;

/// <summary>
/// Warna batang perbandingan emisi. ViewModel cukup menentukan level-nya
/// (<see cref="EmissionLevel"/>), warnanya ditentukan di sini.
/// </summary>
[ValueConversion(typeof(EmissionLevel), typeof(Brush))]
public class EmissionLevelToBrushConverter : IValueConverter
{
    private static readonly Brush LowBrush = CreateFrozen(0x2E, 0x7D, 0x5B);
    private static readonly Brush MediumBrush = CreateFrozen(0xE8, 0xC5, 0x47);
    private static readonly Brush HighBrush = CreateFrozen(0xE0, 0x89, 0x4B);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EmissionLevel.High => HighBrush,
            EmissionLevel.Medium => MediumBrush,
            _ => LowBrush,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Brush CreateFrozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
