using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class ProcurementOrder
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string Supplier { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? StripePaymentIntentId { get; set; }

    public string? Notes { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProcurementOrderItem> ProcurementOrderItems { get; set; } = new List<ProcurementOrderItem>();

    public virtual Store Store { get; set; } = null!;
}
