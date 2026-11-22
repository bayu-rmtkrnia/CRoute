using System.Security.Cryptography;
using CRoute.Core.Interfaces;
using Npgsql;

namespace CRoute.Data;

/// <summary>
/// Implementasi <see cref="IAuthService"/> yang menyimpan akun di PostgreSQL.
/// Password tetap di-hash dengan PBKDF2-HMAC-SHA256 di sisi aplikasi;
/// database hanya menyimpan hash dan salt.
/// </summary>
public sealed class PostgresAuthService : IAuthService
{
    private const int Iterations = 100_000;
    private const int HashLength = 32;
    private const string Algorithm = "pbkdf2-sha256";
    private const int MinPasswordLength = 6;

    private readonly string _connectionString;

    public PostgresAuthService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string database belum diisi.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public Guid? CurrentUserId { get; private set; }

    public string? CurrentUserFullName { get; private set; }

    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return false;

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("""
            SELECT id,
                   full_name,
                   password_hash,
                   password_salt,
                   password_iterations,
                   password_algorithm
            FROM users
            WHERE lower(username) = lower(@username)
            LIMIT 1
            """, connection);

        command.Parameters.AddWithValue("username", username.Trim());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return false;

        Guid id = reader.GetGuid(0);
        string fullName = reader.GetString(1);
        byte[] expectedHash = reader.GetFieldValue<byte[]>(2);
        byte[] salt = reader.GetFieldValue<byte[]>(3);
        int iterations = reader.GetInt32(4);
        string algorithm = reader.GetString(5);

        if (!string.Equals(algorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
            return false;

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            return false;

        CurrentUserId = id;
        CurrentUserFullName = fullName;
        return true;
    }

    public bool Register(string fullName, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
            return false;

        // UI sudah memvalidasi, tetapi service tetap menjaga kalau dipanggil
        // dari tempat lain (mis. skrip/tes) agar tidak ada akun berpassword lemah.
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            throw new ArgumentException(
                $"Password minimal {MinPasswordLength} karakter.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand("""
            INSERT INTO users
                (full_name, username, password_hash, password_salt,
                 password_iterations, password_algorithm)
            VALUES
                (@full_name, lower(@username), @hash, @salt,
                 @iterations, @algorithm)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("full_name", fullName.Trim());
        command.Parameters.AddWithValue("username", username.Trim());
        command.Parameters.AddWithValue("hash", hash);
        command.Parameters.AddWithValue("salt", salt);
        command.Parameters.AddWithValue("iterations", Iterations);
        command.Parameters.AddWithValue("algorithm", Algorithm);

        try
        {
            // Trigger trg_users_default_settings membuat baris settings.
            command.ExecuteScalar();
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    public void Logout()
    {
        CurrentUserId = null;
        CurrentUserFullName = null;
    }
}
