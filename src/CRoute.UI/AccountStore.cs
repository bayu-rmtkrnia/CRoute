using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace CRoute.UI
{
    public static class AccountStore
    {
        private record Account(string FullName, byte[] Salt, byte[] Hash);

        private static readonly Dictionary<string, Account> _accounts =
            new(StringComparer.OrdinalIgnoreCase);

        static AccountStore()
        {
            Register("Akun Demo", "demo", "demo123");
        }

        public static bool Register(string fullName, string username, string password)
        {
            if (_accounts.ContainsKey(username))
                return false;

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            _accounts[username] = new Account(fullName, salt, Hash(password, salt));
            return true;
        }

        public static string? Login(string username, string password)
        {
            if (!_accounts.TryGetValue(username, out Account? account))
                return null;

            byte[] attempt = Hash(password, account.Salt);
            return CryptographicOperations.FixedTimeEquals(attempt, account.Hash)
                ? account.FullName
                : null;
        }

        private static byte[] Hash(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        }
    }
}
