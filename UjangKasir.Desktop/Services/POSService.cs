using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record POSCheckoutItem(
    int ProductId,
    string ProductName,
    decimal Qty,
    decimal UnitPrice,
    decimal Discount);

public record POSCheckoutRequest(
    int UserId,
    IReadOnlyList<POSCheckoutItem> Items,
    string PaymentMethod,
    decimal PaidAmount,
    int? MemberId = null,
    decimal TransactionDiscount = 0,
    string PromoCode = "");

public record POSReceiptLine(
    string ProductName,
    decimal Qty,
    decimal UnitPrice,
    decimal Discount,
    decimal Subtotal);

public record POSCheckoutResult(
    int SaleId,
    string InvoiceNumber,
    DateTime TransactionTime,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal ChangeAmount,
    string PaymentMethod,
    IReadOnlyList<POSReceiptLine> ReceiptLines);

public class POSService(Func<AppDbContext> createDb)
{
    private const string PaymentCash = "Cash";

    public async Task<POSCheckoutResult> CheckoutAsync(POSCheckoutRequest request)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Keranjang masih kosong.");
        }

        await using var db = createDb();
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var requireShift = await GetBoolSettingAsync(db, "RequireShift", defaultValue: false)
                || await GetBoolSettingAsync(db, "RequireShiftBeforeSale", defaultValue: false);
            var allowNegativeStock = await GetBoolSettingAsync(db, "AllowNegativeStock", defaultValue: false);
            var activeShift = await db.CashierShifts
                .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Status == "Open");

            if (requireShift && activeShift is null)
            {
                throw new InvalidOperationException("Shift aktif wajib dibuka sebelum checkout.");
            }

            var normalizedItems = request.Items
                .GroupBy(x => x.ProductId)
                .Select(group => new POSCheckoutItem(
                    group.Key,
                    group.First().ProductName,
                    group.Sum(x => x.Qty),
                    group.First().UnitPrice,
                    group.Sum(x => x.Discount)))
                .ToList();

            ValidateCartItems(normalizedItems);

            var productIds = normalizedItems.Select(x => x.ProductId).ToList();
            var products = await db.Products
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var subtotal = normalizedItems.Sum(x => x.Qty * x.UnitPrice);
            var discountTotal = normalizedItems.Sum(x => x.Discount) + request.TransactionDiscount;
            var grandTotal = subtotal - discountTotal;

            if (grandTotal < 0)
            {
                throw new InvalidOperationException("Total transaksi tidak valid.");
            }

            if (request.PaymentMethod == PaymentCash && request.PaidAmount < grandTotal)
            {
                throw new InvalidOperationException("Pembayaran tunai kurang dari total transaksi.");
            }

            var invoiceNumber = await GenerateInvoiceNumberAsync(db);
            if (request.MemberId.HasValue && !await db.Members.AnyAsync(x => x.Id == request.MemberId.Value && x.IsActive))
            {
                throw new InvalidOperationException("Member tidak valid atau nonaktif.");
            }

            var sale = new Sale
            {
                InvoiceNumber = invoiceNumber,
                UserId = request.UserId,
                CashierShiftId = activeShift?.Id,
                MemberId = request.MemberId,
                TransactionTime = DateTime.UtcNow,
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                TaxTotal = 0,
                GrandTotal = grandTotal,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in normalizedItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                {
                    throw new InvalidOperationException($"Produk {item.ProductName} tidak valid atau sudah nonaktif.");
                }

                if (!allowNegativeStock && product.Stock < item.Qty)
                {
                    throw new InvalidOperationException($"Stok {product.Name} tidak cukup. Stok tersedia: {product.Stock:N2}.");
                }

                var stockBefore = product.Stock;
                var stockAfter = stockBefore - item.Qty;

                sale.Items.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,
                    BarcodeSnapshot = product.Barcode,
                    Quantity = item.Qty,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.Discount,
                    LineTotal = item.Qty * item.UnitPrice - item.Discount
                });

                product.Stock = stockAfter;
                product.UpdatedAt = DateTime.UtcNow;

                db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    UserId = request.UserId,
                    MovementType = "Sale",
                    QuantityChange = -item.Qty,
                    QuantityBefore = stockBefore,
                    QuantityAfter = stockAfter,
                    ReferenceType = nameof(Sale),
                    Note = $"Penjualan {invoiceNumber}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var paidAmount = request.PaymentMethod == PaymentCash ? request.PaidAmount : grandTotal;
            var changeAmount = request.PaymentMethod == PaymentCash ? paidAmount - grandTotal : 0;

            sale.Payments.Add(new Payment
            {
                Method = request.PaymentMethod,
                Amount = paidAmount,
                ChangeAmount = changeAmount,
                PaidAt = DateTime.UtcNow
            });

            db.Sales.Add(sale);
            if (request.MemberId.HasValue)
            {
                var member = await db.Members.FirstAsync(x => x.Id == request.MemberId.Value);
                member.Points += Math.Max(0, (int)Math.Floor(grandTotal / 10000m));
                member.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            foreach (var stockMovement in db.ChangeTracker.Entries<StockMovement>().Select(x => x.Entity))
            {
                if (stockMovement.ReferenceType == nameof(Sale) && stockMovement.ReferenceId is null)
                {
                    stockMovement.ReferenceId = sale.Id;
                }
            }

            db.AuditLogs.Add(new AuditLog
            {
                UserId = request.UserId,
                Action = "SaleCheckout",
                EntityName = nameof(Sale),
                EntityId = sale.Id.ToString(),
                Description = string.IsNullOrWhiteSpace(request.PromoCode)
                    ? $"Checkout {invoiceNumber} total {grandTotal:N0}."
                    : $"Checkout {invoiceNumber} total {grandTotal:N0}, promo {request.PromoCode}.",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new POSCheckoutResult(
                sale.Id,
                sale.InvoiceNumber,
                sale.TransactionTime,
                sale.GrandTotal,
                paidAmount,
                changeAmount,
                request.PaymentMethod,
                sale.Items.Select(x => new POSReceiptLine(
                    x.ProductNameSnapshot,
                    x.Quantity,
                    x.UnitPrice,
                    x.DiscountAmount,
                    x.LineTotal)).ToList());
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void ValidateCartItems(IEnumerable<POSCheckoutItem> items)
    {
        foreach (var item in items)
        {
            if (item.Qty <= 0)
            {
                throw new InvalidOperationException($"Qty {item.ProductName} harus lebih dari 0.");
            }

            if (item.UnitPrice < 0)
            {
                throw new InvalidOperationException($"Harga {item.ProductName} tidak valid.");
            }

            if (item.Discount < 0)
            {
                throw new InvalidOperationException($"Diskon {item.ProductName} tidak boleh negatif.");
            }

            if (item.Discount > item.Qty * item.UnitPrice)
            {
                throw new InvalidOperationException($"Diskon {item.ProductName} melebihi subtotal.");
            }
        }
    }

    private static async Task<bool> GetBoolSettingAsync(AppDbContext db, string key, bool defaultValue)
    {
        var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
        return setting is null
            ? defaultValue
            : bool.TryParse(setting.Value, out var value) ? value : defaultValue;
    }

    private static async Task<string> GenerateInvoiceNumberAsync(AppDbContext db)
    {
        var prefix = $"INV-{DateTime.Now:yyyyMMdd}-";
        var lastInvoice = await db.Sales
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(x => x.InvoiceNumber)
            .Select(x => x.InvoiceNumber)
            .FirstOrDefaultAsync();

        var nextNumber = 1;
        if (!string.IsNullOrWhiteSpace(lastInvoice))
        {
            var suffix = lastInvoice[prefix.Length..];
            if (int.TryParse(suffix, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:0000}";
    }
}
