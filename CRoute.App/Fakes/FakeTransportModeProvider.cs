using CRoute.Core.Interfaces;
using CRoute.Core.Models;

namespace CRoute.App.Fakes;

/// <summary>
/// Daftar moda dengan faktor emisi statis (kg CO2/km) sampai data Climatiq tersedia.
/// </summary>
public class FakeTransportModeProvider : ITransportModeProvider
{
    private readonly List<TransportMode> _modes =
    [
        new TransportMode { Name = "Mobil",      IconName = "car",        EmissionFactor = 0.171 },
        new TransportMode { Name = "Motor",      IconName = "motorcycle", EmissionFactor = 0.103 },
        new TransportMode { Name = "Bus",        IconName = "bus",        EmissionFactor = 0.089 },
        new TransportMode { Name = "Kereta",     IconName = "train",      EmissionFactor = 0.041 },
        new TransportMode { Name = "Sepeda",     IconName = "bicycle",    EmissionFactor = 0.0 },
        new TransportMode { Name = "Jalan kaki", IconName = "walk",       EmissionFactor = 0.0 },
    ];

    public IReadOnlyList<TransportMode> GetAll() => _modes;

    /// <summary>Mobil pribadi jadi acuan "hemat dibanding naik mobil".</summary>
    public TransportMode GetBaseline() => _modes[0];
}
