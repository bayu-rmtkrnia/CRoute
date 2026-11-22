using System.Net;
using System.Text;
using CRoute.Core.Models;
using CRoute.Services;
using CRoute.Services.Configuration;

namespace CRoute.Tests.Services;

/// <summary>
/// Test kontrak untuk RouteService. Test parsing sengaja di-skip sampai Bayu
/// mengimplementasikan ParseRoute. Setelah itu, hapus atribut Skip dan pastikan
/// semua test lulus sebelum integrasi digabung.
/// </summary>
public class RouteServiceTests
{
    private static ApiSettings CreateSettings(string apiKey = "test-key") => new()
    {
        GoogleMaps = new GoogleMapsSettings
        {
            ApiKey = apiKey,
            DistanceMatrixBaseUrl = "https://maps.test/distancematrix/json",
        },
    };

    private static RouteService CreateService(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(new HttpClient(new StubHandler(json, status)), CreateSettings());

    [Fact]
    public async Task GetRouteAsync_KeyPlaceholder_MelemparInvalidOperationTanpaMemanggilHttp()
    {
        var handler = new StubHandler("{}");
        var service = new RouteService(new HttpClient(handler), CreateSettings("GANTI_DENGAN_GOOGLE_MAPS_API_KEY"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetRouteAsync("Kampus", "Stasiun"));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact(Skip = "Aktifkan setelah Bayu mengimplementasikan ParseRoute.")]
    public async Task GetRouteAsync_ResponseOk_MengembalikanJarakDanDurasi()
    {
        const string json = """
            {
              "status": "OK",
              "rows": [
                {
                  "elements": [
                    {
                      "status": "OK",
                      "distance": { "text": "10.2 km", "value": 10200 },
                      "duration": { "text": "25 mins", "value": 1500 }
                    }
                  ]
                }
              ]
            }
            """;

        Route route = await CreateService(json).GetRouteAsync("Kampus", "Stasiun");

        Assert.Equal(10.2, route.DistanceKm, 10);
        Assert.Equal(25, route.DurationMin);
    }

    [Fact(Skip = "Aktifkan setelah Bayu mengimplementasikan ParseRoute.")]
    public async Task GetRouteAsync_RequestDenied_MelemparInvalidOperation()
    {
        const string json = """
            {
              "status": "REQUEST_DENIED",
              "error_message": "The provided API key is invalid."
            }
            """;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(json).GetRouteAsync("Kampus", "Stasiun"));

        Assert.Contains("REQUEST_DENIED", exception.Message);
    }

    [Fact(Skip = "Aktifkan setelah Bayu mengimplementasikan ParseRoute.")]
    public async Task GetRouteAsync_ZeroResults_MelemparInvalidOperation()
    {
        const string json = """
            {
              "status": "OK",
              "rows": [
                {
                  "elements": [
                    { "status": "ZERO_RESULTS" }
                  ]
                }
              ]
            }
            """;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(json).GetRouteAsync("Kampus", "Stasiun"));
    }

    [Fact(Skip = "Aktifkan setelah Bayu mengimplementasikan ParseRoute.")]
    public async Task GetRouteAsync_HttpError_MelemparHttpRequestException()
    {
        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService("{}", HttpStatusCode.InternalServerError).GetRouteAsync("Kampus", "Stasiun"));
    }

    private sealed class StubHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}
