using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CRoute.App.Fakes;
using CRoute.App.Services;
using CRoute.App.ViewModels;
using CRoute.Core.Interfaces;
using CRoute.Data;
using CRoute.Services;
using CRoute.Services.Configuration;

namespace CRoute.App;

/// <summary>
/// Composition root: semua dependency dirakit di sini lalu disuntikkan lewat
/// constructor ViewModel. Auth, riwayat, dan master moda memakai PostgreSQL;
/// rute memakai Google Maps Routes API kalau API key sudah diisi (kalau belum,
/// jatuh ke <see cref="FakeRouteService"/>); emisi masih fake sampai
/// integrasi Climatiq dikerjakan.
/// </summary>
public partial class App : Application
{
    /// <summary>Satu HttpClient untuk seluruh aplikasi (jangan dibuat per request).</summary>
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

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

        ApiSettings settings;
        try
        {
            settings = ApiSettingsLoader.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Gagal membaca appsettings.json: " + ex.Message,
                "CRoute",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        string connectionString = settings.Database.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show(
                "Connection string database belum diatur. Isi Database:ConnectionString di appsettings.json " +
                "atau set environment variable CROUTE_Database__ConnectionString.",
                "CRoute",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        _auth = new PostgresAuthService(connectionString);
        _routeService = settings.GoogleMaps.HasApiKey
            ? new RouteService(Http, settings)
            : new FakeRouteService();
        _emissionService = new FakeEmissionService();
        _modes = new PostgresTransportModeProvider(connectionString);
        _history = new PostgresTripHistoryRepository(
            connectionString,
            () => _auth.CurrentUserId
                  ?? throw new InvalidOperationException("Pengguna belum login."));

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
