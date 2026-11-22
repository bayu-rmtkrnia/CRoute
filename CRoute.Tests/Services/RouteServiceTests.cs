using System.Net;
using System.Text;
using CRoute.Core.Models;
using CRoute.Services;
using CRoute.Services.Configuration;

namespace CRoute.Tests.Services;

/// <summary>
/// Test kontrak untuk RouteService (Google Maps Routes API, computeRoutes).
/// Format respons mengikuti dokumentasi resmi:
/// <c>{ "routes": [ { "distanceMeters": 10200, "duration": "1500s" } ] }</c>.
/// </summary>
public class RouteServiceTests
{
    private static ApiSettings CreateSettings(string apiKey = "test-key") => new()
    {
        GoogleMaps = new GoogleMapsSettings
        {
            ApiKey = apiKey,
            RoutesBaseUrl = "https://maps.test/directions/v2:computeRoutes",
        },
    };

    private static RouteService CreateService(StubHandler handler, string apiKey = "test-key")
        => new(new HttpClient(handler), CreateSettings(apiKey));

    private static RouteService CreateService(string json, HttpStatusCode status = HttpStatusCode.OK)
        => CreateService(new StubHandler(json, status));

    [Theory]
    [InlineData("GANTI_DENGAN_GOOGLE_MAPS_API_KEY")]
    [InlineData("ISI_GOOGLE_MAPS_API_KEY_DI_SINI")]
    [InlineData("")]
    public async Task GetRouteAsync_KeyPlaceholder_MelemparInvalidOperationTanpaMemanggilHttp(string apiKey)
    {
        var handler = new StubHandler("{}");
        RouteService service = CreateService(handler, apiKey);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetRouteAsync("Kampus", "Stasiun"));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetRouteAsync_ResponseOk_MengembalikanJarakDanDurasi()
    {
        const string json = """
            {
              "routes": [
                { "distanceMeters": 10200, "duration": "1500s" }
              ]
            }
            """;

        Route route = await CreateService(json).GetRouteAsync("Kampus", "Stasiun");

        Assert.Equal(10.2, route.DistanceKm, 10);
        Assert.Equal(25, route.DurationMin);
        Assert.Equal("Kampus", route.Origin);
        Assert.Equal("Stasiun", route.Destination);
    }

    [Fact]
    public async Task GetRouteAsync_MengirimRequestSesuaiRoutesApi()
    {
        var handler = new StubHandler("""{ "routes": [ { "distanceMeters": 1000, "duration": "60s" } ] }""");

        await CreateService(handler).GetRouteAsync(" Kampus ", "Stasiun");

        HttpRequestMessage request = handler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://maps.test/directions/v2:computeRoutes", request.RequestUri!.ToString());
        Assert.Equal("test-key", request.Headers.GetValues("X-Goog-Api-Key").Single());
        Assert.Equal("routes.distanceMeters,routes.duration", request.Headers.GetValues("X-Goog-FieldMask").Single());
        // Key dikirim lewat header, bukan query string, supaya tidak ikut tercatat di log URL.
        Assert.DoesNotContain("test-key", request.RequestUri.ToString());
        Assert.Contains("\"origin\":{\"address\":\"Kampus\"}", handler.LastRequestBody);
        Assert.Contains("\"destination\":{\"address\":\"Stasiun\"}", handler.LastRequestBody);
        Assert.Contains("\"travelMode\":\"DRIVE\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task GetRouteAsync_DurasiPecahan_DibulatkanKeMenit()
    {
        const string json = """{ "routes": [ { "distanceMeters": 4700, "duration": "929.5s" } ] }""";

        Route route = await CreateService(json).GetRouteAsync("Kampus", "Stasiun");

        Assert.Equal(4.7, route.DistanceKm, 10);
        Assert.Equal(15, route.DurationMin);
    }

    [Fact]
    public async Task GetRouteAsync_FieldNolTidakDikirim_DianggapNol()
    {
        // Routes API tidak mengirim field yang bernilai default (0).
        Route route = await CreateService("""{ "routes": [ {} ] }""").GetRouteAsync("Kampus", "Stasiun");

        Assert.Equal(0, route.DistanceKm);
        Assert.Equal(0, route.DurationMin);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "routes": [] }""")]
    public async Task GetRouteAsync_RuteTidakDitemukan_MelemparInvalidOperation(string json)
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(json).GetRouteAsync("Kampus", "Stasiun"));

        Assert.Contains("tidak ditemukan", exception.Message);
    }

    [Fact]
    public async Task GetRouteAsync_ApiKeyTidakValid_MelemparHttpRequestDenganPesanJelas()
    {
        const string json = """
            {
              "error": {
                "code": 400,
                "message": "API key not valid. Please pass a valid API key.",
                "status": "INVALID_ARGUMENT"
              }
            }
            """;

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService(json, HttpStatusCode.BadRequest).GetRouteAsync("Kampus", "Stasiun"));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("tidak valid", exception.Message);
        Assert.Contains("INVALID_ARGUMENT", exception.Message);
    }

    [Fact]
    public async Task GetRouteAsync_ApiBelumDiaktifkan_MelemparHttpRequest()
    {
        const string json = """
            {
              "error": {
                "code": 403,
                "message": "Routes API has not been used in project 123 before or it is disabled.",
                "status": "PERMISSION_DENIED"
              }
            }
            """;

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService(json, HttpStatusCode.Forbidden).GetRouteAsync("Kampus", "Stasiun"));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Contains("PERMISSION_DENIED", exception.Message);
    }

    [Fact]
    public async Task GetRouteAsync_HttpError_MelemparHttpRequestException()
    {
        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService("{}", HttpStatusCode.InternalServerError).GetRouteAsync("Kampus", "Stasiun"));
    }

    [Fact]
    public async Task GetRouteAsync_TidakAdaKoneksi_MelemparHttpRequestDenganPesanJelas()
    {
        var handler = new StubHandler("{}") { ThrowOnSend = new HttpRequestException("No such host is known.") };

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService(handler).GetRouteAsync("Kampus", "Stasiun"));

        Assert.Contains("koneksi internet", exception.Message);
    }

    private sealed class StubHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        public Exception? ThrowOnSend { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (ThrowOnSend is not null)
                throw ThrowOnSend;

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}
