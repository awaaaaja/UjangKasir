using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class ExpenseService(Func<AppDbContext> createDb)
{
    public async Task<List<Expense>> GetAsync(DateTime from, DateTime to, string? category = null)
    {
        await using var db = createDb();
        var end = to.Date.AddDays(1);
        var query = db.Expenses
            .AsNoTracking()
            .Include(x => x.CreatedByUser)
            .Where(x => x.ExpenseDate >= from.Date && x.ExpenseDate < end);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var keyword = category.Trim();
            query = query.Where(x => x.Category.Contains(keyword));
        }

        return await query.OrderByDescending(x => x.ExpenseDate).ToListAsync();
    }

    public async Task SaveAsync(Expense expense)
    {
        Validate(expense);
        await using var db = createDb();

        if (expense.Id == 0)
        {
            expense.CreatedAt = DateTime.UtcNow;
            db.Expenses.Add(expense);
        }
        else
        {
            var existing = await db.Expenses.FirstAsync(x => x.Id == expense.Id);
            existing.ExpenseDate = expense.ExpenseDate;
            existing.Category = expense.Category.Trim();
            existing.Amount = expense.Amount;
            existing.Description = expense.Description.Trim();
            existing.PaymentMethod = expense.PaymentMethod;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = createDb();
        var expense = await db.Expenses.FirstAsync(x => x.Id == id);
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
    }

    private static void Validate(Expense expense)
    {
        if (string.IsNullOrWhiteSpace(expense.Category))
        {
            throw new InvalidOperationException("Kategori pengeluaran wajib diisi.");
        }

        if (expense.Amount <= 0)
        {
            throw new InvalidOperationException("Nominal pengeluaran harus lebih dari 0.");
        }
    }
}
