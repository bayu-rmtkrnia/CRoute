using System.Windows;
using CRoute.App.ViewModels;
using CRoute.App.Views;

namespace CRoute.App.Services;

/// <summary>
/// Satu-satunya tempat yang membuat Window. ViewModel dibuat lewat factory
/// dari composition root (App.xaml.cs) lalu dipasang sebagai DataContext.
/// </summary>
public class WindowService : IWindowService
{
    private readonly Func<LoginViewModel> _createLogin;
    private readonly Func<RegisterViewModel> _createRegister;
    private readonly Func<MainViewModel> _createMain;
    private Window? _current;

    public WindowService(
        Func<LoginViewModel> createLogin,
        Func<RegisterViewModel> createRegister,
        Func<MainViewModel> createMain)
    {
        _createLogin = createLogin;
        _createRegister = createRegister;
        _createMain = createMain;
    }

    public void ShowLogin() => SwitchTo(new LoginWindow { DataContext = _createLogin() });

    public void ShowRegister() => SwitchTo(new RegisterWindow { DataContext = _createRegister() });

    public void ShowMain() => SwitchTo(new MainWindow { DataContext = _createMain() });

    public void ExitApplication() => Application.Current.Shutdown();

    private void SwitchTo(Window next)
    {
        Window? previous = _current;
        _current = next;
        Application.Current.MainWindow = next;
        next.Show();
        previous?.Close();
    }
}
