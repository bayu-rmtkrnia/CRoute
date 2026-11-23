using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;
using CRoute.Services.Configuration;

namespace CRoute.Services;

/// <summary>
/// Mengecek ketersediaan Bus dan Kereta lewat Google Maps Routes API
/// (<c>travelMode: TRANSIT</c>). Satu permintaan dipakai untuk kedua moda,
/// lalu hasilnya difilter: Bus hanya boleh berisi langkah bus, Kereta hanya
/// boleh berisi langkah rail, dan total jalan kaki maksimal 2 km.
/// </summary>
public class TransitRouteService : ITransitRouteService
{
    /// <summary>Batas total jalan kaki akses/egress/transfer.</summary>
    public const double MaxWalkKm = 2.0;

    private const string FieldMask =
        "routes.distanceMeters,routes.duration,routes.legs.steps.travelMode," +
        "routes.legs.steps.distanceMeters,routes.legs.steps.transitDetails";

    private static readonly string[] TransitModes =
    [
        "BUS", "SUBWAY", "TRAIN", "LIGHT_RAIL", "RAIL",
    ];

    private readonly HttpClient _httpClient;
    private readonly GoogleMapsSettings _settings;

    public TransitRouteService(HttpClient httpClient, ApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _settings = settings.GoogleMaps;
    }

    /// <inheritdoc />
    public async Task<TransitRouteResult> GetRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Lokasi asal dan tujuan wajib diisi.");

        EnsureConfigured();

