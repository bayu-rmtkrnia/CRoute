using CRoute.Core.Models;
using CRoute.Services;
using CRoute.Services.Configuration;

namespace CRoute.Tests.Services;

public class EmissionServiceTests
{
    private static Route CreateRoute(double km = 10) => new()
    {
        Origin = "Kampus",
        Destination = "Stasiun",
        DistanceKm = km,
        DurationMin = 30,
    };

    private static TransportMode CreateMode(double factor = 0.171) => new()
    {
        Name = "Mobil",
        EmissionFactor = factor,
        IconName = "car",
    };

    [Fact]
    public void Calculate_MenggunakanJarakDikaliFaktorModa()
    {
        var service = new EmissionService(new HttpClient(), new ApiSettings());

        EmissionResult result = service.Calculate(CreateRoute(10), CreateMode(0.171));

        Assert.Equal(1.71, result.TotalEmissionKg, 10);
        Assert.Equal("Mobil", result.Mode.Name);
    }

    [Fact]
    public async Task RefreshEmissionFactor_KeyPlaceholder_MelemparInvalidOperation()
    {
        var settings = new ApiSettings
        {
            Climatiq = new ClimatiqSettings
            {
                ApiKey = "GANTI_DENGAN_CLIMATIQ_API_KEY",
                BaseUrl = "https://api.test/estimate",
            },
        };
        var service = new EmissionService(new HttpClient(), settings);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshEmissionFactorAsync(CreateMode()));
    }
}
