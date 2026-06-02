using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record HeldCartItem(int ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal Discount, decimal Subtotal);

public class HeldTransactionService(Func<AppDbContext> createDb)
{
    public async Task<List<HeldTransaction>> GetHeldTransactionsAsync(int cashierUserId)
    {
        await using var db = createDb();
        return await db.HeldTransactions
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.Items)
            .Where(x => x.CashierUserId == cashierUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<HeldTransaction> HoldAsync(int cashierUserId, int? memberId, IReadOnlyList<HeldCartItem> items, string notes = "")
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Keranjang masih kosong.");
        }

        await using var db = createDb();
        var held = new HeldTransaction
        {
            HoldNumber = await GenerateHoldNumberAsync(db),
            CashierUserId = cashierUserId,
            MemberId = memberId,
            Notes = notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            held.Items.Add(new HeldTransactionItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                Subtotal = item.Subtotal
            });
        }

        db.HeldTransactions.Add(held);
        await db.SaveChangesAsync();
        return held;
    }

    public async Task<HeldTransaction> ResumeAsync(int heldTransactionId, int cashierUserId)
    {
        await using var db = createDb();
        var held = await db.HeldTransactions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == heldTransactionId && x.CashierUserId == cashierUserId);

        if (held is null)
        {
            throw new InvalidOperationException("Transaksi hold tidak ditemukan.");
        }

        var result = new HeldTransaction
        {
            Id = held.Id,
            HoldNumber = held.HoldNumber,
            CashierUserId = held.CashierUserId,
            MemberId = held.MemberId,
            Notes = held.Notes,
            CreatedAt = held.CreatedAt,
            Items = held.Items.Select(x => new HeldTransactionItem
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Discount = x.Discount,
                Subtotal = x.Subtotal
            }).ToList()
        };

        db.HeldTransactions.Remove(held);
        await db.SaveChangesAsync();
        return result;
    }

    private static async Task<string> GenerateHoldNumberAsync(AppDbContext db)
    {
        var prefix = $"HOLD-{DateTime.Now:yyyyMMdd}-";
        var count = await db.HeldTransactions.CountAsync(x => x.HoldNumber.StartsWith(prefix));
        return $"{prefix}{count + 1:0000}";
    }
}
