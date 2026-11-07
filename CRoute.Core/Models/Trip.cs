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

    public void AddResult(EmissionResult r)
    {
        ArgumentNullException.ThrowIfNull(r);

        _results.Add(r);
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
