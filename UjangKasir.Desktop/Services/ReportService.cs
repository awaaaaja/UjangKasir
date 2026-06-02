using System.IO;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public record ReportRow(
    string Column1,
    string Column2,
    string Column3,
    string Column4,
    string Column5,
    string Column6);

public record ReportResult(
    string Title,
    string[] Headers,
    IReadOnlyList<ReportRow> Rows,
    decimal TotalSales,
    decimal TotalGrossProfit,
    int TotalTransactions);

public class ReportService(Func<AppDbContext> createDb)
{
    public static readonly string[] ReportTypes =
    [
        "Penjualan Harian",
        "Penjualan Per Periode",
        "Produk Terlaris",
        "Laporan Kasir",
        "Stok Hampir Habis",
        "Laba Kotor Sederhana",
        "Ringkasan Operasional"
    ];

    public async Task<ReportResult> GenerateAsync(string reportType, DateTime startDate, DateTime endDate)
    {
        var startUtc = startDate.Date.ToUniversalTime();
        var endUtc = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

        return reportType switch
        {
            "Produk Terlaris" => await GetTopProductsAsync(startUtc, endUtc),
            "Laporan Kasir" => await GetCashierReportAsync(startUtc, endUtc),
            "Stok Hampir Habis" => await GetLowStockReportAsync(),
            "Laba Kotor Sederhana" => await GetGrossProfitReportAsync(startUtc, endUtc),
            "Ringkasan Operasional" => await GetOperationalSummaryAsync(startUtc, endUtc),
            "Penjualan Harian" => await GetSalesReportAsync(startUtc, endUtc, "Penjualan Harian"),
            _ => await GetSalesReportAsync(startUtc, endUtc, "Penjualan Per Periode")
        };
    }

    public void ExportExcel(ReportResult report, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Laporan");

        worksheet.Cell(1, 1).Value = report.Title;
        worksheet.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(16);

        for (var i = 0; i < report.Headers.Length; i++)
        {
            worksheet.Cell(3, i + 1).Value = report.Headers[i];
            worksheet.Cell(3, i + 1).Style.Font.SetBold();
            worksheet.Cell(3, i + 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
        }

        for (var i = 0; i < report.Rows.Count; i++)
        {
            var row = report.Rows[i];
            worksheet.Cell(i + 4, 1).Value = row.Column1;
            worksheet.Cell(i + 4, 2).Value = row.Column2;
            worksheet.Cell(i + 4, 3).Value = row.Column3;
            worksheet.Cell(i + 4, 4).Value = row.Column4;
            worksheet.Cell(i + 4, 5).Value = row.Column5;
            worksheet.Cell(i + 4, 6).Value = row.Column6;
        }

        var summaryRow = report.Rows.Count + 6;
        worksheet.Cell(summaryRow, 1).Value = "Total Transaksi";
        worksheet.Cell(summaryRow, 2).Value = report.TotalTransactions;
        worksheet.Cell(summaryRow + 1, 1).Value = "Total Penjualan";
        worksheet.Cell(summaryRow + 1, 2).Value = report.TotalSales;
        worksheet.Cell(summaryRow + 2, 1).Value = "Laba Kotor";
        worksheet.Cell(summaryRow + 2, 2).Value = report.TotalGrossProfit;
        worksheet.Columns().AdjustToContents();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        workbook.SaveAs(filePath);
    }

    public void ExportPdf(ReportResult report, string filePath)
    {
        var document = new PdfDocument();
        document.Info.Title = report.Title;
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
        var headerFont = new XFont("Arial", 9, XFontStyleEx.Bold);
        var bodyFont = new XFont("Arial", 8, XFontStyleEx.Regular);

        var y = 36d;
        gfx.DrawString(report.Title, titleFont, XBrushes.Black, new XRect(36, y, page.Width.Point - 72, 22), XStringFormats.TopLeft);
        y += 34;

        var widths = new[] { 88d, 88d, 78d, 78d, 78d, 94d };
        DrawPdfRow(gfx, report.Headers, headerFont, widths, ref y);

        foreach (var row in report.Rows.Take(24))
        {
            DrawPdfRow(gfx, [row.Column1, row.Column2, row.Column3, row.Column4, row.Column5, row.Column6], bodyFont, widths, ref y);
        }

        y += 18;
        gfx.DrawString($"Total Transaksi: {report.TotalTransactions:N0}", bodyFont, XBrushes.Black, 36, y);
        y += 16;
        gfx.DrawString($"Total Penjualan: {report.TotalSales:N0}", bodyFont, XBrushes.Black, 36, y);
        y += 16;
        gfx.DrawString($"Laba Kotor: {report.TotalGrossProfit:N0}", bodyFont, XBrushes.Black, 36, y);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        document.Save(filePath);
    }

    private static void DrawPdfRow(XGraphics gfx, IReadOnlyList<string> cells, XFont font, IReadOnlyList<double> widths, ref double y)
    {
        var x = 36d;
        for (var i = 0; i < cells.Count; i++)
        {
            var value = cells[i].Length > 22 ? cells[i][..22] : cells[i];
            gfx.DrawString(value, font, XBrushes.Black, new XRect(x, y, widths[i], 14), XStringFormats.TopLeft);
            x += widths[i];
        }

        y += 16;
    }

    private async Task<ReportResult> GetSalesReportAsync(DateTime startUtc, DateTime endUtc, string title)
    {
        await using var db = createDb();
        var sales = await db.Sales
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Payments)
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .Where(x => x.Status == "Completed" && x.TransactionTime >= startUtc && x.TransactionTime <= endUtc)
            .OrderBy(x => x.TransactionTime)
            .ToListAsync();

        var rows = sales.Select(x => new ReportRow(
            x.TransactionTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            x.InvoiceNumber,
            x.User?.FullName ?? "-",
            string.Join(", ", x.Payments.Select(payment => payment.Method).Distinct()),
            x.GrandTotal.ToString("N0"),
            CalculateGrossProfit(x.Items).ToString("N0"))).ToList();

        return new ReportResult(
            title,
            ["Tanggal", "Invoice", "Kasir", "Metode", "Total", "Laba Kotor"],
            rows,
            sales.Sum(x => x.GrandTotal),
            sales.Sum(x => CalculateGrossProfit(x.Items)),
            sales.Count);
    }

