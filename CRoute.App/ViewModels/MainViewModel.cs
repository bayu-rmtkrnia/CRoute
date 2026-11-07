using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Services;
using CRoute.Core.Interfaces;

namespace CRoute.App.ViewModels;

/// <summary>
/// ViewModel MainWindow: menu samping, info pengguna, dan halaman aktif.
/// Halaman ditampilkan lewat DataTemplate (ViewModel → View) di App.xaml.
/// </summary>
public partial class MainViewModel : ObservableObject, INavigationService
{
    private readonly IAuthService _auth;
    private readonly IWindowService _windows;
    private readonly Func<AppPage, INavigationService, ObservableObject> _createPage;

    public MainViewModel(
        IAuthService auth,
        IWindowService windows,
        Func<AppPage, INavigationService, ObservableObject> createPage)
    {
        _auth = auth;
        _windows = windows;
        _createPage = createPage;

        UserFullName = auth.CurrentUserFullName ?? "Tamu";
        UserInitial = UserFullName.Length > 0 ? UserFullName[..1].ToUpperInvariant() : "T";

        NavigateTo(AppPage.Dashboard);
    }

    public string UserFullName { get; }

    public string UserInitial { get; }

    [ObservableProperty]
    private AppPage _currentPage;

    [ObservableProperty]
    private ObservableObject? _currentPageViewModel;

    /// <summary>
    /// ViewModel halaman dibuat ulang tiap navigasi supaya datanya selalu segar
    /// (mis. riwayat baru langsung muncul di Dashboard).
    /// </summary>
    public void NavigateTo(AppPage page)
    {
        CurrentPage = page;
        CurrentPageViewModel = _createPage(page, this);
    }

    [RelayCommand]
    private void Navigate(AppPage page) => NavigateTo(page);

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _windows.ShowLogin();
    }
}
