namespace CRoute.Core.Models;

/// <summary>
/// Riwayat perjalanan pengguna beserta target emisi bulanannya.
/// </summary>
public class TripHistory
{
    private readonly List<Trip> _trips = new();
    private double _monthlyTargetKg;

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Target emisi maksimal per bulan dalam kg CO2. Tidak boleh negatif.</summary>
    public double MonthlyTargetKg
    {
        get => _monthlyTargetKg;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Target bulanan harus berupa angka valid.", nameof(MonthlyTargetKg));
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MonthlyTargetKg), value, "Target bulanan tidak boleh negatif.");

            _monthlyTargetKg = value;
        }
    }

    /// <summary>Perjalanan yang tercatat. Hanya bisa ditambah lewat <see cref="AddTrip"/>.</summary>
    public IReadOnlyList<Trip> Trips => _trips;

    public void AddTrip(Trip t)
    {
        ArgumentNullException.ThrowIfNull(t);

        _trips.Add(t);
    }

    /// <summary>
    /// Total emisi bulan berjalan, dihitung dari opsi terbaik tiap perjalanan.
    /// Perjalanan yang belum punya hasil emisi dianggap 0.
    /// </summary>
    public double GetMonthlyTotal()
    {
        var now = DateTime.Now;

        return GetMonthlyTotal(now.Year, now.Month);
    }

    /// <summary>Total emisi untuk bulan dan tahun tertentu.</summary>
    public double GetMonthlyTotal(int year, int month)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "Bulan harus antara 1 sampai 12.");

        double total = 0;

        foreach (var trip in _trips)
        {
            if (trip.TripDate.Year != year || trip.TripDate.Month != month)
                continue;

            total += trip.GetBestOption()?.TotalEmissionKg ?? 0;
        }

        return total;
    }

    /// <summary>
    /// Rasio pemakaian terhadap target bulanan (0 = belum ada emisi, 1 = pas target).
    /// Nilainya bisa lebih dari 1 kalau target terlampaui. Kalau target belum
    /// diatur (0), hasilnya 0 supaya tidak terjadi pembagian dengan nol.
    /// </summary>
    public double GetProgress()
    {
        if (MonthlyTargetKg == 0)
            return 0;

        return GetMonthlyTotal() / MonthlyTargetKg;
    }
}
