namespace UjangKasir.Desktop.Models;

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
