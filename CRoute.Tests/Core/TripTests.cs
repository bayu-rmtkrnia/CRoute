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

    private static TransportMode CreateMode(double factor = 0.171, string name = "Mobil") => new()
    {
        Name = name,
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
    public void GetChosenOption_TanpaPilihan_MemakaiOpsiTerbaik()
    {
        var trip = new Trip { Route = CreateRoute() };
        TransportMode car = CreateMode();
        TransportMode bus = CreateMode(0.089, "Bus");
        trip.AddResult(CreateResult(0.63, car));
        trip.AddResult(CreateResult(0.28, bus));

        EmissionResult? chosen = trip.GetChosenOption();

        Assert.NotNull(chosen);
        Assert.Equal(0.28, chosen!.TotalEmissionKg, 10);
        Assert.Equal("Bus", chosen.Mode.Name);
    }

    [Fact]
    public void GetChosenOption_DenganPilihan_MengembalikanPilihanPengguna()
    {
        var trip = new Trip { Route = CreateRoute() };
        TransportMode car = CreateMode();
        TransportMode bus = CreateMode(0.089);
        trip.AddResult(CreateResult(0.63, car));
        trip.AddResult(CreateResult(0.28, bus));
        trip.ChosenModeId = car.Id;

        EmissionResult? chosen = trip.GetChosenOption();

        Assert.NotNull(chosen);
        Assert.Equal(0.63, chosen!.TotalEmissionKg, 10);
        Assert.Equal("Mobil", chosen.Mode.Name);
    }

    [Fact]
    public void GetChosenOption_PilihanTidakDitemukan_FallbackKeOpsiTerbaik()
    {
        var trip = new Trip { Route = CreateRoute() };
        trip.AddResult(CreateResult(0.63));
        trip.AddResult(CreateResult(0.28));
        trip.ChosenModeId = Guid.NewGuid();

        EmissionResult? chosen = trip.GetChosenOption();

        Assert.NotNull(chosen);
        Assert.Equal(0.28, chosen!.TotalEmissionKg, 10);
    }

    [Fact]
    public void GetChosenOption_TanpaHasil_MengembalikanNull()
    {
        var trip = new Trip { Route = CreateRoute() };

        Assert.Null(trip.GetChosenOption());
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
