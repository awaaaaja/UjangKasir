using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record ShiftSaleSummary(
    string InvoiceNumber,
    DateTime TransactionTime,
    decimal GrandTotal,
    string PaymentMethod);

public record ShiftSummary(
    int ShiftId,
    int UserId,
    string CashierName,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningCash,
    decimal? ClosingCash,
    decimal CashSales,
    decimal NonCashSales,
    decimal TotalSales,
    int TransactionCount,
    decimal ExpectedCash,
    decimal? CashDifference,
    string Status,
    IReadOnlyList<ShiftSaleSummary> Sales);

public class ShiftService(Func<AppDbContext> createDb)
{
    private const string StatusOpen = "Open";
    private const string StatusClosed = "Closed";
    private const string PaymentCash = "Cash";

    public async Task<CashierShift?> GetActiveShiftByUserAsync(int userId)
    {
        await using var db = createDb();
        return await db.CashierShifts
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == StatusOpen);
    }

    public async Task<ShiftSummary> OpenShiftAsync(int userId, decimal openingCash)
    {
        if (openingCash < 0)
        {
            throw new InvalidOperationException("Modal awal tidak boleh negatif.");
        }

        await using var db = createDb();
        var userExists = await db.Users.AnyAsync(x => x.Id == userId && x.IsActive);
        if (!userExists)
        {
            throw new InvalidOperationException("User kasir tidak valid atau nonaktif.");
        }

        var hasActiveShift = await db.CashierShifts.AnyAsync(x => x.UserId == userId && x.Status == StatusOpen);
        if (hasActiveShift)
        {
            throw new InvalidOperationException("Kasir masih memiliki shift aktif.");
        }

        var shift = new CashierShift
        {
            UserId = userId,
            OpenedAt = DateTime.UtcNow,
            OpeningCash = openingCash,
            Status = StatusOpen
        };

        db.CashierShifts.Add(shift);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "OpenShift",
            EntityName = nameof(CashierShift),
            Description = $"Buka shift dengan modal awal {openingCash:N0}.",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return await CalculateShiftSummaryAsync(shift.Id);
    }

    public async Task<ShiftSummary> CloseShiftAsync(int userId, decimal actualCash)
    {
        if (actualCash < 0)
        {
            throw new InvalidOperationException("Uang aktual tidak boleh negatif.");
        }

        await using var db = createDb();
        var shift = await db.CashierShifts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == StatusOpen);

        if (shift is null)
        {
            throw new InvalidOperationException("Shift aktif tidak ditemukan.");
        }

        var summary = await CalculateShiftSummaryAsync(shift.Id);
        shift.ClosingCash = actualCash;
        shift.ClosedAt = DateTime.UtcNow;
        shift.Status = StatusClosed;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "CloseShift",
            EntityName = nameof(CashierShift),
            EntityId = shift.Id.ToString(),
            Description = $"Tutup shift. Uang aktual {actualCash:N0}, selisih {actualCash - summary.ExpectedCash:N0}.",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return await CalculateShiftSummaryAsync(shift.Id);
    }

    public async Task<ShiftSummary> CalculateShiftSummaryAsync(int shiftId)
    {
        await using var db = createDb();
        var shift = await db.CashierShifts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Sales)
            .ThenInclude(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == shiftId);

        if (shift is null)
        {
            throw new InvalidOperationException("Shift tidak ditemukan.");
        }

        return BuildSummary(shift);
    }

    public async Task<IReadOnlyList<ShiftSummary>> GetRecentShiftSummariesAsync(int take = 20)
    {
        await using var db = createDb();
        var shifts = await db.CashierShifts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Sales)
            .ThenInclude(x => x.Payments)
            .OrderByDescending(x => x.OpenedAt)
            .Take(take)
            .ToListAsync();

        return shifts.Select(BuildSummary).ToList();
    }

    public string BuildPrintableReport(ShiftSummary summary)
    {
        var lines = summary.Sales.Count == 0
            ? "Belum ada transaksi."
            : string.Join(Environment.NewLine, summary.Sales.Select(x =>
                $"{x.TransactionTime.ToLocalTime():dd/MM/yyyy HH:mm} | {x.InvoiceNumber} | {x.PaymentMethod} | {x.GrandTotal:N0}"));

        return $"""
               LAPORAN SHIFT UJANGKASIR
               Kasir       : {summary.CashierName}
               Status      : {summary.Status}
               Buka        : {summary.OpenedAt.ToLocalTime():dd/MM/yyyy HH:mm}
               Tutup       : {(summary.ClosedAt.HasValue ? summary.ClosedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "-")}

               Modal Awal  : {summary.OpeningCash:N0}
               Cash Sales  : {summary.CashSales:N0}
               Non Cash    : {summary.NonCashSales:N0}
               Total Sales : {summary.TotalSales:N0}
               Transaksi   : {summary.TransactionCount:N0}
               Expected    : {summary.ExpectedCash:N0}
               Aktual      : {(summary.ClosingCash.HasValue ? summary.ClosingCash.Value.ToString("N0") : "-")}
               Selisih     : {(summary.CashDifference.HasValue ? summary.CashDifference.Value.ToString("N0") : "-")}

               DETAIL TRANSAKSI
               {lines}
               """;
    }

    private static ShiftSummary BuildSummary(CashierShift shift)
    {
        var completedSales = shift.Sales
            .Where(x => x.Status == "Completed")
            .OrderBy(x => x.TransactionTime)
            .ToList();

        var cashSales = completedSales
            .SelectMany(x => x.Payments)
            .Where(x => string.Equals(x.Method, PaymentCash, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Amount - x.ChangeAmount);

        var nonCashSales = completedSales
            .SelectMany(x => x.Payments)
            .Where(x => !string.Equals(x.Method, PaymentCash, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Amount);

        var expectedCash = shift.OpeningCash + cashSales;
        var cashDifference = shift.ClosingCash.HasValue
            ? shift.ClosingCash.Value - expectedCash
            : (decimal?)null;

        return new ShiftSummary(
            shift.Id,
            shift.UserId,
            shift.User?.FullName ?? $"User #{shift.UserId}",
            shift.OpenedAt,
            shift.ClosedAt,
            shift.OpeningCash,
            shift.ClosingCash,
            cashSales,
            nonCashSales,
            completedSales.Sum(x => x.GrandTotal),
            completedSales.Count,
            expectedCash,
            cashDifference,
            shift.Status,
            completedSales.Select(x => new ShiftSaleSummary(
                x.InvoiceNumber,
                x.TransactionTime,
                x.GrandTotal,
                string.Join(", ", x.Payments.Select(payment => payment.Method).Distinct()))).ToList());
    }
}
