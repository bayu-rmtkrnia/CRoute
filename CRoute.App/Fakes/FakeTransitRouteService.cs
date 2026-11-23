using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.Fakes;

/// <summary>
/// Dipakai kalau API key Google Maps belum diisi. Tidak ada data transit,
/// jadi Bus dan Kereta tidak ditampilkan.
/// </summary>
public class FakeTransitRouteService : ITransitRouteService
{
    public Task<TransitRouteResult> GetRoutesAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(TransitRouteResult.Empty);
    }
}
