using CRoute.Core.Models;

namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak pengecekan ketersediaan Bus dan Kereta untuk satu rute.
/// Implementasi memanggil Google Maps Routes API dengan mode TRANSIT lalu
/// memfilter rute yang benar-benar bus saja atau kereta saja.
/// </summary>
public interface ITransitRouteService
{
    /// <summary>
    /// Mengembalikan opsi Bus dan/atau Kereta yang tersedia. Properti bernilai
    /// <c>null</c> berarti moda tersebut tidak tersedia untuk rute ini.
    /// </summary>
    Task<TransitRouteResult> GetRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default);
}
