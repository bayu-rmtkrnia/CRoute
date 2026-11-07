using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

/// <summary>
/// Hitungan "hemat dibanding moda acuan" (mis. mobil). Hasilnya angka murni,
/// dipakai bersama oleh beberapa ViewModel supaya rumusnya tidak di-copy-paste.
/// </summary>
public static class EmissionSavings
{
    public static double Calculate(Route route, EmissionResult chosen, TransportMode baseline)
    {
        return Math.Max(0, baseline.CalculateEmission(route.DistanceKm) - chosen.TotalEmissionKg);
    }

    /// <summary>Penghematan dari opsi terbaik sebuah perjalanan; 0 kalau belum ada hasil.</summary>
    public static double ForTrip(Trip trip, TransportMode baseline)
    {
        EmissionResult? best = trip.GetBestOption();
        return best is null ? 0 : Calculate(trip.Route, best, baseline);
    }
}
