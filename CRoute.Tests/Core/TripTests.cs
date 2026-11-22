using CRoute.Core.Models;

namespace CRoute.Tests.Core;

public class TripTests
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

    private static EmissionResult CreateResult(double kg, TransportMode? mode = null) => new()
    {
        Mode = mode ?? CreateMode(),
        TotalEmissionKg = kg,
    };

    [Fact]
    public void GetBestOption_TanpaHasil_MengembalikanNull()
    {
        var trip = new Trip { Route = CreateRoute() };

        Assert.Null(trip.GetBestOption());
    }

    [Fact]
    public void GetBestOption_BeberapaHasil_MengembalikanEmisiTerkecil()
    {
        var trip = new Trip { Route = CreateRoute() };
        trip.AddResult(CreateResult(1.03));
        trip.AddResult(CreateResult(0.41));
        trip.AddResult(CreateResult(0.89));

        EmissionResult? best = trip.GetBestOption();

        Assert.NotNull(best);
        Assert.Equal(0.41, best!.TotalEmissionKg, 10);
    }

    [Fact]
    public void Route_Null_MelemparArgumentNullException()
    {
        var trip = new Trip();

        Assert.Throws<ArgumentNullException>(() => trip.Route = null!);
    }

    [Fact]
    public void AddResult_Null_MelemparArgumentNullException()
    {
        var trip = new Trip { Route = CreateRoute() };

        Assert.Throws<ArgumentNullException>(() => trip.AddResult(null!));
    }
}
