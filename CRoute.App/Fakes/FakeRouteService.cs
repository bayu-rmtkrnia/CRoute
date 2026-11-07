using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.Fakes;

/// <summary>
/// Rute palsu: jaraknya diturunkan dari teks asal/tujuan (bukan jarak nyata),
/// supaya hasilnya konsisten untuk input yang sama tanpa memanggil Google Maps.
/// </summary>
public class FakeRouteService : IRouteService
{
    private const double AverageSpeedKmh = 28.0;

    public async Task<Route> GetRouteAsync(string origin, string destination)
    {
        // Simulasi jeda jaringan supaya alur async di ViewModel ikut teruji.
        await Task.Delay(300);

        double km = EstimateDistanceKm(origin, destination);
        return new Route
        {
            Origin = origin,
            Destination = destination,
            DistanceKm = km,
            DurationMin = EstimateDurationMin(km),
        };
    }

    public static int EstimateDurationMin(double km)
    {
        return Math.Max(1, (int)Math.Round(km / AverageSpeedKmh * 60));
    }

    private static double EstimateDistanceKm(string origin, string destination)
    {
        int sum = 0;
        foreach (char c in (origin.Trim() + "|" + destination.Trim()).ToLowerInvariant())
            sum += c;
        return Math.Round(3 + (sum % 220) / 10.0, 1);
    }
}
