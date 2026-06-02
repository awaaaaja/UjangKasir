using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class SupplierViewModel : PageViewModelBase
{
    private readonly SupplierService supplierService;

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public override string Title => "Supplier";
    public override string Description => "Kelola data pemasok barang toko.";

    [ObservableProperty]
    private Supplier selectedSupplier = new() { IsActive = true };

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public SupplierViewModel(SupplierService supplierService)
    {
        this.supplierService = supplierService;
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
            var suppliers = await supplierService.SearchAsync(SearchText);
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }

            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load suppliers");
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
        SelectedSupplier = new Supplier { IsActive = true };
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            await supplierService.SaveAsync(SelectedSupplier);
            StatusMessage = "Supplier tersimpan.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save supplier");
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
        if (SelectedSupplier.Id <= 0)
        {
            StatusMessage = "Pilih supplier terlebih dahulu.";
            return;
        }

        try
        {
            IsBusy = true;
            await supplierService.DeactivateAsync(SelectedSupplier.Id);
            StatusMessage = "Supplier dinonaktifkan.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Deactivate supplier");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
