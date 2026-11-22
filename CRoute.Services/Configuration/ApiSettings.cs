namespace CRoute.Services.Configuration;

/// <summary>
/// Kredensial dan endpoint API eksternal, dibaca dari appsettings.json.
/// Sengaja tidak di-hardcode di kode supaya API key tidak ikut ke repo.
/// </summary>
public class ApiSettings
{
    public GoogleMapsSettings GoogleMaps { get; set; } = new();

    public ClimatiqSettings Climatiq { get; set; } = new();

    public DatabaseSettings Database { get; set; } = new();
}

/// <summary>
/// Konfigurasi koneksi PostgreSQL (lokal maupun Supabase).
/// </summary>
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// Konfigurasi Google Maps. Ada DUA endpoint berbeda yang dipakai proyek ini
/// dan keduanya saling independen — bukan pengganti satu sama lain:
///
/// 1. Routes API (<see cref="RoutesBaseUrl"/>) dipanggil
///    <c>RouteService</c> di layer ini untuk mengisi <c>Route.DistanceKm</c>
///    dan <c>Route.DurationMin</c>, yang jadi dasar perhitungan emisi.
///    (Distance Matrix API sudah legacy dan tidak bisa di-enable di project
///    Google Cloud baru, jadi diganti Routes API.)
/// 2. Embed API (<see cref="EmbedBaseUrl"/>) TIDAK dipanggil dari layer ini
///    sama sekali — outputnya halaman HTML, bukan JSON — melainkan dipasang
///    langsung sebagai URL di View (WebView2 atau elemen sejenis) untuk
///    visualisasi rutenya.
/// </summary>
public class GoogleMapsSettings
{
    /// <summary>API key yang dipakai kedua endpoint di atas.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c> kalau <see cref="ApiKey"/> sudah diisi key asli (bukan
    /// kosong atau teks contoh seperti <c>GANTI_DENGAN_...</c>/<c>ISI_...</c>).
    /// </summary>
    public bool HasApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !ApiKey.StartsWith("GANTI_DENGAN", StringComparison.Ordinal) &&
        !ApiKey.StartsWith("ISI_", StringComparison.Ordinal);

    /// <summary>Endpoint JSON (Routes API computeRoutes) untuk jarak dan durasi rute.</summary>
    public string RoutesBaseUrl { get; set; } = "https://routes.googleapis.com/directions/v2:computeRoutes";

    /// <summary>
    /// Base URL Embed API, disimpan di sini supaya konfigurasinya satu tempat,
    /// tapi belum dipakai di layer Services.
    ///
    /// TODO (CRoute.App, jangan dikerjakan sekarang): View akan menyusun URL
    /// <c>{EmbedBaseUrl}?key={ApiKey}&amp;origin={origin}&amp;destination={destination}</c>
    /// dengan <c>mode=directions</c> sudah termasuk di base URL, supaya peta yang
    /// tampil menunjukkan jalur origin -> destination, bukan sekadar titik lokasi
    /// statis. Pemanggilan ini jalan paralel dan independen dari Distance Matrix:
    /// yang satu untuk visualisasi, yang satu untuk angka jarak. Kegagalan salah
    /// satu tidak boleh membatalkan yang lain.
    /// </summary>
    public string EmbedBaseUrl { get; set; } = string.Empty;
}

public class ClimatiqSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;
}
