using System.Text.Json;
using CRoute.Core.Interfaces;
using CRoute.Core.Models;
using Npgsql;
using NpgsqlTypes;

namespace CRoute.Data;

/// <summary>
/// Implementasi <see cref="ITripHistoryRepository"/> dengan Npgsql.
/// Memakai tabel dan function dari <c>docs/database</c>.
/// </summary>
public sealed class PostgresTripHistoryRepository : ITripHistoryRepository
{
    private readonly string _connectionString;
    private readonly Func<Guid> _getUserId;

    public PostgresTripHistoryRepository(string connectionString, Func<Guid> getUserId)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string database belum diisi.", nameof(connectionString));

        _connectionString = connectionString;
        _getUserId = getUserId ?? throw new ArgumentNullException(nameof(getUserId));
    }

    private Guid UserId => _getUserId();

    public TripHistory GetHistory()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var history = new TripHistory
        {
            MonthlyTargetKg = GetMonthlyTarget(connection),
        };

        var trips = new Dictionary<Guid, Trip>();

        using var command = new NpgsqlCommand("""
            SELECT t.trip_id,
                   t.date,
                   r.route_id,
                   r.origin,
                   r.destination,
                   r.distance_km,
                   r.duration,
                   er.emission_id,
                   er.total_emission_kg,
                   tm.transport_mode_id,
                   tm.name,
                   tm.icon,
                   er.emission_factor
            FROM trips t
            JOIN routes r ON r.trip_id = t.trip_id
            LEFT JOIN emission_results er ON er.trip_id = t.trip_id
            LEFT JOIN transport_modes tm ON tm.transport_mode_id = er.transport_mode_id
            WHERE t.user_id = @user_id
            ORDER BY t.date DESC, er.total_emission_kg ASC
            """, connection);

        command.Parameters.AddWithValue("user_id", UserId);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                Guid tripId = reader.GetGuid(0);

                if (!trips.TryGetValue(tripId, out Trip? trip))
                {
                    trip = new Trip
                    {
                        Id = tripId,
                        TripDate = DateTime
                            .SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc)
                            .ToLocalTime(),
                        Route = new Route
                        {
                            Id = reader.GetGuid(2),
                            Origin = reader.GetString(3),
                            Destination = reader.GetString(4),
                            DistanceKm = (double)reader.GetDecimal(5),
                            DurationMin = reader.GetInt32(6),
                        },
                    };

                    trips.Add(tripId, trip);
                }

                // Trip lama bisa saja belum punya hasil emisi.
                if (reader.IsDBNull(7))
                    continue;

                var mode = new TransportMode
                {
                    Id = reader.GetGuid(9),
                    Name = reader.GetString(10),
                    IconName = reader.GetString(11),
                    EmissionFactor = (double)reader.GetDecimal(12),
                };

                trip.AddResult(new EmissionResult
                {
                    Id = reader.GetGuid(7),
                    Mode = mode,
                    TotalEmissionKg = (double)reader.GetDecimal(8),
                });
            }
        }

        foreach (Trip trip in trips.Values.OrderBy(t => t.TripDate))
            history.AddTrip(trip);

        return history;
    }

    public void AddTrip(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);

        if (trip.Results.Count == 0)
            throw new InvalidOperationException("Perjalanan belum punya hasil emisi.");

        string resultsJson = JsonSerializer.Serialize(trip.Results.Select(r => new
        {
            transport_mode_id = r.Mode.Id,
            total_emission_kg = r.TotalEmissionKg,
            emission_factor = r.Mode.EmissionFactor,
        }));

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("""
            SELECT fn_save_trip(
                @user_id,
                @origin,
                @destination,
                @distance_km,
                @duration,
                @results::jsonb,
                @date)
            """, connection);

        command.Parameters.AddWithValue("user_id", UserId);
        command.Parameters.AddWithValue("origin", trip.Route.Origin);
        command.Parameters.AddWithValue("destination", trip.Route.Destination);
        command.Parameters.AddWithValue("distance_km", (decimal)trip.Route.DistanceKm);
        command.Parameters.AddWithValue("duration", trip.Route.DurationMin);

        command.Parameters.Add(new NpgsqlParameter("date", NpgsqlDbType.TimestampTz)
        {
            Value = trip.TripDate.ToUniversalTime(),
        });

        command.Parameters.Add(new NpgsqlParameter("results", NpgsqlDbType.Jsonb)
        {
            Value = resultsJson,
        });

        // Function mengembalikan UUID trip. ID ini dibuat database,
        // sedangkan objek Trip di memori tidak dipakai lagi setelah disimpan.
        command.ExecuteScalar();
    }

    public void SetMonthlyTarget(double targetKg)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT fn_set_monthly_target(@user_id, @target)", connection);

        command.Parameters.AddWithValue("user_id", UserId);
        command.Parameters.AddWithValue("target", (decimal)targetKg);

        command.ExecuteScalar();
    }

    public void ClearTrips()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT fn_clear_trips(@user_id)", connection);

        command.Parameters.AddWithValue("user_id", UserId);

        command.ExecuteScalar();
    }

    private double GetMonthlyTarget(NpgsqlConnection connection)
    {
        using var command = new NpgsqlCommand(
            "SELECT fn_get_monthly_target(@user_id)", connection);

        command.Parameters.AddWithValue("user_id", UserId);

        return Convert.ToDouble(command.ExecuteScalar());
    }
}
