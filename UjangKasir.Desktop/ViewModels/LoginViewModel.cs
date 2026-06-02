using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class LoginViewModel(AuthService authService) : ObservableObject
{
    [ObservableProperty]
    private string username = "admin";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public event EventHandler<User>? LoginSucceeded;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username dan password wajib diisi.";
            return;
        }

        try
        {
            IsBusy = true;
            var user = await authService.LoginAsync(Username, Password);
            if (user is null)
            {
                ErrorMessage = "Login gagal. Username atau password tidak sesuai.";
                return;
            }

            LoginSucceeded?.Invoke(this, user);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
