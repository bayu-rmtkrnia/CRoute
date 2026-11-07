namespace CRoute.Core.Models;

public class EmissionResult
{
    private double _totalEmissionKg;
    private TransportMode _mode = null!;

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Total emisi CO2 dalam kg. Tidak boleh negatif.</summary>
    public double TotalEmissionKg
    {
        get => _totalEmissionKg;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Total emisi harus berupa angka valid.", nameof(TotalEmissionKg));
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(TotalEmissionKg), value, "Total emisi tidak boleh negatif.");

            _totalEmissionKg = value;
        }
    }

    /// <summary>Moda transportasi yang dipakai untuk hasil ini wajib diisi.</summary>
    public TransportMode Mode
    {
        get => _mode;
        set => _mode = value ?? throw new ArgumentNullException(nameof(Mode), "Moda transportasi wajib diisi.");
    }
}
