namespace UjangKasir.Desktop.Models;

public class Sale
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int UserId { get; set; }
    public int? CashierShiftId { get; set; }
    public int? MemberId { get; set; }
    public DateTime TransactionTime { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public CashierShift? CashierShift { get; set; }
    public Member? Member { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
