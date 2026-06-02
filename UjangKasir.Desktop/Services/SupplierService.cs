using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class SupplierService(Func<AppDbContext> createDb)
{
    public async Task<List<Supplier>> GetAllAsync()
    {
        await using var db = createDb();
        return await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Supplier>> GetActiveAsync()
    {
        await using var db = createDb();
        return await db.Suppliers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Supplier>> SearchAsync(string keyword)
    {
        await using var db = createDb();
        var query = db.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim();
            query = query.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search) ||
                x.Phone.Contains(search) ||
                x.ContactPerson.Contains(search));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        await using var db = createDb();
        return await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task SaveAsync(Supplier supplier)
    {
        Validate(supplier);
        await using var db = createDb();

        if (!string.IsNullOrWhiteSpace(supplier.Code))
        {
            var codeExists = await db.Suppliers.AnyAsync(x => x.Code == supplier.Code.Trim() && x.Id != supplier.Id);
            if (codeExists)
            {
                throw new InvalidOperationException("Kode supplier sudah digunakan.");
            }
        }

        if (supplier.Id == 0)
        {
            supplier.Code = string.IsNullOrWhiteSpace(supplier.Code)
                ? await GenerateSupplierCodeAsync(db)
                : supplier.Code.Trim();
            supplier.CreatedAt = DateTime.UtcNow;
            db.Suppliers.Add(supplier);
        }
        else
        {
            var existing = await db.Suppliers.FirstAsync(x => x.Id == supplier.Id);
            existing.Code = supplier.Code.Trim();
            existing.Name = supplier.Name.Trim();
            existing.Phone = supplier.Phone.Trim();
            existing.Email = supplier.Email.Trim();
            existing.Address = supplier.Address.Trim();
            existing.ContactPerson = supplier.ContactPerson.Trim();
            existing.Notes = supplier.Notes.Trim();
            existing.IsActive = supplier.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        await using var db = createDb();
        var supplier = await db.Suppliers.FirstAsync(x => x.Id == id);
        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static void Validate(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            throw new InvalidOperationException("Nama supplier wajib diisi.");
        }
    }

    private static async Task<string> GenerateSupplierCodeAsync(AppDbContext db)
    {
        var prefix = "SUP-";
        var count = await db.Suppliers.CountAsync();
        var next = count + 1;
        string code;
        do
        {
            code = $"{prefix}{next:0000}";
            next++;
        }
        while (await db.Suppliers.AnyAsync(x => x.Code == code));

        return code;
    }
}
