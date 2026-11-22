using CRoute.Core.Models;

namespace CRoute.Core.Interfaces;

/// <summary>
/// Kontrak penyedia data rute. Implementasi asli ada di CRoute.Services,
/// sementara CRoute.App bisa jalan duluan dengan implementasi fake.
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
