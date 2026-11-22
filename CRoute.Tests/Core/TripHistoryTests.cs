using CRoute.Core.Models;

namespace CRoute.Tests.Core;

public class TripHistoryTests
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

    private static Trip CreateTrip(DateTime date, double bestEmissionKg, double otherEmissionKg = double.NaN)
    {
        var trip = new Trip
        {
            TripDate = date,
            Route = CreateRoute(),
        };

        TransportMode mode = CreateMode();
        trip.AddResult(new EmissionResult { Mode = mode, TotalEmissionKg = bestEmissionKg });

        if (!double.IsNaN(otherEmissionKg))
            trip.AddResult(new EmissionResult { Mode = mode, TotalEmissionKg = otherEmissionKg });

        return trip;
    }

    [Fact]
    public void GetMonthlyTotal_BulanTertentu_MenjumlahkanOpsiTerbaikSaja()
    {
        var history = new TripHistory();
        history.AddTrip(CreateTrip(new DateTime(2026, 11, 15), bestEmissionKg: 1.5, otherEmissionKg: 2.0));
        history.AddTrip(CreateTrip(new DateTime(2026, 11, 20), bestEmissionKg: 0.5));
        history.AddTrip(CreateTrip(new DateTime(2026, 10, 31), bestEmissionKg: 9.0));

        double total = history.GetMonthlyTotal(2026, 11);

        Assert.Equal(2.0, total, 10);
    }

    [Fact]
    public void GetMonthlyTotal_TanpaTrip_MengembalikanNol()
    {
        var history = new TripHistory();

        Assert.Equal(0, history.GetMonthlyTotal(2026, 11));
    }

    [Fact]
    public void GetMonthlyTotal_BulanTidakValid_MelemparArgumentOutOfRange()
    {
        var history = new TripHistory();

        Assert.Throws<ArgumentOutOfRangeException>(() => history.GetMonthlyTotal(2026, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => history.GetMonthlyTotal(2026, 13));
    }

    [Fact]
    public void GetMonthlyTotal_BulanBerjalan_MengabaikanBulanLain()
    {
        var history = new TripHistory();
        DateTime now = DateTime.Now;

        history.AddTrip(CreateTrip(now, bestEmissionKg: 1.0));
        history.AddTrip(CreateTrip(now.AddMonths(-1), bestEmissionKg: 5.0));

        Assert.Equal(1.0, history.GetMonthlyTotal(), 10);
    }

    [Fact]
    public void GetMonthlyTotal_TripUtcDiBatasBulan_MemakaiWaktuJakarta()
    {
        var history = new TripHistory();

        // 30 November 2026 pukul 17.00 UTC = 1 Desember 2026 pukul 00.00 WIB.
        history.AddTrip(CreateTrip(
            new DateTime(2026, 11, 30, 17, 0, 0, DateTimeKind.Utc),
            bestEmissionKg: 2.0));

        Assert.Equal(0, history.GetMonthlyTotal(2026, 11), 10);
        Assert.Equal(2.0, history.GetMonthlyTotal(2026, 12), 10);
    }

    [Fact]
    public void GetMonthlyTotal_TripTanpaHasil_DihitungNol()
    {
        var history = new TripHistory();
        history.AddTrip(new Trip
        {
            TripDate = new DateTime(2026, 11, 10),
            Route = CreateRoute(),
        });

        Assert.Equal(0, history.GetMonthlyTotal(2026, 11));
    }

    [Fact]
    public void GetProgress_TargetNol_MengembalikanNol()
    {
        var history = new TripHistory();
        history.AddTrip(CreateTrip(DateTime.Now, bestEmissionKg: 5.0));

        Assert.Equal(0, history.GetProgress());
    }

    [Fact]
    public void GetProgress_SetengahTarget_MengembalikanNolKomaLima()
    {
        var history = new TripHistory { MonthlyTargetKg = 10 };
        history.AddTrip(CreateTrip(DateTime.Now, bestEmissionKg: 5.0));

        Assert.Equal(0.5, history.GetProgress(), 10);
    }

    [Fact]
    public void GetProgress_TargetTerlampaui_MengembalikanLebihDariSatu()
    {
        var history = new TripHistory { MonthlyTargetKg = 5 };
        history.AddTrip(CreateTrip(DateTime.Now, bestEmissionKg: 7.5));

        Assert.Equal(1.5, history.GetProgress(), 10);
    }

    [Fact]
    public void MonthlyTarget_Negatif_MelemparArgumentOutOfRange()
    {
        var history = new TripHistory();

        Assert.Throws<ArgumentOutOfRangeException>(() => history.MonthlyTargetKg = -1);
    }

    [Fact]
    public void MonthlyTarget_NaN_MelemparArgumentException()
    {
        var history = new TripHistory();

        Assert.Throws<ArgumentException>(() => history.MonthlyTargetKg = double.NaN);
    }
}
