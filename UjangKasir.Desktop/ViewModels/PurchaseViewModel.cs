using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class PurchaseLineViewModel(Product product) : ObservableObject
{
    public Product Product { get; } = product;
    public string ProductName => Product.Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal quantity = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal unitCost = product.PurchasePrice;

    public decimal Subtotal => Quantity * UnitCost;
}

public partial class PurchaseViewModel : PageViewModelBase
{
    private readonly PurchaseService purchaseService;
    private readonly SupplierService supplierService;
    private readonly ProductMasterService productMasterService;
    private readonly User currentUser;

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<PurchaseLineViewModel> Lines { get; } = new();
    public ObservableCollection<Purchase> Purchases { get; } = new();
    public override string Title => "Pembelian";
    public override string Description => "Catat stok masuk dari supplier dengan transaksi database.";

    [ObservableProperty]
    private Supplier? selectedSupplier;

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private decimal quantity = 1;

    [ObservableProperty]
    private decimal unitCost;

    [ObservableProperty]
    private decimal paidAmount;

    [ObservableProperty]
    private string notes = "";

    [ObservableProperty]
    private string productSearchText = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public decimal Total => Lines.Sum(x => x.Subtotal);
    public string TotalLabel => Total.ToString("N0");

    public PurchaseViewModel(
        PurchaseService purchaseService,
        SupplierService supplierService,
        ProductMasterService productMasterService,
        User currentUser)
    {
        this.purchaseService = purchaseService;
        this.supplierService = supplierService;
        this.productMasterService = productMasterService;
        this.currentUser = currentUser;
        Lines.CollectionChanged += (_, _) => RefreshTotal();
        _ = LoadAsync();
    }

    partial void OnProductSearchTextChanged(string value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        UnitCost = value?.PurchasePrice ?? 0;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            Suppliers.Clear();
            foreach (var supplier in await supplierService.GetActiveAsync())
            {
                Suppliers.Add(supplier);
            }

            await LoadProductsAsync();
            await LoadPurchasesAsync();
            SelectedSupplier ??= Suppliers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load purchases");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk terlebih dahulu.";
            return;
        }

        var line = new PurchaseLineViewModel(SelectedProduct)
        {
            Quantity = Quantity,
            UnitCost = UnitCost
        };
        line.PropertyChanged += (_, _) => RefreshTotal();
        Lines.Add(line);
        RefreshTotal();
        StatusMessage = $"{SelectedProduct.Name} ditambahkan.";
    }

    [RelayCommand]
    private void RemoveLine(PurchaseLineViewModel? line)
    {
        if (line is not null)
        {
            Lines.Remove(line);
            RefreshTotal();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            var inputs = Lines.Select(x => new PurchaseLineInput(x.Product.Id, x.Quantity, x.UnitCost)).ToList();
            var purchase = await purchaseService.CreatePurchaseAsync(SelectedSupplier?.Id ?? 0, inputs, PaidAmount, Notes, currentUser.Id);
            Lines.Clear();
            PaidAmount = 0;
            Notes = "";
            StatusMessage = $"Pembelian tersimpan: {purchase.PurchaseNumber}.";
            await LoadProductsAsync();
            await LoadPurchasesAsync();
            RefreshTotal();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save purchase");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProductsAsync()
    {
        Products.Clear();
        foreach (var product in await productMasterService.GetProductsAsync(ProductSearchText, null, true))
        {
            Products.Add(product);
        }
        SelectedProduct ??= Products.FirstOrDefault();
    }

    private async Task LoadPurchasesAsync()
    {
        Purchases.Clear();
        foreach (var purchase in await purchaseService.GetPurchasesAsync())
        {
            Purchases.Add(purchase);
        }
    }

    private void RefreshTotal()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalLabel));
    }
}
