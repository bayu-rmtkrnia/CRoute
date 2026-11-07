using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Services;
using CRoute.Core.Interfaces;

namespace CRoute.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IWindowService _windows;

    public LoginViewModel(IAuthService auth, IWindowService windows)
    {
        _auth = auth;
        _windows = windows;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    private bool CanLogin() => Username.Trim().Length > 0 && Password.Length > 0;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private void Login()
    {
        ErrorMessage = string.Empty;

        if (!_auth.Login(Username.Trim(), Password))
        {
            ErrorMessage = "Username atau password salah.";
            return;
        }

        _windows.ShowMain();
    }

    [RelayCommand]
    private void OpenRegister() => _windows.ShowRegister();

    [RelayCommand]
    private void Close() => _windows.ExitApplication();
}
