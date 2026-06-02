namespace UjangKasir.Desktop.Models;

public class HeldTransactionItem
{
    public int Id { get; set; }
    public int HeldTransactionId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }

    public HeldTransaction? HeldTransaction { get; set; }
    public Product? Product { get; set; }
}
