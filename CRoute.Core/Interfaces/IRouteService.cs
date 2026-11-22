using CRoute.Core.Models;

namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak penyedia data rute. Implementasi Google Maps ada di CRoute.Services;
/// CRoute.App memakai implementasi fake kalau API key belum diisi.
/// </summary>
public interface IRouteService
{
    /// <summary>
    /// Mengambil rute dari titik asal ke titik tujuan. Async karena
    /// implementasi aslinya memanggil API lewat jaringan, jadi ViewModel
    /// bisa await tanpa nge-freeze UI thread WPF.
    /// </summary>
    Task<Route> GetRouteAsync(string origin, string destination);
}
