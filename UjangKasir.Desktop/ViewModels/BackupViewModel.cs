using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class BackupViewModel : PageViewModelBase
{
    private readonly BackupService backupService;
    private readonly User currentUser;

    public ObservableCollection<BackupResult> BackupFiles { get; } = new();

    public override string Title => "Backup";
    public override string Description => "Backup manual database SQLite dan restore khusus Owner/Admin.";

    [ObservableProperty]
    private string backupPath = "";

    [ObservableProperty]
    private string restoreFilePath = "";

    [ObservableProperty]
    private BackupResult? selectedBackup;

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public BackupViewModel(BackupService backupService, User currentUser)
    {
        this.backupService = backupService;
        this.currentUser = currentUser;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            BackupPath = await backupService.GetDefaultBackupPathAsync();
            await LoadBackupFilesAsync();
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load backup");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                StatusMessage = "Backup path wajib diisi.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Membuat backup database...";
            var result = await backupService.BackupAsync(BackupPath);
            StatusMessage = $"Backup berhasil dibuat: {result.FilePath}";
            await LoadBackupFilesAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Backup database");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BrowseRestoreFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "SQLite Backup (*.db)|*.db|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            RestoreFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        try
        {
            IsBusy = true;
            var filePath = !string.IsNullOrWhiteSpace(RestoreFilePath)
                ? RestoreFilePath
                : SelectedBackup?.FilePath;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "Pilih file backup yang akan direstore.";
                return;
            }

            var autoBackup = await backupService.RestoreAsync(currentUser, filePath, BackupPath);
            StatusMessage = $"Restore berhasil. Backup otomatis data sebelumnya: {autoBackup.FilePath}. Restart aplikasi disarankan.";
            await LoadBackupFilesAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Restore database");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedBackupChanged(BackupResult? value)
    {
        if (value is not null)
        {
            RestoreFilePath = value.FilePath;
        }
    }

    private async Task LoadBackupFilesAsync()
    {
        var files = await backupService.GetBackupFilesAsync(BackupPath);
        BackupFiles.Clear();
        foreach (var file in files)
        {
            BackupFiles.Add(file);
        }
    }
}
