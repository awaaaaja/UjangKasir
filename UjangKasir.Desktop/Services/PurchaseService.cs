using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record PurchaseLineInput(int ProductId, decimal Quantity, decimal UnitCost);

public class PurchaseService(Func<AppDbContext> createDb)
{
    public async Task<List<Purchase>> GetPurchasesAsync()
    {
        await using var db = createDb();
        return await db.Purchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .OrderByDescending(x => x.PurchaseDate)
            .Take(100)
            .ToListAsync();
    }

    public async Task<Purchase> CreatePurchaseAsync(int supplierId, IReadOnlyList<PurchaseLineInput> lines, decimal paidAmount, string notes, int userId)
    {
        if (supplierId <= 0)
        {
            throw new InvalidOperationException("Supplier wajib dipilih.");
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Item pembelian masih kosong.");
        }

        if (lines.Any(x => x.Quantity <= 0 || x.UnitCost < 0))
        {
            throw new InvalidOperationException("Qty harus lebih dari 0 dan harga beli tidak boleh negatif.");
        }

        await using var db = createDb();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var productIds = lines.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        var purchase = new Purchase
        {
            PurchaseNumber = await GeneratePurchaseNumberAsync(db),
            SupplierId = supplierId,
            PurchaseDate = DateTime.UtcNow,
            PaidAmount = paidAmount,
            Notes = notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new InvalidOperationException("Produk pembelian tidak valid.");
            }

            var subtotal = line.Quantity * line.UnitCost;
            purchase.Items.Add(new PurchaseItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                Subtotal = subtotal
            });

            var before = product.Stock;
            product.Stock += line.Quantity;
            product.PurchasePrice = line.UnitCost;
            product.UpdatedAt = DateTime.UtcNow;

            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                UserId = userId,
                MovementType = "Purchase",
                QuantityChange = line.Quantity,
                QuantityBefore = before,
                QuantityAfter = product.Stock,
                ReferenceType = nameof(Purchase),
                Note = $"Pembelian {purchase.PurchaseNumber}",
                CreatedAt = DateTime.UtcNow
            });
        }

        purchase.TotalAmount = purchase.Items.Sum(x => x.Subtotal);
        purchase.PaymentStatus = paidAmount >= purchase.TotalAmount ? "Paid" : paidAmount <= 0 ? "Unpaid" : "Partial";

        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();

        foreach (var movement in db.ChangeTracker.Entries<StockMovement>().Select(x => x.Entity))
        {
            if (movement.ReferenceType == nameof(Purchase) && movement.ReferenceId is null)
            {
                movement.ReferenceId = purchase.Id;
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "PurchaseCreated",
            EntityName = nameof(Purchase),
            EntityId = purchase.Id.ToString(),
            Description = $"Pembelian {purchase.PurchaseNumber} total {purchase.TotalAmount:N0}.",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return purchase;
    }

    private static async Task<string> GeneratePurchaseNumberAsync(AppDbContext db)
    {
        var prefix = $"PO-{DateTime.Now:yyyyMMdd}-";
        var lastNumber = await db.Purchases
            .Where(x => x.PurchaseNumber.StartsWith(prefix))
            .OrderByDescending(x => x.PurchaseNumber)
            .Select(x => x.PurchaseNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber) && int.TryParse(lastNumber[prefix.Length..], out var parsed))
        {
            next = parsed + 1;
        }

        return $"{prefix}{next:0000}";
    }
}
