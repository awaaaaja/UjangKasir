namespace UjangKasir.Desktop.Models;

public class Unit
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
