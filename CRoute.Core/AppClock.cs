namespace CRoute.Core;

/// <summary>
/// Waktu acuan aplikasi. Agregasi bulanan dan harian memakai zona
/// <c>Asia/Jakarta</c> supaya hasil di C# sama dengan function SQL di database,
/// berapa pun zona waktu komputer pengguna.
/// </summary>
public static class AppClock
{
    /// <summary>Zona waktu acuan aplikasi (Asia/Jakarta).</summary>
    public static TimeZoneInfo Zone { get; } = ResolveZone();

    /// <summary>Waktu sekarang di zona aplikasi (Asia/Jakarta).</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    /// <summary>
    /// Mengubah waktu ke zona aplikasi. Nilai <see cref="DateTimeKind.Unspecified"/>
    /// dianggap sudah dalam zona aplikasi, misalnya tanggal yang dibuat di test.
    /// </summary>
    public static DateTime ToAppTime(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : TimeZoneInfo.ConvertTime(value, Zone);

    private static TimeZoneInfo ResolveZone()
    {
        // Windows memakai "SE Asia Standard Time"; Linux/macOS memakai
        // "Asia/Jakarta". Jangan diam-diam jatuh ke UTC: itu akan menggeser
        // batas bulan/hari 7 jam tanpa terlihat.
        foreach (string id in new[] { "Asia/Jakarta", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException(
            "Zona waktu Asia/Jakarta tidak tersedia di sistem ini. " +
            "Agregasi bulanan dan harian membutuhkan zona tersebut; " +
            "pasang data zona waktu (ICU/tzdata) lalu jalankan ulang aplikasi.");
    }
}
