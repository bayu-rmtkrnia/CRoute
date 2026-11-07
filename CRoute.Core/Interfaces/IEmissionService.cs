using CRoute.Core.Models;

namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak perhitungan emisi untuk sebuah rute dengan moda tertentu.
/// </summary>
public interface IEmissionService
{
    /// <summary>
    /// Menghitung emisi CO2 sebuah rute untuk moda transportasi tertentu.
    /// Tetap sinkron karena perhitungannya murni aritmetika lokal atas
    /// <see cref="TransportMode.EmissionFactor"/> yang sudah ada di Model.
    /// Pengambilan faktor emisi dari jaringan dipisah ke method async
    /// tersendiri di implementasinya.
    /// </summary>
    EmissionResult Calculate(Route route, TransportMode mode);
}
