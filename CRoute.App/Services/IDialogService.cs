namespace CRoute.App.Services;

/// <summary>
/// Dialog pesan/konfirmasi. Dibungkus interface supaya ViewModel tidak
/// memanggil MessageBox langsung (dan tetap bisa dites tanpa UI).
/// </summary>
public interface IDialogService
{
    void ShowInfo(string message);

    /// <summary>Menampilkan konfirmasi Ya/Tidak. <c>true</c> kalau pengguna memilih Ya.</summary>
    bool Confirm(string message);
}
