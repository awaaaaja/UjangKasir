using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AuthService authService;
    private readonly PermissionService permissionService;
    private readonly ProductMasterService productMasterService;
    private readonly BarcodeService barcodeService;
    private readonly POSService posService;
    private readonly ShiftService shiftService;
    private readonly ReportService reportService;
    private readonly BackupService backupService;
    private readonly UserService userService;
    private readonly InventoryService inventoryService;
    private readonly SupplierService supplierService;
    private readonly PurchaseService purchaseService;
    private readonly MemberService memberService;
    private readonly PromoService promoService;
    private readonly ExpenseService expenseService;
    private readonly HeldTransactionService heldTransactionService;
    private readonly User currentUser;
    private readonly Action logoutRequested;

    private static readonly NavigationItemViewModel[] AllNavigationItems =
    [
        new("Dashboard", "Dashboard"),
        new("POS", "Kasir"),
        new("Shift", "Shift Kasir"),
        new("Product", "Produk"),
        new("Inventory", "Inventory"),
        new("Purchase", "Pembelian"),
        new("Supplier", "Supplier"),
        new("Member", "Member"),
        new("Promo", "Promo"),
        new("Expense", "Pengeluaran"),
        new("Report", "Laporan"),
        new("Backup", "Backup"),
        new("Setting", "Pengaturan")
    ];

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPageTitle))]
    private PageViewModelBase currentViewModel;

    public MainViewModel(
        AuthService authService,
        PermissionService permissionService,
        ProductMasterService productMasterService,
        BarcodeService barcodeService,
        POSService posService,
        ShiftService shiftService,
        ReportService reportService,
        BackupService backupService,
        UserService userService,
        InventoryService inventoryService,
        SupplierService supplierService,
        PurchaseService purchaseService,
        MemberService memberService,
        PromoService promoService,
        ExpenseService expenseService,
        HeldTransactionService heldTransactionService,
        User currentUser,
        Action logoutRequested)
    {
        this.authService = authService;
        this.permissionService = permissionService;
        this.productMasterService = productMasterService;
        this.barcodeService = barcodeService;
        this.posService = posService;
        this.shiftService = shiftService;
        this.reportService = reportService;
        this.backupService = backupService;
        this.userService = userService;
        this.inventoryService = inventoryService;
        this.supplierService = supplierService;
        this.purchaseService = purchaseService;
        this.memberService = memberService;
        this.promoService = promoService;
        this.expenseService = expenseService;
        this.heldTransactionService = heldTransactionService;
        this.currentUser = currentUser;
        this.logoutRequested = logoutRequested;

        foreach (var item in AllNavigationItems.Where(item => permissionService.CanAccessMenu(currentUser, item.Key)))
        {
            NavigationItems.Add(new NavigationItemViewModel(item.Key, item.Title));
        }

        CurrentViewModel = new DashboardViewModel();
        SelectNavigationItem("Dashboard");
    }

    public string CurrentPageTitle => CurrentViewModel.Title;
    public string CurrentUserLabel => $"{currentUser.FullName} ({currentUser.Role?.Name})";

    [RelayCommand]
    private void Navigate(NavigationItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        CurrentViewModel = item.Key switch
        {
            "Dashboard" => new DashboardViewModel(),
            "POS" => new POSViewModel(productMasterService, posService, promoService, memberService, heldTransactionService, currentUser),
            "Shift" => new ShiftViewModel(shiftService, currentUser),
            "Product" => new ProductViewModel(productMasterService, barcodeService, currentUser),
            "Inventory" => new InventoryViewModel(inventoryService),
            "Report" => new ReportViewModel(reportService),
            "Setting" => new SettingViewModel(userService, currentUser),
            "Purchase" => new PurchaseViewModel(purchaseService, supplierService, productMasterService, currentUser),
            "Supplier" => new SupplierViewModel(supplierService),
            "Member" => new MemberViewModel(memberService),
            "Promo" => new PromoViewModel(promoService),
            "Expense" => new ExpenseViewModel(expenseService, currentUser),
            "Backup" => new BackupViewModel(backupService, currentUser),
            _ => new DashboardViewModel()
        };

        SelectNavigationItem(item.Key);
    }

    [RelayCommand]
    private void Logout()
    {
        authService.Logout();
        logoutRequested();
    }

    private void SelectNavigationItem(string key)
    {
        foreach (var navigationItem in NavigationItems)
        {
            navigationItem.IsSelected = navigationItem.Key == key;
        }
    }
}
