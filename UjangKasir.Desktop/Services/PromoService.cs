using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record PromoCalculation(Promo Promo, decimal DiscountAmount);

public class PromoService(Func<AppDbContext> createDb)
{
    public async Task<List<Promo>> SearchAsync(string keyword)
    {
        await using var db = createDb();
        var query = db.Promos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim();
            query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        }

        return await query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name).ToListAsync();
    }

    public async Task SaveAsync(Promo promo)
    {
        Validate(promo);
        await using var db = createDb();

        var exists = await db.Promos.AnyAsync(x => x.Code == promo.Code.Trim() && x.Id != promo.Id);
        if (exists)
        {
            throw new InvalidOperationException("Kode promo sudah digunakan.");
        }

        if (promo.Id == 0)
        {
            promo.Code = promo.Code.Trim().ToUpperInvariant();
            promo.CreatedAt = DateTime.UtcNow;
            db.Promos.Add(promo);
        }
        else
        {
            var existing = await db.Promos.FirstAsync(x => x.Id == promo.Id);
            existing.Code = promo.Code.Trim().ToUpperInvariant();
            existing.Name = promo.Name.Trim();
            existing.PromoType = promo.PromoType;
            existing.DiscountType = promo.DiscountType;
            existing.DiscountValue = promo.DiscountValue;
            existing.MinimumPurchase = promo.MinimumPurchase;
            existing.StartDate = promo.StartDate;
            existing.EndDate = promo.EndDate;
            existing.IsActive = promo.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        await using var db = createDb();
        var promo = await db.Promos.FirstAsync(x => x.Id == id);
        promo.IsActive = false;
        promo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<PromoCalculation> CalculateTransactionDiscountAsync(string code, decimal total)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Kode promo wajib diisi.");
        }

        await using var db = createDb();
        var normalizedCode = code.Trim().ToUpperInvariant();
        var promo = await db.Promos.AsNoTracking().FirstOrDefaultAsync(x => x.Code == normalizedCode && x.IsActive);
        if (promo is null)
        {
            throw new InvalidOperationException("Promo tidak ditemukan atau nonaktif.");
        }

        ValidatePromoUsage(promo, total);
        var discount = promo.DiscountType == "Percent"
            ? total * promo.DiscountValue / 100m
            : promo.DiscountValue;

        discount = Math.Min(total, Math.Max(0, discount));
        return new PromoCalculation(promo, discount);
    }

    private static void Validate(Promo promo)
    {
        if (string.IsNullOrWhiteSpace(promo.Code))
        {
            throw new InvalidOperationException("Kode promo wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(promo.Name))
        {
            throw new InvalidOperationException("Nama promo wajib diisi.");
        }

        if (promo.EndDate.Date < promo.StartDate.Date)
        {
            throw new InvalidOperationException("Tanggal akhir promo tidak boleh sebelum tanggal mulai.");
        }

        if (promo.DiscountValue <= 0)
        {
            throw new InvalidOperationException("Nilai diskon harus lebih dari 0.");
        }

        if (promo.DiscountType == "Percent" && promo.DiscountValue > 100)
        {
            throw new InvalidOperationException("Diskon persen maksimal 100%.");
        }
    }

    private static void ValidatePromoUsage(Promo promo, decimal total)
    {
        var today = DateTime.Now.Date;
        if (today < promo.StartDate.Date || today > promo.EndDate.Date)
        {
            throw new InvalidOperationException("Promo tidak sedang berlaku.");
        }

        if (promo.PromoType != "TransactionDiscount")
        {
            throw new InvalidOperationException("Promo produk belum didukung di POS.");
        }

        if (total < promo.MinimumPurchase)
        {
            throw new InvalidOperationException($"Minimum belanja promo adalah {promo.MinimumPurchase:N0}.");
        }
    }
}
