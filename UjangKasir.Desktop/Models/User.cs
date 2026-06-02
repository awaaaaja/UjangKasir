namespace UjangKasir.Desktop.Models;

public class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Role? Role { get; set; }
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<CashierShift> CashierShifts { get; set; } = new List<CashierShift>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
