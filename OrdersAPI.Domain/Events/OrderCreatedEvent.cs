namespace OrdersAPI.Domain.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public int? TableNumber { get; set; }
    public Guid WaiterId { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
}

public class OrderItemEvent
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string PreparationLocation { get; set; } = string.Empty;
}