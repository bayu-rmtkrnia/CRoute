using System.Globalization;
using System.Windows.Data;

namespace CRoute.App.Converters;

/// <summary>
/// <c>true</c> kalau nilai binding sama dengan ConverterParameter. Dipakai menu
/// samping: RadioButton aktif kalau <c>CurrentPage</c> sama dengan halamannya.
/// </summary>
public class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
