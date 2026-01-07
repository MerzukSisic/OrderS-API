using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Notes { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<OrderItemAccompaniment> OrderItemAccompaniments { get; set; } = new List<OrderItemAccompaniment>();
}