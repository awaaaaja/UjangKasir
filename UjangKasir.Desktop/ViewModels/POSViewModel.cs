using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class POSViewModel : PageViewModelBase
{
    private readonly ProductMasterService productMasterService;
    private readonly POSService posService;
    private readonly PromoService promoService;
    private readonly MemberService memberService;
    private readonly HeldTransactionService heldTransactionService;
    private readonly User currentUser;

    public ObservableCollection<Product> QuickProducts { get; } = new();
    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();
    public ObservableCollection<Member> MemberResults { get; } = new();
    public ObservableCollection<HeldTransaction> HeldTransactions { get; } = new();

    public override string Title => "Kasir";
    public override string Description => "Transaksi kasir, scan barcode, hold/resume, promo, dan checkout database.";

    [ObservableProperty]
    private string barcodeInput = "";

    [ObservableProperty]
    private string productSearchText = "";

    [ObservableProperty]
    private Product? selectedQuickProduct;

    [ObservableProperty]
    private CartItemViewModel? selectedCartItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    [NotifyPropertyChangedFor(nameof(ChangeAmountLabel))]
    private decimal paidAmount;

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private string selectedPaymentMethod = "Cash";

    [ObservableProperty]
    private string memberSearchText = "";

    [ObservableProperty]
    private Member? selectedMember;

    [ObservableProperty]
    private string promoCode = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(TotalLabel))]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    [NotifyPropertyChangedFor(nameof(ChangeAmountLabel))]
    private decimal promoDiscount;

    [ObservableProperty]
    private HeldTransaction? selectedHeldTransaction;

    [ObservableProperty]
    private string checkoutSummary = "";

    [ObservableProperty]
    private bool isBarcodeFocused = true;

    [ObservableProperty]
    private bool isSearchFocused;

    public POSViewModel(
        ProductMasterService productMasterService,
        POSService posService,
        PromoService promoService,
        MemberService memberService,
        HeldTransactionService heldTransactionService,
        User currentUser)
    {
        this.productMasterService = productMasterService;
        this.posService = posService;
        this.promoService = promoService;
        this.memberService = memberService;
        this.heldTransactionService = heldTransactionService;
        this.currentUser = currentUser;
        CartItems.CollectionChanged += OnCartChanged;
        _ = LoadQuickProductsAsync();
        _ = LoadHeldTransactionsAsync();
    }

    public string[] PaymentMethods { get; } = ["Cash", "QRIS Manual", "Transfer", "Debit", "E-Wallet"];
    public decimal CartTotal => CartItems.Sum(x => x.Subtotal);
    public decimal Total => Math.Max(0, CartTotal - PromoDiscount);
    public decimal ChangeAmount => SelectedPaymentMethod == "Cash" ? PaidAmount - Total : 0;
    public string TotalLabel => Total.ToString("N0");
    public string PromoDiscountLabel => PromoDiscount.ToString("N0");
    public string ChangeAmountLabel => ChangeAmount.ToString("N0");
    public string ItemCountLabel => $"{CartItems.Sum(x => x.Qty):N0} item";

    partial void OnProductSearchTextChanged(string value)
    {
        _ = LoadQuickProductsAsync();
    }

    partial void OnSelectedPaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(ChangeAmountLabel));
    }

    partial void OnMemberSearchTextChanged(string value)
    {
        _ = SearchMembersAsync();
    }

    [RelayCommand]
    private async Task LoadQuickProductsAsync()
    {
        var products = await productMasterService.GetProductsAsync(ProductSearchText, null, true);
        QuickProducts.Clear();

        foreach (var product in products.Take(80))
        {
            QuickProducts.Add(product);
        }
    }

    [RelayCommand]
    private async Task AddBarcodeAsync()
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            FocusBarcode();
            return;
        }

        var product = await productMasterService.FindActiveProductByBarcodeAsync(barcode);
        if (product is null)
        {
            StatusMessage = $"Barcode {barcode} tidak ditemukan.";
            BarcodeInput = "";
            FocusBarcode();
            return;
        }

        AddProductToCart(product);
        BarcodeInput = "";
        FocusBarcode();
    }

    [RelayCommand]
    private void AddProductToCart(Product? product)
    {
        if (product is null)
        {
            StatusMessage = "Pilih produk terlebih dahulu.";
            FocusBarcode();
            return;
        }

        var existing = CartItems.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing is not null)
        {
            IncreaseQty(existing);
            return;
        }

        if (product.Stock < 1)
        {
            StatusMessage = $"Stok {product.Name} tidak cukup.";
            FocusBarcode();
            return;
        }

        var item = new CartItemViewModel(product.Id, product.Name, product.SellingPrice, product.Stock);
        item.PropertyChanged += (_, _) => RefreshTotals();
        CartItems.Add(item);
        SelectedCartItem = item;
        StatusMessage = $"{product.Name} ditambahkan ke keranjang.";
        RefreshTotals();
        FocusBarcode();
    }

    [RelayCommand]
    private void IncreaseQty(CartItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.Qty + 1 > item.AvailableStock)
        {
            StatusMessage = $"Stok {item.ProductName} tidak cukup.";
            FocusBarcode();
            return;
        }

        item.Qty += 1;
        SelectedCartItem = item;
        StatusMessage = $"Qty {item.ProductName} ditambah.";
        RefreshTotals();
        FocusBarcode();
    }

    [RelayCommand]
    private void DecreaseQty(CartItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.Qty <= 1)
        {
            RemoveItem(item);
            return;
        }

        item.Qty -= 1;
        StatusMessage = $"Qty {item.ProductName} dikurangi.";
        RefreshTotals();
        FocusBarcode();
    }

    [RelayCommand]
    private void RemoveItem(CartItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        CartItems.Remove(item);
        StatusMessage = $"{item.ProductName} dihapus dari keranjang.";
        RefreshTotals();
        FocusBarcode();
    }

    [RelayCommand]
    private void RemoveSelectedItem()
    {
        RemoveItem(SelectedCartItem);
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        PaidAmount = 0;
        StatusMessage = "Keranjang dikosongkan.";
        RefreshTotals();
        FocusBarcode();
    }

    [RelayCommand]
    private void HoldTransaction()
    {
        _ = HoldTransactionAsync();
    }

    [RelayCommand]
    private void ResumeTransaction()
    {
        _ = ResumeTransactionAsync();
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (CartItems.Count == 0)
        {
            StatusMessage = "Keranjang masih kosong.";
            FocusBarcode();
            return;
        }

        if (SelectedPaymentMethod == "Cash" && PaidAmount < Total)
        {
            StatusMessage = "Uang bayar kurang dari total.";
            FocusBarcode();
            return;
        }

        try
        {
            var request = new POSCheckoutRequest(
                currentUser.Id,
                CartItems.Select(x => new POSCheckoutItem(
                    x.ProductId,
                    x.ProductName,
                    x.Qty,
                    x.UnitPrice,
                    x.Discount)).ToList(),
                SelectedPaymentMethod,
                SelectedPaymentMethod == "Cash" ? PaidAmount : Total,
                SelectedMember?.Id,
                PromoDiscount,
                PromoCode);

            var result = await posService.CheckoutAsync(request);
            CartItems.Clear();
            PaidAmount = 0;
            PromoDiscount = 0;
            PromoCode = "";
            CheckoutSummary = BuildCheckoutSummary(result);
            StatusMessage = $"Checkout sukses: {result.InvoiceNumber}.";
            await LoadQuickProductsAsync();
            RefreshTotals();
            FocusBarcode();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "POS checkout");
            StatusMessage = ex.Message;
            FocusBarcode();
        }
    }

    [RelayCommand]
    private async Task ApplyPromoAsync()
    {
        try
        {
            var calculation = await promoService.CalculateTransactionDiscountAsync(PromoCode, CartTotal);
            PromoDiscount = calculation.DiscountAmount;
            StatusMessage = $"Promo {calculation.Promo.Code} diterapkan. Diskon {PromoDiscount:N0}.";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Apply promo");
            PromoDiscount = 0;
            StatusMessage = ex.Message;
        }
        finally
        {
            RefreshTotals();
            FocusBarcode();
        }
    }

    [RelayCommand]
    private void FocusBarcode()
    {
        IsBarcodeFocused = true;
    }

    [RelayCommand]
    private void FocusSearch()
    {
        IsSearchFocused = true;
    }

    [RelayCommand]
    private void CancelDialog()
    {
        StatusMessage = "";
        FocusBarcode();
    }

    [RelayCommand]
    private void PrintReceipt()
    {
        if (string.IsNullOrWhiteSpace(CheckoutSummary))
        {
            StatusMessage = "Belum ada struk transaksi yang bisa dicetak.";
            FocusBarcode();
            return;
        }

        try
        {
            var document = new FlowDocument(new Paragraph(new Run(CheckoutSummary)))
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                PagePadding = new Thickness(24)
            };

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Struk UjangKasir");
                StatusMessage = "Struk dikirim ke printer.";
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Print receipt");
            StatusMessage = ex.Message;
        }
        finally
        {
            FocusBarcode();
        }
    }

    private void OnCartChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PromoDiscount = 0;
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalLabel));
        OnPropertyChanged(nameof(PromoDiscountLabel));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(ChangeAmountLabel));
        OnPropertyChanged(nameof(ItemCountLabel));
    }

    private async Task SearchMembersAsync()
    {
        try
        {
            var members = await memberService.SearchAsync(MemberSearchText, activeOnly: true);
            MemberResults.Clear();
            foreach (var member in members.Take(20))
            {
                MemberResults.Add(member);
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Search member POS");
            StatusMessage = ex.Message;
        }
    }

    private async Task LoadHeldTransactionsAsync()
    {
        try
        {
            var helds = await heldTransactionService.GetHeldTransactionsAsync(currentUser.Id);
            HeldTransactions.Clear();
            foreach (var held in helds)
            {
                HeldTransactions.Add(held);
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load held transactions");
            StatusMessage = ex.Message;
        }
    }

    private async Task HoldTransactionAsync()
    {
        try
        {
            var items = CartItems.Select(x => new HeldCartItem(
                x.ProductId,
                x.ProductName,
                x.Qty,
                x.UnitPrice,
                x.Discount,
                x.Subtotal)).ToList();

            var held = await heldTransactionService.HoldAsync(currentUser.Id, SelectedMember?.Id, items);
            CartItems.Clear();
            PaidAmount = 0;
            PromoDiscount = 0;
            PromoCode = "";
            StatusMessage = $"Transaksi di-hold: {held.HoldNumber}.";
            await LoadHeldTransactionsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Hold transaction");
            StatusMessage = ex.Message;
        }
        finally
        {
            RefreshTotals();
            FocusBarcode();
        }
    }

    private async Task ResumeTransactionAsync()
    {
        try
        {
            var held = SelectedHeldTransaction ?? HeldTransactions.FirstOrDefault();
            if (held is null)
            {
                StatusMessage = "Tidak ada transaksi hold.";
                FocusBarcode();
                return;
            }

            var resumed = await heldTransactionService.ResumeAsync(held.Id, currentUser.Id);
            CartItems.Clear();
            foreach (var item in resumed.Items)
            {
                var cartItem = new CartItemViewModel(item.ProductId, item.ProductName, item.UnitPrice, decimal.MaxValue)
                {
                    Qty = item.Quantity,
                    Discount = item.Discount
                };
                cartItem.PropertyChanged += (_, _) => RefreshTotals();
                CartItems.Add(cartItem);
            }

            if (resumed.MemberId.HasValue)
            {
                SelectedMember = await memberService.GetByIdAsync(resumed.MemberId.Value);
            }

            StatusMessage = $"Transaksi {resumed.HoldNumber} dilanjutkan.";
            await LoadHeldTransactionsAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Resume transaction");
            StatusMessage = ex.Message;
        }
        finally
        {
            RefreshTotals();
            FocusBarcode();
        }
    }

    private static string BuildCheckoutSummary(POSCheckoutResult result)
    {
        var lines = string.Join(Environment.NewLine, result.ReceiptLines.Select(x =>
            $"{x.ProductName} | {x.Qty:N0} x {x.UnitPrice:N0} = {x.Subtotal:N0}"));

        return $"""
               Invoice: {result.InvoiceNumber}
               Waktu: {result.TransactionTime.ToLocalTime():dd/MM/yyyy HH:mm}
               Metode: {result.PaymentMethod}
               Total: {result.GrandTotal:N0}
               Bayar: {result.PaidAmount:N0}
               Kembali: {result.ChangeAmount:N0}

               {lines}
               """;
    }
}
