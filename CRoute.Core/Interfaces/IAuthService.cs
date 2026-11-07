namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak autentikasi pengguna (login, daftar, logout).
/// </summary>
public interface IAuthService
{
    /// <summary>Nama lengkap pengguna yang sedang login, atau <c>null</c> kalau belum login.</summary>
    string? CurrentUserFullName { get; }

    /// <summary>Mencoba login. Mengembalikan <c>false</c> kalau username/password salah.</summary>
    bool Login(string username, string password);

    /// <summary>Mendaftarkan akun baru. Mengembalikan <c>false</c> kalau username sudah dipakai.</summary>
    bool Register(string fullName, string username, string password);

    void Logout();
}