        using var request = BuildRequest(origin.Trim(), destination.Trim());

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpRequestException(
                "Google Maps tidak merespons (timeout). Coba lagi sebentar.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException(
                "Tidak bisa terhubung ke Google Maps. Cek koneksi internet.",
                ex,
                ex.StatusCode);
        }

        using (response)
        {
            string body = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    DescribeError(response.StatusCode, TryParseJson(body)),
                    null,
                    response.StatusCode);
            }

            return Parse(body);
        }
    }

    private HttpRequestMessage BuildRequest(string origin, string destination)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _settings.RoutesBaseUrl)
        {
            Content = JsonContent.Create(new
            {
                origin = new { address = origin },
                destination = new { address = destination },
                travelMode = "TRANSIT",
                computeAlternativeRoutes = true,
                languageCode = "id",
                regionCode = "ID",
                transitPreferences = new
                {
                    routingPreference = "LESS_WALKING",
                    allowedTravelModes = TransitModes,
                },
            }),
        };

        request.Headers.Add("X-Goog-Api-Key", _settings.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", FieldMask);

        return request;
    }

    /// <summary>
    /// Membaca semua alternatif rute dan memilih kandidat bus-only dan
    /// rail-only terbaik. Rute campuran bus + kereta sengaja ditolak.
    /// </summary>
    public static TransitRouteResult Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return TransitRouteResult.Empty;

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("routes", out JsonElement routes) ||
            routes.ValueKind != JsonValueKind.Array)
        {
            return TransitRouteResult.Empty;
        }

        TransitRouteOption? bestBus = null;
        TransitRouteOption? bestTrain = null;

        foreach (JsonElement route in routes.EnumerateArray())
        {
            if (!route.TryGetProperty("legs", out JsonElement legs) ||
                legs.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            double walkMeters = 0;
            double busKm = 0;
            double railKm = 0;
            bool hasBus = false;
            bool hasRail = false;
            bool hasOtherTransit = false;
            var lines = new List<string>();

            foreach (JsonElement leg in legs.EnumerateArray())
            {
                if (!leg.TryGetProperty("steps", out JsonElement steps) ||
                    steps.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement step in steps.EnumerateArray())
                {
                    string travelMode = GetString(step, "travelMode");
                    double distanceMeters = GetDouble(step, "distanceMeters");

                    if (string.Equals(travelMode, "WALK", StringComparison.OrdinalIgnoreCase))
                    {
                        walkMeters += distanceMeters;
                        continue;
                    }

                    if (!string.Equals(travelMode, "TRANSIT", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string vehicleType = string.Empty;
                    string lineName = string.Empty;

                    if (step.TryGetProperty("transitDetails", out JsonElement details) &&
                        details.ValueKind == JsonValueKind.Object &&
                        details.TryGetProperty("transitLine", out JsonElement transitLine) &&
                        transitLine.ValueKind == JsonValueKind.Object)
                    {
                        vehicleType = GetNestedString(transitLine, "vehicle", "type");
                        lineName = GetString(transitLine, "nameShort");
                        if (string.IsNullOrWhiteSpace(lineName))
                            lineName = GetString(transitLine, "name");
                    }

                    if (IsBus(vehicleType))
                    {
                        hasBus = true;
                        busKm += distanceMeters / 1000.0;
                        AddLine(lines, lineName);
                    }
                    else if (IsRail(vehicleType))
                    {
                        hasRail = true;
                        railKm += distanceMeters / 1000.0;
                        AddLine(lines, lineName);
                    }
                    else
                    {
                        // Ferry, taxi, atau tipe transit lain membuat rute tidak
                        // memenuhi syarat "bus saja" maupun "kereta saja".
                        hasOtherTransit = true;
                    }
                }
            }

            double walkKm = walkMeters / 1000.0;
            int durationMin = ParseDurationMinutes(GetString(route, "duration"));
            string[] routeLines = lines
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (hasBus && !hasRail && !hasOtherTransit && busKm > 0 && walkKm <= MaxWalkKm)
            {
                var candidate = new TransitRouteOption(busKm, walkKm, durationMin, routeLines);
                if (IsBetter(candidate, bestBus))
                    bestBus = candidate;
            }

            if (hasRail && !hasBus && !hasOtherTransit && railKm > 0 && walkKm <= MaxWalkKm)
            {
                var candidate = new TransitRouteOption(railKm, walkKm, durationMin, routeLines);
                if (IsBetter(candidate, bestTrain))
                    bestTrain = candidate;
            }
        }

        return new TransitRouteResult(bestBus, bestTrain);
    }

    private static bool IsBetter(TransitRouteOption candidate, TransitRouteOption? current)
    {
        if (current is null)
            return true;

        if (candidate.TransitDistanceKm != current.TransitDistanceKm)
            return candidate.TransitDistanceKm < current.TransitDistanceKm;

        if (candidate.WalkDistanceKm != current.WalkDistanceKm)
            return candidate.WalkDistanceKm < current.WalkDistanceKm;

        return candidate.DurationMin < current.DurationMin;
    }

    private static bool IsBus(string vehicleType) =>
        vehicleType.Equals("BUS", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("INTERCITY_BUS", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("TROLLEYBUS", StringComparison.OrdinalIgnoreCase);

    private static bool IsRail(string vehicleType) =>
        vehicleType.Equals("RAIL", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("TRAIN", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("COMMUTER_TRAIN", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("HEAVY_RAIL", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("METRO_RAIL", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("LIGHT_RAIL", StringComparison.OrdinalIgnoreCase) ||
        vehicleType.Equals("SUBWAY", StringComparison.OrdinalIgnoreCase);

    private static void AddLine(List<string> lines, string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            lines.Add(line.Trim());
    }

    private static string GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out JsonElement value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string GetNestedString(JsonElement element, string parent, string property)
    {
        return element.TryGetProperty(parent, out JsonElement child) &&
               child.ValueKind == JsonValueKind.Object
            ? GetString(child, property)
            : string.Empty;
    }

    private static double GetDouble(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value))
            return 0;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out double number) => number,
            JsonValueKind.String when double.TryParse(
                value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
            _ => 0,
        };
    }

    private static int ParseDurationMinutes(string duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
            return 0;

        string secondsText = duration.EndsWith('s') ? duration[..^1] : duration;
        if (!double.TryParse(secondsText, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
            return 0;

        return (int)Math.Round(seconds / 60.0, MidpointRounding.AwayFromZero);
    }

    private void EnsureConfigured()
    {
        if (!_settings.HasApiKey)
        {
            throw new InvalidOperationException(
                "Google Maps API key belum diisi. Isi GoogleMaps:ApiKey di appsettings.local.json " +
                "atau set environment variable CROUTE_GoogleMaps__ApiKey.");
        }

        if (string.IsNullOrWhiteSpace(_settings.RoutesBaseUrl))
        {
            throw new InvalidOperationException(
                "GoogleMaps:RoutesBaseUrl belum diisi. Isi di appsettings.json.");
        }
    }

    private static JsonElement? TryParseJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string DescribeError(HttpStatusCode statusCode, JsonElement? payload)
    {
        string? apiMessage = null;

        if (payload is { ValueKind: JsonValueKind.Object } root &&
            root.TryGetProperty("error", out JsonElement error) &&
            error.ValueKind == JsonValueKind.Object &&
            error.TryGetProperty("message", out JsonElement message) &&
            message.ValueKind == JsonValueKind.String)
        {
            apiMessage = message.GetString();
        }

        string hint = statusCode switch
        {
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized =>
                "API key ditolak. Pastikan Routes API sudah di-enable dan billing aktif di Google Cloud.",
            HttpStatusCode.TooManyRequests =>
                "Kuota Google Maps habis atau terlalu banyak permintaan.",
            HttpStatusCode.BadRequest when apiMessage?.Contains("API key", StringComparison.OrdinalIgnoreCase) == true =>
                "API key Google Maps tidak valid.",
            _ => $"Google Maps mengembalikan error {(int)statusCode}.",
        };

        return string.IsNullOrWhiteSpace(apiMessage) ? hint : $"{hint} ({apiMessage})";
    }
}
