using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Services;
using UjangKasir.Desktop.ViewModels;
using UjangKasir.Desktop.Views;

namespace UjangKasir.Desktop;

public partial class App : Application
{
    private Func<AppDbContext>? createDb;
    private AuthService? authService;
    private PermissionService? permissionService;
    private ProductMasterService? productMasterService;
    private BarcodeService? barcodeService;
    private POSService? posService;
    private ShiftService? shiftService;
    private ReportService? reportService;
    private BackupService? backupService;
    private UserService? userService;
    private InventoryService? inventoryService;
    private SupplierService? supplierService;
    private PurchaseService? purchaseService;
    private MemberService? memberService;
    private PromoService? promoService;
    private ExpenseService? expenseService;
    private HeldTransactionService? heldTransactionService;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLogger.Log(args.Exception, "DispatcherUnhandledException");
            MessageBox.Show(
                "Terjadi error yang tidak terduga. Detail sudah dicatat di folder Logs.",
                "UjangKasir",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var configuredConnectionString = configuration.GetConnectionString("SQLite")
                ?? "Data Source=Data/ujangkasir.db";
            var connectionString = DatabasePathHelper.BuildSqliteConnectionString(configuredConnectionString);
            ErrorLogger.LogInfo($"Using SQLite database: {DatabasePathHelper.GetDataSource(connectionString)}", "Database");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;

            createDb = () => new AppDbContext(options);
            authService = new AuthService(createDb);
            permissionService = new PermissionService(createDb);
            productMasterService = new ProductMasterService(createDb);
            barcodeService = new BarcodeService(createDb);
            posService = new POSService(createDb);
            shiftService = new ShiftService(createDb);
            reportService = new ReportService(createDb);
            backupService = new BackupService(createDb);
            userService = new UserService(createDb);
            inventoryService = new InventoryService(createDb);
            supplierService = new SupplierService(createDb);
            purchaseService = new PurchaseService(createDb);
            memberService = new MemberService(createDb);
            promoService = new PromoService(createDb);
            expenseService = new ExpenseService(createDb);
            heldTransactionService = new HeldTransactionService(createDb);

            await using (var db = createDb())
            {
                await db.Database.MigrateAsync();
                await DbInitializer.SeedAsync(db);
            }

            ShowLogin();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Application startup");
            MessageBox.Show(
                $"Aplikasi gagal dijalankan: {ex.Message}",
                "UjangKasir",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ShowLogin()
    {
        if (authService is null)
        {
            throw new InvalidOperationException("AuthService belum siap.");
        }

        var loginViewModel = new LoginViewModel(authService);
        var loginView = new LoginView(loginViewModel);
        loginViewModel.LoginSucceeded += (_, user) =>
        {
            ShowMainWindow(user);
            loginView.Close();
        };

        loginView.Show();
    }

    private void ShowMainWindow(Models.User user)
    {
        if (authService is null || permissionService is null || productMasterService is null || barcodeService is null || posService is null || shiftService is null || reportService is null || backupService is null || userService is null || inventoryService is null || supplierService is null || purchaseService is null || memberService is null || promoService is null || expenseService is null || heldTransactionService is null)
        {
            throw new InvalidOperationException("Service aplikasi belum siap.");
        }

        MainWindow? mainWindow = null;
        var mainViewModel = new MainViewModel(authService, permissionService, productMasterService, barcodeService, posService, shiftService, reportService, backupService, userService, inventoryService, supplierService, purchaseService, memberService, promoService, expenseService, heldTransactionService, user, () =>
        {
            ShowLogin();
            mainWindow?.Close();
        });

        mainWindow = new MainWindow(mainViewModel);
        mainWindow.Show();
    }

}
