namespace CRoute.App.Services;

/// <summary>
/// Pindah halaman di dalam MainWindow. Dipakai ViewModel halaman supaya
/// tidak perlu tahu soal View/Window sama sekali.
/// </summary>
public interface INavigationService
{
    void NavigateTo(AppPage page);
}
