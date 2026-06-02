namespace UjangKasir.Desktop.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? UserId { get; set; }
    public string MovementType { get; set; } = "";
    public decimal QuantityChange { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    public string ReferenceType { get; set; } = "";
    public int? ReferenceId { get; set; }
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product? Product { get; set; }
    public User? User { get; set; }
}
