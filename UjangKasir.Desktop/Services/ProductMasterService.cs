using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class ProductMasterService(Func<AppDbContext> createDb)
{
    public async Task<List<Product>> GetProductsAsync(string searchText, int? categoryId, bool? isActive)
    {
        await using var db = createDb();
        var query = db.Products
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Include(x => x.Supplier)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var keyword = searchText.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Code.Contains(keyword) ||
                x.Barcode.Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Product?> FindActiveProductByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        await using var db = createDb();
        return await db.Products
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Barcode == barcode.Trim() && x.IsActive);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = createDb();
        return await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Unit>> GetUnitsAsync()
    {
        await using var db = createDb();
        return await db.Units.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        await using var db = createDb();
        return await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? currentProductId = null)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return true;
        }

        await using var db = createDb();
        return !await db.Products.AnyAsync(x =>
            x.Barcode == barcode.Trim() &&
            (!currentProductId.HasValue || x.Id != currentProductId.Value));
    }

    public async Task SaveProductAsync(Product product, int userId)
    {
        ValidateProduct(product);

        await using var db = createDb();
        await using var transaction = await db.Database.BeginTransactionAsync();

        if (!await IsBarcodeUniqueAsync(product.Barcode, product.Id == 0 ? null : product.Id))
        {
            throw new InvalidOperationException("Barcode sudah digunakan produk lain.");
        }

        var codeExists = await db.Products.AnyAsync(x => x.Code == product.Code && x.Id != product.Id);
        if (codeExists)
        {
            throw new InvalidOperationException("Kode produk sudah digunakan produk lain.");
        }

        if (product.Id == 0)
        {
            product.CreatedAt = DateTime.UtcNow;
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "ProductCreated",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                Description = $"Produk '{product.Name}' dibuat.",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            var existing = await db.Products.FirstAsync(x => x.Id == product.Id);
            existing.Code = product.Code;
            existing.Barcode = product.Barcode;
            existing.Name = product.Name;
            existing.CategoryId = product.CategoryId;
            existing.UnitId = product.UnitId;
            existing.SupplierId = product.SupplierId;
            existing.PurchasePrice = product.PurchasePrice;
            existing.SellingPrice = product.SellingPrice;
            existing.Stock = product.Stock;
            existing.MinimumStock = product.MinimumStock;
            existing.ExpiredDate = product.ExpiredDate;
            existing.IsActive = product.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "ProductUpdated",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                Description = $"Produk '{product.Name}' diedit.",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeactivateProductAsync(int productId, int userId)
    {
        await using var db = createDb();
        var product = await db.Products.FirstAsync(x => x.Id == productId);
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "ProductDeactivated",
            EntityName = nameof(Product),
            EntityId = product.Id.ToString(),
            Description = $"Produk '{product.Name}' dinonaktifkan.",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public async Task SaveCategoryAsync(Category category)
    {
        await using var db = createDb();
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            throw new InvalidOperationException("Nama kategori wajib diisi.");
        }

        if (category.Id == 0)
        {
            db.Categories.Add(category);
        }
        else
        {
            var existing = await db.Categories.FirstAsync(x => x.Id == category.Id);
            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.IsActive = category.IsActive;
        }

        await db.SaveChangesAsync();
    }

    public async Task SaveUnitAsync(Unit unit)
    {
        await using var db = createDb();
        if (string.IsNullOrWhiteSpace(unit.Name) || string.IsNullOrWhiteSpace(unit.Symbol))
        {
            throw new InvalidOperationException("Nama dan simbol satuan wajib diisi.");
        }

        if (unit.Id == 0)
        {
            db.Units.Add(unit);
        }
        else
        {
            var existing = await db.Units.FirstAsync(x => x.Id == unit.Id);
            existing.Name = unit.Name;
            existing.Symbol = unit.Symbol;
            existing.IsActive = unit.IsActive;
        }

        await db.SaveChangesAsync();
    }

    public async Task SaveSupplierAsync(Supplier supplier)
    {
        await using var db = createDb();
        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            throw new InvalidOperationException("Nama supplier wajib diisi.");
        }

        if (supplier.Id == 0)
        {
            supplier.CreatedAt = DateTime.UtcNow;
            db.Suppliers.Add(supplier);
        }
        else
        {
            var existing = await db.Suppliers.FirstAsync(x => x.Id == supplier.Id);
            existing.Code = supplier.Code;
            existing.Name = supplier.Name;
            existing.Phone = supplier.Phone;
            existing.Email = supplier.Email;
            existing.Address = supplier.Address;
            existing.ContactPerson = supplier.ContactPerson;
            existing.Notes = supplier.Notes;
            existing.IsActive = supplier.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeactivateCategoryAsync(int categoryId)
    {
        await using var db = createDb();
        var category = await db.Categories.FirstAsync(x => x.Id == categoryId);
        category.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task DeactivateUnitAsync(int unitId)
    {
        await using var db = createDb();
        var unit = await db.Units.FirstAsync(x => x.Id == unitId);
        unit.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task DeactivateSupplierAsync(int supplierId)
    {
        await using var db = createDb();
        var supplier = await db.Suppliers.FirstAsync(x => x.Id == supplierId);
        supplier.IsActive = false;
        await db.SaveChangesAsync();
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Code))
        {
            throw new InvalidOperationException("Kode produk wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new InvalidOperationException("Nama produk wajib diisi.");
        }

        if (product.SellingPrice <= 0)
        {
            throw new InvalidOperationException("Harga jual wajib lebih dari 0.");
        }

        if (product.PurchasePrice < 0)
        {
            throw new InvalidOperationException("Harga beli tidak boleh negatif.");
        }

        if (product.CategoryId <= 0 || product.UnitId <= 0)
        {
            throw new InvalidOperationException("Kategori dan satuan wajib dipilih.");
        }
    }
}
