using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class InventoryViewModel : PageViewModelBase
{
    private readonly InventoryService inventoryService;

    public ObservableCollection<Product> LowStockProducts { get; } = new();
    public ObservableCollection<StockMovement> StockMovements { get; } = new();

    public override string Title => "Inventory";
    public override string Description => "Pantau stok hampir habis dan riwayat pergerakan stok.";

    [ObservableProperty]
    private string statusMessage = "";

    public InventoryViewModel(InventoryService inventoryService)
    {
        this.inventoryService = inventoryService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            LowStockProducts.Clear();
            foreach (var product in await inventoryService.GetLowStockProductsAsync())
            {
                LowStockProducts.Add(product);
            }

            StockMovements.Clear();
            foreach (var movement in await inventoryService.GetRecentStockMovementsAsync())
            {
                StockMovements.Add(movement);
            }

            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load inventory");
            StatusMessage = ex.Message;
        }
    }
}
