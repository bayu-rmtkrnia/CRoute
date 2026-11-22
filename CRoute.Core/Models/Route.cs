namespace CRoute.Core.Models;

///<summary>
/// Rute perjalanan dari satu titik ke titik lain beserta jarak dan durasinya.
///</summary>
public class Route
{
    private string _origin = string.Empty;
    private string _destination = string.Empty;
    private double _distanceKm;
    private int _durationMin;

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Titik asal. Tidak boleh kosong.</summary>
    public string Origin
    {
        get => _origin;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Titik asal tidak boleh kosong.", nameof(Origin));

            _origin = value.Trim();
        }
    }

    /// <summary>Titik tujuan. Tidak boleh kosong.</summary>
    public string Destination
    {
        get => _destination;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Titik tujuan tidak boleh kosong.", nameof(Destination));

            _destination = value.Trim();
        }
    }

    /// <summary>Jarak tempuh dalam km. Tidak boleh negatif.</summary>
    public double DistanceKm
    {
        get => _distanceKm;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Jarak harus berupa angka valid.", nameof(DistanceKm));
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(DistanceKm), value, "Jarak tidak boleh negatif.");

            _distanceKm = value;
        }
    }

    /// <summary>Estimasi durasi dalam menit. Tidak boleh negatif.</summary>
    public int DurationMin
    {
        get => _durationMin;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(DurationMin), value, "Durasi tidak boleh negatif.");

            _durationMin = value;
        }
    }
}
