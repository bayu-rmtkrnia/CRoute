using System.Globalization;
using System.Windows.Data;

namespace CRoute.App.Converters;

/// <summary>
/// Mengubah <c>TransportMode.IconName</c> (mis. "bus") jadi glyph emoji
/// yang ditampilkan di UI. Model cukup menyimpan nama ikon; bentuk
/// tampilannya urusan View.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public class TransportModeIconConverter : IValueConverter
{
    private const string FallbackGlyph = "📍";

    private static readonly Dictionary<string, string> Glyphs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["car"] = "🚗",
        ["motorcycle"] = "🏍",
        ["bus"] = "🚌",
        ["train"] = "🚆",
        ["bicycle"] = "🚲",
        ["walk"] = "🚶",
    };

    public static string ToGlyph(string? iconName)
    {
        return iconName is not null && Glyphs.TryGetValue(iconName, out string? glyph) ? glyph : FallbackGlyph;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ToGlyph(value as string);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
