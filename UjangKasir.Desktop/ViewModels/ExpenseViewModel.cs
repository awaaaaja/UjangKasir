using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class ExpenseViewModel : PageViewModelBase
{
    private readonly ExpenseService expenseService;
    private readonly User currentUser;

    public ObservableCollection<Expense> Expenses { get; } = new();
    public string[] PaymentMethods { get; } = ["Cash", "Transfer", "Debit", "E-Wallet"];
    public override string Title => "Pengeluaran";
    public override string Description => "Catat dan filter biaya operasional toko.";

    [ObservableProperty]
    private Expense selectedExpense;

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime toDate = DateTime.Today;

    [ObservableProperty]
    private string categoryFilter = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public decimal TotalExpense => Expenses.Sum(x => x.Amount);
    public string TotalExpenseLabel => TotalExpense.ToString("N0");

    public ExpenseViewModel(ExpenseService expenseService, User currentUser)
    {
        this.expenseService = expenseService;
        this.currentUser = currentUser;
        selectedExpense = NewExpense();
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var expenses = await expenseService.GetAsync(FromDate, ToDate, CategoryFilter);
            Expenses.Clear();
            foreach (var expense in expenses)
            {
                Expenses.Add(expense);
            }

            OnPropertyChanged(nameof(TotalExpense));
            OnPropertyChanged(nameof(TotalExpenseLabel));
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load expenses");
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
        SelectedExpense = NewExpense();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            SelectedExpense.CreatedByUserId = currentUser.Id;
            await expenseService.SaveAsync(SelectedExpense);
            StatusMessage = "Pengeluaran tersimpan.";
            SelectedExpense = NewExpense();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save expense");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedExpense.Id <= 0)
        {
            StatusMessage = "Pilih pengeluaran terlebih dahulu.";
            return;
        }

        try
        {
            IsBusy = true;
            await expenseService.DeleteAsync(SelectedExpense.Id);
            StatusMessage = "Pengeluaran dihapus.";
            SelectedExpense = NewExpense();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Delete expense");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Expense NewExpense()
    {
        return new Expense
        {
            ExpenseDate = DateTime.Today,
            PaymentMethod = "Cash",
            CreatedByUserId = currentUser.Id
        };
    }
}
