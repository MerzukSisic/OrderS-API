namespace OrdersAPI.Domain.Entities;

public class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StoreProduct> StoreProducts { get; set; } = new List<StoreProduct>();
}
