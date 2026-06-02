using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class ProductViewModel : PageViewModelBase
{
    private readonly ProductMasterService productMasterService;
    private readonly BarcodeService barcodeService;
    private readonly User currentUser;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Unit> Units { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public string[] ActiveFilterOptions { get; } = ["Semua", "Aktif", "Nonaktif"];

    public override string Title => "Produk";
    public override string Description => "Kelola produk, kategori, satuan, supplier, barcode, harga, dan stok.";

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private Category? filterCategory;

    [ObservableProperty]
    private string selectedActiveFilter = "Aktif";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private string scanBarcodeText = "";

    [ObservableProperty]
    private string scanResultMessage = "";

    [ObservableProperty]
    private string barcodePreviewPath = "";

    [ObservableProperty]
    private string qrCodePreviewPath = "";

    [ObservableProperty]
    private string labelPreviewPath = "";

    [ObservableProperty]
    private int editingProductId;

    [ObservableProperty]
    private string code = "";

    [ObservableProperty]
    private string barcode = "";

    [ObservableProperty]
    private string productName = "";

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private Unit? selectedUnit;

    [ObservableProperty]
    private Supplier? selectedSupplier;

    [ObservableProperty]
    private decimal purchasePrice;

    [ObservableProperty]
    private decimal sellingPrice;

    [ObservableProperty]
    private decimal stock;

    [ObservableProperty]
    private decimal minimumStock;

    [ObservableProperty]
    private DateTime? expiredDate;

    [ObservableProperty]
    private bool productIsActive = true;

    [ObservableProperty]
    private Category? selectedMasterCategory;

    [ObservableProperty]
    private Unit? selectedMasterUnit;

    [ObservableProperty]
    private Supplier? selectedMasterSupplier;

    public ProductViewModel(ProductMasterService productMasterService, BarcodeService barcodeService, User currentUser)
    {
        this.productMasterService = productMasterService;
        this.barcodeService = barcodeService;
        this.currentUser = currentUser;
        _ = LoadAsync();
    }

    partial void OnSelectedMasterCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(SelectedMasterCategory));
    }

    partial void OnSelectedMasterUnitChanged(Unit? value)
    {
        OnPropertyChanged(nameof(SelectedMasterUnit));
    }

    partial void OnSelectedMasterSupplierChanged(Supplier? value)
    {
        OnPropertyChanged(nameof(SelectedMasterSupplier));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadReferencesAsync();
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        Products.Clear();
        var isActive = SelectedActiveFilter switch
        {
            "Aktif" => true,
            "Nonaktif" => false,
            _ => (bool?)null
        };

        var products = await productMasterService.GetProductsAsync(SearchText, FilterCategory?.Id, isActive);
        foreach (var product in products)
        {
            Products.Add(product);
        }
    }

    [RelayCommand]
    private async Task ResetFilterAsync()
    {
        SearchText = "";
        FilterCategory = null;
        SelectedActiveFilter = "Aktif";
        await LoadProductsAsync();
    }

    [RelayCommand]
    private void NewProduct()
    {
        EditingProductId = 0;
        Code = "";
        Barcode = "";
        ProductName = "";
        SelectedCategory = Categories.FirstOrDefault(x => x.IsActive);
        SelectedUnit = Units.FirstOrDefault(x => x.IsActive);
        SelectedSupplier = Suppliers.FirstOrDefault(x => x.IsActive);
        PurchasePrice = 0;
        SellingPrice = 0;
        Stock = 0;
        MinimumStock = 0;
        ExpiredDate = null;
        ProductIsActive = true;
        StatusMessage = "Form produk baru siap diisi.";
    }

    [RelayCommand]
    private void EditSelectedProduct()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk yang ingin diedit.";
            return;
        }

        EditingProductId = SelectedProduct.Id;
        Code = SelectedProduct.Code;
        Barcode = SelectedProduct.Barcode;
        ProductName = SelectedProduct.Name;
        SelectedCategory = Categories.FirstOrDefault(x => x.Id == SelectedProduct.CategoryId);
        SelectedUnit = Units.FirstOrDefault(x => x.Id == SelectedProduct.UnitId);
        SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == SelectedProduct.SupplierId);
        PurchasePrice = SelectedProduct.PurchasePrice;
        SellingPrice = SelectedProduct.SellingPrice;
        Stock = SelectedProduct.Stock;
        MinimumStock = SelectedProduct.MinimumStock;
        ExpiredDate = SelectedProduct.ExpiredDate;
        ProductIsActive = SelectedProduct.IsActive;
        BarcodePreviewPath = "";
        QrCodePreviewPath = "";
        LabelPreviewPath = "";
        StatusMessage = $"Mengedit produk {SelectedProduct.Name}.";
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        try
        {
            if (SelectedCategory is null || SelectedUnit is null)
            {
                StatusMessage = "Kategori dan satuan wajib dipilih.";
                return;
            }

            var product = new Product
            {
                Id = EditingProductId,
                Code = Code.Trim(),
                Barcode = Barcode.Trim(),
                Name = ProductName.Trim(),
                CategoryId = SelectedCategory.Id,
                UnitId = SelectedUnit.Id,
                SupplierId = SelectedSupplier?.Id,
                PurchasePrice = PurchasePrice,
                SellingPrice = SellingPrice,
                Stock = Stock,
                MinimumStock = MinimumStock,
                ExpiredDate = ExpiredDate,
                IsActive = ProductIsActive
            };

            await productMasterService.SaveProductAsync(product, currentUser.Id);
            StatusMessage = EditingProductId == 0 ? "Produk berhasil ditambahkan." : "Produk berhasil diperbarui.";
            NewProduct();
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save product");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateSelectedProductAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk yang ingin dinonaktifkan.";
            return;
        }

        await productMasterService.DeactivateProductAsync(SelectedProduct.Id, currentUser.Id);
        StatusMessage = $"Produk {SelectedProduct.Name} dinonaktifkan.";
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task GenerateBarcodeAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk terlebih dahulu.";
            return;
        }

        try
        {
            var output = await barcodeService.GenerateAssetsAsync(SelectedProduct.Id, currentUser.Id);
            SelectedProduct.Barcode = output.BarcodeValue;
            Barcode = output.BarcodeValue;
            BarcodePreviewPath = output.BarcodePath;
            LabelPreviewPath = output.LabelPath;
            StatusMessage = $"Barcode dibuat: {output.BarcodePath}";
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Generate barcode");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task GenerateQrCodeAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk terlebih dahulu.";
            return;
        }

        try
        {
            var output = await barcodeService.GenerateAssetsAsync(SelectedProduct.Id, currentUser.Id);
            SelectedProduct.Barcode = output.BarcodeValue;
            QrCodePreviewPath = output.QrCodePath;
            LabelPreviewPath = output.LabelPath;
            StatusMessage = $"QR code dibuat: {output.QrCodePath}";
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Generate QR code");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task PreviewBarcodeAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk terlebih dahulu.";
            return;
        }

        try
        {
            var output = await barcodeService.GenerateAssetsAsync(SelectedProduct.Id, currentUser.Id);
            SelectedProduct.Barcode = output.BarcodeValue;
            Barcode = output.BarcodeValue;
            BarcodePreviewPath = output.BarcodePath;
            QrCodePreviewPath = output.QrCodePath;
            LabelPreviewPath = output.LabelPath;
            StatusMessage = "Preview barcode dan QR code diperbarui.";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Preview barcode");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task PrintLabelBarcodeAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Pilih produk terlebih dahulu.";
            return;
        }

        try
        {
            var output = await barcodeService.GenerateAssetsAsync(SelectedProduct.Id, currentUser.Id);
            SelectedProduct.Barcode = output.BarcodeValue;
            BarcodePreviewPath = output.BarcodePath;
            LabelPreviewPath = output.LabelPath;
            barcodeService.PrintLabel(SelectedProduct);
            StatusMessage = "Dialog print label barcode dibuka.";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Print barcode label");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        var product = await barcodeService.FindProductByBarcodeAsync(ScanBarcodeText);
        if (product is null)
        {
            ScanResultMessage = "Produk tidak ditemukan.";
            return;
        }

        ScanResultMessage = $"Ditemukan: {product.Name} - {product.SellingPrice:N0}";
        SelectedProduct = Products.FirstOrDefault(x => x.Id == product.Id) ?? product;
        EditSelectedProduct();
    }

    [RelayCommand]
    private void NewCategory()
    {
        SelectedMasterCategory = new Category { IsActive = true };
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (SelectedMasterCategory is null)
        {
            StatusMessage = "Pilih atau tambah kategori terlebih dahulu.";
            return;
        }

        await SaveReferenceAsync(
            () => productMasterService.SaveCategoryAsync(SelectedMasterCategory),
            "Kategori tersimpan.");
    }

    [RelayCommand]
    private async Task DeactivateCategoryAsync()
    {
        if (SelectedMasterCategory?.Id > 0)
        {
            await SaveReferenceAsync(
                () => productMasterService.DeactivateCategoryAsync(SelectedMasterCategory.Id),
                "Kategori dinonaktifkan.");
        }
    }

    [RelayCommand]
    private void NewUnit()
    {
        SelectedMasterUnit = new Unit { IsActive = true };
    }

    [RelayCommand]
    private async Task SaveUnitAsync()
    {
        if (SelectedMasterUnit is null)
        {
            StatusMessage = "Pilih atau tambah satuan terlebih dahulu.";
            return;
        }

        await SaveReferenceAsync(
            () => productMasterService.SaveUnitAsync(SelectedMasterUnit),
            "Satuan tersimpan.");
    }

    [RelayCommand]
    private async Task DeactivateUnitAsync()
    {
        if (SelectedMasterUnit?.Id > 0)
        {
            await SaveReferenceAsync(
                () => productMasterService.DeactivateUnitAsync(SelectedMasterUnit.Id),
                "Satuan dinonaktifkan.");
        }
    }

    [RelayCommand]
    private void NewSupplier()
    {
        SelectedMasterSupplier = new Supplier { IsActive = true };
    }

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        if (SelectedMasterSupplier is null)
        {
            StatusMessage = "Pilih atau tambah supplier terlebih dahulu.";
            return;
        }

        await SaveReferenceAsync(
            () => productMasterService.SaveSupplierAsync(SelectedMasterSupplier),
            "Supplier tersimpan.");
    }

    [RelayCommand]
    private async Task DeactivateSupplierAsync()
    {
        if (SelectedMasterSupplier?.Id > 0)
        {
            await SaveReferenceAsync(
                () => productMasterService.DeactivateSupplierAsync(SelectedMasterSupplier.Id),
                "Supplier dinonaktifkan.");
        }
    }

    private async Task SaveReferenceAsync(Func<Task> saveAction, string successMessage)
    {
        try
        {
            await saveAction();
            StatusMessage = successMessage;
            await LoadReferencesAsync();
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save reference data");
            StatusMessage = ex.Message;
        }
    }

    private async Task LoadReferencesAsync()
    {
        Categories.Clear();
        Units.Clear();
        Suppliers.Clear();

        foreach (var category in await productMasterService.GetCategoriesAsync())
        {
            Categories.Add(category);
        }

        foreach (var unit in await productMasterService.GetUnitsAsync())
        {
            Units.Add(unit);
        }

        foreach (var supplier in await productMasterService.GetSuppliersAsync())
        {
            Suppliers.Add(supplier);
        }
    }
}
