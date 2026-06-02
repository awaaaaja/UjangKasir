using CommunityToolkit.Mvvm.ComponentModel;

namespace UjangKasir.Desktop.ViewModels;

public partial class CartItemViewModel(int productId, string productName, decimal unitPrice, decimal availableStock) : ObservableObject
{
    public int ProductId { get; } = productId;
    public string ProductName { get; } = productName;
    public decimal UnitPrice { get; } = unitPrice;
    public decimal AvailableStock { get; } = availableStock;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal qty = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal discount;

    public decimal Subtotal => Qty * UnitPrice - Discount;
}
