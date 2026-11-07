using System.Net.Http.Json;
using System.Text.Json;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;
using CRoute.Services.Configuration;

namespace CRoute.Services;

/// <summary>
/// Implementasi asli <see cref="IEmissionService"/>.
/// </summary>
public class EmissionService : IEmissionService
{
    private readonly HttpClient _httpClient;
    private readonly ClimatiqSettings _settings;

    public EmissionService(HttpClient httpClient, ApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _settings = settings.Climatiq;
    }

    /// <inheritdoc />
    public EmissionResult Calculate(Route route, TransportMode mode)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(mode);

        return new EmissionResult
        {
            Mode = mode,
            TotalEmissionKg = mode.CalculateEmission(route.DistanceKm),
        };
    }

    public async Task RefreshEmissionFactorAsync(
        TransportMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mode);

        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        request.Content = JsonContent.Create(new { mode = mode.Name });

        using var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<JsonElement>(cancellationToken)
            .ConfigureAwait(false);

        // TODO: parsing response Climatiq belum diimplementasikan karena struktur
        // response-nya belum dipastikan.   
        _ = payload;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.StartsWith("GANTI_DENGAN", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Climatiq API key belum diisi. Isi Climatiq:ApiKey di appsettings.json.");

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            throw new InvalidOperationException(
                "Climatiq BaseUrl belum diisi. Isi Climatiq:BaseUrl di appsettings.json.");
    }
}
