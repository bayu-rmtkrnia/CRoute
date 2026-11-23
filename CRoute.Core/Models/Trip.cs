namespace CRoute.Core.Models;

/// <summary>
/// Satu perjalanan: sebuah rute beserta hasil perhitungan emisi
/// dari beberapa moda transportasi yang dibandingkan.
/// </summary>
public class Trip
{
    private readonly List<EmissionResult> _results = new();
    private Route _route = null!;

    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime TripDate { get; set; } = DateTime.Now;

    /// <summary>Rute yang ditempuh pada perjalanan ini. Wajib diisi.</summary>
    public Route Route
    {
        get => _route;
        set => _route = value ?? throw new ArgumentNullException(nameof(Route), "Rute wajib diisi.");
    }

    /// <summary>Hasil emisi per moda. Hanya bisa ditambah lewat <see cref="AddResult"/>.</summary>
    public IReadOnlyList<EmissionResult> Results => _results;

    /// <summary>
    /// Moda yang benar-benar dipakai pengguna untuk perjalanan ini. Nilainya
    /// <c>null</c> untuk data lama atau sebelum pengguna memilih. Saat membaca
    /// data lama, <see cref="GetChosenOption"/> memakai opsi terbaik sebagai
    /// fallback supaya perilaku lama tidak berubah.
    /// </summary>
    public Guid? ChosenModeId { get; set; }

    public void AddResult(EmissionResult r)
    {
        ArgumentNullException.ThrowIfNull(r);

        _results.Add(r);
    }

    /// <summary>
    /// Opsi yang dipilih pengguna. Kalau belum ada pilihan (data lama) atau
    /// moda pilihan tidak ditemukan lagi, hasilnya fallback ke opsi terbaik.
    /// </summary>
    public EmissionResult? GetChosenOption()
    {
        if (ChosenModeId is Guid chosenModeId)
        {
            foreach (var result in _results)
            {
                if (result.Mode.Id == chosenModeId)
                    return result;
            }
        }

        return GetBestOption();
    }

    /// <summary>
    /// Opsi dengan emisi paling kecil, atau <c>null</c> kalau belum ada hasil sama sekali.
    /// </summary>
    public EmissionResult? GetBestOption()
    {
        EmissionResult? best = null;

        foreach (var result in _results)
        {
            if (best is null || result.TotalEmissionKg < best.TotalEmissionKg)
                best = result;
        }

        return best;
    }
}
