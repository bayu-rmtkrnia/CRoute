namespace CRoute.App.Services;

/// <summary>
/// Berpindah antar-window (Login, Daftar, Utama). Window yang sedang
/// terbuka otomatis ditutup saat window baru ditampilkan.
/// </summary>
public interface IWindowService
{
    void ShowLogin();

    void ShowRegister();

    void ShowMain();

    void ExitApplication();
}
