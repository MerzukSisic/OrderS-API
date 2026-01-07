namespace OrdersAPI.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid WaiterId { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public Guid? TableId { get; set; }
    public string? TableNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPartnerOrder { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PreparationLocation { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SelectedAccompanimentDto> SelectedAccompaniments { get; set; } = new();
}

public class CreateOrderDto
{
    public Guid? TableId { get; set; }
    public string Type { get; set; } = "DineIn";
    public bool IsPartnerOrder { get; set; } = false;
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public List<Guid> SelectedAccompanimentIds { get; set; } = new();
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateOrderItemStatusDto
{
    public string Status { get; set; } = string.Empty;
}