using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.Services;

/// <summary>
/// Perhitungan emisi lokal dari <see cref="TransportMode.EmissionFactor"/>.
/// Faktor berasal dari tabel <c>transport_modes</c>; integrasi Climatiq ditunda,
/// jadi kelas ini adalah implementasi produksi saat ini.
/// </summary>
public class LocalEmissionService : IEmissionService
{
    public EmissionResult Calculate(Route route, TransportMode mode)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(mode);

        return new EmissionResult
        {
            Mode = mode,
            TotalEmissionKg = mode.CalculateEmission(route.DistanceKm),
        };
    }
}
