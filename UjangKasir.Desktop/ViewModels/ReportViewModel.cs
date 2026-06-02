using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class ReportViewModel(ReportService reportService) : PageViewModelBase
{
    public ObservableCollection<ReportRow> Rows { get; } = new();
    public IReadOnlyList<string> ReportTypes { get; } = ReportService.ReportTypes;

    public override string Title => "Laporan";
    public override string Description => "Laporan penjualan, produk terlaris, kasir, stok hampir habis, dan laba kotor sederhana.";

    [ObservableProperty]
    private DateTime startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime endDate = DateTime.Today;

    [ObservableProperty]
    private string selectedReportType = "Penjualan Harian";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSalesLabel))]
    [NotifyPropertyChangedFor(nameof(TotalGrossProfitLabel))]
    [NotifyPropertyChangedFor(nameof(TotalTransactionsLabel))]
    private ReportResult? currentReport;

    public string TotalSalesLabel => CurrentReport?.TotalSales.ToString("N0") ?? "0";
    public string TotalGrossProfitLabel => CurrentReport?.TotalGrossProfit.ToString("N0") ?? "0";
    public string TotalTransactionsLabel => CurrentReport?.TotalTransactions.ToString("N0") ?? "0";

    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            if (EndDate.Date < StartDate.Date)
            {
                StatusMessage = "Tanggal akhir tidak boleh lebih kecil dari tanggal mulai.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Membuat laporan...";
            CurrentReport = await reportService.GenerateAsync(SelectedReportType, StartDate, EndDate);
            Rows.Clear();
            foreach (var row in CurrentReport.Rows)
            {
                Rows.Add(row);
            }

            StatusMessage = $"Laporan {CurrentReport.Title} berhasil dibuat.";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Generate report");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!EnsureReportReady())
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"laporan-ujangkasir-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true && CurrentReport is not null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Export Excel...";
                await Task.Run(() => reportService.ExportExcel(CurrentReport, dialog.FileName));
                StatusMessage = $"Excel berhasil dibuat: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, "Export Excel");
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (!EnsureReportReady())
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"laporan-ujangkasir-{DateTime.Now:yyyyMMdd-HHmmss}.pdf"
        };

        if (dialog.ShowDialog() == true && CurrentReport is not null)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Export PDF...";
                await Task.Run(() => reportService.ExportPdf(CurrentReport, dialog.FileName));
                StatusMessage = $"PDF berhasil dibuat: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, "Export PDF");
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private bool EnsureReportReady()
    {
        if (CurrentReport is not null)
        {
            return true;
        }

        StatusMessage = "Buat laporan terlebih dahulu sebelum export.";
        return false;
    }
}
