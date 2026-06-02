namespace UjangKasir.Desktop.Models;

public class Payment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string Method { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public Sale? Sale { get; set; }
}
