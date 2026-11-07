using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.Fakes;

/// <summary>
/// Perhitungan emisi lokal murni dari <see cref="TransportMode.EmissionFactor"/>.
/// </summary>
public class FakeEmissionService : IEmissionService
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
