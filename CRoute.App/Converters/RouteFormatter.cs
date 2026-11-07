using System.Globalization;
using System.Windows.Data;
using CRoute.Core.Models;

namespace CRoute.App.Converters;

/// <summary>
/// Format tampilan rute (pengganti <c>Route.GetSummary()</c> di diagram yang
/// disubmit). Sebagai converter, mengubah <see cref="Route"/> jadi ringkasan
/// "Asal → Tujuan".
/// </summary>
[ValueConversion(typeof(Route), typeof(string))]
public class RouteFormatter : IValueConverter
{
    /// <summary>Contoh: "Malioboro  →  Stasiun Tugu".</summary>
    public static string ToSummaryString(Route route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return route.Origin + "  →  " + route.Destination;
    }

    /// <summary>Contoh: "12,4 km  ·  ± 27 menit (estimasi)".</summary>
    public static string ToDetailString(Route route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return FormatDistance(route.DistanceKm) + "  ·  ± " + route.DurationMin + " menit (estimasi)";
    }

    /// <summary>Contoh: "12,4 km".</summary>
    public static string FormatDistance(double km)
    {
        return km.ToString("F1", DisplayCulture.Indonesian) + " km";
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Route route ? ToSummaryString(route) : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
