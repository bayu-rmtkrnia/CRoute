using CRoute.Core.Models;

namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak penyimpanan riwayat perjalanan. Implementasi asli nanti ada di
/// CRoute.Data (SQLite); sebelum itu CRoute.App pakai implementasi fake.
/// </summary>
public interface ITripHistoryRepository
{
    /// <summary>
    /// Riwayat perjalanan pengguna. Perubahan data harus lewat method di
    /// repository ini (bukan langsung ke <see cref="TripHistory"/>) supaya ikut tersimpan.
    /// </summary>
    TripHistory GetHistory();

    /// <summary>Menyimpan perjalanan baru ke riwayat.</summary>
    void AddTrip(Trip trip);

    /// <summary>Mengubah target emisi bulanan (kg CO2).</summary>
    void SetMonthlyTarget(double targetKg);

    /// <summary>Menghapus semua perjalanan; target bulanan tetap.</summary>
    void ClearTrips();
}
