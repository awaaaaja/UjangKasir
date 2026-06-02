using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Promo> Promos => Set<Promo>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<HeldTransaction> HeldTransactions => Set<HeldTransaction>();
    public DbSet<HeldTransactionItem> HeldTransactionItems => Set<HeldTransactionItem>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<Permission>().ToTable("Permissions");
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Unit>().ToTable("Units");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<Sale>().ToTable("Sales");
        modelBuilder.Entity<SaleItem>().ToTable("SaleItems");
        modelBuilder.Entity<Payment>().ToTable("Payments");
        modelBuilder.Entity<StockMovement>().ToTable("StockMovements");
        modelBuilder.Entity<CashierShift>().ToTable("CashierShifts");
        modelBuilder.Entity<Purchase>().ToTable("Purchases");
        modelBuilder.Entity<PurchaseItem>().ToTable("PurchaseItems");
        modelBuilder.Entity<Member>().ToTable("Members");
        modelBuilder.Entity<Promo>().ToTable("Promos");
        modelBuilder.Entity<Expense>().ToTable("Expenses");
        modelBuilder.Entity<HeldTransaction>().ToTable("HeldTransactions");
        modelBuilder.Entity<HeldTransactionItem>().ToTable("HeldTransactionItems");
        modelBuilder.Entity<AppSetting>().ToTable("AppSettings");
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");

        modelBuilder.Entity<Role>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasKey(x => new { x.RoleId, x.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(x => x.Role)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<Unit>()
            .HasIndex(x => x.Symbol)
            .IsUnique();

        modelBuilder.Entity<Supplier>()
            .HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("Code <> ''");

        modelBuilder.Entity<Product>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(x => x.Barcode)
            .IsUnique()
            .HasFilter("Barcode <> ''");

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Unit)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Supplier)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Sale>()
            .HasIndex(x => x.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<Sale>()
            .HasOne(x => x.User)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sale>()
            .HasOne(x => x.CashierShift)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.CashierShiftId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Sale>()
            .HasOne(x => x.Member)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SaleItem>()
            .HasOne(x => x.Sale)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SaleItem>()
            .HasOne(x => x.Product)
            .WithMany(x => x.SaleItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Sale)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockMovement>()
            .HasOne(x => x.Product)
            .WithMany(x => x.StockMovements)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CashierShift>()
            .HasOne(x => x.User)
            .WithMany(x => x.CashierShifts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>()
            .HasIndex(x => x.PurchaseNumber)
            .IsUnique();

        modelBuilder.Entity<Purchase>()
            .HasOne(x => x.Supplier)
            .WithMany(x => x.Purchases)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseItem>()
            .HasOne(x => x.Purchase)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Member>()
            .HasIndex(x => x.MemberCode)
            .IsUnique();

        modelBuilder.Entity<Promo>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Expense>()
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HeldTransaction>()
            .HasIndex(x => x.HoldNumber)
            .IsUnique();

        modelBuilder.Entity<HeldTransaction>()
            .HasOne(x => x.CashierUser)
            .WithMany()
            .HasForeignKey(x => x.CashierUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HeldTransaction>()
            .HasOne(x => x.Member)
            .WithMany(x => x.HeldTransactions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HeldTransactionItem>()
            .HasOne(x => x.HeldTransaction)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.HeldTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HeldTransactionItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppSetting>()
            .HasIndex(x => x.Key)
            .IsUnique();

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        ConfigureDecimalPrecision(modelBuilder);
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entity => entity.GetProperties())
                     .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }
    }
}
