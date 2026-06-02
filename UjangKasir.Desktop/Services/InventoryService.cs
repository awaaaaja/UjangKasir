using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class InventoryService(Func<AppDbContext> createDb)
{
    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        await using var db = createDb();
        var products = await db.Products
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync();

        return products
            .Where(x => x.Stock <= x.MinimumStock)
            .OrderBy(x => x.Stock)
            .ToList();
    }

    public async Task<List<StockMovement>> GetRecentStockMovementsAsync(int take = 100)
    {
        await using var db = createDb();
        return await db.StockMovements
            .Include(x => x.Product)
            .Include(x => x.User)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
