using System.Security.Cryptography;
using CRoute.Core.Interfaces;

namespace CRoute.App.Fakes;

/// <summary>
/// Akun hanya disimpan di memori (hilang saat aplikasi ditutup).
/// Akun demo bawaan: username <c>demo</c>, password <c>demo123</c>.
/// </summary>
public class FakeAuthService : IAuthService
{
    private record Account(string FullName, byte[] Salt, byte[] Hash);

    private readonly Dictionary<string, Account> _accounts = new(StringComparer.OrdinalIgnoreCase);

    public FakeAuthService()
    {
        Register("Akun Demo", "demo", "demo123");
    }

    public string? CurrentUserFullName { get; private set; }

    public bool Login(string username, string password)
    {
        if (!_accounts.TryGetValue(username, out Account? account))
            return false;

        byte[] attempt = Hash(password, account.Salt);
        if (!CryptographicOperations.FixedTimeEquals(attempt, account.Hash))
            return false;

        CurrentUserFullName = account.FullName;
        return true;
    }

    public bool Register(string fullName, string username, string password)
    {
        if (_accounts.ContainsKey(username))
            return false;

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        _accounts[username] = new Account(fullName, salt, Hash(password, salt));
        return true;
    }

    public void Logout()
    {
        CurrentUserFullName = null;
    }

    private static byte[] Hash(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }
}
