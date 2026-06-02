using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;
using ZXing;
using ZXing.Common;

namespace UjangKasir.Desktop.Services;

public record BarcodeOutput(string BarcodeValue, string BarcodePath, string QrCodePath, string LabelPath);

public class BarcodeService(Func<AppDbContext> createDb)
{
    private const int BarcodeWidth = 520;
    private const int BarcodeHeight = 150;
    private const int QrSize = 260;

    public async Task<BarcodeOutput> GenerateAssetsAsync(int productId, int userId)
    {
        await using var db = createDb();
        var product = await db.Products.FirstAsync(x => x.Id == productId);

        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            product.Barcode = await GenerateUniqueBarcodeAsync(db);
            product.UpdatedAt = DateTime.UtcNow;

            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "ProductBarcodeGenerated",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                Description = $"Barcode '{product.Barcode}' dibuat untuk produk '{product.Name}'.",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        return GenerateFiles(product);
    }

    public async Task<Product?> FindProductByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        await using var db = createDb();
        return await db.Products
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Include(x => x.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Barcode == barcode.Trim() && x.IsActive);
    }

    public BarcodeOutput GenerateFiles(Product product)
    {
        var code = ResolveBarcodeValue(product);
        var directory = GetProductBarcodeDirectory(product);
        Directory.CreateDirectory(directory);

        var barcodePath = Path.Combine(directory, $"{product.Code}_barcode.png");
        var qrCodePath = Path.Combine(directory, $"{product.Code}_qr.png");
        var labelPath = Path.Combine(directory, $"{product.Code}_label.png");

        SaveMatrixPng(code, BarcodeFormat.CODE_128, BarcodeWidth, BarcodeHeight, barcodePath);
        SaveMatrixPng(product.Code, BarcodeFormat.QR_CODE, QrSize, QrSize, qrCodePath);
        SaveLabelPng(product, barcodePath, labelPath);

        return new BarcodeOutput(code, barcodePath, qrCodePath, labelPath);
    }

    public void PrintLabel(Product product)
    {
        var output = GenerateFiles(product);
        var visual = CreateLabelVisual(product, output.BarcodePath);
        var printDialog = new PrintDialog();

        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintVisual(visual, $"Label Barcode {product.Code}");
        }
    }

    private static async Task<string> GenerateUniqueBarcodeAsync(AppDbContext db)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = $"UJ{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(0, 1000):000}";
            var exists = await db.Products.AnyAsync(x => x.Barcode == candidate);

            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Gagal membuat barcode unik. Coba ulangi beberapa saat lagi.");
    }

    private static string ResolveBarcodeValue(Product product)
    {
        return string.IsNullOrWhiteSpace(product.Barcode) ? product.Code : product.Barcode;
    }

    private static string GetProductBarcodeDirectory(Product product)
    {
        var safeCode = string.Join("_", product.Code.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(AppContext.BaseDirectory, "Generated", "Barcodes", safeCode);
    }

    private static void SaveMatrixPng(string value, BarcodeFormat format, int width, int height, string path)
    {
        var matrix = new MultiFormatWriter().encode(
            value,
            format,
            width,
            height,
            new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.MARGIN] = 2
            });

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            var cellWidth = (double)width / matrix.Width;
            var cellHeight = (double)height / matrix.Height;

            for (var y = 0; y < matrix.Height; y++)
            {
                for (var x = 0; x < matrix.Width; x++)
                {
                    if (matrix[x, y])
                    {
                        context.DrawRectangle(
                            Brushes.Black,
                            null,
                            new Rect(x * cellWidth, y * cellHeight, cellWidth, cellHeight));
                    }
                }
            }
        }

        SaveVisualAsPng(visual, width, height, path);
    }

    private static void SaveLabelPng(Product product, string barcodePath, string labelPath)
    {
        var visual = CreateLabelVisual(product, barcodePath);
        SaveVisualAsPng(visual, 420, 260, labelPath);
    }

    private static DrawingVisual CreateLabelVisual(Product product, string barcodePath)
    {
        var visual = new DrawingVisual();
        using var context = visual.RenderOpen();
        var bounds = new Rect(0, 0, 420, 260);
        context.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 1), bounds);

        DrawText(context, product.Name, 18, FontWeights.Bold, 18, 16, 384);
        DrawText(context, product.SellingPrice.ToString("C0", new CultureInfo("id-ID")), 18, FontWeights.SemiBold, 18, 48, 384);
        DrawText(context, $"Kode: {product.Code}", 12, FontWeights.Normal, 18, 78, 384);
        DrawText(context, $"Barcode: {ResolveBarcodeValue(product)}", 12, FontWeights.Normal, 18, 96, 384);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(barcodePath);
        bitmap.EndInit();
        bitmap.Freeze();

        context.DrawImage(bitmap, new Rect(18, 120, 384, 92));
        DrawText(context, ResolveBarcodeValue(product), 13, FontWeights.SemiBold, 18, 218, 384);
        return visual;
    }

    private static void DrawText(DrawingContext context, string text, double fontSize, FontWeight weight, double x, double y, double maxWidth)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            fontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip)
        {
            MaxTextWidth = maxWidth,
            Trimming = TextTrimming.CharacterEllipsis
        };

        context.DrawText(formattedText, new Point(x, y));
    }

    private static void SaveVisualAsPng(DrawingVisual visual, int width, int height, string path)
    {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
