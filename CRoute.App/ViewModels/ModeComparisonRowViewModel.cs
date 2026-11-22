using CRoute.Core.Models;

namespace CRoute.App.ViewModels;

/// <summary>Tingkat emisi relatif, dipetakan ke warna oleh converter di View.</summary>
public enum EmissionLevel
{
    Low,
    Medium,
    High,
}

/// <summary>Satu batang di perbandingan emisi per moda (halaman Cari Rute).</summary>
public class ModeComparisonRowViewModel
{
    /// <param name="ratio">Emisi moda ini dibagi emisi tertinggi (0..1).</param>
    public ModeComparisonRowViewModel(EmissionResult result, bool isBest, double ratio)
    {
        ArgumentNullException.ThrowIfNull(result);

        Result = result;
        IsBest = isBest;
        Percent = ratio * 100;
        Level = ratio > 0.66 ? EmissionLevel.High
              : ratio > 0.33 ? EmissionLevel.Medium
              : EmissionLevel.Low;
    }

    public EmissionResult Result { get; }

    public TransportMode Mode => Result.Mode;

    public bool IsBest { get; }

    /// <summary>Panjang batang 0..100 relatif terhadap emisi tertinggi.</summary>
    public double Percent { get; }

    public EmissionLevel Level { get; }
}
