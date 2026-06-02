namespace UjangKasir.Desktop.Models;

public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }

    public Purchase? Purchase { get; set; }
    public Product? Product { get; set; }
}
