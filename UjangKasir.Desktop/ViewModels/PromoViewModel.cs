using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class PromoViewModel : PageViewModelBase
{
    private readonly PromoService promoService;

    public ObservableCollection<Promo> Promos { get; } = new();
    public string[] PromoTypes { get; } = ["TransactionDiscount", "ProductDiscount"];
    public string[] DiscountTypes { get; } = ["Percent", "FixedAmount"];
    public override string Title => "Promo";
    public override string Description => "Kelola diskon transaksi dan kode promo.";

    [ObservableProperty]
    private Promo selectedPromo = new() { IsActive = true };

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public PromoViewModel(PromoService promoService)
    {
        this.promoService = promoService;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var promos = await promoService.SearchAsync(SearchText);
            Promos.Clear();
            foreach (var promo in promos)
            {
                Promos.Add(promo);
            }

            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load promos");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void New()
    {
        SelectedPromo = new Promo { IsActive = true };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            await promoService.SaveAsync(SelectedPromo);
            StatusMessage = "Promo tersimpan.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save promo");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedPromo.Id <= 0)
        {
            StatusMessage = "Pilih promo terlebih dahulu.";
            return;
        }

        try
        {
            IsBusy = true;
            await promoService.DeactivateAsync(SelectedPromo.Id);
            StatusMessage = "Promo dinonaktifkan.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Deactivate promo");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
