using System.Globalization;
using System.Windows.Data;

namespace CRoute.App.Converters;

/// <summary>
/// Mengubah rasio 0..1 jadi panjang piksel (mis. tinggi batang grafik).
/// ViewModel tidak perlu tahu ukuran piksel di View.
/// </summary>
[ValueConversion(typeof(double), typeof(double))]
public class RatioToLengthConverter : IValueConverter
{
    public double MaxLength { get; set; } = 150;

    public double MinLength { get; set; } = 3;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double ratio = value is double d ? d : 0;
        return Math.Max(MinLength, ratio * MaxLength);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
