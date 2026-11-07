using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CRoute.App.Fakes;
using CRoute.App.Services;
using CRoute.App.ViewModels;
using CRoute.Core.Interfaces;

namespace CRoute.App;

/// <summary>
/// Composition root: semua dependency dirakit di sini lalu disuntikkan lewat
/// constructor ViewModel. Untuk pindah ke backend asli, cukup ganti
/// implementasi Fakes di <see cref="OnStartup"/> dengan kelas dari
/// CRoute.Services / CRoute.Data — ViewModel dan View tidak perlu diubah.
/// </summary>
public partial class App : Application
{
    private IAuthService _auth = null!;
    private IRouteService _routeService = null!;
    private IEmissionService _emissionService = null!;
    private ITransportModeProvider _modes = null!;
    private ITripHistoryRepository _history = null!;
    private IDialogService _dialogs = null!;
    private IWindowService _windows = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _auth = new FakeAuthService();
        _routeService = new FakeRouteService();
        _emissionService = new FakeEmissionService();
        _modes = new FakeTransportModeProvider();
        _history = new FakeTripHistoryRepository(_modes);
        _dialogs = new DialogService();
        _windows = new WindowService(CreateLoginViewModel, CreateRegisterViewModel, CreateMainViewModel);

        _windows.ShowLogin();
    }

    private LoginViewModel CreateLoginViewModel() => new(_auth, _windows);

    private RegisterViewModel CreateRegisterViewModel() => new(_auth, _windows, _dialogs);

    private MainViewModel CreateMainViewModel() => new(_auth, _windows, CreatePageViewModel);

    private ObservableObject CreatePageViewModel(AppPage page, INavigationService navigation) => page switch
    {
        AppPage.CariRute => new CariRuteViewModel(_routeService, _emissionService, _modes, _history, navigation),
        AppPage.History => new HistoryViewModel(_history, _modes, _dialogs, navigation),
        AppPage.Stats => new StatsViewModel(_history, _modes),
        _ => new DashboardViewModel(_auth, _history, _modes, navigation),
    };
}
