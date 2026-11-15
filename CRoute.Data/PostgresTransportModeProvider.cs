using CRoute.Core.Interfaces;
using CRoute.Core.Models;
using Npgsql;

namespace CRoute.Data;

/// <summary>
/// Membaca daftar moda transportasi dari tabel <c>transport_modes</c>.
/// Hasilnya di-cache karena master data ini jarang berubah.
/// </summary>
public sealed class PostgresTransportModeProvider : ITransportModeProvider
{
    private readonly string _connectionString;
    private readonly List<TransportMode> _cache = [];
    private Guid? _baselineId;
    private bool _loaded;

    public PostgresTransportModeProvider(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string database belum diisi.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public IReadOnlyList<TransportMode> GetAll()
    {
        EnsureLoaded();
        return _cache;
    }

    public TransportMode GetBaseline()
    {
        EnsureLoaded();

        TransportMode? baseline = _cache.FirstOrDefault(m => m.Id == _baselineId);
        if (baseline is null)
        {
            throw new InvalidOperationException(
                "Moda transportasi baseline belum diatur di tabel transport_modes.");
        }

        return baseline;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("""
            SELECT transport_mode_id,
                   name,
                   icon,
                   emission_factor,
                   is_baseline
            FROM transport_modes
            WHERE is_active
            ORDER BY sort_order, name
            """, connection);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var mode = new TransportMode
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                IconName = reader.GetString(2),
                EmissionFactor = (double)reader.GetDecimal(3),
            };

            _cache.Add(mode);

            if (reader.GetBoolean(4))
                _baselineId = mode.Id;
        }

        _loaded = true;
    }
}
