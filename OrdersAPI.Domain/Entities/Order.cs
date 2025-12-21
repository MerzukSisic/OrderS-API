namespace OrdersAPI.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid WaiterId { get; set; }
    public Guid? TableId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public OrderType Type { get; set; } = OrderType.DineIn;
    public bool IsPartnerOrder { get; set; } = false;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public User Waiter { get; set; } = null!;
    public CafeTable? Table { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
