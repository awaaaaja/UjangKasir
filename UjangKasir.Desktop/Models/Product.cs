namespace UjangKasir.Desktop.Models;

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int UnitId { get; set; }
    public int? SupplierId { get; set; }
    public string Code { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Category? Category { get; set; }
    public Unit? Unit { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
