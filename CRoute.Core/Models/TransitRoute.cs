namespace CRoute.Core.Models;

/// <summary>
/// Satu opsi rute transit untuk Bus atau Kereta. Rute yang tidak memenuhi
/// syarat (moda campuran atau jalan kaki terlalu jauh) tidak dibuat.
/// </summary>
public sealed record TransitRouteOption(
    double TransitDistanceKm,
    double WalkDistanceKm,
    int DurationMin,
    IReadOnlyList<string> Lines)
{
    /// <summary>Jarak total door-to-door, termasuk jalan kaki.</summary>
    public double TotalDistanceKm => TransitDistanceKm + WalkDistanceKm;
}

/// <summary>
/// Hasil pengecekan rute transit. Properti bernilai <c>null</c> berarti moda
/// tersebut tidak tersedia untuk rute yang diminta, sehingga tidak ditampilkan.
/// </summary>
public sealed record TransitRouteResult(
    TransitRouteOption? Bus,
    TransitRouteOption? Train)
{
    public static TransitRouteResult Empty { get; } = new(null, null);

    public bool HasAny => Bus is not null || Train is not null;
}
