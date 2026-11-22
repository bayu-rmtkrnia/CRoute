using CRoute.Core.Models;

namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak penyedia daftar moda transportasi beserta faktor emisinya.
/// </summary>
public interface ITransportModeProvider
{
    /// <summary>Semua moda yang bisa dibandingkan.</summary>
    IReadOnlyList<TransportMode> GetAll();

    /// <summary>
    /// Moda acuan untuk menghitung penghematan emisi (mis. mobil pribadi).
    /// Selalu salah satu moda dari <see cref="GetAll"/>.
    /// </summary>
    TransportMode GetBaseline();
}
