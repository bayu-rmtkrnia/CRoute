using System.Net;
using System.Text;
using CRoute.Core.Models;
using CRoute.Services;
using CRoute.Services.Configuration;

namespace CRoute.Tests.Services;

public class TransitRouteServiceTests
{
    private const string Walk500 = """{"travelMode":"WALK","distanceMeters":500}""";
    private const string Walk2000 = """{"travelMode":"WALK","distanceMeters":2000}""";
    private const string Walk2100 = """{"travelMode":"WALK","distanceMeters":2100}""";

    private static string Bus(int meters, string line) =>
        "{\"travelMode\":\"TRANSIT\",\"distanceMeters\":" + meters +
        ",\"transitDetails\":{\"transitLine\":{\"nameShort\":\"" + line +
        "\",\"vehicle\":{\"type\":\"BUS\"}}}}";

    private static string Rail(int meters, string line) =>
        "{\"travelMode\":\"TRANSIT\",\"distanceMeters\":" + meters +
        ",\"transitDetails\":{\"transitLine\":{\"nameShort\":\"" + line +
        "\",\"vehicle\":{\"type\":\"RAIL\"}}}}";

    private static string Response(params string[] steps) => $$"""
        {
          "routes": [
            {
              "distanceMeters": 5000,
              "duration": "2130s",
              "legs": [
                {
                  "steps": [ {{string.Join(",", steps)}} ]
                }
              ]
            }
          ]
        }
        """;

    private static TransitRouteService CreateService(
        StubHandler handler,
        string apiKey = "test-key")
        => new(new HttpClient(handler), new ApiSettings
        {
            GoogleMaps = new GoogleMapsSettings
            {
                ApiKey = apiKey,
                RoutesBaseUrl = "https://maps.test/directions/v2:computeRoutes",
            },
        });

    [Fact]
    public async Task GetRoutesAsync_BusOnly_MengembalikanBus()
    {
        var handler = new StubHandler(Response(
            Walk500,
            Bus(1526, "1A"),
            Bus(1650, "5B")));

        TransitRouteResult result = await CreateService(handler)
            .GetRoutesAsync("Malioboro", "UGM");

        Assert.NotNull(result.Bus);
        Assert.Null(result.Train);
        Assert.Equal(3.176, result.Bus!.TransitDistanceKm, precision: 3);
        Assert.Equal(0.5, result.Bus.WalkDistanceKm, precision: 3);
        Assert.Equal(36, result.Bus.DurationMin);
        Assert.Equal(["1A", "5B"], result.Bus.Lines);
    }

    [Fact]
    public async Task GetRoutesAsync_RailOnly_MengembalikanKereta()
    {
        var handler = new StubHandler(Response(
            Walk500,
            Rail(12000, "Prameks")));

        TransitRouteResult result = await CreateService(handler)
            .GetRoutesAsync("Stasiun Tugu", "Stasiun Maguwo");

        Assert.NotNull(result.Train);
        Assert.Null(result.Bus);
        Assert.Equal(12.0, result.Train!.TransitDistanceKm, precision: 3);
        Assert.Equal(0.5, result.Train.WalkDistanceKm, precision: 3);
    }

    [Fact]
    public async Task GetRoutesAsync_CampuranBusDanKereta_TidakMengembalikanKeduanya()
    {
        var handler = new StubHandler(Response(
            Walk500,
            Bus(3000, "1A"),
            Rail(8000, "Prameks")));

        TransitRouteResult result = await CreateService(handler)
            .GetRoutesAsync("Malioboro", "UGM");

        Assert.Null(result.Bus);
        Assert.Null(result.Train);
    }

    [Fact]
    public async Task GetRoutesAsync_JalanKakiLebihDariDuaKm_TidakTersedia()
    {
        var handler = new StubHandler(Response(
            Walk2100,
            Bus(3000, "1A")));

        TransitRouteResult result = await CreateService(handler)
            .GetRoutesAsync("Malioboro", "UGM");

        Assert.Null(result.Bus);
    }

    [Fact]
    public async Task GetRoutesAsync_JalanKakiTepatDuaKm_Diterima()
    {
        var handler = new StubHandler(Response(
            Walk2000,
            Bus(3000, "1A")));

        TransitRouteResult result = await CreateService(handler)
            .GetRoutesAsync("Malioboro", "UGM");

        Assert.NotNull(result.Bus);
    }

    [Fact]
    public async Task GetRoutesAsync_TidakAdaRute_MengembalikanKosong()
    {
        var handler = new StubHandler("""{"routes":[]}""");

        TransitRouteResult result = await CreateService(handler)
            .GetRoutesAsync("Malioboro", "UGM");

        Assert.False(result.HasAny);
    }

    [Fact]
    public async Task GetRoutesAsync_RequestMemakaiTransitDanFieldMask()
    {
        var handler = new StubHandler(Response(Walk500, Bus(3000, "1A")));

        await CreateService(handler).GetRoutesAsync("Malioboro", "UGM");

        Assert.Contains("\"travelMode\":\"TRANSIT\"", handler.LastRequestBody);
        Assert.Contains("\"computeAlternativeRoutes\":true", handler.LastRequestBody);
        Assert.Contains("LESS_WALKING", handler.LastRequestBody);
        Assert.Contains(handler.LastRequest!.Headers, h => h.Key == "X-Goog-FieldMask");
    }

    [Fact]
    public async Task GetRoutesAsync_KeyPlaceholder_MelemparInvalidOperation()
    {
        var handler = new StubHandler("""{"routes":[]}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(handler, "GANTI_DENGAN_GOOGLE_MAPS_API_KEY")
                .GetRoutesAsync("Malioboro", "UGM"));
    }

    [Fact]
    public async Task GetRoutesAsync_HttpError_MelemparHttpRequestException()
    {
        var handler = new StubHandler("{}", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService(handler).GetRoutesAsync("Malioboro", "UGM"));
    }

    private sealed class StubHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}
