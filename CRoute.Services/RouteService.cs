using System.Net.Http.Json;
using System.Text.Json;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;
using CRoute.Services.Configuration;

namespace CRoute.Services;

/// <summary>
/// Implementasi asli <see cref="IRouteService"/> yang mengambil jarak dan
/// durasi rute dari Google Maps Distance Matrix API.
/// </summary>
public class RouteService : IRouteService
{
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

        var requestUri = BuildRequestUri(origin, destination);

        using var response = await _httpClient
            .GetAsync(requestUri, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken)
            .ConfigureAwait(false);

        return ParseRoute(payload, origin, destination);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.StartsWith("GANTI_DENGAN", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Google Maps API key belum diisi. Isi GoogleMaps:ApiKey di appsettings.json.");

        if (string.IsNullOrWhiteSpace(_settings.DistanceMatrixBaseUrl))
            throw new InvalidOperationException(
                "Google Maps DistanceMatrixBaseUrl belum diisi. Isi di appsettings.json.");
    }

    private string BuildRequestUri(string origin, string destination)
    {
        var query = new[]
        {
            $"origins={Uri.EscapeDataString(origin)}",
            $"destinations={Uri.EscapeDataString(destination)}",
            $"key={Uri.EscapeDataString(_settings.ApiKey)}",
        };

        return $"{_settings.DistanceMatrixBaseUrl}?{string.Join('&', query)}";
    }

    /// <summary>
    /// Mengubah response API jadi <see cref="Route"/>.
    /// </summary>
    private static Route ParseRoute(JsonElement payload, string origin, string destination)
    {
        // TODO: parsing response Distance Matrix yang sebenarnya belum
        // diimplementasikan karena struktur response-nya belum dipastikan.
        // Setelah ada contoh response asli, baca jarak (meter -> km) dan durasi
        // (detik -> menit) dari payload, lalu cek juga field status supaya error
        // dari API tidak diam-diam jadi 0.
        _ = payload;

        return new Route
        {
            Origin = origin,
            Destination = destination,
            DistanceKm = 0,
            DurationMin = 0,
        };
    }
}
