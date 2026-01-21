namespace OrdersAPI.Infrastructure.Messaging.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty; // ✅ DODANO
    public Guid WaiterId { get; set; }
    public string WaiterName { get; set; } = string.Empty; // ✅ DODANO
    public Guid? TableId { get; set; }
    public string? TableNumber { get; set; } // ✅ DODANO
    public string OrderType { get; set; } = string.Empty; // ✅ DODANO (DineIn/Takeaway)
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemData> Items { get; set; } = new();
}

public class OrderItemData
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty; // ✅ DODANO
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string PreparationLocation { get; set; } = string.Empty; // ✅ DODANO (Kitchen/Bar)
}