namespace UjangKasir.Desktop.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string BarcodeSnapshot { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
}
