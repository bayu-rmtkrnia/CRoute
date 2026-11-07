using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.Fakes;

/// <summary>
/// Riwayat perjalanan di memori, diisi beberapa contoh perjalanan.
/// Hilang saat aplikasi ditutup; versi permanennya nanti di CRoute.Data (SQLite).
/// </summary>
public class FakeTripHistoryRepository : ITripHistoryRepository
{
    private TripHistory _history = new() { MonthlyTargetKg = 15.0 };

    public FakeTripHistoryRepository(ITransportModeProvider modes)
    {
        ArgumentNullException.ThrowIfNull(modes);

        Seed(modes, 1, "Kaliurang, Sleman", "UGM, Bulaksumur", 12.4, "Bus");
        Seed(modes, 2, "Malioboro", "Stasiun Tugu", 2.1, "Jalan kaki");
        Seed(modes, 3, "UGM, Bulaksumur", "Bandara YIA", 42.0, "Kereta");
        Seed(modes, 5, "Condongcatur", "UGM, Bulaksumur", 8.3, "Sepeda");
        Seed(modes, 6, "Sleman City Hall", "UGM, Bulaksumur", 9.6, "Motor");
    }

    public TripHistory GetHistory() => _history;

    public void AddTrip(Trip trip) => _history.AddTrip(trip);

    public void SetMonthlyTarget(double targetKg) => _history.MonthlyTargetKg = targetKg;

    public void ClearTrips()
    {
        _history = new TripHistory { MonthlyTargetKg = _history.MonthlyTargetKg };
    }

    private void Seed(ITransportModeProvider modes, int daysAgo, string origin, string destination,
                      double km, string modeName)
    {
        TransportMode mode = modes.GetAll().First(m => m.Name == modeName);

        var trip = new Trip
        {
            TripDate = DateTime.Today.AddDays(-daysAgo).AddHours(8),
            Route = new Route
            {
                Origin = origin,
                Destination = destination,
                DistanceKm = km,
                DurationMin = FakeRouteService.EstimateDurationMin(km),
            },
        };
        trip.AddResult(new EmissionResult { Mode = mode, TotalEmissionKg = mode.CalculateEmission(km) });

        _history.AddTrip(trip);
    }
}
