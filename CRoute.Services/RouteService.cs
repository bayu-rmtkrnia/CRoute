using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;
using CRoute.Services.Configuration;

namespace CRoute.Services;

/// <summary>
/// Implementasi asli <see cref="IRouteService"/> yang mengambil jarak dan
/// durasi rute dari Google Maps Routes API (<c>computeRoutes</c>).
/// </summary>
/// <remarks>
/// Distance Matrix API sudah berstatus legacy dan tidak bisa diaktifkan
/// lagi di project Google Cloud baru, jadi dipakai Routes API sebagai gantinya.
/// Kontrak error: masalah jaringan/HTTP (key ditolak, kuota habis, server
/// error, timeout) dilempar sebagai <see cref="HttpRequestException"/>;
/// masalah konfigurasi atau data (key belum diisi, rute tidak ditemukan)
/// sebagai <see cref="InvalidOperationException"/>. Pesan keduanya sudah
/// ramah pengguna.
/// </remarks>
public class RouteService : IRouteService
{
    /// <summary>Field yang diminta dari API; wajib diisi untuk Routes API.</summary>
    /// <remarks>
    /// <c>routes.polyline.encodedPolyline</c> dipakai untuk menggambar garis
    /// rute di peta Leaflet (CariRuteView).
    /// </remarks>
    private const string FieldMask = "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline";

    private readonly HttpClient _httpClient;
    private readonly GoogleMapsSettings _settings;

    public RouteService(HttpClient httpClient, ApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _settings = settings.GoogleMaps;
    }

    /// <inheritdoc />
    public async Task<Route> GetRouteAsync(string origin, string destination)
    {
        return await GetRouteAsync(origin, destination, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<Route> GetRouteAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin))
            throw new ArgumentException("Titik asal tidak boleh kosong.", nameof(origin));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Titik tujuan tidak boleh kosong.", nameof(destination));

        EnsureConfigured();

        using var request = BuildRequest(origin.Trim(), destination.Trim());

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException(
                "Tidak bisa terhubung ke Google Maps. Cek koneksi internet.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpRequestException(
                "Google Maps tidak merespons (timeout). Coba lagi sebentar.", ex);
        }

        using (response)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            JsonElement? payload = TryParseJson(body);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(DescribeError(response.StatusCode, payload), null, response.StatusCode);

            if (payload is null)
                throw new InvalidOperationException("Respons Google Maps tidak bisa dibaca.");

            return ParseRoute(payload.Value, origin, destination);
        }
    }

    private void EnsureConfigured()
    {
        if (!_settings.HasApiKey)
            throw new InvalidOperationException(
                "Google Maps API key belum diisi. Isi GoogleMaps:ApiKey di appsettings.local.json.");

        if (string.IsNullOrWhiteSpace(_settings.RoutesBaseUrl))
            throw new InvalidOperationException(
                "Google Maps RoutesBaseUrl belum diisi. Isi di appsettings.json.");
    }

    private HttpRequestMessage BuildRequest(string origin, string destination)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _settings.RoutesBaseUrl)
        {
            Content = JsonContent.Create(new
            {
                origin = new { address = origin },
                destination = new { address = destination },
                travelMode = "DRIVE",
                languageCode = "id",
                regionCode = "ID",
            }),
        };

        request.Headers.Add("X-Goog-Api-Key", _settings.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", FieldMask);

        return request;
    }

    private static JsonElement? TryParseJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Pesan error yang bisa dibaca pengguna dari respons gagal. Format error
    /// Routes API: <c>{ "error": { "code", "message", "status" } }</c>.
    /// </summary>
    private static string DescribeError(HttpStatusCode statusCode, JsonElement? payload)
    {
        string? apiMessage = null;
        string? apiStatus = null;

        if (payload is { ValueKind: JsonValueKind.Object } root &&
            root.TryGetProperty("error", out JsonElement error) &&
            error.ValueKind == JsonValueKind.Object)
        {
            if (error.TryGetProperty("message", out JsonElement message))
                apiMessage = message.GetString();
            if (error.TryGetProperty("status", out JsonElement status))
                apiStatus = status.GetString();
        }

        string hint = statusCode switch
        {
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized =>
                "API key ditolak. Pastikan Routes API sudah di-enable dan billing aktif di Google Cloud.",
            HttpStatusCode.TooManyRequests =>
                "Kuota Google Maps habis atau terlalu banyak permintaan.",
            HttpStatusCode.BadRequest when apiMessage?.Contains("API key", StringComparison.OrdinalIgnoreCase) == true =>
                "API key Google Maps tidak valid.",
            HttpStatusCode.BadRequest =>
                "Alamat tidak dikenali Google Maps. Coba tulis lebih lengkap.",
            _ => $"Google Maps mengembalikan error {(int)statusCode}.",
        };

        return apiMessage is null
            ? hint
            : $"{hint} ({apiStatus ?? ((int)statusCode).ToString(CultureInfo.InvariantCulture)}: {apiMessage})";
    }

    /// <summary>
    /// Mengubah respons <c>computeRoutes</c> jadi <see cref="Route"/>.
    /// Contoh respons: <c>{ "routes": [ { "distanceMeters": 12345, "duration": "1500s" } ] }</c>.
    /// Kalau rute tidak ditemukan, API membalas objek kosong <c>{}</c>.
    /// </summary>
    private static Route ParseRoute(JsonElement payload, string origin, string destination)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("routes", out JsonElement routes) ||
            routes.ValueKind != JsonValueKind.Array ||
            routes.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                $"Rute dari \"{origin.Trim()}\" ke \"{destination.Trim()}\" tidak ditemukan. " +
                "Coba tulis alamat lebih lengkap (mis. tambahkan nama kota).");
        }

        JsonElement first = routes[0];

        // distanceMeters dan duration tidak dikirim API kalau nilainya 0.
        double meters = first.TryGetProperty("distanceMeters", out JsonElement distance)
            ? distance.GetDouble()
            : 0;

        double seconds = first.TryGetProperty("duration", out JsonElement duration)
            ? ParseDurationSeconds(duration.GetString())
            : 0;

        string? polyline =
            first.TryGetProperty("polyline", out JsonElement poly) &&
            poly.ValueKind == JsonValueKind.Object &&
            poly.TryGetProperty("encodedPolyline", out JsonElement encoded) &&
            encoded.ValueKind == JsonValueKind.String
                ? encoded.GetString()
                : null;

        return new Route
        {
            Origin = origin,
            Destination = destination,
            DistanceKm = meters / 1000.0,
            DurationMin = (int)Math.Round(seconds / 60.0, MidpointRounding.AwayFromZero),
            EncodedPolyline = string.IsNullOrEmpty(polyline) ? null : polyline,
        };
    }

    /// <summary>Durasi Routes API berformat string detik, mis. <c>"1500s"</c>.</summary>
    private static double ParseDurationSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        string number = value.Trim().TrimEnd('s', 'S');

        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            ? seconds
            : throw new InvalidOperationException($"Format durasi dari Google Maps tidak dikenali: \"{value}\".");
    }
}
