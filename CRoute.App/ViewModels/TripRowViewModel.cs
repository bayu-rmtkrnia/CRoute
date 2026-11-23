using CRoute.App.Converters;
using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

/// <summary>
/// Satu baris perjalanan di daftar (Dashboard dan Riwayat).
/// Ikon, judul rute, dan emisi di-format lewat converter di XAML.
/// </summary>
public class TripRowViewModel
{
    public TripRowViewModel(Trip trip, TransportMode baseline)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(baseline);

        Trip = trip;
        ChosenResult = trip.GetChosenOption();

        Subtitle = trip.TripDate.ToString("d MMM", DisplayCulture.Indonesian) + "  ·  " +
                   RouteFormatter.FormatDistance(trip.Route.DistanceKm) + "  ·  " +
                   (ChosenResult?.Mode.Name ?? "-");

        double saved = EmissionSavings.ForTrip(trip, baseline);
        SavedText = saved > 0 ? "hemat " + EmissionResultFormatter.FormatKg(saved) : string.Empty;
    }

    public Trip Trip { get; }

    public Route Route => Trip.Route;

    /// <summary>Moda yang dipilih pengguna pada perjalanan ini (fallback opsi terbaik untuk data lama).</summary>
    public EmissionResult? ChosenResult { get; }

    public string? IconName => ChosenResult?.Mode.IconName;

    public string Subtitle { get; }

    public string SavedText { get; }
}
