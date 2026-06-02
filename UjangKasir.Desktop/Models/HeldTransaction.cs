namespace UjangKasir.Desktop.Models;

public class HeldTransaction
{
    public int Id { get; set; }
    public string HoldNumber { get; set; } = "";
    public int CashierUserId { get; set; }
    public int? MemberId { get; set; }
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? CashierUser { get; set; }
    public Member? Member { get; set; }
    public ICollection<HeldTransactionItem> Items { get; set; } = new List<HeldTransactionItem>();
}
