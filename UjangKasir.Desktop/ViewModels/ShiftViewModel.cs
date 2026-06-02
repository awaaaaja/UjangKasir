using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class ShiftViewModel : PageViewModelBase
{
    private readonly ShiftService shiftService;
    private readonly User currentUser;

    public ObservableCollection<ShiftSummary> RecentShifts { get; } = new();

    public override string Title => "Shift Kasir";
    public override string Description => "Buka shift, tutup shift, hitung kas aktual, selisih, dan cetak laporan shift.";

    [ObservableProperty]
    private decimal openingCashInput;

    [ObservableProperty]
    private decimal closingCashInput;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveShift))]
    [NotifyPropertyChangedFor(nameof(ActiveShiftLabel))]
    private ShiftSummary? activeShiftSummary;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShiftReportText))]
    private ShiftSummary? selectedShiftSummary;

    [ObservableProperty]
    private string statusMessage = "";

    public bool HasActiveShift => ActiveShiftSummary is not null;
    public string ActiveShiftLabel => ActiveShiftSummary is null
        ? "Belum ada shift aktif."
        : $"Shift #{ActiveShiftSummary.ShiftId} dibuka {ActiveShiftSummary.OpenedAt.ToLocalTime():dd/MM/yyyy HH:mm}";

    public string ShiftReportText => SelectedShiftSummary is null
        ? "Pilih atau buka shift untuk melihat laporan."
        : shiftService.BuildPrintableReport(SelectedShiftSummary);

    public ShiftViewModel(ShiftService shiftService, User currentUser)
    {
        this.shiftService = shiftService;
        this.currentUser = currentUser;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var activeShift = await shiftService.GetActiveShiftByUserAsync(currentUser.Id);
            ActiveShiftSummary = activeShift is null
                ? null
                : await shiftService.CalculateShiftSummaryAsync(activeShift.Id);

            SelectedShiftSummary = ActiveShiftSummary;
            await LoadRecentShiftsAsync();
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load shift");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        try
        {
            ActiveShiftSummary = await shiftService.OpenShiftAsync(currentUser.Id, OpeningCashInput);
            SelectedShiftSummary = ActiveShiftSummary;
            ClosingCashInput = ActiveShiftSummary.ExpectedCash;
            StatusMessage = "Shift berhasil dibuka.";
            await LoadRecentShiftsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Open shift");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        try
        {
            if (ActiveShiftSummary is null)
            {
                StatusMessage = "Tidak ada shift aktif yang bisa ditutup.";
                return;
            }

            var closedSummary = await shiftService.CloseShiftAsync(currentUser.Id, ClosingCashInput);
            ActiveShiftSummary = null;
            SelectedShiftSummary = closedSummary;
            StatusMessage = "Shift berhasil ditutup.";
            await LoadRecentShiftsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Close shift");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RefreshSummaryAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void PrintShiftReport()
    {
        if (SelectedShiftSummary is null)
        {
            StatusMessage = "Pilih shift terlebih dahulu.";
            return;
        }

        var document = new FlowDocument(new Paragraph(new Run(ShiftReportText)))
        {
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12,
            PagePadding = new Thickness(36)
        };

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Laporan Shift #{SelectedShiftSummary.ShiftId}");
            StatusMessage = "Laporan shift dikirim ke printer.";
        }
    }

    partial void OnSelectedShiftSummaryChanged(ShiftSummary? value)
    {
        OnPropertyChanged(nameof(ShiftReportText));
    }

    private async Task LoadRecentShiftsAsync()
    {
        var summaries = await shiftService.GetRecentShiftSummariesAsync();
        RecentShifts.Clear();
        foreach (var summary in summaries)
        {
            RecentShifts.Add(summary);
        }
    }
}
