namespace OrdersAPI.Application.DTOs;

public class ReceiptDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPartnerOrder { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
}

public class ReceiptItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Notes { get; set; }
    public List<string> SelectedAccompaniments { get; set; } = new();
}

public class KitchenReceiptDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public List<KitchenReceiptItemDto> Items { get; set; } = new();
}

public class KitchenReceiptItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public List<string> SelectedAccompaniments { get; set; } = new();
    public List<string> Ingredients { get; set; } = new();
}

public class BarReceiptDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public List<BarReceiptItemDto> Items { get; set; } = new();
}

public class BarReceiptItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public List<string> SelectedAccompaniments { get; set; } = new();
}
