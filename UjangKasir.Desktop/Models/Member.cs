namespace UjangKasir.Desktop.Models;

public class Member
{
    public int Id { get; set; }
    public string MemberCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public int Points { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<HeldTransaction> HeldTransactions { get; set; } = new List<HeldTransaction>();
}
