namespace CRoute.App.ViewModels;

/// <summary>Satu batang di grafik emisi 7 hari terakhir (halaman Statistik).</summary>
public class DailyEmissionBarViewModel
{
    public string Label { get; init; } = string.Empty;

    public string ValueText { get; init; } = string.Empty;

    /// <summary>Tinggi batang relatif terhadap hari dengan emisi tertinggi (0..1).</summary>
    public double Ratio { get; init; }
}
