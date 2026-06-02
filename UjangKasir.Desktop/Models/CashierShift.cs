namespace UjangKasir.Desktop.Models;

public class CashierShift
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public string Status { get; set; } = "Open";

    public User? User { get; set; }
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
