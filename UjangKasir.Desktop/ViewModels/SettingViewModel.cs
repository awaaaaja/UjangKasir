using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class SettingViewModel : PageViewModelBase
{
    private readonly UserService userService;
    private readonly User currentUser;

    public ObservableCollection<User> Users { get; } = new();
    public ObservableCollection<Role> Roles { get; } = new();

    public override string Title => "Pengaturan";
    public override string Description => "Pengaturan user, role, dan operasional dasar aplikasi.";

    [ObservableProperty]
    private User? selectedUser;

    [ObservableProperty]
    private int editingUserId;

    [ObservableProperty]
    private string username = "";

    [ObservableProperty]
    private string fullName = "";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    private Role? selectedRole;

    [ObservableProperty]
    private bool userIsActive = true;

    [ObservableProperty]
    private string statusMessage = "";

    public SettingViewModel(UserService userService, User currentUser)
    {
        this.userService = userService;
        this.currentUser = currentUser;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            Roles.Clear();
            foreach (var role in await userService.GetRolesAsync())
            {
                Roles.Add(role);
            }

            await LoadUsersAsync();
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load settings");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void NewUser()
    {
        EditingUserId = 0;
        Username = "";
        FullName = "";
        Password = "";
        SelectedRole = Roles.FirstOrDefault(x => x.Name == "Kasir") ?? Roles.FirstOrDefault();
        UserIsActive = true;
        StatusMessage = "Form user baru siap diisi.";
    }

    [RelayCommand]
    private void EditSelectedUser()
    {
        if (SelectedUser is null)
        {
            StatusMessage = "Pilih user terlebih dahulu.";
            return;
        }

        EditingUserId = SelectedUser.Id;
        Username = SelectedUser.Username;
        FullName = SelectedUser.FullName;
        Password = "";
        SelectedRole = Roles.FirstOrDefault(x => x.Id == SelectedUser.RoleId);
        UserIsActive = SelectedUser.IsActive;
        StatusMessage = $"Mengedit user {SelectedUser.Username}. Kosongkan password jika tidak ingin mengganti.";
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        try
        {
            if (SelectedRole is null)
            {
                StatusMessage = "Role wajib dipilih.";
                return;
            }

            var user = new User
            {
                Id = EditingUserId,
                Username = Username,
                FullName = FullName,
                RoleId = SelectedRole.Id,
                IsActive = UserIsActive
            };

            await userService.SaveUserAsync(user, Password, currentUser.Id);
            StatusMessage = EditingUserId == 0 ? "User berhasil dibuat." : "User berhasil diperbarui.";
            NewUser();
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save user");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateSelectedUserAsync()
    {
        try
        {
            if (SelectedUser is null)
            {
                StatusMessage = "Pilih user yang ingin dinonaktifkan.";
                return;
            }

            await userService.DeactivateUserAsync(SelectedUser.Id, currentUser.Id);
            StatusMessage = $"User {SelectedUser.Username} dinonaktifkan.";
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Deactivate user");
            StatusMessage = ex.Message;
        }
    }

    private async Task LoadUsersAsync()
    {
        Users.Clear();
        foreach (var user in await userService.GetUsersAsync())
        {
            Users.Add(user);
        }
    }
}
