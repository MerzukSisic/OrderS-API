namespace OrdersAPI.Infrastructure.Messaging.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid WaiterId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemData> Items { get; set; } = new();
}

public class OrderItemData
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
