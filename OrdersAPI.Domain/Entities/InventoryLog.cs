using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Domain.Entities;

public class InventoryLog
{
    public Guid Id { get; set; }
    public Guid StoreProductId { get; set; }
    public int QuantityChange { get; set; }
    public InventoryLogType Type { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StoreProduct StoreProduct { get; set; } = null!;
}
