using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Domain.Entities;

public class ProcurementOrder
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public ProcurementStatus Status { get; set; } = ProcurementStatus.Pending;
    public string? StripePaymentIntentId { get; set; }
    public string? Notes { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public ICollection<ProcurementOrderItem> Items { get; set; } = new List<ProcurementOrderItem>();
}
