using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedRolesAsync(db);
        await SeedPermissionsAsync(db);
        await SeedRolePermissionsAsync(db);
        await SeedDefaultOwnerAsync(db);
        await SeedDefaultCashierAsync(db);
        await SeedCategoriesAsync(db);
        await SeedUnitsAsync(db);
        await SeedAppSettingsAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(AppDbContext db)
    {
        var menuKeys = new[]
        {
            "Dashboard",
            "POS",
            "Shift",
            "Product",
            "Inventory",
            "Purchase",
            "Supplier",
            "Member",
            "Promo",
            "Expense",
            "Report",
            "Backup",
            "Setting"
        };

        foreach (var menuKey in menuKeys)
        {
            var code = $"Menu.{menuKey}";
            if (!await db.Permissions.AnyAsync(x => x.Code == code))
            {
                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Name = $"Akses menu {menuKey}"
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedRolePermissionsAsync(AppDbContext db)
    {
        var roleMenuMap = new Dictionary<string, string[]>
        {
            ["Owner"] = ["Dashboard", "POS", "Shift", "Product", "Inventory", "Purchase", "Supplier", "Member", "Promo", "Expense", "Report", "Backup", "Setting"],
            ["Admin"] = ["Dashboard", "POS", "Shift", "Product", "Inventory", "Purchase", "Supplier", "Member", "Promo", "Expense", "Report", "Backup", "Setting"],
            ["Kasir"] = ["Dashboard", "POS", "Shift", "Member", "Promo"],
            ["Gudang"] = ["Dashboard", "Product", "Inventory", "Purchase", "Supplier"],
            ["Supervisor"] = ["Dashboard", "POS", "Shift", "Product", "Inventory", "Report", "Backup"]
        };

        foreach (var (roleName, menuKeys) in roleMenuMap)
        {
            var role = await db.Roles.FirstAsync(x => x.Name == roleName);
            foreach (var menuKey in menuKeys)
            {
                var permissionCode = $"Menu.{menuKey}";
                var permission = await db.Permissions.FirstAsync(x => x.Code == permissionCode);
                var exists = await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id);

                if (!exists)
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        var roleNames = new[] { "Owner", "Admin", "Kasir", "Gudang", "Supervisor" };
        foreach (var roleName in roleNames)
        {
            if (!await db.Roles.AnyAsync(x => x.Name == roleName))
            {
                db.Roles.Add(new Role
                {
                    Name = roleName,
                    Description = $"Role {roleName}"
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDefaultOwnerAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(x => x.Username == "admin"))
        {
            return;
        }

        var ownerRole = await db.Roles.FirstAsync(x => x.Name == "Owner");
        db.Users.Add(new User
        {
            RoleId = ownerRole.Id,
            Username = "admin",
            FullName = "Owner UjangKasir",
            PasswordHash = PasswordHasher.Hash("admin123"),
            IsActive = true
        });
    }

    private static async Task SeedDefaultCashierAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(x => x.Username == "kasir"))
        {
            return;
        }

        var cashierRole = await db.Roles.FirstAsync(x => x.Name == "Kasir");
        db.Users.Add(new User
        {
            RoleId = cashierRole.Id,
            Username = "kasir",
            FullName = "Kasir Demo",
            PasswordHash = PasswordHasher.Hash("kasir123"),
            IsActive = true
        });
    }

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        var categories = new[] { "Sembako", "Minuman", "Makanan Ringan", "Perawatan Rumah" };
        foreach (var category in categories)
        {
            if (!await db.Categories.AnyAsync(x => x.Name == category))
            {
                db.Categories.Add(new Category { Name = category });
            }
        }
    }

    private static async Task SeedUnitsAsync(AppDbContext db)
    {
        var units = new[]
        {
            new Unit { Name = "Pieces", Symbol = "Pcs" },
            new Unit { Name = "Pack", Symbol = "Pack" },
            new Unit { Name = "Dus", Symbol = "Dus" },
            new Unit { Name = "Kilogram", Symbol = "Kg" },
            new Unit { Name = "Liter", Symbol = "Liter" }
        };

        foreach (var unit in units)
        {
            if (!await db.Units.AnyAsync(x => x.Symbol == unit.Symbol))
            {
                db.Units.Add(unit);
            }
        }
    }

    private static async Task SeedAppSettingsAsync(AppDbContext db)
    {
        var settings = new[]
        {
            new AppSetting { Key = "StoreName", Value = "UjangKasir Store", Description = "Nama toko pada aplikasi dan struk." },
            new AppSetting { Key = "DefaultReceiptPrinter", Value = "", Description = "Nama printer struk default." },
            new AppSetting { Key = "BackupPath", Value = "Backups", Description = "Folder default backup database." },
            new AppSetting { Key = "RequireShift", Value = "false", Description = "Wajibkan shift aktif sebelum checkout." },
            new AppSetting { Key = "RequireShiftBeforeSale", Value = "false", Description = "Wajibkan shift aktif sebelum checkout penjualan." },
            new AppSetting { Key = "AllowNegativeStock", Value = "false", Description = "Izinkan stok minus saat checkout." }
        };

        foreach (var setting in settings)
        {
            if (!await db.AppSettings.AnyAsync(x => x.Key == setting.Key))
            {
                db.AppSettings.Add(setting);
            }
        }
    }
}
