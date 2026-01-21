namespace OrdersAPI.Application.DTOs;

public class ProcurementOrderDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }
    public string? Notes { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<ProcurementOrderItemDto> Items { get; set; } = new();
}

public class ProcurementOrderItemDto
{
    public Guid Id { get; set; }
    public Guid StoreProductId { get; set; }
    public string StoreProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateProcurementDto
{
    public Guid StoreId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateProcurementItemDto> Items { get; set; } = new();
}

public class CreateProcurementItemDto
{
    public Guid StoreProductId { get; set; }
    public int Quantity { get; set; }
}

public class PaymentIntentDto
{
    public string ClientSecret { get; set; } = string.Empty;
}

public class ConfirmPaymentDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class ReceiveProcurementDto
{
    public List<ReceiveProcurementItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
}

public class ReceiveProcurementItemDto
{
    public Guid ItemId { get; set; }
    public int ReceivedQuantity { get; set; }
}

