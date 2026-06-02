using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class UserService(Func<AppDbContext> createDb)
{
    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = createDb();
        return await db.Users
            .Include(x => x.Role)
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .ToListAsync();
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        await using var db = createDb();
        return await db.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task SaveUserAsync(User user, string? password, int actorUserId)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            throw new InvalidOperationException("Username wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new InvalidOperationException("Nama lengkap wajib diisi.");
        }

        if (user.RoleId <= 0)
        {
            throw new InvalidOperationException("Role wajib dipilih.");
        }

        await using var db = createDb();
        var normalizedUsername = user.Username.Trim();
        var usernameExists = await db.Users.AnyAsync(x => x.Username == normalizedUsername && x.Id != user.Id);
        if (usernameExists)
        {
            throw new InvalidOperationException("Username sudah digunakan.");
        }

        if (user.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Password wajib diisi untuk user baru.");
            }

            user.Username = normalizedUsername;
            user.PasswordHash = PasswordHasher.Hash(password);
            user.CreatedAt = DateTime.UtcNow;
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.AuditLogs.Add(new AuditLog
            {
                UserId = actorUserId,
                Action = "UserCreated",
                EntityName = nameof(User),
                EntityId = user.Id.ToString(),
                Description = $"User '{user.Username}' dibuat.",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            var existing = await db.Users.FirstAsync(x => x.Id == user.Id);
            existing.Username = normalizedUsername;
            existing.FullName = user.FullName.Trim();
            existing.RoleId = user.RoleId;
            existing.IsActive = user.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(password))
            {
                existing.PasswordHash = PasswordHasher.Hash(password);
            }

            db.AuditLogs.Add(new AuditLog
            {
                UserId = actorUserId,
                Action = "UserUpdated",
                EntityName = nameof(User),
                EntityId = existing.Id.ToString(),
                Description = $"User '{existing.Username}' diperbarui.",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task DeactivateUserAsync(int userId, int actorUserId)
    {
        if (userId == actorUserId)
        {
            throw new InvalidOperationException("User tidak bisa menonaktifkan akunnya sendiri.");
        }

        await using var db = createDb();
        var user = await db.Users.FirstAsync(x => x.Id == userId);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actorUserId,
            Action = "UserDeactivated",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Description = $"User '{user.Username}' dinonaktifkan.",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
