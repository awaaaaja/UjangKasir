using System.IO;
using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record BackupResult(string FilePath, DateTime CreatedAt, long SizeBytes);

public class BackupService(Func<AppDbContext> createDb)
{
    public async Task<string> GetDefaultBackupPathAsync()
    {
        await using var db = createDb();
        var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == "BackupPath");
        var path = string.IsNullOrWhiteSpace(setting?.Value) ? "Backups" : setting.Value;
        return Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);
    }

    public async Task<BackupResult> BackupAsync(string? targetFolder = null)
    {
        var databasePath = await GetDatabasePathAsync();
        var backupFolder = string.IsNullOrWhiteSpace(targetFolder)
            ? await GetDefaultBackupPathAsync()
            : targetFolder;

        Directory.CreateDirectory(backupFolder);
        var backupPath = Path.Combine(backupFolder, $"backup-ujangkasir-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(databasePath, backupPath, overwrite: false);

        var fileInfo = new FileInfo(backupPath);
        return new BackupResult(backupPath, fileInfo.CreationTime, fileInfo.Length);
    }

    public async Task<BackupResult> RestoreAsync(User currentUser, string backupFilePath, string? autoBackupFolder = null)
    {
        if (!CanRestore(currentUser))
        {
            throw new InvalidOperationException("Restore database hanya boleh dilakukan oleh Owner atau Admin.");
        }

        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("File backup tidak ditemukan.", backupFilePath);
        }

        var autoBackup = await BackupAsync(autoBackupFolder);
        var databasePath = await GetDatabasePathAsync();
        File.Copy(backupFilePath, databasePath, overwrite: true);
        return autoBackup;
    }

    public async Task<IReadOnlyList<BackupResult>> GetBackupFilesAsync(string? folder = null)
    {
        var backupFolder = string.IsNullOrWhiteSpace(folder)
            ? await GetDefaultBackupPathAsync()
            : folder;

        if (!Directory.Exists(backupFolder))
        {
            return [];
        }

        return Directory.GetFiles(backupFolder, "backup-ujangkasir-*.db")
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new BackupResult(path, info.CreationTime, info.Length);
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    private async Task<string> GetDatabasePathAsync()
    {
        await using var db = createDb();
        var path = db.Database.GetDbConnection().DataSource;
        await db.Database.CloseConnectionAsync();

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Path database SQLite tidak ditemukan.");
        }

        return Path.GetFullPath(path);
    }

    private static bool CanRestore(User user)
    {
        var roleName = user.Role?.Name ?? "";
        return string.Equals(roleName, "Owner", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