    private async Task<ReportResult> GetTopProductsAsync(DateTime startUtc, DateTime endUtc)
    {
        await using var db = createDb();
        var items = await db.SaleItems
            .AsNoTracking()
            .Include(x => x.Sale)
            .Include(x => x.Product)
            .Where(x => x.Sale != null && x.Sale.Status == "Completed" && x.Sale.TransactionTime >= startUtc && x.Sale.TransactionTime <= endUtc)
            .ToListAsync();

        var groups = items
            .GroupBy(x => new { x.ProductId, x.ProductNameSnapshot })
            .Select(group =>
            {
                var quantity = group.Sum(x => x.Quantity);
                var total = group.Sum(x => x.LineTotal);
                var profit = group.Sum(x => x.LineTotal - ((x.Product?.PurchasePrice ?? 0) * x.Quantity));
                return new { group.Key.ProductNameSnapshot, Quantity = quantity, Total = total, Profit = profit };
            })
            .ToList();

        var rows = groups
            .OrderByDescending(x => x.Quantity)
            .Select(x => new ReportRow(x.ProductNameSnapshot, x.Quantity.ToString("N2"), x.Total.ToString("N0"), x.Profit.ToString("N0"), "-", "-"))
            .ToList();

        return new ReportResult("Produk Terlaris", ["Produk", "Qty", "Total", "Laba", "-", "-"], rows, groups.Sum(x => x.Total), groups.Sum(x => x.Profit), items.Select(x => x.SaleId).Distinct().Count());
    }

    private async Task<ReportResult> GetCashierReportAsync(DateTime startUtc, DateTime endUtc)
    {
        await using var db = createDb();
        var sales = await db.Sales
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Payments)
            .Where(x => x.Status == "Completed" && x.TransactionTime >= startUtc && x.TransactionTime <= endUtc)
            .ToListAsync();

        var rows = sales
            .GroupBy(x => x.User?.FullName ?? "-")
            .Select(group =>
            {
                var cash = group.SelectMany(x => x.Payments).Where(x => x.Method == "Cash").Sum(x => x.Amount - x.ChangeAmount);
                var nonCash = group.SelectMany(x => x.Payments).Where(x => x.Method != "Cash").Sum(x => x.Amount);
                return new ReportRow(group.Key, group.Count().ToString("N0"), cash.ToString("N0"), nonCash.ToString("N0"), group.Sum(x => x.GrandTotal).ToString("N0"), "-");
            })
            .ToList();

