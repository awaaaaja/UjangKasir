using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class AuthService(Func<AppDbContext> createDb)
{
    public User? CurrentUser { get; private set; }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var normalizedUsername = username.Trim();
        await using var db = createDb();

        var user = await db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == normalizedUsername && x.IsActive);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            db.AuditLogs.Add(new AuditLog
            {
                Action = "LoginFailed",
                EntityName = nameof(User),
                EntityId = normalizedUsername,
                Description = $"Login gagal untuk username '{normalizedUsername}'.",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return null;
        }

        CurrentUser = user;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "LoginSuccess",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Description = $"User '{user.Username}' berhasil login.",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }
}
