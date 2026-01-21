namespace OrdersAPI.Domain.Entities;

public class StoreProduct
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; } = 10;
    public string Unit { get; set; } = "pcs";
    public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
}