        return new ReportResult("Laporan Kasir", ["Kasir", "Transaksi", "Cash", "Non Cash", "Total", "-"], rows, sales.Sum(x => x.GrandTotal), 0, sales.Count);
    }

    private async Task<ReportResult> GetLowStockReportAsync()
    {
        await using var db = createDb();
        var products = await db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Where(x => x.IsActive)
            .ToListAsync();

        var rows = products
            .Where(x => x.Stock <= x.MinimumStock)
            .OrderBy(x => x.Stock)
            .Select(x => new ReportRow(
            x.Code,
            x.Name,
            x.Category?.Name ?? "-",
            x.Stock.ToString("N2"),
            x.MinimumStock.ToString("N2"),
            x.Unit?.Symbol ?? "-")).ToList();

        return new ReportResult("Stok Hampir Habis", ["Kode", "Produk", "Kategori", "Stok", "Minimum", "Satuan"], rows, 0, 0, rows.Count);
    }

    private async Task<ReportResult> GetGrossProfitReportAsync(DateTime startUtc, DateTime endUtc)
    {
        await using var db = createDb();
        var items = await db.SaleItems
            .AsNoTracking()
            .Include(x => x.Sale)
            .Include(x => x.Product)
            .Where(x => x.Sale != null && x.Sale.Status == "Completed" && x.Sale.TransactionTime >= startUtc && x.Sale.TransactionTime <= endUtc)
            .ToListAsync();

        var lineRows = items.Select(x =>
        {
            var cost = (x.Product?.PurchasePrice ?? 0) * x.Quantity;
            var profit = x.LineTotal - cost;
            return new
            {
                Row = new ReportRow(x.ProductNameSnapshot, x.Quantity.ToString("N2"), x.LineTotal.ToString("N0"), cost.ToString("N0"), profit.ToString("N0"), x.Sale?.InvoiceNumber ?? "-"),
                x.LineTotal,
                Profit = profit
            };
        }).ToList();

        return new ReportResult(
            "Laba Kotor Sederhana",
            ["Produk", "Qty", "Penjualan", "HPP", "Laba", "Invoice"],
            lineRows.Select(x => x.Row).ToList(),
            lineRows.Sum(x => x.LineTotal),
            lineRows.Sum(x => x.Profit),
            items.Select(x => x.SaleId).Distinct().Count());
    }

    private async Task<ReportResult> GetOperationalSummaryAsync(DateTime startUtc, DateTime endUtc)
    {
        await using var db = createDb();
        var sales = await db.Sales
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .Where(x => x.Status == "Completed" && x.TransactionTime >= startUtc && x.TransactionTime <= endUtc)
            .ToListAsync();

        var purchases = await db.Purchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= startUtc && x.PurchaseDate <= endUtc)
            .ToListAsync();

        var expenses = await db.Expenses
            .AsNoTracking()
            .Where(x => x.ExpenseDate >= startUtc && x.ExpenseDate <= endUtc)
            .ToListAsync();

        var totalSales = sales.Sum(x => x.GrandTotal);
        var grossProfit = sales.Sum(x => CalculateGrossProfit(x.Items));
        var totalPurchases = purchases.Sum(x => x.TotalAmount);
        var totalExpenses = expenses.Sum(x => x.Amount);
        var estimatedNet = grossProfit - totalExpenses;

        var rows = new List<ReportRow>
        {
            new("Total Penjualan", totalSales.ToString("N0"), sales.Count.ToString("N0"), "-", "-", "-"),
            new("Total Pembelian", totalPurchases.ToString("N0"), purchases.Count.ToString("N0"), "-", "-", "-"),
            new("Total Pengeluaran", totalExpenses.ToString("N0"), expenses.Count.ToString("N0"), "-", "-", "-"),
            new("Laba Kotor", grossProfit.ToString("N0"), "-", "-", "-", "-"),
            new("Estimasi Laba Setelah Pengeluaran", estimatedNet.ToString("N0"), "-", "-", "-", "-")
        };

        return new ReportResult("Ringkasan Operasional", ["Metrik", "Nominal", "Jumlah", "-", "-", "-"], rows, totalSales, grossProfit, sales.Count);
    }

    private static decimal CalculateGrossProfit(IEnumerable<SaleItem> items)
    {
        return items.Sum(x => x.LineTotal - ((x.Product?.PurchasePrice ?? 0) * x.Quantity));
    }
}
