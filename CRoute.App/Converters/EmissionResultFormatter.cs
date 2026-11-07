using System.Globalization;
using System.Windows.Data;
using CRoute.Core.Models;

namespace CRoute.App.Converters;

/// <summary>
/// Format tampilan emisi (pengganti <c>EmissionResult.ToDisplayString()</c> di
/// diagram yang disubmit). Bisa dipakai sebagai converter di XAML
/// (<c>{Binding Result, Converter={StaticResource EmissionResultFormatter}}</c>)
/// atau dipanggil statis dari ViewModel.
/// </summary>
[ValueConversion(typeof(EmissionResult), typeof(string))]
public class EmissionResultFormatter : IValueConverter
{
    /// <summary>Contoh: "1,10 kg".</summary>
    public static string ToDisplayString(EmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return FormatKg(result.TotalEmissionKg);
    }

    /// <summary>Format angka kg CO2 dengan jumlah desimal tertentu, opsional pakai satuan.</summary>
    public static string FormatKg(double kg, int decimals = 2, bool withUnit = true)
    {
        string number = kg.ToString("F" + decimals, DisplayCulture.Indonesian);
        return withUnit ? number + " kg" : number;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EmissionResult result => ToDisplayString(result),
            double kg => FormatKg(kg),
            _ => string.Empty,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
