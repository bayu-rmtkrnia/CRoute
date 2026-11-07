using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRoute.App.Services;
using CRoute.Core.Interfaces;

namespace CRoute.App.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private const int MinPasswordLength = 6;

    private readonly IAuthService _auth;
    private readonly IWindowService _windows;
    private readonly IDialogService _dialogs;

    public RegisterViewModel(IAuthService auth, IWindowService windows, IDialogService dialogs)
    {
        _auth = auth;
        _windows = windows;
        _dialogs = dialogs;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _fullName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    private bool CanRegister() =>
        FullName.Trim().Length > 0 && Username.Trim().Length > 0 && Password.Length > 0;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private void Register()
    {
        ErrorMessage = string.Empty;

        // Validasi berurutan, berhenti di kesalahan pertama.
        if (Password.Length < MinPasswordLength)
        {
            ErrorMessage = $"Password minimal {MinPasswordLength} karakter.";
            return;
        }
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Konfirmasi password tidak sama dengan password.";
            return;
        }
        if (!_auth.Register(FullName.Trim(), Username.Trim(), Password))
        {
            ErrorMessage = "Username sudah dipakai, coba yang lain.";
            return;
        }

        _dialogs.ShowInfo("Akun berhasil dibuat. Silakan login.");
        _windows.ShowLogin();
    }

    [RelayCommand]
    private void Cancel() => _windows.ShowLogin();
}
