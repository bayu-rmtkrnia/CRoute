namespace CRoute.Core.Models;

/// <summary>
/// Moda transportasi beserta faktor emisinya (kg CO2 per km).
/// </summary>
public class TransportMode
{
    private string _name = string.Empty;
    private double _emissionFactor;
    private string _iconName = string.Empty;

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nama moda, mis. "Mobil", "Bus", "Sepeda". Tidak boleh kosong.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nama moda transportasi tidak boleh kosong.", nameof(Name));

            _name = value.Trim();
        }
    }

    /// <summary>Faktor emisi dalam kg CO2 per km. Tidak boleh negatif.</summary>
    public double EmissionFactor
    {
        get => _emissionFactor;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Faktor emisi harus berupa angka valid.", nameof(EmissionFactor));
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(EmissionFactor), value, "Faktor emisi tidak boleh negatif.");

            _emissionFactor = value;
        }
    }

    /// <summary>Nama ikon yang mewakili moda ini. Tidak boleh kosong.</summary>
    public string IconName
    {
        get => _iconName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nama ikon tidak boleh kosong.", nameof(IconName));

            _iconName = value.Trim();
        }
    }

    /// <summary>
    /// Menghitung emisi CO2 (kg) untuk jarak tertentu. 
    /// </summary>
    public double CalculateEmission(double km)
    {
        if (double.IsNaN(km) || double.IsInfinity(km))
            throw new ArgumentException("Jarak harus berupa angka valid.", nameof(km));
        if (km < 0)
            throw new ArgumentOutOfRangeException(nameof(km), km, "Jarak tidak boleh negatif.");

        return km * EmissionFactor;
    }
}
