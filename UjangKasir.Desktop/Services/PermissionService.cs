using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class PermissionService(Func<AppDbContext> createDb)
{
    public bool CanAccessMenu(User user, string menuKey)
    {
        var permissionCode = GetMenuPermissionCode(menuKey);
        using var db = createDb();

        return db.RolePermissions
            .Include(x => x.Permission)
            .Any(x =>
                x.RoleId == user.RoleId &&
                x.Permission != null &&
                x.Permission.Code == permissionCode);
    }

    public static string GetMenuPermissionCode(string menuKey)
    {
        return $"Menu.{menuKey}";
    }
}
